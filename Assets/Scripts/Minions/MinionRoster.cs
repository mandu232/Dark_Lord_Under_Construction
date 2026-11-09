using UnityEngine;

namespace DarkLord.Minions
{
    // Values are saved in game files: only ever append.
    // BloodArcher, BloodSupport, BladeSlime and AcidSlime are retired: they stay only so old saves can load their replacements.
    public enum MinionRole { BloodSlime = 0, BloodArcher = 1, BloodSupport = 2, BoneSlime = 3, BladeSlime = 4, AcidSlime = 5, VampireBat = 6, BloodGolem = 7, Ghoul = 8, BloodWraith = 9, RedScorpion = 10, WebSpider = 11, BloodKnight = 12, VampireHealer = 13 }

    // Bundled prefab references keep old scene/prefab summoning fields compatible.
    public sealed class MinionRoster : ScriptableObject
    {
        public static readonly MinionRole[] Summonable = { MinionRole.BloodSlime, MinionRole.BoneSlime, MinionRole.VampireBat, MinionRole.BloodGolem, MinionRole.Ghoul, MinionRole.BloodWraith, MinionRole.RedScorpion, MinionRole.WebSpider, MinionRole.BloodKnight, MinionRole.VampireHealer };
        public MinionController bone, bat, golem, ghoul, wraith, scorpion, spider, knight, healer;
        public MinionSkinAsset workerSkin;
        public static MinionRole Replacement(MinionRole saved)
        {
            switch (saved)
            {
                case MinionRole.BloodArcher: case MinionRole.AcidSlime: return MinionRole.RedScorpion;
                case MinionRole.BloodSupport: return MinionRole.VampireHealer;
                case MinionRole.BladeSlime: return MinionRole.VampireBat;
                default: return saved;
            }
        }
        // The blood knight is far stronger than the rest, so it comes up a third as often.
        public static int SummonWeight(MinionRole role) => role == MinionRole.BloodKnight ? 1 : 3;
        public MinionController Find(MinionRole role)
        {
            switch (role)
            {
                case MinionRole.BoneSlime: return bone;
                case MinionRole.VampireBat: return bat;
                case MinionRole.BloodGolem: return golem;
                case MinionRole.Ghoul: return ghoul;
                case MinionRole.BloodWraith: return wraith;
                case MinionRole.RedScorpion: return scorpion;
                case MinionRole.WebSpider: return spider;
                case MinionRole.BloodKnight: return knight;
                case MinionRole.VampireHealer: return healer;
                default: return null;
            }
        }
    }
}
