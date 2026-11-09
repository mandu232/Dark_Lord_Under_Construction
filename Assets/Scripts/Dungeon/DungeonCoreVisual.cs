using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Dungeon
{
    // Plays the turning gem on the core's body, and the one-shot collapse when the castle falls.
    [DisallowMultipleComponent]
    public sealed class DungeonCoreVisual : MonoBehaviour
    {
        // Where the core-collapse sheet bursts, in seconds from its first frame.
        public const float CollapseBurst = 1.3f;
        private SpriteRenderer body;
        private GameplayEffectLibrary.Clip loop, collapse;
        private float collapseStarted = -1;
        public bool IsCollapsing => collapseStarted >= 0;

        public void Configure(SpriteRenderer target)
        {
            body = target;
            var library = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            if (library == null) return;
            loop = library.Find(GameplayEffectKind.CoreGem);
            collapse = library.Find(GameplayEffectKind.CoreCollapse);
        }

        public void PlayCollapse() { if (collapseStarted < 0) collapseStarted = Time.time; }

        private void Update()
        {
            if (body == null) return;
            if (collapseStarted >= 0 && collapse != null && collapse.frames != null && collapse.frames.Length > 0)
            {
                int frame = Mathf.FloorToInt((Time.time - collapseStarted) / collapse.duration * collapse.frames.Length);
                body.sprite = collapse.frames[Mathf.Min(frame, collapse.frames.Length - 1)];
            }
            else if (loop != null && loop.frames != null && loop.frames.Length > 0)
                body.sprite = loop.frames[Mathf.FloorToInt(Mathf.Repeat(Time.time / loop.duration, 1) * loop.frames.Length) % loop.frames.Length];
        }
    }
}
