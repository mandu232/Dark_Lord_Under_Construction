var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var t = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
var window = UnityEditor.EditorWindow.GetWindow(t);
t.GetProperty("selectedSizeIndex", flags).SetValue(window, int.Parse(System.IO.File.ReadAllText("tmp/dialogue-gameview-index.txt")));
return "Restored original Game view size selection";
