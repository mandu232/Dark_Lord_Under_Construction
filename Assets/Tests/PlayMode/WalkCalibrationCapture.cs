using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Player;
using DarkLord.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    public sealed class WalkCalibrationCapture
    {
        [UnityTest] public IEnumerator RecordActualGameplayWalk()
        {
            Time.timeScale=1;
            GameSaveSession.BeginNew(0,new NewGameOptions {difficulty=GameDifficulty.Easy});
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            yield return null; yield return null;
            var director=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WaveDirector>(true)).Single();
            while(director.Tutorial.DialogueOpen) director.Tutorial.NextDialogue();
            yield return null; yield return null;
            var oldSettings=InputSystem.settings; var flags=oldSettings.hideFlags; oldSettings.hideFlags=HideFlags.DontUnloadUnusedAsset;
            var settings=Object.Instantiate(oldSettings); settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=settings;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var player=director.Lord; var sprite=player.GetComponent<SpriteRenderer>(); var body=player.GetComponent<Rigidbody2D>();
            var animator=player.GetComponent<Animator>(); var samples=new List<string>{"time,x,y,velocityMagnitude,sprite,rate"};
            Directory.CreateDirectory("Temp/WalkCalibration");
            try {
                Key[][] directions={new[]{Key.D}, new[]{Key.W}, new[]{Key.A}, new[]{Key.W,Key.D}};
                string[] labels={"right","up","left","diagonal"};
                int[] rows={2,4,2,3};
                for(int d=0;d<directions.Length;d++) {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(directions[d]));
                    for(int i=0;i<12;i++) {
                        yield return new WaitForSecondsRealtime(.03f);
                        samples.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4},{3:F4},{4},{5:F4}",Time.time,body.position.x,body.position.y,body.linearVelocity.magnitude,sprite.sprite!=null?sprite.sprite.name:"null",animator.GetFloat("WalkRate")));
                        if(i==4 && System.Environment.GetEnvironmentVariable("DARKLORD_WALK_CAPTURE")=="1")
                            ScreenCapture.CaptureScreenshot("Temp/WalkCalibration/"+labels[d]+".png");
                    }
                    Assert.That(body.linearVelocity.magnitude, Is.EqualTo(2.475f).Within(.15f), labels[d]);
                    Assert.AreEqual("Walk", sprite.sprite.texture.name, labels[d]);
                    Assert.That(sprite.sprite.rect.y, Is.EqualTo(256-rows[d]*64), labels[d]);
                    Assert.AreEqual(d==2, sprite.flipX, labels[d]);
                }
                var motor=player.GetComponent<DarkLord.Actors.TopDownMotor>();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D)); motor.ApplySlow(.5f,2f);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(body.linearVelocity.magnitude, Is.EqualTo(1.2375f).Within(.1f));
                Assert.That(animator.GetFloat("WalkRate"), Is.EqualTo(.5f).Within(.05f));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(animator.GetFloat("WalkRate"), Is.LessThan(.05f));
                File.WriteAllLines("Temp/WalkCalibration/measurements.csv",samples);
                Assert.Greater(samples.Count,10);
            }
            finally {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings=oldSettings; oldSettings.hideFlags=flags; Object.DestroyImmediate(settings); Time.timeScale=1;
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
