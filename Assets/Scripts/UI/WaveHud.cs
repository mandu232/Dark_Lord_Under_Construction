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
            waveText.text = director.CurrentWave == 0 ? "습격  --" : $"습격  {director.CurrentWave}";
            switch (director.State)
            {
                case WaveState.Preparing:
                    statusText.text = $"다음 습격  {Mathf.CeilToInt(director.Countdown)}초";
                    break;
                case WaveState.Active:
                    statusText.text = $"남은 침입자  {director.RemainingEnemies}";
                    break;
                default:
                    waveText.text = "성 함락";
                    statusText.text = "침입자에게 패배했습니다";
                    break;
            }
        }
    }
}
