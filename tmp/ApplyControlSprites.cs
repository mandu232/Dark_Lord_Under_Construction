if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Exit Play Mode before applying UI assets.");
const string art="Assets/Art/UI/Controls/";
var sprites=new System.Collections.Generic.Dictionary<string,UnityEngine.Sprite>();
foreach(var name in new[]{"CategoryTab","ActionSecondary","ActionPrimary","SaveCardFrame","SliderTrack","SliderHandle"}) {
 string path=art+name+".png";
 UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var imp=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
 imp.textureType=UnityEditor.TextureImporterType.Sprite;imp.spriteImportMode=UnityEditor.SpriteImportMode.Single;
 imp.spritePixelsPerUnit=100;imp.filterMode=UnityEngine.FilterMode.Point;imp.mipmapEnabled=false;
 imp.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;
 imp.npotScale=UnityEditor.TextureImporterNPOTScale.None;imp.SaveAndReimport();
 sprites[name]=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
}
System.Action<UnityEngine.Object> record=o=>{UnityEditor.EditorUtility.SetDirty(o);if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(o))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(o);};
System.Action<UnityEngine.UI.Image,string> skin=(image,name)=>{
 image.sprite=sprites[name];image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=false;image.color=UnityEngine.Color.white;record(image);
};
System.Action<UnityEngine.UI.Button,string> button=(b,name)=>{
 skin(b.image,name); b.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
 var c=b.colors;c.normalColor=UnityEngine.Color.white;c.highlightedColor=new UnityEngine.Color(1.2f,1.12f,1.12f,1);c.selectedColor=c.highlightedColor;
 c.pressedColor=new UnityEngine.Color(.72f,.65f,.7f,1);c.disabledColor=new UnityEngine.Color(.35f,.32f,.38f,.65f);c.fadeDuration=.08f;b.colors=c;record(b);
};
System.Action<UnityEngine.GameObject> settings=root=>{
 foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
 string key=b.name.StartsWith("Category ")||b.name.EndsWith(" Value")?"CategoryTab":b.name=="Apply"?"ActionPrimary":"ActionSecondary";
 button(b,key);
 if(b.name=="Defaults"){b.image.color=new UnityEngine.Color(.8f,.78f,.83f,1);record(b.image);}
 }
 foreach(var s in root.GetComponentsInChildren<UnityEngine.UI.Slider>(true)) {
 var track=s.transform.Find("Track").GetComponent<UnityEngine.UI.Image>(); skin(track,"SliderTrack");track.raycastTarget=false;
 track.rectTransform.anchoredPosition=new UnityEngine.Vector2(0,-9);track.rectTransform.sizeDelta=new UnityEngine.Vector2(930,26);record(track);record(track.rectTransform);
 var fillArea=(UnityEngine.RectTransform)s.fillRect.parent;
 fillArea.anchoredPosition=new UnityEngine.Vector2(16,-16);fillArea.sizeDelta=new UnityEngine.Vector2(898,12);record(fillArea);
 var fill=s.fillRect.GetComponent<UnityEngine.UI.Image>();fill.sprite=null;fill.color=new UnityEngine.Color32(154,38,62,255);fill.raycastTarget=false;record(fill);
 var handleArea=(UnityEngine.RectTransform)s.handleRect.parent;handleArea.anchoredPosition=new UnityEngine.Vector2(16,0);handleArea.sizeDelta=new UnityEngine.Vector2(898,44);record(handleArea);
 var handle=s.handleRect.GetComponent<UnityEngine.UI.Image>();skin(handle,"SliderHandle");handle.preserveAspect=true;
 s.handleRect.sizeDelta=new UnityEngine.Vector2(42,2);record(s.handleRect);record(handle);
 s.targetGraphic=handle;var c=s.colors;c.normalColor=UnityEngine.Color.white;c.highlightedColor=new UnityEngine.Color(1.25f,1.1f,1.1f,1);c.selectedColor=c.highlightedColor;c.pressedColor=new UnityEngine.Color(.8f,.7f,.75f,1);s.colors=c;record(s);
 }
};
System.Action<UnityEngine.GameObject> slots=root=>{
 foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
 if(!b.name.StartsWith("Slot ")){button(b,b.name=="Confirm"?"ActionPrimary":"ActionSecondary");continue;}
 foreach(UnityEngine.Transform child in b.transform)if(child.name.StartsWith("Archive Edge ")){child.gameObject.SetActive(false);record(child.gameObject);}
 var old=b.transform.Find("Card Frame");
 var frame=old!=null?old.GetComponent<UnityEngine.UI.Image>():new UnityEngine.GameObject("Card Frame",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
 if(old==null)frame.transform.SetParent(b.transform,false);
 frame.transform.SetAsFirstSibling();var r=frame.rectTransform;r.anchorMin=UnityEngine.Vector2.zero;r.anchorMax=UnityEngine.Vector2.one;r.offsetMin=r.offsetMax=UnityEngine.Vector2.zero;
 skin(frame,"SaveCardFrame");frame.color=new UnityEngine.Color(.85f,.76f,.8f,1);frame.raycastTarget=false;record(frame);record(r);
 var original=b.GetComponent<UnityEngine.UI.Image>();original.sprite=null;original.color=UnityEngine.Color.clear;original.raycastTarget=true;record(original);
 var outline=b.GetComponent<UnityEngine.UI.Outline>();if(outline!=null){outline.enabled=false;record(outline);}
 var fillTransform=b.transform.Find("Card Selection Fill");
 var surface=fillTransform!=null?fillTransform.GetComponent<UnityEngine.UI.Image>():new UnityEngine.GameObject("Card Selection Fill",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
 if(fillTransform==null)surface.transform.SetParent(b.transform,false);
 surface.transform.SetAsFirstSibling();var sr=surface.rectTransform;sr.anchorMin=UnityEngine.Vector2.zero;sr.anchorMax=UnityEngine.Vector2.one;sr.offsetMin=new UnityEngine.Vector2(18,35);sr.offsetMax=new UnityEngine.Vector2(-18,-35);
 surface.color=UnityEngine.Color.white;surface.raycastTarget=false;b.targetGraphic=surface;record(surface);record(sr);
 var c=b.colors;c.normalColor=new UnityEngine.Color(.12f,.02f,.06f,.12f);c.highlightedColor=new UnityEngine.Color(.5f,.08f,.14f,.38f);c.selectedColor=c.highlightedColor;c.pressedColor=new UnityEngine.Color(.55f,.12f,.18f,.52f);c.disabledColor=new UnityEngine.Color(.04f,.02f,.05f,.28f);b.colors=c;record(b);
 }
};
foreach(var path in new[]{"Assets/Prefabs/UI/SettingsPanel.prefab","Assets/Prefabs/UI/SaveSlotPanel.prefab"}) {
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{if(root.GetComponent<DarkLord.UI.SettingsPanelView>()!=null)settings(root);else slots(root);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);}
 finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
foreach(var path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/SampleScene.unity"}) {
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 foreach(var root in scene.GetRootGameObjects()) {
 foreach(var view in root.GetComponentsInChildren<DarkLord.UI.SettingsPanelView>(true))settings(view.gameObject);
 foreach(var view in root.GetComponentsInChildren<DarkLord.UI.SaveSlotPanel>(true))slots(view.gameObject);
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
UnityEditor.AssetDatabase.SaveAssets();
return "Applied six control sprites to settings/save prefabs and MainMenu/SampleScene instances.";
