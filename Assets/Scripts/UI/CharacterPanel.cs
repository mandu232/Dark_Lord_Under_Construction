using System;
using System.Collections.Generic;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    // The I key window: Lilia's stats and the skills on left click, Q and R.
    public sealed class CharacterPanel : MonoBehaviour
    {
        private const float Width = 640, Height = 860;
        private static readonly Color TextColor = new Color32(247, 231, 210, 255), Equipped = new Color32(214, 40, 66, 255),
            Available = new Color32(120, 70, 82, 255), Locked = new Color32(58, 44, 50, 255), Surface = new Color32(24, 15, 22, 245);

        private sealed class Option { public Button button; public TMP_Text label; public string name; public Func<bool> unlocked, equipped; }

        private WaveDirector waves;
        private TMP_FontAsset font;
        private RectTransform host, root;
        private TMP_Text statsLeft, statsRight, status;
        private readonly List<Option> options = new List<Option>();
        private readonly List<Action> describe = new List<Action>();
        private RunEconomy Economy => waves.Economy;

        public bool IsVisible => root != null && root.gameObject.activeInHierarchy;

        public void Configure(WaveDirector director, TMP_FontAsset labelFont)
        {
            waves = director; font = labelFont; host = GetComponent<RectTransform>();
            root = Rect("Character window", transform, new Vector2(24, 0), new Vector2(Width, Height));
            root.anchorMin = root.anchorMax = new Vector2(0, .5f); root.pivot = new Vector2(0, .5f);
            Background(root);
            Label("Character title", root, "캐릭터 정보", 34, new Vector2(-130, 386), new Vector2(330, 50)).alignment = TextAlignmentOptions.Left;
            var close = Button("Close character window", root, new Vector2(225, 386), new Vector2(130, 46), out var closeLabel);
            closeLabel.text = "닫기 · I"; closeLabel.fontSize = 18; close.onClick.AddListener(() => Economy.CloseCharacter());
            statsLeft = Label("Character stats left", root, "", 20, new Vector2(-150, 262), new Vector2(290, 170)); statsLeft.alignment = TextAlignmentOptions.TopLeft;
            statsRight = Label("Character stats right", root, "", 20, new Vector2(150, 262), new Vector2(290, 170)); statsRight.alignment = TextAlignmentOptions.TopLeft;

            var attacks = (AttackStyle[])Enum.GetValues(typeof(AttackStyle));
            Slot("좌클릭 · 기본 공격", 150, "attack", attacks, s => TechCatalog.AttackName(s), s => Economy.AttackUnlocked(s), s => Economy.EquippedAttack == s,
                s => Economy.EquipAttack(s), () => AttackText(Economy.EquippedAttack));
            var spells = new[] { AttackSpell.BloodNova, AttackSpell.FrostNova };
            Slot("Q · 공격 마법", -40, "offensive", spells, s => TechCatalog.OffensiveName(s), s => Economy.OffensiveUnlocked(s), s => Economy.EquippedOffensive == s,
                s => Economy.EquipOffensive(s), () => OffensiveText(Economy.EquippedOffensive));
            var supports = (SupportSpell[])Enum.GetValues(typeof(SupportSpell));
            Slot("R · 보조 마법", -230, "support", supports, s => TechCatalog.SupportName(s), s => Economy.SupportUnlocked(s), s => Economy.EquippedSupport == s,
                s => Economy.EquipSupport(s), () => SupportText(Economy.EquippedSupport));

            status = Label("Character status", root, "", 17, new Vector2(0, -398), new Vector2(590, 36));
            root.gameObject.SetActive(false);
        }

        private static string AttackText(AttackStyle style) => style == AttackStyle.WideSlash ? TechCatalog.Get(TechId.WideSlash).Description :
            style == AttackStyle.BloodLance ? TechCatalog.Get(TechId.BloodLance).Description : "가까운 적을 앞으로 베는 기본 공격입니다.";
        private static string OffensiveText(AttackSpell spell) => spell == AttackSpell.None ? "장착한 공격 마법이 없습니다. 코어에서 혈파를 연구하세요." :
            TechCatalog.Get(spell == AttackSpell.FrostNova ? TechId.FrostNova : TechId.BloodNova).Description;
        private static string SupportText(SupportSpell spell) => spell == SupportSpell.Mend ? TechCatalog.Get(TechId.Mend).Description :
            spell == SupportSpell.Haste ? TechCatalog.Get(TechId.Haste).Description : spell == SupportSpell.Aegis ? TechCatalog.Get(TechId.Aegis).Description :
            "2초 동안 받는 피해를 막습니다. (마나 25 · 재사용 10초)";

        private void Slot<T>(string heading, float y, string id, T[] values, Func<T, string> name, Func<T, bool> unlocked, Func<T, bool> equipped, Func<T, bool> equip, Func<string> text)
        {
            Label(id + " heading", root, heading, 22, new Vector2(0, y + 62), new Vector2(590, 30)).alignment = TextAlignmentOptions.Left;
            float width = (590f - 10 * (values.Length - 1)) / values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                var button = Button($"Equip {id} {value}", root, new Vector2(-295 + width * .5f + i * (width + 10), y + 12), new Vector2(width, 62), out var label);
                button.onClick.AddListener(() => equip(value));
                options.Add(new Option { button = button, label = label, name = name(value), unlocked = () => unlocked(value), equipped = () => equipped(value) });
            }
            var description = Label(id + " description", root, "", 18, new Vector2(0, y - 56), new Vector2(590, 66)); description.alignment = TextAlignmentOptions.TopLeft;
            describe.Add(() => description.text = text());
        }

        private void LateUpdate()
        {
            if (root == null || waves == null || Economy == null) return;
            bool visible = Economy.IsCharacterOpen && Time.timeScale > 0 && !waves.IsFinished;
            root.gameObject.SetActive(visible);
            if (!visible) return;
            root.SetAsLastSibling();
            root.localScale = Vector3.one * Mathf.Min(1, Mathf.Min((host.rect.width - 32) / Width, (host.rect.height - 32) / Height));
            var health = waves.Lord; var combat = waves.Combat; var e = Economy;
            statsLeft.text = $"레벨  {health.Level}  (경험치 {health.Experience}/{health.ExperienceToNextLevel})\n체력  {health.CurrentHealth} / {health.MaximumHealth}\n" +
                $"마나  {Mathf.FloorToInt(combat.Mana)} / {combat.MaximumMana:0}\n기본 공격 피해  {15 + e.AttackBonus}\n공격 마법 피해  {30 + e.MagicBonus}";
            statsRight.text = $"물리 저항  {health.PhysicalResistance}%\n마법 저항  {health.MagicResistance}%\n코어 피해 감소  {waves.Core.TechnologyResistance}%\n" +
                $"이동 속도  +{Mathf.RoundToInt(e.SpeedBonus * 100)}%\n회피 재사용  {combat.DodgeRecoveryTime:0.0}초";
            bool canChange = e.CanChangeLoadout;
            foreach (var option in options)
            {
                bool unlocked = option.unlocked(), equipped = option.equipped();
                option.button.interactable = canChange && unlocked && !equipped;
                option.label.text = option.name + (equipped ? "\n<size=14>장착 중</size>" : unlocked ? "" : "\n<size=14>잠김 · 코어 연구</size>");
                option.button.image.color = equipped ? Equipped : unlocked ? Available : Locked;
            }
            foreach (var update in describe) update();
            status.text = canChange ? "스킬을 눌러 장착합니다. 잠긴 스킬은 코어에서 연구하세요." : "전투 중에는 장착을 바꿀 수 없습니다.";
        }

        private void OnDestroy() { if (root != null) Destroy(root.gameObject); }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false); var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private TMP_Text Label(string name, Transform parent, string value, int size, Vector2 position, Vector2 bounds)
        {
            var text = Rect(name, parent, position, bounds).gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = value;
            text.alignment = TextAlignmentOptions.Center; text.color = TextColor; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        private Button Button(string name, Transform parent, Vector2 position, Vector2 size, out TMP_Text label)
        {
            var rect = Rect(name, parent, position, size); var image = rect.gameObject.AddComponent<Image>(); image.color = Available;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            label = Label(name + " label", rect, "", 18, Vector2.zero, size - new Vector2(8, 4)); return button;
        }
        private void Background(RectTransform rect)
        {
            // The whole window catches the pointer so clicks on it never become attacks during combat.
            var image = rect.gameObject.AddComponent<Image>(); image.color = Surface; image.raycastTarget = true;
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            if (skin == null || skin.technologyTextBox == null) return;
            image.sprite = skin.technologyTextBox; image.type = Image.Type.Sliced; image.color = Color.white;
            var canvas = host.GetComponentInParent<Canvas>();
            image.pixelsPerUnitMultiplier = (canvas != null ? canvas.referencePixelsPerUnit : 100) / (image.sprite.pixelsPerUnit * 2);
        }
    }
}
