using System.Security.Cryptography;
using System.Text;
using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.AiOpponentLoopQa;

internal static partial class AiOpponentLoopQaProgram
{
    private static string SetupFingerprint(MapSpec map, UnitBattlefield battlefield)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        WriteFloatBits(writer, map.WorldSize.Width);
        WriteFloatBits(writer, map.WorldSize.Height);

        foreach (var start in map.OwnerStarts.OrderBy(start => start.OwnerId.Value))
        {
            writer.Write(start.OwnerId.Value);
            writer.Write((int)start.Faction);
            WriteFloatBits(writer, start.Position.X);
            WriteFloatBits(writer, start.Position.Y);
            WriteFloatBits(writer, start.Facing);
            writer.Write(start.StartingCredits);
        }

        foreach (var resource in map.Resources.OrderBy(resource => resource.Id, StringComparer.Ordinal))
        {
            writer.Write(resource.Id);
            WriteFloatBits(writer, resource.Position.X);
            WriteFloatBits(writer, resource.Position.Y);
            WriteFloatBits(writer, resource.Radius);
            writer.Write(resource.Amount);
            writer.Write(resource.Accent.Hex);
        }

        foreach (var building in battlefield.BuildingSnapshots().OrderBy(building => building.Id))
        {
            writer.Write(building.Id);
            writer.Write(building.Kind);
            writer.Write(building.PlayerSlotId.Value);
            writer.Write((int)building.Faction);
            WriteVectorBits(writer, building.Position);
            WriteFloatBits(writer, building.Facing);
            WriteVectorBits(writer, building.Footprint);
        }

        foreach (var unit in battlefield.Units.OrderBy(unit => unit.Id))
        {
            writer.Write(unit.Id);
            writer.Write(unit.Spec.Id);
            writer.Write(unit.PlayerSlotId.Value);
            WriteVectorBits(writer, unit.Position);
            WriteFloatBits(writer, unit.Facing);
            WriteFloatBits(writer, unit.Spec.Collision.Radius);
        }

        writer.Flush();
        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static TournamentStateFailure? ValidateLiveState(UnitBattlefield battlefield, int tick)
    {
        foreach (var unit in battlefield.Units.OrderBy(unit => unit.Id))
        {
            var subject = $"unit:{unit.Id}";
            if (!Finite(unit.Position))
            {
                return new TournamentStateFailure("unit.non_finite_position", subject, tick);
            }

            if (!Finite(unit.Velocity))
            {
                return new TournamentStateFailure("unit.non_finite_velocity", subject, tick);
            }

            if (!float.IsFinite(unit.Facing))
            {
                return new TournamentStateFailure("unit.non_finite_facing", subject, tick);
            }

            if (!float.IsFinite(unit.Hp))
            {
                return new TournamentStateFailure("unit.non_finite_hp", subject, tick);
            }

            if (!Inside(unit.Position, battlefield.WorldSize))
            {
                return new TournamentStateFailure("unit.out_of_bounds", subject, tick);
            }
        }

        foreach (var building in battlefield.BuildingSnapshots().OrderBy(building => building.Id))
        {
            var subject = $"building:{building.Id}";
            if (!Finite(building.Position))
            {
                return new TournamentStateFailure("building.non_finite_position", subject, tick);
            }

            if (!float.IsFinite(building.Facing))
            {
                return new TournamentStateFailure("building.non_finite_facing", subject, tick);
            }

            if (!float.IsFinite(building.Hp))
            {
                return new TournamentStateFailure("building.non_finite_hp", subject, tick);
            }

            if (!float.IsFinite(battlefield.BuildingBuildProgress(building.Id)))
            {
                return new TournamentStateFailure("building.non_finite_build_progress", subject, tick);
            }

            if (!Inside(building.Position, battlefield.WorldSize))
            {
                return new TournamentStateFailure("building.out_of_bounds", subject, tick);
            }
        }

        return null;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static bool Inside(Vector2 position, Vector2 worldSize) =>
        position.X >= 0 && position.Y >= 0 && position.X <= worldSize.X && position.Y <= worldSize.Y;

    private static void WriteVectorBits(BinaryWriter writer, Vector2 value)
    {
        WriteFloatBits(writer, value.X);
        WriteFloatBits(writer, value.Y);
    }

    private static void WriteFloatBits(BinaryWriter writer, float value)
    {
        writer.Write(BitConverter.SingleToInt32Bits(value));
    }
}
