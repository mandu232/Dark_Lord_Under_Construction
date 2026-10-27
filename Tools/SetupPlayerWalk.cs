using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupPlayerWalk
{
    const string Root="Assets/Animations/Player/";
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        const string path="Assets/Sprtie/Characters/Player/Walk.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.spritePixelsPerUnit=16; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s=>s.rect.y).ThenBy(s=>s.rect.x).ToArray();
        if(sprites.Length!=30 || sprites.Any(s=>s.rect.width!=64 || s.rect.height!=64)) throw new InvalidOperationException("Expected 5 rows x 6 frames, 64px.");
        string[] names={"Front","FrontDiagonal","Side","BackDiagonal","Back"};
        int[] rows={0,1,2,3,4}; // Front, front diagonal, side, back diagonal, back.
        var clips=new AnimationClip[5];
        for(int d=0;d<5;d++)
        {
            string clipPath=Root+"Walk"+names[d]+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(clip==null) {clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,clipPath);}
            clip.name="Walk"+names[d]; clip.frameRate=14;
            var keys=new ObjectReferenceKeyframe[6];
            for(int frame=0;frame<6;frame++) keys[frame]=new ObjectReferenceKeyframe {time=frame/14f,value=sprites[rows[d]*6+frame]};
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,settings);
            EditorUtility.SetDirty(clip); clips[d]=clip;
        }
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"PlayerDirectional.controller");
        var all=AssetDatabase.LoadAllAssetsAtPath(Root+"PlayerDirectional.controller");
        var walk=all.OfType<AnimatorState>().Single(s=>s.name=="Walk"); var tree=(BlendTree)walk.motion;
        if (!controller.parameters.Any(p=>p.name=="WalkRate")) controller.AddParameter("WalkRate",AnimatorControllerParameterType.Float);
        var parameters=controller.parameters; foreach(var parameter in parameters) if(parameter.name=="WalkRate") parameter.defaultFloat=1f; controller.parameters=parameters;
        walk.speedParameter="WalkRate"; walk.speedParameterActive=true; EditorUtility.SetDirty(walk);
        Vector2[] positions={Vector2.down,new Vector2(1,-1),Vector2.right,Vector2.one,Vector2.up};
        tree.children=Enumerable.Range(0,5).Select(i=>new ChildMotion {motion=clips[i],position=positions[i],timeScale=1}).ToArray();
        EditorUtility.SetDirty(tree);
        foreach(var state in all.OfType<AnimatorState>())
        {
            if(state.name=="Vp_Walk_Front") {state.name="Walk_Front"; state.motion=clips[0]; EditorUtility.SetDirty(state);}
            if(state.name=="Vp_Walk_FrontDiagonal") {state.name="Walk_FrontDiagonal"; state.motion=clips[1]; EditorUtility.SetDirty(state);}
        }
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        return "Walk configured: 5 directions x 6 frames, 14 fps, 16 PPU. Back row 4, BackDiagonal row 3; WalkRate follows player velocity.";
    }

    public static string RemoveLegacy()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        string[] roots={"Assets/Sprtie/Characters/Player/Vampire","Assets/Sprtie/Characters/Player/LegacyRed","Assets/Animations/Vampire","Assets/Animations/Player/Source"};
        var targets=AssetDatabase.GetAllAssetPaths().Where(p=>roots.Any(r=>p==r || p.StartsWith(r+"/",StringComparison.Ordinal))).ToArray();
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p) && !targets.Contains(p)))
        {
            var deps=AssetDatabase.GetDependencies(path,false);
            if(deps.Any(p=>targets.Contains(p))) throw new InvalidOperationException("Retained asset still references legacy art: "+path);
        }
        File.WriteAllLines("Temp/RemovedPlayerLegacyAssets.txt",targets);
        foreach(string root in roots)
        {
            string absolute=Path.GetFullPath(root); string workspace=Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar;
            if(!absolute.StartsWith(workspace,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Target outside Assets");
            if(AssetDatabase.IsValidFolder(root) && !AssetDatabase.DeleteAsset(root)) throw new InvalidOperationException("Failed to delete "+root);
        }
        AssetDatabase.SaveAssets();
        return "Removed "+targets.Count(p=>!string.IsNullOrEmpty(Path.GetExtension(p)))+" legacy assets after dependency checks; four legacy folders removed.";
    }
}
