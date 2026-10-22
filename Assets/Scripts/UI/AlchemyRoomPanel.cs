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
        private GameObject window;
        private TMP_Text balance, status, progress, prompt;
        private UnityEngine.UI.Button blood, crystal, collect, close;
        private TMP_Text collectLabel;
        private UnityEngine.UI.Image progressFill;
        private DungeonExpansion construction;
        private MinionSummoning summoning;
        public bool IsVisible => window != null && window.activeSelf;
        public bool BloodInteractable => blood != null && blood.interactable;
        public bool CrystalInteractable => crystal != null && crystal.interactable;
        public bool CollectInteractable => collect != null && collect.interactable;

        public void Configure(WaveDirector director, TMP_Text fontSource)
        {
            if (window != null) return;
            waves = director;
            construction = director.Lord.GetComponent<DungeonExpansion>();
            summoning = director.Lord.GetComponent<MinionSummoning>();
            window = new GameObject("Alchemy room exchange", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            window.transform.SetParent(transform, false);
            var rect = window.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f);
            rect.anchoredPosition = new Vector2(-24, 0); rect.sizeDelta = new Vector2(400, 416);
            window.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f, .035f, .16f, .97f);
            Label("Title", "연금술실", 24, -12, 30, fontSource.font);
            balance = Label("Balances", "", 18, -48, 28, fontSource.font);
            progress = Label("Progress", "", 17, -80, 26, fontSource.font);
            var track = new GameObject("Alchemy progress", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            track.transform.SetParent(window.transform, false);
            var bar = track.GetComponent<RectTransform>(); bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0, 1);
            bar.anchoredPosition = new Vector2(12, -108); bar.sizeDelta = new Vector2(376, 6);
            track.GetComponent<UnityEngine.UI.Image>().color = new Color(.25f, .16f, .25f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image)); fill.transform.SetParent(track.transform, false);
            progressFill = fill.GetComponent<UnityEngine.UI.Image>(); progressFill.color = new Color(.7f, .35f, .9f); progressFill.raycastTarget = false;
            progressFill.rectTransform.anchorMin = Vector2.zero; progressFill.rectTransform.anchorMax = Vector2.one;
            progressFill.rectTransform.offsetMin = progressFill.rectTransform.offsetMax = Vector2.zero;
            blood = Button("Blood exchange", $"혈액 {AlchemyExchange.BloodCost} → 금화 {AlchemyExchange.BloodGold} · {AlchemyExchange.BloodDuration:0}초", -146, fontSource.font);
            crystal = Button("Crystal exchange", $"혈석 {AlchemyExchange.CrystalCost} → 금화 {AlchemyExchange.CrystalGold} · {AlchemyExchange.CrystalDuration:0}초", -198, fontSource.font);
            blood.onClick.AddListener(ExchangeBlood); crystal.onClick.AddListener(ExchangeCrystal);
            collect = Button("Collect alchemy", "금화 수령", -250, fontSource.font);
            collect.image.color = new Color(.5f, .32f, .08f);
            collectLabel = collect.GetComponentInChildren<TMP_Text>(); collect.onClick.AddListener(Collect);
            status = Label("Status", "", 16, -282, 60, fontSource.font);
            close = Button("Close alchemy", "닫기", -382, fontSource.font); close.onClick.AddListener(Close);
            prompt = Label("Alchemy interaction hint", "F · 중앙 연성 장치 사용", 20, 0, 36, fontSource.font);
            prompt.transform.SetParent(transform, false);
            prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = prompt.rectTransform.pivot = new Vector2(.5f, 0);
            prompt.rectTransform.anchoredPosition = new Vector2(0, 150); prompt.rectTransform.sizeDelta = new Vector2(460, 36);
            prompt.alignment = TextAlignmentOptions.Center; prompt.gameObject.SetActive(false);
            window.SetActive(false);
            gameObject.AddComponent<AlchemyReadyOverlay>().Configure(director, fontSource.font);
        }

        private TMP_Text Label(string name, string text, int size, float y, float height, TMP_FontAsset font)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(window.transform, false);
            var label = node.GetComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.text = text;
            label.color = new Color(1, .93f, .86f); label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.TopLeft; label.textWrappingMode = TextWrappingModes.Normal;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(12, y); rect.sizeDelta = new Vector2(376, height);
            return label;
        }
        private UnityEngine.UI.Button Button(string name, string title, float y, TMP_FontAsset font)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            node.transform.SetParent(window.transform, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(360, 42);
            node.GetComponent<UnityEngine.UI.Image>().color = new Color(.36f, .12f, .3f);
            var label = Label(name + " label", title, 20, 0, 42, font);
            label.transform.SetParent(node.transform, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8, 2); label.rectTransform.offsetMax = new Vector2(-8, -2);
            label.alignment = TextAlignmentOptions.Center;
            return node.GetComponent<UnityEngine.UI.Button>();
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
