using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Translates due player/AI commands into mutations of authoritative component
/// state. Nothing else in the simulation mutates target/order state directly;
/// input becomes a command, the command becomes component state here. This is
/// the single write point that keeps the command log a faithful description of
/// what the simulation did.
/// </summary>
public sealed partial class CommandSystem : ISimSystem
{
    private readonly List<EntityInstance> _scalarOrderMembers = [];
    private readonly HashSet<int> _selectionSubjectIds = [];
    private readonly List<EntityInstance> _groupOrderMembers = [];
    private readonly List<FormationUnit> _groupMoveFormationUnits = [];
    private readonly List<FormationDestination> _groupMoveDestinationResults = [];
    private readonly List<FormationUnit> _groupMoveOrderedUnits = [];
    private readonly List<(float X, float Y)> _groupMoveSlots = [];
    private readonly List<(float X, float Y)> _groupMoveRemainingSlots = [];
    private readonly Dictionary<int, FormationDestination> _groupMoveDestinations = [];
    private readonly List<AttackSlotUnit> _groupAttackSlotUnits = [];
    private readonly List<AttackSlotAssignment> _groupAttackAssignmentResults = [];
    private readonly List<AttackSlotUnit> _groupAttackOrderedUnits = [];
    private readonly List<AttackSlotUnit> _groupAttackAnchors = [];
    private readonly List<AttackSlotUnit> _groupAttackMovers = [];
    private readonly List<Vector2> _groupAttackFreeSlots = [];
    private readonly Dictionary<int, AttackSlotAssignment> _groupAttackAssignments = [];

    public void Step(SimContext context)
    {
        // Commands arrive already ordered (tick, issuer, sequence) from the buffer.
        foreach (var sequenced in context.Commands)
        {
            switch (sequenced.Command)
            {
                case GroupMoveEntityCommand groupMove:
                    ApplyGroupMove(context.World, groupMove);
                    break;

                case GroupAttackEntityCommand groupAttack:
                    ApplyGroupAttack(context.World, groupAttack);
                    break;

                case AttackGroundEntityCommand attackGround:
                    ApplyAttackGround(context.World, attackGround);
                    break;

                case MoveEntityCommand move:
                    ApplyMove(context.World, move.Issuer, move.Subjects, move.Target, move.Mode, manualAttack: false);
                    break;

                case AttackMoveEntityCommand attackMove:
                    ApplyMove(context.World, attackMove.Issuer, attackMove.Subjects, attackMove.Target, attackMove.Mode, manualAttack: false);
                    break;

                case PatrolEntityCommand patrol:
                    ApplyPatrol(context.World, patrol);
                    break;

                case GuardEntityCommand guard:
                    ApplyGuard(context.World, guard);
                    break;

                case AttackEntityCommand attack:
                    ApplyAttack(context.World, attack);
                    break;

                case SetSelectionEntityCommand selection:
                    ApplySelection(context.World, selection);
                    break;

                case StopEntityCommand stop:
                    ApplyStop(context.World, stop.Issuer, stop.Subjects, hold: false);
                    break;

                case HoldPositionEntityCommand hold:
                    ApplyStop(context.World, hold.Issuer, hold.Subjects, hold: true);
                    break;

                case SetStanceEntityCommand stance:
                    ApplyStance(context.World, stance);
                    break;

                case HarvestEntityCommand harvest:
                    ApplyHarvest(context.World, harvest);
                    break;

                case AutoHarvestEntityCommand autoHarvest:
                    ApplyAutoHarvest(context.World, autoHarvest);
                    break;

                case RepairEntityCommand repair:
                    ApplyRepair(context.World, repair);
                    break;

                // Produce / Build are wired in as their systems come online.
            }
        }
    }

}
