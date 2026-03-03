using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AraniHourlyAI
{
    [Range(0, 20)] public int hour12AM = 0;
    [Range(0, 20)] public int hour1AM = 0;
    [Range(0, 20)] public int hour2AM = 0;
    [Range(0, 20)] public int hour3AM = 0;
    [Range(0, 20)] public int hour4AM = 0;
    [Range(0, 20)] public int hour5AM = 0;
    
    public int GetAIForHour(int hour)
    {
        switch (hour)
        {
            case 0: return hour12AM;
            case 1: return hour1AM;
            case 2: return hour2AM;
            case 3: return hour3AM;
            case 4: return hour4AM;
            case 5: return hour5AM;
            default: return 0;
        }
    }
}

[System.Serializable]
public class AraniNightConfiguration
{
    public string nightName;
    public AraniHourlyAI hourlyAI = new AraniHourlyAI();
}

public class AraniAI : MonoBehaviour
{
    [Header("=== ARANI AI SYSTEM ===")]
    [SerializeField] private bool enableAraniAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Advanced AI System")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private AraniNightConfiguration[] nightConfigurations = new AraniNightConfiguration[6];
    
    [Header("Camera Buttons (10 tlačítek - bez CAM 09)")]
    [SerializeField] private Button cameraButton1;
    [SerializeField] private Button cameraButton2;
    [SerializeField] private Button cameraButton3;
    [SerializeField] private Button cameraButton4;
    [SerializeField] private Button cameraButton5;
    [SerializeField] private Button cameraButton6;
    [SerializeField] private Button cameraButton7;
    [SerializeField] private Button cameraButton8;
    [SerializeField] private Button cameraButton10;
    [SerializeField] private Button cameraButton11;
    
    [Header("Additional Camera Buttons (pouze pro detekci změny kamery - BEZ spawnu Arani)")]
    [SerializeField] private Button cameraButton9; // CAM 09
    [SerializeField] private Button ventCameraButton1; // Vent CAM 01
    [SerializeField] private Button ventCameraButton2; // Vent CAM 02
    [SerializeField] private Button ventCameraButton3; // Vent CAM 03
    [SerializeField] private Button ventCameraButton4; // Vent CAM 04
    [SerializeField] private Button ventCameraButton5; // Vent CAM 05
    [SerializeField] private Button ventCameraButton6; // Vent CAM 06
    [SerializeField] private Button switchToCamsButton; // Přepnutí na normální kamery
    [SerializeField] private Button switchToVentCamsButton; // Přepnutí na ventilační kamery
    
    [Header("Arani Panels")]
    [SerializeField] private GameObject[] redEyePanels = new GameObject[5]; // 5 panelů s červenýma očima
    [SerializeField] private GameObject[] blueEyePanels = new GameObject[5]; // 5 panelů s modrýma očima
    
    [Header("Static Panels (10 kamer - bez CAM 09)")]
    [SerializeField] private GameObject staticPanelCAM01;
    [SerializeField] private GameObject staticPanelCAM02;
    [SerializeField] private GameObject staticPanelCAM03;
    [SerializeField] private GameObject staticPanelCAM04;
    [SerializeField] private GameObject staticPanelCAM05;
    [SerializeField] private GameObject staticPanelCAM06;
    [SerializeField] private GameObject staticPanelCAM07;
    [SerializeField] private GameObject staticPanelCAM08;
    [SerializeField] private GameObject staticPanelCAM10;
    [SerializeField] private GameObject staticPanelCAM11;
    
    [Header("UI References")]
    [SerializeField] private Button camsOnButton; // Detekce kanceláře
    [SerializeField] private Button camsOffButton; // Detekce kamer
    
    [Header("Audio System")]
    [SerializeField] private AudioSource camPunchDestroyingSource;
    [SerializeField] private AudioClip camPunchDestroyingClip;
    [SerializeField] private AudioSource staticSoundSource;
    [SerializeField] private AudioClip staticSoundClip;
    
    [Header("Menu Detection - AI=0")]
    [SerializeField] private GameObject[] menuPanels;
    
    [Header("System References")]
    [SerializeField] private CameraSystem cameraSystem;
    [SerializeField] private CameryVentSystem ventCameraSystem;
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private ResetNight resetNight;
    [SerializeField] private ThatcherAI thatcherAI;
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pausing
    [SerializeField] private ZofeyeAI zofeyeAI;
    [SerializeField] private SledgeAI sledgeAI;
    [SerializeField] private FenerirAI fenerirAI;
    [SerializeField] private OryxAIVerse3 oryxAI;
    [SerializeField] private DokkiAI dokkiAI;
    
