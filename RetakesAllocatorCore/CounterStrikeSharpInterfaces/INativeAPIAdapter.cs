namespace RetakesAllocatorCore.CounterStrikeSharpInterfaces;

public interface INativeAPIAdapter
{
    public void IssueClientCommand(int clientIndex, string command);
}
