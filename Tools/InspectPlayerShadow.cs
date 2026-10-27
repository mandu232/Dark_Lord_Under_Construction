var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
try {
var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DarkLord.Player.PlayerMovement>(true)).Single();
var source=player.GetComponent<UnityEngine.SpriteRenderer>(); var projection=player.transform.Find("Ground Shadow").GetComponent<DarkLord.Actors.SpriteProjectedShadow>(); var shadow=projection.GetComponent<UnityEngine.SpriteRenderer>();
if(UnityEditor.ShaderUtil.ShaderHasError(shadow.sharedMaterial.shader)) throw new System.Exception("Shader errors");
int count=0; foreach(var n in new[]{"Front","Front Side","Sidet","Back Side","Back"}) foreach(var sprite in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/Characters/Player/Idle Collection "+n+".png").OfType<UnityEngine.Sprite>()) {source.sprite=sprite; source.flipX=count%2==0; projection.Refresh(); if(shadow.sprite!=source.sprite || shadow.flipX!=source.flipX) throw new System.Exception("Sync failed"); count++;}
source.enabled=false; projection.Refresh(); if(shadow.enabled) throw new System.Exception("Visibility sync failed");
source.enabled=true; player.transform.position+=new UnityEngine.Vector3(2,1,0); projection.Refresh(); var props=new UnityEngine.MaterialPropertyBlock(); shadow.GetPropertyBlock(props); float expected=projection.transform.TransformPoint(new UnityEngine.Vector3(0,-.84375f,0)).y; if(UnityEngine.Mathf.Abs(props.GetFloat("_GroundWorldY")-expected)>.001f) throw new System.Exception("Foot anchor failed");
return "Verified "+count+" sprites incl. expressions, flips, visibility and moving world foot anchor. Shader compilation valid.";
} finally {UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
