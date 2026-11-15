using UnityEngine;

namespace DarkLord.Minions
{
    // All minions share one animator; swapping the sprite after it runs gives each role its own look on the same clips.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MinionSkin : MonoBehaviour
    {
        [SerializeField] private MinionSkinAsset skin;
        private SpriteRenderer visual;
        private Sprite resolved;
        private MinionSkinAsset resolvedFrom;

        public void Apply(MinionSkinAsset asset)
        {
            skin = asset;
            resolved = null;
            ResolveSprite();
        }

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            ResolveSprite();
        }

        private void LateUpdate() => ResolveSprite();

        private void ResolveSprite()
        {
            // Only a frame the Animator just wrote needs mapping; a paused (offscreen) one costs nothing.
            if (skin == null || visual == null || (visual.sprite == resolved && resolvedFrom == skin)) return;
            visual.sprite = resolved = skin.Resolve(visual.sprite);
            resolvedFrom = skin;
        }
    }
}
