using UnityEngine;

namespace DarkLord.Minions
{
    // All minions share one animator; swapping the sprite after it runs gives each role its own look on the same clips.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MinionSkin : MonoBehaviour
    {
        [SerializeField] private MinionSkinAsset skin;
        private SpriteRenderer visual;

        public void Apply(MinionSkinAsset asset)
        {
            skin = asset;
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
            if (skin != null && visual != null) visual.sprite = skin.Resolve(visual.sprite);
        }
    }
}
