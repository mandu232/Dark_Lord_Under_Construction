var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
waves.Harvest.DropCarried();
UnityEngine.ScreenCapture.CaptureScreenshot("tmp/NearbyCorpseHighlightReview.png");
return "Nearby F hint preview";
