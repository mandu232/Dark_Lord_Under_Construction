using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Dungeon
{
    /// <summary>
    /// Real 2D point lights for the dungeon's light sources. The scene's global light is the dim
    /// ambient; these add falloff pools that light floors and tint actors standing in them.
    /// </summary>
    public static class DungeonLighting
    {
        public static readonly Color TorchColor = new Color(1f, .58f, .42f);
        public static readonly Color CoreColor = new Color(1f, .3f, .42f);
        public static readonly Color RiftColor = new Color(.78f, .36f, 1f);
        public static readonly Color LordColor = new Color(1f, .82f, .78f);

        /// <summary>Adds (or returns the existing) point light on a child of <paramref name="host"/>.</summary>
        public static Light2D AddPoint(Transform host, string name, Vector3 localPosition, Color color, float intensity, float radius, float falloff = .55f)
        {
            var existing = host.Find(name);
            if (existing != null && existing.TryGetComponent(out Light2D found)) return found;
            var child = new GameObject(name);
            child.transform.SetParent(host, false);
            child.transform.localPosition = localPosition;
            var light = child.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = radius * .15f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = falloff;
            light.shadowsEnabled = false;
            return light;
        }
    }

    /// <summary>Slow, cheap flicker for flame lights. One sine pair per light, no allocations.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Light2D))]
    public sealed class LightFlicker : MonoBehaviour
    {
        [SerializeField] private float amount = .08f;
        private Light2D target;
        private float baseIntensity, seed;

        public void Configure(float flicker) { amount = flicker; }
        private void OnEnable()
        {
            target = GetComponent<Light2D>();
            baseIntensity = target.intensity;
            seed = Random.value * 50f;
        }
        private void Update()
        {
            float t = Time.time + seed;
            target.intensity = baseIntensity * (1 + amount * (Mathf.Sin(t * 7.3f) * .6f + Mathf.Sin(t * 13.1f) * .4f));
        }
    }
}
