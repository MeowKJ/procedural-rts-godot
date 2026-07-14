using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private sealed class OpponentLoopMetrics
    {
        private readonly OpponentLoopRuntime _runtime;
        private readonly HashSet<int> _raiderIds;
        private readonly Dictionary<int, float> _buildingHp;

        public OpponentLoopMetrics(OpponentLoopRuntime runtime)
        {
            _runtime = runtime;
            _raiderIds = runtime.Raiders.Select(unit => unit.Id).ToHashSet();
            _buildingHp = runtime.Battlefield.BuildingSnapshots().ToDictionary(building => building.Id, building => building.Hp);
            MaxEnemyCombatUnitsAlive = runtime.Battlefield.Units.Count(unit => IsCombat(unit, PlayerSlotId.Two));
            MinRaiderHp = runtime.Raiders.Sum(unit => MathF.Max(0, unit.Hp));
            MaxEnemyCredits = runtime.InitialEnemyCredits;
            Subscribe();
        }

        public int CurrentTick { get; set; }
        public int QueuedEvents { get; private set; }
        public int CompletedEvents { get; private set; }
        public List<string> ProducedDesignIds { get; } = [];
        public List<string> ProducedCombatDesignIds { get; } = [];
        public int DefenseBuildingHits { get; private set; }
        public int DefenseUnitHits { get; private set; }
        public int RaiderDeaths { get; private set; }
        public int EnemyBuildingHits { get; private set; }
        public int ResourceEvents { get; private set; }
        public int HarvestAssignments { get; set; }
        public int HarvestBridgeCommands { get; set; }
        public int ConstructionBridgeCommands { get; set; }
        public int ProductionBridgeCommands { get; set; }
        public int WaveBridgeCommands { get; set; }
        public int LeftAttackCommands { get; set; }
        public float LeftToRightDamage { get; private set; }
        public float RightToLeftDamage { get; private set; }
        public int LaunchedWaveUnitOrders { get; set; }
        public int MaxManualWaveAttackers { get; private set; }
        public int MaxEnemyCombatUnitsAlive { get; private set; }
        public float MinRaiderHp { get; private set; }
        public int MaxEnemyCredits { get; private set; }
        public int FirstWaveTick { get; set; } = -1;
        public int SecondWaveTick { get; set; } = -1;
        public int FirstHarvestTick { get; set; } = -1;
        public int FirstConstructionTick { get; set; } = -1;
        public int FirstProductionTick { get; private set; } = -1;
        public int FirstEngagementTick { get; private set; } = -1;
        public bool RaidCommanded { get; set; }
        public HashSet<int> AssignedHarvesters { get; } = [];

        public void UpdateAfterTick()
        {
            var battlefield = _runtime.Battlefield;
            MaxEnemyCredits = Math.Max(MaxEnemyCredits, battlefield.Credits(PlayerSlotId.Two));
            MaxEnemyCombatUnitsAlive = Math.Max(
                MaxEnemyCombatUnitsAlive,
                battlefield.Units.Count(unit => IsCombat(unit, PlayerSlotId.Two)));
            MaxManualWaveAttackers = Math.Max(
                MaxManualWaveAttackers,
                battlefield.Units.Count(unit =>
                    unit.PlayerSlotId == PlayerSlotId.Two
                    && unit.AttackTargetIsManual
                    && unit.AttackTargetKind == CombatTargetKind.Building
                    && unit.AttackTargetId == _runtime.PlayerBase.Headquarters.Id));
            MinRaiderHp = Math.Min(
                MinRaiderHp,
                battlefield.Units.Where(unit => _raiderIds.Contains(unit.Id)).Sum(unit => MathF.Max(0, unit.Hp)));
        }

        private void Subscribe()
        {
            var battlefield = _runtime.Battlefield;
            battlefield.ProductionQueued += (building, _) =>
            {
                if (building.PlayerSlotId == PlayerSlotId.Two)
                {
                    QueuedEvents++;
                }
            };
            battlefield.ProductionCompleted += (building, _, unit) =>
            {
                if (building.PlayerSlotId != PlayerSlotId.Two || unit.PlayerSlotId != PlayerSlotId.Two)
                {
                    return;
                }

                CompletedEvents++;
                FirstProductionTick = FirstProductionTick < 0 ? CurrentTick : FirstProductionTick;
                ProducedDesignIds.Add(unit.Spec.Id);
                if (!unit.Spec.RoleTags.Contains(UnitRoleTag.Economy))
                {
                    ProducedCombatDesignIds.Add(unit.Spec.Id);
                }
            };
            battlefield.UnitAttacked += (target, attacker) =>
            {
                RecordDamage(attacker.PlayerSlotId, target.PlayerSlotId, target.LastDamageAmount);
                if (target.PlayerSlotId == PlayerSlotId.One && attacker.PlayerSlotId == PlayerSlotId.Two)
                {
                    DefenseUnitHits++;
                }
            };
            battlefield.UnitAttackedByBuilding += (target, attacker) =>
            {
                RecordDamage(attacker.PlayerSlotId, target.PlayerSlotId, target.LastDamageAmount);
                if (target.PlayerSlotId == PlayerSlotId.One && attacker.PlayerSlotId == PlayerSlotId.Two)
                {
                    DefenseBuildingHits++;
                }
            };
            battlefield.BuildingAttacked += (target, attacker) =>
            {
                var previousHp = _buildingHp.TryGetValue(target.Id, out var knownHp)
                    ? knownHp
                    : BuildSpecCatalog.For(target.Kind).MaxHp;
                RecordDamage(attacker.PlayerSlotId, target.PlayerSlotId, MathF.Max(0, previousHp - target.Hp));
                _buildingHp[target.Id] = target.Hp;
                if (target.PlayerSlotId == PlayerSlotId.One && attacker.PlayerSlotId == PlayerSlotId.Two)
                {
                    EnemyBuildingHits++;
                }
            };
            battlefield.UnitsRemoved += deaths =>
            {
                RaiderDeaths += deaths.Count(death => _raiderIds.Contains(death.Id));
            };
            battlefield.ResourceInventoryChanged += (slot, _) =>
            {
                if (slot == PlayerSlotId.Two)
                {
                    ResourceEvents++;
                }
            };
        }

        private void RecordDamage(PlayerSlotId attacker, PlayerSlotId target, float amount)
        {
            if (!float.IsFinite(amount) || amount <= 0 || attacker == target)
            {
                return;
            }

            if (attacker == PlayerSlotId.One && target == PlayerSlotId.Two)
            {
                LeftToRightDamage += amount;
            }
            else if (attacker == PlayerSlotId.Two && target == PlayerSlotId.One)
            {
                RightToLeftDamage += amount;
            }

            FirstEngagementTick = FirstEngagementTick < 0 ? CurrentTick : FirstEngagementTick;
        }
    }
}
