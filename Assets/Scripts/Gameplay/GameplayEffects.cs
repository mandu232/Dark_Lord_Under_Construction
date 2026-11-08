using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Gameplay
{
    // A bounded, scene-owned renderer pool. Presentation never controls damage or ability timing.
    public sealed class GameplayEffects : MonoBehaviour
    {
        private sealed class Playback
        {
            public SpriteRenderer renderer;
            public GameplayEffectLibrary.Clip clip;
            public Transform follow;
            public bool follows;
            public Vector3 offset;
            public float elapsed, duration;
            public DarkLord.Actors.TopDownMotor motor;
        }
        private static readonly Dictionary<UnityEngine.SceneManagement.SceneHandle, GameplayEffects> scenes = new Dictionary<UnityEngine.SceneManagement.SceneHandle, GameplayEffects>();
        private static GameplayEffectLibrary library;
        private readonly List<Playback> pool = new List<Playback>();
        private UnityEngine.SceneManagement.SceneHandle sceneHandle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { scenes.Clear(); library = null; }

        public static void Show(Transform owner, GameplayEffectKind kind, Vector3 position,
            Vector2 direction = default, bool follow = false, float duration = 0, float scale = 1, float stretch = 1)
        {
            if (!Application.isPlaying || owner == null || !owner.gameObject.scene.IsValid()) return;
            if (library == null) library = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            var clip = library != null ? library.Find(kind) : null;
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
            var handle = owner.gameObject.scene.handle;
            if (!scenes.TryGetValue(handle, out var effects) || effects == null)
            {
                var root = new GameObject("Gameplay effects");
                SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
                effects = root.AddComponent<GameplayEffects>(); effects.sceneHandle = handle;
                scenes[handle] = effects;
            }
            effects.Play(owner, clip, position, direction, follow, duration, scale, stretch);
        }

        // A right-facing shot or stream whose art spans `reach` world units, fitted between two points.
        public static void ShowBetween(Transform owner, GameplayEffectKind kind, Vector3 from, Vector3 to, float reach)
        {
            Vector2 delta = to - from;
            Show(owner, kind, from, delta.normalized, stretch: Mathf.Max(.1f, delta.magnitude / reach));
        }

        private void Play(Transform owner, GameplayEffectLibrary.Clip clip, Vector3 position, Vector2 direction, bool follow, float duration, float scale, float stretch)
        {
            // Keep repeated healing/marks readable instead of layering identical effects on one actor.
            Playback entry = null;
            if (clip.kind == GameplayEffectKind.Heal || clip.kind == GameplayEffectKind.HolyHeal || clip.kind == GameplayEffectKind.Curse)
                foreach (var item in pool)
                    if (item.renderer.enabled && item.follow == owner && item.clip == clip) return;
            foreach (var item in pool) if (!item.renderer.enabled) { entry = item; break; }
            if (entry == null)
            {
                if (pool.Count >= 48) return;
                var node = new GameObject("Effect", typeof(SpriteRenderer)); node.transform.SetParent(transform, false);
                entry = new Playback { renderer = node.GetComponent<SpriteRenderer>() }; pool.Add(entry);
            }
            entry.clip = clip; entry.follow = owner; entry.follows = follow;
            position += (Vector3)clip.offset;
            entry.offset = position - owner.position; entry.elapsed = 0;
            entry.duration = duration > 0 ? duration : clip.duration;
            var renderer = entry.renderer;
            if (library.material != null) renderer.sharedMaterial = library.material;
            renderer.name = clip.kind.ToString(); renderer.sprite = clip.frames[0]; renderer.color = clip.tint;
            entry.motor = clip.facesMovement ? owner.GetComponent<DarkLord.Actors.TopDownMotor>() : null;
            renderer.transform.position = position;
            renderer.transform.rotation = entry.motor != null ? FacingRotation(entry.motor) : Quaternion.Euler(0, 0, direction.sqrMagnitude > .001f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0);
            float size = scale * clip.width / Mathf.Max(.001f, clip.frames[0].bounds.size.x);
            renderer.transform.localScale = new Vector3(size * stretch, size * clip.heightScale, 1);
            renderer.sortingOrder = Mathf.Clamp(-Mathf.RoundToInt(owner.position.y * 16) + clip.sortingOffset, -30000, 30000);
            renderer.enabled = true;
        }

        private static Quaternion FacingRotation(DarkLord.Actors.TopDownMotor motor) =>
            Quaternion.Euler(0, 0, Mathf.Atan2(motor.Facing.y, motor.Facing.x) * Mathf.Rad2Deg);

        private void Update()
        {
            foreach (var entry in pool)
            {
                if (!entry.renderer.enabled) continue;
                entry.elapsed += Time.deltaTime;
                if (entry.elapsed >= entry.duration || (entry.follows && (entry.follow == null || !entry.follow.gameObject.activeInHierarchy)))
                { entry.renderer.enabled = false; entry.follow = null; continue; }
                if (entry.follows)
                {
                    entry.renderer.transform.position = entry.follow.position + entry.offset;
                    if (entry.motor != null) entry.renderer.transform.rotation = FacingRotation(entry.motor);
                    entry.renderer.sortingOrder = Mathf.Clamp(-Mathf.RoundToInt(entry.follow.position.y * 16) + entry.clip.sortingOffset, -30000, 30000);
                }
                float progress = entry.clip.loop ? Mathf.Repeat(entry.elapsed / entry.clip.duration, 1) : entry.elapsed / entry.duration;
                int last = entry.clip.frames.Length - 1;
                int frame = Mathf.Min(last, (int)(progress * entry.clip.frames.Length));
                float transition = Mathf.Min(entry.clip.transitionDuration, entry.duration * .5f);
                if (entry.clip.sustainFrame >= 0 && transition > 0)
                {
                    int hold = Mathf.Min(last, entry.clip.sustainFrame);
                    frame = entry.elapsed < transition ? Mathf.FloorToInt(Mathf.Lerp(0, hold + 1, entry.elapsed / transition)) :
                        entry.elapsed > entry.duration - transition ? Mathf.Min(last, hold + Mathf.FloorToInt((last - hold + 1) * (1 - (entry.duration - entry.elapsed) / transition))) : hold;
                }
                entry.renderer.sprite = entry.clip.frames[frame];
                var color = entry.clip.tint;
                if (entry.clip.fadeOut > 0) color.a *= Mathf.Clamp01((entry.duration - entry.elapsed) / entry.clip.fadeOut);
                entry.renderer.color = color;
            }
        }

        private void OnDestroy() { if (scenes.TryGetValue(sceneHandle, out var current) && current == this) scenes.Remove(sceneHandle); }
    }
}
