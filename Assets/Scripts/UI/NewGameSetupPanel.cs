using DarkLord.Gameplay;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class NewGameSetupPanel : MonoBehaviour
    {
        [SerializeField] private TMP_InputField saveName;
        [SerializeField] private TMP_Text heading, description, error;
        [SerializeField] private UnityEngine.UI.Button[] difficultyButtons;
        [SerializeField] private UnityEngine.UI.Toggle skipOpening;
        private GameMenu owner;
        private NewGameOptions draft = new NewGameOptions();
        public UnityEngine.UI.Selectable Focus => difficultyButtons[(int)draft.difficulty];
        public void Bind(GameMenu menu) => owner = menu;

        public void Begin(int slot)
        {
            draft = new NewGameOptions { saveName = "새로운 군주 " + (slot + 1) };
            heading.text = "새로운 통치의 시작";
            saveName.SetTextWithoutNotify(draft.saveName);
            skipOpening.SetIsOnWithoutNotify(false);
            error.text = "";
            Refresh();
        }

        public void ChooseNormal() => Choose(GameDifficulty.Normal);
        public void ChooseEasy() => Choose(GameDifficulty.Easy);
        public void ChooseHard() => Choose(GameDifficulty.Hard);
        public void ChooseNightmare() => Choose(GameDifficulty.Nightmare);
        private void Choose(GameDifficulty value) { draft.difficulty = value; Refresh(); }
        private void Refresh()
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                bool selected = i == (int)draft.difficulty;
                var colors = difficultyButtons[i].colors;
                colors.normalColor = selected ? new Color32(153, 38, 56, 255) : new Color32(46, 22, 33, 255);
                colors.selectedColor = selected ? new Color32(186, 53, 72, 255) : new Color32(92, 39, 55, 255);
                difficultyButtons[i].colors = colors;
                var label = difficultyButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = (selected ? "◆  " : "") + new NewGameOptions { difficulty = (GameDifficulty)i }.DifficultyName;
            }
            description.text = draft.difficulty == GameDifficulty.Easy ? "여유롭게 성을 세우며 통치에 익숙해지는 여정" :
                draft.difficulty == GameDifficulty.Hard ? "치밀한 준비와 판단이 필요한 위험한 통치" :
                draft.difficulty == GameDifficulty.Nightmare ? "가장 가혹한 침공을 견뎌야 하는 군주의 시련" : "건설과 전투의 균형을 즐기는 기본 난이도";
        }
        public void Confirm()
        {
            draft.saveName = NewGameOptions.NormalizeName(saveName.text);
            if (string.IsNullOrEmpty(draft.saveName)) { Error("저장 파일 이름을 입력해 주세요."); saveName.Select(); return; }
            draft.skipOpening = skipOpening.isOn;
            owner.BeginConfiguredGame(draft.Copy());
        }
        public void Cancel() => owner.CancelNewGameSetup();
        public void Error(string message) => error.text = message;
    }
}
