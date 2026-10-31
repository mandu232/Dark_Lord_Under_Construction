using UnityEngine;

namespace DarkLord.Dungeon
{
    /// <summary>Places raised facility art between actors while leaving floor lighting on the floor.</summary>
    [DisallowMultipleComponent]
    public sealed class FacilityDepthSorting : MonoBehaviour
    {
        // Actor roots are 13 source pixels above their feet. Facilities meet the
        // floor four pixels below the tile centre; compare both in actor-root space.
        [SerializeField] private float depthOffsetY = .5625f;
        [SerializeField] private SpriteRenderer body;
        private SpriteRenderer[] renderers;
        private int[] offsets;
        private float[] originalAlpha, appliedAlpha;
        private bool[] fadeable;
        private DarkLord.Actors.ActorHealth occlusionTarget;
        private UnityEngine.Rendering.SortingGroup targetSorting;
        private float opacity = 1;

        public void SetOcclusionTarget(DarkLord.Actors.ActorHealth target)
        {
            occlusionTarget = target;
            targetSorting = target != null ? target.GetComponent<UnityEngine.Rendering.SortingGroup>() : null;
        }

        private void Start()
        {
            if (occlusionTarget != null) return;
            foreach (var director in FindObjectsByType<DarkLord.Gameplay.WaveDirector>(FindObjectsSortMode.None))
                if (director.gameObject.scene == gameObject.scene) { SetOcclusionTarget(director.Lord); break; }
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            offsets = new int[renderers.Length];
            originalAlpha = new float[renderers.Length]; appliedAlpha = new float[renderers.Length];
            fadeable = new bool[renderers.Length];
            int baseOrder = body != null ? body.sortingOrder : -90;
            for (int i = 0; i < renderers.Length; i++)
            {
                offsets[i] = renderers[i].sortingOrder <= -31000 ? int.MinValue : renderers[i].sortingOrder - baseOrder;
                originalAlpha[i] = appliedAlpha[i] = renderers[i].color.a;
                var material = renderers[i].sharedMaterial;
                fadeable[i] = offsets[i] != int.MinValue && renderers[i].GetComponent<DarkLord.Actors.SpriteProjectedShadow>() == null &&
                    !(material != null && material.shader.name.Contains("Shadow"));
            }
            Refresh();
        }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            int order = Mathf.Clamp(-Mathf.RoundToInt((transform.position.y + depthOffsetY) * 16), -30000, 30000);
            bool occluded = false;
            if (body != null && body.enabled && body.gameObject.activeInHierarchy && occlusionTarget != null &&
                occlusionTarget.isActiveAndEnabled && occlusionTarget.IsAlive && targetSorting != null && targetSorting.sortingOrder < order)
            {
                // Test the visible character, rather than the transparent margins of its 64px animation frames.
                var playerBounds = new Bounds(occlusionTarget.transform.position + new Vector3(0,-.1875f), new Vector3(.5f,1.25f,1));
                var facilityBounds = body.bounds;
                occluded = playerBounds.min.x < facilityBounds.max.x && playerBounds.max.x > facilityBounds.min.x &&
                    playerBounds.min.y < facilityBounds.max.y && playerBounds.max.y > facilityBounds.min.y;
            }
            opacity = Mathf.MoveTowards(opacity, occluded ? .45f : 1, Time.unscaledDeltaTime * 5);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && offsets[i] != int.MinValue)
                    renderers[i].sortingOrder = order + offsets[i];
                // Keep floor lighting and contact/projected shadows solid. Fade only raised art and its emission.
                if (renderers[i] == null || !fadeable[i]) continue;
                var color = renderers[i].color;
                if (!Mathf.Approximately(color.a, appliedAlpha[i])) originalAlpha[i] = color.a;
                color.a = originalAlpha[i] * opacity;
                renderers[i].color = color; appliedAlpha[i] = color.a;
            }
        }

        private void OnDisable()
        {
            if (renderers == null) return;
            for (int i=0;i<renderers.Length;i++)
                if (renderers[i] != null && Mathf.Approximately(renderers[i].color.a,appliedAlpha[i]))
                { var color=renderers[i].color; color.a=originalAlpha[i]; renderers[i].color=color; appliedAlpha[i]=color.a; }
            opacity=1;
        }
    }
}
