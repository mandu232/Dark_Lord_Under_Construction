using System.Linq;
using DarkLord.Dungeon;
using DarkLord.Minions;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum TutorialTrainingStep { None, Pickup, Deliver, Summon, Complete, Corridor, Entrance }
    public sealed partial class StartingTutorial
    {
        private CorpseHarvest trainingHarvest;
        private MinionSummoning trainingSummoning;
        public TutorialTrainingStep TrainingStep { get; private set; }
        public bool IsTraining => Active && TrainingStep != TutorialTrainingStep.None;
        public bool TrainingBlocksSummoning => IsTraining && (TrainingStep == TutorialTrainingStep.Pickup || TrainingStep == TutorialTrainingStep.Deliver);
        public int TrainingNumber => TrainingStep == TutorialTrainingStep.Corridor ? 4 : TrainingStep == TutorialTrainingStep.Entrance ? 5 : (int)TrainingStep;
        public string TrainingInstruction => TrainingStep == TutorialTrainingStep.Pickup ? "시체 옆으로 이동해 F키로 들어 올리세요.\n시체를 들면 소환소로 운반할 수 있습니다." :
            TrainingStep == TutorialTrainingStep.Deliver ? "시체를 든 채 소환소로 이동하세요.\n소환소에 도착하면 시체가 흡수되어 소환 스택 1개를 얻습니다." :
            TrainingStep == TutorialTrainingStep.Summon ? "소환소 옆에서 F → 혈 슬라임 소환을 누르세요.\n소환 스택 1개로 첫 하수인인 혈 슬라임을 불러옵니다.\n소환소 정비 보상: 시작방 정원 +1" :
            TrainingStep == TutorialTrainingStep.Corridor ? "B → 방·복도 → 복도를 선택하세요.\n기존 바닥에서 바깥으로 드래그해 연결 복도를 만들고 굴착이 끝날 때까지 기다리세요." :
            "B → 방·복도 → 균열을 선택하세요.\n새로 완성한 복도의 빈 바닥을 클릭해 균열을 옮기세요.";
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
            TrainingStep = TutorialTrainingStep.Corridor;
        }
        private void UpdateExpansionTraining()
        {
            if (TrainingStep == TutorialTrainingStep.Corridor && dungeon.Construction.Sections.Any(s => s.Kind == ConstructionKind.Corridor && s.IsComplete &&
                s.Cells.Any(c => dungeon.Construction.FindPath(c, dungeon.Layout.Core, new System.Collections.Generic.List<Vector2Int>()))))
                TrainingStep = TutorialTrainingStep.Entrance;
            if (TrainingStep != TutorialTrainingStep.Entrance || !dungeon.Layout.HasEntrance) return;
            var entranceSection = dungeon.Construction.At(dungeon.Layout.Entrance);
            if (entranceSection == null || entranceSection.Kind != ConstructionKind.Corridor || !entranceSection.IsComplete) return;
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
            foreach (var enemy in enemies)
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.IsAlive && enemy.GetComponent<OpeningRetreat>() == null)
                    enemy.gameObject.AddComponent<OpeningRetreat>().Begin(dungeon);
        }
    }
}
