using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Menu Music")]
    public AudioClip menuMusic;

    [Header("Safe Space Music")]
    public AudioClip[] safeSpaceMusic = new AudioClip[2];

    [Header("Dungeon Music")]
    public AudioClip[] dungeonMusic = new AudioClip[2];

    [Header("Boss Music")]
    public AudioClip bossMusic;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    public float fadeTime = 1.0f;

    //two audio sources for fading
    private AudioSource audioSource1;
    private AudioSource audioSource2;
    private bool usingSource1 = true;
    
    private Coroutine currentFadeCoroutine;
    private AudioClip currentlyRequestedClip;

    public enum SceneType { Menu, SafeSpace, Dungeon, Boss }
    private SceneType currentScene;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        audioSource1 = gameObject.AddComponent<AudioSource>();
        audioSource2 = gameObject.AddComponent<AudioSource>();

        audioSource1.loop = true;
        audioSource1.playOnAwake = false;
        audioSource1.volume = musicVolume;

        audioSource2.loop = true;
        audioSource2.playOnAwake = false;
        audioSource2.volume = 0;
    }

    private AudioSource CurrentSource => usingSource1 ? audioSource1 : audioSource2;

    private AudioSource NextSource => usingSource1 ? audioSource2 : audioSource1;

    public void SetScene(SceneType sceneType)
    {
        currentScene = sceneType;
        PlayMusicForCurrentScene();
    }
    
    public void CheckForBossDungeon()
    {
        if (currentScene == SceneType.Dungeon)
        {
            DungeonGenerator dungeonGen = FindObjectOfType<DungeonGenerator>();
            if (dungeonGen != null && dungeonGen.spawnedRooms != null)
            {
                bool isBossDungeon = false;
                
                //check if any spawned room is a boss room
                foreach (Room room in dungeonGen.spawnedRooms)
                {
                    if (room != null && room.spawnPoints != null && 
                        room.spawnPoints.roomType == RoomSpawnPoints.RoomType.Boss)
                    {
                        isBossDungeon = true;
                        break;
                    }
                }
                
                //if found a boss room, switch to boss music
                if (isBossDungeon)
                {
                    currentScene = SceneType.Boss;
                    PlayMusicForCurrentScene();
                }
            }
        }
    }

    private void PlayMusicForCurrentScene()
    {
        AudioClip clipToPlay = null;

        switch (currentScene)
        {
            case SceneType.Menu:
                clipToPlay = menuMusic;
                break;
            case SceneType.SafeSpace:
                clipToPlay = safeSpaceMusic.Length > 0 ? safeSpaceMusic[Random.Range(0, safeSpaceMusic.Length)] : null;
                break;
            case SceneType.Dungeon:
                clipToPlay = dungeonMusic.Length > 0 ? dungeonMusic[Random.Range(0, dungeonMusic.Length)] : null;
                break;
            case SceneType.Boss:
                clipToPlay = bossMusic;
                break;
        }

        if (clipToPlay != null)
        {
            FadeToTrack(clipToPlay);
        }
    }

    private void FadeToTrack(AudioClip newTrack)
    {
        currentlyRequestedClip = newTrack;
        
        //if the same track is playing, dont change
        if (CurrentSource.clip == newTrack && CurrentSource.isPlaying)
            return;
            
        //if in the middle of a fade, stop it
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        NextSource.clip = newTrack;
        NextSource.volume = 0;
        NextSource.Play();

        //start a fade coroutine and treack it
        currentFadeCoroutine = StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        float timeElapsed = 0;
        AudioClip targetClip = currentlyRequestedClip;
        
        //store the starting volumes to interpolate from
        float startVolume1 = CurrentSource.volume;
        float startVolume2 = NextSource.volume;

        while (timeElapsed < fadeTime)
        {
            //if the requested clip changed during this fade, exit early (can change scenes while fading)
            if (targetClip != currentlyRequestedClip)
            {
                yield break;
            }
            
            float t = timeElapsed / fadeTime;

            CurrentSource.volume = Mathf.Lerp(startVolume1, 0, t); //fade out
            NextSource.volume = Mathf.Lerp(startVolume2, musicVolume, t); //fade in

            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        
        if (CurrentSource.isPlaying)
        {
            CurrentSource.volume = musicVolume;
        }
    }
}