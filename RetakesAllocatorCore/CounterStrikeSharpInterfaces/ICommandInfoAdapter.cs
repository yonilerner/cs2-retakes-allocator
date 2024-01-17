namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface ICommandInfoAdapter
{
    public string GetArg(int index);
    public int ArgCount { get; }
}
