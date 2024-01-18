using RetakesAllocatorCore;

namespace RetakesAllocator.Managers;

public class RoundTypeManager
{
    #region Instance management
    private static RoundTypeManager? _instance;
    
    public static RoundTypeManager GetInstance()
    {
        return _instance ??= new RoundTypeManager();
    }
    #endregion
    
    private RoundType? _nextRoundType;
    private RoundType? _currentRoundType;
    
    public RoundType? GetNextRoundType()
    {
        return _nextRoundType;
    }

    public void SetNextRoundType(RoundType? nextRoundType)
    {
        _nextRoundType = nextRoundType;
    }

    public RoundType? GetCurrentRoundType()
    {
        return _currentRoundType;
    }

    public void SetCurrentRoundType(RoundType? currentRoundType)
    {
        _currentRoundType = currentRoundType;
    }
}
