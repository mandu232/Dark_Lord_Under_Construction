var waves = UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
var grid = UnityEngine.Object.FindFirstObjectByType<DarkLord.Dungeon.DungeonGrid>();
var session = UnityEngine.Object.FindFirstObjectByType<DarkLord.Persistence.GameSaveSession>();
var summoning = UnityEngine.Object.FindFirstObjectByType<DarkLord.Player.MinionSummoning>();
var saved = session.Capture();
var source = grid.Layout.Core + new UnityEngine.Vector2Int(-9,4);
var destination = grid.Layout.Core + new UnityEngine.Vector2Int(9,7);
foreach (var center in new[] { source, destination })
{
    var bounds = DarkLord.Dungeon.DungeonConstruction.Footprint(DarkLord.Dungeon.ConstructionKind.Room, center);
    var cells = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
    foreach (var cell in bounds.allPositionsWithin) { cells.Add(cell); saved.dungeon.cells[cell.y*saved.dungeon.width+cell.x] = (int)DarkLord.Dungeon.DungeonCell.Floor; }
    saved.dungeon.sections = saved.dungeon.sections.Concat(new[] {new DarkLord.Persistence.SectionSaveData {kind=DarkLord.Dungeon.ConstructionKind.Room,bounds=bounds,cells=cells.ToArray(),complete=true,initial=true}}).ToArray();
}
session.Restore(saved);
var item=UnityEngine.Resources.FindObjectsOfTypeAll<DarkLord.Dungeon.BuildItemDefinition>().First(i=>i.name=="MinionRecruitment");
grid.Inventory.Grant(item); grid.TryPlaceBuildingItem(source,item);
waves.Lord.transform.position=grid.CellCenter(source+UnityEngine.Vector2Int.down); waves.Lord.GetComponent<UnityEngine.Rigidbody2D>().position=waves.Lord.transform.position;
UnityEngine.Physics2D.SyncTransforms();
summoning.TryRecruit(); summoning.TrySummon(DarkLord.Minions.MinionRole.BloodSlime);
summoning.TryRecruit(); summoning.TrySummon(DarkLord.Minions.MinionRole.BloodArcher);
var guard=grid.Guards.Last(); summoning.BeginRelocation(guard,true); summoning.TryPlace(destination);
var camera=UnityEngine.Camera.main;
camera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>().enabled=false;
camera.GetComponent<DarkLord.Player.CameraFollow>().enabled=false;
camera.transform.position=UnityEngine.Vector3.Lerp(grid.CellCenter(source),grid.CellCenter(destination),.5f)+UnityEngine.Vector3.back*10;
camera.orthographicSize=23; camera.ResetWorldToCameraMatrix(); camera.ResetProjectionMatrix();
return "Summoned both roles immediately; archer teleported to remote room.";
