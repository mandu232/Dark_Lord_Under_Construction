using DarkLord.UI;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed partial class StartingTutorial
    {
        private GameMenu pauseMenu;
        public bool OwnsDialoguePause { get; private set; }
        public float DialogueResumeScale { get; private set; } = 1;
        private bool MenuOwnsPause
        {
            get
            {
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return false;
                if (pauseMenu == null)
                    foreach (var root in gameObject.scene.GetRootGameObjects())
                    { pauseMenu = root.GetComponentInChildren<GameMenu>(true); if (pauseMenu != null) break; }
                return pauseMenu != null && pauseMenu.OwnsPause;
            }
        }
        private void SynchronizeDialoguePause()
        {
            SynchronizePlayerExpression();
            if (!isActiveAndEnabled || !Active || !DialogueOpen) { ReleaseDialoguePause(); return; }
            if (!OwnsDialoguePause)
            {
                DialogueResumeScale = Time.timeScale > 0 ? Time.timeScale : 1;
                OwnsDialoguePause = true;
            }
            // Scaled physics, animations, attack windups, cooldowns and projectiles all stop together.
            Time.timeScale = 0;
        }
        private void ReleaseDialoguePause()
        {
            if (!OwnsDialoguePause) return;
            OwnsDialoguePause = false;
            if (!MenuOwnsPause && (waves == null || waves.State != WaveState.RewardSelection) && Time.timeScale == 0)
                Time.timeScale = DialogueResumeScale;
        }
    }
}
