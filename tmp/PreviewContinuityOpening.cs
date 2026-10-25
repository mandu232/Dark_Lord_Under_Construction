foreach(var old in UnityEngine.Object.FindObjectsByType<DarkLord.UI.OpeningStory>(UnityEngine.FindObjectsSortMode.None)) UnityEngine.Object.Destroy(old.gameObject);
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/UI/Story/OpeningStory.prefab");
var instance = UnityEngine.Object.Instantiate(prefab);
var story = instance.GetComponent<DarkLord.UI.OpeningStory>();
story.Begin(() => {});
var host = UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var index = typeof(DarkLord.UI.OpeningStory).GetField("index", flags);
var show = typeof(DarkLord.UI.OpeningStory).GetMethod("ShowCut", flags);
var subtitle = instance.transform.Find("Bottom Box/Subtitle").GetComponent<TMPro.TMP_Text>();
System.IO.Directory.CreateDirectory("output/dialogue-review/continuity/opening");
System.Collections.IEnumerator Capture() {
 var report = new System.Text.StringBuilder();
 for (int cut = 0; cut < 5; cut++) {
  index.SetValue(story,cut); show.Invoke(story,new object[] { false });
  yield return null; yield return null;
  story.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
  yield return new UnityEngine.WaitForEndOfFrame();
  subtitle.ForceMeshUpdate();
  report.AppendLine($"cut={cut+1} font={subtitle.fontSize} lines={subtitle.textInfo.lineCount} height={subtitle.preferredHeight}/{subtitle.rectTransform.rect.height} overflow={subtitle.isTextOverflowing}");
  UnityEngine.ScreenCapture.CaptureScreenshot($"output/dialogue-review/continuity/opening/cut-{cut+1}.png");
  yield return null; yield return null;
 }
 System.IO.File.WriteAllText("output/dialogue-review/continuity/opening/layout-report.txt",report.ToString());
 UnityEngine.Object.Destroy(instance);
}
host.StartCoroutine(Capture()); return "Capturing actual opening prefab and checking all five subtitles";

