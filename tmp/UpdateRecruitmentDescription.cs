var item = UnityEditor.AssetDatabase.LoadAssetAtPath<DarkLord.Dungeon.BuildItemDefinition>("Assets/Data/Construction/MinionRecruitment.asset");
var building = item.prefab.GetComponent<DarkLord.Dungeon.ResourceBuilding>();
item.description = "설치: 금화 100 · 수리: 금화 50 · 체력 180\n" + building.EffectDescription + "\n완성된 방의 빈 바닥에 설치합니다. 파괴되면 모집이 중단되며 수리하면 복구됩니다.";
UnityEditor.EditorUtility.SetDirty(item);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
return item.description;

