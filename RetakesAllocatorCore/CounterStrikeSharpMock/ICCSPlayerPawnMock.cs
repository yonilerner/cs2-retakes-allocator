namespace RetakesAllocatorCore.CounterStrikeSharpMock;

public interface ICCSPlayerPawnMock
{
    public IWeaponServicesMock? WeaponServices { get; }

    public void RemovePlayerItem(IPlayerWeaponMock weapon);
}