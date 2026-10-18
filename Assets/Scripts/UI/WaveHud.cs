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
        [SerializeField] private DarkLord.Dungeon.DungeonExpansion construction;
        private bool lastEntrance;
        private void LateUpdate()
        {
            if (director != null && lastEntrance != director.HasEntrance) Refresh();
            if (startButton != null) startButton.interactable = director != null && director.CanStartWave && (construction == null || !construction.ExpansionMode);
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

        private void StartWave() { if (director != null && (construction == null || !construction.ExpansionMode)) director.BeginNextWave(); }

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
            waveText.text = $"습격  {director.CurrentWave} / {director.FinalWave}";
            switch (director.State)
            {
                case WaveState.Preparing:
                    statusText.text = director.HasEntrance ? $"다음: 전사 {director.NextEnemyCount}명 · 체력 {director.NextEnemyHealth}\n시작 버튼을 눌러 습격 시작" : "입구 배치 대기";
                    break;
                case WaveState.Active:
                    statusText.text = $"남은 침입자  {director.RemainingEnemies}" + (director.HasEntrance ? "" : " · 입구 대기");
                    break;
                case WaveState.Victorious:
                    waveText.text = "방어 성공";
                    statusText.text = "모든 침입자를 격퇴했습니다";
                    break;
                case WaveState.RewardSelection:
                    statusText.text = "보상을 하나 선택하세요";
                    break;
                default:
                    waveText.text = "성 함락";
                    statusText.text = "침입자에게 패배했습니다";
                    break;
            }
        }
    }
}
