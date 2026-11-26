using UnityEngine;

public class SfxManager : MonoBehaviour
{
    public static SfxManager singleton;

    private AudioSource source;

    private AudioClip shotClip;
    private AudioClip hitWallClip;
    private AudioClip hitSteelClip;
    private AudioClip hitTankClip;
    private AudioClip explosionClip;
    private AudioClip beepClip;
    private AudioClip goClip;

    public static void EnsureExists()
    {
        if (singleton == null)
        {
            GameObject go = new GameObject("SfxManager");
            go.AddComponent<SfxManager>();
        }
    }

    void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        BuildClips();
    }

    void BuildClips()
    {
        shotClip = MakeNoise(0.12f, 0.45f, 900f);
        hitWallClip = MakeNoise(0.10f, 0.40f, 1200f);
        hitSteelClip = MakeTone(0.08f, 1400f, 0.35f);
        hitTankClip = MakeNoise(0.20f, 0.50f, 500f);
        explosionClip = MakeNoise(0.50f, 0.60f, 350f);
        beepClip = MakeTone(0.15f, 880f, 0.40f);
        goClip = MakeTone(0.35f, 660f, 0.50f);
    }

    // 带低通滤波和衰减包络的噪声
    AudioClip MakeNoise(float duration, float volume, float cutoff)
    {
        int rate = 44100;
        int samples = Mathf.CeilToInt(rate * duration);
        float[] data = new float[samples];
        float prev = 0f;
        float alpha = Mathf.Clamp(cutoff / rate, 0.01f, 0.5f);
        System.Random rng = new System.Random(20261125);

        for (int i = 0; i < samples; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, white, alpha);
            float env = Mathf.Pow(1f - (float)i / samples, 1.5f);
            data[i] = prev * volume * env;
        }

        AudioClip clip = AudioClip.Create("noise", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 带衰减包络的正弦音
    AudioClip MakeTone(float duration, float freq, float volume)
    {
        int rate = 44100;
        int samples = Mathf.CeilToInt(rate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Pow(1f - (float)i / samples, 1.2f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
        }

        AudioClip clip = AudioClip.Create("tone", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayShot(Vector3 pos)
    {
        Play(shotClip);
    }

    public void PlayHit(HitKind kind, Vector3 pos)
    {
        switch (kind)
        {
            case HitKind.Wall: Play(hitWallClip); break;
            case HitKind.Steel: Play(hitSteelClip); break;
            case HitKind.Tank: Play(hitTankClip); break;
            case HitKind.Base: Play(explosionClip); break;
        }
    }

    public void PlayExplosion()
    {
        Play(explosionClip);
    }

    public void PlayBeep()
    {
        Play(beepClip);
    }

    public void PlayGo()
    {
        Play(goClip);
    }

    void Play(AudioClip clip)
    {
        if (clip == null || source == null) return;
        source.PlayOneShot(clip);
    }
}
