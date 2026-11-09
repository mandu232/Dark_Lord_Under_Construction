using UnityEngine;

namespace DarkLord.Minions
{
    // All minions share one animator; swapping the sprite after it runs gives each role its own look on the same clips.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MinionSkin : MonoBehaviour
    {
        [SerializeField] private MinionSkinAsset skin;
        private SpriteRenderer visual;

        public void Apply(MinionSkinAsset asset) => skin = asset;
        private void Awake() => visual = GetComponent<SpriteRenderer>();
        private void LateUpdate() { if (skin != null) visual.sprite = skin.Resolve(visual.sprite); }
    }
}
