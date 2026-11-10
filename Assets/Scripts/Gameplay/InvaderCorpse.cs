using DarkLord.Dungeon;
using DarkLord.Minions;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace DarkLord.Gameplay
{
    public sealed class InvaderCorpse : MonoBehaviour
    {
        // ActorDepthSorting reserves -30000..30000; dungeon floor artwork is below -31000.
        private const int CorpseSortingOrder = -30001;
        public InvaderRole Role { get; private set; }
        public bool Consumed { get; private set; }
        // The wave/day number on which this corpse fell; it rots two days later.
        public int Day { get; internal set; }
        internal DarkLord.Minions.CorpseWorker AssignedWorker { get; set; }
        public SpriteRenderer Visual { get; private set; }
        private Color baseColor;
        private Sprite contactSprite;
        private Texture2D contactTexture;
        private RowenAnimator fallingRowen;
        public void Configure(SpriteRenderer source, InvaderRole role)
        {
            Role = role;
            Visual = gameObject.AddComponent<SpriteRenderer>();
            Visual.sharedMaterial = source.sharedMaterial;
            var rowen = source.GetComponentInParent<RowenAnimator>();
            if (rowen != null && rowen.DeathSprite != null)
            {
                fallingRowen = rowen;
                Visual.sprite = rowen.DeathFrame(0);
                StartCoroutine(PlayFall());
            }
            else Visual.sprite = DeathPose(source) ?? source.sprite;
            var actorGroup = source.GetComponentInParent<SortingGroup>();
            Visual.sortingLayerID = actorGroup != null ? actorGroup.sortingLayerID : source.sortingLayerID;
            Visual.sortingOrder = CorpseSortingOrder;
            var tactics = source.GetComponentInParent<InvaderTactics>();
            Visual.flipX = source.flipX; baseColor = new Color(.65f, .42f, .43f, 1) * (tactics != null ? tactics.RaceTint : Color.white); Visual.color = baseColor;
            transform.localScale = source.transform.lossyScale;
            transform.rotation = source.transform.rotation;
            contactTexture = new Texture2D(32, 12, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[32 * 12];
            for (int y = 0; y < 12; y++) for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 16, dy = (y - 5.5f) / 6;
                pixels[y * 32 + x] = new Color32(0, 0, 0, (byte)(dx * dx + dy * dy <= 1 ? 100 : 0));
            }
            contactTexture.SetPixels32(pixels); contactTexture.Apply();
            contactSprite = Sprite.Create(contactTexture, new Rect(0, 0, 32, 12), Vector2.one * .5f, 32);
            var shadow = new GameObject("Corpse ground contact", typeof(SpriteRenderer)); shadow.transform.SetParent(transform, false);
            var contact = shadow.GetComponent<SpriteRenderer>(); contact.sprite = contactSprite;
            contact.sharedMaterial = source.sharedMaterial; contact.sortingLayerID = Visual.sortingLayerID;
            contact.sortingOrder = CorpseSortingOrder - 1;
        }
        private static Sprite DeathPose(SpriteRenderer source)
        {
            var animator = source.GetComponentInParent<Animator>();
            if (animator == null) animator = source.transform.root.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return null;
            var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => {
                string name = c.name.ToLowerInvariant(); return name.EndsWith("die") || name.Contains("death") || name.EndsWith("dead"); });
            if (clip == null) return null;
            var root = new GameObject("Sample final death pose");
            var names = new List<string>();
            for (var node = source.transform; node != null && node != animator.transform; node = node.parent) names.Add(node.name);
            var target = root.transform;
            for (int i = names.Count - 1; i >= 0; i--) { var child = new GameObject(names[i]); child.transform.SetParent(target, false); target = child.transform; }
            var renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            var evaluator = root.AddComponent<Animator>(); evaluator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var graph = PlayableGraph.Create("Final corpse pose");
            var playable = AnimationClipPlayable.Create(graph, clip);
            var output = AnimationPlayableOutput.Create(graph, "Death pose", evaluator);
            output.SetSourcePlayable(playable); playable.SetTime(Mathf.Max(0, clip.length - .0001f)); graph.Evaluate(0);
            var sprite = renderer.sprite;
            graph.Destroy();
            Destroy(root); return sprite;
        }
        private void OnDestroy()
        {
            if (contactSprite != null) Destroy(contactSprite);
            if (contactTexture != null) Destroy(contactTexture);
        }
        private IEnumerator PlayFall()
        {
            float startedAt = Time.time;
            while (fallingRowen != null)
            {
                Visual.sprite = fallingRowen.DeathFrame(Time.time - startedAt);
                if (Visual.sprite == fallingRowen.DeathSprite)
                {
                    fallingRowen = null;
                    yield break;
                }
                yield return null;
            }
        }
        private void FinishFall()
        {
            if (fallingRowen == null) return;
            Visual.sprite = fallingRowen.DeathSprite;
            fallingRowen = null;
        }
        public void SetHeld(bool held)
        {
            if (held) FinishFall();
            if (Visual != null) { Visual.sortingOrder = CorpseSortingOrder; Visual.color = baseColor; }
        }
        public void Absorb(ResourceBuilding facility)
        {
            if (Consumed) return;
            FinishFall();
            Consumed = true;
            gameObject.AddComponent<CorpseAbsorptionEffect>().Begin(Visual, facility.transform.position);
        }
    }
}
