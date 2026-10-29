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
        private static readonly int GroundWorldY = Shader.PropertyToID("_GroundWorldY");
        private static readonly int ExcludeSlashEffect = Shader.PropertyToID("_ExcludeSlashEffect");

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (shadow == null) shadow = GetComponent<SpriteRenderer>();
            if (source == null && transform.parent != null)
                source = transform.parent.GetComponent<SpriteRenderer>();
            if (source == null) { shadow.enabled = false; return; }
            shadow.enabled = source.enabled && source.gameObject.activeInHierarchy;
            // Follow the actual pose, including both attacks. The shader removes the
            // effect instead of freezing an earlier pose. Sub-sprite names are unreliable
            // (the second sheet contains the legacy spelling "Slahs_2").
            shadow.sprite = source.sprite;
            shadow.flipX = source.flipX;
            shadow.flipY = source.flipY;
            shadow.color = new Color(1f, 1f, 1f, source.color.a);
            shadow.sortingLayerID = source.sortingLayerID;
            shadow.sortingOrder = source.sortingOrder - 1;
            if (properties == null) properties = new MaterialPropertyBlock();
            shadow.GetPropertyBlock(properties);
            string sheet = source.sprite != null ? source.sprite.texture.name : null;
            properties.SetFloat(ExcludeSlashEffect, sheet == "Slash_1" || sheet == "Slash_2" ? 1f : 0f);
            properties.SetFloat(GroundWorldY, transform.TransformPoint(new Vector3(0, -.84375f, 0)).y);
            shadow.SetPropertyBlock(properties);
        }
    }
}
