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
        private int lastCountdown = -1;
        private TMP_Text forecast;
        private void Start() { CreateForecast(); Refresh(); }

        private void CreateForecast()
        {
            if (forecast != null || statusText == null || director == null) return;
            var canvas = GetComponentInParent<UnityEngine.Canvas>();
            if (canvas == null) return;
            var node = new GameObject("Next wave formation", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            node.transform.SetParent(canvas.transform, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-24, 100); rect.sizeDelta = new Vector2(460, 220);
            var background = node.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(.07f, .025f, .05f, .94f); background.raycastTarget = false;
            var label = new GameObject("Formation and counters", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(node.transform, false);
            forecast = label.GetComponent<TextMeshProUGUI>();
            forecast.font = statusText.font; forecast.fontSize = 18; forecast.color = new Color(1, .92f, .88f);
            forecast.raycastTarget = false; forecast.textWrappingMode = TextWrappingModes.Normal;
            forecast.rectTransform.anchorMin = Vector2.zero; forecast.rectTransform.anchorMax = Vector2.one;
            forecast.rectTransform.offsetMin = new Vector2(16, 12); forecast.rectTransform.offsetMax = new Vector2(-16, -12);
        }
        private void LateUpdate()
        {
            if (director != null && (lastEntrance != director.HasEntrance || director.AutomaticWaves && lastCountdown != Mathf.CeilToInt(director.NextWaveRemaining))) Refresh();
            var summoning = director != null && director.Lord != null ? director.Lord.GetComponent<DarkLord.Player.MinionSummoning>() : null;
            bool interacting = summoning != null && summoning.BlocksGameplayInput || director != null && director.Alchemy != null && director.Alchemy.BlocksGameplayInput;
            interacting |= director != null && director.Economy != null && director.Economy.BlocksGameplayInput;
            interacting |= director != null && director.Tutorial != null && director.Tutorial.Active;
            if (director != null && director.Tutorial != null && director.Tutorial.Active)
            {
                waveText.text = "서막 · 로웬의 침공";
                statusText.text = director.Tutorial.Heading;
                if (startButton != null) startButton.gameObject.SetActive(false);
            }
            if (forecast != null) forecast.transform.parent.gameObject.SetActive(director != null && (director.State == WaveState.Preparing || director.AutomaticWaves && director.State == WaveState.Active && director.HasNextWave) && !interacting && (construction == null || !construction.ExpansionMode));
            if (startButton != null) startButton.interactable = director != null && director.CanStartWave && !interacting && (construction == null || !construction.ExpansionMode);
        }

        private void OnEnable()
        {
            if (director != null) director.Changed += Refresh;
            if (startButton != null) startButton.onClick.AddListener(StartWave);
            Refresh();
        }

        private void OnDisable()
        {
            if (forecast != null) forecast.transform.parent.gameObject.SetActive(false);
            if (director != null) director.Changed -= Refresh;
            if (startButton != null) startButton.onClick.RemoveListener(StartWave);
        }
        private void OnDestroy() { if (forecast != null) Destroy(forecast.transform.parent.gameObject); }

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
            lastCountdown = Mathf.CeilToInt(director.NextWaveRemaining);
            if (forecast != null)
            {
                bool preparing = director.HasNextWave && (director.State == WaveState.Preparing || director.AutomaticWaves && director.State == WaveState.Active);
                forecast.transform.parent.gameObject.SetActive(preparing);
                if (preparing) forecast.text = $"<b>다음 습격 {director.CurrentWave + 1} · {WaveFormation.Name(director.CurrentWave + 1)}</b>\n" +
                    WaveFormation.Summary(director.NextFormation) + $"\n총 {director.NextEnemyCount}명 · 개별 체력 {director.NextEnemyHealth}\n\n" +
                    WaveFormation.Advice(director.CurrentWave + 1) + "\n<color=#9DA6B5>표식: 파랑 방패 · 초록 십자 · 빨강 돌격</color>";
            }
            if (startButton != null) startButton.gameObject.SetActive(!director.AutomaticWaves && director.State == WaveState.Preparing);
            if (startButtonText != null) startButtonText.text = $"웨이브 {director.CurrentWave + 1} 시작";
            waveText.text = $"습격  {director.CurrentWave} / {director.FinalWave}";
            switch (director.State)
            {
                case WaveState.Preparing:
                    statusText.text = director.HasEntrance ? $"다음: {WaveFormation.Name(director.CurrentWave + 1)} · {director.NextEnemyCount}명" : "입구 배치 대기";
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
            if (director.AutomaticWaves && director.HasNextWave && (director.State == WaveState.Preparing || director.State == WaveState.Active))
                statusText.text += $" · 다음 침공 {lastCountdown / 60}:{lastCountdown % 60:00}";
        }
    }
}
