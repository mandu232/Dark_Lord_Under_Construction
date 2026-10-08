using System.Linq;
using DarkLord.UI;
using DarkLord.Persistence;
using DarkLord.Actors;
using DarkLord.Player;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DarkLord.Editor
{
    public static class SaveUiSetup
    {
        private static void Ref(Object obj,string field,Object value){var s=new SerializedObject(obj);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
        private static TMP_Text Text(string name,Transform parent,TMP_Text template,string text,Vector2 size,Vector2 pos,int font=30)
        {var r=Rect(name,parent,size,pos);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=template.font;t.fontSharedMaterial=template.fontSharedMaterial;t.fontSize=font;t.color=template.color;t.alignment=TextAlignmentOptions.Center;t.text=text;t.raycastTarget=false;return t;}
        private static UnityEngine.UI.Button Button(string name,Transform parent,UnityEngine.UI.Button template,string text,Vector2 size,Vector2 pos,UnityEngine.Events.UnityAction action)
        {var b=Object.Instantiate(template,parent);b.name=name;var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.GetComponentInChildren<TMP_Text>().text=text;return b;}
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
            var game=SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");if(!game.isLoaded)game=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Additive);
            var title=SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");if(!title.isLoaded)title=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Additive);
            GameMenu Menu(Scene s)=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameMenu>(true)).Single();
            var gm=Menu(game);var tm=Menu(title);var gs=new SerializedObject(gm);
            if(gs.FindProperty("slotPanel").objectReferenceValue!=null)throw new System.InvalidOperationException("Save UI already configured; edit its prefabs directly.");
            var pause=(GameObject)gs.FindProperty("pausePanel").objectReferenceValue;
            var quit=(GameObject)gs.FindProperty("quitPanel").objectReferenceValue;
            var template=pause.transform.Find("Pause Actions/Resume").GetComponent<UnityEngine.UI.Button>();
            var label=template.GetComponentInChildren<TMP_Text>();
            // New reusable slot panel; existing screens and their styling remain intact.
            SceneManager.SetActiveScene(game);
            var root=Rect("Save Slots",null,new Vector2(1050,860),Vector2.zero);
            var image=root.gameObject.AddComponent<UnityEngine.UI.Image>();var source=pause.GetComponent<UnityEngine.UI.Image>();
            EditorUtility.CopySerialized(source,image);image.raycastTarget=true;
            var view=root.gameObject.AddComponent<SaveSlotPanel>();
            Ref(view,"title",Text("Title",root,label,"저장 공간 선택",new Vector2(940,80),new Vector2(0,335),42));
            Ref(view,"message",Text("Message",root,label,"",new Vector2(940,66),new Vector2(0,-250),24));
            var slots=new UnityEngine.UI.Button[3];var labels=new TMP_Text[3];
            for(int i=0;i<3;i++)
            {
                UnityEngine.Events.UnityAction callback=i==0?(UnityEngine.Events.UnityAction)view.SelectFirst:i==1?view.SelectSecond:view.SelectThird;
                slots[i]=Button("Slot "+(i+1),root,template,"저장 공간 "+(i+1),new Vector2(920,130),new Vector2(0,190-i*146),callback);
                labels[i]=slots[i].GetComponentInChildren<TMP_Text>();labels[i].fontSize=28;labels[i].alignment=TextAlignmentOptions.Center;
            }
            var sv=new SerializedObject(view);var sb=sv.FindProperty("slots");var sl=sv.FindProperty("slotLabels");sb.arraySize=sl.arraySize=3;
            for(int i=0;i<3;i++){sb.GetArrayElementAtIndex(i).objectReferenceValue=slots[i];sl.GetArrayElementAtIndex(i).objectReferenceValue=labels[i];}sv.ApplyModifiedPropertiesWithoutUndo();
            Button("Cancel",root,template,"취소",new Vector2(420,70),new Vector2(0,-345),view.Cancel);
            var overlay=Rect("Overwrite Confirmation",root,new Vector2(1000,530),Vector2.zero);var oi=overlay.gameObject.AddComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(source,oi);oi.color=new Color(.12f,.09f,.16f,1);
            Ref(view,"overwritePanel",overlay.gameObject);
            Ref(view,"overwriteDescription",Text("Description",overlay,label,"기존 기록을 덮어쓸까요?",new Vector2(900,220),new Vector2(0,60),32));
            Button("Confirm",overlay,template,"계속",new Vector2(380,76),new Vector2(210,-155),view.ConfirmOverwrite);
            Button("Cancel",overlay,template,"취소",new Vector2(380,76),new Vector2(-210,-155),view.CancelOverwrite);
            overlay.gameObject.SetActive(false);root.gameObject.SetActive(false);
            var asset=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/UI/SaveSlotPanel.prefab");Object.DestroyImmediate(root.gameObject);
            foreach(var menu in new[]{gm,tm}){var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,menu.transform);Ref(menu,"slotPanel",instance.GetComponent<SaveSlotPanel>());}
            // Edit pause prefab through PrefabUtility, retaining its existing layout and references.
            var contents=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/PausePanel.prefab");
            try
            {
                var actions=contents.transform.Find("Pause Actions");var resume=actions.Find("Resume").GetComponent<UnityEngine.UI.Button>();var bridge=contents.GetComponent<PausePanelBindings>();
                if(actions.Find("Save")==null){var b=Button("Save",actions,resume,"저장",Vector2.zero,Vector2.zero,bridge.SaveGame);b.transform.SetSiblingIndex(1);}
                ((RectTransform)contents.transform).sizeDelta=new Vector2(710,780);((RectTransform)actions).sizeDelta=new Vector2(598,410);
                ((RectTransform)contents.transform.Find("Hint")).anchoredPosition=new Vector2(56,-712);
                if(contents.transform.Find("Save Status")==null)Text("Save Status",contents.transform,label,"",new Vector2(598,44),new Vector2(0,-296),24);
                PrefabUtility.SaveAsPrefabAsset(contents,"Assets/Prefabs/UI/PausePanel.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            Ref(gm,"saveStatus",pause.transform.Find("Save Status").GetComponent<TMP_Text>());
            var qa=quit.transform.Find("Quit Actions");var keep=qa.Find("Keep Playing").GetComponent<UnityEngine.UI.Button>();
            var confirm=qa.Find("Confirm Quit").GetComponent<UnityEngine.UI.Button>();
            ((RectTransform)quit.transform).sizeDelta=new Vector2(1000,510);((RectTransform)qa).sizeDelta=new Vector2(888,150);
            var se=Button("Save And Exit",qa,keep,"저장 후 나가기",new Vector2(888,64),new Vector2(444,-32),gm.SaveAndExit);((RectTransform)se.transform).anchorMin=((RectTransform)se.transform).anchorMax=new Vector2(0,1);((RectTransform)se.transform).anchoredPosition=new Vector2(444,-32);
            ((RectTransform)keep.transform).sizeDelta=new Vector2(436,64);((RectTransform)keep.transform).anchoredPosition=new Vector2(218,-112);
            ((RectTransform)confirm.transform).sizeDelta=new Vector2(436,64);((RectTransform)confirm.transform).anchoredPosition=new Vector2(670,-112);
            keep.GetComponentInChildren<TMP_Text>().text="취소";confirm.GetComponentInChildren<TMP_Text>().text="저장 없이 나가기";
            var session=gm.GetComponent<GameSaveSession>()??gm.gameObject.AddComponent<GameSaveSession>();
            T Find<T>() where T:Component=>game.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).First();
            var waves=Find<WaveDirector>();var wd=new SerializedObject(waves);
            Ref(session,"dungeon",Find<DungeonGrid>());Ref(session,"player",wd.FindProperty("dungeonLord").objectReferenceValue);Ref(session,"core",wd.FindProperty("dungeonCore").objectReferenceValue);
            Ref(session,"wallet",Find<CurrencyWallet>());Ref(session,"summoning",Find<MinionSummoning>());Ref(session,"waves",waves);Ref(gm,"saveSession",session);
            var catalog=AssetDatabase.FindAssets("t:BuildItemDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var ss=new SerializedObject(session);var cp=ss.FindProperty("catalog");cp.arraySize=catalog.Length;for(int i=0;i<catalog.Length;i++)cp.GetArrayElementAtIndex(i).objectReferenceValue=catalog[i];var ids=ss.FindProperty("catalogIds");ids.arraySize=catalog.Length;for(int i=0;i<catalog.Length;i++)ids.GetArrayElementAtIndex(i).stringValue=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(catalog[i]));ss.ApplyModifiedPropertiesWithoutUndo();
            var ts=new SerializedObject(tm);var start=(UnityEngine.UI.Button)ts.FindProperty("startButton").objectReferenceValue;
            var cb=Button("Continue",start.transform.parent,start,"이어하기",((RectTransform)start.transform).sizeDelta,new Vector2(0,-180),tm.ContinueGame);cb.transform.SetSiblingIndex(1);Ref(tm,"continueButton",cb);
            start.GetComponentInChildren<TMP_Text>().text="새 게임";
            EditorSceneManager.MarkSceneDirty(game);EditorSceneManager.MarkSceneDirty(title);EditorSceneManager.SaveScene(game);EditorSceneManager.SaveScene(title);AssetDatabase.SaveAssets();
            SceneManager.SetActiveScene(game);return "Save UI wired in both scenes. Catalog: "+catalog.Length;
        }
    }
}



