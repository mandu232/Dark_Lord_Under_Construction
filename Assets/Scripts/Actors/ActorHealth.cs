using System;
using UnityEngine;

namespace DarkLord.Actors
{
    public enum Faction { Dungeon, Invader }
    public enum DamageType { Physical = 0, Magic = 1 }
    [Flags] public enum MinionTrait { None = 0, Magic = 1, Acid = 2, Melee = 4 }

    [DisallowMultipleComponent]
    public sealed class ActorHealth : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Dungeon;
        [SerializeField, Min(1)] private int maximumHealth = 100;
        private int currentHealth;
        private bool initialized;
        [SerializeField, Range(0, 100)] private int physicalResistance;
        [SerializeField, Range(0, 100)] private int magicResistance;
        public int PhysicalResistance => Resistance(DamageType.Physical);
        public int MagicResistance => Resistance(DamageType.Magic);
        public int BasePhysicalResistance => Mathf.Clamp(physicalResistance, 0, 100);
        public int BaseMagicResistance => Mathf.Clamp(magicResistance, 0, 100);
        public int TechnologyResistance { get; internal set; }
        private int Resistance(DamageType type)
        {
            var minion = GetComponent<DarkLord.Minions.MinionController>();
            int physical = 0, magic = 0;
            if (minion != null) minion.GetRoomBonuses(out _, out physical, out magic);
            return Mathf.Clamp((type == DamageType.Magic ? magicResistance : physicalResistance) +
                (type == DamageType.Magic ? magic : physical) + TechnologyResistance, 0, 100);
        }

        public Faction Faction => faction;
        public int CurrentHealth { get { Initialize(); return currentHealth; } }
        public int MaximumHealth => maximumHealth;
        public bool IsAlive => CurrentHealth > 0;
        public event Action<ActorHealth> Damaged;
        public event Action Died;
        internal Func<ActorHealth, bool> DamageFilter;
        internal int ScriptedHealthFloor;
        internal Func<ActorHealth, DamageType, float> DamageMultiplier;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            currentHealth = maximumHealth;
            initialized = true;
        }

        public bool TakeDamage(int amount, ActorHealth source = null, DamageType damageType = DamageType.Physical)
        {
            if (amount <= 0 || !IsAlive || !isActiveAndEnabled) return false;
            if (source != null && (!source.IsAlive || source.Faction == faction)) return false;
            if (DamageFilter != null && !DamageFilter(source)) return false;
            if (DamageMultiplier != null) amount = Mathf.RoundToInt(amount * Mathf.Clamp01(DamageMultiplier(source, damageType)));
            amount = Mathf.RoundToInt(amount * (1f - Resistance(damageType) / 100f));
            if (amount <= 0) return false;
            currentHealth = Mathf.Max(Mathf.Min(currentHealth, ScriptedHealthFloor), currentHealth - amount);
            if (currentHealth == 0) Died?.Invoke();
            else Damaged?.Invoke(source);
            return true;
        }

        public void RestoreHealth(int maximum, int current)
        {
            maximumHealth = Mathf.Max(1, maximum);
            currentHealth = Mathf.Clamp(current, 0, maximumHealth);
            initialized = true;
        }

        // Healing never revives a dead actor. Loading a save remains explicit via RestoreHealth.
        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int recovered = Mathf.Min(amount, maximumHealth - currentHealth);
            currentHealth += recovered;
            return recovered;
        }

        public void Kill()
        {
            if (!IsAlive) return;
            currentHealth = 0;
            Died?.Invoke();
        }

        public void Configure(Faction team, int health)
        {
            faction = team;
            maximumHealth = Mathf.Max(1, health);
            currentHealth = maximumHealth;
            initialized = true;
        }
    }
}
