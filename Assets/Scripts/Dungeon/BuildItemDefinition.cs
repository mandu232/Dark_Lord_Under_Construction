using UnityEngine;

namespace DarkLord.Dungeon
{
    public enum BuildCategory { Rooms, Minions, Traps, Entrances }

    [CreateAssetMenu(menuName = "Dark Lord/Build Item")]
    public sealed class BuildItemDefinition : ScriptableObject
    {
        public BuildCategory category;
        public string displayName;
        [TextArea(3, 8)] public string description;
        public Sprite icon;
        public ConstructionKind constructionKind;
        [Tooltip("Section: centered on the completed footprint. Trap: centered on the selected floor tile.")]
        public GameObject prefab;
        public bool available = true;
    }
}
