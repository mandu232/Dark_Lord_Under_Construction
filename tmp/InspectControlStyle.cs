var report = new System.Text.StringBuilder();
foreach(var path in new[]{"Assets/Prefabs/UI/SettingsPanel.prefab","Assets/Prefabs/UI/SaveSlotPanel.prefab"}) {
var root=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
report.AppendLine(path);
foreach(var c in root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true)) {
var r=(UnityEngine.RectTransform)c.transform;
report.AppendLine(c.GetType().Name+" "+UnityEditor.AnimationUtility.CalculateTransformPath(c.transform,root.transform)+" size="+r.sizeDelta+" sprite="+(c.image != null && c.image.sprite != null ? c.image.sprite.name : "none")+" color="+c.image?.color);
if(c is UnityEngine.UI.Slider s) foreach(var im in s.GetComponentsInChildren<UnityEngine.UI.Image>(true))report.AppendLine("  "+UnityEditor.AnimationUtility.CalculateTransformPath(im.transform,s.transform)+" size="+im.rectTransform.sizeDelta+" anchors="+im.rectTransform.anchorMin+"/"+im.rectTransform.anchorMax);
}}
return report.ToString();

