using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class AttackTimingCapture
    {
        [UnityTest] public IEnumerator GameplayAttackShowsImpactFrameAtDamage()
        {
            Time.timeScale=1; GameSaveSession.BeginNew(0,new NewGameOptions {difficulty=GameDifficulty.Easy});
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            var waves=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WaveDirector>(true)).Single();
            foreach(var minion in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DarkLord.Minions.MinionController>(true)))
                minion.enabled=false;
            while(waves.Tutorial.DialogueOpen) waves.Tutorial.NextDialogue();
            yield return null; yield return null;
            var player=waves.Lord; var sprite=player.GetComponent<SpriteRenderer>(); var animator=player.GetComponent<Animator>();
            var target=new GameObject("Attack calibration target"); SceneManager.MoveGameObjectToScene(target,scene);
            target.transform.position=player.transform.position+Vector3.right*1.15f;
            var targetRenderer=target.AddComponent<SpriteRenderer>(); targetRenderer.sprite=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/Characters/Player/Idle Collection Front.png").OfType<Sprite>().First();
            targetRenderer.color=new Color(.4f,.6f,.9f);targetRenderer.sortingOrder=100;
            var collider=target.AddComponent<CircleCollider2D>();collider.radius=.2f;
            var enemy=target.AddComponent<ActorHealth>();enemy.Configure(Faction.Invader,100);
            Physics2D.SyncTransforms();
            float started=Time.time; int hitFrame=-1; float hitTime=-1; string hitSprite=null;
            enemy.Damaged+=a=>{hitFrame=Time.frameCount;hitTime=Time.time-started;hitSprite=sprite.sprite!=null?sprite.sprite.name:null;};
            var records=new List<string>{"time,frame,sprite,enemy_hp,attacking,normalized"};
            Directory.CreateDirectory("Temp/AttackTiming");
            try {
                Assert.IsTrue(waves.Combat.TryAttack(Vector2.right), "Gameplay attack should start");
                Assert.AreEqual("Slash_1_12", hitSprite, "The impact frame must be sampled before damage.");
                int i=0;
                while(Time.time-started<.6f) {
                    yield return null;
                    var state=animator.GetCurrentAnimatorStateInfo(0);
                    records.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F4},{1},{2},{3},{4},{5:F3}",Time.time-started,Time.frameCount,sprite.sprite!=null?sprite.sprite.name:"null",enemy.CurrentHealth,waves.Combat.IsAttacking,state.normalizedTime));
                    if((i==2 || i==8) && System.Environment.GetEnvironmentVariable("DARKLORD_ATTACK_CAPTURE")=="1") ScreenCapture.CaptureScreenshot("Temp/AttackTiming/attack-"+i+".png");
                    i++;
                }
                File.WriteAllLines("Temp/AttackTiming/attack.csv",records);
                Assert.That(hitFrame,Is.GreaterThan(0),"Gameplay attack should hit target");
                Assert.That(hitTime,Is.GreaterThanOrEqualTo(0));
                Assert.That(enemy.CurrentHealth,Is.EqualTo(75));
                Assert.IsTrue(records.Any(r=>r.Contains("Slash_1_")),"Slash should be displayed");
                Assert.IsFalse(waves.Combat.IsAttacking);
                Vector2[] directions={Vector2.down,new Vector2(1,-1),Vector2.right,Vector2.one,Vector2.up,new Vector2(-1,1),Vector2.left,-Vector2.one};
                int[] rows={0,1,2,3,4,3,2,1};
                for(int direction=0;direction<directions.Length;direction++) {
                    Assert.IsTrue(waves.Combat.TryAttack(directions[direction]), "Attack "+direction);
                    Assert.AreEqual("Slash_1",sprite.sprite.texture.name);
                    Assert.AreEqual(256-rows[direction]*64,sprite.sprite.rect.y);
                    Assert.AreEqual(directions[direction].x<0,sprite.flipX);
                    yield return new WaitForSecondsRealtime(.48f);
                    Assert.IsFalse(waves.Combat.IsAttacking);
                    Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
                    yield return new WaitForSecondsRealtime(.1f);
                }
            } finally {Object.Destroy(target);Time.timeScale=1;}
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
