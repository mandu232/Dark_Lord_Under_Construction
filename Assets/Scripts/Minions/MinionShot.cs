using UnityEngine;

namespace DarkLord.Minions
{
    // A brief visual trace for an authoritative hitscan attack. No delayed damage is lost on reload.
    public sealed class MinionShot : MonoBehaviour
    {
        private float remaining = .16f;
        private Material material;
        public static void Show(Transform source, Vector3 target, Color color)
        {
            var node = new GameObject("Blood magic trace"); node.transform.SetParent(source, false);
            var effect = node.AddComponent<MinionShot>();
            var line = node.AddComponent<LineRenderer>();
            effect.material = new Material(Shader.Find("Sprites/Default")); line.sharedMaterial = effect.material;
            line.positionCount = 2; line.useWorldSpace = true; line.startWidth = .12f; line.endWidth = .035f;
            line.startColor = line.endColor = color; line.sortingOrder = 40;
            line.SetPosition(0, source.position); line.SetPosition(1, target);
        }
        private void Update() { remaining -= Time.deltaTime; if (remaining <= 0) Destroy(gameObject); }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
