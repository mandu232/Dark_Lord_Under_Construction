using System;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Persistence;
using DarkLord.Player;
using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Editor
{
    public static class ResourceBuildingSetup
    {
        private static void Ref(UnityEngine.Object owner, string field, UnityEngine.Object value)
        { var data=new SerializedObject(owner);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo(); }
        private static Sprite RuinsSprite()
        {
            const string path="Assets/Art/UI/Construction/BuildingRuins.png";
            if(!System.IO.File.Exists(path))
            {
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
                var pixels=new Color32[1024];
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    bool block=(x>=3&&x<12&&y>=6&&y<11)||(x>=13&&x<21&&y>=4&&y<9)||(x>=22&&x<29&&y>=7&&y<13)||(x>=9&&x<15&&y>=11&&y<17)||(x>=18&&x<23&&y>=10&&y<19);
                    if(block)pixels[y*32+x]=(x+y)%4==0?new Color32(132,118,128,255):new Color32(80,73,88,255);
                }
                texture.SetPixels32(pixels);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static GameObject Prefab(string key,string title,CurrencyType currency,int yield,int cost,int repair,Sprite icon,Color color,Sprite ruins)
        {
            string path="Assets/Prefabs/Dungeon/"+key+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;
            var root=new GameObject(key);var health=root.AddComponent<ActorHealth>();health.Configure(Faction.Dungeon,180);
            root.AddComponent<CircleCollider2D>().radius=.45f;
            var intact=new GameObject("Intact");intact.transform.SetParent(root.transform,false);
            var body=intact.AddComponent<SpriteRenderer>();body.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Construction/RoomIcon.png");body.color=color;body.sortingOrder=-90;
            var resource=new GameObject("Resource Symbol");resource.transform.SetParent(intact.transform,false);resource.transform.localPosition=new Vector3(0,.125f,0);var symbol=resource.AddComponent<SpriteRenderer>();symbol.sprite=icon;symbol.sortingOrder=-89;
            var remains=new GameObject("Ruins");remains.transform.SetParent(root.transform,false);var rubble=remains.AddComponent<SpriteRenderer>();rubble.sprite=ruins;rubble.sortingOrder=-90;remains.SetActive(false);
            var building=root.AddComponent<ResourceBuilding>();
            var data=new SerializedObject(building);data.FindProperty("displayName").stringValue=title;data.FindProperty("resource").enumValueIndex=(int)currency;data.FindProperty("productionPerRound").intValue=yield;data.FindProperty("constructionCost").intValue=cost;data.FindProperty("repairCost").intValue=repair;data.ApplyModifiedPropertiesWithoutUndo();
            Ref(building,"intactVisual",intact);Ref(building,"ruinsVisual",remains);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        private static BuildItemDefinition Item(string key,string title,string description,Sprite icon,GameObject prefab,bool repair=false)
        {
            string path="Assets/Data/Construction/"+key+".asset";var item=AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(path);if(item!=null)return item;
            item=ScriptableObject.CreateInstance<BuildItemDefinition>();item.category=BuildCategory.Buildings;item.displayName=title;item.description=description;item.icon=icon;item.prefab=prefab;item.available=true;item.repairTool=repair;AssetDatabase.CreateAsset(item,path);return item;
        }
        private static void AppendItems(ConstructionPanel view,BuildItemDefinition[] additions)
        {
            var data=new SerializedObject(view);var list=data.FindProperty("items");
            foreach(var item in additions)
            {bool present=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==item)present=true;if(!present){int i=list.arraySize++;list.GetArrayElementAtIndex(i).objectReferenceValue=item;}}
            for(int i=list.arraySize-1;i>=0;i--) { var item=list.GetArrayElementAtIndex(i).objectReferenceValue as BuildItemDefinition; if(item!=null && item.repairTool) { list.GetArrayElementAtIndex(i).objectReferenceValue=null; list.DeleteArrayElementAtIndex(i); } }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before setup.");
            var icons=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/UI/Icons/ResourceIconSheet.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            icons=new[] {icons[1],icons[0],icons[2]}; // Sheet order: blood, gold, bloodstone.
            var ruins=RuinsSprite();var items=new BuildItemDefinition[3];
            string[] keys={"GoldMint","BloodAltar","BloodstoneExtractor"};string[] titles={"금화 주조소","혈액 제단","혈석 추출기"};string[] resources={"금화","혈액","혈석"};
            int[] yields={35,12,2},costs={80,90,120},repairs={40,45,60};Color[] colors={new Color32(175,129,52,255),new Color32(165,59,65,255),new Color32(124,68,172,255)};
            for(int i=0;i<3;i++)
            {
                var prefab=Prefab(keys[i],titles[i],(CurrencyType)i,yields[i],costs[i],repairs[i],icons[i],colors[i],ruins);
                items[i]=Item(keys[i],titles[i],$"설치: 금화 {costs[i]} · 수리: 금화 {repairs[i]}\n체력 180 · 라운드 클리어마다 {resources[i]} +{yields[i]}\n\n코어를 제외한 완성된 방에 1개씩 설치합니다. 방의 파수꾼이 모두 쓰러지면 용사가 건물을 공격합니다. 파괴되면 잔해로 남고 생산이 중단됩니다. 건물을 우클릭해 정보창에서 수리하세요.",icons[i],prefab);
            }
            string uiPath="Assets/Prefabs/UI/ConstructionPanel.prefab";
            var stage=PrefabStageUtility.GetCurrentPrefabStage();bool stageRoot=stage!=null && stage.assetPath==uiPath;
            var ui=stageRoot?stage.prefabContentsRoot:PrefabUtility.LoadPrefabContents(uiPath);
            try
            {
                var view=ui.GetComponent<ConstructionPanel>();var window=ui.transform.Find("Construction Window");var existing=window.Find("Buildings Tab");
                if(existing==null){var tab=UnityEngine.Object.Instantiate(window.Find("Entrances Tab").gameObject,window);tab.name="Buildings Tab";tab.GetComponentInChildren<TMP_Text>().text="생산 건물";tab.GetComponent<UnityEngine.UI.Button>().onClick=new UnityEngine.UI.Button.ButtonClickedEvent();existing=tab.transform;}
                Ref(view,"buildingsTab",existing.GetComponent<UnityEngine.UI.Button>());
                var tabs=new[]{"Rooms Tab","Minions Tab","Traps Tab","Entrances Tab","Buildings Tab"};
                for(int i=0;i<tabs.Length;i++){var rect=window.Find(tabs[i]).GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-68-i*40);rect.sizeDelta=new Vector2(rect.sizeDelta.x,36);}
                AppendItems(view,items);PrefabUtility.SaveAsPrefabAsset(ui,uiPath);
            }
            finally{if(!stageRoot)PrefabUtility.UnloadPrefabContents(ui);}
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var view in root.GetComponentsInChildren<ConstructionPanel>(true))AppendItems(view,items);
                foreach(var session in root.GetComponentsInChildren<GameSaveSession>(true))
                {
                    var catalog=AssetDatabase.FindAssets("t:BuildItemDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<BuildItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                    var data=new SerializedObject(session);var list=data.FindProperty("catalog");var ids=data.FindProperty("catalogIds");list.arraySize=ids.arraySize=catalog.Length;
                    for(int i=0;i<catalog.Length;i++){list.GetArrayElementAtIndex(i).objectReferenceValue=catalog[i];ids.GetArrayElementAtIndex(i).stringValue=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(catalog[i]));}data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Three production buildings and repair installed.";
        }
    }
}

