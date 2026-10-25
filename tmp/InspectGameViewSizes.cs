var assembly = typeof(UnityEditor.Editor).Assembly;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var viewType = assembly.GetType("UnityEditor.GameView");
var view = UnityEditor.EditorWindow.GetWindow(viewType);
var output = new System.Text.StringBuilder();
foreach (var name in new[] { "UnityEditor.GameView", "UnityEditor.GameViewSizes", "UnityEditor.GameViewSize", "UnityEditor.GameViewSizeGroup" }) {
 var t = assembly.GetType(name); output.AppendLine(name);
 foreach(var m in t.GetMethods(flags).Where(m => m.Name.Contains("Size") || m.Name.Contains("Group") || m.Name.Contains("Resolution") || m.Name == "get_instance")) output.AppendLine(m.ToString());
 foreach(var c in t.GetConstructors(flags)) output.AppendLine(c.ToString());
}
return output.ToString();
