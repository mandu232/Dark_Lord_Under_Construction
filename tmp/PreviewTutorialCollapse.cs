var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
waves.Tutorial.Advance(42.4f-waves.Tutorial.Elapsed);
System.Collections.IEnumerator Capture(){yield return null;yield return null;UnityEngine.ScreenCapture.CaptureScreenshot("tmp/StartingTutorialCollapse.png");}
waves.StartCoroutine(Capture());
return new {elapsed=waves.Tutorial.Elapsed,collapsed=waves.Tutorial.Collapsed};
