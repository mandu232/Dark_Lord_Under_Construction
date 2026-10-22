var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
void Dismiss(){int n=0;while(waves.Tutorial.DialogueOpen&&n++<20)waves.Tutorial.NextDialogue();}
Dismiss();waves.Tutorial.Advance(18);
System.Collections.IEnumerator Capture(){yield return new UnityEngine.WaitForSecondsRealtime(11);waves.Tutorial.Advance(DarkLord.Gameplay.StartingTutorial.BattleEnds-waves.Tutorial.Elapsed);Dismiss();yield return new UnityEngine.WaitForSecondsRealtime(1.5f);UnityEngine.ScreenCapture.CaptureScreenshot("tmp/RowenRetreatReview.png");yield return new UnityEngine.WaitForSecondsRealtime(3.3f);waves.Tutorial.Advance(46-waves.Tutorial.Elapsed);Dismiss();yield return new UnityEngine.WaitForSecondsRealtime(.3f);UnityEngine.ScreenCapture.CaptureScreenshot("tmp/CorpseTrainingObjectiveReview.png");}
waves.StartCoroutine(Capture());return "Capturing actual Rowen movement and reconstruction objective";
