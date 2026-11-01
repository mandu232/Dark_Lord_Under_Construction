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
        private ActorHealth sharedHealthOwner;
        private readonly System.Collections.Generic.List<ActorHealth> sharedMembers = new System.Collections.Generic.List<ActorHealth>();
        private ActorHealth HealthOwner => sharedHealthOwner != null ? sharedHealthOwner.HealthOwner : this;
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
        public int CurrentHealth { get { var owner = HealthOwner; owner.Initialize(); return owner.currentHealth; } }
        public int MaximumHealth => HealthOwner.maximumHealth;
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

        public void ShareHealthWith(ActorHealth owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            owner = owner.HealthOwner;
            if (owner == this) return;
            if (sharedMembers.Count > 0) throw new InvalidOperationException("A health owner cannot be linked beneath another owner.");
            if (sharedHealthOwner != null) sharedHealthOwner.sharedMembers.Remove(this);
            sharedHealthOwner = owner;
            owner.Initialize();
            if (!owner.sharedMembers.Contains(this)) owner.sharedMembers.Add(this);
        }

        private void NotifyDamage(ActorHealth source, bool died)
        {
            if (died) Died?.Invoke(); else Damaged?.Invoke(source);
            foreach (var member in sharedMembers)
                if (member != null) { if (died) member.Died?.Invoke(); else member.Damaged?.Invoke(source); }
        }

        private void OnDestroy()
        {
            if (sharedHealthOwner != null) sharedHealthOwner.sharedMembers.Remove(this);
        }

        public bool TakeDamage(int amount, ActorHealth source = null, DamageType damageType = DamageType.Physical)
        {
            if (amount <= 0 || !IsAlive || !isActiveAndEnabled) return false;
            if (source != null && (!source.IsAlive || source.Faction == faction)) return false;
            if (DamageFilter != null && !DamageFilter(source)) return false;
            if (DamageMultiplier != null) amount = Mathf.RoundToInt(amount * Mathf.Clamp01(DamageMultiplier(source, damageType)));
            amount = Mathf.RoundToInt(amount * (1f - Resistance(damageType) / 100f));
            if (amount <= 0) return false;
            var owner = HealthOwner;
            int before = owner.currentHealth;
            int floor = owner.ScriptedHealthFloor;
            foreach (var member in owner.sharedMembers) if (member != null) floor = Mathf.Max(floor, member.ScriptedHealthFloor);
            owner.currentHealth = Mathf.Max(Mathf.Min(before, floor), before - amount);
            if (owner.currentHealth < before)
            {
                GetComponent<DarkLord.Minions.MinionController>()?.RegisterPoiseHit();
                ActorHitFeedback.Show(this, source);
            }
            owner.NotifyDamage(source, owner.currentHealth == 0);
            if (owner.currentHealth < before) DarkLord.Player.PlayerHitStop.OnDamage(this, source);
            return true;
        }

        public void RestoreHealth(int maximum, int current)
        {
            var owner = HealthOwner;
            owner.maximumHealth = Mathf.Max(1, maximum);
            owner.currentHealth = Mathf.Clamp(current, 0, owner.maximumHealth);
            owner.initialized = true;
        }

        // Healing never revives a dead actor. Loading a save remains explicit via RestoreHealth.
        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            var owner = HealthOwner;
            int recovered = Mathf.Min(amount, owner.maximumHealth - owner.currentHealth);
            owner.currentHealth += recovered;
            if (recovered > 0) DarkLord.Gameplay.GameplayEffects.Show(transform, DarkLord.Gameplay.GameplayEffectKind.Heal, transform.position, follow: true);
            return recovered;
        }

        public void Kill()
        {
            if (!IsAlive) return;
            var owner = HealthOwner;
            owner.currentHealth = 0;
            owner.NotifyDamage(null, true);
        }

        public void Configure(Faction team, int health)
        {
            faction = team;
            int maximum = Mathf.Max(1, health);
            RestoreHealth(maximum, maximum);
        }
    }
}
