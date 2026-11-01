using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEditor;
using UnityEngine;

public static class ReviewWaveEffects
{
    public static async Task<string> HitStopFight(int milliseconds = 8000)
    {
        int pulses = 0; float beginning = 0, total = 0, longest = 0; bool stopped = false;
        var fight = Fight(milliseconds, true);
        while (!fight.IsCompleted)
        {
            bool now = Time.timeScale > 0 && Time.timeScale < .2f;
            if (now && !stopped) { beginning = Time.unscaledTime; pulses++; }
            if (!now && stopped)
            {
                float duration = Time.unscaledTime - beginning; total += duration; longest = Mathf.Max(longest, duration);
            }
            stopped = now;
            await Task.Delay(8);
        }
        return await fight + $"; observed hit-stop pulses={pulses}, longest sampled={longest * 1000:F1}ms, total slowed={total:F3}s";
    }
    public static async Task<string> Burst()
    {
        var w = UnityEngine.Object.FindAnyObjectByType<WaveDirector>();
        Time.timeScale = 1;
        float wait = Mathf.Max(w.Combat.NovaCooldown, w.Combat.WardCooldown);
        if (wait > 0) await Task.Delay(Mathf.CeilToInt((wait + .05f) * 1000));
        bool nova = w.Combat.TryNova(), ward = w.Combat.TryWard();
        await Task.Delay(160);
        Time.timeScale = 0;
        return $"Real combat overlap: nova={nova}, ward={ward}, wave={w.CurrentWave}, enemies={w.RemainingEnemies}";
    }
    public static string Setup()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before reviewing a wave.");
        Time.timeScale = 1;
        var w = UnityEngine.Object.FindAnyObjectByType<WaveDirector>();
        w.ConfigureRun(new NewGameOptions { difficulty = GameDifficulty.Nightmare, skipOpening = true });
        var p = w.Combat;
        var data = w.Economy.Capture();
        data.technology ??= new TechTreeSaveData();
        data.technology.ranks = TechCatalog.All.Select(t => t.MaxRank).ToArray();
        data.technology.attack = AttackStyle.WideSlash; data.technology.offensive = AttackSpell.FrostNova; data.technology.support = SupportSpell.Aegis;
        w.Economy.Restore(data);
        p.transform.position = w.EntrancePosition + Vector3.right * 7;
        w.Lord.RestoreHealth(5000, 4800); // Review-only survivability; combat AI and cooldowns stay live.
        var grid = p.GetComponent<MinionSummoning>().Dungeon;
        var guards = UnityEngine.Object.FindObjectsByType<MinionController>().Where(m => m.Health.Faction == Faction.Dungeon).ToArray();
        for (int i = 0; i < guards.Length; i++)
        {
            var position = w.EntrancePosition + new Vector3(5 + i % 2, (i / 2 - .5f) * 1.4f);
            guards[i].transform.position = position; guards[i].Defend(position); guards[i].Health.RestoreHealth(1000, 700);
        }
        var roster = AssetDatabase.LoadAssetAtPath<MinionRoster>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:MinionRoster")[0]));
        for (int i = 0; i < 3; i++)
        {
            var position = w.EntrancePosition + new Vector3(7 + i % 2, (i - 1) * .9f);
            var guard = UnityEngine.Object.Instantiate(i == 0 ? roster.archer : roster.support, position, Quaternion.identity);
            guard.Defend(position); guard.Health.RestoreHealth(1000, 700); grid.RegisterGuard(guard);
        }
        var cf = Camera.main.GetComponent<CameraFollow>(); if (cf != null) cf.enabled = false;
        Camera.main.transform.position = w.EntrancePosition + new Vector3(6, 0, -10.5f);
        Camera.main.orthographicSize = 5.5f;
        typeof(WaveDirector).GetField("<CurrentWave>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(w, 4);
        var completed = (System.Collections.Generic.HashSet<int>)typeof(WaveDirector).GetField("completedWaves", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(w); for (int wave = 1; wave < 5; wave++) completed.Add(wave);
        typeof(WaveDirector).GetField("nextWaveRemaining", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(w, 0f);
        Time.timeScale = 0;
        return "Nightmare wave 5 queued through the real wave scheduler; seven defenders including archer/two supports";
    }

    public static async Task<string> Fight(int milliseconds = 3000, bool bloodNova = false)
    {
        var w = UnityEngine.Object.FindAnyObjectByType<WaveDirector>();
        int peak = 0, attacks = 0, novas = 0, wards = 0;
        Time.timeScale = 1;
        try
        {
            for (int elapsed = 0; elapsed < milliseconds; elapsed += 100)
            {
                var p = w.Combat;
                var target = UnityEngine.Object.FindObjectsByType<ActorHealth>().Where(h => h.Faction == Faction.Invader && h.IsAlive)
                    .OrderBy(h => Vector2.Distance(p.transform.position, h.transform.position)).FirstOrDefault();
                if (target != null && Vector2.Distance(p.transform.position, target.transform.position) < 4)
                {
                    if (p.TryAttack(target.transform.position - p.transform.position)) attacks++;
                    if (bloodNova ? p.TryNova() : p.TryOffensiveSpell()) novas++;
                    if (p.TrySupportSpell()) wards++;
                }
                await Task.Delay(100);
                peak = Math.Max(peak, UnityEngine.Object.FindObjectsByType<GameplayEffects>().Sum(fx => fx.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled)));
            }
        }
        finally { Time.timeScale = 0; }
        return $"wave={w.CurrentWave} state={w.State} enemies={w.RemainingEnemies} peakEffects={peak} attacks={attacks} novas={novas} wards={wards}; roles=" +
            string.Join(",", UnityEngine.Object.FindObjectsByType<InvaderTactics>().Where(t => t.GetComponent<ActorHealth>().IsAlive).GroupBy(t => t.Role).Select(g => g.Key + ":" + g.Count()));
    }
}
