var player=new UnityEngine.GameObject("Animation validation");
try {
 var source=player.AddComponent<UnityEngine.SpriteRenderer>(); var child=new UnityEngine.GameObject("Shadow");child.transform.SetParent(player.transform);var shadow=child.AddComponent<UnityEngine.SpriteRenderer>();var projection=child.AddComponent<DarkLord.Actors.SpriteProjectedShadow>();int count=0;
 foreach(var action in new[]{"Sheate","Unsheate","Dash","Roll"}) foreach(var dir in new[]{"Front","FrontDiagonal","Side","BackDiagonal","Back"}) {
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Animations/Player/"+action+dir+".anim"); if(clip==null)throw new System.Exception("Missing "+action+dir);
 var binding=UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(); var frames=UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip,binding); int expected=action=="Sheate"?2:action=="Unsheate"?3:action=="Dash"?4:10;if(frames.Length!=expected)throw new System.Exception("Frame count "+action+dir);
 foreach(var frame in frames){source.sprite=frame.value as UnityEngine.Sprite;if(source.sprite==null)throw new System.Exception("Missing sprite");projection.Refresh();if(shadow.sprite!=source.sprite)throw new System.Exception("Shadow mismatch "+action);count++;}
 }
 return "Verified "+count+" frames and shadow synchronization across 20 directional clips.";
} finally {UnityEngine.Object.DestroyImmediate(player);}
