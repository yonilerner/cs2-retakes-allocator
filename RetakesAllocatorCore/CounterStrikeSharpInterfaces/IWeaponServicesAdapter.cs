namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface IWeaponServicesAdapter
{
    public ICollection<IPlayerWeaponAdapter> MyWeapons { get; }
}