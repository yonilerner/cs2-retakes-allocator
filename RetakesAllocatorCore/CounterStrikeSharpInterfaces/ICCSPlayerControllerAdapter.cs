using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface ICCSPlayerControllerAdapter
{
    public int? UserId { get; }
    public bool IsValid { get; }
    public ulong SteamId { get; }
    public CsTeam Team { get; }
    
    public ICCSPlayerPawnAdapter? PlayerPawn { get; }
    
    public ICCSPlayer_ItemServicesAdapter? ItemServices { get; }

    public void GiveNamedItem(CsItem item);
}
