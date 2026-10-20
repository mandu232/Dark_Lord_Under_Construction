using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DarkLord.UI;
using DarkLord.Dungeon;
public static class ConstructionDetailsPreview {
 public static string Main() {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
 var camGo=new GameObject("Preview Camera",typeof(Camera)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo,scene);
 var camera=camGo.GetComponent<Camera>(); camera.scene=scene; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.10f,.14f); camera.orthographic=true; camera.transform.position=new Vector3(0,0,-10);
 var canvasGo=new GameObject("Preview Canvas",typeof(Canvas)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo,scene);
 var canvas=canvasGo.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
 var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/InGame/ConstructionPanel.prefab"),canvas.transform);
 var rootRect=(RectTransform)root.transform; rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;
 root.SetActive(true); foreach(var g in root.GetComponentsInChildren<CanvasGroup>(true)) g.alpha=1; var window=root.transform.Find("Construction Window"); window.gameObject.SetActive(true);
 root.transform.Find("Open Construction").gameObject.SetActive(false);
 var data=new SerializedObject(root.GetComponent<ConstructionPanel>());
 var template=(BuildItemButton)data.FindProperty("itemTemplate").objectReferenceValue;
 var items=data.FindProperty("items");
 for(int i=0;i<items.arraySize;i++) {var item=(BuildItemDefinition)items.GetArrayElementAtIndex(i).objectReferenceValue; if(item.category!=BuildCategory.Buildings||item.repairTool)continue; var card=Object.Instantiate(template,template.transform.parent);card.gameObject.SetActive(true);card.Configure(root.GetComponent<ConstructionPanel>(),item);card.SetSelected(item.name=="WarBanner");if(item.name=="WarBanner"){var d=window.Find("Details");d.gameObject.SetActive(true); ((TMP_Text)data.FindProperty("detailTitle").objectReferenceValue).text=item.displayName;((TMP_Text)data.FindProperty("detailText").objectReferenceValue).text=ConstructionPanel.DescribeItem(item);var icon=(Image)data.FindProperty("detailIcon").objectReferenceValue;icon.sprite=item.icon;icon.enabled=false;}}
 window.Find("Details Placeholder").gameObject.SetActive(false);
 var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();
 camera.Render();RenderTexture.active=target;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes("Temp/construction-details-preview.png",tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(target);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);return "Temp/construction-details-preview.png";
 }
}

