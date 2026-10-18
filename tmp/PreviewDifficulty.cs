var stage = new GameObject("Preview", typeof(Canvas));
var cameraObject = new GameObject("Preview Camera", typeof(Camera));
var rt = new RenderTexture(800,960,24);
var oldActive = RenderTexture.active;
try {
var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(14,10,18,255); camera.cullingMask = 1 << 31; camera.orthographic = true;
var canvas = stage.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
var root = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MainMenu/NewGameSetupPanel.prefab"), stage.transform); root.SetActive(true);
root.GetComponent<DarkLord.UI.SettingsReferenceLayout>().enabled = false;
var rootRect = (RectTransform)root.transform; rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one; rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
((RectTransform)root.transform.Find("Setup Card")).anchoredPosition = Vector2.zero;
root.GetComponent<DarkLord.UI.NewGameSetupPanel>().Begin(0);
foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
stage.layer = 31;
Canvas.ForceUpdateCanvases(); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
var texture = new Texture2D(800,960,TextureFormat.RGBA32,false); texture.ReadPixels(new Rect(0,0,800,960),0,0); texture.Apply();
System.IO.File.WriteAllBytes("tmp/difficulty-preview.png",texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
} finally { RenderTexture.active = oldActive; UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(rt); }
return "Rendered";


