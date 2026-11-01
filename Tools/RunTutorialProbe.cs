using System.Threading.Tasks;
using UnityEngine;
public static class RunTutorialProbe {
 public static async Task<string> Run(string fixture) {
  if(!Application.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first.");
  var node=new GameObject("Tutorial regression probe");Object.DontDestroyOnLoad(node);
  try {return await node.AddComponent<DarkLord.Tests.TutorialRegressionProbe>().RunFixture(fixture);}
  finally {Object.Destroy(node);}
 }
}
