using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuTitle : MonoBehaviour
    {
        public enum TitleLanguage { Korean, English }
        [SerializeField] private TitleLanguage language;
        [SerializeField] private Sprite korean, english;
        [SerializeField] private UnityEngine.UI.Image front, back;

        private void OnEnable() => Refresh();
        private void OnValidate() => Refresh();

        public void SetLanguage(TitleLanguage value)
        {
            language = value;
            Refresh();
        }

        private void Refresh()
        {
            var sprite = language == TitleLanguage.Korean ? korean : english;
            if (front != null) front.sprite = sprite;
            if (back != null) back.sprite = sprite;
        }
    }
}
