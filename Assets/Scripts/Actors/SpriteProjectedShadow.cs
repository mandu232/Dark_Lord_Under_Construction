using UnityEngine;

namespace DarkLord.Actors
{
    /// <summary>Copies only the current silhouette; the material projects it about the feet.</summary>
    [ExecuteAlways, DefaultExecutionOrder(100), DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteProjectedShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        private SpriteRenderer shadow;
        private MaterialPropertyBlock properties;
        private Sprite lastBodySprite;
        private static readonly int GroundWorldY = Shader.PropertyToID("_GroundWorldY");

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (shadow == null) shadow = GetComponent<SpriteRenderer>();
            if (source == null && transform.parent != null)
                source = transform.parent.GetComponent<SpriteRenderer>();
            if (source == null) { shadow.enabled = false; return; }
            shadow.enabled = source.enabled && source.gameObject.activeInHierarchy;
            // The attack sheet includes the red slash effect; it should not cast a body shadow.
            if (source.sprite != null && !source.sprite.name.StartsWith("Slash_1_", System.StringComparison.Ordinal))
                lastBodySprite = source.sprite;
            shadow.sprite = source.sprite == null ? null : (lastBodySprite != null ? lastBodySprite : source.sprite);
            shadow.flipX = source.flipX;
            shadow.flipY = source.flipY;
            shadow.color = new Color(1f, 1f, 1f, source.color.a);
            shadow.sortingLayerID = source.sortingLayerID;
            shadow.sortingOrder = source.sortingOrder - 1;
            if (properties == null) properties = new MaterialPropertyBlock();
            shadow.GetPropertyBlock(properties);
            properties.SetFloat(GroundWorldY, transform.TransformPoint(new Vector3(0, -.84375f, 0)).y);
            shadow.SetPropertyBlock(properties);
        }
    }
}
