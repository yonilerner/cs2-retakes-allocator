namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface ICCSPlayerPawnAdapter
{
    public IWeaponServicesAdapter? WeaponServices { get; }

    public void RemovePlayerItem(IPlayerWeaponAdapter weapon);
}