using UnityEngine;

namespace DarkLord.UI
{
    // Prefab-local callbacks forward to the scene menu that owns pause state.
    public sealed class PausePanelBindings : MonoBehaviour
    {
        private GameMenu owner;
        public void Bind(GameMenu menu) => owner = menu;
        public void StartGame() => owner?.StartGame();
        public void SaveGame() => owner?.SaveGame();
        public void OpenSettings() => owner?.OpenSettings();
        public void ReturnToMain() => owner?.ReturnToMain();
        public void AskQuit() => owner?.AskQuit();
    }
}
