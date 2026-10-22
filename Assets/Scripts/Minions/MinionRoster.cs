using UnityEngine;

namespace DarkLord.Minions
{
    public enum MinionRole { BloodSlime = 0, BloodArcher = 1, BloodSupport = 2 }

    // Bundled prefab references keep old scene/prefab summoning fields compatible.
    public sealed class MinionRoster : ScriptableObject
    {
        public MinionController archer, support;
        public MinionController Find(MinionRole role) => role == MinionRole.BloodArcher ? archer : role == MinionRole.BloodSupport ? support : null;
    }
}
