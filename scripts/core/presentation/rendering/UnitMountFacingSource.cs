namespace ProceduralRts.Core;

public readonly struct UnitMountFacingSource
{
    private readonly IReadOnlyList<WeaponMountRuntimeState>? _runtimeMounts;
    private readonly IReadOnlyList<WeaponMountSpec>? _legacyMountSpecs;
    private readonly string? _singleMountId;
    private readonly float _bodyFacing;
    private readonly float _turretFacing;
    private readonly float _singleFacing;
    private readonly SourceKind _kind;

    private UnitMountFacingSource(
        SourceKind kind,
        IReadOnlyList<WeaponMountRuntimeState>? runtimeMounts,
        IReadOnlyList<WeaponMountSpec>? legacyMountSpecs,
        string? singleMountId,
        float bodyFacing,
        float turretFacing,
        float singleFacing)
    {
        _kind = kind;
        _runtimeMounts = runtimeMounts;
        _legacyMountSpecs = legacyMountSpecs;
        _singleMountId = singleMountId;
        _bodyFacing = bodyFacing;
        _turretFacing = turretFacing;
        _singleFacing = singleFacing;
    }

    public static UnitMountFacingSource FromRuntimeMounts(IReadOnlyList<WeaponMountRuntimeState> mounts)
    {
        return new UnitMountFacingSource(SourceKind.RuntimeMounts, mounts, null, null, 0, 0, 0);
    }

    public static UnitMountFacingSource FromLegacyUnit(UnitSpec spec, float bodyFacing, float turretFacing)
    {
        return new UnitMountFacingSource(SourceKind.LegacyUnit, null, spec.Weapons, null, bodyFacing, turretFacing, 0);
    }

    public static UnitMountFacingSource Single(string mountId, float facing)
    {
        return new UnitMountFacingSource(SourceKind.Single, null, null, mountId, 0, 0, facing);
    }

    public bool TryGetFacing(string mountId, out float facing)
    {
        switch (_kind)
        {
            case SourceKind.RuntimeMounts:
                return TryGetRuntimeFacing(mountId, out facing);
            case SourceKind.LegacyUnit:
                return TryGetLegacyFacing(mountId, out facing);
            case SourceKind.Single when string.Equals(_singleMountId, mountId, StringComparison.Ordinal):
                facing = _singleFacing;
                return true;
            default:
                facing = 0;
                return false;
        }
    }

    private bool TryGetRuntimeFacing(string mountId, out float facing)
    {
        if (_runtimeMounts is not null)
        {
            foreach (var mount in _runtimeMounts)
            {
                if (string.Equals(mount.MountId, mountId, StringComparison.Ordinal))
                {
                    facing = mount.Facing;
                    return true;
                }
            }
        }

        facing = 0;
        return false;
    }

    private bool TryGetLegacyFacing(string mountId, out float facing)
    {
        if (_legacyMountSpecs is not null)
        {
            foreach (var mount in _legacyMountSpecs)
            {
                if (string.Equals(mount.MountId, mountId, StringComparison.Ordinal))
                {
                    facing = mount.FacingMode == WeaponMountFacingMode.BodyFixed ? _bodyFacing : _turretFacing;
                    return true;
                }
            }
        }

        facing = 0;
        return false;
    }

    private enum SourceKind
    {
        None,
        RuntimeMounts,
        LegacyUnit,
        Single,
    }
}
