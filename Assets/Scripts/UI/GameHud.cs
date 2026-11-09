using System.Collections;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Gameplay;
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
        private int lastExperience = -1, lastLevel = -1;
        private TMP_Text experienceText;
        private UnityEngine.UI.Image experienceFill;
        private RectTransform orbFlights;
        private readonly Dictionary<RectTransform, (Vector3 scale, float started)> orbPulses =
            new Dictionary<RectTransform, (Vector3, float)>();
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
            DeathLoot.OrbCollected += OnOrbCollected;
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            DeathLoot.OrbCollected -= OnOrbCollected;
            StopAllCoroutines();
            foreach (var pulse in orbPulses)
                if (pulse.Key != null) pulse.Key.localScale = pulse.Value.scale;
            orbPulses.Clear();
            if (orbFlights != null)
            {
                orbFlights.gameObject.SetActive(false);
                Destroy(orbFlights.gameObject);
                orbFlights = null;
            }
            Unsubscribe();
        }

        private void OnOrbCollected(ActorHealth collector, DeathLoot.Kind kind, Vector3 worldPosition, Sprite sprite)
        {
            if (collector != playerHealth || sprite == null) return;
            RectTransform destination = kind switch
            {
                DeathLoot.Kind.Gold => goldText != null ? goldText.rectTransform : null,
                DeathLoot.Kind.Blood => soulsText != null ? soulsText.rectTransform : null,
                DeathLoot.Kind.Crystal => crystalsText != null ? crystalsText.rectTransform : null,
                DeathLoot.Kind.Experience => experienceFill != null ? (RectTransform)experienceFill.transform.parent : null,
                _ => null
            };
            if (destination == null || !destination.gameObject.activeInHierarchy) return;
            var worldCamera = Camera.main;
            var canvas = GetComponentInParent<Canvas>();
            if (worldCamera == null || canvas == null) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0) return;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var parent = (RectTransform)canvas.transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var start)) return;
            if (orbFlights == null)
            {
                orbFlights = new GameObject("Orb flights", typeof(RectTransform)).GetComponent<RectTransform>();
                orbFlights.SetParent(parent, false);
                orbFlights.anchorMin = orbFlights.anchorMax = parent.pivot;
                orbFlights.anchoredPosition = Vector2.zero;
            }
            var flight = new GameObject("Loot orb flight", typeof(RectTransform)).GetComponent<RectTransform>();
            flight.SetParent(orbFlights, false);
            var orb = new GameObject("Flying loot orb", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            orb.transform.SetParent(flight, false);
            var image = orb.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = Vector2.one * 40; // Exactly 2x the 20 px loot frame, whose art fills about half of it.
            Color tint = kind switch
            {
                DeathLoot.Kind.Gold => new Color(1f, .82f, .24f),
                DeathLoot.Kind.Blood => new Color(1f, .22f, .32f),
                DeathLoot.Kind.Crystal => new Color(1f, .35f, .77f),
                _ => new Color(.28f, 1f, .8f)
            };
            var pulseTarget = kind == DeathLoot.Kind.Experience ? (RectTransform)destination.parent : destination;
            StartCoroutine(FlyOrb(flight, image.rectTransform, start, destination, pulseTarget, tint));
        }

        private IEnumerator FlyOrb(RectTransform flight, RectTransform orb, Vector2 start, RectTransform destination, RectTransform pulseTarget, Color tint)
        {
            const float duration = .55f;
            const int segments = 8;
            var halo = new UnityEngine.UI.Image[segments];
            var core = new UnityEngine.UI.Image[segments];
            for (int i = 0; i < segments; i++) halo[i] = CreateTrailSegment(flight);
            for (int i = 0; i < segments; i++) core[i] = CreateTrailSegment(flight);
            orb.SetAsLastSibling();
            orb.localPosition = start;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
                if (orb == null || destination == null) break;
                float t = Mathf.Clamp01((elapsed + Time.unscaledDeltaTime) / duration);
                var end = (Vector2)flight.InverseTransformPoint(destination.TransformPoint(destination.rect.center));
                float travel = t * t;
                orb.localPosition = OrbFlightPoint(start, end, t);
                orb.localScale = Vector3.one * Mathf.Lerp(1, .45f, travel);
                Vector2 head = orb.localPosition;
                for (int i = 0; i < segments; i++)
                {
                    float fraction = (i + 1f) / segments;
                    Vector2 tail = OrbFlightPoint(start, end, Mathf.Max(0, t - .26f * fraction));
                    float strength = 1 - i / (float)segments;
                    SetTrailSegment(halo[i], head, tail, tint, 13 * strength, .36f * strength);
                    SetTrailSegment(core[i], head, tail, Color.Lerp(tint, Color.white, .7f), 5 * strength, strength);
                    head = tail;
                }
            }
            if (orb != null && destination != null && destination.gameObject.activeInHierarchy)
            {
                PulseOrbDestination(pulseTarget);
                orb.gameObject.SetActive(false);
                for (int i = 0; i < segments; i++) halo[i].enabled = core[i].enabled = false;
                const float sparkleDuration = .18f;
                for (float elapsed = 0; elapsed < sparkleDuration; elapsed += Time.unscaledDeltaTime)
                {
                    if (destination == null || !destination.gameObject.activeInHierarchy) break;
                    Vector2 center = flight.InverseTransformPoint(destination.TransformPoint(destination.rect.center));
                    float progress = elapsed / sparkleDuration;
                    float radius = Mathf.Lerp(5, 16, progress);
                    float opacity = (1 - progress) * (1 - progress);
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = (i + .5f) * Mathf.PI * .5f;
                        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Vector2 tip = center + direction * radius;
                        Vector2 inner = center + direction * (progress * 7);
                        SetTrailSegment(halo[i], tip, inner, tint, 6, .45f * opacity);
                        SetTrailSegment(core[i], tip, inner, Color.Lerp(tint, Color.white, .8f), 2, opacity);
                    }
                    yield return null;
                }
            }
            if (flight != null) Destroy(flight.gameObject);
        }

        private void PulseOrbDestination(RectTransform target)
        {
            if (target == null) return;
            if (orbPulses.TryGetValue(target, out var pulse))
            {
                orbPulses[target] = (pulse.scale, Time.unscaledTime);
                return;
            }
            orbPulses.Add(target, (target.localScale, Time.unscaledTime));
            StartCoroutine(AnimateOrbPulse(target));
        }

        private IEnumerator AnimateOrbPulse(RectTransform target)
        {
            while (target != null && orbPulses.TryGetValue(target, out var pulse))
            {
                float progress = (Time.unscaledTime - pulse.started) / .22f;
                if (progress >= 1)
                {
                    target.localScale = pulse.scale;
                    break;
                }
                float swell = Mathf.Sin(Mathf.PI * Mathf.Pow(progress, .65f));
                target.localScale = pulse.scale * (1 + .1f * swell);
                yield return null;
            }
            orbPulses.Remove(target);
        }

        private static Vector2 OrbFlightPoint(Vector2 start, Vector2 end, float time)
        {
            float travel = time * time;
            return Vector2.Lerp(start, end, travel) + Vector2.up * (Mathf.Sin(travel * Mathf.PI) * 70);
        }

        private static UnityEngine.UI.Image CreateTrailSegment(RectTransform parent)
        {
            var segment = new GameObject("Orb trail segment", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            segment.transform.SetParent(parent, false);
            var image = segment.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void SetTrailSegment(UnityEngine.UI.Image image, Vector2 head, Vector2 tail, Color tint, float width, float opacity)
        {
            Vector2 delta = head - tail;
            image.enabled = delta.sqrMagnitude > .01f;
            if (!image.enabled) return;
            var rect = image.rectTransform;
            rect.localPosition = (head + tail) * .5f;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.sizeDelta = new Vector2(delta.magnitude + 1, width);
            tint.a = opacity;
            image.color = tint;
        }

        private void LateUpdate()
        {
            RefreshMana();
            if (wallet != null && (lastGoldCapacity != wallet.GetCapacity(CurrencyType.Gold) ||
                lastSoulsCapacity != wallet.GetCapacity(CurrencyType.Souls) || lastCrystalsCapacity != wallet.GetCapacity(CurrencyType.DarkCrystals)))
                RefreshCurrencies();
            if (playerHealth != null && (playerHealth.CurrentHealth != lastHealth ||
                playerHealth.MaximumHealth != lastMaximumHealth || playerHealth.Experience != lastExperience || playerHealth.Level != lastLevel)) RefreshHealth();
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
            lastExperience = playerHealth.Experience; lastLevel = playerHealth.Level;
            healthText.text = $"{lastHealth} / {playerHealth.MaximumHealth}";
            if (experienceText != null) experienceText.text = $"Lv.{lastLevel}  EXP {lastExperience}/{playerHealth.ExperienceToNextLevel}";
            SetBarRatio(experienceFill, (float)lastExperience / playerHealth.ExperienceToNextLevel);
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
            CreateBar("Experience Bar", parent, new Vector2(208,-156), new Vector2(310,28),
                new Color32(61,190,151,255), null, out experienceFill, out experienceText);
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
