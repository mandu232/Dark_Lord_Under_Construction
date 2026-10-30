using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class AlchemyRoomPanel : MonoBehaviour
    {
        private WaveDirector waves;
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text balance, status, progress, prompt;
        [SerializeField] private UnityEngine.UI.Button blood, crystal, collect, close;
        [SerializeField] private TMP_Text collectLabel;
        [SerializeField] private UnityEngine.UI.Image progressFill;
        private DungeonExpansion construction;
        private MinionSummoning summoning;
        public bool IsVisible => window != null && window.activeSelf;
        public bool BloodInteractable => blood != null && blood.interactable;
        public bool CrystalInteractable => crystal != null && crystal.interactable;
        public bool CollectInteractable => collect != null && collect.interactable;

        public void Configure(WaveDirector director, TMP_Text fontSource)
        {
            if (waves != null) return;
            waves = director;
            construction = director.Lord.GetComponent<DungeonExpansion>();
            summoning = director.Lord.GetComponent<MinionSummoning>();
            blood.onClick.AddListener(ExchangeBlood); crystal.onClick.AddListener(ExchangeCrystal);
            collect.onClick.AddListener(Collect); close.onClick.AddListener(Close);
            window.SetActive(false); prompt.gameObject.SetActive(false);
            gameObject.AddComponent<AlchemyReadyOverlay>().Configure(director, fontSource.font);
        }

        public void ExchangeBlood() => Exchange(CurrencyType.Souls);
        public void ExchangeCrystal() => Exchange(CurrencyType.DarkCrystals);
        public void Collect()
        {
            if (!IsVisible || waves == null || waves.Alchemy == null) return;
            waves.Alchemy.TryCollect(); Refresh();
        }
        public void Close() { if (waves != null) waves.Alchemy?.Close(); Refresh(); }
        private void Exchange(CurrencyType currency)
        {
            if (!IsVisible || waves == null || waves.Alchemy == null || construction != null && construction.ExpansionMode || summoning != null && summoning.IsOpen) return;
            waves.Alchemy.TryExchange(currency); Refresh();
        }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (window == null) return;
            var exchange = waves != null ? waves.Alchemy : null;
            bool visible = exchange != null && exchange.isActiveAndEnabled && Time.timeScale > 0 &&
                !waves.IsFinished && waves.Lord.IsAlive && exchange.IsOpen && exchange.SelectedStation == exchange.NearbyStation &&
                (construction == null || !construction.ExpansionMode) && (summoning == null || !summoning.IsOpen);
            prompt.gameObject.SetActive(exchange != null && waves.CanUseFacilities && !exchange.IsOpen && exchange.NearbyStation != null && Time.timeScale > 0 && !waves.IsFinished && waves.Lord.IsAlive &&
                (construction == null || !construction.ExpansionMode) && (summoning == null || !summoning.IsOpen));
            if (prompt.gameObject.activeSelf) prompt.text = exchange.NearbyStation.IsReady ? "F · 연성 결과물 수령창 열기" : "F · 중앙 연성 장치 사용";
            window.SetActive(visible);
            if (!visible) return;
            var wallet = exchange.Wallet;
            balance.text = $"금화 {wallet.Gold:N0} / {wallet.GetCapacity(CurrencyType.Gold):N0}";
            blood.interactable = exchange.CanExchange(CurrencyType.Souls, out var bloodReason);
            crystal.interactable = exchange.CanExchange(CurrencyType.DarkCrystals, out var crystalReason);
            var station = exchange.SelectedStation;
            collect.interactable = exchange.CanCollect(out var collectReason);
            collectLabel.text = station.IsReady ? $"금화 {station.GoldOutput} 수령" : station.HasJob ? "연성 완료 후 수령" : "수령할 결과물 없음";
            progress.text = station.HasJob ? station.IsReady ? "연성 완료 · 수령 가능" : $"연성 중 · {Mathf.CeilToInt(station.Remaining)}초 남음" : $"대기 · 혈액 {AlchemyExchange.BloodDuration:0}초 / 혈석 {AlchemyExchange.CrystalDuration:0}초";
            progressFill.rectTransform.anchorMax = new Vector2(station.HasJob ? 1 - station.Remaining / station.Duration : 0, 1);
            status.text = station.IsReady && !collect.interactable ? collectReason : station.HasJob ? station.Message : !blood.interactable && !crystal.interactable
                ? bloodReason + (bloodReason != crystalReason ? "\n" + crystalReason : "") : station.Message;
        }
        private void OnDisable() { if (window != null) window.SetActive(false); if (prompt != null) prompt.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            if (blood != null) blood.onClick.RemoveListener(ExchangeBlood);
            if (crystal != null) crystal.onClick.RemoveListener(ExchangeCrystal);
            if (collect != null) collect.onClick.RemoveListener(Collect);
            if (close != null) close.onClick.RemoveListener(Close);
        }
    }
}
