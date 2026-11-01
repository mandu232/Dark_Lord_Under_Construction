using System;
using System.Linq;
using System.Threading.Tasks;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PreviewTutorialBalance {
 public static async Task<string> Run(bool summon=false) {
  if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
  GameSaveSession.BeginNew(0,new NewGameOptions{difficulty=GameDifficulty.Normal,skipOpening=true});
  var load=SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Single);
  while(!load.isDone)await Task.Delay(10);
  await Task.Delay(100);
  var w=UnityEngine.Object.FindAnyObjectByType<WaveDirector>();
  w.Tutorial.Advance(50);int safety=0;while(w.Tutorial.DialogueOpen&&safety++<20)w.Tutorial.NextDialogue();
  await Task.Delay(100);
  var s=w.Lord.GetComponent<MinionSummoning>();int survivors=s.GuardCount;
  var corpse=w.Harvest.Corpses.First();Move(w,corpse.transform.position);bool pickup=w.Harvest.TryInteract();await Task.Delay(100);
  var facility=s.Dungeon.RecruitmentFacilities.Single();Move(w,facility.transform.position+Vector3.down);await Task.Delay(100);
  bool open=s.TryRecruit();await Task.Delay(100);
  var panel=UnityEngine.Object.FindAnyObjectByType<DarkLord.UI.RecruitmentPanel>();
  string label=panel.RandomSummonButton.GetComponentInChildren<TMPro.TMP_Text>().text;
  panel.RandomSummonButton.GetComponentInChildren<TMPro.TMP_Text>().ForceMeshUpdate();bool overflow=panel.RandomSummonButton.GetComponentInChildren<TMPro.TMP_Text>().isTextOverflowing;
  if(summon){panel.RandomSummonButton.onClick.Invoke();await Task.Delay(100);}
  return $"survivors={survivors}, pickup={pickup}, delivered={facility.SummonStacks}, open={open}, label={label}, overflow={overflow}, guards={s.GuardCount}, allSlimes={s.Dungeon.Guards.All(g=>g.Role==DarkLord.Minions.MinionRole.BloodSlime)}, phase={w.Tutorial.TrainingStep}";
 }
 private static void Move(WaveDirector w,Vector3 pos){w.Lord.transform.position=pos;w.Lord.GetComponent<Rigidbody2D>().position=pos;Physics2D.SyncTransforms();}
}
