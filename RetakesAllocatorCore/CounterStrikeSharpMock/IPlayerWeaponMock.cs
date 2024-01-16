using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocatorCore.CounterStrikeSharpMock;

public interface IPlayerWeaponMock
{
    public bool IsValid { get; }
    public string DesignerName { get; }
    public CsItem? Item { get; }
    
    public nint? Handle { get; }

    public void Remove();
}
