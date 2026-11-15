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
        private const float VisualScale = .3f; // A fifth of the original 1.5.
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
        private Sprite orbSprite;
        private SpriteRenderer visualRenderer;
        private GameplayEffectLibrary.Clip visualClip;
        private float animationOffset;
        internal static event Action<ActorHealth, Kind, Vector3, Sprite> OrbCollected;
        public bool Collected { get; private set; }
        public Kind Type => kind;
        public int Amount => amount;
        // Orbs at rest. A landing orb joins a resting one of its kind nearby, so a long fight leaves
        // a few larger orbs rather than hundreds that each animate and test for pickup every frame.
        internal const float MergeRadius = .75f;
        private static readonly System.Collections.Generic.List<DeathLoot> Resting = new System.Collections.Generic.List<DeathLoot>();
        private static readonly RaycastHit2D[] WallHits = new RaycastHit2D[16];
        private bool settled, merged;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetResting() => Resting.Clear();
        private void OnDisable() => Resting.Remove(this);

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
            // The art bakes in its glow, so no separate halo is drawn.
            var clip = effectLibrary != null ? effectLibrary.Find(GameplayEffectKind.LootBlood + (int)type) : null;
            if (clip != null && clip.frames != null && clip.frames.Length > 0)
            {
                renderer.sprite = clip.frames[0];
                visual.transform.localScale = Vector3.one * VisualScale;
                drop.visualClip = clip; drop.visualRenderer = renderer;
                drop.animationOffset = UnityEngine.Random.value * clip.duration;
            }
            renderer.sortingOrder = 45;
            drop.orbSprite = renderer.sprite;
            drop.RefreshSize();
            if (effectLibrary != null && effectLibrary.material != null) renderer.sharedMaterial = effectLibrary.material;
            return drop;
        }

        private void Update()
        {
            if (visualClip != null)
            {
                float loop = Mathf.Repeat((Time.time + animationOffset) / visualClip.duration, 1);
                visualRenderer.sprite = visualClip.frames[Mathf.Min(visualClip.frames.Length - 1, (int)(loop * visualClip.frames.Length))];
            }
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
            if (!settled && Settle()) return;
            if (waves != null && waves.InCombat) return;
            float distance = Vector2.Distance(transform.position, collector.position);
            if (distance > 1.6f) return;
            if (Walled(transform.position, collector.position)) return;
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
                GrowthFeedback.Pickup(collector, color); PlayCollectBurst();
            }
            else
            {
                int accepted = Mathf.Min(amount, wallet.GetCapacity(Currency()) - wallet.GetAmount(Currency()));
                wallet.Add(Currency(), accepted); amount -= accepted;
                if (accepted > 0)
                {
                    OrbCollected?.Invoke(health, kind, transform.position, orbSprite);
                    GrowthFeedback.Pickup(collector, color); PlayCollectBurst();
                }
                if (amount > 0) return;
            }
            Collected = true; gameObject.SetActive(false); Destroy(gameObject);
        }
        // True when this orb merged into a neighbour and is gone.
        private bool Settle()
        {
            settled = true;
            DeathLoot nearest = null;
            float best = MergeRadius * MergeRadius;
            foreach (var other in Resting)
            {
                if (other == null || other.merged || other.Collected || other.kind != kind || other.collector != collector) continue;
                float distance = ((Vector2)(other.transform.position - transform.position)).sqrMagnitude;
                if (distance > best || Walled(transform.position, other.transform.position)) continue;
                best = distance; nearest = other;
            }
            if (nearest == null) { Resting.Add(this); return false; }
            nearest.amount += amount; nearest.RefreshSize();
            amount = 0; merged = true;
            gameObject.SetActive(false); Destroy(gameObject);
            return true;
        }

        // A merged orb grows a little with its worth; the growth flattens so a big pile stays orb sized.
        private void RefreshSize()
        {
            if (visualRenderer == null) return;
            visualRenderer.transform.localScale = Vector3.one *
                (VisualScale * Mathf.Min(1.75f, 1 + .25f * Mathf.Log(Mathf.Max(1, amount), 2)));
        }

        // Terrain between two points; actors and triggers never block.
        private static bool Walled(Vector2 from, Vector2 to)
        {
            int count = Physics2D.Linecast(from, to, new ContactFilter2D { useTriggers = false }, WallHits);
            for (int i = 0; i < count; i++)
                if (!WallHits[i].collider.isTrigger && WallHits[i].collider.GetComponentInParent<ActorHealth>() == null) return true;
            return false;
        }

        // The pop where the orb is absorbed, themed for blood, bloodstone, gold or soul (same order as Kind).
        private void PlayCollectBurst() => GameplayEffects.Show(collector, GameplayEffectKind.CollectBlood + (int)kind, transform.position);
        private CurrencyType Currency() => kind == Kind.Blood ? CurrencyType.Souls : kind == Kind.Crystal ? CurrencyType.DarkCrystals : CurrencyType.Gold;
        public static LootSaveData[] Capture(Transform player) => FindObjectsByType<DeathLoot>(FindObjectsSortMode.None)
            .Where(d => d.collector == player && !d.Collected && !d.merged).Select(d => new LootSaveData {
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
