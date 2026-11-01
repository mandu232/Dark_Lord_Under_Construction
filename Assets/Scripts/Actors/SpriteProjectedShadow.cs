using UnityEngine;

namespace DarkLord.Actors
{
    /// <summary>Copies only the current silhouette; the material projects it about the feet.</summary>
    [ExecuteAlways, DefaultExecutionOrder(100), DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteProjectedShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private float groundLocalY = -.84375f;
        private SpriteRenderer shadow;
        private MaterialPropertyBlock properties;
        private Vector2 lightProjection;
        private bool projectionInitialized;
        private float lightOpacity = 1;
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
            properties.SetFloat(GroundWorldY, transform.TransformPoint(new Vector3(0, groundLocalY, 0)).y);
            if (Application.isPlaying && source.GetComponent<DarkLord.Player.PlayerMovement>() != null && shadow.sharedMaterial != null)
            {
                var fallback = new Vector2(shadow.sharedMaterial.GetFloat("_Shear"), shadow.sharedMaterial.GetFloat("_Flatten"));
                Vector3 feet = transform.TransformPoint(new Vector3(0, groundLocalY, 0));
                Vector2 desired = TorchShadowLight.Projection(feet, gameObject.scene, fallback);
                float opacity = TorchShadowLight.Opacity(feet, gameObject.scene);
                lightOpacity = projectionInitialized ? Mathf.Lerp(lightOpacity, opacity, 1 - Mathf.Exp(-8 * Time.deltaTime)) : opacity;
                shadow.color = new Color(1, 1, 1, source.color.a * lightOpacity);
                lightProjection = projectionInitialized ? Vector2.Lerp(lightProjection, desired, 1 - Mathf.Exp(-8 * Time.deltaTime)) : desired;
                projectionInitialized = true;
                properties.SetFloat("_Shear", lightProjection.x);
                properties.SetFloat("_Flatten", lightProjection.y);
            }
            shadow.SetPropertyBlock(properties);
        }
    }
}
