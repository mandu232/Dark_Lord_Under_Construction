using UnityEngine;

namespace DarkLord.UI
{
    public sealed class RewardUiSkin : ScriptableObject
    {
        public Sprite cardFrame, button;
        public Sprite[] icons;
        public Sprite Icon(int id) => icons != null && id >= 0 && id < icons.Length ? icons[id] : null;
    }
}
