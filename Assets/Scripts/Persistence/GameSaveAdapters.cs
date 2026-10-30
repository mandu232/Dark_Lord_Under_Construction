using System;
using System.Collections.Generic;
using System.Linq;
using DarkLord.Persistence;
using DarkLord.Actors;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Persistence
{
    public static class SaveSnapshots
    {
        public static MinionSaveData Actor(MinionController actor) => new MinionSaveData
        {
            position = actor.transform.position, post = actor.GuardPosition, order = actor.Order, behaviour = actor.Behaviour,
            health = actor.Health.CurrentHealth, maximumHealth = actor.Health.MaximumHealth,
            slows = actor.GetComponent<TopDownMotor>().CaptureSlows(),
            tactics = actor.GetComponent<InvaderTactics>()?.CaptureSave(),
            role = actor.Role, attackCooldown = actor.AttackCooldownRemaining, support = actor.GetComponent<MinionSupport>()?.CaptureSave(),
            invasionWave = actor.GetComponent<DarkLord.Gameplay.WaveMember>()?.Number ?? 0,
            augmentStatus = actor.GetComponent<AugmentStatus>()?.CaptureSave(), poise = actor.CapturePoise()
        };
    }
}
namespace DarkLord.Dungeon
{
    public sealed partial class DungeonLayout
    {
        internal void Restore(DungeonSaveData data)
        {
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) cells[x,y] = (DungeonCell)data.cells[y * Width + x];
            Entrance = data.entrance; HasEntrance = data.hasEntrance; ExpandedCells = data.expandedCells;
        }
    }
    public sealed partial class DungeonConstruction
    {
        internal void Restore(SectionSaveData[] data)
        {
            sections.Clear(); Array.Clear(owners, 0, owners.Length);
            foreach (var saved in data) Add(new DungeonSection(saved.kind, saved.bounds, saved.complete, saved.core, new List<Vector2Int>(saved.cells)));
            ReleaseCollapsedOpening();
            Revision++;
        }
    }
    public sealed partial class DungeonGrid
    {
        private readonly Dictionary<DungeonSection, GameObject> savedDesigns = new Dictionary<DungeonSection, GameObject>();
        private readonly Dictionary<Vector2Int, GameObject> savedTrapDesigns = new Dictionary<Vector2Int, GameObject>();
        public DungeonSaveData CaptureSave(Func<GameObject,string> identify)
        {
            Initialize();
            var data = new DungeonSaveData { width=Layout.Width, height=Layout.Height, expandedCells=Layout.ExpandedCells,
                entrance=Layout.Entrance, hasEntrance=Layout.HasEntrance, cells=new int[Layout.Width * Layout.Height], queue=constructionTiles.ToArray() };
            for(int y=0;y<Layout.Height;y++)for(int x=0;x<Layout.Width;x++)data.cells[y*Layout.Width+x]=(int)Layout.GetCell(new Vector2Int(x,y));
            data.sections=Construction.Sections.Select(s=>new SectionSaveData { kind=s.Kind, bounds=s.Bounds, core=s.IsCore, complete=s.IsComplete,
                initial=!savedDesigns.ContainsKey(s), designId=savedDesigns.TryGetValue(s,out var design)?identify(design):"", cells=s.Cells.ToArray() }).ToArray();
            data.traps=traps.Where(p=>p.Value!=null && p.Value.activeSelf).OrderBy(p=>p.Key.y).ThenBy(p=>p.Key.x)
                .Select(p=>new TrapSaveData { cell=p.Key, designId=identify(savedTrapDesigns[p.Key]),
                    cooldownRemaining=p.Value.GetComponent<SpikeTrap>()?.CooldownRemaining ?? 0 }).ToArray();
            data.buildings = CaptureBuildings(identify);
            data.alchemyJobs = CaptureAlchemyJobs();
            data.constructionItems = Inventory.Capture();
            data.lastConstructionRewardDay = LastConstructionRewardDay;
            return data;
        }
        public void RestoreSave(DungeonSaveData data, Func<string,GameObject> resolve)
        {
            if(Layout!=null)Layout.CellChanged-=OnCellChanged;
            if(generated!=null){generated.SetActive(false);Destroy(generated);}
            if(mesh!=null)Destroy(mesh);
            alchemyStations.Clear();
            width=data.width; height=data.height; Layout=new DungeonLayout(width,height); Layout.Restore(data);
            Construction=new DungeonConstruction(Layout); Construction.Restore(data.sections);
            highlighted=-1; areaHighlight.Clear(); miningMarks.Clear(); constructionTiles.Clear(); breakingCell=null;
            nextBreakAt=Time.time+tileBreakInterval; sectionPrefabs.Clear(); savedDesigns.Clear(); traps.Clear(); savedTrapDesigns.Clear(); entranceMarker=null;
            generated=new GameObject("Generated dungeon cells"); generated.layer=gameObject.layer; generated.transform.SetParent(transform,false);
            BuildPresentation(); Layout.CellChanged+=OnCellChanged;
            for(int i=0;i<data.sections.Length;i++)
            {
                var saved=data.sections[i]; var section=Construction.Sections[i];
                var prefab=saved.initial?InitialPrefab(section):resolve(saved.designId);
                if(!saved.initial)savedDesigns[section]=prefab;
                if(section.IsComplete && prefab!=null && !UsesArtTiles)
                {
                    if(saved.initial)
                    {
                        var instance=Instantiate(prefab,CellCenter(section.Center),transform.rotation,generated.transform);
                        instance.name="Initial "+prefab.name;
                        ScaleInitialDesign(instance, section);
                    }
                    else {sectionPrefabs[section]=prefab;OnCellChanged(section.Cells[0]);}
                }
                else if(!section.IsComplete)QueueConstruction(section,prefab);
            }
            Inventory.Restore(data.constructionItems);
            LastConstructionRewardDay = data.lastConstructionRewardDay;
            RestoreBuildings(data.buildings, resolve);
            RestoreAlchemyJobs(data.alchemyJobs);
            constructionTiles.Clear();constructionTiles.AddRange(data.queue);RefreshEntranceMarker();
            foreach(var trap in data.traps)
            {
                var prefab=resolve(trap.designId);
                traps[trap.cell]=Instantiate(prefab,CellCenter(trap.cell),transform.rotation,generated.transform);savedTrapDesigns[trap.cell]=prefab;
                traps[trap.cell].GetComponent<SpikeTrap>()?.RestoreCooldown(trap.cooldownRemaining);
            }
        }
    }
}
namespace DarkLord.Player
{
    public sealed partial class MinionSummoning
    {
        public MinionSaveData[] CaptureGuards() => guards.Where(g=>g!=null && g.IsAlive).Select(SaveSnapshots.Actor).ToArray();
        public void ClearSavedGuards()
        { foreach(var guard in guards.ToArray()) if(guard!=null)RemoveGuard(guard); }
        public void RestoreGuards(MinionSaveData[] saved)
        {
            foreach(var data in saved)
            {
                var definition = RecruitDefinitionFor(data.role);
                if (definition == null) throw new InvalidOperationException("저장된 하수인 병과를 찾을 수 없습니다.");
                var guard=Instantiate(definition,data.position,Quaternion.identity);guard.name=guard.DisplayName;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(guard.gameObject, gameObject.scene);
                guard.Health.RestoreHealth(data.maximumHealth,data.health);guard.Defend(data.post);guard.SetBehaviour(data.behaviour);
                guard.RestoreAttackCooldown(data.attackCooldown);
                guard.GetComponent<MinionSupport>()?.RestoreSave(data.support);
                guard.GetComponent<TopDownMotor>().RestoreSlows(data.slows);
                if (data.augmentStatus != null) AugmentStatus.Get(guard.Health).RestoreSave(data.augmentStatus);
                if(data.order==MinionOrder.Return)guard.ReturnToPost();
                guards.Add(guard);dungeon.RegisterGuard(guard);commands?.Register(guard);
            }
        }
    }
}
namespace DarkLord.Gameplay
{
    public sealed partial class WaveDirector
    {
        public WaveSaveData CaptureSave() => new WaveSaveData { number=CurrentWave, state=State, pending=pendingSpawns, rewards=(int[])rewardChoices.Clone(),
            rewardItemIds=rewardBuildItems.Select(i => i != null ? i.name : "").ToArray(),
            pendingRoles=pendingRoles.ToArray(),
            augments=Augments.Capture(), schedule=CaptureSchedule(),
            enemies=aliveInvaders.Where(e=>e!=null && e.IsAlive).Select(e=>SaveSnapshots.Actor(e.GetComponent<MinionController>())).ToArray() };
        public void RestoreSave(WaveSaveData data)
        {
            if (State == WaveState.RewardSelection) Time.timeScale = rewardTimeScale;
            if(spawnRoutine!=null)StopCoroutine(spawnRoutine);spawnRoutine=null;
            foreach(var pair in deathHandlers)if(pair.Key!=null)pair.Key.Died-=pair.Value;
            deathHandlers.Clear();
            foreach(var enemy in aliveInvaders)if(enemy!=null){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}
            aliveInvaders.Clear();CurrentWave=data.number;State=data.state;pendingSpawns=data.pending;
            Augments.Restore(data.augments);
            pendingRoles.Clear(); pendingWaves.Clear();
            RestoreSchedule(data.schedule);
            for (int i = 0; i < pendingSpawns; i++) pendingRoles.Enqueue(data.pendingRoles != null && i < data.pendingRoles.Length ? data.pendingRoles[i] : InvaderRole.Warrior);
            for (int i = 0; i < pendingSpawns; i++) pendingWaves.Enqueue(data.schedule != null && data.schedule.pendingWaves != null && i < data.schedule.pendingWaves.Length ? data.schedule.pendingWaves[i] : CurrentWave);
            if (State == WaveState.Preparing && CurrentWave >= finalWave && pendingSpawns == 0 && data.enemies.Length == 0 && waveRewardQueue.Count == 0) State = WaveState.Victorious;
            rewardChoices = data.rewards == null ? Array.Empty<int>() : (int[])data.rewards.Clone();
            rewardBuildItems = new DarkLord.Dungeon.BuildItemDefinition[rewardChoices.Length];
            if (data.rewardItemIds != null)
                for (int i = 0; i < rewardBuildItems.Length && i < data.rewardItemIds.Length; i++)
                    if (!string.IsNullOrEmpty(data.rewardItemIds[i])) rewardBuildItems[i] = dungeon.Inventory.Find(data.rewardItemIds[i]);
            foreach(var saved in data.enemies)
            {
                var enemy=Instantiate(invaderPrefab,saved.position,Quaternion.identity,transform);enemy.name=$"Wave {CurrentWave} Invader";
                enemy.gameObject.AddComponent<WaveMember>().Number = saved.invasionWave > 0 ? saved.invasionWave : CurrentWave;
                var health=enemy.Health;health.RestoreHealth(saved.maximumHealth,saved.health);enemy.Assault(dungeonCore,dungeon);
                enemy.RestorePoise(saved.poise);
                ConfigureInvader(enemy, saved.tactics != null ? saved.tactics.role : InvaderRole.Warrior).RestoreSave(saved.tactics);
                enemy.GetComponent<TopDownMotor>().RestoreSlows(saved.slows);
                if (saved.augmentStatus != null) AugmentStatus.Get(health).RestoreSave(saved.augmentStatus);
                Action handler=()=>OnInvaderDied(health);deathHandlers.Add(health,handler);health.Died+=handler;aliveInvaders.Add(health);
                if(State==WaveState.Defeated)enemy.enabled=false;
            }
            if((State==WaveState.Active || State==WaveState.RewardSelection) && pendingSpawns>0)spawnRoutine=StartCoroutine(ResumeSavedWave());
            ReconcileHealth();
            if (State == WaveState.RewardSelection) PauseForReward();
            Changed?.Invoke();
        }
        private System.Collections.IEnumerator ResumeSavedWave()
        { yield return null; while(Time.timeScale==0)yield return null; yield return SpawnWave(); }
    }
}

