using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.UI
{
    public sealed class StorySceneController : MonoBehaviour
    {
        public const string ScenePath = "Assets/Scenes/Story.unity";
        [SerializeField] private OpeningStory story;
        [SerializeField] private string gameplayScene = "Assets/Scenes/InGame.unity";
        private bool loading;

        private void Start()
        {
            // A new scene must not inherit a paused title menu's global clock.
            Time.timeScale = 1;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (story == null) { Debug.LogError("Story scene is missing its opening view.", this); return; }
            if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
            { Debug.LogError("Story destination is missing from Build Settings: " + gameplayScene, this); return; }
            story.Begin(LoadGameplay);
        }

        private void LoadGameplay()
        {
            if (loading) return;
            loading = true;
            Time.timeScale = 1;
            AudioListener.pause = false;
            SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
        }
    }
}
