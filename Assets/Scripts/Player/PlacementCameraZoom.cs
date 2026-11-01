using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Player
{
    // Shared ownership prevents nested construction/relocation zooms from multiplying.
    public sealed class PlacementCameraZoom : MonoBehaviour
    {
        private readonly HashSet<Object> owners = new HashSet<Object>();
        private Camera view;
        private PixelPerfectCamera pixels;
        private float size;
        private int resolutionX, resolutionY;
        public static PlacementCameraZoom Acquire(Camera camera, Object owner)
        {
            if (camera == null || !camera.orthographic) return null;
            camera.GetComponent<CameraFollow>()?.CancelImpact();
            var zoom = camera.GetComponent<PlacementCameraZoom>();
            if (zoom == null) zoom = camera.gameObject.AddComponent<PlacementCameraZoom>();
            if (zoom.owners.Count == 0)
            {
                zoom.view = camera; zoom.size = camera.orthographicSize;
                zoom.pixels = camera.GetComponent<PixelPerfectCamera>();
                if (zoom.pixels != null && zoom.pixels.isActiveAndEnabled)
                {
                    zoom.resolutionX = zoom.pixels.refResolutionX; zoom.resolutionY = zoom.pixels.refResolutionY;
                    zoom.pixels.refResolutionX *= 2; zoom.pixels.refResolutionY *= 2;
                }
                else zoom.pixels = null;
                camera.orthographicSize = zoom.size * 2;
            }
            zoom.owners.Add(owner);
            return zoom;
        }
        public void Release(Object owner)
        {
            if (owners.Remove(owner) && owners.Count == 0) Restore();
        }
        private void Restore()
        {
            if (pixels != null) { pixels.refResolutionX = resolutionX; pixels.refResolutionY = resolutionY; }
            if (view != null) view.orthographicSize = size;
            owners.Clear();
        }
        private void OnDisable() => Restore();
    }
}
