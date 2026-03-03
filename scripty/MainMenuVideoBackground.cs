using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

/// <summary>
/// Spravuje přehrávání video pozadí v main menu
/// </summary>
public class MainMenuVideoBackground : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip backgroundVideo;
    [SerializeField] private RawImage videoDisplay; // UI element pro zobrazení videa
    
    [Header("Playback Settings")]
    [SerializeField] private bool loopVideo = true;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool muteAudio = false;
    [SerializeField] private float volume = 0.5f;
    
    [Header("Main Menu Integration")]
    [SerializeField] private GameObject mainMenuPanel; // Reference na main menu panel pro sledování aktivního stavu
    [SerializeField] private NightShiftManagement nightShiftManagement; // Reference na night shift management
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool isVideoPlaying = false;
    private bool isMainMenuActive = true;
    private RenderTexture videoRenderTexture;

    void Start()
    {
        InitializeVideoSystem();
        
        // Najdi NightShiftManagement pokud není přiřazen
        if (nightShiftManagement == null)
            nightShiftManagement = FindFirstObjectByType<NightShiftManagement>();
        
        if (playOnAwake && isMainMenuActive)
        {
            PlayVideo();
        }
    }

    void Update()
    {
        // Kontroluj stav main menu
        CheckMainMenuState();
    }

    void OnEnable()
    {
        if (videoPlayer != null && backgroundVideo != null)
        {
            PlayVideo();
        }
    }

    void OnDisable()
    {
        StopVideo();
    }

    /// <summary>
    /// Kontroluje stav main menu a ovládá video podle toho
    /// </summary>
    private void CheckMainMenuState()
    {
        bool shouldPlayVideo = false;
        
        // Zkontroluj různé způsoby detekce main menu
        if (mainMenuPanel != null)
        {
            shouldPlayVideo = mainMenuPanel.activeInHierarchy;
        }
        else if (nightShiftManagement != null)
        {
            shouldPlayVideo = nightShiftManagement.IsInMainMenu();
        }
        else
        {
            // Fallback - zkontroluj aktivní stav tohoto GameObject
            shouldPlayVideo = gameObject.activeInHierarchy;
        }
        
        // Ovládej video podle stavu menu
        if (shouldPlayVideo && !isVideoPlaying)
        {
            PlayVideo();
        }
        else if (!shouldPlayVideo && isVideoPlaying)
        {
            StopVideo();
        }
        
        isMainMenuActive = shouldPlayVideo;
    }

    /// <summary>
    /// Inicializuje video systém
    /// </summary>
    private void InitializeVideoSystem()
    {
        // Vytvoř VideoPlayer component pokud neexistuje
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
        }

        // Nastav VideoPlayer settings
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = loopVideo;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            // Vytvoř RenderTexture pro video
            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(1920, 1080, 0);
                videoPlayer.targetTexture = videoRenderTexture;
                
                // Přiřaď RenderTexture k UI Raw Image
                if (videoDisplay != null)
                {
                    videoDisplay.texture = videoRenderTexture;
                }
            }

            // Audio settings
            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                audioSource.mute = muteAudio;
                audioSource.volume = volume;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
            else
            {
                // Nastavit direct audio output
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            }

            // Přiřaď video clip
            if (backgroundVideo != null)
            {
                videoPlayer.clip = backgroundVideo;
            }

            // Event callbacks
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoLooped;
            videoPlayer.errorReceived += OnVideoError;
        }

        if (enableDebugLogs)
            Debug.Log("[MainMenuVideo] Video system initialized");
    }

    /// <summary>
    /// Spustí přehrávání videa
    /// </summary>
    public void PlayVideo()
    {
        if (videoPlayer == null || backgroundVideo == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[MainMenuVideo] Cannot play video - VideoPlayer or VideoClip is null");
            return;
        }

        if (!isVideoPlaying)
        {
            // Ujisti se, že video display je viditelný
            if (videoDisplay != null)
            {
                videoDisplay.gameObject.SetActive(true);
            }
            
            videoPlayer.Prepare();
            
            if (enableDebugLogs)
                Debug.Log("[MainMenuVideo] Starting video playback");
        }
    }

    /// <summary>
    /// Zastaví přehrávání videa
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null && isVideoPlaying)
        {
            videoPlayer.Stop();
            isVideoPlaying = false;
            
            // Skryj video display
            if (videoDisplay != null)
            {
                videoDisplay.gameObject.SetActive(false);
            }
            
            if (enableDebugLogs)
                Debug.Log("[MainMenuVideo] Video playback stopped");
        }
    }

    /// <summary>
    /// Pozastaví/obnoví přehrávání videa
    /// </summary>
    public void PauseVideo()
    {
        if (videoPlayer != null && isVideoPlaying)
        {
            videoPlayer.Pause();
            
            if (enableDebugLogs)
                Debug.Log("[MainMenuVideo] Video paused");
        }
    }

    /// <summary>
    /// Obnoví přehrávání pozastaveného videa
    /// </summary>
    public void ResumeVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[MainMenuVideo] Video resumed");
        }
    }

    /// <summary>
    /// Nastaví hlasitost videa
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        
        if (videoPlayer != null && videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[MainMenuVideo] Volume set to: {volume}");
    }

    /// <summary>
    /// Ztlumí/odtlumí audio videa
    /// </summary>
    public void ToggleMute()
    {
        muteAudio = !muteAudio;
        
        if (videoPlayer != null && videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.mute = muteAudio;
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[MainMenuVideo] Audio muted: {muteAudio}");
    }

    /// <summary>
    /// Veřejná metoda pro manuální aktivaci/deaktivaci videa (volaná z externích scriptů)
    /// </summary>
    public void SetVideoActive(bool active)
    {
        if (active && !isVideoPlaying)
        {
            PlayVideo();
        }
        else if (!active && isVideoPlaying)
        {
            StopVideo();
        }
    }

    // Event callbacks
    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
        isVideoPlaying = true;
        
        if (enableDebugLogs)
            Debug.Log("[MainMenuVideo] Video prepared and playing");
    }

    private void OnVideoLooped(VideoPlayer vp)
    {
        if (enableDebugLogs)
            Debug.Log("[MainMenuVideo] Video looped");
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        if (enableDebugLogs)
            Debug.LogError($"[MainMenuVideo] Video error: {message}");
    }

    // Public properties pro externí scripty
    public bool IsVideoPlaying => isVideoPlaying;
    public float VideoDuration => videoPlayer != null ? (float)videoPlayer.length : 0f;
    public float CurrentTime => videoPlayer != null ? (float)videoPlayer.time : 0f;
    public bool IsMainMenuActive => isMainMenuActive;

    void OnDestroy()
    {
        // Cleanup event callbacks
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoLooped;
            videoPlayer.errorReceived -= OnVideoError;
        }
        
        StopVideo();
        
        // Cleanup RenderTexture
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            videoRenderTexture = null;
        }
    }
}