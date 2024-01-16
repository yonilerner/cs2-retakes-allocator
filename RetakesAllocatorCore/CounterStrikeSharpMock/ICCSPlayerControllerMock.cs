using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocatorCore.CounterStrikeSharpMock;

public interface ICCSPlayerControllerMock
{
    public int? UserId { get; }
    public bool IsValid { get; }
    public ulong SteamId { get; }
    public CsTeam Team { get; }
    
    public ICCSPlayerPawnMock? PlayerPawn { get; }
    
    public ICCSPlayer_ItemServicesMock? ItemServices { get; }

    public void GiveNamedItem(CsItem item);
}
