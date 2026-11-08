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
        private float shakeDuration = .14f, shakeStrength = .065f;
        private Camera view;
        private float impactStarted, originalSize;
        private bool impactActive, restorePixels;
        public void Shake(float strength = .065f)
        {
            if (impactActive) return;
            shakeDuration = .14f; shakeStrength = strength;
            shakeStarted = Time.unscaledTime;
            shakeUntil = shakeStarted + .14f;
        }
        public void NovaImpact()
        {
            if (!isActiveAndEnabled) return;
            if (view == null) view = GetComponent<Camera>();
            if (view == null || !view.orthographic) return;
            if (!impactActive)
            {
                originalSize = view.orthographicSize;
                restorePixels = pixelCamera != null && pixelCamera.enabled;
                if (restorePixels) pixelCamera.enabled = false;
            }
            impactActive = true;
            view.orthographicSize = originalSize * .92f;
            impactStarted = shakeStarted = Time.unscaledTime;
            shakeDuration = .38f; shakeStrength = .18f;
            shakeUntil = shakeStarted + shakeDuration;
        }
        public void CancelImpact()
        {
            if (!impactActive) return;
            impactActive = false;
            if (view != null) view.orthographicSize = originalSize;
            if (restorePixels && pixelCamera != null) pixelCamera.enabled = true;
            restorePixels = false;
            shakeUntil = 0;
        }
        private void Awake()
        {
            pixelCamera = GetComponent<PixelPerfectCamera>();
            if (target != null) harvest = target.GetComponent<CorpseHarvest>();
        }
        private void LateUpdate()
        {
            if (impactActive)
            {
                float elapsed = Time.unscaledTime - impactStarted;
                if (elapsed >= .65f || Time.timeScale == 0) CancelImpact();
                else
                {
                    // Snap into the impact, then ease out with a soft landing.
                    float remaining = 1 - Mathf.Clamp01((elapsed - .06f) / .59f);
                    float pulse = remaining * remaining;
                    view.orthographicSize = originalSize * (1 - .08f * pulse);
                }
            }
            if (target != null)
            {
                if (harvest == null) harvest = target.GetComponent<CorpseHarvest>();
                UpdateCarrySnapping(harvest != null && harvest.isActiveAndEnabled && harvest.Carried != null);
                float remaining = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / shakeDuration);
                float phase = (Time.unscaledTime - shakeStarted) * 75f;
                Vector2 offset = remaining > 0 ? new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.3f)) * (shakeStrength * remaining) : Vector2.zero;
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

        private void OnDisable() { CancelImpact(); RestoreCarrySnapping(); }
    }
}
