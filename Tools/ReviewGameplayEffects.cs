using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Player;
using DarkLord.Minions;
using UnityEditor;
using UnityEngine;

public static class ReviewGameplayEffects
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<string> Live(string name, float angle = 0)
    {
        string result = Stage(name, 0, angle);
        Time.timeScale = 1;
        await Task.Delay(130);
        Time.timeScale = 0;
        return result + " sampled after 130ms of live playback";
    }
    public static string Stage(string name, float progress = .45f, float angle = 0)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before reviewing effects.");
        Time.timeScale = 0;
        foreach (var fx in UnityEngine.Object.FindObjectsByType<GameplayEffects>())
            foreach (var r in fx.GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        var p = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
        foreach (var trace in UnityEngine.Object.FindObjectsByType<MinionShot>()) UnityEngine.Object.DestroyImmediate(trace.gameObject);
        foreach (var renderer in p.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = true;
        var oldHero = GameObject.Find("Review Rowen");
        if (oldHero != null) UnityEngine.Object.DestroyImmediate(oldHero);
        var follow = Camera.main.GetComponent<CameraFollow>(); if (follow != null) follow.enabled = false;
        p.transform.position = new Vector3(.37f, -1.65f, 0);
        Camera.main.transform.position = new Vector3(.37f, -1.3f, -10);
        var kind = (GameplayEffectKind)Enum.Parse(typeof(GameplayEffectKind), name);
        var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        var data = p.GetComponent<RunEconomy>().Capture();
        data.technology ??= new TechTreeSaveData();
        data.technology.ranks = TechCatalog.All.Select(t => t.MaxRank).ToArray();
        data.technology.attack = kind == GameplayEffectKind.WideSlash ? AttackStyle.WideSlash : AttackStyle.Slash;
        data.technology.offensive = AttackSpell.FrostNova;
        data.technology.support = kind == GameplayEffectKind.Haste ? SupportSpell.Haste : SupportSpell.Aegis;
        p.GetComponent<RunEconomy>().Restore(data);
        p.RestoreSave(new CombatSaveData { mana = 100 });
        Time.timeScale = 1;
        bool actual = true;
        if (kind == GameplayEffectKind.Slash || kind == GameplayEffectKind.WideSlash)
        {
            typeof(PlayerCombat).GetField("weaponDrawn", Private).SetValue(p, true);
            typeof(PlayerCombat).GetField("weaponActionUntil", Private).SetValue(p, 0f);
            actual = p.TryAttack(dir);
        }
        else if (kind == GameplayEffectKind.FrostNova) actual = p.TryOffensiveSpell();
        else if (kind == GameplayEffectKind.Aegis || kind == GameplayEffectKind.Haste) actual = p.TrySupportSpell();
        else if (kind == GameplayEffectKind.Heal)
        {
            var h = p.GetComponent<ActorHealth>(); h.RestoreHealth(h.MaximumHealth, h.MaximumHealth - 30); actual = h.Heal(20) == 20;
        }
        else if (kind == GameplayEffectKind.Curse) { AugmentStatus.Get(p.GetComponent<ActorHealth>()).ConsumeLord(); AugmentStatus.Get(p.GetComponent<ActorHealth>()).MarkLord(); }
        else if (kind == GameplayEffectKind.Summon)
        {
            var summon = p.GetComponent<MinionSummoning>();
            var grid = summon.Dungeon;
            var cell = grid.WorldToCell(p.transform.position);
            var create = typeof(MinionSummoning).GetMethod("CreateGuard", Private);
            var actor = (MinionController)create.Invoke(summon, new object[] { summon.RecruitDefinition, cell });
            // Keep the summoned guard at its actual cell so its ground portal stays aligned.
            foreach (var renderer in p.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
        }
        else if (kind == GameplayEffectKind.HolySlash || kind == GameplayEffectKind.HolyCharge)
        {
            var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MinionController>("Assets/Prefabs/Enemies/WaveInvader.prefab"), p.transform.position, Quaternion.identity);
            hero.name = "Review Rowen"; hero.transform.localScale *= 1.4f;
            hero.Health.Configure(Faction.Invader, 20000);
            hero.Assault(p.GetComponent<ActorHealth>(), p.GetComponent<MinionSummoning>().Dungeon);
            var tactics = hero.GetComponent<InvaderTactics>() ?? hero.gameObject.AddComponent<InvaderTactics>();
            tactics.Configure(InvaderRole.Charger); tactics.SetHero(true);
            typeof(InvaderTactics).GetField("direction", Private).SetValue(tactics, dir);
            hero.GetComponent<ActorAnimation>()?.Present(kind == GameplayEffectKind.HolySlash ? ActorState.Attack : ActorState.Run, dir);
            typeof(InvaderTactics).GetMethod("ShowHeroEffect", Private).Invoke(tactics, new object[] { kind == GameplayEffectKind.HolyCharge });
            foreach (var renderer in p.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
        }
        else GameplayEffects.Show(p.transform, kind, p.transform.position + (kind == GameplayEffectKind.HolySlash || kind == GameplayEffectKind.HolyCharge ? (Vector3)dir * .7f : Vector3.zero), dir,
            kind == GameplayEffectKind.HolyCharge);
        Time.timeScale = 0;
        int visible = 0;
        foreach (var fx in UnityEngine.Object.FindObjectsByType<GameplayEffects>())
        {
            var pool = (IEnumerable)typeof(GameplayEffects).GetField("pool", Private).GetValue(fx);
            foreach (var entry in pool)
            {
                var type = entry.GetType(); var renderer = (SpriteRenderer)type.GetField("renderer").GetValue(entry);
                if (!renderer.enabled) continue;
                var clip = (GameplayEffectLibrary.Clip)type.GetField("clip").GetValue(entry);
                var duration = (float)type.GetField("duration").GetValue(entry);
                type.GetField("elapsed").SetValue(entry, progress * (clip.loop ? clip.duration : duration));
                visible++;
            }
            fx.SendMessage("Update");
        }
        return name + " trigger=" + actual + " renderers=" + visible;
    }
}
