using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.Actors
{
    /// <summary>
    /// Lowest solid pixel row of each character frame, in sprite-local units. Baked by Apply Pixel Lighting because
    /// textures are not readable at runtime and tight meshes sit 2-3px below the feet (shadows then float).
    /// </summary>
    public sealed class SpriteFeetTable : ScriptableObject
    {
        public Sprite[] sprites;
        public float[] feet;

        private static Dictionary<Sprite, float> lookup;

        public static bool TryGet(Sprite sprite, out float value)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<Sprite, float>();
                var table = Resources.Load<SpriteFeetTable>("SpriteFeet");
                if (table != null)
                    for (int i = 0; i < table.sprites.Length; i++)
                        if (table.sprites[i] != null) lookup[table.sprites[i]] = table.feet[i];
            }
            return lookup.TryGetValue(sprite, out value);
        }
    }
}
