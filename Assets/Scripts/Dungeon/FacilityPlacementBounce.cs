using UnityEngine;

namespace DarkLord.Dungeon
{
    // Animate the intact artwork only; the facility root, health and collider stay fixed.
    [DisallowMultipleComponent]
    public sealed class FacilityPlacementBounce : MonoBehaviour
    {
        public const float Duration = .55f;
        private Vector3 restingPosition, restingScale;
        private float elapsed;
        private bool playing, captured;
        public void Play()
        {
            if (!captured)
            {
                restingPosition = transform.localPosition; restingScale = transform.localScale; captured = true;
            }
            elapsed = 0; playing = true; enabled = true;
            Present(0);
        }
        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            if (elapsed >= Duration) { Restore(); enabled = false; return; }
            Present(elapsed / Duration);
        }
        private void Present(float t)
        {
            float squash = Mathf.Sin(t * Mathf.PI * 5) * Mathf.Pow(1 - t, 2) * .22f;
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2)) * Mathf.Pow(1 - t, 2) * .28f;
            transform.localScale = Vector3.Scale(restingScale, new Vector3(1 - squash * .6f, 1 + squash, 1));
            transform.localPosition = restingPosition + Vector3.up * hop;
        }
        private void Restore()
        {
            if (!captured) return;
            transform.localPosition = restingPosition; transform.localScale = restingScale; playing = false;
        }
        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
