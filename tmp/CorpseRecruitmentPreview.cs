var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();
var grid=UnityEngine.Object.FindFirstObjectByType<DarkLord.Dungeon.DungeonGrid>();
var station=grid.RecruitmentFacilities.Single();
waves.Harvest.DropCarried();
waves.Lord.transform.position=station.transform.position+UnityEngine.Vector3.down*2;
waves.Lord.GetComponent<UnityEngine.Rigidbody2D>().position=waves.Lord.transform.position;
return "Ready to open recruitment";
