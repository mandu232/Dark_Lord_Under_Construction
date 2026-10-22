var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
UnityEngine.Time.timeScale=0;waves.Tutorial.Advance(39-waves.Tutorial.Elapsed);
System.Collections.IEnumerator Capture(){yield return null;yield return null;UnityEngine.ScreenCapture.CaptureScreenshot("tmp/TutorialBottomDialoguePan.png");}
waves.StartCoroutine(Capture());return UnityEngine.Camera.main.orthographicSize;
