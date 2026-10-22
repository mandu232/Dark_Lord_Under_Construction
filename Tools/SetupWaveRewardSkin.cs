var root = UnityEngine.Application.dataPath;
if (!root.EndsWith("Dark Lord Under Construction/Assets")) throw new System.InvalidOperationException("Unexpected project");
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources")) UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
var path = "Assets/Resources/WaveRewardSkin.asset";
var skin = UnityEditor.AssetDatabase.LoadAssetAtPath<DarkLord.UI.RewardUiSkin>(path);
if (skin == null) { skin = UnityEngine.ScriptableObject.CreateInstance<DarkLord.UI.RewardUiSkin>(); UnityEditor.AssetDatabase.CreateAsset(skin, path); }
System.Func<string,UnityEngine.Sprite> sprite = p => UnityEditor.AssetDatabase.LoadAllAssetsAtPath(p).OfType<UnityEngine.Sprite>().First();
skin.cardFrame = sprite("Assets/Sprtie/UI/Frame/SaveCardFrame.png");
skin.button = sprite("Assets/Sprtie/UI/Button/CastleButton.png");
var resources = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprtie/UI/Icons/ResourceIconSheet.png").OfType<UnityEngine.Sprite>().OrderBy(s => s.name).ToArray();
var ruby = sprite("Assets/Sprtie/UI/InGame/PendantHealth/PendantRuby.png");
var guard = sprite("Assets/Sprtie/UI/Construction/GuardIcon.png");
var sticky = sprite("Assets/Sprtie/UI/Construction/StickyTrap.png");
var spike = sprite("Assets/Sprtie/UI/Construction/SpikeTrapIcon.png");
skin.icons = new[] {resources[1], resources[0], resources[2], ruby, guard, sticky, ruby, guard, spike, ruby, resources[0]};
UnityEditor.EditorUtility.SetDirty(skin); UnityEditor.AssetDatabase.SaveAssets();
return "Reward skin references original UI card, button and icon sprites.";
