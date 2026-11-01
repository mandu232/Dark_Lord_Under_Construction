using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Actors
{
    [ExecuteAlways, DefaultExecutionOrder(150), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class ActorContactShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        private SpriteRenderer shadow;
        private MinionController minion;
        private SpriteRenderer observedSource;
        private Vector3 restingPosition, restingScale;
        private Quaternion restingRotation;
        private Vector2 lightProjection;
        private float shadowLength = 1;
        private float lightOpacity = 1;
        private bool initialized;

        private void OnEnable()
        {
            if (!initialized)
            {
                restingPosition = transform.localPosition;
                restingScale = transform.localScale;
                restingRotation = transform.localRotation;
                initialized = true;
            }
            Refresh();
        }
        private void LateUpdate() => Refresh();
        private void OnDisable() { if (shadow != null) shadow.enabled = false; }

        public void Refresh()
        {
            if (shadow == null) shadow = GetComponent<SpriteRenderer>();
            if (source == null && transform.parent != null) source = transform.parent.GetComponent<SpriteRenderer>();
            if (source != observedSource)
            {
                observedSource = source;
                minion = source != null ? source.GetComponent<MinionController>() : null;
            }
            bool visible = source != null && source.enabled && source.sprite != null && source.gameObject.activeInHierarchy &&
                (minion == null || !minion.IsHeld) && source.color.a > 0;
            shadow.enabled = visible;
            if (!visible) return;
            shadow.sortingLayerID = source.sortingLayerID;
            shadow.sortingOrder = source.sortingOrder - 1;
            var color = new Color(1,1,1,source.color.a);
            if (shadow.color != color) shadow.color = color;
            if (Application.isPlaying && minion != null)
            {
                Vector3 feet = transform.parent.TransformPoint(restingPosition);
                Vector2 desired = TorchShadowLight.Projection(feet, gameObject.scene, Vector2.zero, out float desiredLength);
                desiredLength = desired == Vector2.zero ? 1 : Mathf.Lerp(.65f, 1.9f, Mathf.InverseLerp(.25f, 1.35f, desiredLength));
                float blend = 1 - Mathf.Exp(-8 * Time.deltaTime);
                lightOpacity = Mathf.Lerp(lightOpacity, TorchShadowLight.Opacity(feet, gameObject.scene), blend);
                shadow.color = new Color(1, 1, 1, source.color.a * lightOpacity);
                lightProjection = Vector2.Lerp(lightProjection, desired, blend);
                shadowLength = Mathf.Lerp(shadowLength, desiredLength, blend);
                float strength = Mathf.Clamp01(lightProjection.magnitude);
                transform.position = feet;
                Vector3 localDirection = transform.parent.InverseTransformVector(lightProjection);
                float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;
                transform.localRotation = strength > .001f ? Quaternion.Euler(0, 0, angle) : restingRotation;
                bool thickShadow = minion.Health.Faction == Faction.Invader || minion.Role == MinionRole.BloodSlime;
                float thickness = thickShadow ? 2.4f : 1;
                transform.localScale = new Vector3(restingScale.x * shadowLength, restingScale.y * thickness, restingScale.z);
                if (shadow.sprite != null && strength > .001f)
                {
                    // Preserve the short contact patch. Add all extra length on the unlit side,
                    // shifting by exactly half the added width so its near edge never retreats.
                    float addedHalfWidth = shadow.sprite.bounds.extents.x * Mathf.Max(0, shadowLength - .65f);
                    Vector3 shift = transform.localRotation * new Vector3(restingScale.x * addedHalfWidth, 0, 0);
                    transform.position = feet + transform.parent.TransformVector(shift);
                }
            }
        }
    }
}
