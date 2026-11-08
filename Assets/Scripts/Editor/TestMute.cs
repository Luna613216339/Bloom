using UnityEditor;

/// <summary>
/// AI 在编辑器里进 Play 跑截图、巡游关卡时用的静音。
///
///   TestMute.On();                       // 进 Play 之前调
///   EditorApplication.isPlaying = true;
///   或者一步到位：TestMute.EnterPlayMuted();
///
/// 打开的是 Game 视图工具栏上的 Mute Audio。**退出 Play 时自动恢复**，
/// 所以用户自己按 Play 照常有声音；只有 AI 打开过的那一次才会关掉。
/// 用户自己手动点过 Mute Audio 的，这里不会去动它。
/// </summary>
[InitializeOnLoad]
public static class TestMute
{
    const string Key = "Bloom.TestMute";

    static TestMute()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Off();
        };
    }

    public static void On()
    {
        if (EditorUtility.audioMasterMute) return;   // 本来就静音，不归我们管
        SessionState.SetBool(Key, true);
        EditorUtility.audioMasterMute = true;
    }

    public static void Off()
    {
        if (!SessionState.GetBool(Key, false)) return;
        SessionState.EraseBool(Key);
        EditorUtility.audioMasterMute = false;
    }

    public static void EnterPlayMuted()
    {
        On();
        EditorApplication.isPlaying = true;
    }
}
