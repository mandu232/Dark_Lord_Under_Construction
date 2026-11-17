using UnityEngine;

namespace DarkLord.Actors
{
    /// <summary>Copies only the current silhouette; the material projects it straight down from the feet (fixed, not light-driven).</summary>
    [ExecuteAlways, DefaultExecutionOrder(100), DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteProjectedShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private float groundLocalY = -.84375f;
        // Minions/enemies share a placeholder sprite in their prefab and swap sheets at runtime, so without a baked
        // entry they fall back to the frame's tight mesh instead of a fixed height.
        [SerializeField] private bool feetFromSprite;
        private static readonly System.Collections.Generic.Dictionary<Sprite, float> feetCache = new System.Collections.Generic.Dictionary<Sprite, float>();
        private static float FeetOf(Sprite sprite)
        {
            if (feetCache.TryGetValue(sprite, out float feet)) return feet;
            feet = float.MaxValue;
            foreach (var v in sprite.vertices) feet = Mathf.Min(feet, v.y);
            return feetCache[sprite] = feet;
        }
        private SpriteRenderer shadow;
        private MaterialPropertyBlock properties;
        private static readonly int GroundWorldY = Shader.PropertyToID("_GroundWorldY");
        private static readonly int ExcludeSlashEffect = Shader.PropertyToID("_ExcludeSlashEffect");

        private Sprite sheetSprite;
        private bool slashSheet;

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
            // Texture.name allocates a string, so only look again when the frame changes.
            if (source.sprite != sheetSprite)
            {
                sheetSprite = source.sprite;
                string sheet = sheetSprite != null ? sheetSprite.texture.name : null;
                slashSheet = sheet == "Slash_1" || sheet == "Slash_2";
            }
            properties.SetFloat(ExcludeSlashEffect, slashSheet ? 1f : 0f);
            // Baked exact feet win for every frame (player included), so the shadow touches the feet.
            float ground = groundLocalY;
            if (source.sprite != null && !SpriteFeetTable.TryGet(source.sprite, out ground))
                ground = feetFromSprite ? FeetOf(source.sprite) : groundLocalY;
            properties.SetFloat(GroundWorldY, transform.TransformPoint(new Vector3(0, ground, 0)).y);
            shadow.SetPropertyBlock(properties);
        }
    }
}
