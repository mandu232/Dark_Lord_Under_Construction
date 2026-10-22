var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
var summoning=waves.Lord.GetComponent<DarkLord.Player.MinionSummoning>();
void Move(UnityEngine.Vector3 position){waves.Lord.transform.position=position;waves.Lord.GetComponent<UnityEngine.Rigidbody2D>().position=position;UnityEngine.Physics2D.SyncTransforms();}
Move(waves.Harvest.Corpses.First().transform.position);waves.Harvest.TryInteract();
System.Collections.IEnumerator Capture(){yield return new UnityEngine.WaitForSecondsRealtime(.2f);UnityEngine.ScreenCapture.CaptureScreenshot("tmp/CorpseTrainingCarryReview.png");Move(summoning.Dungeon.RecruitmentFacilities.Single().transform.position+UnityEngine.Vector3.down);yield return new UnityEngine.WaitForSecondsRealtime(.4f);summoning.TryRecruit();yield return null;UnityEngine.ScreenCapture.CaptureScreenshot("tmp/CorpseTrainingSummonReview.png");}
waves.StartCoroutine(Capture());return waves.Tutorial.TrainingStep;
