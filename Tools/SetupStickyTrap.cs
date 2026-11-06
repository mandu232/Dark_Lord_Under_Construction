if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Exit play mode first.");
const string path="Assets/Prefabs/Dungeon/StickyTrap.prefab";
const string iconPath="Assets/Sprtie/UI/Construction/StickyTrap.png";
var texture=new UnityEngine.Texture2D(32,32,UnityEngine.TextureFormat.RGBA32,false);
var pixels=new UnityEngine.Color[1024];
for(int y=0;y<32;y++) for(int x=0;x<32;x++) {
 float dx=(x-15.5f)/13f,dy=(y-15.5f)/10f;
 bool glue=dx*dx+dy*dy<1 || (x-24)*(x-24)+(y-8)*(y-8)<10 || (x-6)*(x-6)+(y-24)*(y-24)<6;
 pixels[y*32+x]=glue ? (y%7==0 && x%5<3 ? new UnityEngine.Color32(172,222,108,255) : new UnityEngine.Color32(78,124,51,255)) : UnityEngine.Color.clear;
}
texture.SetPixels(pixels); texture.Apply(); System.IO.File.WriteAllBytes(iconPath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
UnityEditor.AssetDatabase.ImportAsset(iconPath);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(iconPath);
importer.textureType=UnityEditor.TextureImporterType.Sprite; importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;
importer.spritePixelsPerUnit=16; importer.filterMode=UnityEngine.FilterMode.Point; importer.mipmapEnabled=false;
importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true; importer.SaveAndReimport();
var icon=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(iconPath);
if(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path)==null) UnityEditor.AssetDatabase.CopyAsset("Assets/Prefabs/Dungeon/SpikeTrap.prefab",path);
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try {
 root.name="StickyTrap"; root.GetComponent<UnityEngine.SpriteRenderer>().sprite=icon;
 var data=new UnityEditor.SerializedObject(root.GetComponent<DarkLord.Dungeon.SpikeTrap>());
 data.FindProperty("damage").intValue=0;data.FindProperty("cooldown").floatValue=2;
 data.FindProperty("slowMultiplier").floatValue=.75f;data.FindProperty("slowDuration").floatValue=.8f;
 data.ApplyModifiedPropertiesWithoutUndo(); UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
} finally {UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
const string itemPath="Assets/Data/Construction/StickyTrap.asset";
var item=UnityEditor.AssetDatabase.LoadAssetAtPath<DarkLord.Dungeon.BuildItemDefinition>(itemPath);
if(item==null){item=UnityEngine.ScriptableObject.CreateInstance<DarkLord.Dungeon.BuildItemDefinition>();UnityEditor.AssetDatabase.CreateAsset(item,itemPath);}
item.category=DarkLord.Dungeon.BuildCategory.Traps;item.displayName="끈끈이 함정";item.icon=icon;
item.prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);item.available=true;
item.description="공격속도: 초당 0.5회 (2초 간격)\n범위: 설치 칸의 1.6 × 1.6유닛\n피해 없음 · 이동속도 25% 감소 · 지속 0.8초\n\n발동 시 범위 안의 모든 용사에게 동시에 적용합니다. 같은 적의 콜라이더가 여러 개여도 한 번만 적용합니다. 둔화는 갱신되며 겹치면 가장 강한 효과가 적용됩니다. 아군에게는 적용되지 않습니다.\n\n빈 바닥에 설치합니다. 침입 중에는 마왕 근처, 적과 떨어진 위치에만 설치할 수 있습니다.";
UnityEditor.EditorUtility.SetDirty(item);
var spike=UnityEditor.AssetDatabase.LoadAssetAtPath<DarkLord.Dungeon.BuildItemDefinition>("Assets/Data/Construction/SpikeTrap.asset");
spike.description="공격속도: 초당 0.5회 (2초 간격)\n범위: 설치 칸의 1.6 × 1.6유닛\n물리 피해 8\n\n발동 시 범위 안의 모든 용사에게 동시에 피해를 줍니다. 함정마다 하나의 발동 주기를 사용하며, 새로 들어온 적도 다음 발동을 기다립니다. 아군에게는 피해를 주지 않습니다.\n\n완성된 빈 바닥에 설치하세요.";
UnityEditor.EditorUtility.SetDirty(spike);
const string uiPath="Assets/Prefabs/UI/InGame/ConstructionPanel.prefab";
var ui=UnityEditor.PrefabUtility.LoadPrefabContents(uiPath);
try {
 var data=new UnityEditor.SerializedObject(ui.GetComponent<DarkLord.UI.ConstructionPanel>());var list=data.FindProperty("items");bool exists=false;
 for(int i=0;i<list.arraySize;i++) if(list.GetArrayElementAtIndex(i).objectReferenceValue==item) exists=true;
 if(!exists)list.GetArrayElementAtIndex(list.arraySize++).objectReferenceValue=item;
 data.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(ui,uiPath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(ui);}
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
foreach(var sceneRoot in scene.GetRootGameObjects()) {
 foreach(var session in sceneRoot.GetComponentsInChildren<DarkLord.Persistence.GameSaveSession>(true)) {
  var data=new UnityEditor.SerializedObject(session);var list=data.FindProperty("catalog");var ids=data.FindProperty("catalogIds");bool exists=false;
  for(int i=0;i<list.arraySize;i++) if(list.GetArrayElementAtIndex(i).objectReferenceValue==item)exists=true;
  if(!exists){int i=list.arraySize++;ids.arraySize=list.arraySize;list.GetArrayElementAtIndex(i).objectReferenceValue=item;ids.GetArrayElementAtIndex(i).stringValue=UnityEditor.AssetDatabase.AssetPathToGUID(itemPath);}
  data.ApplyModifiedPropertiesWithoutUndo();
 }
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
UnityEditor.AssetDatabase.SaveAssets();return "Sticky trap authored and registered; descriptions updated.";
