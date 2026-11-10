using System;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Minions
{
    public sealed partial class InvaderTactics
    {
        private const string SoldierArtPath = "Characters/Invaders/";
        private const int PatternStart = 8, SheetFrames = 16;
        private const float ImpactPoseTime = .1f, FinishPoseTime = .16f;
        private bool soldierArtDirty = true;
        private Animator soldierAnimator;
        private RuntimeAnimatorController originalSoldierController;
        private SpriteRenderer soldierRenderer;
        private Sprite[] soldierFrames;
        private bool hasSoldierArt;
        private float soldierImpactAt = float.NegativeInfinity;
        private bool soldierImpactFacesLeft;
        private bool UsesRegionalChampionArt => hero && Race != InvaderRace.Human;

        private void DrawSoldierArt()
        {
            if (soldierArtDirty) LoadSoldierArt();
            if (!hasSoldierArt || !health.IsAlive) return;

            soldierRenderer.color = RageTint;
            if (soldierFrames == null || controller.IsFlinching) return;

            float sinceImpact = Time.time - soldierImpactAt;
            if (UsesRegionalChampionArt)
            {
                DrawRegionalChampion(sinceImpact);
                return;
            }
            if (sinceImpact < ImpactPoseTime + FinishPoseTime)
            {
                soldierRenderer.sprite = soldierFrames[PatternStart + (sinceImpact < ImpactPoseTime ? 2 : 3)];
                soldierRenderer.flipX = soldierImpactFacesLeft;
                return;
            }

            int frame = Action switch
            {
                InvaderAction.ShieldBash or InvaderAction.Healing or InvaderAction.Aiming => ActionProgress < .45f ? 0 : 1,
                InvaderAction.WindingUp => 0,
                InvaderAction.Charging when Role == InvaderRole.Paladin => -1,
                InvaderAction.Charging => 1 + (Mathf.Min(3, Mathf.FloorToInt(ActionProgress * 4)) % 2),
                InvaderAction.Recovering => 3,
                _ => -1
            };
            if (frame < 0) return;
            soldierRenderer.sprite = soldierFrames[PatternStart + frame];
            soldierRenderer.flipX = SoldierPatternFacesLeft();
        }

        private void DrawRegionalChampion(float sinceImpact)
        {
            // Champion sheets use separate poses for preparation, cleave, cast, dash and roar.
            if (sinceImpact < ImpactPoseTime)
            {
                soldierRenderer.sprite = soldierFrames[9];
                soldierRenderer.flipX = soldierImpactFacesLeft;
                return;
            }

            int frame = Action switch
            {
                InvaderAction.HeroCleave or InvaderAction.WindingUp => ActionProgress < .25f ? 3 : 8,
                InvaderAction.Charging => 11,
                InvaderAction.Judging => 10,
                InvaderAction.Signature => ActionProgress < .4f ? 3 : 10,
                InvaderAction.Roaring => ActionProgress < .35f ? 3 : 12,
                InvaderAction.Recovering => 3,
                _ => -1
            };
            if (frame < 0) return;
            soldierRenderer.sprite = soldierFrames[frame];
            soldierRenderer.flipX = SoldierPatternFacesLeft();
        }

        private string SoldierArtName()
        {
            string role = Role switch
            {
                InvaderRole.Shieldbearer => "shield",
                InvaderRole.Healer => "healer",
                InvaderRole.Charger => "charger",
                InvaderRole.Archer => "archer",
                InvaderRole.HolyKnight => "holy-knight",
                InvaderRole.Paladin => "paladin",
                InvaderRole.Cleric => "cleric",
                _ => "warrior"
            };
            return Race switch
            {
                InvaderRace.Orc => "orc-" + (hero ? "champion" : role),
                InvaderRace.Elf => "elf-" + (hero ? "champion" : role),
                InvaderRace.Frostkin => "frost-" + (hero ? "champion" : role),
                InvaderRace.Emberborn => "ember-" + (hero ? "champion" : role),
                _ when hero && Role != InvaderRole.Paladin => null,
                _ => Role == InvaderRole.Charger ? "knight" : role
            };
        }

        private void LoadSoldierArt()
        {
            soldierArtDirty = false;
            soldierImpactAt = float.NegativeInfinity;
            soldierFrames = null;
            string name = SoldierArtName();
            if (name == null && !hasSoldierArt) return;
            soldierAnimator ??= GetComponentInChildren<Animator>();
            soldierRenderer ??= GetComponentInChildren<SpriteRenderer>();
            if (soldierAnimator == null || soldierRenderer == null) return;

            originalSoldierController ??= soldierAnimator.runtimeAnimatorController;
            var replacement = name != null ? Resources.Load<AnimatorOverrideController>(SoldierArtPath + "Controllers/" + name) : null;
            hasSoldierArt = replacement != null;
            soldierAnimator.runtimeAnimatorController = hasSoldierArt ? replacement : originalSoldierController;
            if (!hasSoldierArt)
            {
                soldierRenderer.color = RoleTint(Role);
                return;
            }
            if (!UsesRegionalChampionArt && (Role == InvaderRole.Warrior || Role == InvaderRole.HolyKnight)) return;

            var frames = new Sprite[SheetFrames];
            int count = 0;
            foreach (Sprite sprite in Resources.LoadAll<Sprite>(SoldierArtPath + name))
            {
                int separator = sprite.name.LastIndexOf('_');
                if (separator >= 0 && int.TryParse(sprite.name.AsSpan(separator + 1), out int index) &&
                    (uint)index < SheetFrames && frames[index] == null)
                {
                    frames[index] = sprite;
                    count++;
                }
            }
            if (count == SheetFrames) soldierFrames = frames;
        }

        private bool SoldierPatternFacesLeft()
        {
            float aimX = Action == InvaderAction.Healing ? savedHealPosition.x - transform.position.x :
                IsArcher && IsPerforming ? direction.x : motor.Facing.x;
            return Mathf.Abs(aimX) > .01f ? aimX < 0 : soldierRenderer.flipX;
        }

        private void ShowSoldierImpact()
        {
            if (soldierArtDirty) LoadSoldierArt();
            if (soldierFrames == null) return;
            soldierImpactAt = Time.time;
            soldierImpactFacesLeft = SoldierPatternFacesLeft();
        }
    }
}
