using System;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Gameplay
{
    [Serializable] public sealed class LootSaveData
    {
        public Vector3 position;
        public int kind, amount;
    }

    // No physics callbacks: only the owning player can collect drops, including during preparation.
    public sealed class DeathLoot : MonoBehaviour
    {
        public enum Kind { Blood, Crystal, Gold, Experience }
        private Transform collector;
        private WaveDirector waves;
        private ActorHealth health;
        private CurrencyWallet wallet;
        private Kind kind;
        private int amount;
        private float age;
        private float scatterDuration;
        private Vector3 origin, destination;
        private TrailRenderer absorptionTrail;
        private Color color;
        private static GameplayEffectLibrary effectLibrary;
        private static Sprite glowSprite;
        private Sprite orbSprite;
        internal static event Action<ActorHealth, Kind, Vector3, Sprite> OrbCollected;
        public bool Collected { get; private set; }

        public static void Scatter(Vector3 position, Transform player, Transform root, WaveDirector waves = null)
        {
            const int experienceOrbs = 3;
            const float crystalChance = .08f;
            float goldChance = player.GetComponent<RunEconomy>()?.Has(TechId.Salvage) == true ? .25f : .15f;
            float roll = UnityEngine.Random.value;
            Kind resource;
            int count;
            if (roll < goldChance)
            {
                resource = Kind.Gold;
                count = UnityEngine.Random.Range(2, 6);
            }
            else if (roll < goldChance + crystalChance)
            {
                resource = Kind.Crystal;
                count = 1;
            }
            else
            {
                resource = Kind.Blood;
                count = UnityEngine.Random.Range(2, 5);
            }
            for (int i = 0; i < count; i++) Spawn(position, resource, 1, player, root, true, waves);
            for (int i = 0; i < experienceOrbs; i++) Spawn(position, Kind.Experience, 1, player, root, true, waves);
        }
        private static DeathLoot Spawn(Vector3 position, Kind type, int count, Transform player, Transform root, bool scatter, WaveDirector waves)
        {
            var node = new GameObject(type + " drop");
            node.transform.SetParent(root, false); node.transform.position = position;
            var drop = node.AddComponent<DeathLoot>();
            drop.waves = waves;
            drop.collector = player; drop.health = player.GetComponent<ActorHealth>();
            drop.wallet = player.GetComponent<CurrencyWallet>(); drop.kind = type; drop.amount = count;
            drop.origin = drop.destination = position;
            drop.scatterDuration = UnityEngine.Random.Range(.28f, .52f);
            if (scatter)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(.45f, 1.8f);
                Vector3 end = position + (Vector3)offset;
                foreach (var hit in Physics2D.LinecastAll(position, end))
                    if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<ActorHealth>() == null)
                    { end = (Vector3)hit.point - (Vector3)offset.normalized * .15f; break; }
                drop.destination = end;
            }
            else drop.age = drop.scatterDuration + .1f;
            Color color = type == Kind.Blood ? new Color(.85f,.08f,.15f) : type == Kind.Crystal ? new Color(1,.2f,.65f) :
                type == Kind.Gold ? new Color(1,.8f,.15f) : new Color(.25f,1,.8f);
            drop.color = color;
            var trailNode = new GameObject("Absorption trail"); trailNode.transform.SetParent(node.transform, false);
            drop.absorptionTrail = trailNode.AddComponent<TrailRenderer>();
            drop.absorptionTrail.sharedMaterial = Resources.Load<Material>("GameplayTraceOriginalColor");
            drop.absorptionTrail.time = .14f; drop.absorptionTrail.minVertexDistance = .025f;
            drop.absorptionTrail.startWidth = .1f; drop.absorptionTrail.endWidth = 0;
            drop.absorptionTrail.startColor = color; drop.absorptionTrail.endColor = new Color(color.r, color.g, color.b, 0);
            drop.absorptionTrail.sortingOrder = 46; drop.absorptionTrail.emitting = false;
            if (effectLibrary == null) effectLibrary = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            var visual = new GameObject("Orb sprite"); visual.transform.SetParent(node.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = effectLibrary != null && effectLibrary.lootSprites != null && (int)type < effectLibrary.lootSprites.Length
                ? effectLibrary.lootSprites[(int)type] : null;
            renderer.sortingOrder = 45;
            drop.orbSprite = renderer.sprite;
            if (effectLibrary != null && effectLibrary.material != null) renderer.sharedMaterial = effectLibrary.material;
            if (renderer.sprite != null)
            {
                visual.transform.localScale = Vector3.one * (.27f / Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y));
                var glow = new GameObject("Orb glow");
                glow.transform.SetParent(node.transform, false);
                glow.transform.localScale = Vector3.one * .85f;
                var halo = glow.AddComponent<SpriteRenderer>();
                halo.sprite = GetGlowSprite();
                halo.sharedMaterial = renderer.sharedMaterial;
                halo.sortingOrder = renderer.sortingOrder - 1;
                halo.color = new Color(color.r, color.g, color.b, .65f);
            }
            return drop;
        }

        private static Sprite GetGlowSprite()
        {
            if (glowSprite != null) return glowSprite;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Orb halo",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2(x + .5f, y + .5f) / (size * .5f) - Vector2.one;
                    float falloff = Mathf.Clamp01(1 - point.magnitude);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * falloff * falloff));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, size, 0, SpriteMeshType.FullRect);
            glowSprite.hideFlags = HideFlags.DontSave;
            return glowSprite;
        }
        private void Update()
        {
            absorptionTrail.emitting = false;
            if (Time.timeScale <= 0 || collector == null || health == null || !health.IsAlive) return;
            age += Time.deltaTime;
            if (age < scatterDuration)
            {
                float t = age / scatterDuration;
                transform.position = Vector3.Lerp(origin, destination, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .4f);
                return;
            }
            if (age - Time.deltaTime < scatterDuration) transform.position = destination;
            if (waves != null && waves.InCombat) return;
            float distance = Vector2.Distance(transform.position, collector.position);
            if (distance > 1.6f) return;
            foreach (var hit in Physics2D.LinecastAll(transform.position, collector.position))
                if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<ActorHealth>() == null) return;
            if (kind != Kind.Experience && (wallet == null || wallet.GetAmount(Currency()) >= wallet.GetCapacity(Currency()))) return;
            absorptionTrail.emitting = true;
            transform.localScale = Vector3.one * Mathf.Lerp(.35f, 1, Mathf.InverseLerp(.2f, 1.6f, distance));
            transform.position = Vector3.MoveTowards(transform.position, collector.position, 7 * Time.deltaTime);
            if (Vector2.Distance(transform.position, collector.position) > .4f) return;
            if (kind == Kind.Experience)
            {
                int level = health.Level;
                int experience = health.Experience;
                health.AddExperience(amount);
                if (health.Level != level || health.Experience != experience)
                    OrbCollected?.Invoke(health, kind, transform.position, orbSprite);
                GrowthFeedback.Pickup(collector, color);
            }
            else
            {
                int accepted = Mathf.Min(amount, wallet.GetCapacity(Currency()) - wallet.GetAmount(Currency()));
                wallet.Add(Currency(), accepted); amount -= accepted;
                if (accepted > 0)
                {
                    OrbCollected?.Invoke(health, kind, transform.position, orbSprite);
                    GrowthFeedback.Pickup(collector, color);
                }
                if (amount > 0) return;
            }
            Collected = true; gameObject.SetActive(false); Destroy(gameObject);
        }
        private CurrencyType Currency() => kind == Kind.Blood ? CurrencyType.Souls : kind == Kind.Crystal ? CurrencyType.DarkCrystals : CurrencyType.Gold;
        public static LootSaveData[] Capture(Transform player) => FindObjectsByType<DeathLoot>(FindObjectsSortMode.None)
            .Where(d => d.collector == player && !d.Collected).Select(d => new LootSaveData {
                position = d.age < d.scatterDuration ? d.destination : d.transform.position, kind = (int)d.kind, amount = d.amount }).ToArray();
        public static void Restore(LootSaveData[] saved, Transform player, Transform root, WaveDirector waves = null)
        {
            foreach (var drop in FindObjectsByType<DeathLoot>(FindObjectsSortMode.None))
                if (drop.collector == player) { drop.gameObject.SetActive(false); Destroy(drop.gameObject); }
            if (saved == null) return;
            foreach (var data in saved)
                if (data != null && data.kind >= 0 && data.kind <= 3 && data.amount > 0)
                    Spawn(data.position, (Kind)data.kind, data.amount, player, root, false, waves);
        }
    }
}
