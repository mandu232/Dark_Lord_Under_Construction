using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Temporary: runs PlayMode tests named by Temp/claude_run.txt (a regex) and writes Temp/claude_result.txt. Delete after use.
[InitializeOnLoad]
public static class TempTestBridge
{
    private const string Trigger = "Temp/claude_run.txt", Output = "Temp/claude_result.txt";
    private static readonly TestRunnerApi Api;

    static TempTestBridge()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new Callbacks());
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (!File.Exists(Trigger) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        string filter = File.ReadAllText(Trigger).Trim();
        File.Delete(Trigger);
        Api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, groupNames = new[] { filter } }));
    }

    private sealed class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            var text = new StringBuilder($"passed {result.PassCount} failed {result.FailCount} skipped {result.SkipCount}\n");
            Collect(result, text);
            File.WriteAllText(Output, text.ToString());
        }
        private static void Collect(ITestResultAdaptor node, StringBuilder text)
        {
            if (!node.HasChildren)
            {
                if (node.TestStatus == TestStatus.Failed) text.Append($"FAIL {node.Test.FullName}\n  {node.Message}\n  {node.StackTrace}\n");
                return;
            }
            foreach (var child in node.Children) Collect(child, text);
        }
    }
}
