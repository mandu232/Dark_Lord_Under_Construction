using System;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum GameplayEffectKind { Slash, WideSlash, BloodNova, BloodWard, Summon, Curse, Heal, HolySlash, HolyCharge, FrostNova, Aegis, Haste }

    public sealed class GameplayEffectLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Clip
        {
            public GameplayEffectKind kind;
            public Sprite[] frames;
            public float width = 2;
            public float duration = .5f;
            public bool loop;
            public Color tint = Color.white;
            public Vector2 offset;
            public float heightScale = 1;
            public int sortingOffset = 8;
            // Hold a readable shield between its short appearance and disappearance.
            public int sustainFrame = -1;
            public float transitionDuration = .18f;
            public float fadeOut = .08f;
        }
        public Material material;
        // Indexed by DeathLoot.Kind: blood, crystal, gold, experience.
        public Sprite[] lootSprites;
        public Clip[] clips;
        public Clip Find(GameplayEffectKind kind)
        {
            if (clips != null) foreach (var clip in clips) if (clip.kind == kind) return clip;
            return null;
        }
    }
}
