using UnityEngine;

/// <summary>
/// 全局音频。自己在游戏启动时创建，跨场景常驻，所以九个场景都不用挂任何东西。
///
/// 换成真音频文件的做法：把 clip 字段在 Inspector 上拖上去就行，
/// 留空则自动用 AudioHelper 合成的占位音。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const int VoiceCount = 24;          // 同时能响几个音，连锁高峰期要够用
    const int SemitonesPerOctave = 12;  // 连锁音爬满一个八度就回到起点

    [Header("留空则使用代码合成的占位音")]
    public AudioClip music;
    public AudioClip chain;
    public AudioClip gold;
    public AudioClip click;
    public AudioClip success;
    public AudioClip fail;
    public AudioClip purchase;
    public AudioClip equip;
    public AudioClip door;

    [Header("音量")]
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.6f;

    private AudioSource musicSource;
    private AudioSource[] voices;
    private int nextVoice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            voices[i] = src;
        }

        if (music != null)
        {
            musicSource.clip = music;
            musicSource.Play();
        }
    }

    void Play(AudioClip clip, float pitch, float volumeScale)
    {
        if (clip == null || voices == null) return;

        var src = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;

        src.clip = clip;
        src.pitch = pitch;
        src.volume = sfxVolume * volumeScale;
        src.Play();
    }

    /// <summary>
    /// 连锁反应音。第 index 颗球比第一颗高 index 个半音，爬满一个八度回到起点。
    /// 一个音效文件就能做出整条"嘟嘟嘟嘟↗"，不用准备一串文件。
    /// </summary>
    public void PlayChain(int index)
    {
        int step = Mathf.Max(0, index - 1) % SemitonesPerOctave;
        float pitch = Mathf.Pow(2f, step / (float)SemitonesPerOctave);
        Play(chain != null ? chain : AudioHelper.Chain, pitch, 0.7f);
    }

    public void PlayGold() => Play(gold != null ? gold : AudioHelper.Gold, 1f, 1f);
    public void PlayClick() => Play(click != null ? click : AudioHelper.Click, 1f, 0.7f);
    public void PlaySuccess() => Play(success != null ? success : AudioHelper.Success, 1f, 0.9f);
    public void PlayFail() => Play(fail != null ? fail : AudioHelper.Fail, 1f, 0.9f);
    public void PlayPurchase() => Play(purchase != null ? purchase : AudioHelper.Purchase, 1f, 1f);
    public void PlayEquip() => Play(equip != null ? equip : AudioHelper.Equip, 1f, 0.8f);

    /// <summary>开门音调高，关门音调低 —— 一个音效两种状态</summary>
    public void PlayDoor(bool opening)
        => Play(door != null ? door : AudioHelper.Door, opening ? 1.15f : 0.8f, 0.8f);

    // ---- 给 OnGUI 用的包装，按下就出声 ----

    public static bool Button(Rect rect, string text, GUIStyle style)
    {
        bool pressed = GUI.Button(rect, text, style);
        if (pressed && Instance != null) Instance.PlayClick();
        return pressed;
    }
}
