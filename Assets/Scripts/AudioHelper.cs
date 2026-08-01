using UnityEngine;

/// <summary>
/// 运行时合成占位音效，和 SpriteHelper 运行时生成图形是同一个套路。
/// 好处是整套音频系统现在就能跑通、能听到手感，等真正的音频文件到位
/// 只要在 AudioManager 的 Inspector 上拖进去，代码一行不用改。
/// </summary>
public static class AudioHelper
{
    const int SampleRate = 44100;

    private static AudioClip _chain, _gold, _click, _success, _fail, _purchase, _equip, _door;

    /// <summary>连锁反应的"嘟"。播放时由 AudioManager 改 pitch 实现音阶上升</summary>
    public static AudioClip Chain => _chain != null ? _chain
        : _chain = Tone("sfx_chain", 523.25f, 0.20f, 0.55f, harmonic: 0.25f);

    /// <summary>金球：两个音的小琶音，和普通连锁音要一耳朵分开</summary>
    public static AudioClip Gold => _gold != null ? _gold
        : _gold = Sequence("sfx_gold", new[]
        {
            new Note(880f,   0.00f, 0.12f, 0.5f),
            new Note(1318.5f, 0.07f, 0.30f, 0.45f),
        });

    public static AudioClip Click => _click != null ? _click
        : _click = Tone("sfx_click", 740f, 0.06f, 0.28f, harmonic: 0.1f);

    public static AudioClip Success => _success != null ? _success
        : _success = Sequence("sfx_success", new[]
        {
            new Note(523.25f, 0.00f, 0.16f, 0.45f),
            new Note(659.25f, 0.10f, 0.16f, 0.45f),
            new Note(783.99f, 0.20f, 0.36f, 0.45f),
        });

    public static AudioClip Fail => _fail != null ? _fail
        : _fail = Sequence("sfx_fail", new[]
        {
            new Note(392f,    0.00f, 0.20f, 0.45f),
            new Note(311.13f, 0.14f, 0.42f, 0.45f),
        });

    public static AudioClip Purchase => _purchase != null ? _purchase
        : _purchase = Sequence("sfx_purchase", new[]
        {
            new Note(659.25f, 0.00f, 0.14f, 0.45f),
            new Note(987.77f, 0.08f, 0.14f, 0.45f),
            new Note(1318.5f, 0.16f, 0.34f, 0.4f),
        });

    public static AudioClip Equip => _equip != null ? _equip
        : _equip = Tone("sfx_equip", 587.33f, 0.16f, 0.35f, harmonic: 0.2f);

    /// <summary>门开关。低频短促，播放时用 pitch 区分开门（高）和关门（低）</summary>
    public static AudioClip Door => _door != null ? _door
        : _door = Tone("sfx_door", 196f, 0.14f, 0.5f, harmonic: 0.45f);

    struct Note
    {
        public float freq, start, duration, amplitude;
        public Note(float freq, float start, float duration, float amplitude)
        {
            this.freq = freq; this.start = start;
            this.duration = duration; this.amplitude = amplitude;
        }
    }

    static AudioClip Tone(string name, float freq, float duration, float amplitude, float harmonic)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        var data = new float[count];
        WriteTone(data, 0, freq, duration, amplitude, harmonic);
        return Build(name, data);
    }

    static AudioClip Sequence(string name, Note[] notes)
    {
        float total = 0f;
        foreach (var n in notes)
            total = Mathf.Max(total, n.start + n.duration);

        var data = new float[Mathf.CeilToInt(SampleRate * total)];
        foreach (var n in notes)
            WriteTone(data, Mathf.FloorToInt(n.start * SampleRate),
                n.freq, n.duration, n.amplitude, 0.2f);

        return Build(name, data);
    }

    /// <summary>正弦波 + 一点二次谐波，起音 3ms，之后指数衰减</summary>
    static void WriteTone(float[] data, int offset, float freq,
        float duration, float amplitude, float harmonic)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        int attack = Mathf.Max(1, Mathf.FloorToInt(SampleRate * 0.003f));

        for (int i = 0; i < count; i++)
        {
            int idx = offset + i;
            if (idx < 0 || idx >= data.Length) continue;

            float t = i / (float)SampleRate;
            float phase = 2f * Mathf.PI * freq * t;
            float sample = Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * harmonic;

            float env = i < attack
                ? i / (float)attack
                : Mathf.Exp(-4f * (i - attack) / (float)(count - attack));

            data[idx] += sample * env * amplitude;
        }
    }

    static AudioClip Build(string name, float[] data)
    {
        // 防削波
        float peak = 0f;
        for (int i = 0; i < data.Length; i++)
            peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        if (peak > 1f)
            for (int i = 0; i < data.Length; i++)
                data[i] /= peak;

        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
