using UnityEngine;

namespace DarkLord.Dungeon
{
    // Keep existing values stable for authored assets and saved references.
    public enum BuildCategory { Rooms = 0, Minions = 1, Traps = 2, Entrances = 3, Buildings = 4, Corridors = 5, Decorations = 6 }

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
        public bool repairTool;
    }
}
