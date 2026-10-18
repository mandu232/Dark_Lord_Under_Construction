var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/MainMenu/NewGameSetupPanel.prefab");
try {
root.SetActive(true);
var panel=root.GetComponent<DarkLord.UI.NewGameSetupPanel>(); panel.Begin(0);
var toggle=root.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
var image=toggle.GetComponent<UnityEngine.UI.Image>();
if(toggle.graphic != null || toggle.transform.Find("Selection Frame").gameObject.activeSelf) throw new System.Exception("Duplicate frame remains");
for(int pass=0;pass<3;pass++) {
toggle.isOn=true; if(image.sprite.name!="CategoryButton_Type01_0") throw new System.Exception("Skip on sprite incorrect");
toggle.isOn=false; if(image.sprite.name!="CategoryButton_Type02_1") throw new System.Exception("Skip off sprite incorrect");
root.SetActive(false); root.SetActive(true);
}
toggle.isOn=true; panel.Begin(0); if(toggle.isOn || image.sprite.name!="CategoryButton_Type02_1") throw new System.Exception("Reset failed");
var reference=root.transform.Find("Setup Card/Back").GetComponent<UnityEngine.UI.Image>();
foreach(var name in new[]{"Easy","Normal","Hard","Nightmare","Skip Opening"}) {
var item=root.transform.Find("Setup Card/"+name).GetComponent<UnityEngine.UI.Image>();
if(item.type!=UnityEngine.UI.Image.Type.Sliced || Mathf.Abs(item.pixelsPerUnitMultiplier-reference.pixelsPerUnitMultiplier)>.0001f || Mathf.Abs(item.rectTransform.rect.height-reference.rectTransform.rect.height)>.01f) throw new System.Exception("Pixel scale mismatch: "+name);
}
return "PASS: matching pixel scale for 5 controls, single skip frame, repeated toggles and reopen/reset";
} finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
