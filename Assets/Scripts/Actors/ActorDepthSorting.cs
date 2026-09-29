using UnityEngine;
using UnityEngine.Rendering;

namespace DarkLord.Actors
{
    /// <summary>Sort the complete actor by its ground position, including child renderers.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SortingGroup))]
    public sealed class ActorDepthSorting : MonoBehaviour
    {
        [SerializeField] private float groundOffsetY;
        private SortingGroup group;

        private void Awake() => group = GetComponent<SortingGroup>();
        private void OnEnable()
        {
            if (group == null) group = GetComponent<SortingGroup>();
            UpdateDepth();
        }
        private void LateUpdate() => UpdateDepth();

        private void UpdateDepth()
        {
            // One sorting step per source pixel; terrain stays below this entire range.
            int order = Mathf.Clamp(-Mathf.RoundToInt((transform.position.y + groundOffsetY) * 16), -30000, 30000);
            if (group.sortingOrder != order) group.sortingOrder = order;
        }
    }
}
