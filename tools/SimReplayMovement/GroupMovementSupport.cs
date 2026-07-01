static partial class Program
{
    const int GroupSize = 30;
    const int AttackTicks = 1500;

    static readonly Vector2 GroupTarget = new(1800, 1200);

    static readonly List<EntityId> GroupIds = Enumerable.Range(1, GroupSize)
        .Select(i => new EntityId(i))
        .ToList();

    static (EntityWorld World, List<EntityId> Ids) BuildGroup(int size, ulong seed)
    {
        var world = new EntityWorld(seed) { WorldWidth = 3600, WorldHeight = 2400 };
        world.AddSystem(new CommandSystem());
        world.AddSystem(new MovementSystem());
        world.AddSystem(new SeparationSystem());

        var spec = new EntitySpec
        {
            Id = "replay.grunt",
            Kind = EntityKind.Unit,
            Display = new EntityDisplaySpec("Grunt", "g.name", "g.role", "GR", IconGlyph.Infantry),
        };

        var ids = new List<EntityId>(size);
        var rng = new Random(7);
        for (var i = 0; i < size; i++)
        {
            var start = new Vector2(300 + (float)(rng.NextDouble() * 400), 300 + (float)(rng.NextDouble() * 1500));
            var e = world.Spawn(spec, new OwnerId(1), EntityTransform.At(start), new EntityComponentState[]
            {
                new MovementComponentState(Velocity: default),
                new MovementProfileComponentState(MaxSpeed: 160f),
                new CollisionComponentState(Radius: 14, Mass: 1, PushPriority: 1, BlocksMovement: true),
            });
            ids.Add(e.Id);
        }

        return (world, ids);
    }
}
