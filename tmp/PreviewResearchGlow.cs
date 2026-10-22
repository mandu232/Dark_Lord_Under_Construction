var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
var panel=UnityEngine.Object.FindFirstObjectByType<DarkLord.UI.CoreTechPanel>();
panel.HideTooltip(DarkLord.Player.TechId.Aegis);
waves.Economy.TryResearch(DarkLord.Player.TechId.Armor);
System.Collections.IEnumerator CaptureGlow(){yield return new UnityEngine.WaitForSecondsRealtime(.4f);UnityEngine.ScreenCapture.CaptureScreenshot("tmp/ResearchGlowReview.png");}
waves.StartCoroutine(CaptureGlow());
return "Capturing real research animation at 0.4 seconds";
