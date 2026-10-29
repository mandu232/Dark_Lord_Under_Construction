        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        const string root="Assets/Animations/Player/", sheet="Assets/Sprite/Characters/Player/Dash/Dash.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(sheet);
        if(importer==null) throw new InvalidOperationException("Missing Dash sprite sheet");
        importer.spritePixelsPerUnit=16; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderByDescending(s=>s.rect.y).ThenBy(s=>s.rect.x).ToArray();
        if(sprites.Length!=20 || sprites.Any(s=>s.rect.width!=64 || s.rect.height!=64)) throw new InvalidOperationException("Expected 5 x 6 sprites sized 64x64.");
        var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(root+"PlayerDirectional.controller");
        if(controller==null) throw new InvalidOperationException("Missing player controller");
        string[] directions={"Front","FrontDiagonal","Side","BackDiagonal","Back"};
        Vector2[] positions={Vector2.down,new Vector2(1,-1),Vector2.right,Vector2.one,Vector2.up};
        var clips=new AnimationClip[5];
        for(int row=0;row<5;row++)
        {
            string path=root+"Dash"+directions[row]+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null) {clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.name="Dash"+directions[row]; clip.frameRate=56;
            var frames=new ObjectReferenceKeyframe[4];
            for(int i=0;i<4;i++)frames[i]=new ObjectReferenceKeyframe {time=i/56f,value=sprites[row*4+i]};
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),frames);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
            EditorUtility.SetDirty(clip);clips[row]=clip;
        }
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Dash") ?? machine.AddState("Dash");
        var tree=state.motion as UnityEditor.Animations.BlendTree;
        if(tree==null || tree.name!="Dash Directions"){tree=new UnityEditor.Animations.BlendTree{name="Dash Directions"};AssetDatabase.AddObjectToAsset(tree,controller);}
        tree.blendType=UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;tree.blendParameter="FacingX";tree.blendParameterY="FacingY";tree.useAutomaticThresholds=false;
        tree.children=Enumerable.Range(0,5).Select(i=>new UnityEditor.Animations.ChildMotion{motion=clips[i],position=positions[i],timeScale=1}).ToArray();
        state.motion=tree;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        return "Dash: five directions, 56fps, one shot.";