    [Header("READ ONLY Status")]
    [SerializeField] [ReadOnly] private int currentEffectiveAI = 0;
    [SerializeField] [ReadOnly] private bool isAraniActive = false;
    [SerializeField] [ReadOnly] private bool isRedEye = false;
    [SerializeField] [ReadOnly] private bool isBlueEye = false;
    [SerializeField] [ReadOnly] private int activeRedPanelIndex = -1;
    [SerializeField] [ReadOnly] private int activeBluePanelIndex = -1;
    [SerializeField] [ReadOnly] private bool isOnCooldown = false;
    [SerializeField] [ReadOnly] private float cooldownTimer = 0f;
    [SerializeField] [ReadOnly] private float redEyeTimer = 0f;
    [SerializeField] [ReadOnly] private float blueEyeTimer = 0f;
    [SerializeField] [ReadOnly] private int currentCameraIndex = -1;
    [SerializeField] [ReadOnly] private int previousCameraIndex = -1;
    [SerializeField] [ReadOnly] private string systemStatus = "Waiting";
    
    [Header("Broken Cameras Status")]
    [SerializeField] [ReadOnly] private bool brokenCAM01 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM02 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM03 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM04 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM05 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM06 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM07 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM08 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM10 = false;
    [SerializeField] [ReadOnly] private bool brokenCAM11 = false;
    
    // Private variables
    private float currentRedEyeDestroyTime = 5f; // AI 1 = 5s, AI 20 = 1s
    private const float BLUE_EYE_SURVIVE_TIME = 3f; // Fixní 4 sekundy pro modré oči
    private const float COOLDOWN_DURATION = 10f; // 10 sekund cooldown
    private const float MIN_RED_EYE_TIME = 1f; // AI 20 = 1 sekunda
    private const float MAX_RED_EYE_TIME = 5f; // AI 1 = 5 sekund
    private Button[] allCameraButtons;
    private Button[] additionalCameraButtons; // Tlačítka pro detekci změny kamery (bez spawnu Arani)
    private int lastCheckedCameraForStatic = -1; // Pro tracking StaticSound
    private bool wasOnVentSystem = false; // Pro tracking přepnutí z ventilačních kamer
    private GameObject[] allStaticPanels;
    private Coroutine blueEyeFadeCoroutine = null;
    private const float BLUE_EYE_FADE_DURATION = 1f; // 1 sekunda fade-out
    private bool hasCameraSwitched = false; // Flag pro detekci přepnutí kamery během Arani útoku
    private int araniSpawnCameraIndex = -1; // Index kamery, kde se Arani původně objevila
    
    /// <summary>
    /// Konverze CameraSystem indexu na AraniAI index (kvůli chybějící CAM 09)
    /// CameraSystem: 0-7=CAM01-08, 8=CAM09, 9=CAM10, 10=CAM11
    /// AraniAI: 0-7=CAM01-08, 8=CAM10, 9=CAM11 (CAM09 chybí)
    /// </summary>
    int ConvertCameraSystemIndexToAraniIndex(int cameraSystemIndex)
    {
        if (cameraSystemIndex < 0) return -1;
        if (cameraSystemIndex <= 7) return cameraSystemIndex; // CAM 01-08 jsou stejné
        if (cameraSystemIndex == 8) return -1; // CAM 09 není v AraniAI broken cameras
        if (cameraSystemIndex == 9) return 8; // CAM 10
        if (cameraSystemIndex == 10) return 9; // CAM 11
        return -1;
    }
    
    void Start()
    {
        InitializeAraniAI();
    }
    
    void Update()
    {
        if (!enableAraniAI) return;
        
        // CHECK FOR WARDEN - PAUSE ALL TIMERS IF WARDEN IS ATTACKING
        if (wardenAI != null && wardenAI.IsWardenActive())
            return;
        
        UpdateEffectiveAI();
        
        if (isOnCooldown)
        {
            HandleCooldown();
        }
        
        if (isAraniActive)
        {
            HandleAraniTimer();
        }
        
        // Handle StaticSound pro zničené kamery
        HandleStaticSound();
    }
    
    #region Initialization
    
