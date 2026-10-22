var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Enemies/WaveInvader.prefab");
var clip=prefab.GetComponent<UnityEngine.Animator>().runtimeAnimatorController.animationClips.First(c=>c.name.EndsWith("Die"));
var sample=new UnityEngine.GameObject("Sample death");var renderer=sample.AddComponent<UnityEngine.SpriteRenderer>();var animator=sample.AddComponent<UnityEngine.Animator>();animator.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;
var graph=UnityEngine.Playables.PlayableGraph.Create();var playable=UnityEngine.Animations.AnimationClipPlayable.Create(graph,clip);var output=UnityEngine.Animations.AnimationPlayableOutput.Create(graph,"Death pose",animator);UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(output,playable);UnityEngine.Playables.PlayableExtensions.SetTime(playable,.999);graph.Evaluate(0);
var result=renderer.sprite?.name;graph.Destroy();UnityEngine.Object.Destroy(sample);return result;

