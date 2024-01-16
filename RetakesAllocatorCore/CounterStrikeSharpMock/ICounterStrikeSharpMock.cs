using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocatorCore.CounterStrikeSharpMock;

public interface ICounterStrikeSharpMock
{
    public INativeAPIMock NativeApi { get; }
    public IServerMock Server { get; }
    public IUtilitiesMock Utilities { get; }
    
    public string MessagePrefix { get; }

    void AllocateItemsForPlayer(ICCSPlayerControllerMock player, ICollection<CsItem> items, string? slotToSelect);

    public void GiveDefuseKit(ICCSPlayerControllerMock player);

    public void AddTimer(float interval, Action callback);

    public bool PlayerIsValid(ICCSPlayerControllerMock? player);

    public ICollection<string> CommandInfoToArgList(ICommandInfoMock commandInfo, bool includeFirst = false);

    public bool RemoveWeapons(ICCSPlayerControllerMock playerController, Func<CsItem, bool>? where = null);

    public bool IsWarmup();

    public bool IsWeaponAllocationAllowed();
    
    public double GetVectorDistance(IVector v1, IVector v2);
}
