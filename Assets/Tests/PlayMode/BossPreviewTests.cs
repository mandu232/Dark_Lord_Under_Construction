using System.Collections;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    // Temporary visual preview of the Rowen fight; delete after review.
    public sealed class BossPreviewTests
    {
        [UnityTest]public IEnumerator PreviewRowenFight()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            var waves=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WaveDirector>(true)).Single();
            waves.ConfigureRun(new NewGameOptions{difficulty=GameDifficulty.Easy});
            waves.RestoreSave(new WaveSaveData{number=waves.FinalWave-1,state=WaveState.Preparing,enemies=new MinionSaveData[0]});
            var data=new UnityEditor.SerializedObject(waves);data.FindProperty("spawnInterval").floatValue=.05f;data.ApplyModifiedPropertiesWithoutUndo();
            waves.BeginNextWave();
            for(float end=Time.time+4;waves.ActiveBoss==null&&Time.time<end;)yield return null;
            var boss=waves.ActiveBoss;
            Debug.Log("Boss spawned: "+(boss!=null?boss.name+" hp "+boss.MaximumHealth:"none"));
            foreach(var enemy in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MinionController>()).Where(m=>m.Health.Faction==DarkLord.Actors.Faction.Invader&&m.Health!=boss).ToArray())enemy.Health.Kill();
            var lord=waves.Lord;
            boss.GetComponent<Rigidbody2D>().position=(Vector2)lord.transform.position+Vector2.right*3f;
            float halfAt=Time.time+14;bool halved=false;
            for(float end=Time.time+34;Time.time<end&&boss!=null&&boss.IsAlive;)
            {
                lord.RestoreHealth(lord.MaximumHealth,lord.MaximumHealth);
                if(Random.value<.25f)waves.Combat.TryAttack(((Vector2)boss.transform.position-(Vector2)lord.transform.position).normalized);
                if(!halved&&Time.time>halfAt){halved=true;boss.RestoreHealth(boss.MaximumHealth,boss.MaximumHealth/2);Debug.Log("Boss halved");}
                yield return null;
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
