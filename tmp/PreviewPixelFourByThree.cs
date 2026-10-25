var waves = UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
if (waves.Tutorial == null) waves.BeginTutorial();
var presentation = UnityEngine.Object.FindFirstObjectByType<DarkLord.UI.StartingTutorialPresentation>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var pageField = typeof(DarkLord.Gameplay.StartingTutorial).GetField("dialoguePage", flags);
var lineField = typeof(DarkLord.Gameplay.StartingTutorial).GetField("dialogueLine", flags);
var detail = presentation.transform.Find("Opening tutorial/Tutorial instructions").GetComponent<TMPro.TMP_Text>();
System.IO.Directory.CreateDirectory("output/dialogue-review/pixel/1024x768");
System.Collections.IEnumerator Capture() {
 var report = new System.Text.StringBuilder();
 for (int page = 0; page < 5; page++) {
  for (int line = 0; line < DarkLord.Gameplay.StartingTutorial.DialogueLineCount(page); line++) {
   pageField.SetValue(waves.Tutorial, page); lineField.SetValue(waves.Tutorial, line);
   yield return null; yield return null;
   typeof(DarkLord.UI.StartingTutorialPresentation).GetField("revealedCharacters", flags).SetValue(presentation, (float)detail.textInfo.characterCount); detail.maxVisibleCharacters = int.MaxValue;
   yield return new UnityEngine.WaitForEndOfFrame();
   UnityEngine.Canvas.ForceUpdateCanvases(); detail.ForceMeshUpdate();
   report.AppendLine($"{page}/{line} speaker={waves.Tutorial.SpeakerName} lines={detail.textInfo.lineCount} font={detail.fontSize} height={detail.preferredHeight}/{detail.rectTransform.rect.height} overflow={detail.isTextOverflowing}");
   UnityEngine.ScreenCapture.CaptureScreenshot($"output/dialogue-review/pixel/1024x768/dialogue-{page}-{line}.png");
   yield return null; yield return null;
  }
 }
 System.IO.File.WriteAllText("output/dialogue-review/pixel/1024x768/layout-report.txt", report.ToString());
}
waves.StartCoroutine(Capture());
return "Capturing all 13 dialogue lines with actual overlay canvas";


