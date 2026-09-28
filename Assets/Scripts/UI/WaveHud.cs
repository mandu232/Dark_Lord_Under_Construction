using DarkLord.Gameplay;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class WaveHud : MonoBehaviour
    {
        [SerializeField] private WaveDirector director;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text statusText;

        private void OnEnable()
        {
            if (director != null) director.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (director != null) director.Changed -= Refresh;
        }

        public void Configure(WaveDirector source, TMP_Text wave, TMP_Text status)
        {
            if (director != null) director.Changed -= Refresh;
            director = source; waveText = wave; statusText = status;
            if (isActiveAndEnabled && director != null) director.Changed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (director == null || waveText == null || statusText == null) return;
            waveText.text = director.CurrentWave == 0 ? "WAVE  --" : $"WAVE  {director.CurrentWave}";
            switch (director.State)
            {
                case WaveState.Preparing:
                    statusText.text = $"NEXT ASSAULT  {Mathf.CeilToInt(director.Countdown)}s";
                    break;
                case WaveState.Active:
                    statusText.text = $"INVADERS  {director.RemainingEnemies}";
                    break;
                default:
                    waveText.text = "DUNGEON FALLEN";
                    statusText.text = "THE INVADERS HAVE WON";
                    break;
            }
        }
    }
}
