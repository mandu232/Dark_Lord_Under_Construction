using DarkLord.Actors;
using UnityEngine;
namespace DarkLord.Gameplay
{
    public sealed class MinionRecoveryIndicator : MonoBehaviour
    {
        private ActorHealth health;
        private LineRenderer ring, horizontal, vertical;
        private float remaining;
        public bool IsVisible => remaining > 0 && ring != null && ring.enabled;
        public static void Show(ActorHealth actor)
        {
            var indicator = actor.GetComponentInChildren<MinionRecoveryIndicator>(true);
            if (indicator == null)
            {
                var node = new GameObject("Blood recovery indicator"); node.transform.SetParent(actor.transform, false);
                indicator = node.AddComponent<MinionRecoveryIndicator>(); indicator.health = actor;
                indicator.ring = indicator.Line("Healing ground ring", 24, true);
                indicator.horizontal = indicator.Line("Healing cross horizontal", 2, false);
                indicator.vertical = indicator.Line("Healing cross vertical", 2, false);
            }
            indicator.remaining = 1.6f; indicator.enabled = true; indicator.Present();
        }
        private LineRenderer Line(string name, int count, bool loop)
        {
            var node = new GameObject(name); node.transform.SetParent(transform, false);
            var line = node.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = count; line.loop = loop;
            line.sharedMaterial = Resources.Load<Material>("GameplayTraceOriginalColor");
            line.startWidth = line.endWidth = .07f; return line;
        }
        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0 || health == null || !health.IsAlive) { Stop(); return; }
            Present();
        }
        private void Present()
        {
            int order = Mathf.Clamp(-Mathf.RoundToInt(transform.position.y * 16) + 45, -30000, 30000);
            float pulse = .8f + Mathf.Sin(Time.time * 7) * .2f;
            Color color = new Color(.35f, 1, .55f, Mathf.Min(1, remaining * 3) * pulse);
            Style(ring, color, order); Style(horizontal, color, order); Style(vertical, color, order);
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .4f, Mathf.Sin(angle) * .19f, 0));
            }
            float height = 1.15f + (1.6f - remaining) * .15f;
            horizontal.SetPosition(0, new Vector3(-.15f,height,0)); horizontal.SetPosition(1, new Vector3(.15f,height,0));
            vertical.SetPosition(0, new Vector3(0,height-.15f,0)); vertical.SetPosition(1, new Vector3(0,height+.15f,0));
        }
        private static void Style(LineRenderer line, Color color, int order)
        { line.enabled = true; line.startColor = line.endColor = color; line.sortingOrder = order; }
        public void Stop() { remaining = 0; enabled = false; }
        private void OnDisable()
        {
            remaining = 0;
            if (ring != null) ring.enabled = false;
            if (horizontal != null) horizontal.enabled = false;
            if (vertical != null) vertical.enabled = false;
        }
    }
}
