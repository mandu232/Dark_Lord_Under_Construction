using UnityEngine;
namespace DarkLord.Dungeon
{
    // Shared by placement and inspection so both show the same gameplay coverage.
    public sealed class FacilityCoverageDisplay : MonoBehaviour
    {
        private LineRenderer outline;
        public bool IsVisible => outline != null && outline.enabled;
        public LineRenderer Outline => outline;
        public void Show(ResourceBuilding definition, DungeonGrid dungeon, Vector2Int cell, bool allowed = true)
        {
            if (definition == null || dungeon == null || !definition.IsBuffFacility && definition.RoomCapacityBonus <= 0)
            { Hide(); return; }
            bool roomEffect = definition.RoomCapacityBonus > 0;
            var room = roomEffect ? dungeon.Construction.RoomAt(cell) : null;
            if (roomEffect && room == null) { Hide(); return; }
            if (outline == null)
            {
                outline = gameObject.AddComponent<LineRenderer>(); outline.useWorldSpace = true; outline.loop = true;
                outline.sharedMaterial = Resources.Load<Material>("GameplayTraceOriginalColor");
                outline.startWidth = outline.endWidth = .06f; outline.sortingOrder = 40;
            }
            outline.enabled = true; outline.positionCount = roomEffect ? 4 : 64;
            outline.startColor = outline.endColor = allowed ? new Color(.35f,1,.8f,.85f) : new Color(1,.3f,.3f,.8f);
            Vector3 center = dungeon.CellCenter(cell);
            if (roomEffect)
            {
                var min = dungeon.CellCenter(room.Bounds.min) - Vector3.one * DungeonGrid.CellSize * .5f;
                var max = dungeon.CellCenter(room.Bounds.max - Vector2Int.one) + Vector3.one * DungeonGrid.CellSize * .5f;
                outline.SetPosition(0,new Vector3(min.x,min.y,center.z)); outline.SetPosition(1,new Vector3(max.x,min.y,center.z));
                outline.SetPosition(2,new Vector3(max.x,max.y,center.z)); outline.SetPosition(3,new Vector3(min.x,max.y,center.z));
            }
            else for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI / 32;
                outline.SetPosition(i, center + new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0) * definition.BuffRadius);
            }
        }
        public void Hide() { if (outline != null) outline.enabled = false; }
        private void OnDisable() => Hide();
    }
}
