using UnityEngine;
using UnityEditor;
using TMPro;
public static class PendantPreview {
 public static string Main() {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
 try {
 var cg=new GameObject("Camera",typeof(Camera)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);
 var cam=cg.GetComponent<Camera>();cam.scene=scene;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.075f,.11f);cam.transform.position=new Vector3(0,0,-10);
 var go=new GameObject("Canvas",typeof(Canvas));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;canvas.pixelPerfect=true;
 var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/InGame/GameHud.prefab").transform.Find("Player Status");
 float[] ratios={1,.5f,.2f};for(int i=0;i<3;i++){var p=Object.Instantiate(source,go.transform);((RectTransform)p).anchoredPosition=new Vector2(20+i*320,-20);var fill=p.Find("Pendant Ruby").GetComponent<UnityEngine.UI.Image>();fill.fillAmount=ratios[i];p.Find("Health").GetComponent<TMP_Text>().text="체력  "+(int)(ratios[i]*100)+" / 100";}
 var rt=new RenderTexture(980,190,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(980,190,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,980,190),0,0);tex.Apply();System.IO.File.WriteAllBytes("output/health-pendant/health-states.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);return "output/health-pendant/health-states.png";
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
}
