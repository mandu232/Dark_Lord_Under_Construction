var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/MainMenu/NewGameSetupPanel.prefab");
try {
var panel = root.GetComponent<DarkLord.UI.NewGameSetupPanel>();
var data = new UnityEditor.SerializedObject(panel);
var buttons = data.FindProperty("difficultyButtons");
var methods = new[] {"ChooseNormal", "ChooseEasy", "ChooseHard", "ChooseNightmare"};
for(int selected = 0; selected < 4; selected++) {
typeof(DarkLord.UI.NewGameSetupPanel).GetMethod(methods[selected]).Invoke(panel, null);
for(int i = 0; i < 4; i++) {
var button = (UnityEngine.UI.Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
var expected = i == selected ? "CategoryButton_Type01_0" : "CategoryButton_Type02_1";
if(button.image.sprite == null || button.image.sprite.name != expected) throw new System.Exception("Incorrect sprite");
if(button.GetComponentInChildren<TMPro.TMP_Text>().text != new DarkLord.Gameplay.NewGameOptions { difficulty = (DarkLord.Gameplay.GameDifficulty)i }.DifficultyName) throw new System.Exception("Incorrect label");
}
}
return "PASS: all four selections, 16 sprite and label states; compiling=" + UnityEditor.EditorApplication.isCompiling;
} finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
