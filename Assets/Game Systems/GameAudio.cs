using UnityEngine;

public class GameAudio : MonoBehaviour
{
    private static GameAudio instance;

    private AudioSource audioSource;
    private AudioClip throwSound;
    private AudioClip hitSound;
    private AudioClip buttonSound;
    private AudioClip failSound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null)
            return;

        GameObject audioObject = new GameObject("Game Audio");
        instance = audioObject.AddComponent<GameAudio>();
        DontDestroyOnLoad(audioObject);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        throwSound = Resources.Load<AudioClip>("Audio/SFX/Throw");
        hitSound = Resources.Load<AudioClip>("Audio/SFX/Hit");
        buttonSound = Resources.Load<AudioClip>("Audio/SFX/Button");
        failSound = Resources.Load<AudioClip>("Audio/SFX/Fail");
    }

    public static void PlayThrow()
    {
        Play(instance != null ? instance.throwSound : null, 0.65f);
    }

    public static void PlayHit()
    {
        Play(instance != null ? instance.hitSound : null, 0.75f);
    }

    public static void PlayButton()
    {
        Play(instance != null ? instance.buttonSound : null, 0.7f);
    }

    public static void PlayFail()
    {
        Play(instance != null ? instance.failSound : null, 0.8f);
    }

    static void Play(AudioClip clip, float volume)
    {
        if (instance != null && clip != null)
            instance.audioSource.PlayOneShot(clip, volume);
    }
}
