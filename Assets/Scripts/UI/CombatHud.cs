using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    // Built beside the existing HUD so old scene/prefab instances need no migration.
    public sealed class CombatHud : MonoBehaviour
    {
        private WaveDirector waves;

        private TMP_Text augmentLabel;
        private string lastAugmentText;
        private TMP_Text preparationHint, recoveryLabel, traitProcLabel;
        private UnityEngine.UI.Button recoveryButton;
        private void ToggleRecovery() => waves.SetAutoMinionRecovery(!waves.AutoMinionRecoveryEnabled);
        public void Configure(WaveDirector source, TMP_Text fontSource)
        {
            waves = source;
            var proc=new GameObject("Trait activation notice",typeof(RectTransform),typeof(TextMeshProUGUI));proc.transform.SetParent(transform,false);
            traitProcLabel=proc.GetComponent<TextMeshProUGUI>();traitProcLabel.font=fontSource.font;traitProcLabel.fontSize=20;
            traitProcLabel.fontStyle=FontStyles.Bold;traitProcLabel.raycastTarget=false;
            traitProcLabel.rectTransform.anchorMin=traitProcLabel.rectTransform.anchorMax=traitProcLabel.rectTransform.pivot=new Vector2(0,1);
            traitProcLabel.rectTransform.anchoredPosition=new Vector2(36,-254);traitProcLabel.rectTransform.sizeDelta=new Vector2(480,32);
            var recovery = new GameObject("Automatic minion recovery", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            recovery.transform.SetParent(transform, false);
            var recoveryRect = recovery.GetComponent<RectTransform>();
            recoveryRect.anchorMin = recoveryRect.anchorMax = recoveryRect.pivot = new Vector2(0, 1);
            recoveryRect.anchoredPosition = new Vector2(208, -198); recoveryRect.sizeDelta = new Vector2(310, 48);
            recovery.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f,.15f,.15f,.94f);
            recoveryButton = recovery.GetComponent<UnityEngine.UI.Button>(); recoveryButton.onClick.AddListener(ToggleRecovery);
            var textNode = new GameObject("Recovery setting", typeof(RectTransform), typeof(TextMeshProUGUI));
            textNode.transform.SetParent(recovery.transform, false); recoveryLabel = textNode.GetComponent<TextMeshProUGUI>();
            recoveryLabel.font = fontSource.font; recoveryLabel.fontSize = 16; recoveryLabel.alignment = TextAlignmentOptions.Center;
            recoveryLabel.raycastTarget = false; recoveryLabel.rectTransform.anchorMin = Vector2.zero; recoveryLabel.rectTransform.anchorMax = Vector2.one;
            recoveryLabel.rectTransform.offsetMin = recoveryLabel.rectTransform.offsetMax = Vector2.zero;
            var augments = new GameObject("Active combat augments", typeof(RectTransform), typeof(TextMeshProUGUI));
            augments.transform.SetParent(transform, false);
            augmentLabel = augments.GetComponent<TextMeshProUGUI>();
            RewardUiSkin.ConfigureBuffText(augmentLabel);
            augmentLabel.font = fontSource.font; augmentLabel.fontSize = 17;
            augmentLabel.color = new Color(1, .82f, .6f); augmentLabel.raycastTarget = false;
            augmentLabel.rectTransform.anchorMin = augmentLabel.rectTransform.anchorMax = augmentLabel.rectTransform.pivot = new Vector2(0, 1);
            augmentLabel.rectTransform.anchoredPosition = new Vector2(36, -294); augmentLabel.rectTransform.sizeDelta = new Vector2(420, 140);
            gameObject.AddComponent<WaveRewardPanel>().Configure(waves, fontSource.font);
            var hint = new GameObject("Defense preparation hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hint.transform.SetParent(transform, false);
            preparationHint = hint.GetComponent<TextMeshProUGUI>();
            preparationHint.font = fontSource.font; preparationHint.fontSize = 18;
            preparationHint.color = new Color(1, .88f, .65f); preparationHint.raycastTarget = false;
            preparationHint.alignment = TextAlignmentOptions.TopRight;
            preparationHint.textWrappingMode = TextWrappingModes.Normal;
            preparationHint.rectTransform.anchorMin = preparationHint.rectTransform.anchorMax = preparationHint.rectTransform.pivot = Vector2.one;
            preparationHint.rectTransform.anchoredPosition = new Vector2(-28, -140);
            preparationHint.rectTransform.sizeDelta = new Vector2(470, 140);
            preparationHint.text = "<b>[B] 던전 건설 · 코어 근처 [F] 강화 · [I] 캐릭터</b>\n방: 병력 분산 배치 · 숙소: 수용 인원 증가\n함정: 길목 방어 · 시설: 생산·전투 강화";
            waves.Changed += RefreshAugments;
            RefreshAugments();
        }
        private void RefreshAugments()
        {
            var ids = waves.Augments.Capture();
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            lastAugmentText = ids.Length == 0 ? "" : "<b>계승된 전투 효과</b>\n" + string.Join(" · ", System.Array.ConvertAll(ids, id => RewardUiSkin.BuffSymbol(skin != null ? skin.Icon(id) : null) + WaveRewardCatalog.Name(id)));
        }
        private void OnDestroy() { if (waves != null) waves.Changed -= RefreshAugments; if (recoveryButton != null) recoveryButton.onClick.RemoveListener(ToggleRecovery); }
        private void LateUpdate()
        {
            if (waves == null || augmentLabel == null) return;
            float procAge=Time.unscaledTime-waves.Augments.LastTriggerTime;
            if(traitProcLabel!=null)
            {
                traitProcLabel.text=procAge<1.5f?waves.Augments.LastTrigger:"";
                var procColor=TraitProcFeedback.ColorFor(waves.Augments.LastTriggeredId);procColor.a=Mathf.Clamp01((1.5f-procAge)*2);traitProcLabel.color=procColor;
            }
            augmentLabel.text = lastAugmentText + (waves.Augments.Has(WaveRewardCatalog.Tribute) ? $"\n영혼 과충전 {waves.Augments.TributeCharges}/5 · 다음 혈파 +{waves.Augments.TributeCharges*10}" : "");
            if (recoveryLabel != null)
            {
                recoveryLabel.text = waves.AutoMinionRecoveryEnabled ? "자동 회복 ON  ·  클릭하여 끄기\n2초마다 혈액 1 → 체력 5 / 하수인" : "자동 회복 OFF  ·  클릭하여 켜기\n혈액을 소모하지 않습니다";
                recoveryLabel.color = waves.AutoMinionRecoveryEnabled ? new Color(.5f,1,.65f) : new Color(.85f,.8f,.73f);
                recoveryButton.interactable = !waves.IsFinished && Time.timeScale > 0;
            }
            if (preparationHint != null) preparationHint.gameObject.SetActive(waves.CanUseFacilities &&
                waves.Tutorial?.Active != true && waves.Economy?.IsOpen != true && waves.Alchemy?.IsOpen != true &&
                waves.Lord.GetComponent<DarkLord.Dungeon.DungeonExpansion>()?.ExpansionMode != true &&
                waves.Lord.GetComponent<MinionSummoning>()?.IsOpen != true);
        }
    }
}
