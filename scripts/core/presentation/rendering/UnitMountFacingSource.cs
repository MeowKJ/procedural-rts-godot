namespace ProceduralRts.Core;

public readonly struct UnitMountFacingSource
{
    private readonly IReadOnlyList<WeaponMountRuntimeState>? _runtimeMounts;
    private readonly string? _singleMountId;
    private readonly float _singleFacing;
    private readonly SourceKind _kind;

    private UnitMountFacingSource(
        SourceKind kind,
        IReadOnlyList<WeaponMountRuntimeState>? runtimeMounts,
        string? singleMountId,
        float singleFacing)
    {
        _kind = kind;
        _runtimeMounts = runtimeMounts;
        _singleMountId = singleMountId;
        _singleFacing = singleFacing;
    }

    public static UnitMountFacingSource FromRuntimeMounts(IReadOnlyList<WeaponMountRuntimeState> mounts)
    {
        return new UnitMountFacingSource(SourceKind.RuntimeMounts, mounts, null, 0);
    }

    public static UnitMountFacingSource Single(string mountId, float facing)
    {
        return new UnitMountFacingSource(SourceKind.Single, null, mountId, facing);
    }

    public bool TryGetFacing(string mountId, out float facing)
    {
        switch (_kind)
        {
            case SourceKind.RuntimeMounts:
                return TryGetRuntimeFacing(mountId, out facing);
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

    private enum SourceKind
    {
        None,
        RuntimeMounts,
        Single,
    }
}
