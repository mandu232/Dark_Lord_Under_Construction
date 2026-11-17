using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Dungeon
{
    /// <summary>
    /// Real 2D lights for the dungeon's light sources. The scene's global light is the dim ambient; these add
    /// stepped pixel-art pools (2D Lights sample's SpotLight cookie) that light floors and tint actors in them.
    /// </summary>
    public static class DungeonLighting
    {
        public static readonly Color TorchColor = new Color(1f, .58f, .42f);
        public static readonly Color CoreColor = new Color(1f, .3f, .42f);
        public static readonly Color RiftColor = new Color(.78f, .36f, 1f);
        public static readonly Color WindowColor = new Color(1f, .2f, .24f);
        public static readonly Color LordColor = new Color(1f, .82f, .78f);

        /// <summary>Adds (or returns the existing) banded pixel light on a child of <paramref name="host"/>.</summary>
        public static Light2D AddPoint(Transform host, string name, Vector3 localPosition, Color color, float intensity, float radius)
        {
            var existing = host.Find(name);
            if (existing != null && existing.TryGetComponent(out Light2D found)) return found;
            var child = new GameObject(name);
            child.transform.SetParent(host, false);
            child.transform.localPosition = localPosition;
            var light = child.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            light.lightCookieSprite = BandedCookie(radius, FloorPerspective);
            light.color = color;
            light.intensity = intensity;
            light.shadowsEnabled = false;
            // Floor pools (and their shadows) stop at the wall; the wall gets its own wash (DungeonArtTiles).
            light.targetSortingLayers = LayersExcept(WallLayer);
            // Rim companion (2D Lights sample): "Additive with Mask" only brightens _MaskTex outline pixels.
            var rim = new GameObject("Rim light").AddComponent<Light2D>();
            rim.transform.SetParent(child.transform, false);
            rim.lightType = Light2D.LightType.Point;
            rim.blendStyleIndex = 2;
            rim.color = color;
            rim.intensity = intensity * RimStrength;
            rim.pointLightInnerRadius = 0;
            rim.pointLightOuterRadius = radius * FloorPerspective; // stays inside the foreshortened pool
            rim.falloffIntensity = 0;
            rim.shadowsEnabled = false;
            rim.targetSortingLayers = light.targetSortingLayers;
            return light;
        }

        public const float RimStrength = 2f;
        public const string FloorLayer = "DungeonFloor", WallLayer = "DungeonWalls";
        // 2.5D: the 3/4 camera foreshortens a round floor pool into a flat ellipse.
        public const float FloorPerspective = .6f;

        public static int[] LayersExcept(string name) => SortingLayer.layers.Where(l => l.name != name).Select(l => l.id).ToArray();
        private const int PixelsPerUnit = 16;
        // Sample SpotLight.png rings, as (outer edge / radius, brightness); outside the last ring is dark.
        private static readonly (float edge, float value)[] Bands = { (.48f, 1f), (.59f, .69f), (.77f, .45f), (.94f, .29f), (1f, .13f) };
        private static readonly Dictionary<(int, int), Sprite> cookies = new Dictionary<(int, int), Sprite>();

        /// <summary>Point-filtered stepped ellipse (height = radius * aspect) at the game's 16 PPU, so each ring lands on whole art pixels.</summary>
        public static Sprite BandedCookie(float radius, float aspect = 1)
        {
            int r = Mathf.Max(2, Mathf.RoundToInt(radius * PixelsPerUnit));
            int ry = Mathf.Max(2, Mathf.RoundToInt(radius * aspect * PixelsPerUnit));
            if (cookies.TryGetValue((r, ry), out var cached) && cached != null) return cached;
            int width = r * 2, height = ry * 2;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float d = new Vector2((x + .5f - r) / r, (y + .5f - ry) / ry).magnitude;
                    float v = 0;
                    foreach (var band in Bands) if (d < band.edge) { v = band.value; break; }
                    byte b = (byte)Mathf.RoundToInt(v * 255);
                    pixels[y * width + x] = new Color32(b, b, b, 255);
                }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = $"Banded light {r}x{ry}" };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            return cookies[(r, ry)] = sprite;
        }
    }

    /// <summary>Sample's "Breath Light": the cookie's scale swells and shrinks on a slow sine.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Light2D))]
    public sealed class LightBreath : MonoBehaviour
    {
        [SerializeField] private float speed = 1f, amount = .1f;
        private Vector3 baseScale;

        private void OnEnable() => baseScale = transform.localScale;
        private void OnDisable() => transform.localScale = baseScale;
        private void Update() => transform.localScale = baseScale * (1 + Mathf.Sin(Time.time * speed) * amount);
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
