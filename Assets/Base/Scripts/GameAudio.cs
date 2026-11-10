using UnityEngine;

public class GameAudio : MonoBehaviour
{
    static GameAudio _instance;

    [SerializeField] AudioClip backMusic;
    [SerializeField] AudioClip holdSound;
    [SerializeField] AudioClip shootSound;
    [SerializeField] AudioClip popSound;
    [SerializeField] AudioClip winSound;
    [SerializeField] AudioClip lostSound;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip pathClearedSound;
    [SerializeField] float musicVolume = 0.09f;
    [SerializeField] float sfxVolume = 0.6f;
    [SerializeField] float shootVolume = 0.3f;
    [SerializeField] float popVolume = 0.15f;
    [SerializeField] float jumpVolume = 0.3f;
    [SerializeField] float winLoseVolume = 0.3f;
    [SerializeField] float pathClearedVolume = 0.3f;

    AudioSource _music;
    AudioSource _hold;
    AudioSource _oneShot;
    AudioSource _jump;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        _music = CreateSource(true, musicVolume);
        _hold = CreateSource(true, sfxVolume);
        _oneShot = CreateSource(false, 1f);
        _jump = CreateSource(false, 1f);
        if (backMusic == null)
            return;

        _music.clip = backMusic;
        _music.volume = musicVolume;
        _music.Play();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    AudioSource CreateSource(bool loop, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    public static void PlayHold()
    {
        if (_instance == null || _instance.holdSound == null || _instance._hold.isPlaying)
            return;

        _instance._hold.clip = _instance.holdSound;
        _instance._hold.pitch = 2f;
        _instance._hold.Play();
    }

    public static void StopHold()
    {
        if (_instance != null)
            _instance._hold.Stop();
    }

    public static void PlayShoot()
    {
        Play(_instance != null ? _instance.shootSound : null, _instance != null ? _instance.shootVolume : 0f);
    }

    public static void PlayPop()
    {
        Play(_instance != null ? _instance.popSound : null, _instance != null ? _instance.popVolume : 0f);
    }

    public static void PlayWin()
    {
        Play(_instance != null ? _instance.winSound : null, _instance != null ? _instance.winLoseVolume : 0f);
    }

    public static void PlayLost()
    {
        Play(_instance != null ? _instance.lostSound : null, _instance != null ? _instance.winLoseVolume : 0f);
    }

    public static void PlayJump()
    {
        if (_instance == null || _instance.jumpSound == null)
            return;

        _instance._jump.pitch = Random.Range(0.9f, 1.1f);
        _instance._jump.PlayOneShot(_instance.jumpSound, _instance.jumpVolume);
    }

    public static void PlayPathCleared()
    {
        Play(_instance != null ? _instance.pathClearedSound : null, _instance != null ? _instance.pathClearedVolume : 0f);
    }

    static void Play(AudioClip clip, float volume)
    {
        if (_instance != null && clip != null)
            _instance._oneShot.PlayOneShot(clip, volume);
    }
}
