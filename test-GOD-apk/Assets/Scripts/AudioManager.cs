using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager AudioInstance { get; private set; }

    public AudioMixer audioMixer;

    [Header("Global Audio Sources")]
    [SerializeField] public AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("Global Audio Clips")]
    public AudioClip mainMusic;
    public AudioClip coinSfx;
    public AudioClip coinMergeSfx;
    public AudioClip upgradeSfx;

    void Awake()
    {
        if (AudioInstance != null && AudioInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        AudioInstance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        PlayBGM(mainMusic);
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    public void PlayBGM(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }
}