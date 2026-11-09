using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.Minions
{
    // Pairs each frame of the shared slime sheet with the same frame of a variant sheet.
    public sealed class MinionSkinAsset : ScriptableObject
    {
        [SerializeField] private Sprite[] source, variant;
        private Dictionary<Sprite, Sprite> lookup;

        public void Configure(Sprite[] sourceFrames, Sprite[] variantFrames)
        {
            source = sourceFrames; variant = variantFrames; lookup = null;
        }

        public Sprite Resolve(Sprite frame)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<Sprite, Sprite>();
                for (int i = 0; i < source.Length; i++) lookup[source[i]] = variant[i];
            }
            return frame != null && lookup.TryGetValue(frame, out var mapped) ? mapped : frame;
        }
    }
}