    void InitializeAraniAI()
    {
        Debug.Log("[AraniAI] Initializing Arani AI system");
        
        // Setup camera buttons array (10 kamer pro spawn Arani)
        allCameraButtons = new Button[]
        {
            cameraButton1, cameraButton2, cameraButton3, cameraButton4,
            cameraButton5, cameraButton6, cameraButton7, cameraButton8,
            cameraButton10, cameraButton11
        };
        
        // Setup additional camera buttons (pouze pro detekci změny kamery - BEZ spawnu Arani)
        additionalCameraButtons = new Button[]
        {
            cameraButton9, // CAM 09
            ventCameraButton1, ventCameraButton2, ventCameraButton3, // Vent CAM 01-03
            ventCameraButton4, ventCameraButton5, ventCameraButton6, // Vent CAM 04-06
            switchToCamsButton, switchToVentCamsButton // Přepínací tlačítka
        };
        
        // Add listeners to all camera buttons (10 kamer pro spawn)
        for (int i = 0; i < allCameraButtons.Length; i++)
        {
            if (allCameraButtons[i] != null)
            {
                int buttonIndex = i; // Capture index for closure
                allCameraButtons[i].onClick.AddListener(() => OnCameraButtonClicked(buttonIndex));
            }
        }
        
        // Add listeners to additional camera buttons (pouze pro detekci změny kamery)
        for (int i = 0; i < additionalCameraButtons.Length; i++)
        {
            if (additionalCameraButtons[i] != null)
            {
                int buttonIndex = -1000 - i; // Speciální negativní index pro rozlišení
                additionalCameraButtons[i].onClick.AddListener(() => OnAdditionalCameraButtonClicked(buttonIndex));
            }
        }
        
        // Add listener to CamsOff button (slezení z kamer)
        if (camsOffButton != null)
        {
            camsOffButton.onClick.AddListener(OnCamsOffButtonClicked);
        }
        
        // Add listener to CamsOn button (otevření kamer)
        if (camsOnButton != null)
        {
            camsOnButton.onClick.AddListener(OnCamsOnButtonClicked);
        }
        
        // Setup all static panels array (10 kamer)
        allStaticPanels = new GameObject[]
        {
            staticPanelCAM01, staticPanelCAM02, staticPanelCAM03, staticPanelCAM04,
            staticPanelCAM05, staticPanelCAM06, staticPanelCAM07, staticPanelCAM08,
            staticPanelCAM10, staticPanelCAM11
        };
        
        // Deactivate all Arani panels
        DeactivateAllAraniPanels();
        
        // Deactivate all static panels
        DeactivateAllStaticPanels();
        
        // Setup StaticSound - loop ale vypnutý na začátku, hlasitost 30%
        if (staticSoundSource != null && staticSoundClip != null)
        {
            staticSoundSource.clip = staticSoundClip;
            staticSoundSource.loop = true;
            staticSoundSource.volume = 0.3f; // 30% hlasitost
            staticSoundSource.Stop();
        }
        
        Debug.Log("[AraniAI] Arani AI initialized - Ready to start");
    }
    
    #endregion
    
    #region Core Update Systems
    
    void UpdateEffectiveAI()
    {
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            // NOVÉ: Pokud se menu právě otevřelo, resetuj Arani
            if (currentEffectiveAI > 0 || isAraniActive)
            {
                Debug.Log("[AraniAI] Menu opened - triggering full reset");
                ResetDestroyedCameras("Menu Opened");
            }
            
            currentEffectiveAI = 0;
            systemStatus = "Menu Active - AI Paused";
            return;
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("ARANI");
            if (customAI >= 0)
            {
                currentEffectiveAI = customAI;
                systemStatus = $"Custom Night: AI {currentEffectiveAI}";
                UpdateRedEyeDestroyTime();
                return;
            }
        }
        
        if (useAdvancedAISystem && shiftTimer != null && nightManager != null)
        {
            int currentHour = shiftTimer.GetCurrentHour();
            int currentNight = nightManager.GetCurrentNight();
            
            if (currentNight >= 1 && currentNight <= 6 && nightConfigurations[currentNight - 1] != null)
            {
                currentEffectiveAI = nightConfigurations[currentNight - 1].hourlyAI.GetAIForHour(currentHour);
                systemStatus = $"Advanced AI: Night {currentNight}, Hour {currentHour}AM, AI {currentEffectiveAI}";
            }
        }
        else
        {
            currentEffectiveAI = manualAILevel;
            systemStatus = $"Manual AI: {currentEffectiveAI}";
        }
        
