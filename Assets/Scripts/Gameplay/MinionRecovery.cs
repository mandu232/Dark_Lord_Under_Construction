using DarkLord.Actors;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed partial class WaveDirector
    {
        public const float MinionRecoveryInterval = 2f;
        public const int MinionRecoveryAmount = 5, MinionRecoveryBloodCost = 1;
        public bool AutoMinionRecoveryEnabled { get; private set; } = true;
        public void SetAutoMinionRecovery(bool enabled)
        {
            AutoMinionRecoveryEnabled = enabled;
            minionRecoveryElapsed = 0;
            if (!enabled) ClearRecoveryIndicators();
            Changed?.Invoke();
        }
        private void ClearRecoveryIndicators()
        {
            if (dungeon == null) return;
            foreach (var guard in dungeon.Guards)
                if (guard != null) guard.GetComponentInChildren<MinionRecoveryIndicator>(true)?.Stop();
        }
        private float minionRecoveryElapsed;
        private int minionRecoveryCursor;

        private void UpdatePreparationHealing(float elapsed)
        {
            if (!AutoMinionRecoveryEnabled || !isActiveAndEnabled || Time.timeScale <= 0 || State != WaveState.Preparing || RemainingEnemies != 0 ||
                Tutorial != null && Tutorial.Active || dungeon == null || wallet == null ||
                dungeonLord == null || !dungeonLord.IsAlive || dungeonCore == null || !dungeonCore.IsAlive)
            {
                minionRecoveryElapsed = 0;
                ClearRecoveryIndicators();
                return;
            }
            minionRecoveryElapsed += Mathf.Max(0, elapsed);
            if (minionRecoveryElapsed < MinionRecoveryInterval) return;
            // Do not bill multiple recovery pulses at once after a long frame.
            minionRecoveryElapsed = 0;
            var guards = dungeon.Guards;
            int count = guards.Count;
            if (count == 0) { minionRecoveryCursor = 0; return; }
            int start = minionRecoveryCursor % count;
            for (int offset = 0; offset < count; offset++)
            {
                int index = (start + offset) % count;
                var guard = guards[index];
                if (guard == null || !guard.isActiveAndEnabled || guard.gameObject.scene != gameObject.scene ||
                    guard.Health.Faction != Faction.Dungeon || !guard.IsAlive ||
                    guard.Health.CurrentHealth >= guard.Health.MaximumHealth) continue;
                if (!wallet.TrySpend(CurrencyType.Souls, MinionRecoveryBloodCost)) break;
                if (guard.Health.Heal(MinionRecoveryAmount) > 0) MinionRecoveryIndicator.Show(guard.Health);
                // Scarce blood is shared fairly rather than always going to the first guard.
                minionRecoveryCursor = (index + 1) % count;
            }
        }
    }
}
