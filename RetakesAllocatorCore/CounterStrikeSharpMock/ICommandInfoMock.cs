namespace RetakesAllocatorCore.CounterStrikeSharpMock;

public interface ICommandInfoMock
{
    public string GetArg(int index);
    public int ArgCount { get; }
}
