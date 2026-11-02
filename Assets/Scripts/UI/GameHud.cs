using DarkLord.Actors;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class GameHud : MonoBehaviour
    {
        [Header("Game state")]
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private ActorHealth playerHealth;
        [SerializeField] private MinionSummoning summoning;

        [Header("Labels")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text soulsText;
        [SerializeField] private TMP_Text crystalsText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text minionsText;
        [SerializeField] private UnityEngine.UI.Image healthFill;

        private int lastHealth = -1;
        private int lastMaximumHealth = -1;
        private int lastGuardCount = -1;
        private int lastGoldCapacity = -1, lastSoulsCapacity = -1, lastCrystalsCapacity = -1;
        private DarkLord.Gameplay.WaveDirector director;
        private UnityEngine.UI.Image healthBarFill, manaBarFill;
        private TMP_Text manaText;
        private int lastMana = -1;

        private void Start()
        {
            director = FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
            if (director != null && director.gameObject.scene == gameObject.scene && healthText != null)
            {
                CreateVitalBars();
                DungeonMinimap.Create(transform, director);
                foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    if (button.name == "Start Wave Button")
                        gameObject.AddComponent<WaveStartButtonPresentation>().Configure(director, button, healthText.font);
                gameObject.AddComponent<CombatHud>().Configure(director, healthText);
                gameObject.AddComponent<InvasionAlert>().Configure(director, healthText.font);
                gameObject.AddComponent<StartingTutorialPresentation>().Configure(director, healthText.font);
                gameObject.AddComponent<CorpseHarvestHud>().Configure(director, healthText.font);
                CreatePanel<AlchemyRoomPanel>().Configure(director, healthText);
                CreatePanel<RecruitmentPanel>().Configure(summoning, healthText.font);
                gameObject.AddComponent<SummonPresentation>().Configure(summoning, director, healthText.font);
                CreatePanel<EconomyPanel>().Configure(director, healthText.font);
            }
        }

        private T CreatePanel<T>() where T : Component
        {
            var prefab = Resources.Load<T>("UI/" + typeof(T).Name);
            var panel = Instantiate(prefab, transform, false);
            panel.gameObject.SetActive(true);
            return panel;
        }

        public void Configure(CurrencyWallet currencyWallet, ActorHealth health, MinionSummoning minionSummoning)
        {
            Unsubscribe();
            wallet = currencyWallet;
            playerHealth = health;
            summoning = minionSummoning;
            Subscribe();
            RefreshAll();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            RefreshMana();
            if (wallet != null && (lastGoldCapacity != wallet.GetCapacity(CurrencyType.Gold) ||
                lastSoulsCapacity != wallet.GetCapacity(CurrencyType.Souls) || lastCrystalsCapacity != wallet.GetCapacity(CurrencyType.DarkCrystals)))
                RefreshCurrencies();
            if (playerHealth != null && (playerHealth.CurrentHealth != lastHealth ||
                playerHealth.MaximumHealth != lastMaximumHealth)) RefreshHealth();
            if (summoning != null && summoning.GuardCount != lastGuardCount) RefreshMinions();
        }

        private void Subscribe()
        {
            if (wallet != null)
            {
                wallet.Changed -= OnCurrencyChanged;
                wallet.Changed += OnCurrencyChanged;
            }
        }

        private void Unsubscribe()
        {
            if (wallet != null) wallet.Changed -= OnCurrencyChanged;
        }

        private void OnCurrencyChanged(CurrencyType currency, int amount)
        {
            switch (currency)
            {
                case CurrencyType.Gold: SetAmount(goldText, currency, amount); break;
                case CurrencyType.Souls: SetAmount(soulsText, currency, amount); break;
                case CurrencyType.DarkCrystals: SetAmount(crystalsText, currency, amount); break;
            }
        }

        private void RefreshAll()
        {
            if (wallet != null)
            {
                RefreshCurrencies();
            }
            RefreshHealth();
            RefreshMinions();
        }

        private void RefreshHealth()
        {
            if (playerHealth == null || healthText == null) return;
            lastHealth = playerHealth.CurrentHealth;
            lastMaximumHealth = playerHealth.MaximumHealth;
            healthText.text = $"{lastHealth} / {playerHealth.MaximumHealth}";
            SetBarRatio(healthBarFill, (float)lastHealth / playerHealth.MaximumHealth);
            if (healthFill != null)
            {
                float ratio = Mathf.Clamp01((float)lastHealth / playerHealth.MaximumHealth);
                if (healthFill.type == UnityEngine.UI.Image.Type.Filled)
                    healthFill.fillAmount = ratio;
                else
                    healthFill.rectTransform.anchorMax = new Vector2(ratio, 1);
                healthFill.enabled = ratio > 0;
            }
        }

        private void CreateVitalBars()
        {
            var parent = healthText.transform.parent;
            CreateBar("Shared Health Bar", parent, new Vector2(208,-76), new Vector2(310,28),
                new Color32(190,47,76,255), healthText, out healthBarFill, out _);
            CreateBar("Mana Bar", parent, new Vector2(208,-116), new Vector2(310,28),
                new Color32(57,115,194,255), null, out manaBarFill, out manaText);
            RefreshHealth(); RefreshMana();
        }

        private void CreateBar(string name, Transform parent, Vector2 position, Vector2 size, Color color,
            TMP_Text existing, out UnityEngine.UI.Image fill, out TMP_Text label)
        {
            var frame = new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));
            frame.transform.SetParent(parent,false);
            var rect = frame.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=position; rect.sizeDelta=size;
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            var frameImage = frame.GetComponent<UnityEngine.UI.Image>();
            frameImage.sprite = skin != null ? skin.vitalTrack : null;
            frameImage.type = UnityEngine.UI.Image.Type.Sliced;
            var canvas = GetComponentInParent<Canvas>();
            if (frameImage.sprite != null)
                frameImage.pixelsPerUnitMultiplier = (canvas != null ? canvas.referencePixelsPerUnit : 100) / (frameImage.sprite.pixelsPerUnit * 4);
            frameImage.color=Color.white;
            frame.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var back = new GameObject("Track",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            back.transform.SetParent(frame.transform,false);
            var track = back.GetComponent<RectTransform>(); track.anchorMin=Vector2.zero;track.anchorMax=Vector2.one;
            track.offsetMin=new Vector2(20,8);track.offsetMax=new Vector2(-20,-8);
            back.GetComponent<UnityEngine.UI.Image>().color=new Color32(26,20,34,255);
            back.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var node = new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));node.transform.SetParent(back.transform,false);
            fill=node.GetComponent<UnityEngine.UI.Image>();fill.color=color;fill.raycastTarget=false;
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;
            fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            AddBarStripe(fill.transform, "Highlight", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-3), Vector2.zero, new Color(1,1,1,.28f));
            AddBarStripe(fill.transform, "Shade", Vector2.zero, new Vector2(1,0), Vector2.zero, new Vector2(0,3), new Color(0,0,0,.28f));
            label=existing;
            if(label==null)
            {
                var text = new GameObject("Value",typeof(RectTransform),typeof(TextMeshProUGUI));
                label=text.GetComponent<TextMeshProUGUI>();label.font=healthText.font;
            }
            label.transform.SetParent(frame.transform,false);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=new Vector2(6,0);label.rectTransform.offsetMax=new Vector2(-6,0);
            label.fontSize=16;label.characterSpacing=0;label.alignment=TextAlignmentOptions.Center;
            label.color=new Color32(250,237,222,255);label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;
        }

        private static void AddBarStripe(Transform parent, string name, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            node.transform.SetParent(parent, false);
            var image = node.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
            image.rectTransform.anchorMin = min; image.rectTransform.anchorMax = max;
            image.rectTransform.offsetMin = insetMin; image.rectTransform.offsetMax = insetMax;
        }

        private static void SetBarRatio(UnityEngine.UI.Image fill, float ratio)
        {
            if(fill==null)return;
            ratio=Mathf.Clamp01(ratio);fill.rectTransform.anchorMax=new Vector2(ratio,1);fill.enabled=ratio>0;
        }

        private void RefreshMana()
        {
            if(manaBarFill==null||director==null||director.Combat==null)return;
            var combat=director.Combat;SetBarRatio(manaBarFill,combat.Mana/combat.MaximumMana);
            int value=Mathf.FloorToInt(combat.Mana);
            if(value==lastMana)return;lastMana=value;manaText.text=$"{value} / {combat.MaximumMana:0}";
        }

        private void RefreshMinions()
        {
            if (summoning == null || minionsText == null) return;
            lastGuardCount = summoning.GuardCount;
            minionsText.text = $"하수인  {lastGuardCount}";
        }

        private void RefreshCurrencies()
        {
            lastGoldCapacity = wallet.GetCapacity(CurrencyType.Gold);
            lastSoulsCapacity = wallet.GetCapacity(CurrencyType.Souls);
            lastCrystalsCapacity = wallet.GetCapacity(CurrencyType.DarkCrystals);
            SetAmount(goldText, CurrencyType.Gold, wallet.Gold);
            SetAmount(soulsText, CurrencyType.Souls, wallet.Souls);
            SetAmount(crystalsText, CurrencyType.DarkCrystals, wallet.DarkCrystals);
        }
        private void SetAmount(TMP_Text label, CurrencyType currency, int amount)
        {
            if (label == null) return;
            int capacity = wallet.GetCapacity(currency);
            int amountSize = amount >= 1000000 ? 50 : 75;
            int capacitySize = capacity >= 1000000 ? 45 : 60;
            label.text = $"<size={amountSize}%>{amount:N0}\n<size={capacitySize}%>/ {capacity:N0}</size></size>";
        }
    }
}
