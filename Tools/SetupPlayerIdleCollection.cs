using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupPlayerIdleCollection
{
    const string Root = "Assets/Animations/Player/";
    static readonly string[] Directions = { "Front", "FrontDiagonal", "Side", "BackDiagonal", "Back" };
    static readonly string[] Sheets = { "Front", "Front Side", "Sidet", "Back Side", "Back" };
    static readonly Vector2[] Positions = { Vector2.down, new Vector2(1,-1), Vector2.right, Vector2.one, Vector2.up };
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "PlayerDirectional.controller");
        if (controller == null) throw new InvalidOperationException("Missing player controller");
        var sprites = Sheets.Select(n => {
            string path = "Assets/Sprtie/Characters/Player/Idle Collection " + n + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 16;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s=>s.rect.y).ThenBy(s=>s.rect.x).ToArray();
        }).ToArray();
        var idleClips = Enumerable.Range(0,5).Select(i=>WriteClip("IdleDefault"+Directions[i],sprites[i],0,5,true,i)).ToArray();
        var all = AssetDatabase.LoadAllAssetsAtPath(Root+"PlayerDirectional.controller");
        var idle = all.OfType<AnimatorState>().Single(s=>s.name=="Idle_Default");
        var idleTree = (BlendTree)idle.motion;
        SetChildren(idleTree,idleClips);
        EditorUtility.SetDirty(idleTree);
        foreach (var state in all.OfType<AnimatorState>())
        {
            if (!(state.motion is BlendTree) && !state.name.StartsWith("Walk_", StringComparison.Ordinal)) state.motion = idleTree;
            EditorUtility.SetDirty(state);
        }
        var machine = controller.layers[0].stateMachine;
        string[] emotions = { "Down", "Crying", "Blinking", "Accept", "Decline" };
        int[] starts = { 5,9,14,16,20 }, counts = { 4,5,2,4,4 };
        for(int e=0;e<emotions.Length;e++)
        {
            var front = WriteClip("Expression_"+emotions[e]+"_Front",sprites[0],starts[e],counts[e],true,0);
            var side = WriteClip("Expression_"+emotions[e]+"_Side",sprites[2],starts[e],counts[e],true,2);
            string name = "Expression_"+emotions[e];
            var state = machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name) ?? machine.AddState(name);
            var tree = state.motion as BlendTree;
            if(tree==null) { tree=new BlendTree {name=name+" Directions"}; AssetDatabase.AddObjectToAsset(tree,controller); }
            // Only front/side expression art exists. Other facings deliberately use front.
            SetChildren(tree,new[]{front,front,side,front,front});
            state.motion=tree;
            EditorUtility.SetDirty(tree); EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
        bool opened = !scene.isLoaded;
        if(opened) scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            foreach(var animator in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Animator>(true)))
                if(animator.runtimeAnimatorController==controller ||
                    animator.runtimeAnimatorController is AnimatorOverrideController overrides && overrides.runtimeAnimatorController==controller)
                {
                    animator.runtimeAnimatorController=controller;
                    EditorUtility.SetDirty(animator);
                    var renderer=animator.GetComponent<SpriteRenderer>();
                    if(renderer!=null) {renderer.sprite=sprites[0][0]; EditorUtility.SetDirty(renderer);}
                }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        finally {if(opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();
        return "Updated all player controller motions to new directional Idle; added 5 story expression states and 10 clips.";
    }
    static void SetChildren(BlendTree tree, AnimationClip[] clips)
    {
        tree.blendType=BlendTreeType.SimpleDirectional2D; tree.blendParameter="FacingX"; tree.blendParameterY="FacingY";
        tree.useAutomaticThresholds=false;
        tree.children=Enumerable.Range(0,5).Select(i=>new ChildMotion { motion=clips[i],position=Positions[i],timeScale=1 }).ToArray();
    }
    static AnimationClip WriteClip(string name, Sprite[] sprites, int start, int count, bool loop, int direction)
    {
        string path=Root+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null) {clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,path);}
        clip.name=name; clip.frameRate=40;
        float time=0;
        var keys=new ObjectReferenceKeyframe[count+1];
        for(int i=0;i<count;i++)
        {
            keys[i]=new ObjectReferenceKeyframe {time=time,value=sprites[start+i]};
            bool longer=direction==0 && (start==14 || start==20);
            time+=longer ? .125f : .1f;
        }
        keys[count]=new ObjectReferenceKeyframe {time=time,value=sprites[start+count-1]};
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=loop;
        AnimationUtility.SetAnimationClipSettings(clip,settings); EditorUtility.SetDirty(clip); return clip;
    }
}
