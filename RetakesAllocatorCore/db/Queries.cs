namespace RetakesAllocatorCore.db;

public class Queries
{
    public static UserSetting? GetUserSettings(ulong userId)
    {
        return Db.GetInstance().UserSettings.FirstOrDefault(u => u.UserId == userId);
    }
}
