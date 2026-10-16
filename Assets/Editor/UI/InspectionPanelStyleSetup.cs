using DarkLord.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace DarkLord.Editor
{
    public static class InspectionPanelStyleSetup
    {
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            if (existing == null) go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static Image Image(RectTransform rect, Color color)
        {
            var image = rect.GetComponent<Image>(); if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        private static void Font(TMP_Text text, float size, bool bold = false)
        {
            text.font = bold ? StardustFontSetup.Bold : StardustFontSetup.Regular; text.fontStyle = FontStyles.Normal;
            text.fontSize = size; text.enableAutoSizing = false; text.color = new Color32(250, 244, 236, 255);
            text.lineSpacing = 8; text.paragraphSpacing = 2; text.raycastTarget = false;
        }
        public static void Apply(Component panel)
        {
            var data = new SerializedObject(panel); var window = (GameObject)data.FindProperty("window").objectReferenceValue;
            window.GetComponent<RectTransform>().sizeDelta = new Vector2(448, 700);
            window.GetComponent<Image>().color = new Color32(20, 17, 25, 255);
            var title = (TMP_Text)data.FindProperty("titleText").objectReferenceValue; Font(title, 30, true);
            title.rectTransform.sizeDelta = new Vector2(360, 44);
            var stats = (TMP_Text)data.FindProperty("statsText").objectReferenceValue; Font(stats, 26);
            stats.rectTransform.anchoredPosition = new Vector2(24, -170); stats.rectTransform.sizeDelta = new Vector2(400, 370);
            Image(Rect(window.transform,"Title Divider",new Vector2(24,-67),new Vector2(400,2)),new Color32(85,69,91,255));
            var health = Rect(window.transform,"Health",new Vector2(24,-86),new Vector2(400,64));
            var bar = health.GetComponent<InspectionHealthBar>(); if(bar == null)bar = health.gameObject.AddComponent<InspectionHealthBar>();
            var numberRect = Rect(health,"Value",Vector2.zero,new Vector2(400,32));
            var number = numberRect.GetComponent<TextMeshProUGUI>(); if(number == null)number = numberRect.gameObject.AddComponent<TextMeshProUGUI>(); Font(number,26,true);number.text="체력  100 / 100";
            var track=Rect(health,"Track",new Vector2(0,-40),new Vector2(400,20));Image(track,new Color32(77,65,84,255));
            var inset=Rect(track,"Inset",new Vector2(2,-2),new Vector2(396,16));Image(inset,new Color32(35,29,41,255));
            var fillRect=Rect(inset,"Fill",Vector2.zero,new Vector2(396,16));fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=fillRect.offsetMax=Vector2.zero;
            var fill=Image(fillRect,new Color32(92,204,133,255));
            var healthData=new SerializedObject(bar);healthData.FindProperty("valueText").objectReferenceValue=number;healthData.FindProperty("fill").objectReferenceValue=fill;healthData.ApplyModifiedPropertiesWithoutUndo();
            data.FindProperty("healthBar").objectReferenceValue=bar;data.ApplyModifiedPropertiesWithoutUndo();
            foreach(var button in window.GetComponentsInChildren<Button>(true))
            {
                var label=button.GetComponentInChildren<TMP_Text>();if(label != null)Font(label,button.name=="Close"?24:26,true);
                if(button.name=="Relocate"||button.name=="Repair")button.GetComponent<RectTransform>().sizeDelta=new Vector2(400,52);
                if(button.name=="Aggressive"||button.name=="Defensive")
                {var br=button.GetComponent<RectTransform>();br.sizeDelta=new Vector2(194,44);br.anchoredPosition=new Vector2(button.name=="Aggressive"?-103:103,92);}
            }
        }
        public static void Run()
        {
            const string path="Assets/Prefabs/UI/InGame/BuildingInspectionPanel.prefab";
            var stage=PrefabStageUtility.GetCurrentPrefabStage();bool editing=stage!=null&&stage.assetPath==path;
            var root=editing?stage.prefabContentsRoot:PrefabUtility.LoadPrefabContents(path);
            try{Apply(root.GetComponent<BuildingInspectionPanel>());PrefabUtility.SaveAsPrefabAsset(root,path);}finally{if(!editing)PrefabUtility.UnloadPrefabContents(root);}
            foreach(var panel in Object.FindObjectsByType<MinionInspectionPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Apply(panel);EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);}
            foreach(var panel in Object.FindObjectsByType<BuildingInspectionPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Apply(panel);EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);EditorSceneManager.SaveScene(panel.gameObject.scene);}
            AssetDatabase.SaveAssets();
        }
    }
}
