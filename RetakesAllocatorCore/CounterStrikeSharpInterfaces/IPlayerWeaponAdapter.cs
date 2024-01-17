using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface IPlayerWeaponAdapter
{
    public bool IsValid { get; }
    public string DesignerName { get; }
    public CsItem? Item { get; }
    
    public nint? Handle { get; }

    public void Remove();
}
