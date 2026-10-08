using System;
using UnityEngine;

namespace DarkLord.Actors
{
    public enum Faction { Dungeon, Invader }

    [DisallowMultipleComponent]
    public sealed class ActorHealth : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Dungeon;
        [SerializeField, Min(1)] private int maximumHealth = 100;
        private int currentHealth;
        private bool initialized;

        public Faction Faction => faction;
        public int CurrentHealth { get { Initialize(); return currentHealth; } }
        public int MaximumHealth => maximumHealth;
        public bool IsAlive => CurrentHealth > 0;
        public event Action<ActorHealth> Damaged;
        public event Action Died;
        internal Func<ActorHealth, bool> DamageFilter;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            currentHealth = maximumHealth;
            initialized = true;
        }

        public bool TakeDamage(int amount, ActorHealth source = null)
        {
            if (amount <= 0 || !IsAlive || !isActiveAndEnabled) return false;
            if (source != null && (!source.IsAlive || source.Faction == faction)) return false;
            if (DamageFilter != null && !DamageFilter(source)) return false;
            currentHealth = Mathf.Max(0, currentHealth - amount);
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

        public void Configure(Faction team, int health)
        {
            faction = team;
            maximumHealth = Mathf.Max(1, health);
            currentHealth = maximumHealth;
            initialized = true;
        }
    }
}
