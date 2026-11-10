using UnityEngine;

namespace DarkLord.Gameplay
{
    // One reusable visual per actor keeps a shower of pickups from spawning overlapping effects.
    [DisallowMultipleComponent]
    public sealed class GrowthFeedback : MonoBehaviour
    {
        private LineRenderer pickupRing;
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
            For(actor).levelRemaining = LevelDuration;
            GameplayEffects.Show(actor, GameplayEffectKind.LevelUp, actor.position, follow: true);
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
        }
    }
}
