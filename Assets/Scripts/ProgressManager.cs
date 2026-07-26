public static class ProgressManager
{
    private static int unlockedLevel = 1;

    public static int UnlockedLevel
    {
        get => unlockedLevel;
        set => unlockedLevel = value;
    }

    public static void CompleteLevel(int globalLevel)
    {
        if (globalLevel >= unlockedLevel)
            unlockedLevel = globalLevel + 1;
    }
}
