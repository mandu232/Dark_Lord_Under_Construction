using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Player
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        private PixelPerfectCamera pixelCamera;
        private CorpseHarvest harvest;
        private PixelPerfectCamera.GridSnapping previousGridSnapping;
        private bool carryingOverride;
        private float shakeUntil;
        private float shakeStarted;
        public void Shake()
        {
            shakeStarted = Time.unscaledTime;
            shakeUntil = shakeStarted + .14f;
        }
        private void Awake()
        {
            pixelCamera = GetComponent<PixelPerfectCamera>();
            if (target != null) harvest = target.GetComponent<CorpseHarvest>();
        }
        private void LateUpdate()
        {
            if (target != null)
            {
                if (harvest == null) harvest = target.GetComponent<CorpseHarvest>();
                UpdateCarrySnapping(harvest != null && harvest.isActiveAndEnabled && harvest.Carried != null);
                float remaining = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / .14f);
                float phase = (Time.unscaledTime - shakeStarted) * 75f;
                Vector2 offset = remaining > 0 ? new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.3f)) * (.065f * remaining) : Vector2.zero;
                transform.position = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            }
            else UpdateCarrySnapping(false);
        }

        private void UpdateCarrySnapping(bool carrying)
        {
            if (pixelCamera == null) return;
            if (carrying && !carryingOverride && pixelCamera.enabled && pixelCamera.gridSnapping == PixelPerfectCamera.GridSnapping.PixelSnapping)
            {
                previousGridSnapping = pixelCamera.gridSnapping;
                pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.None;
                carryingOverride = true;
            }
            else if (!carrying && carryingOverride) RestoreCarrySnapping();
        }

        private void RestoreCarrySnapping()
        {
            if (!carryingOverride) return;
            if (pixelCamera != null && pixelCamera.gridSnapping == PixelPerfectCamera.GridSnapping.None)
                pixelCamera.gridSnapping = previousGridSnapping;
            carryingOverride = false;
        }

        private void OnDisable() => RestoreCarrySnapping();
    }
}
