using System.Collections;
using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("RelocationEdgePan")]
    [Category("OpeningRegression")]
    public sealed class RelocationEdgePanTests
    {
        private Scene scene;
        private DungeonGrid grid;
        private MinionSummoning summoning;
        private Camera camera;
        private CameraFollow follow;
        private Mouse mouse;
        private Keyboard keyboard;
        private InputSettings originalSettings,settings;
        private HideFlags flags;
        private float previousTime;
        private T Find<T>()where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
        [UnitySetUp]public IEnumerator Setup()
        {
            previousTime=Time.timeScale;Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");yield return null;yield return null;
            grid=Find<DungeonGrid>();summoning=Find<MinionSummoning>();camera=Find<Camera>();follow=camera.GetComponent<CameraFollow>();
            Find<WaveDirector>().ConfigureRun(new NewGameOptions{difficulty=GameDifficulty.Easy});
            camera.GetComponents<Behaviour>().First(b=>b.GetType().Name=="PixelPerfectCamera").enabled=false;camera.orthographicSize=8;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();
            originalSettings=InputSystem.settings;flags=originalSettings.hideFlags;originalSettings.hideFlags=HideFlags.DontUnloadUnusedAsset;
            settings=Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;mouse=InputSystem.AddDevice<Mouse>();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            Time.timeScale=1;if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(originalSettings!=null){InputSystem.settings=originalSettings;originalSettings.hideFlags=flags;}if(settings!=null)Object.DestroyImmediate(settings);Time.timeScale=previousTime;
        }
        private void Pointer(Vector2 point,bool held=true)=>InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=(ushort)(held?1:0)});
        [UnityTest] public IEnumerator HeldPreviewStaysUnderPointerDuringWasdPanWithPixelPerfectCamera()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();
            var pixels=camera.GetComponents<Behaviour>().First(b=>b.GetType().Name=="PixelPerfectCamera");
            pixels.enabled=true;
            var guard=grid.Guards.First();
            Assert.IsTrue(summoning.BeginRelocation(guard,true));
            Vector2 cursor=camera.pixelRect.center;
            Pointer(cursor);yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));
            var visual=summoning.GetComponent<MinionRelocationVisuals>();
            float worst=0;var start=camera.transform.position;
            for(int i=0;i<25;i++)
            {
                yield return new WaitForEndOfFrame();
                Assert.IsTrue(summoning.IsRelocating);Assert.IsNotNull(visual.GroundShadow);
                Vector3 screen=camera.WorldToScreenPoint(visual.GroundShadow.transform.position+Vector3.up*.12f);
                worst=Mathf.Max(worst,Vector2.Distance(screen,cursor));
                yield return null;
            }
            Assert.Greater(Vector2.Distance(start,camera.transform.position),.5f);
            Assert.Less(worst,1.5f,"Held preview should remain anchored to the pointer while the pixel-perfect view moves.");
            cursor += new Vector2(80,40);Pointer(cursor);
            yield return null;yield return new WaitForEndOfFrame();
            Vector3 moved=camera.WorldToScreenPoint(visual.GroundShadow.transform.position+Vector3.up*.12f);
            Assert.Less(Vector2.Distance(moved,cursor),1.5f,"Mouse movement must also track during keyboard panning.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            var room=grid.Construction.RoomAt(grid.Layout.Core);
            var cell=room.Cells.First(c=>summoning.CanPlace(c,out _));
            Vector2 drop=camera.WorldToScreenPoint(grid.CellCenter(cell));
            Assert.IsTrue(camera.pixelRect.Contains(drop));
            Pointer(drop);yield return null;yield return new WaitForEndOfFrame();
            Assert.AreEqual(cell,grid.WorldToCell(visual.GroundShadow.transform.position+Vector3.up*.12f));
            Pointer(drop,false);yield return null;yield return null;
            Assert.IsFalse(summoning.IsRelocating);Assert.AreSame(room,guard.AssignedRoom);
        }
        [UnityTest]public IEnumerator WasdMovesPlacementCameraWithoutMovingPlayerAndCancelRestoresView()
        {
            keyboard=InputSystem.AddDevice<Keyboard>();
            var lord=Find<WaveDirector>().Lord;
            var origin=camera.transform.position;var playerOrigin=lord.transform.position;
            Assert.IsTrue(summoning.BeginRelocation(grid.Guards.First(),true));
            Pointer(camera.pixelRect.center);yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(camera.transform.position.x,origin.x+.5f);Assert.Greater(camera.transform.position.y,origin.y+.5f);
            Assert.Less(Vector3.Distance(lord.transform.position,playerOrigin),.03f);
            Assert.IsTrue(summoning.IsRelocating);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
            var stopped=camera.transform.position;yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Vector3.Distance(stopped,camera.transform.position),Is.LessThan(.001f));
            summoning.SetOpen(false);Assert.That(Vector3.Distance(origin,camera.transform.position),Is.LessThan(.001f));
            Assert.IsTrue(follow.enabled);Assert.AreEqual(8,camera.orthographicSize);
        }
        [UnityTest]public IEnumerator HeldMouseAtEdgeRevealsRemoteRoomAndDropRestoresCamera()
        {
            var destination=grid.Layout.Core+new Vector2Int(14,9);var session=Find<GameSaveSession>();var saved=session.Capture();
            var bounds=DungeonConstruction.Footprint(ConstructionKind.Room,destination);var cellList=new System.Collections.Generic.List<Vector2Int>();foreach(var cell in bounds.allPositionsWithin)cellList.Add(cell);var cells=cellList.ToArray();
            foreach(var cell in cells)saved.dungeon.cells[cell.y*saved.dungeon.width+cell.x]=(int)DungeonCell.Floor;
            saved.dungeon.sections=saved.dungeon.sections.Concat(new[]{new SectionSaveData{kind=ConstructionKind.Room,bounds=bounds,cells=cells,complete=true,initial=true}}).ToArray();session.Restore(saved);Time.timeScale=1;
            yield return null;var original=camera.transform.position;var guard=grid.Guards.First();
            var screen=camera.WorldToScreenPoint(grid.CellCenter(destination));Assert.IsFalse(camera.pixelRect.Contains(screen));
            Assert.IsTrue(summoning.BeginRelocation(guard,true));Assert.IsFalse(follow.enabled);
            var viewport=camera.pixelRect;Pointer(new Vector2(viewport.xMax-2,viewport.yMax-2));yield return new WaitForSeconds(1.1f);
            Assert.Greater(camera.transform.position.x,original.x+3);Assert.Greater(camera.transform.position.y,original.y+3);Assert.IsTrue(summoning.IsRelocating);
            screen=camera.WorldToScreenPoint(grid.CellCenter(destination));Assert.IsTrue(camera.pixelRect.Contains(screen));
            Pointer(screen);yield return null;yield return null;Pointer(screen,false);yield return null;yield return null;
            Assert.IsFalse(summoning.IsRelocating);Assert.AreSame(grid.Construction.RoomAt(destination),guard.AssignedRoom);
            Assert.Less(Vector3.Distance(original,camera.transform.position), .001f);Assert.AreEqual(8,camera.orthographicSize);Assert.IsTrue(follow.enabled);
        }
        [UnityTest]public IEnumerator CenterAndOutsideViewportStopPanningAndCancelRestoresFollow()
        {
            var original=camera.transform.position;Assert.IsTrue(summoning.BeginRelocation(grid.Guards.First(),true));
            var viewport=camera.pixelRect;Pointer(new Vector2(viewport.xMax-1,viewport.center.y));yield return new WaitForSeconds(.25f);Assert.Greater(camera.transform.position.x,original.x);
            Pointer(viewport.center);yield return null;yield return null;var stopped=camera.transform.position;yield return new WaitForSeconds(.15f);Assert.AreEqual(stopped,camera.transform.position);
            Pointer(new Vector2(viewport.xMax+20,viewport.yMax+20));yield return null;yield return null;Assert.AreEqual(stopped,camera.transform.position);
            summoning.SetOpen(false);Assert.AreEqual(original,camera.transform.position);Assert.IsTrue(follow.enabled);Assert.IsFalse(camera.GetComponent<PlacementCameraPan>().IsActive);
        }
        [Test]public void BoundsPauseAndOriginallyDisabledFollowArePreserved()
        {
            follow.enabled=false;var original=camera.transform.position;Assert.IsTrue(summoning.BeginRelocation(grid.Guards.First()));var pan=camera.GetComponent<PlacementCameraPan>();var viewport=camera.pixelRect;
            Time.timeScale=0;pan.Pan(new Vector2(viewport.xMax-1,viewport.yMax-1),.05f);Assert.AreEqual(original,camera.transform.position);Time.timeScale=1;
            Time.timeScale=0;pan.Pan(null,Vector2.one,.05f);Assert.AreEqual(original,camera.transform.position);Time.timeScale=1;
            for(int i=0;i<1000;i++)pan.Pan(new Vector2(viewport.xMax-1,viewport.yMax-1),.05f);
            var max=grid.CellCenter(new Vector2Int(grid.Layout.Width-1,grid.Layout.Height-1));Assert.LessOrEqual(camera.transform.position.x,max.x);Assert.LessOrEqual(camera.transform.position.y,max.y);
            pan.Pan(null,Vector2.one,.05f);Assert.LessOrEqual(camera.transform.position.x,max.x);Assert.LessOrEqual(camera.transform.position.y,max.y);
            summoning.SetOpen(false);Assert.IsFalse(follow.enabled);Assert.AreEqual(original,camera.transform.position);
        }
        [UnityTest]public IEnumerator DisablingSummoningRestoresCameraAndZoom()
        {
            var original=camera.transform.position;Assert.IsTrue(summoning.BeginRelocation(grid.Guards.First()));var pan=camera.GetComponent<PlacementCameraPan>();pan.Pan(new Vector2(camera.pixelRect.xMax-1,camera.pixelRect.center.y),.05f);
            summoning.enabled=false;Assert.AreEqual(original,camera.transform.position);yield return null;
            Assert.Less(Vector3.Distance(original,camera.transform.position),.02f,"Restored follow may reflect tiny physics corrections to the lord");Assert.AreEqual(8,camera.orthographicSize);Assert.IsTrue(follow.enabled);Assert.IsFalse(pan.IsActive);
        }
    }
}
