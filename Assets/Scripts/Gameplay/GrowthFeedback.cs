using UnityEngine;

namespace DarkLord.Gameplay
{
    // One reusable visual per actor keeps a shower of pickups from spawning overlapping effects.
    [DisallowMultipleComponent]
    public sealed class GrowthFeedback : MonoBehaviour
    {
        private LineRenderer pickupRing, levelRing, levelHalo;
        private readonly LineRenderer[] sparks = new LineRenderer[8];
        private float pickupRemaining, levelRemaining;
        private Color pickupColor;
        private const float PickupDuration = .24f, LevelDuration = 1.15f;
        public bool IsLevelUpPlaying => levelRemaining > 0;
        public bool IsPickupPlaying => pickupRemaining > 0;
        private static GrowthFeedback For(Transform actor)
        {
            var effect = actor.GetComponentInChildren<GrowthFeedback>(true);
            if (effect != null) return effect;
            var node = new GameObject("Pickup and level feedback"); node.transform.SetParent(actor, false);
            return node.AddComponent<GrowthFeedback>();
        }
        public static void Pickup(Transform actor, Color color)
        {
            if (!Application.isPlaying || actor == null) return;
            var effect = For(actor);
            if (effect.pickupRing == null) effect.pickupRing = effect.MakeLine("Absorption flash", 24, true);
            effect.pickupColor = color; effect.pickupRemaining = PickupDuration;
            effect.Present();
        }
        public static void LevelUp(Transform actor)
        {
            if (!Application.isPlaying || actor == null) return;
            var effect = For(actor);
            if (effect.levelRing == null)
            {
                effect.levelRing = effect.MakeLine("Level up ground burst", 32, true);
                effect.levelHalo = effect.MakeLine("Rising level halo", 32, true);
                for (int i = 0; i < effect.sparks.Length; i++) effect.sparks[i] = effect.MakeLine("Rising gold spark", 2, false);
            }
            effect.levelRemaining = LevelDuration;
            effect.Present();
        }
        private LineRenderer MakeLine(string name, int count, bool loop)
        {
            var node = new GameObject(name); node.transform.SetParent(transform, false);
            var line = node.AddComponent<LineRenderer>();
            line.sharedMaterial = Resources.Load<Material>("GameplayTraceOriginalColor");
            line.useWorldSpace = false; line.loop = loop; line.positionCount = count;
            line.startWidth = line.endWidth = .07f; line.numCapVertices = 2;
            return line;
        }
        private void Update()
        {
            pickupRemaining = Mathf.Max(0, pickupRemaining - Time.deltaTime);
            levelRemaining = Mathf.Max(0, levelRemaining - Time.deltaTime);
            Present();
        }
        private void Present()
        {
            int order = Mathf.Clamp(-Mathf.RoundToInt(transform.position.y * 16) + 40, -30000, 30000);
            if (pickupRing != null)
            {
                float t = 1 - pickupRemaining / PickupDuration;
                Ring(pickupRing, .12f + .38f * t, .6f, .25f, pickupColor, 1 - t, order);
                pickupRing.enabled = pickupRemaining > 0;
            }
            if (levelRing == null) return;
            float progress = 1 - levelRemaining / LevelDuration;
            Color gold = new Color(1, .78f, .2f);
            float alpha = Mathf.Clamp01((1 - progress) * 2.5f);
            Ring(levelRing, .25f + progress * 1.15f, .45f, .05f, gold, alpha, order);
            Ring(levelHalo, .7f + Mathf.Sin(progress * Mathf.PI) * .2f, .45f, .15f + progress * 1.5f,
                new Color(1, .95f, .65f), alpha, order + 1);
            levelRing.enabled = levelHalo.enabled = levelRemaining > 0;
            for (int i = 0; i < sparks.Length; i++)
            {
                var line = sparks[i]; float angle = i * Mathf.PI / 4 + progress * .8f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * (.35f + progress * .55f),
                    .2f + progress * 1.8f + Mathf.Sin(angle) * .18f, 0);
                line.SetPosition(0, position); line.SetPosition(1, position + Vector3.up * (.12f + progress * .15f));
                Tint(line, gold, alpha, order + 2); line.enabled = levelRemaining > 0;
            }
        }
        private static void Ring(LineRenderer line, float radius, float flatten, float height, Color color, float alpha, int order)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, height + Mathf.Sin(angle) * radius * flatten, 0));
            }
            Tint(line, color, alpha, order);
        }
        private static void Tint(LineRenderer line, Color color, float alpha, int order)
        {
            color.a = alpha; line.startColor = line.endColor = color; line.sortingOrder = order;
        }
        private void OnDisable()
        {
            pickupRemaining = levelRemaining = 0;
            if (pickupRing != null) pickupRing.enabled = false;
            if (levelRing == null) return;
            levelRing.enabled = levelHalo.enabled = false;
            foreach (var spark in sparks) if (spark != null) spark.enabled = false;
        }
    }
}
