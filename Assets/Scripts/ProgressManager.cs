using UnityEngine;

public static class ProgressManager
{
    private const string Key = "UnlockedLevel";

    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt(Key, 1);
        set
        {
            PlayerPrefs.SetInt(Key, value);
            PlayerPrefs.Save();
        }
    }

    public static void CompleteLevel(int globalLevel)
    {
        if (globalLevel >= UnlockedLevel)
            UnlockedLevel = globalLevel + 1;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetInEditor()
    {
        PlayerPrefs.DeleteKey(Key);
    }
#endif
}
