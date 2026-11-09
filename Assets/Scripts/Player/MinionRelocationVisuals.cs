using DarkLord.Minions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    [DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class MinionRelocationVisuals : MonoBehaviour
    {
        [SerializeField, Min(0)] private float liftHeight = 0.65f;
        [SerializeField, Range(1, 1.5f)] private float heldScale = 1.12f;
        private MinionSummoning summoning;
        private MinionController held;
        private SpriteRenderer source, carried, shadow;
        private Color originalColor;
        private Texture2D shadowTexture;
        private Sprite shadowSprite;
        public SpriteRenderer CarriedSprite => carried;
        public SpriteRenderer GroundShadow => shadow;
        private void Awake() => summoning = GetComponent<MinionSummoning>();
        private void LateUpdate()
        {
            var guard = summoning != null ? summoning.RelocatingGuard : null;
            if (guard == null || !guard.isActiveAndEnabled || !guard.IsAlive)
            {
                if (!ReferenceEquals(held, null)) Clear();
                return;
            }
            if (held != guard) { Clear(); Create(guard); }
            if (guard == null || carried == null) return;
            var camera = Camera.main;
            var mouse = Mouse.current;
            bool visible = camera != null && mouse != null && camera.pixelRect.Contains(mouse.position.ReadValue()) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            carried.enabled = shadow.enabled = visible;
            if (!visible) return;
            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            var dungeon = summoning.Dungeon;
            if (!new Plane(dungeon.transform.forward, dungeon.transform.position).Raycast(ray, out float distance)) return;
            var ground = ray.GetPoint(distance);
            bool allowed = summoning.CanPlace(dungeon.WorldToCell(ground), out _);
            var tint = allowed ? Color.white : new Color(1, 0.4f, 0.4f, 0.9f);
            carried.sprite = source.sprite; carried.flipX = source.flipX; carried.flipY = source.flipY;
            carried.color = tint;
            // Source-pixel snapping keeps the hovering preview crisp.
            var offset = source.transform.position - guard.transform.position;
            var position = ground + offset + Vector3.up * (liftHeight + Mathf.Sin(Time.unscaledTime * 5) * 0.045f);
            position.x = Mathf.Round(position.x * 16) / 16; position.y = Mathf.Round(position.y * 16) / 16;
            carried.transform.position = position;
            shadow.transform.position = ground + Vector3.down * 0.12f;
            shadow.color = allowed ? new Color(0, 0, 0, 0.4f) : new Color(0.65f, 0, 0, 0.45f);
        }
        private void Create(MinionController guard)
        {
            source = guard.GetComponentInChildren<SpriteRenderer>();
            if (source == null) return;
            held = guard; originalColor = source.color;
            source.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * 0.25f);
            carried = new GameObject("Held Minion Preview", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            carried.transform.SetParent(transform, true);
            carried.sprite = source.sprite; carried.sortingLayerID = source.sortingLayerID; carried.sortingOrder = 32750;
            carried.transform.localScale = source.transform.lossyScale * heldScale;
            shadowTexture = new Texture2D(32, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Held minion shadow" };
            var colors = new Color[512];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 7.5f) / 7.5f;
                colors[y * 32 + x] = dx * dx + dy * dy <= 1 ? Color.white : Color.clear;
            }
            shadowTexture.SetPixels(colors); shadowTexture.Apply();
            shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 32, 16), Vector2.one * 0.5f, 32);
            shadow = new GameObject("Held Minion Shadow", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, true); shadow.sprite = shadowSprite;
            shadow.sortingLayerID = source.sortingLayerID; shadow.sortingOrder = 32749;
        }
        internal void Clear()
        {
            if (source != null) source.color = originalColor;
            if (carried != null) { carried.gameObject.SetActive(false); Destroy(carried.gameObject); }
            if (shadow != null) { shadow.gameObject.SetActive(false); Destroy(shadow.gameObject); }
            if (shadowSprite != null) Destroy(shadowSprite);
            if (shadowTexture != null) Destroy(shadowTexture);
            held = null; source = carried = shadow = null;
            shadowSprite = null; shadowTexture = null;
        }
        private void OnDisable() => Clear();
    }
}
