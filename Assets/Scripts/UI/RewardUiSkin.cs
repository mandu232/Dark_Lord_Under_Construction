using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RewardUiSkin : ScriptableObject
    {
        public Sprite cardFrame, button;
        public Sprite vitalTrack, waveStartButton;
        public Sprite technologyTextBox;
        public Sprite[] icons;
        public Sprite Icon(int id) => icons != null && id >= 0 && id < icons.Length ? icons[id] : null;
        public static void ConfigureBuffText(TMPro.TMP_Text text)
        {
            if (text != null) text.spriteAsset = Resources.Load<TMPro.TMP_SpriteAsset>("BuffIcons");
        }
        public static string BuffSymbol(Sprite sprite) => sprite != null ? $"<size=145%><sprite name=\"{sprite.name}\"></size> " : "";
        public static string BuffDetail(string text) => $"<size=85%><color=#B0A29B><font-weight=400>{text}</font-weight></color></size>";
    }
}
