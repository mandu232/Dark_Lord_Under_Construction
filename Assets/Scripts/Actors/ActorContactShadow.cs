using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Actors
{
    [ExecuteAlways, DefaultExecutionOrder(150), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class ActorContactShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        private SpriteRenderer shadow;
        private MinionController minion;
        private SpriteRenderer observedSource;

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void OnDisable() { if (shadow != null) shadow.enabled = false; }

        public void Refresh()
        {
            if (shadow == null) shadow = GetComponent<SpriteRenderer>();
            if (source == null && transform.parent != null) source = transform.parent.GetComponent<SpriteRenderer>();
            if (source != observedSource)
            {
                observedSource = source;
                minion = source != null ? source.GetComponent<MinionController>() : null;
            }
            bool visible = source != null && source.enabled && source.sprite != null && source.gameObject.activeInHierarchy &&
                (minion == null || !minion.IsHeld) && source.color.a > 0;
            shadow.enabled = visible;
            if (!visible) return;
            shadow.sortingLayerID = source.sortingLayerID;
            shadow.sortingOrder = source.sortingOrder - 1;
            var color = new Color(1,1,1,source.color.a);
            if (shadow.color != color) shadow.color = color;
        }
    }
}
