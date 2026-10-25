var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var tutorial=waves.Tutorial;
System.Collections.IEnumerator Capture(){
 typeof(DarkLord.Gameplay.StartingTutorial).GetField("dialoguePage",flags).SetValue(tutorial,3);
 typeof(DarkLord.Gameplay.StartingTutorial).GetField("dialogueLine",flags).SetValue(tutorial,3);
 tutorial.NextDialogue();
 yield return null;yield return null;yield return new UnityEngine.WaitForEndOfFrame();
 UnityEngine.ScreenCapture.CaptureScreenshot("output/dialogue-review/applied/1024x768/RubeTraining.png");
 yield return null;yield return null;
 typeof(DarkLord.Gameplay.StartingTutorial).GetMethod("CompleteTutorial",flags).Invoke(tutorial,null);
 UnityEngine.Time.timeScale=1;
 yield return null;yield return null;
 var economy=waves.Economy;economy.Close();var saved=economy.Capture();saved.nextMerchantWave=2;economy.Restore(saved);
 waves.RestoreSave(new DarkLord.Persistence.WaveSaveData{number=2,state=DarkLord.Gameplay.WaveState.Preparing,enemies=new DarkLord.Persistence.MinionSaveData[0]});
 economy.OnRoundStarted(2);
 waves.Lord.transform.position=economy.MerchantPosition+UnityEngine.Vector3.down*1.5f;
 waves.Lord.GetComponent<UnityEngine.Rigidbody2D>().position=waves.Lord.transform.position;
 yield return null;
 economy.TryInteract();
 yield return null;yield return null;yield return new UnityEngine.WaitForEndOfFrame();
 UnityEngine.ScreenCapture.CaptureScreenshot("output/dialogue-review/applied/1024x768/NibbleMerchant.png");
}
waves.StartCoroutine(Capture());return "Capturing Rube training and Nibble merchant UI";

