using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.Actors
{
    [DisallowMultipleComponent]
    public sealed class TorchShadowLight : MonoBehaviour
    {
        private static readonly HashSet<TorchShadowLight> lights = new HashSet<TorchShadowLight>();
        private void OnEnable() => lights.Add(this);
        private void OnDisable() => lights.Remove(this);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLights() => lights.Clear();

        public static float Opacity(Vector3 feet, UnityEngine.SceneManagement.Scene scene)
        {
            float nearest = float.PositiveInfinity;
            foreach (var light in lights)
                if (light != null && light.gameObject.scene == scene)
                    nearest = Mathf.Min(nearest, Vector2.Distance(feet, light.transform.position));
            return float.IsPositiveInfinity(nearest) ? 1 : Mathf.Lerp(1.35f, .3f, Mathf.SmoothStep(0, 1, nearest / 7));
        }

        public static Vector2 Projection(Vector3 feet, UnityEngine.SceneManagement.Scene scene, Vector2 fallback)
            => Projection(feet, scene, fallback, out _);

        public static Vector2 Projection(Vector3 feet, UnityEngine.SceneManagement.Scene scene, Vector2 fallback, out float length)
        {
            Vector2 sum = Vector2.zero;
            float total = 0;
            float nearest = float.PositiveInfinity;
            foreach (var light in lights)
            {
                if (light == null || light.gameObject.scene != scene) continue;
                // The fixture base gives its floor position; the flame is elevated on the wall.
                Vector2 delta = feet - light.transform.position;
                float distance = delta.magnitude;
                nearest = Mathf.Min(nearest, distance);
                float weight = 1 / Mathf.Max(.25f, distance * distance * distance * distance);
                sum += delta / Mathf.Max(1, distance) * weight;
                total += weight;
            }
            length = total > 0 ? Mathf.Lerp(.25f, 1.35f, Mathf.Clamp01(nearest / 7)) : 1;
            if (total <= 0) return fallback;
            Vector2 average = sum / total;
            Vector2 direction = average.sqrMagnitude > .000001f ? average.normalized : fallback.normalized;
            return direction * length;
        }
    }
}