        UpdateRedEyeDestroyTime();
    }
    
    void UpdateRedEyeDestroyTime()
    {
        if (currentEffectiveAI <= 0)
        {
            currentRedEyeDestroyTime = MAX_RED_EYE_TIME;
            return;
        }
        
        // Linear interpolation: AI 1 = 5s, AI 20 = 1s
        float aiNormalized = Mathf.Clamp01((currentEffectiveAI - 1f) / 19f);
        currentRedEyeDestroyTime = Mathf.Lerp(MAX_RED_EYE_TIME, MIN_RED_EYE_TIME, aiNormalized);
    }
    
    #endregion
    
    #region Camera Button Click Handler
    
    void OnCameraButtonClicked(int buttonIndex)
    {
        Debug.Log($"[AraniAI] Camera button {buttonIndex} clicked");
        
        // Aktualizuj předchozí a současnou kameru
        previousCameraIndex = currentCameraIndex;
        currentCameraIndex = buttonIndex;
        
        // Pokud je Arani aktivní, označíme že hráč přepnul kameru
        if (isAraniActive)
        {
            // OPRAVA: Zkontroluj jestli to OPRAVDU je jiná kamera
            if (previousCameraIndex != buttonIndex)
            {
                hasCameraSwitched = true;
                Debug.Log("[AraniAI] Player switched camera during active Arani!");
                
                // KRITICKÉ: Okamžitě deaktivuj Arani panel při přepnutí kamery
                DeactivateAllAraniPanels();
                Debug.Log("[AraniAI] Arani panels immediately deactivated - Arani stays on original camera");
            }
            else
            {
                Debug.Log("[AraniAI] Player clicked same camera - not counted as switch");
            }
            
            return; // Nepokračuj ve spawnu nové Arani
        }
        
        // Kontrola blokačních podmínek
        if (!CanAraniAppear())
        {
            Debug.Log($"[AraniAI] Arani cannot appear - blocked conditions");
            return;
        }
        
        // Pokud hráč kliknul na stejnou kameru, blokuj Arani
        if (previousCameraIndex == currentCameraIndex)
        {
            Debug.Log($"[AraniAI] Same camera clicked ({currentCameraIndex}) - Arani blocked");
            return;
        }
        
        // NOVÁ KONTROLA: Pokud je současná kamera rozbitá, blokuj Arani spawn
        if (IsCameraBroken(currentCameraIndex))
        {
            Debug.Log($"[AraniAI] Camera {currentCameraIndex} is already broken - Arani spawn blocked");
            return;
        }
        
        // Trigger 1:3 šance
        AttemptAraniSpawn();
    }
    
    /// <summary>
    /// Handler pro dodatečná tlačítka (CAM 09, Vent kamery, přepínací tlačítka)
    /// Pouze detekuje změnu kamery, NESPAWNUJE Arani
    /// </summary>
    void OnAdditionalCameraButtonClicked(int buttonIndex)
    {
        Debug.Log($"[AraniAI] Additional camera button {buttonIndex} clicked (no spawn)");
        
        // Aktualizuj předchozí a současnou kameru
        previousCameraIndex = currentCameraIndex;
        currentCameraIndex = buttonIndex; // Speciální negativní index
        
        // Speciální handling pro přepínač na normální kamery (-1007 = switchToCamsButton)
        if (buttonIndex == -1007 && cameraSystem != null)
        {
            // Při přepnutí na normální kamery aktualizuj index z CameraSystem
            int actualCameraIndex = cameraSystem.GetCurrentCameraIndex();
            currentCameraIndex = actualCameraIndex;
            Debug.Log($"[AraniAI] Switched to normal cameras - current camera set to: {currentCameraIndex}");
        }
        
        // Pokud je Arani aktivní, označíme že hráč přepnul kameru
        if (isAraniActive)
        {
            hasCameraSwitched = true;
            Debug.Log("[AraniAI] Player switched to additional camera during active Arani!");
            
            // KRITICKÉ: Okamžitě deaktivuj Arani panel při přepnutí kamery
            DeactivateAllAraniPanels();
            Debug.Log("[AraniAI] Arani panels immediately deactivated due to camera switch");
        }
        
        // NEPOVOLUJ spawn Arani na těchto tlačítkách - pouze detekce změny
    }
    
    /// <summary>
    /// Handler pro CamsOff tlačítko (slezení z kamer)
    /// Deaktivuje Arani panely když hráč opustí kamery
    /// </summary>
    void OnCamsOffButtonClicked()
    {
        Debug.Log("[AraniAI] CamsOff button clicked - player leaving cameras");
        
        // Pokud je Arani aktivní, deaktivuj panely
        if (isAraniActive)
        {
            hasCameraSwitched = true; // Považujeme to za změnu kamery
            Debug.Log("[AraniAI] Player left cameras during active Arani!");
            
            // Deaktivuj Arani panely
            DeactivateAllAraniPanels();
            Debug.Log("[AraniAI] Arani panels deactivated - player left cameras");
        }
        
        // Reset current camera index
        previousCameraIndex = currentCameraIndex;
        currentCameraIndex = -1; // -1 = žádná kamera (hráč v kanceláři)
    }
    
    void OnCamsOnButtonClicked()
    {
        Debug.Log("[AraniAI] CamsOn button clicked - updating current camera index from CameraSystem");
        
        // Získej aktuální kameru z CameraSystem
        if (cameraSystem != null)
        {
            int currentCam = cameraSystem.GetCurrentCameraIndex();
            previousCameraIndex = currentCameraIndex;
            currentCameraIndex = currentCam;
            Debug.Log($"[AraniAI] Cameras opened - current camera set to: {currentCameraIndex}");
        }
        else
        {
            Debug.LogWarning("[AraniAI] CameraSystem reference is null - cannot update current camera index");
        }
    }
    
    #endregion
    
    #region Spawn System
    
    bool CanAraniAppear()
    {
        // AI = 0 -> blokováno
        if (currentEffectiveAI <= 0)
        {
            Debug.Log("[AraniAI] AI = 0 - Arani blocked");
            return false;
        }
        
        // Cooldown aktivní -> blokováno
        if (isOnCooldown)
        {
            Debug.Log($"[AraniAI] Cooldown active ({cooldownTimer:F1}s remaining) - Arani blocked");
            return false;
        }
        
        // Hráč není na kamerách -> blokováno
        if (!IsPlayerOnCameras())
        {
            Debug.Log("[AraniAI] Player not on cameras - Arani blocked");
            return false;
        }
        
        // CamsOn button aktivní (hráč v kanceláři) -> blokováno
        if (camsOnButton != null && camsOnButton.gameObject.activeSelf)
        {
            Debug.Log("[AraniAI] CamsOn button active - Arani blocked");
            return false;
        }
        
        return true;
    }
    
    bool IsPlayerOnCameras()
    {
        // Pokud je CamsOff tlačítko aktivní, hráč je na kamerách
        if (camsOffButton != null && camsOffButton.gameObject.activeSelf)
        {
            return true;
        }
        
        return false;
    }
    
    void AttemptAraniSpawn()
    {
        // 1:3 šance - 33% červené, 33% modré, 33% nic
        int randomChance = Random.Range(0, 3);
        
        Debug.Log($"[AraniAI] ===== SPAWN ATTEMPT - Roll: {randomChance} =====");
        
        switch (randomChance)
        {
            case 0: // Červené oči
                SpawnRedEyeArani();
                break;
                
            case 1: // Modré oči
                SpawnBlueEyeArani();
                break;
                
            case 2: // Nic se nestane
                Debug.Log("[AraniAI] No spawn - player is safe this time");
                break;
        }
    }
    
    void SpawnRedEyeArani()
    {
        // Náhodný panel z červených
        int randomIndex = Random.Range(0, redEyePanels.Length);
        
        if (redEyePanels[randomIndex] != null)
        {
            Debug.Log($"[AraniAI] RED EYE ARANI SPAWNED - Panel {randomIndex}");
            Debug.Log($"[AraniAI] Player has {currentRedEyeDestroyTime:F1}s to switch/close camera!");
            
            DeactivateAllAraniPanels();
            redEyePanels[randomIndex].SetActive(true);
            
            isAraniActive = true;
            isRedEye = true;
            isBlueEye = false;
            activeRedPanelIndex = randomIndex;
            activeBluePanelIndex = -1;
            redEyeTimer = 0f;
            hasCameraSwitched = false; // Reset flag
            araniSpawnCameraIndex = currentCameraIndex; // Ulož kameru, kde se Arani objevila
            
            systemStatus = $"RED EYE Active - {currentRedEyeDestroyTime:F1}s to escape!";
        }
    }
    
    void SpawnBlueEyeArani()
    {
        // Náhodný panel z modrých
        int randomIndex = Random.Range(0, blueEyePanels.Length);
        
        if (blueEyePanels[randomIndex] != null)
        {
            Debug.Log($"[AraniAI] BLUE EYE ARANI SPAWNED - Panel {randomIndex}");
            Debug.Log($"[AraniAI] Player must watch for {BLUE_EYE_SURVIVE_TIME}s!");
            
            DeactivateAllAraniPanels();
            blueEyePanels[randomIndex].SetActive(true);
            
            isAraniActive = true;
            isRedEye = false;
            isBlueEye = true;
            activeRedPanelIndex = -1;
            activeBluePanelIndex = randomIndex;
            blueEyeTimer = 0f;
            hasCameraSwitched = false; // Reset flag
            araniSpawnCameraIndex = currentCameraIndex; // Ulož kameru, kde se Arani objevila
            
            systemStatus = $"BLUE EYE Active - Watch for {BLUE_EYE_SURVIVE_TIME}s!";
        }
    }
    
    #endregion
    
    #region Timer Handlers
    
    void HandleAraniTimer()
    {
        if (isRedEye)
        {
            HandleRedEyeTimer();
        }
        else if (isBlueEye)
        {
            HandleBlueEyeTimer();
        }
    }
    
    void HandleRedEyeTimer()
    {
        redEyeTimer += Time.deltaTime;
        
        // Kontrola změny kamery nebo zavření kamer
        if (!IsPlayerOnCameras() || HasPlayerSwitchedCamera())
        {
            OnPlayerSurvived();
            return;
        }
        
        // Pokud uplynul čas, zničí kameru
        if (redEyeTimer >= currentRedEyeDestroyTime)
        {
            DestroyCameraByArani();
        }
    }
    
    void HandleBlueEyeTimer()
    {
        blueEyeTimer += Time.deltaTime;
        
        // Kontrola změny kamery nebo zavření kamer -> NEÚSPĚCH
        if (!IsPlayerOnCameras() || HasPlayerSwitchedCamera())
        {
            Debug.Log("[AraniAI] Player switched/closed camera during BLUE EYE - CAMERA DESTROYED!");
            DestroyCameraByArani();
            return;
        }
        
        // Pokud hráč sledoval 4 sekundy -> ÚSPĚCH
        if (blueEyeTimer >= BLUE_EYE_SURVIVE_TIME)
        {
            OnPlayerSurvived();
        }
    }
    
    bool HasPlayerSwitchedCamera()
    {
        return hasCameraSwitched;
    }
    
    #endregion
    
    #region Camera Destruction
    
    void DestroyCameraByArani()
    {
        // Použij PŮVODNÍ kameru, kde se Arani objevila, ne současnou!
        int cameraToDestroy = araniSpawnCameraIndex;
        
        Debug.Log($"[AraniAI] ===== CAMERA {cameraToDestroy} DESTROYED BY ARANI =====");
        Debug.Log($"[AraniAI] Arani spawned on camera {araniSpawnCameraIndex}, current camera is {currentCameraIndex}");
        
        // Označ konkrétní kameru jako zničenou
        SetCameraAsBroken(cameraToDestroy, true);
        
        // Přehraj zvuk zničení
        if (camPunchDestroyingSource != null && camPunchDestroyingClip != null)
        {
            camPunchDestroyingSource.PlayOneShot(camPunchDestroyingClip);
        }
        
        // Deaktivuj Arani panel
        DeactivateAllAraniPanels();
        
        // Aktivuj StaticPanel pro tuto konkrétní kameru
        ShowStaticPanelForCamera(cameraToDestroy);
        
        // Reset stavu
        isAraniActive = false;
        isRedEye = false;
        isBlueEye = false;
        activeRedPanelIndex = -1;
        activeBluePanelIndex = -1;
        redEyeTimer = 0f;
        blueEyeTimer = 0f;
        hasCameraSwitched = false;
        araniSpawnCameraIndex = -1;
        
        // Spusť cooldown
        StartCooldown();
        
        systemStatus = $"Camera {cameraToDestroy} destroyed - Cooldown started";
    }
    
    void SetCameraAsBroken(int cameraIndex, bool broken)
    {
        switch (cameraIndex)
        {
            case 0: brokenCAM01 = broken; break;
            case 1: brokenCAM02 = broken; break;
            case 2: brokenCAM03 = broken; break;
            case 3: brokenCAM04 = broken; break;
            case 4: brokenCAM05 = broken; break;
            case 5: brokenCAM06 = broken; break;
            case 6: brokenCAM07 = broken; break;
            case 7: brokenCAM08 = broken; break;
            case 8: brokenCAM10 = broken; break;
            case 9: brokenCAM11 = broken; break;
        }
        Debug.Log($"[AraniAI] Camera {cameraIndex} broken status set to: {broken}");
    }
    
    bool IsCameraBroken(int cameraIndex)
    {
        switch (cameraIndex)
        {
            case 0: return brokenCAM01;
            case 1: return brokenCAM02;
            case 2: return brokenCAM03;
            case 3: return brokenCAM04;
            case 4: return brokenCAM05;
            case 5: return brokenCAM06;
            case 6: return brokenCAM07;
            case 7: return brokenCAM08;
            case 8: return brokenCAM10;
            case 9: return brokenCAM11;
            default: return false;
        }
    }
    
    void ShowStaticPanelForCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= allStaticPanels.Length)
            return;
            
        if (allStaticPanels[cameraIndex] != null)
        {
            allStaticPanels[cameraIndex].SetActive(true);
            Debug.Log($"[AraniAI] StaticPanel activated for camera {cameraIndex}");
        }
    }
    
    void HideStaticPanelForCamera(int cameraIndex)
    {
        if (cameraIndex < 0 || cameraIndex >= allStaticPanels.Length)
            return;
            
        if (allStaticPanels[cameraIndex] != null)
        {
            allStaticPanels[cameraIndex].SetActive(false);
        }
    }
    
    public bool IsCameraDestroyed(int cameraIndex)
    {
        return IsCameraBroken(cameraIndex);
    }
    
    #endregion
    
    #region Survival & Cooldown
    
    void OnPlayerSurvived()
    {
        if (isRedEye)
        {
            Debug.Log("[AraniAI] ✅ Player survived RED EYE - switched/closed camera in time!");
            
            // Červené oči - okamžitě zmizí
            DeactivateAllAraniPanels();
            hasCameraSwitched = false; // Reset flag
            CompleteAraniReset();
        }
        else if (isBlueEye)
        {
            Debug.Log("[AraniAI] ✅ Player survived BLUE EYE - watched for 4 seconds!");
            
            // Modré oči - fade-out efekt (ale musíme nastavit isAraniActive = false aby cooldown začal)
            if (activeBluePanelIndex >= 0 && activeBluePanelIndex < blueEyePanels.Length)
            {
                GameObject activeBluePanel = blueEyePanels[activeBluePanelIndex];
                if (activeBluePanel != null)
                {
                    // Spusť fade-out coroutine
                    if (blueEyeFadeCoroutine != null)
                    {
                        StopCoroutine(blueEyeFadeCoroutine);
                    }
                    
                    // Okamžitě nastav isAraniActive = false aby cooldown začal
                    isAraniActive = false;
                    hasCameraSwitched = false; // Reset flag
                    
                    blueEyeFadeCoroutine = StartCoroutine(FadeOutBlueEye(activeBluePanel));
                }
            }
            else
            {
                // Fallback pokud panel neexistuje
                DeactivateAllAraniPanels();
                hasCameraSwitched = false;
                CompleteAraniReset();
            }
        }
    }
    
    void CompleteAraniReset()
    {
        // Reset stavu
        isAraniActive = false;
        isRedEye = false;
        isBlueEye = false;
        activeRedPanelIndex = -1;
        activeBluePanelIndex = -1;
        redEyeTimer = 0f;
        blueEyeTimer = 0f;
        hasCameraSwitched = false;
        
        // Spusť cooldown
        StartCooldown();
        
        systemStatus = "Player survived - Cooldown started";
    }
    
    IEnumerator FadeOutBlueEye(GameObject blueEyePanel)
    {
        Debug.Log("[AraniAI] Starting fade-out effect for blue eye panel");
        
        // NEJDŘÍVE spusť cooldown (před fade-out)
        StartCooldown();
        
        // Získej nebo přidej CanvasGroup komponentu
        CanvasGroup canvasGroup = blueEyePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = blueEyePanel.AddComponent<CanvasGroup>();
        }
        
        float elapsedTime = 0f;
        
        // Fade z 100% na 0% za 1 sekundu
        while (elapsedTime < BLUE_EYE_FADE_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / BLUE_EYE_FADE_DURATION);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        
        // Ujisti se, že je alpha na 0%
        canvasGroup.alpha = 0f;
        
        Debug.Log("[AraniAI] Fade-out complete - deactivating blue eye panel");
        
        // Deaktivuj panel
        blueEyePanel.SetActive(false);
        
        // Resetuj alpha zpět na 100% pro příští použití
        canvasGroup.alpha = 1f;
        
        // Reset stavů (ale BEZ spuštění cooldownu - už běží)
        isRedEye = false;
        isBlueEye = false;
        activeRedPanelIndex = -1;
        activeBluePanelIndex = -1;
        redEyeTimer = 0f;
        blueEyeTimer = 0f;
        araniSpawnCameraIndex = -1;
        
        systemStatus = "Player survived - Cooldown started";
        
        blueEyeFadeCoroutine = null;
    }
    
    void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = COOLDOWN_DURATION;
        Debug.Log($"[AraniAI] Cooldown started - {COOLDOWN_DURATION}s");
    }
    
    void HandleCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        
        if (cooldownTimer <= 0f)
        {
            isOnCooldown = false;
            cooldownTimer = 0f;
            Debug.Log("[AraniAI] Cooldown ended - Arani can spawn again");
            systemStatus = "Ready - Arani can spawn";
        }
    }
    
    #endregion
    
    #region Static Sound Management
    
    void HandleStaticSound()
    {
        // KRITICKÁ KONTROLA - nejdříve VŽDY zastav StaticSound
        if (staticSoundSource != null && staticSoundSource.isPlaying)
        {
            staticSoundSource.Stop();
            Debug.Log("[AraniAI] StaticSound force stopped at start of check");
        }
        
        // Skryj všechny static panely na začátku
        DeactivateAllStaticPanels();
        
        // DETEKCE KANCELÁŘE - pokud hráč není na kamerách, KONEC
        bool isOnCameras = (camsOffButton != null && camsOffButton.gameObject.activeSelf);
        
        if (!isOnCameras)
        {
            
            lastCheckedCameraForStatic = -1;
            wasOnVentSystem = false;
            return;
        }
        
        // DETEKCE VENTILAČNÍHO SYSTÉMU - pokud hráč je na ventilačních kamerách, KONEC
        bool isOnVentSystemNow = (ventCameraSystem != null && ventCameraSystem.AreVentCamerasOpen());
        
        if (isOnVentSystemNow)
        {
            Debug.Log("[AraniAI] Player on ventilation system - StaticPanel blocked");
            lastCheckedCameraForStatic = -1;
            wasOnVentSystem = true; // Zapamatuj si, že hráč byl na ventilačním systému
            return;
        }
        
        // DETEKCE NÁVRATU Z VENTILAČNÍHO SYSTÉMU - pokud hráč přepnul zpět, resetuj tracking
        if (!isOnVentSystemNow && wasOnVentSystem)
        {
            Debug.Log("[AraniAI] Player returned from ventilation system to normal cameras - forcing static check");
            wasOnVentSystem = false;
            lastCheckedCameraForStatic = -1; // Resetuj tracking aby se znovu vyhodnotil panel
        }
        
        // Získej aktuální index kamery přímo z CameraSystem a konvertuj na AraniAI mapping
        int actualCameraIndex = -1;
        if (cameraSystem != null)
        {
            int cameraSystemIndex = cameraSystem.GetCurrentCameraIndex();
            actualCameraIndex = ConvertCameraSystemIndexToAraniIndex(cameraSystemIndex);
            Debug.Log($"[AraniAI] CameraSystem index {cameraSystemIndex} converted to AraniAI index {actualCameraIndex}");
        }
        
        // Hráč JE na kamerách - zkontroluj současnou kameru
        if (actualCameraIndex < 0)
        {
            Debug.Log("[AraniAI] Invalid camera index - StaticSound blocked");
            lastCheckedCameraForStatic = -1;
            return;
        }
        
        // Zkontroluj jestli je SOUČASNÁ kamera zničená
        bool isCurrentCameraBroken = IsCameraBroken(actualCameraIndex);
        
        Debug.Log($"[AraniAI] Camera {actualCameraIndex} check - broken: {isCurrentCameraBroken}");
        
        // Aktualizuj tracking
        lastCheckedCameraForStatic = actualCameraIndex;
        
        // POUZE pokud je současná kamera zničená, spusť zvuk a panel
        if (isCurrentCameraBroken)
        {
            if (staticSoundSource != null && staticSoundClip != null)
            {
                staticSoundSource.Play();
                Debug.Log($"[AraniAI] StaticSound STARTED - camera {actualCameraIndex} IS DESTROYED");
            }
            
            ShowStaticPanelForCamera(actualCameraIndex);
        }
        else
        {
            Debug.Log($"[AraniAI] Camera {actualCameraIndex} is NOT destroyed - no sound/panel");
        }
    }
    
    #endregion
    
    #region Panel Management
    
    void DeactivateAllAraniPanels()
    {
        // Deaktivuj všechny červené panely
        foreach (GameObject panel in redEyePanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        // Deaktivuj všechny modré panely a resetuj jejich alpha
        foreach (GameObject panel in blueEyePanels)
        {
            if (panel != null)
            {
                // Resetuj alpha na 100% před deaktivací
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
                panel.SetActive(false);
            }
        }
    }
    
    void DeactivateAllStaticPanels()
    {
        if (allStaticPanels != null)
        {
            foreach (GameObject panel in allStaticPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }
    }
    
    #endregion
    
    #region Reset System
    
    public void ResetDestroyedCameras(string reason)
    {
        Debug.Log($"[AraniAI] ===== RESETTING DESTROYED CAMERAS - Reason: {reason} =====");
        
        // Resetuj všechny broken stavy na false (10 kamer)
        for (int i = 0; i < 10; i++)
        {
            SetCameraAsBroken(i, false);
        }
        
        // Deaktivuj všechny StaticPanels
        DeactivateAllStaticPanels();
        
        // Zastav StaticSound
        if (staticSoundSource != null && staticSoundSource.isPlaying)
        {
            staticSoundSource.Stop();
            Debug.Log("[AraniAI] StaticSound stopped - cameras reset");
        }
        lastCheckedCameraForStatic = -1;
        
        // Deaktivuj Arani
        DeactivateAllAraniPanels();
        
        // Reset všech stavů
        isAraniActive = false;
        isRedEye = false;
        isBlueEye = false;
        activeRedPanelIndex = -1;
        activeBluePanelIndex = -1;
        redEyeTimer = 0f;
        blueEyeTimer = 0f;
        isOnCooldown = false;
        cooldownTimer = 0f;
        currentCameraIndex = -1;
        previousCameraIndex = -1;
        hasCameraSwitched = false;
        araniSpawnCameraIndex = -1;
        
        systemStatus = "Reset complete - All cameras restored";
        
        Debug.Log("[AraniAI] All cameras restored and Arani reset");
    }
    
    #endregion
    
    #region Menu Detection
    
    bool IsMenuActive()
    {
        if (menuPanels != null)
        {
            foreach (GameObject menuPanel in menuPanels)
            {
                if (menuPanel != null && menuPanel.activeSelf)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    #endregion
    
    #region Public Interface
    
    public int GetEffectiveAI()
    {
        return currentEffectiveAI;
    }
    
    public bool IsActive()
    {
        return isAraniActive;
    }
    
    public List<int> GetDestroyedCameras()
    {
        List<int> destroyedList = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            if (IsCameraBroken(i))
            {
                destroyedList.Add(i);
            }
        }
        return destroyedList;
    }
    
    #endregion
    
    #region Debug
    
    void OnValidate()
    {
        // Ensure night configurations array has 6 elements
        if (nightConfigurations == null || nightConfigurations.Length != 6)
        {
            nightConfigurations = new AraniNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                if (nightConfigurations[i] == null)
                {
                    nightConfigurations[i] = new AraniNightConfiguration();
                    nightConfigurations[i].nightName = $"Night {i + 1}";
                }
            }
        }
        
        // Ensure panel arrays have correct size
        if (redEyePanels == null || redEyePanels.Length != 5)
        {
            System.Array.Resize(ref redEyePanels, 5);
        }
        
        if (blueEyePanels == null || blueEyePanels.Length != 5)
        {
            System.Array.Resize(ref blueEyePanels, 5);
        }
        
        // Ensure menuPanels array is not null
        if (menuPanels == null)
        {
            menuPanels = new GameObject[0];
        }
    }
    
    #endregion
}
