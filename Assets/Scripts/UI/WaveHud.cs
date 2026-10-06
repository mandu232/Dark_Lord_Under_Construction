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
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private TMP_Text startButtonText;
        private bool lastEntrance;
        private void LateUpdate()
        {
            if (director != null && lastEntrance != director.HasEntrance) Refresh();
            if (startButton != null) startButton.interactable = director != null && director.CanStartWave;
        }

        private void OnEnable()
        {
            if (director != null) director.Changed += Refresh;
            if (startButton != null) startButton.onClick.AddListener(StartWave);
            Refresh();
        }

        private void OnDisable()
        {
            if (director != null) director.Changed -= Refresh;
            if (startButton != null) startButton.onClick.RemoveListener(StartWave);
        }

        private void StartWave() { if (director != null) director.BeginNextWave(); }

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
            lastEntrance = director.HasEntrance;
            if (startButton != null) startButton.gameObject.SetActive(director.State == WaveState.Preparing);
            if (startButtonText != null) startButtonText.text = $"웨이브 {director.CurrentWave + 1} 시작";
            waveText.text = director.CurrentWave == 0 ? "습격  --" : $"습격  {director.CurrentWave}";
            switch (director.State)
            {
                case WaveState.Preparing:
                    statusText.text = director.HasEntrance ? "시작 버튼을 눌러 습격 시작" : "입구 배치 대기";
                    break;
                case WaveState.Active:
                    statusText.text = $"남은 침입자  {director.RemainingEnemies}" + (director.HasEntrance ? "" : " · 입구 대기");
                    break;
                default:
                    waveText.text = "성 함락";
                    statusText.text = "침입자에게 패배했습니다";
                    break;
            }
        }
    }
}
