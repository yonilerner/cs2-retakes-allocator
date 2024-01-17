using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface ICounterStrikeSharpAdapter
{
    public INativeAPIAdapter NativeApi { get; }
    public IServerAdapter Server { get; }
    public IUtilitiesAdapter Utilities { get; }
    
    public string MessagePrefix { get; }

    void AllocateItemsForPlayer(ICCSPlayerControllerAdapter player, ICollection<CsItem> items, string? slotToSelect);

    public void GiveDefuseKit(ICCSPlayerControllerAdapter player);

    public void AddTimer(float interval, Action callback);

    public bool PlayerIsValid(ICCSPlayerControllerAdapter? player);

    public ICollection<string> CommandInfoToArgList(ICommandInfoAdapter commandInfo, bool includeFirst = false);

    public bool RemoveWeapons(ICCSPlayerControllerAdapter playerController, Func<CsItem, bool>? where = null);

    public bool IsWarmup();

    public bool IsWeaponAllocationAllowed();
    
    public double GetVectorDistance(IVector v1, IVector v2);
}
