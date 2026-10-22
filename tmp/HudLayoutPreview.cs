using UnityEngine;
using UnityEditor;
using TMPro;
public static class HudLayoutPreview {
 public static string Main() {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
 try {
 var cg=new GameObject("Camera",typeof(Camera)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);
 var cam=cg.GetComponent<Camera>();cam.scene=scene;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.1f,.14f);cam.transform.position=new Vector3(0,0,-10);
 var go=new GameObject("Canvas",typeof(Canvas));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/InGame/GameHud.prefab");var source=prefab.transform.Find("Resources");var res=Object.Instantiate(source,go.transform);
 string[] names={"Gold","Souls","Crystals"};string[] values={"<size=75%>1,000\n<size=60%>/ 2,000</size></size>","<size=75%>200\n<size=60%>/ 400</size></size>","<size=75%>50\n<size=60%>/ 100</size></size>"};
 for(int i=0;i<3;i++)res.Find(names[i]+"/Amount").GetComponent<TMP_Text>().text=values[i];
 var rt=new RenderTexture(960,240,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();
 string report="";foreach(var text in res.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();report+=text.transform.parent.name+": lines="+text.textInfo.lineCount+", overflow="+text.isTextOverflowing+"; ";}
 cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(960,240,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,240),0,0);tex.Apply();System.IO.File.WriteAllBytes("Temp/hud-layout-preview.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);return report;
 } finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
}
