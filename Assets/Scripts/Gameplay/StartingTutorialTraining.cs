using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum TutorialTrainingStep { None, Pickup, Deliver, Summon, Complete }
    public sealed partial class StartingTutorial
    {
        private CorpseHarvest trainingHarvest;
        private MinionSummoning trainingSummoning;
        public TutorialTrainingStep TrainingStep { get; private set; }
        public bool IsTraining => Active && TrainingStep != TutorialTrainingStep.None;
        public bool TrainingBlocksSummoning => IsTraining && (TrainingStep == TutorialTrainingStep.Pickup || TrainingStep == TutorialTrainingStep.Deliver);
        public string TrainingInstruction => TrainingStep == TutorialTrainingStep.Pickup ? "① 시체 옆으로 이동해 F키로 들어 올리세요.\n실제로 시체를 들어야 다음 단계가 열립니다." :
            TrainingStep == TutorialTrainingStep.Deliver ? "② 시체를 든 채 소환소로 이동하세요.\n가까이 가면 시체가 흡수되고 스택이 쌓입니다." :
            "③ 소환소 옆에서 F → 무작위 소환을 누르세요.\n얻은 스택 1개로 새 하수인을 소환하세요.\n소환소 정비 보상: 시작방 정원 +1";
        private void BindTrainingEvents()
        {
            trainingHarvest = waves.Harvest; trainingSummoning = waves.Lord.GetComponent<MinionSummoning>();
            trainingHarvest.CorpsePickedUp += OnTrainingPickup;
            trainingHarvest.CorpseDelivered += OnTrainingDelivery;
            trainingSummoning.RandomMinionSummoned += OnTrainingSummon;
        }
        private void UnbindTrainingEvents()
        {
            if (trainingHarvest != null) { trainingHarvest.CorpsePickedUp -= OnTrainingPickup; trainingHarvest.CorpseDelivered -= OnTrainingDelivery; }
            if (trainingSummoning != null) trainingSummoning.RandomMinionSummoned -= OnTrainingSummon;
        }
        private void BeginTraining()
        {
            var facility = dungeon.RecruitmentFacilities.FirstOrDefault(b => dungeon.Construction.RoomAt(b.Cell)?.IsCore == true);
            if (facility != null) facility.RestoreTutorialCapacity(true);
            TrainingStep = trainingHarvest.Carried != null ? TutorialTrainingStep.Deliver : TutorialTrainingStep.Pickup;
            EnsureTrainingCorpse();
        }
        private void EnsureTrainingCorpse()
        {
            if (trainingHarvest == null || trainingHarvest.Corpses.Any(c => c != null && !c.Consumed)) return;
            trainingHarvest.LeaveCollapseVictim(dungeon.CellCenter(dungeon.Layout.Core + new Vector2Int(-2, -2)));
        }
        private void OnTrainingPickup(InvaderCorpse corpse)
        { if (IsTraining && TrainingStep == TutorialTrainingStep.Pickup) TrainingStep = TutorialTrainingStep.Deliver; }
        private void OnTrainingDelivery(InvaderCorpse corpse, ResourceBuilding facility, bool carried)
        { if (IsTraining && TrainingStep == TutorialTrainingStep.Deliver && carried) TrainingStep = TutorialTrainingStep.Summon; }
        private void OnTrainingSummon(MinionController minion)
        {
            if (!IsTraining || TrainingStep != TutorialTrainingStep.Summon) return;
            TrainingStep = TutorialTrainingStep.Complete;
            dialoguePage = 4; dialogueLine = 0; DialogueOpen = true; SynchronizeDialoguePause();
        }
        private void CompleteTutorial()
        {
            Elapsed = FinishAt; Active = false; DialogueOpen = false;
            UnbindTrainingEvents(); ReleaseProtection(); waves.FinishTutorial();
        }
        private void UpdateRetreat()
        {
            if (Elapsed < BattleEnds || collapsed || DialogueOpen) return;
            var rowen = Rowen;
            if (rowen != null && rowen.GetComponent<OpeningRetreat>() == null)
                rowen.gameObject.AddComponent<OpeningRetreat>().Begin(dungeon);
        }
    }
}
