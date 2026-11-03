using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RewardUiSkin : ScriptableObject
    {
        public Sprite cardFrame, button;
        public Sprite vitalTrack, waveStartButton;
        public Sprite technologyTextBox;
        public Sprite[] icons;
        public Sprite Icon(int id)
        {
            int index = id == 14 ? 9 : id == 15 ? 6 : id == 16 ? 10 : id == 17 ? 5 : id;
            return icons != null && index >= 0 && index < icons.Length ? icons[index] : null;
        }
        public static void ConfigureBuffText(TMPro.TMP_Text text)
        {
            if (text != null) text.spriteAsset = Resources.Load<TMPro.TMP_SpriteAsset>("BuffIcons");
        }
        public static string BuffSymbol(Sprite sprite) => sprite != null ? $"<size=145%><sprite name=\"{sprite.name}\"></size> " : "";
        public static string BuffDetail(string text) => $"<size=85%><color=#B0A29B><font-weight=400>{text}</font-weight></color></size>";
    }
}
