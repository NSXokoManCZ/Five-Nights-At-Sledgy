using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OryxHourlyAI
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

    public void SetAIForHour(int hour, int aiLevel)
    {
        aiLevel = Mathf.Clamp(aiLevel, 0, 20);
        switch (hour)
        {
            case 0: hour12AM = aiLevel; break;
            case 1: hour1AM = aiLevel; break;
            case 2: hour2AM = aiLevel; break;
            case 3: hour3AM = aiLevel; break;
            case 4: hour4AM = aiLevel; break;
            case 5: hour5AM = aiLevel; break;
        }
    }
}

[System.Serializable]
public class OryxNightConfiguration
{
    public string nightName;
    public OryxHourlyAI hourlyAI = new OryxHourlyAI();
}

public class OryxAIVerse3 : MonoBehaviour
{
    [Header("=== ORAX AI SYSTEM ===")]
    [SerializeField] private bool enableOraxAI = true; // Enable Orax AI - Only programmer can control via Inspector
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0; // Manual AI - funguje pouze když useAdvancedAISystem = false
    [SerializeField] private float movementInterval = 15f; // Normal: 15 seconds
    [SerializeField] private float aggressiveInterval = 5f; // Aggressive: 5 seconds (power outage)
    
    [Header("Advanced AI System")]
    [SerializeField] private bool useAdvancedAISystem = false; // Zapíná advanced AI systém jako u Zofaye
    [SerializeField] private bool enableRealTimeAIUpdates = true; // Automatické AI aktualizace podle času
    [SerializeField] private float aiUpdateInterval = 1f; // Jak často kontrolovat AI aktualizace
    [SerializeField] private OryxNightConfiguration[] aiSettings = new OryxNightConfiguration[6]; // AI nastavení pro všech 6 nocí
    [SerializeField] private bool allowExternalAIControl = false; // Povolit externím scriptům nastavení AI
    
    [Header("READ ONLY - Current Status")]
    [SerializeField] private int currentAILevel = 0;
    [SerializeField] private int currentNight = 1;
    [SerializeField] private int currentHour = 0;
    [SerializeField] private float currentAIChance = 0f;
    [SerializeField] private bool usingManualAI = false;
    [SerializeField] private string systemStatusDisplay = "Advanced AI System";
    [SerializeField] private string currentAISourceDisplay = "Manual AI";
    
    [Header("UI References")]
    [SerializeField] private GameObject deadPanelOrax;
    [Header("Other Characters Death Detection")]
    [SerializeField] private GameObject[] otherCharacterDeadPanels; // Dead panely jiných animatroniků pro detekci smrti
    [SerializeField] private bool enableOtherCharacterDeathDetection = true; // Zapnout/vypnout detekci smrti jiných postav
    
    [Header("Direct Character AI References - UNIVERSAL RESET SYSTEM")]
    [SerializeField] private MonoBehaviour zofayeAI; // Zofaye AI script reference
    [SerializeField] private MonoBehaviour fereneriAI; // Fereneri AI script reference
    [SerializeField] private MonoBehaviour thatcherAI; // Thatcher AI script reference
    [SerializeField] private bool enableUniversalResetSystem = true; // Univerzální reset systém
    [SerializeField] private GameObject movingPanelOrax;
    [SerializeField] private GameObject camsOnButton;
    [SerializeField] private GameObject camsOffButton;
    
    [Header("Menu Panels for AI=0 Detection")]
    [SerializeField] private GameObject[] menuPanels;
    
    [Header("System References")]
    [SerializeField] private VentilaceLaseryDvere ventilationLasers;
    [SerializeField] private CameraSystem cameraSystem;
    [SerializeField] private ElectricityManagement electricitySystem;
    [SerializeField] private dvere_kancelar leftDoorScript;
    [SerializeField] private ShiftTimer shiftTimer; // Pro sledování času a noci
    [SerializeField] private NightShiftManagement nightManager; // Pro night management
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pausing
    [SerializeField] private CaviraAI caviraAI; // Reference na Cavira pro detekci blokování dveří CAM 04
    [SerializeField] private IanidAI ianidAI; // Reference na Ianid pro vzajemne blokovani dveri
    
    [Header("Laser Defense GameObjects")]
    [SerializeField] private GameObject[] laserBarriers = new GameObject[6];
    
    [Header("Camera Positions (18 GameObjects)")]
    [SerializeField] private GameObject cam10Podium;     // Position 0: CAM 10 PODIUM
    [SerializeField] private GameObject cam10;           // Position 1: CAM 10
    [SerializeField] private GameObject cam08;           // Position 2: CAM 08
    [SerializeField] private GameObject cam09;           // Position 3: CAM 09
    [SerializeField] private GameObject cam05;           // Position 4: CAM 05
    // Position 5 (CAM 04 Door) REMOVED - no longer used
    [SerializeField] private GameObject cam04VentEntry;  // Position 6: CAM 04 (Vent Entry)
    [SerializeField] private GameObject glassLR;         // Position 7: GLASS LR
    [SerializeField] private GameObject glassRL;         // Position 8: GLASS RL
    [SerializeField] private GameObject cam02Cam11;      // Position 9: CAM 02/CAM 11
    [SerializeField] private GameObject cam03;           // Position 10: CAM 03
    
    [Header("Ventilation Positions (6 GameObjects)")]
    [SerializeField] private GameObject ventCam01;       // Position 11: VENT CAM 01
    [SerializeField] private GameObject ventCam02;       // Position 12: VENT CAM 02
    [SerializeField] private GameObject ventCam03;       // Position 13: VENT CAM 03
    [SerializeField] private GameObject ventCam04;       // Position 14: VENT CAM 04
    [SerializeField] private GameObject ventCam05;       // Position 15: VENT CAM 05
    [SerializeField] private GameObject ventCam06;       // Position 16: VENT CAM 06
    
    [Header("Office Position (1 GameObject)")]
    [SerializeField] private GameObject kancelar;        // Position 17: KANCELAR (Office)
    
    [Header("Light System - Vypnutí před jumpscarem")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo (samostatné)
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři (vypne všechny děti)
    [SerializeField] private float lightOffBeforeJumpscare = 0.9f; // 0.9s před jumpscarem zhasnou světla
    [SerializeField] private float lightOnAfterDelay = 1f; // 1s po jumpscare se světla rozsvítí
    [SerializeField] private AudioSource lightOffAudioSource; // Audio source pro zvuk vypnutí světel
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí elektřiny
    [SerializeField] private AudioSource electricityGeneratorAudioSource; // Audio source generátoru (zastaví se při jumpscare)
    [SerializeField] private float gameOverDelay = 3f; // Doba zobrazení dead panelu před návratem do menu
    
    [Header("Death System - Video Player")]
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player na dead panelu
    
    [Header("Office Loop Sound System")]
    [SerializeField] private AudioSource officeLoopAudioSource; // Audio source pro loop zvuk v kanceláři
    [SerializeField] private AudioClip officeLoopSound; // Loop zvuk když je Orax v kanceláři
    
    [Header("Orax Audio System")]
    [SerializeField] private AudioSource oraxJumpSource;
    [SerializeField] private AudioClip oraxJumpSound;
    [SerializeField] private AudioSource errorCamSource;
    [SerializeField] private AudioClip errorCamSound;
    [SerializeField] private AudioSource jumpscareSource;
    [SerializeField] private AudioClip jumpscareSound;
    [SerializeField] private AudioSource punchIntoWallSource;
    [SerializeField] private AudioClip punchIntoWallSound;
    [SerializeField] private AudioSource runningBehindGlassSource;
    [SerializeField] private AudioClip runningBehindGlassSound;
    [SerializeField] private AudioSource ventilationMovementSource;
    [SerializeField] private AudioClip ventilationMovementSound;
    [SerializeField] private AudioSource metalDetectorSource;
    [SerializeField] private AudioClip metalDetectorSound;
    [SerializeField] private AudioSource appierSoundOraxSource;
    [SerializeField] private AudioClip appierSoundOraxSound;
    
    [Header("Night Start Settings")]
    [SerializeField] private float nightStartDelay = 8f; // Delay before Orax can start moving (seconds)
    [SerializeField] private bool enableNightStartDelay = true; // Enable/disable night start delay
    
    // Door attack mechanics REMOVED - CAM 04 Door position no longer used
    
    [Header("GLASS Movement Settings - Unified System")]
    [SerializeField] private float glassMovementDistance = 10f; // X-axis movement distance (fixed 10 units)
    [SerializeField] private float glassMovementSpeed = 2f; // UNIFIED: Time to complete 10-unit movement (seconds)
    [SerializeField] private float glassWaitTimeAtPosition2 = 1.5f; // Jak dlouho čekat na pozici 2 před dalším pohybem

    [Header("Ventilation Jump Settings")]
    [SerializeField] private float ventilationJumpHeight = 4f; // Y-axis movement for ventilation jump
    [SerializeField] private float ventilationJumpSpeed = 1.5f; // Duration of jump animation
    [SerializeField] private float ventilationPreJumpDelay = 0.2f; // Dřep delay before jump
    [SerializeField] private float ventilationPreJumpDepth = 1f; // How deep the pre-jump crouch goes
    [SerializeField] private Vector3 glassLROriginalPosition = Vector3.zero; // Původní pozice GLASS LR (nastaví se automaticky)
    [SerializeField] private Vector3 glassRLOriginalPosition = Vector3.zero; // Původní pozice GLASS RL (nastaví se automaticky)
    
    // Private variables
    private GameObject[] allPositions;
    private int currentPosition = 0;
    private int previousPosition = -1; // Track predchádzajúcej pozície pre return logic
    private bool isMoving = false;
    // isAttackingDoor REMOVED - door attack mechanics no longer used
    private bool isInOffice = false;
    private bool isInJumpscare = false; // Ochrana před prerušením jumpscare sekvence
    
    // Camera tracking for AppierSoundOrax
    private bool playerWasOnCameras = false;
    private bool appierSoundPlayedThisNight = false; // Track if AppierSound was already played this night
    
    // Other character death detection
    private bool[] otherCharacterDeadPanelStates; // Sledování stavu jiných dead panelů
    
    // AI Level change detection for auto-reset
    private int previousAILevel = -1; // Sledování předchozího AI levelu pro detekci změny na 0
    
    // previousDoorClosed REMOVED - door attack mechanics no longer used
    
    // Ventilation Wait System - podle vzoru Zofaye u dveří  
    private bool isWaitingInVentilation = false; // Zda Orax čeká ve ventilaci (jako Zofaye u dveří)
    private bool isWaitingForCameras = false; // Zda Orax čeká na zapnutí kamer po vypršení 12 sekund
    private float ventilationWaitTimer = 0f; // Odpočítávání času čekání ve ventilaci
    private float ventilationWaitRequiredTime = 12f; // 12 sekund čekání před možností jít do KANCELAR
    private float ventilationAccumulatedTime = 0f; // NOVÉ: Pamatuje si celkový progres i při přepínání laseru
    
    // Laser Defense System - OPRAVENÉ: 100% návrat po 12 sekundách
    private bool isLaserDefenseBlocking = false; // Zda laser blokuje Orax
    private float laserDefenseTimer = 0f; // Odpočítávání času do návratu
    private float laserDefenseRequiredTime = 12f; // Přesně 12 sekund, pak 100% návrat
    
    // GLASS Movement System - podle vzoru Sledgy
    private bool isMovingGlassLRToLR2 = false; // Pohyb GLASS LR → GLASS LR 2
    private bool isMovingGlassRLToRL2 = false; // Pohyb GLASS RL → GLASS RL 2
    private bool isAtGlassLR2Position = false; // Je na pozici GLASS LR 2
    private bool isAtGlassRL2Position = false; // Je na pozici GLASS RL 2
    private Coroutine glassLRMovementCoroutine; // Coroutine pro GLASS LR movement
    private Coroutine glassRLMovementCoroutine; // Coroutine pro GLASS RL movement
    
    // Timing
    private float nextMoveTime;
    private float timeArrivedAtPosition; // Time when Orax arrived at current position
    private float nightStartTime; // Time when night started (for delay)
    // doorAttackCoroutine REMOVED - door attack mechanics no longer used
    private Coroutine officeJumpscareCoroutine;
    
    // Ventilation Mapping System
    private Dictionary<int, int> cameraToVentMapping;
    private Dictionary<int, int> ventToCameraMapping;
    
    // Advanced AI System variables
    private int lastKnownNight = 1;
    private int lastKnownHour = 0;
    private float aiUpdateTimer = 0f;
    
    void Start()
    {
        InitializePositions();
        InitializeVentilationMapping();
        InitializeAudio();
        InitializeGlassPositions(); // Initialize GLASS original positions - podle vzoru Sledgy
        InitializeAdvancedAISystem(); // Initialize advanced AI system
        FindSystemReferences(); // Auto-find system references
        SetNextMoveTime();
        timeArrivedAtPosition = Time.time; // Initialize time tracking
        nightStartTime = Time.time; // Initialize night start time
        
        // previousDoorClosed initialization REMOVED - door attack mechanics no longer used
        
        // Initialize Universal Reset System
        if (enableUniversalResetSystem)
        {
            InitializeOtherCharacterDeathDetection(); // Inicializuje sledování jiných dead panelů
            Debug.Log("[OryxAI] Universal Reset System ENABLED - monitoring other character deaths, menu returns, and AI level changes");
        }
        
        // Set Orax to starting position (CAM 10 PODIUM) - ROBUST RESET
        Debug.Log($"[OryxAI] Start() - Setting Orax to starting position (CAM 10 PODIUM, position 0). Night start delay: {(enableNightStartDelay ? nightStartDelay.ToString("F1") + "s" : "disabled")}");
        Debug.Log($"[OryxAI] Advanced AI System: {(useAdvancedAISystem ? "ENABLED" : "DISABLED")} | Manual AI: {manualAILevel}");
        
        // FORCE EXPLICIT RESET to starting position (SAFETY po jumpscare)
        currentPosition = 0;
        previousPosition = -1;
        isInOffice = false; // KRITICKÉ: reset office state
        
        // Hide all positions
        for (int i = 0; i < allPositions.Length; i++)
        {
            if (allPositions[i] != null)
                allPositions[i].SetActive(false);
        }
        
        // Show only CAM 10 PODIUM
        if (allPositions[0] != null)
        {
            allPositions[0].SetActive(true);
            Debug.Log("[OryxAI] Start() - CAM 10 PODIUM activated, all other positions hidden");
        }
    }
    
    void Update()
    {
        // KRITICKÉ: Kontrola menu - musí být PRVNÍ pro office loop sound
        bool menuIsActive = IsMenuActive();
        
        // KRITICKÉ: Kontrola a správa office loop sound - musí běžet i když isInOffice == true
        HandleOfficeLoopSound(menuIsActive);
        
        // Check for AppierSoundOrax trigger (player closes cameras while Oryx in office)
        CheckAppierSoundTrigger();
        
        // UNIVERZÁLNÍ RESET SYSTÉM - VŽDY se spustí (i během menu)
        if (enableUniversalResetSystem)
        {
            CheckForUniversalReset();
        }
        
        // EARLY RETURN pro blokování dalšího pohybu když je Orax v kanceláři
        if (!enableOraxAI || isInOffice) return;
        
        // CHECK FOR WARDEN - PAUSE ALL TIMERS IF WARDEN IS ATTACKING
        if (wardenAI != null && wardenAI.IsWardenActive())
            return;
        
        // Pokud je menu aktivní, TEĎ ukončí jen movement část
        if (menuIsActive)
        {
            return; // Stop movement but reset checks already ran
        }
        
        // Handle advanced AI system updates (jako u Zofaye)
        if (useAdvancedAISystem && enableRealTimeAIUpdates)
        {
            HandleAdvancedAIUpdates();
        }
        
        // Handle ventilation wait system - podle vzoru Zofaye u dveří
        HandleVentilationWaitSystem(); // Volej vždy - metoda sama zkontroluje pozici a resetuje stavy
        
        // Door attack monitoring REMOVED - CAM 04 Door position no longer used
        // Forced movement after 20 seconds at door REMOVED
        
        // Check night start delay
        if (enableNightStartDelay && (Time.time - nightStartTime) < nightStartDelay)
        {
            // Still in night start delay period - don't allow movement
            return;
        }
        
        // Handle movement timing
        bool timeForMovement = Time.time >= nextMoveTime;
        bool canMove = !isMoving && !isMovingGlassLRToLR2 && !isMovingGlassRLToRL2; // isAttackingDoor removed
        
        // Debug movement timing every 5 seconds
        if (Time.time % 5f < Time.deltaTime)
        {
            float timeUntilNext = nextMoveTime - Time.time;
            Debug.Log($"[OryxAI] Movement Status: TimeForMove={timeForMovement}, CanMove={canMove}, NextIn={timeUntilNext:F1}s");
            Debug.Log($"[OryxAI] Blocking states: LaserDefense={isLaserDefenseBlocking}, VentWait={isWaitingInVentilation}, Position={currentPosition}");
        }
        
        if (timeForMovement && canMove)
        {
            // Check for blocking conditions
            if (IsThatcherBlocking())
            {
                Debug.Log("[OryxAI] Movement blocked by Thatcher");
                SetNextMoveTime();
                return; // Thatcher blocks Orax
            }
            
            // OPRAVA: Zkontroluj zda blocking stavy jsou relevantní pro aktuální pozici
            if (isLaserDefenseBlocking)
            {
                if (currentPosition >= 11 && currentPosition <= 16)
                {
                    Debug.Log("[OryxAI] Movement blocked by laser defense");
                    SetNextMoveTime();
                    return; // Laser defense blocks Orax - musí čekat 10 sekund
                }
                else
                {
                    // Orax není ve ventilaci ale má laser defense blocking - resetuj
                    Debug.Log($"[OryxAI] SAFETY RESET: Orax at position {currentPosition} but has laser defense blocking - resetting");
                    isLaserDefenseBlocking = false;
                    laserDefenseTimer = 0f;
                }
            }
            
            if (isWaitingInVentilation)
            {
                if (currentPosition >= 11 && currentPosition <= 16)
                {
                    Debug.Log("[OryxAI] Movement blocked - waiting in ventilation");
                    SetNextMoveTime();
                    return; // Čeká ve ventilaci - musí čekat 10 sekund
                }
                else
                {
                    // Orax není ve ventilaci ale má ventilation waiting - resetuj
                    Debug.Log($"[OryxAI] SAFETY RESET: Orax at position {currentPosition} but has ventilation waiting - resetting");
                    isWaitingInVentilation = false;
                    isWaitingForCameras = false;
                    ventilationWaitTimer = 0f;
                }
            }
            
            // Perform movement
            AttemptMovement();
        }
    }
    
    void InitializePositions()
    {
        allPositions = new GameObject[18];
        
        // Camera positions (0-10)
        allPositions[0] = cam10Podium;
        allPositions[1] = cam10;
        allPositions[2] = cam08;
        allPositions[3] = cam09;
        allPositions[4] = cam05;
        // allPositions[5] REMOVED - cam04Door no longer used
        allPositions[6] = cam04VentEntry;
        allPositions[7] = glassLR;
        allPositions[8] = glassRL;
        allPositions[9] = cam02Cam11;
        allPositions[10] = cam03;
        
        // Ventilation positions (11-16)
        allPositions[11] = ventCam01;
        allPositions[12] = ventCam02;
        allPositions[13] = ventCam03;
        allPositions[14] = ventCam04;
        allPositions[15] = ventCam05;
        allPositions[16] = ventCam06;
        
        // Office position (17)
        allPositions[17] = kancelar;
    }
    
    void InitializeVentilationMapping()
    {
        // Camera to Ventilation mapping podle specifikace
        cameraToVentMapping = new Dictionary<int, int>
        {
            {4, 13},  // CAM 05 -> VENT CAM 03
            {3, 14},  // CAM 09 -> VENT CAM 04
            {2, 16},  // CAM 08 -> VENT CAM 06
            {10, 15}, // CAM 03 -> VENT CAM 05
            {9, 11},  // CAM 02/CAM 11 -> VENT CAM 01
            {6, 12}   // CAM 04 (Vent Entry) -> VENT CAM 02
        };
        
        // Ventilation to Camera mapping (pro návrat)
        ventToCameraMapping = new Dictionary<int, int>
        {
            {11, 9},  // VENT CAM 01 -> CAM 02/CAM 11
            {12, 6},  // VENT CAM 02 -> CAM 04 (Vent Entry)
            {13, 4},  // VENT CAM 03 -> CAM 05
            {14, 3},  // VENT CAM 04 -> CAM 09
            {15, 10}, // VENT CAM 05 -> CAM 03
            {16, 2}   // VENT CAM 06 -> CAM 08
        };
    }
    
    void InitializeGlassPositions()
    {
        // Uložit původní pozice GLASS GameObjectů - podle vzoru Sledgy
        if (glassLR != null)
        {
            glassLROriginalPosition = glassLR.transform.position;
            Debug.Log($"[OryxAI] GLASS LR original position stored: {glassLROriginalPosition}");
        }
        
        if (glassRL != null)
        {
            glassRLOriginalPosition = glassRL.transform.position;
            Debug.Log($"[OryxAI] GLASS RL original position stored: {glassRLOriginalPosition}");
        }
        
        Debug.Log("[OryxAI] GLASS positions initialized - ready for Sledgy-style movement");
    }
    
    void InitializeAudio()
    {
        // Setup audio sources with proper 3D settings
        if (oraxJumpSource != null)
        {
            oraxJumpSource.spatialBlend = 1f; // 3D sound
            oraxJumpSource.playOnAwake = false;
        }
        
        if (runningBehindGlassSource != null)
        {
            runningBehindGlassSource.spatialBlend = 0.5f; // OPRAVENÉ: Mix 2D/3D pro lepší slyšitelnost
            runningBehindGlassSource.playOnAwake = false;
            // Store original volume for proximity system
            if (originalGlassProximityVolume <= 0f)
                originalGlassProximityVolume = runningBehindGlassSource.volume;
        }
        
        if (metalDetectorSource != null)
        {
            metalDetectorSource.spatialBlend = 1f;
            metalDetectorSource.playOnAwake = false;
        }
        
        if (punchIntoWallSource != null)
        {
            punchIntoWallSource.spatialBlend = 1f;
            punchIntoWallSource.playOnAwake = false;
        }
        
        if (jumpscareSource != null)
        {
            jumpscareSource.spatialBlend = 0f; // 2D sound for jumpscare
            jumpscareSource.playOnAwake = false;
        }
        
        if (errorCamSource != null)
        {
            errorCamSource.spatialBlend = 0f; // 2D sound for UI
            errorCamSource.playOnAwake = false;
        }
        
        if (ventilationMovementSource != null)
        {
            ventilationMovementSource.spatialBlend = 1f; // 3D sound
            ventilationMovementSource.playOnAwake = false;
        }
        
        if (appierSoundOraxSource != null)
        {
            appierSoundOraxSource.spatialBlend = 0f; // 2D sound for UI warning
            appierSoundOraxSource.playOnAwake = false;
        }
    }
    
    void PlayMovingPanelAndErrorCam()
    {
        // KRITICKÉ: Nezobrazuj Error Cam v menu nebo Custom Night panelu
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return;
        }
        
        // Moving Panel
        if (movingPanelOrax != null)
            movingPanelOrax.SetActive(true);
            
        // Error Cam Sound
        if (errorCamSource != null && errorCamSound != null)
        {
            errorCamSource.clip = errorCamSound;
            errorCamSource.Play();
        }
    }
    
    void HideMovingPanel()
    {
        if (movingPanelOrax != null)
            movingPanelOrax.SetActive(false);
    }
    
    void PlayMetalDetectorWithVolumeCheck()
    {
        if (metalDetectorSource != null && metalDetectorSound != null)
        {
            metalDetectorSource.clip = metalDetectorSound;
            
            // OPRAVENÉ: 70% hlasitost keď JE hráč na kamerách, 30% keď nie je
            float volume = IsPlayerOnCameras() ? 0.7f : 0.3f;
            metalDetectorSource.volume = volume;
            
            Debug.Log($"[OryxAI] ===== METAL DETECTOR TRIGGERED =====");
            Debug.Log($"[OryxAI] Player on cameras: {IsPlayerOnCameras()}");
            Debug.Log($"[OryxAI] Volume: {(volume * 100):F0}%");
            metalDetectorSource.Play();
        }
        else
        {
            Debug.LogError($"[OryxAI] METAL DETECTOR FAILED - Source: {metalDetectorSource}, Clip: {metalDetectorSound}");
        }
    }
    
    void PlayVentilationCrawl(float volume)
    {
        if (ventilationMovementSource != null && ventilationMovementSound != null)
        {
            ventilationMovementSource.clip = ventilationMovementSound;
            ventilationMovementSource.volume = volume;
            Debug.Log($"[OryxAI] Playing Ventilation Crawl Sound - Volume: {volume * 100}%");
            ventilationMovementSource.Play();
        }
        else
        {
            Debug.LogWarning("[OryxAI] Cannot play Ventilation Crawl - missing source or sound clip");
        }
    }
    
    IEnumerator PlayVentilationCrawlProgressive()
    {
        // Progressive volume increase from 60% to 100% during vent→office movement
        if (ventilationMovementSource != null && ventilationMovementSound != null)
        {
            ventilationMovementSource.clip = ventilationMovementSound;
            ventilationMovementSource.Play();
            
            float duration = 2f; // Duration of progressive volume increase
            float elapsed = 0f;
            float startVolume = 0.6f; // 60%
            float endVolume = 1.0f;   // 100%
            
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float currentVolume = Mathf.Lerp(startVolume, endVolume, progress);
                ventilationMovementSource.volume = currentVolume;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            ventilationMovementSource.volume = endVolume;
        }
    }
    
    void SetNextMoveTime()
    {
        float interval = movementInterval;
        
        // Use aggressive timing during power outage
        if (electricitySystem != null && !electricitySystem.IsPowerOn())
        {
            interval = aggressiveInterval;
        }
        
        nextMoveTime = Time.time + interval;
    }
    
    void AttemptMovement()
    {
        Debug.Log($"[OryxAI] ===== ATTEMPT MOVEMENT =====");
        Debug.Log($"[OryxAI] Current state: Position={currentPosition}, isMoving={isMoving}"); // isAttackingDoor removed
        Debug.Log($"[OryxAI] GLASS states: isMovingLRToLR2={isMovingGlassLRToLR2}, isMovingRLToRL2={isMovingGlassRLToRL2}");
        Debug.Log($"[OryxAI] Ventilation wait: isWaiting={isWaitingInVentilation}, timer={ventilationWaitTimer:F1}s");
        Debug.Log($"[OryxAI] Laser defense: isBlocking={isLaserDefenseBlocking}, timer={laserDefenseTimer:F1}s");
        
        // Check night start delay
        float timeSinceNightStart = Time.time - nightStartTime;
        if (enableNightStartDelay && timeSinceNightStart < nightStartDelay)
        {
            Debug.Log($"[OryxAI] Movement blocked by night start delay - {timeSinceNightStart:F1}s/{nightStartDelay:F1}s");
            SetNextMoveTime();
            return;
        }
        
        // AI level check (1-20 system with percentage chance)
        float aiChance = CalculateAIChance();
        float randomRoll = Random.value;
        Debug.Log($"[OryxAI] AI Check: Level={currentAILevel}, Chance={aiChance:F2} ({aiChance*100:F0}%), Roll={randomRoll:F3}");
        
        if (randomRoll > aiChance)
        {
            Debug.Log($"[OryxAI] Movement FAILED - Roll {randomRoll:F3} > Chance {aiChance:F3} - Next attempt in {movementInterval}s");
            SetNextMoveTime();
            return;
        }
        
        // Determine next position based on current position
        int nextPosition = GetNextPosition();
        Debug.Log($"[OryxAI] Movement APPROVED - Moving from position {currentPosition} to position {nextPosition}");
        
        if (nextPosition != -1)
        {
            MoveToPosition(nextPosition);
        }
        else
        {
            Debug.Log("[OryxAI] No valid next position - setting next move time");
            SetNextMoveTime();
        }
    }
    
    int GetNextPosition()
    {
        // Rozvětvený pohybový systém podle specifikace
        switch (currentPosition)
        {
            case 0: // CAM 10 PODIUM -> CAM 10
                return 1;
                
            case 1: // CAM 10 -> (CAM 08 | CAM 09 | CAM 05)
                int[] options1 = {2, 3, 4}; // CAM 08, CAM 09, CAM 05
                return options1[Random.Range(0, options1.Length)];
                
            case 2: // CAM 08 -> (CAM 03 | CAM 02/CAM 11 | CAM 09) + možnost ventilation jump
                // Možnost skoku do ventilace
                if (Random.value < 0.3f && cameraToVentMapping.ContainsKey(currentPosition))
                {
                    return cameraToVentMapping[currentPosition]; // Skok do ventilace
                }
                int[] options2 = {10, 9, 3}; // CAM 03, CAM 02/CAM 11, CAM 09
                return options2[Random.Range(0, options2.Length)];
                
            case 3: // CAM 09 -> (CAM 08 | CAM 05) + možnost ventilation jump
                // Možnost skoku do ventilace
                if (Random.value < 0.3f && cameraToVentMapping.ContainsKey(currentPosition))
                {
                    return cameraToVentMapping[currentPosition]; // Skok do ventilace
                }
                int[] options3 = {2, 4}; // CAM 08, CAM 05
                return options3[Random.Range(0, options3.Length)];
                
            case 4: // CAM 05 -> (CAM 09 | CAM 04 Vent Entry) + možnost ventilation jump
                // Možnost skoku do ventilace
                float jumpChance = Random.value;
                if (jumpChance < 0.3f && cameraToVentMapping.ContainsKey(currentPosition))
                {
                    int ventPos = cameraToVentMapping[currentPosition];
                    Debug.Log($"[OryxAI] CAM 05: Ventilation jump chosen (chance: {jumpChance:F2} < 0.3) -> Position {ventPos}");
                    return ventPos; // Skok do ventilace
                }
                // Normal movement - CAM 09 or CAM 04 Vent Entry
                int[] options4 = {3, 6}; // CAM 09, CAM 04 Vent Entry
                return options4[Random.Range(0, options4.Length)];
                
            // case 5: CAM 04 Door REMOVED - position no longer exists
                
            case 6: // CAM 04 Vent Entry -> (CAM 05 | GLASS LR) + možnost ventilation jump
                // Cavira/Ianid blocking checks REMOVED - no longer relevant without door position
                
                // Možnost skoku do ventilace
                jumpChance = Random.value;
                if (jumpChance < 0.3f && cameraToVentMapping.ContainsKey(currentPosition))
                {
                    int ventPos = cameraToVentMapping[currentPosition];
                    Debug.Log($"[OryxAI] CAM 04 Vent Entry: Ventilation jump chosen (chance: {jumpChance:F2} < 0.3) -> Position {ventPos}");
                    return ventPos; // Skok do ventilace
                }
                
                // Normal movement - CAM 05 or GLASS LR
                int[] options6 = {4, 7}; // CAM 05, GLASS LR
                return options6[Random.Range(0, options6.Length)];
                
            case 7: // GLASS LR -> NOVÝ SYSTÉM: nejdřív GLASS LR 2, pak možnosti
                if (!isAtGlassLR2Position)
                {
                    // Ještě nebyl na pozici GLASS LR 2 - musí tam jít nejdřív (speciální hodnota 71)
                    Debug.Log("[OryxAI] GLASS LR: Moving to GLASS LR 2 position first (like Sledgy)");
                    return 71; // Speciální hodnota pro GLASS LR 2
                }
                else
                {
                    // Už byl na GLASS LR 2 - teď může jít na normální pozice
                    int[] options7 = {6, 8, 9}; // CAM 04 Vent Entry, GLASS RL, CAM 02/CAM 11
                    return options7[Random.Range(0, options7.Length)];
                }
                
            case 8: // GLASS RL -> NOVÝ SYSTÉM: nejdřív GLASS RL 2, pak možnosti  
                if (!isAtGlassRL2Position)
                {
                    // Ještě nebyl na pozici GLASS RL 2 - musí tam jít nejdřív (speciální hodnota 81)
                    Debug.Log("[OryxAI] GLASS RL: Moving to GLASS RL 2 position first (like Sledgy)");
                    return 81; // Speciální hodnota pro GLASS RL 2
                }
                else
                {
                    // OPRAVENÉ: ZÁKAZ přesunu na CAM 02/CAM 11 - pouze CAM 04 Vent Entry a GLASS LR
                    int[] options8 = {6, 7}; // CAM 04 Vent Entry, GLASS LR (REMOVED: CAM 02/CAM 11)
                    Debug.Log("[OryxAI] GLASS RL movement - RESTRICTED to CAM 04 Vent Entry or GLASS LR only");
                    return options8[Random.Range(0, options8.Length)];
                }
                
            case 9: // CAM 02/CAM 11 -> (GLASS RL | CAM 03 | CAM 08) + možnost ventilation jump
                // Možnost skoku do ventilace
                if (Random.value < 0.3f && cameraToVentMapping.ContainsKey(currentPosition))
                {
                    return cameraToVentMapping[currentPosition]; // Skok do ventilace
                }
                int[] options9 = {8, 10, 2}; // GLASS RL, CAM 03, CAM 08
                return options9[Random.Range(0, options9.Length)];
                
            case 10: // CAM 03 -> (CAM 08 | CAM 02/CAM 11) + možnost ventilation jump
                // Možnost skoku do ventilace
                if (Random.value < 0.3f && cameraToVentMapping.ContainsKey(currentPosition))
                {
                    return cameraToVentMapping[currentPosition]; // Skok do ventilace
                }
                int[] options10 = {2, 9}; // CAM 08, CAM 02/CAM 11
                return options10[Random.Range(0, options10.Length)];
                
            // Ventilation positions - NOVÉ CHOVÁNÍ: Orax NIKDY nejde hned do KANCELAR
            // Musí nejdřív počkat 10 sekund (handled ve HandleVentilationWaitSystem)
            case 11: // VENT CAM 01 -> Čeká 10 sekund, pak možnost KANCELAR
            case 12: // VENT CAM 02 -> Čeká 10 sekund, pak možnost KANCELAR  
            case 13: // VENT CAM 03 -> Čeká 10 sekund, pak možnost KANCELAR
            case 14: // VENT CAM 04 -> Čeká 10 sekund, pak možnost KANCELAR
            case 15: // VENT CAM 05 -> Čeká 10 sekund, pak možnost KANCELAR
            case 16: // VENT CAM 06 -> Čeká 10 sekund, pak možnost KANCELAR
                // NOVÁ LOGIKA: GetNextPosition už nerozhoduje o KANCELAR
                // HandleVentilationWaitSystem řídí možnost jít do KANCELAR po 10 sekundách
                Debug.Log($"[OryxAI] Orax in ventilation position {currentPosition} - waiting system will handle KANCELAR move");
                return -1; // Žádný automatický pohyb - řídí HandleVentilationWaitSystem
                
            case 17: // KANCELAR - no movement, only jumpscare
                return -1;
                
            // Speciální pozice pro GLASS movement - podle vzoru Sledgy
            case 71: // GLASS LR 2 (virtuální pozice) - po čekání se může vrátit na GLASS LR možnosti
                // Po čekání na GLASS LR 2 se může přesunout na normální pozice
                int[] optionsFromGlassLR2 = {6, 8, 9}; // CAM 04 Vent Entry, GLASS RL, CAM 02/CAM 11
                return optionsFromGlassLR2[Random.Range(0, optionsFromGlassLR2.Length)];
                
            case 81: // GLASS RL 2 (virtuální pozice) - po čekání se může vrátit na GLASS RL možnosti
                // OPRAVENÉ: ZÁKAZ přesunu na CAM 02/CAM 11 - pouze CAM 04 Vent Entry a GLASS LR
                int[] optionsFromGlassRL2 = {6, 7}; // CAM 04 Vent Entry, GLASS LR (REMOVED: CAM 02/CAM 11)
                return optionsFromGlassRL2[Random.Range(0, optionsFromGlassRL2.Length)];
                
            default:
                return -1; // No movement
        }
    }
    
    void MoveToPosition(int newPosition)
    {
        // Speciální handling pro virtuální GLASS pozice
        if (newPosition == 71 || newPosition == 81)
        {
            StartCoroutine(MovementSequence(newPosition));
            return;
        }
        
        if (newPosition < 0 || newPosition >= allPositions.Length) return;
        
        // Reset GLASS pozice stavy když se Orax přesouvá z GLASS pozic na normální pozice
        if (currentPosition == 71 || currentPosition == 81)
        {
            Debug.Log($"[OryxAI] Moving from virtual GLASS position {currentPosition} to normal position {newPosition}");
            
            // Reset GLASS states and properly hide/reset GLASS GameObjects
            if (currentPosition == 71)
            {
                isAtGlassLR2Position = false;
                // Vrať GLASS LR na původní pozici a vypni GameObject
                if (glassLR != null && glassLROriginalPosition != Vector3.zero)
                {
                    glassLR.transform.position = glassLROriginalPosition;
                    glassLR.SetActive(false); // OPRAVENÉ: Správne vypni GLASS LR
                    Debug.Log("[OryxAI] GLASS LR returned to original position and deactivated before normal move");
                }
            }
            else if (currentPosition == 81)
            {
                isAtGlassRL2Position = false;
                // Vrať GLASS RL na původní pozici a vypni GameObject
                if (glassRL != null && glassRLOriginalPosition != Vector3.zero)
                {
                    glassRL.transform.position = glassRLOriginalPosition;
                    glassRL.SetActive(false); // OPRAVENÉ: Správne vypni GLASS RL
                    Debug.Log("[OryxAI] GLASS RL returned to original position and deactivated before normal move");
                }
            }
            // OPRAVENÉ: Tiež reset keď sa opúšťa normálne GLASS pozice (7, 8)
            else if (currentPosition == 7) // GLASS LR
            {
                if (glassLR != null)
                {
                    glassLR.SetActive(false); // Vypni GLASS LR
                    Debug.Log("[OryxAI] GLASS LR deactivated when leaving position 7");
                }
            }
            else if (currentPosition == 8) // GLASS RL
            {
                if (glassRL != null)
                {
                    glassRL.SetActive(false); // Vypni GLASS RL
                    Debug.Log("[OryxAI] GLASS RL deactivated when leaving position 8");
                }
            }
        }
        
        StartCoroutine(MovementSequence(newPosition));
    }
    
    IEnumerator MovementSequence(int newPosition)
    {
        isMoving = true;
        
        // Check for special movement types
        bool isVentilationJump = IsVentilationJump(currentPosition, newPosition);
        bool isGlassMovement = IsGlassMovement(currentPosition, newPosition);
        bool isGlassLRToLR2Movement = (currentPosition == 7 && newPosition == 71); // GLASS LR → GLASS LR 2
        bool isGlassRLToRL2Movement = (currentPosition == 8 && newPosition == 81); // GLASS RL → GLASS RL 2
        // isDoorAttackMovement REMOVED - door position no longer used
        
        Debug.Log($"[OryxAI] Movement type analysis: VentJump={isVentilationJump}, Glass={isGlassMovement}, GlassLR→LR2={isGlassLRToLR2Movement}, GlassRL→RL2={isGlassRLToRL2Movement}"); // Door removed
        
        // Handle Metal Detector Sound for specific transitions
        if (ShouldPlayMetalDetector(currentPosition, newPosition))
        {
            PlayMetalDetectorWithVolumeCheck();
        }
        
        // Handle physical movement effects BEFORE showing panels
        if (isVentilationJump)
        {
            Debug.Log("[OryxAI] Using ventilation jump movement");
            yield return StartCoroutine(HandleVentilationJumpMovement(newPosition));
        }
        else if (isGlassLRToLR2Movement)
        {
            Debug.Log("[OryxAI] Using GLASS LR → GLASS LR 2 movement (like Sledgy)");
            yield return StartCoroutine(HandleGlassLRToLR2Movement());
        }
        else if (isGlassRLToRL2Movement)
        {
            Debug.Log("[OryxAI] Using GLASS RL → GLASS RL 2 movement (like Sledgy)");
            yield return StartCoroutine(HandleGlassRLToRL2Movement());
        }
        else if (isGlassMovement)
        {
            yield return StartCoroutine(HandleGlassMovement(newPosition));
        }
        // isDoorAttackMovement handling REMOVED
        else
        {
            // Normal movement
            yield return StartCoroutine(HandleNormalMovement(newPosition));
        }
        
        isMoving = false;
    }
    
    bool IsVentilationJump(int from, int to)
    {
        bool isJump = cameraToVentMapping.ContainsKey(from) && cameraToVentMapping[from] == to;
        if (isJump)
        {
            Debug.Log($"[OryxAI] Ventilation jump detected: Position {from} -> Position {to}");
        }
        return isJump;
    }
    
    bool IsGlassMovement(int from, int to)
    {
        return (from == 7 && (to == 8 || to == 9 || to == 6)) || // GLASS LR movement
               (from == 8 && (to == 7 || to == 9 || to == 6));   // GLASS RL movement
    }
    
    bool ShouldPlayMetalDetector(int from, int to)
    {
        Debug.Log($"[OryxAI] ===== METAL DETECTOR CHECK =====");
        Debug.Log($"[OryxAI] Movement: Position {from} -> Position {to}");
        
        // SPECIFICATION: Metal Detector triggers ONLY on these transitions:
        // 1. CAM 08 (pos 2) ↔ CAM 02/CAM 11 (pos 9)  
        // 2. CAM 03 (pos 10) ↔ CAM 02/CAM 11 (pos 9)
        
        // CAM 08 ↔ CAM 02/CAM 11
        if (from == 2 && to == 9)
        {
            Debug.Log($"[OryxAI] ✅ METAL DETECTOR TRIGGERED: CAM 08 (pos {from}) -> CAM 02/CAM 11 (pos {to})");
            return true;
        }
        if (from == 9 && to == 2)
        {
            Debug.Log($"[OryxAI] ✅ METAL DETECTOR TRIGGERED: CAM 02/CAM 11 (pos {from}) -> CAM 08 (pos {to})");
            return true;
        }
        
        // CAM 03 ↔ CAM 02/CAM 11  
        if (from == 10 && to == 9)
        {
            Debug.Log($"[OryxAI] ✅ METAL DETECTOR TRIGGERED: CAM 03 (pos {from}) -> CAM 02/CAM 11 (pos {to})");
            return true;
        }
        if (from == 9 && to == 10)
        {
            Debug.Log($"[OryxAI] ✅ METAL DETECTOR TRIGGERED: CAM 02/CAM 11 (pos {from}) -> CAM 03 (pos {to})");
            return true;
        }
        
        Debug.Log($"[OryxAI] ❌ Metal Detector NOT triggered for this movement");
        return false;
    }
    
    IEnumerator HandleNormalMovement(int newPosition)
    {
        // Show moving panel and error cam if player on cameras
        if (IsPlayerOnCameras())
        {
            PlayMovingPanelAndErrorCam();
        }
        
        // Hide current position
        if (currentPosition < allPositions.Length && allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(false);
        
        // Movement delay
        yield return new WaitForSeconds(0.5f);
        
        // Update position tracking
        previousPosition = currentPosition; // Save previous position before update
        currentPosition = newPosition;
        timeArrivedAtPosition = Time.time; // Record arrival time
        
        // Set office state when entering position 17 (KANCELAR)
        if (newPosition == 17)
        {
            isInOffice = true;
            Debug.Log("[OryxAI] Office state activated - Orax entered office via normal movement (position 17)");
        }
        
        // Reset office state when leaving position 17
        if (previousPosition == 17 && newPosition != 17)
        {
            isInOffice = false;
            Debug.Log("[OryxAI] Left office - isInOffice reset to false");
        }
        
        Debug.Log($"[OryxAI] Position updated: {previousPosition} -> {currentPosition}");
        
        // Show new position
        if (allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(true);
        
        // Hide moving panel
        HideMovingPanel();
        
        // DŮLEŽITÉ: Nastav next move time aby pokračoval v pohybu
        SetNextMoveTime();
        
        Debug.Log($"[OryxAI] Normal movement complete to position {currentPosition} - next move scheduled");
        
        // Handle special position mechanics
        HandleSpecialPositionMechanics(newPosition);
    }
    
    IEnumerator HandleVentilationJumpMovement(int newPosition)
    {
        Debug.Log($"[OryxAI] Starting ventilation jump from position {currentPosition} to position {newPosition}");
        
        // NOVÝ POŽADAVEK: MovingPanel + Error Cam Sound při přechodu z kamer na ventilační kamery
        bool isJumpingToVentilation = (newPosition >= 11 && newPosition <= 16); // Cílová pozice je ventilace
        bool isJumpingFromCamera = (currentPosition >= 0 && currentPosition <= 10); // Startovní pozice je kamera
        
        if (isJumpingFromCamera && isJumpingToVentilation && IsPlayerOnCameras())
        {
            // Přehrej MovingPanel + Error Cam Sound při přechodu kamera → ventilace
            Debug.Log("[OryxAI] Camera→Ventilation jump - showing MovingPanel + Error Cam Sound");
            PlayMovingPanelAndErrorCam();
        }
        
        // Ventilation jump: configurable dřep depth, then configurable jump height
        Vector3 originalPos = transform.position;
        
        // Check if player is NOT on cameras (don't play crawl sound if on cameras)
        bool playerOnCameras = CheckIfPlayerOnCameras();
        
        // Dřep effect (configurable depth)
        Vector3 crouchPos = originalPos + Vector3.down * ventilationPreJumpDepth;
        transform.position = crouchPos;
        Debug.Log($"[OryxAI] Crouch phase - moving down {ventilationPreJumpDepth} units");
        yield return new WaitForSeconds(ventilationPreJumpDelay);
        
        // Jump effect with sound
        PlayVentilationJumpSound();
        
        // Wait for jump sound to finish, then play crawling sound
        yield return new WaitForSeconds(0.3f); // Let jump sound play first
        
        // Play Ventilation Crawl sound AFTER jump - different volume based on camera state
        if (playerOnCameras)
        {
            PlayVentilationCrawl(0.5f); // 50% volume if player on cameras
            Debug.Log("[OryxAI] Playing Ventilation Crawl Sound - Player on cameras: 50% volume");
        }
        else
        {
            PlayVentilationCrawl(0.6f); // 60% volume if player in office
            Debug.Log("[OryxAI] Playing Ventilation Crawl Sound - Player in office: 60% volume");
        }
        
        Debug.Log($"[OryxAI] Jump phase - moving up {ventilationJumpHeight} units over {ventilationJumpSpeed} seconds");
        StartCoroutine(MovementPhysics(originalPos, Vector3.up * ventilationJumpHeight, ventilationJumpSpeed));
        
        // Hide current position during jump
        if (currentPosition < allPositions.Length && allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(false);
        
        yield return new WaitForSeconds(0.8f); // Mid-jump
        
        // Update position
        int oldPosition = currentPosition;
        currentPosition = newPosition;
        timeArrivedAtPosition = Time.time; // Record arrival time
        
        // Reset office state when leaving position 17
        if (oldPosition == 17 && newPosition != 17)
        {
            isInOffice = false;
            Debug.Log("[OryxAI] Left office via ventilation - isInOffice reset to false");
        }
        
        // Show new position
        if (allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(true);
        
        // Hide MovingPanel po dokončení skoku kamera→ventilace
        if (isJumpingFromCamera && isJumpingToVentilation)
        {
            HideMovingPanel();
            Debug.Log("[OryxAI] MovingPanel hidden after camera→ventilation jump");
        }
        
        // Special case: when moving from vent to office (position 17: KANCELAR), play crawling sound
        if (newPosition == 17) // Office position (KANCELAR)
        {
            // Crawling sound na 90% hlasitosti při přechodu z ventilace do kanceláře
            Debug.Log("[OryxAI] ===== CRAWLING ZVUK - VENT → KANCELAR =====");
            Debug.Log($"[OryxAI] Přechod z ventilace {currentPosition} do KANCELAR 17");
            Debug.Log($"[OryxAI] VentilationMovementSource: {(ventilationMovementSource != null ? "OK" : "NULL")}");
            Debug.Log($"[OryxAI] VentilationMovementSound: {(ventilationMovementSound != null ? "OK" : "NULL")}");
            Debug.Log("[OryxAI] Přehrávám CrawlingZvuk (VentilationMovementSound) při přechodu z ventilace do kanceláře - hlasitost 90%");
            PlayVentilationCrawl(0.9f);
            
            // Play Error Cam Sound for vent→office movement
            if (errorCamSource != null && errorCamSound != null)
            {
                errorCamSource.clip = errorCamSound;
                errorCamSource.Play();
            }
            
            // NOVÉ: Nastavení office stavu a spuštění loop soundu
            isInOffice = true; // KRITICKÉ: nastav stav že Orax je v kanceláři
            StartOfficeLoopSound();
            Debug.Log("[OryxAI] Office state activated - Orax entered office (position 17)");
        }
        
        yield return new WaitForSeconds(0.7f); // Complete jump
        
        // Return to original Y position
        yield return new WaitForSeconds(0.2f);
    }
    
    IEnumerator HandleGlassMovement(int newPosition)
    {
        // Glass movement with proximity sound effect
        float direction = (currentPosition == 7) ? 10f : -10f; // GLASS LR = +10, GLASS RL = -10
        
        // Start proximity sound
        StartCoroutine(PlayProximityGlassSound());
        
        // Physical movement with configurable speed
        Vector3 originalPos = transform.position;
        StartCoroutine(MovementPhysics(originalPos, Vector3.right * direction, glassMovementSpeed));
        
        // Hide current position
        if (currentPosition < allPositions.Length && allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(false);
        
        yield return new WaitForSeconds(glassMovementSpeed * 0.5f); // Mid-movement (half of total time)
        
        // Update position
        int oldPosition = currentPosition;
        currentPosition = newPosition;
        timeArrivedAtPosition = Time.time; // Record arrival time
        
        // Set office state when entering position 17 via glass movement
        if (newPosition == 17)
        {
            isInOffice = true;
            Debug.Log("[OryxAI] Office state activated - Orax entered office via glass movement (position 17)");
        }
        
        // Reset office state when leaving position 17
        if (oldPosition == 17 && newPosition != 17)
        {
            isInOffice = false;
            Debug.Log("[OryxAI] Left office via glass - isInOffice reset to false");
        }
        
        // Show new position
        if (allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(true);
        
        yield return new WaitForSeconds(glassMovementSpeed * 0.5f); // Complete movement (remaining half)
        
        // Show moving panel and error cam after physical movement
        if (IsPlayerOnCameras())
        {
            PlayMovingPanelAndErrorCam();
            yield return new WaitForSeconds(0.5f);
            HideMovingPanel();
        }
    }
    
    /// <summary>
    /// GLASS LR → GLASS LR 2 movement podle vzoru Sledgy
    /// </summary>
    IEnumerator HandleGlassLRToLR2Movement()
    {
        Debug.Log("[OryxAI] Starting GLASS LR → GLASS LR 2 movement (Sledgy-style)");
        
        if (glassLR != null)
        {
            isMovingGlassLRToLR2 = true;
            
            // OPRAVENÉ: GLASS LR pohyb o +10 jednotek na ose X (Left to Right)
            Vector3 startPos = glassLR.transform.position;
            Vector3 endPos = startPos + Vector3.right * 10f; // +10 jednotek X (LR = Left to Right)
            
            // Začátek pohybu - spusť proximity sound effect s proper parametry
            StartGlassProximitySound(startPos, endPos);
            
            Debug.Log($"[OryxAI] GLASS LR moving from {startPos} to {endPos} over {glassMovementSpeed} seconds");
            
            // Plynulý pohyb jako u Sledgy
            float elapsed = 0f;
            while (elapsed < glassMovementSpeed)
            {
                float progress = elapsed / glassMovementSpeed;
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                glassLR.transform.position = currentPos;
                
                // Update proximity sound volume podle pozice
                UpdateGlassProximityVolume(currentPos, startPos, endPos);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ujisti se, že je na konečné pozici
            glassLR.transform.position = endPos;
            
            // Stop proximity sound
            StopGlassProximitySound();
            
            // Update stavu
            currentPosition = 71; // GLASS LR 2 (virtuální pozice)
            timeArrivedAtPosition = Time.time;
            isAtGlassLR2Position = true;
            isMovingGlassLRToLR2 = false;
            
            Debug.Log("[OryxAI] GLASS LR reached GLASS LR 2 position - waiting before next move");
            
            // Počkej na pozici GLASS LR 2 (jako u Sledgy)
            yield return new WaitForSeconds(glassWaitTimeAtPosition2);
            
            // Show moving panel and error cam AFTER movement if player on cameras
            if (IsPlayerOnCameras())
            {
                PlayMovingPanelAndErrorCam();
                yield return new WaitForSeconds(0.5f);
                HideMovingPanel();
            }
            
            // DŮLEŽITÉ: Nastav next move time aby pokračoval v pohybu
            SetNextMoveTime();
            Debug.Log($"[OryxAI] GLASS LR movement complete - next move scheduled");
        }
        else
        {
            Debug.LogWarning("[OryxAI] GLASS LR GameObject is null - cannot perform movement!");
        }
    }
    
    /// <summary>
    /// GLASS RL → GLASS RL 2 movement podle vzoru Sledgy
    /// </summary>
    IEnumerator HandleGlassRLToRL2Movement()
    {
        Debug.Log("[OryxAI] Starting GLASS RL → GLASS RL 2 movement (Sledgy-style)");
        
        if (glassRL != null)
        {
            isMovingGlassRLToRL2 = true;
            
            // OPRAVENÉ: GLASS RL pohyb o -10 jednotek na ose X (Right to Left)
            Vector3 startPos = glassRL.transform.position;
            Vector3 endPos = startPos + Vector3.left * 10f; // -10 jednotek X (RL = Right to Left)
            
            // Začátek pohybu - spusť proximity sound effect s proper parametry
            StartGlassProximitySound(startPos, endPos);
            
            Debug.Log($"[OryxAI] GLASS RL moving from {startPos} to {endPos} over {glassMovementSpeed} seconds");
            
            // Plynulý pohyb jako u Sledgy s proximity sound updates
            float elapsed = 0f;
            while (elapsed < glassMovementSpeed)
            {
                float progress = elapsed / glassMovementSpeed;
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                glassRL.transform.position = currentPos;
                
                // Update proximity sound volume podle pozice
                UpdateGlassProximityVolume(currentPos, startPos, endPos);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ujisti se, že je na konečné pozici
            glassRL.transform.position = endPos;
            
            // Stop proximity sound
            StopGlassProximitySound();
            
            // Update stavu
            currentPosition = 81; // GLASS RL 2 (virtuální pozice)
            timeArrivedAtPosition = Time.time;
            isAtGlassRL2Position = true;
            isMovingGlassRLToRL2 = false;
            
            Debug.Log("[OryxAI] GLASS RL reached GLASS RL 2 position - waiting before next move");
            
            // Počkej na pozici GLASS RL 2 (jako u Sledgy)
            yield return new WaitForSeconds(glassWaitTimeAtPosition2);
            
            // Show moving panel and error cam AFTER movement if player on cameras
            if (IsPlayerOnCameras())
            {
                PlayMovingPanelAndErrorCam();
                yield return new WaitForSeconds(0.5f);
                HideMovingPanel();
            }
            
            // DŮLEŽITÉ: Nastav next move time aby pokračoval v pohybu
            SetNextMoveTime();
            Debug.Log($"[OryxAI] GLASS RL movement complete - next move scheduled");
        }
        else
        {
            Debug.LogWarning("[OryxAI] GLASS RL GameObject is null - cannot perform movement!");
        }
    }
    
    IEnumerator PlayProximityGlassSound()
    {
        // Dynamické přiblížení a oddálení jako SLEDGY
        if (runningBehindGlassSource != null && runningBehindGlassSound != null)
        {
            runningBehindGlassSource.clip = runningBehindGlassSound;
            runningBehindGlassSource.Play();
            
            // Dynamic volume effect
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                // Volume peaks in middle, then fades
                float baseVolume = Mathf.Sin(progress * Mathf.PI);
                
                // Aplikuj camera modifier - ztlum na 20% pokud hráč na kamerách
                float finalVolume = IsPlayerOnCameras() ? baseVolume * 0.2f : baseVolume;
                runningBehindGlassSource.volume = finalVolume;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    void HandleSpecialPositionMechanics(int position)
    {
        switch (position)
        {
            // case 5: CAM 04 Door REMOVED - door attack mechanics no longer used
                
            case 7: // GLASS LR - Glass movement with X-axis physics
            case 8: // GLASS RL
                PlayGlassMovementSound();
                SimulateGlassMovement();
                break;
                
            case 11: // Ventilation positions - Metal detector + Y-axis jump
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
                PlayMetalDetectorSound();
                PlayVentilationJumpSound();
                SimulateVentilationJump();
                break;
                
            case 17: // KANCELAR - Office Attack
                StartOfficeAttack();
                break;
        }
    }
    
    // Door attack mechanics COMPLETELY REMOVED from this point:
    // - HandleDoorAttackMonitoring()
    // - IsLeftDoorClosed() 
    // - StartDoorAttack()
    // - DoorAttackSequence()
    // - ExecuteDoorPunch()
    // CAM 04 Door position (5) is no longer used in the game
    
    void PlayGlassMovementSound()
    {
        if (runningBehindGlassSource != null && runningBehindGlassSound != null)
        {
            runningBehindGlassSource.clip = runningBehindGlassSound;
            runningBehindGlassSource.Play();
        }
    }
    
    void SimulateGlassMovement()
    {
        // X-axis movement simulation for glass positions
        Vector3 originalPos = transform.position;
        StartCoroutine(MovementPhysics(originalPos, Vector3.right * glassMovementDistance, 2f));
    }
    
    void PlayVentilationJumpSound()
    {
        if (oraxJumpSource != null && oraxJumpSound != null)
        {
            oraxJumpSource.clip = oraxJumpSound;
            oraxJumpSource.volume = 1.2f; // Zvýšit hlasitost o 20%
            Debug.Log("[OryxAI] Playing Ventilation Jump Sound - Volume: 120%");
            oraxJumpSource.Play();
        }
    }
    
    void PlayMetalDetectorSound()
    {
        if (metalDetectorSource != null && metalDetectorSound != null)
        {
            metalDetectorSource.clip = metalDetectorSound;
            metalDetectorSource.Play();
        }
    }
    
    void SimulateVentilationJump()
    {
        // Y-axis jump simulation for ventilation positions
        Vector3 originalPos = transform.position;
        StartCoroutine(MovementPhysics(originalPos, Vector3.up * ventilationJumpHeight, 1.5f));
    }
    
    IEnumerator MovementPhysics(Vector3 startPos, Vector3 direction, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float height = Mathf.Sin(progress * Mathf.PI); // Arc movement
            
            transform.position = startPos + direction * height;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = startPos; // Return to original position
    }
    
    void StartOfficeAttack()
    {
        isInOffice = true;
        officeJumpscareCoroutine = StartCoroutine(OfficeJumpscareSequence());
    }
    
    IEnumerator OfficeJumpscareSequence()
    {
        // AI-based jumpscare timing - každých 15 sekund losování
        while (isInOffice)
        {
            yield return new WaitForSeconds(15f);
            
            // Losování podle AI chance
            float aiChance = CalculateAIChance();
            if (Random.value <= aiChance)
            {
                // Execute jumpscare
                yield return StartCoroutine(ExecuteOfficeJumpscare());
                break; // End the sequence after jumpscare
            }
        }
    }
    
    IEnumerator ExecuteOfficeJumpscare()
    {
        Debug.Log("[OryxAI] Executing office jumpscare - position: " + currentPosition);
        
        // KRITICKÉ: Nastav flag pro ochranu před resetem během sekvence
        isInJumpscare = true;
        
        // KRITICKÉ: Zastav office loop sound při začátku jumpscaru
        StopOfficeLoopSound();
        
        // Reset other characters
        ResetAllCharacters();
        
        // 1. Vypnout světla s 0.9s zpožděním před jumpscarem
        yield return StartCoroutine(FlickerLightsJumpscare(false));
        
        // 2. Zobrazit dead panel s videem (0.1s delay pro načtení)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // 3. Přehrát jumpscare sound
        if (jumpscareSource != null && jumpscareSound != null)
        {
            jumpscareSource.clip = jumpscareSound;
            jumpscareSource.Play();
            Debug.Log("[OryxAI] Playing jumpscare sound");
        }
        
        // 4. Spustit game over sekvenci (generátor a světla zůstanou vypnuté po celou dobu)
        StartCoroutine(HandleGameOver());
    }
    
    void ResetAllCharacters()
    {
        // Reset Orax to starting position
        ResetToStartingPosition();
        
        // TODO: Add references to other animatronic scripts and reset them
        // Similar to how Zofeye, Fenerir, or Sledgy do it
        Debug.Log("Orax killed player - resetting all characters");
    }
    
    /// <summary>
    /// Zpracovává ukončení hry po jumpcare - jako u Zofaye
    /// </summary>
    private System.Collections.IEnumerator HandleGameOver()
    {
        Debug.Log($"[OryxAI] Starting game over sequence - waiting {gameOverDelay} seconds");
        
        // IHNED resetuj Orax na začáteční pozici (PŘED čekáním)
        ResetToStartingPosition();
        Debug.Log("[OryxAI] Orax reset to CAM 10 PODIUM immediately after jumpscare");
        
        // Počkej gameOverDelay sekund (pro zobrazení dead panelu a zvuku)
        yield return new WaitForSeconds(gameOverDelay);
        
        // Stop video player
        StopDeadPanelVideo();
        
        // NEJDŘÍV zapnout světla (s 1s pauzou)
        yield return StartCoroutine(FlickerLightsJumpscare(true));
        
        // TEĎ skryj dead panel - AŽ PO zapnutí světel, aby video zakrývalo pauzu
        if (deadPanelOrax != null)
        {
            deadPanelOrax.SetActive(false);
            Debug.Log("[OryxAI] Dead panel hidden after lights turned on");
        }
        
        // KRITICKÉ: Uvolnit jumpscare flag PŘED návratem do menu
        isInJumpscare = false;
        
        // Pokus se ukončit hru přes NightShiftManagement
        if (nightManager != null)
        {
            Debug.Log("[OryxAI] Ending game via NightShiftManagement");
            
            // Použij metodu pro návrat do menu
            nightManager.ForceReturnToMenu();
        }
        else
        {
            // Fallback - pokus se najít NightShiftManagement
            NightShiftManagement foundNightManager = FindFirstObjectByType<NightShiftManagement>();
            if (foundNightManager != null)
            {
                Debug.Log("[OryxAI] Found NightShiftManagement - ending game");
                foundNightManager.ForceReturnToMenu();
            }
            else
            {
                // Pokud NightShiftManagement není k dispozici, zkus jiné metody
                Debug.LogWarning("[OryxAI] NightShiftManagement not found - trying alternative game over methods");
                TryAlternativeGameOver();
            }
        }
        
        // AI remains active - only programmer can control via Inspector
    }
    
    /// <summary>
    /// Alternativní metoda pro ukončení hry pokud NightShiftManagement není k dispozici
    /// </summary>
    private void TryAlternativeGameOver()
    {
        Debug.Log("[OryxAI] Trying alternative game over methods");
        
        // Pokus se najít a deaktivovat herní systémy
        if (shiftTimer != null)
        {
            shiftTimer.gameObject.SetActive(false);
            Debug.Log("[OryxAI] Deactivated ShiftTimer");
        }
        
        // Další fallback možnosti...
        // AI remains active - only programmer can control via Inspector
    }
    
    public void ResetToStartingPosition()
    {
        // doorAttackCoroutine stop REMOVED - no longer exists
        
        if (officeJumpscareCoroutine != null)
        {
            StopCoroutine(officeJumpscareCoroutine);
            officeJumpscareCoroutine = null;
        }
        
        // KRITICKÉ: Zastav office loop sound při resetu
        StopOfficeLoopSound();
        
        // Reset state - AI remains controlled only by programmer via Inspector
        isMoving = false;
        // isAttackingDoor REMOVED
        isInOffice = false;
        
        Debug.Log($"[OryxAI] ResetToStartingPosition: Resetting from position {currentPosition} to 0 (CAM 10 PODIUM)");
        
        // STEJNÁ LOGIKA JAKO PŘI ORAX JUMPSCARU - manuální nastavení pozic
        // Hide all positions first
        for (int i = 0; i < allPositions.Length; i++)
        {
            if (allPositions[i] != null)
                allPositions[i].SetActive(false);
        }
        
        // FORCE SET pozici 0 (CAM 10 PODIUM) - stejně jako po jumpscaru
        currentPosition = 0;
        previousPosition = -1;
        
        // Show only CAM 10 PODIUM - stejně jako po jumpscaru
        if (allPositions[0] != null)
        {
            allPositions[0].SetActive(true);
            Debug.Log("[OryxAI] CAM 10 PODIUM activated, position set to 0");
        }
        
        // Reset AI timing
        SetNextMoveTime();
        timeArrivedAtPosition = Time.time; // Reset position time tracking
        nightStartTime = Time.time; // Reset night start time
        
        // previousDoorClosed reset REMOVED - door attack mechanics no longer used
        
        // Reset AppierSound system for new night
        appierSoundPlayedThisNight = false;
        playerWasOnCameras = false;
        
        // Reset Ventilation Wait System
        isWaitingInVentilation = false;
        isWaitingForCameras = false;
        ventilationWaitTimer = 0f;
        ventilationAccumulatedTime = 0f; // Reset progress memory
        
        // Reset Laser Defense System
        isLaserDefenseBlocking = false;
        laserDefenseTimer = 0f;
        
        // Reset GLASS Movement System - podle vzoru Sledgy
        isMovingGlassLRToLR2 = false;
        isMovingGlassRLToRL2 = false;
        isAtGlassLR2Position = false;
        isAtGlassRL2Position = false;
        
        // Stop GLASS movement coroutines
        if (glassLRMovementCoroutine != null)
        {
            StopCoroutine(glassLRMovementCoroutine);
            glassLRMovementCoroutine = null;
        }
        
        if (glassRLMovementCoroutine != null)
        {
            StopCoroutine(glassRLMovementCoroutine);
            glassRLMovementCoroutine = null;
        }
        
        // Vrať GLASS GameObjecty na původní pozice
        if (glassLR != null && glassLROriginalPosition != Vector3.zero)
        {
            glassLR.transform.position = glassLROriginalPosition;
            Debug.Log("[OryxAI] GLASS LR returned to original position");
        }
        
        if (glassRL != null && glassRLOriginalPosition != Vector3.zero)
        {
            glassRL.transform.position = glassRLOriginalPosition;
            Debug.Log("[OryxAI] GLASS RL returned to original position");
        }
        
        Debug.Log("[OryxAI] Reset to starting position complete - AppierSound, Laser Defense & GLASS Movement reset for new night");
    }
    
    // Public method to be called at the start of each night
    public void InitializeNewNight()
    {
        Debug.Log("[OryxAI] ===== NEW NIGHT INITIALIZATION =====");
        Debug.Log($"[OryxAI] Initializing new night - current position before reset: {currentPosition}");
        Debug.Log($"[OryxAI] Current AI status before new night: enableOraxAI={enableOraxAI}, currentAILevel={currentAILevel}");
        
        // Update AI level for new night
        UpdateAILevel();
        Debug.Log($"[OryxAI] New night AI Level: {currentAILevel} - AI control via Inspector only");
        
        // Reset to starting position
        ResetToStartingPosition();
        
        // Additional safety reset for new night
        nightStartTime = Time.time;
        
        Debug.Log($"[OryxAI] New night initialization completed - Orax ready at CAM 10 PODIUM, enableOraxAI={enableOraxAI}");
        
        // Inicializuj sledování jiných dead panelů
        InitializeOtherCharacterDeathDetection();
    }
    
    public void OnPlayerDeath()
    {
        Debug.Log("[OryxAI] Player death detected - resetting");
        ResetToStartingPosition();
    }
    
    #region Jumpscare Light & Video System
    
    private System.Collections.IEnumerator FlickerLightsJumpscare(bool turnOn)
    {
        if (!turnOn)
        {
            // Zastavit zvuk generátoru při vypnutí světel
            if (electricityGeneratorAudioSource != null && electricityGeneratorAudioSource.isPlaying)
            {
                electricityGeneratorAudioSource.Stop();
            }
            
            // Před vypnutím světel přehrát zvuk elektřiny (pouze jednou)
            if (lightOffAudioSource != null && lightOffSound != null && !lightOffAudioSource.isPlaying)
            {
                lightOffAudioSource.clip = lightOffSound;
                lightOffAudioSource.Play();
            }
        }
        
        // Ovládání světel
        if (mainLight != null)
        {
            mainLight.SetActive(turnOn);
        }
        if (parentLight != null)
        {
            parentLight.SetActive(turnOn);
        }
        
        // Pokud vypínáme světla, počkat 0.9s před jumpscarem
        if (!turnOn)
        {
            Debug.Log($"[OryxAI] Lights turned OFF - waiting {lightOffBeforeJumpscare}s before jumpscare");
            yield return new WaitForSeconds(lightOffBeforeJumpscare);
        }
        else
        {
            // Znovu spustit zvuk generátoru při zapnutí světel
            if (electricityGeneratorAudioSource != null && !electricityGeneratorAudioSource.isPlaying)
            {
                electricityGeneratorAudioSource.Play();
            }
            
            Debug.Log("[OryxAI] Lights turned back ON after game over");
            yield return new WaitForSeconds(lightOnAfterDelay);
        }
    }
    
    #endregion
    
    #region Video Player Management
    
    private System.Collections.IEnumerator ShowDeadPanelWithVideoDelay()
    {
        // Nejdřív aktivuj video GameObject a připrav video
        PrepareDeadPanelVideo();
        
        // Počkej 0.2s aby se video stihlo načíst před zobrazením panelu
        yield return new WaitForSeconds(0.2f);
        
        // Teď zobraz dead panel a spusť video
        StartDeadPanelVideo();
    }
    
    private void PrepareDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.gameObject.SetActive(true);
            deadPanelVideoPlayer.Prepare();
            deadPanelVideoPlayer.frame = 0;
            Debug.Log("[OryxAI] Dead panel video prepared");
        }
    }
    
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null && deadPanelOrax != null)
        {
            deadPanelOrax.SetActive(true);
            deadPanelVideoPlayer.Play();
            Debug.Log("[OryxAI] Dead panel video started");
        }
    }
    
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Stop();
            deadPanelVideoPlayer.frame = 0;
            // NEODEBIREJ GameObject.SetActive(false) - to se dělá jen přes deadPanelOrax.SetActive(false)
            
            Debug.Log("[OryxAI] Dead panel video stopped (GameObject stays active until ForceReturnToMenu)");
        }
    }
    
    #endregion
    
    #region Office Loop Sound System
    
    /// <summary>
    /// Spustí loop zvuk když Orax vstoupí do kanceláře (pozice 17)
    /// </summary>
    private void StartOfficeLoopSound()
    {
        if (officeLoopAudioSource != null && officeLoopSound != null && !officeLoopAudioSource.isPlaying)
        {
            officeLoopAudioSource.clip = officeLoopSound;
            officeLoopAudioSource.loop = true;
            officeLoopAudioSource.Play();
            Debug.Log("[OryxAI] Office loop sound started");
        }
    }
    
    /// <summary>
    /// Zastaví loop zvuk (při jumpscaru nebo opuštění kanceláře)
    /// </summary>
    private void StopOfficeLoopSound()
    {
        if (officeLoopAudioSource != null && officeLoopAudioSource.isPlaying)
        {
            officeLoopAudioSource.Stop();
            Debug.Log("[OryxAI] Office loop sound stopped");
        }
    }
    
    /// <summary>
    /// Spravuje office loop sound - spustí/zastaví podle pozice a stavu
    /// </summary>
    private void HandleOfficeLoopSound(bool menuIsActive)
    {
        // Podmínky pro přehrávání office loop sound:
        // 1. Orax je v kanceláři (pozice 17)
        // 2. isInOffice je true (Orax provádí office attack)
        // 3. NENÍ během jumpscaru (isInJumpscare)
        // 4. Menu NENÍ aktivní
        bool shouldPlayLoopSound = (currentPosition == 17) && isInOffice && !isInJumpscare && !menuIsActive;
        
        // DEBUG: Loguj hodnoty podmínek
        if (currentPosition == 17 || isInOffice)
        {
            Debug.Log($"[OryxAI] Office Loop Check - Position:{currentPosition} isInOffice:{isInOffice} isInJumpscare:{isInJumpscare} menuActive:{menuIsActive} shouldPlay:{shouldPlayLoopSound}");
        }
        
        if (shouldPlayLoopSound)
        {
            // Spustit loop sound pokud ještě neběží
            StartOfficeLoopSound();
        }
        else
        {
            // Zastavit loop sound pokud běží
            StopOfficeLoopSound();
        }
    }
    
    #endregion
    
    public void OnNightEnd()
    {
        Debug.Log("[OryxAI] ===== NIGHT END DETECTED - FORCE RESET =====");
        Debug.Log("[OryxAI] Night ended (6AM) - resetting and preparing for next night");
        
        // KRITICKÉ: Zastav všechny coroutiny (včetně kill timeru v kanceláři)
        StopAllCoroutines();
        
        // FORCE RESET na pozici 0 (CAM 10 PODIUM)
        if (allPositions[currentPosition] != null)
            allPositions[currentPosition].SetActive(false);
            
        currentPosition = 0;
        previousPosition = -1;
        if (allPositions[0] != null)
        {
            allPositions[0].SetActive(true);
            Debug.Log("[OryxAI] NEW NIGHT - ORAX FORCED TO CAM 10 PODIUM (position 0)");
        }
        
        // Reset všech mechanik
        isMoving = false;
        // isAttackingDoor REMOVED
        isInOffice = false;
        timeArrivedAtPosition = Time.time;
        nightStartTime = Time.time;
        SetNextMoveTime();
        
        // Reset AppierSound pro novou noc
        appierSoundPlayedThisNight = false;
        playerWasOnCameras = false;
        
        Debug.Log($"[OryxAI] NEW NIGHT RESET COMPLETE - Orax at CAM 10 PODIUM, AI Level: {currentAILevel}, all coroutines stopped");
    }
    
    public void OnEscapeMenu()
    {
        Debug.Log("[OryxAI] ===== ESC MENU DETECTED - FORCE RESET =====");
        Debug.Log("[OryxAI] ESC menu opened - resetting");
        ResetToStartingPosition();
    }
    

    
    // Utility Methods
    bool IsMenuActive()
    {
        // Check if any menu panel is active - AI = 0 when menu is active
        if (menuPanels != null)
        {
            foreach (GameObject panel in menuPanels)
            {
                if (panel != null && panel.activeInHierarchy)
                    return true;
            }
        }
        
        // Also check for Escape key
        return Input.GetKey(KeyCode.Escape);
    }
    
    bool IsPlayerOnCameras()
    {
        // KRITICKÉ: V menu nebo Custom Night panelu hráč NENÍ na kamerách
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return false;
        }
        
        // CamsOff button active = player on cameras
        // CamsOn button active = player in office  
        // Neither active = player in office
        if (camsOffButton != null && camsOffButton.activeInHierarchy)
        {
            return true; // Player on cameras
        }
        return false; // Player in office
    }
    
    bool IsThatcherBlocking()
    {
        // Thatcher blocks Orax completely when active
        // Especially stops interval when Orax is in ventilation
        // TODO: Add Thatcher script reference and check if active
        return false; // placeholder - implement when Thatcher script is available
    }
    
    bool IsLaserDefenseActive()
    {
        if (ventilationLasers == null) return false;
        
        // Check if laser defense is active for current ventilation position
        if (currentPosition >= 11 && currentPosition <= 16)
        {
            int laserIndex = currentPosition - 11;
            if (laserIndex < laserBarriers.Length && laserBarriers[laserIndex] != null)
            {
                return laserBarriers[laserIndex].activeInHierarchy;
            }
        }
        
        return false;
    }
    
    void HandleVentilationWaitSystem()
    {
        // VENTILAČNÍ ČEKACÍ SYSTÉM - podle vzoru Zofaye u dveří:
        // Orax přijde do ventilace → čeká 10 sekund → pak se může rozhodnout kam jít
        // Pokud se zapne laser během čekání → vrátí se na kameru (laser defense)
        // Pokud se laser nezapne po 10 sekundách → může jít do KANCELAR (pouze pokud hráč na kamerách)
        
        if (currentPosition < 11 || currentPosition > 16)
        {
            // OPRAVA: Orax NENÍ ve ventilaci - resetuj všechny ventilation blocking stavy
            if (isWaitingInVentilation || isLaserDefenseBlocking || isWaitingForCameras)
            {
                Debug.Log($"[OryxAI] Orax left ventilation (position {currentPosition}) - resetting all ventilation blocking states");
                isWaitingInVentilation = false;
                isWaitingForCameras = false;
                ventilationWaitTimer = 0f;
                ventilationAccumulatedTime = 0f; // Reset progress memory when leaving ventilation
                isLaserDefenseBlocking = false;
                laserDefenseTimer = 0f;
            }
            return; // Není ve ventilaci - nic nedělej
        }
        
        bool laserCurrentlyActive = IsLaserDefenseActive();
        
        if (laserCurrentlyActive)
        {
            // LASER JE ZAPNUTÝ - aktivní obrana (stejné jako dříve)
            if (!isLaserDefenseBlocking)
            {
                // Laser se právě zapnul - přeruš čekání a začni laser defense
                isWaitingInVentilation = false; // Přeruš normální čekání
                ventilationWaitTimer = 0f;
                
                isLaserDefenseBlocking = true;
                laserDefenseTimer = laserDefenseRequiredTime; // 10 sekund
                
                Debug.Log($"[OryxAI] ===== LASEROVÁ OBRANA AKTIVOVÁNA =====");
                Debug.Log($"[OryxAI] Čekání ve ventilaci přerušeno na {ventilationAccumulatedTime:F1}s! Orax uvězněn na pozici {currentPosition}! Vrátí se na kameru za {laserDefenseRequiredTime} sekund (100% šance)");
            }
            else
            {
                // Laser stále aktivní - odpočítávej laser defense a pokračuj v progress
                laserDefenseTimer -= Time.deltaTime;
                ventilationAccumulatedTime += Time.deltaTime; // CONTINUE PROGRESS aj počas laser defense
                
                if (laserDefenseTimer > 0f)
                {
                    // Stále musí čekat - BLOKUJE POHYB
                    if (laserDefenseTimer % 2f < Time.deltaTime) // Log každé 2 sekundy
                        Debug.Log($"[OryxAI] Laser defense countdown: {laserDefenseTimer:F1}s remaining (will return to camera at 0)");
                    return; // Blokuje pohyb
                }
                else
                {
                    // Čas vypršel - FORCE RETURN na kameru (100% šance)
                    Debug.Log("[OryxAI] ===== LASER DEFENSE TIMER EXPIRED =====");
                    Debug.Log($"[OryxAI] Orax survived {laserDefenseRequiredTime} seconds! FORCING return to camera (100% chance)");
                    
                    // Najdi odpovídající kameru pro tuto ventilaci
                    if (ventToCameraMapping.ContainsKey(currentPosition))
                    {
                        int returnCamera = ventToCameraMapping[currentPosition];
                        
                        Debug.Log($"[OryxAI] Forcing Orax return from vent position {currentPosition} to camera position {returnCamera}");
                        
                        // FORCE MOVE - resetuj VŠECHNY ventilation stavy
                        isLaserDefenseBlocking = false;
                        laserDefenseTimer = 0f;
                        isWaitingInVentilation = false;
                        isWaitingForCameras = false;
                        ventilationWaitTimer = 0f;
                        
                        // DŮLEŽITÉ: Reset movement timer aby se mohl hýbat po návratu
                        SetNextMoveTime();
                        timeArrivedAtPosition = Time.time; // Reset času příchodu
                        
                        Debug.Log($"[OryxAI] All ventilation states reset after laser defense return - next move in {movementInterval}s");
                        
                        // Immediate move to camera
                        MoveToPosition(returnCamera);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[OryxAI] No camera mapping found for vent position {currentPosition}!");
                        isLaserDefenseBlocking = false;
                    }
                }
            }
        }
        else
        {
            // LASER NENÍ ZAPNUTÝ - normální čekání ve ventilaci (jako Zofaye u dveří)
            
            // KONTROLA: Hráč právě zapnul kamery (CamsOn klik) - okamžitý přechod do KANCELAR
            bool playerJustOpenedCameras = !playerWasOnCameras && IsPlayerOnCameras();
            if (playerJustOpenedCameras && (isWaitingInVentilation || isWaitingForCameras))
            {
                Debug.Log("[OryxAI] ===== CAMS ON CLICKED =====");
                Debug.Log($"[OryxAI] Hráč zapnul kamery - Orax okamžitě přechází z ventilace {currentPosition} do KANCELAR");
                
                // Reset všech ventilation stavů
                isWaitingInVentilation = false;
                isWaitingForCameras = false;
                ventilationWaitTimer = 0f;
                ventilationAccumulatedTime = 0f;
                
                // Reset movement timer
                SetNextMoveTime();
                timeArrivedAtPosition = Time.time;
                
                // Okamžitý přechod do KANCELAR
                MoveToPosition(17); // KANCELAR
                return;
            }
            
            if (isLaserDefenseBlocking)
            {
                // Laser se právě vypnul během laser defense - REMEMBER PROGRESS!
                Debug.Log("[OryxAI] Laser deactivated during defense - switching to normal ventilation wait WITH PROGRESS MEMORY");
                isLaserDefenseBlocking = false;
                laserDefenseTimer = 0f;
                
                // Začni normální čekání - ale PAMATUJ SI PROGRES
                isWaitingInVentilation = true;
                float remainingTime = ventilationWaitRequiredTime - ventilationAccumulatedTime;
                ventilationWaitTimer = Mathf.Max(0f, remainingTime); // Nikdy nie menej ako 0
                
                Debug.Log($"[OryxAI] PROGRESS MEMORY: Accumulated {ventilationAccumulatedTime:F1}s, remaining {ventilationWaitTimer:F1}s until KANCELAR opportunity");
            }
            else if (!isWaitingInVentilation)
            {
                // Právě přišel do ventilace - začni čekat (jako Zofaye u dveří)
                isWaitingInVentilation = true;
                ventilationWaitTimer = ventilationWaitRequiredTime; // Prvýkrát - plných 10 sekúnd
                ventilationAccumulatedTime = 0f; // Reset progress pre nový vstup
                
                Debug.Log($"[OryxAI] ===== VENTILATION WAIT STARTED =====");
                Debug.Log($"[OryxAI] Orax arrived at vent position {currentPosition} - waiting {ventilationWaitRequiredTime} seconds before KANCELAR opportunity");
            }
            else if (isWaitingInVentilation)
            {
                // Čekání pokračuje - odpočítávej a aktualizuj progres
                ventilationWaitTimer -= Time.deltaTime;
                ventilationAccumulatedTime += Time.deltaTime; // UPDATE PROGRESS MEMORY
                
                if (ventilationWaitTimer > 0f)
                {
                    // Stále čeká
                    if (ventilationWaitTimer % 2f < Time.deltaTime) // Log každé 2 sekundy
                    {
                        Debug.Log($"[OryxAI] Ventilation wait: {ventilationWaitTimer:F1}s remaining (progress: {ventilationAccumulatedTime:F1}s/{ventilationWaitRequiredTime}s)");
                    }
                    return; // Blokuje pohyb
                }
                else
                {
                    // Čekání skončilo - může se rozhodnout kam jít
                    Debug.Log("[OryxAI] ===== VENTILATION WAIT COMPLETED =====");
                    Debug.Log($"[OryxAI] Orax waited {ventilationWaitRequiredTime} seconds - can now move to KANCELAR or stay");
                    
                    isWaitingInVentilation = false;
                    ventilationWaitTimer = 0f;
                    ventilationAccumulatedTime = 0f; // Reset po dokončení
                    
                    // Rozhodnutí: jít do KANCELAR pokud hráč sleduje kamery
                    if (IsPlayerOnCameras())
                    {
                        Debug.Log("[OryxAI] Player on cameras - Orax moving to KANCELAR after wait");
                        
                        // Reset movement timer
                        SetNextMoveTime();
                        timeArrivedAtPosition = Time.time;
                        
                        // Move to office
                        MoveToPosition(17); // KANCELAR
                        return;
                    }
                    else
                    {
                        Debug.Log("[OryxAI] Hráč NENÍ na kamerách - Orax čeká ve ventilaci dokud hráč nezapne kamery");
                        
                        // NOVÉ: Nastav stav čekání na kamery - Orax je připraven okamžitě jít do KANCELAR
                        isWaitingForCameras = true;
                        Debug.Log("[OryxAI] ===== ČEKÁNÍ NA KAMERY ===== Orax je připraven okamžitě jít do KANCELAR po kliknutí CamsOn");
                        Debug.Log("[OryxAI] HRÁČ SELHAL! Bez laserů už se Orax NEMŮŽE vrátit na kamery - pouze čeká na CamsOn klik!");
                        
                        // Zůstává ve ventilaci a čeká na CamsOn klik - ŽÁDNÉ další timery!
                        SetNextMoveTime();
                        return;
                    }
                }
            }
            else if (isWaitingForCameras)
            {
                // Orax už počkal 10 sekund a nyní pouze čeká na CamsOn klik - ŽÁDNÉ TIMERY!
                Debug.Log("[OryxAI] Orax čeká na CamsOn klik - hráč selhal s lasery, návrat na kamery ZABLOKOVÁN");
                return; // Blokuje další pohyb dokud hráč nezapne kamery
            }
        }
    }
    
    // AI Calculation Methods
    float CalculateAIChance()
    {
        UpdateAILevel();
        currentAIChance = currentAILevel / 20f; // AI 20 = 100% chance
        Debug.Log($"[OryxAI] CalculateAIChance - AI Level: {currentAILevel}, Chance: {currentAIChance:F2} ({currentAIChance * 100:F0}%)");
        return currentAIChance;
    }
    
    void UpdateAILevel()
    {
        Debug.Log($"[OryxAI] UpdateAILevel called - useAdvancedAISystem: {useAdvancedAISystem}, manualAILevel: {manualAILevel}");
        
        if (useAdvancedAISystem)
        {
            // Používá advanced systém jako Zofaye - AI se mění podle noci a času
            int night = GetCurrentNight();
            int hour = GetCurrentHour();
            currentAILevel = GetEffectiveAILevel(night, hour);
            currentNight = night;
            currentHour = hour;
            usingManualAI = false;
            systemStatusDisplay = "Advanced AI System: ENABLED";
            currentAISourceDisplay = $"Night {night}, Hour {hour}AM";
            Debug.Log($"[OryxAI] Using Advanced AI: Night {night}, Hour {hour}, AI Level: {currentAILevel}");
        }
        else
        {
            // Manual AI systém - funguje pouze když advanced systém je vypnutý
            currentAILevel = Mathf.Clamp(manualAILevel, 0, 20);
            usingManualAI = true;
            systemStatusDisplay = "Advanced AI System: DISABLED";
            currentAISourceDisplay = $"Manual AI: {currentAILevel}";
            Debug.Log($"[OryxAI] Using Manual AI: {currentAILevel}, usingManualAI: {usingManualAI}");
        }
    }
    
    int GetEffectiveAILevel(int night, int hour)
    {
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return 0;
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("ORAX");
            if (customAI >= 0)
                return customAI;
        }
        
        if (aiSettings == null || aiSettings.Length == 0) return 0;
        
        int nightIndex = Mathf.Clamp(night - 1, 0, aiSettings.Length - 1);
        if (aiSettings[nightIndex] == null || aiSettings[nightIndex].hourlyAI == null) return 0;
        
        return aiSettings[nightIndex].hourlyAI.GetAIForHour(hour);
    }
    
    // Public methods for external control
    public void SetAILevel(int newLevel)
    {
        Debug.Log($"[OryxAI] SetAILevel called with newLevel: {newLevel} (from: {manualAILevel}), allowExternalAIControl: {allowExternalAIControl}, useAdvancedAISystem: {useAdvancedAISystem}");
        
        // Zabránit automatickému nastavení pokud není povoleno
        if (!allowExternalAIControl && newLevel != 0)
        {
            Debug.LogWarning($"[OryxAI] External AI control is disabled! Ignoring SetAILevel({newLevel}). Enable 'allowExternalAIControl' in Inspector to allow external AI control.");
            return;
        }
        
        // Pokud je advanced systém aktivní, manual AI se ignoruje (kromě resetování na 0)
        if (useAdvancedAISystem && newLevel != 0)
        {
            Debug.LogWarning($"[OryxAI] Advanced AI System is active! Manual AI ignored except for reset to 0. Current AI controlled by Night/Hour system.");
            return;
        }
        
        manualAILevel = Mathf.Clamp(newLevel, 0, 20);
        Debug.Log($"[OryxAI] Manual AI Level set to: {manualAILevel}");
        UpdateAILevel(); // Update AI immediately when changed
    }
    
    public void SetNightAndHour(int night, int hour)
    {
        currentNight = Mathf.Clamp(night, 1, 6);
        currentHour = Mathf.Clamp(hour, 0, 6);
        UpdateAILevel(); // Update AI immediately when night/hour changes
    }
    
    public void SetSpecificNightHourAI(int night, int hour, int aiLevel)
    {
        if (aiSettings == null || aiSettings.Length == 0) return;
        
        int nightIndex = Mathf.Clamp(night - 1, 0, aiSettings.Length - 1);
        if (aiSettings[nightIndex] == null || aiSettings[nightIndex].hourlyAI == null) return;
        
        aiSettings[nightIndex].hourlyAI.SetAIForHour(hour, aiLevel);
        Debug.Log($"[OryxAI] Set Night {night}, Hour {hour} AI to {aiLevel}");
    }
    
    public int GetCurrentAILevel()
    {
        return currentAILevel;
    }
    
    public int GetAIForNightAndHour(int night, int hour)
    {
        return GetEffectiveAILevel(night, hour);
    }
    
    public void ActivateAI(bool active)
    {
        enableOraxAI = active;
        Debug.Log($"[OryxAI] AI activation changed via code: {enableOraxAI}");
    }
    
    public int GetCurrentPosition()
    {
        return currentPosition;
    }
    
    public bool IsInOffice()
    {
        return isInOffice;
    }
    
    // IsAttackingDoor() REMOVED - door attack mechanics no longer used
    
    /// <summary>
    /// Kontrola zda je Cavira u dveří na CAM 04
    /// </summary>
    private bool IsCaviraAtDoors()
    {
        if (caviraAI == null) return false;
        return caviraAI.IsAtDoors;
    }
    
    /// <summary>
    /// Kontrola zda je Ianid u dveri
    /// </summary>
    private bool IsIanidAtDoors()
    {
        if (ianidAI == null) return false;
        return ianidAI.IsAtDoors;
    }
    
    /// <summary>
    /// Public property pro Caviru - kontrola zda je Orax u dveří CAM 04
    /// OBSOLETE: Position 5 (CAM 04 Door) has been removed - always returns false
    /// </summary>
    public bool IsAtCAM04Door => false; // Position 5 removed - always false
    
    bool CheckIfPlayerOnCameras()
    {
        // Check if player is currently viewing cameras
        if (cameraSystem != null)
        {
            // Use reflection to access private CamerasOpen field
            try
            {
                System.Reflection.FieldInfo field = cameraSystem.GetType().GetField("CamerasOpen", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (bool)field.GetValue(cameraSystem);
                }
            }
            catch (System.Exception)
            {
                // Fallback if reflection fails
                return false;
            }
        }
        return false;
    }
    
    void CheckAppierSoundTrigger()
    {
        // JEDNODUCHÁ PODMÍNKA podle požadavku:
        // Pokud je Orax na pozici KANCELAR (17) + hráč klikne na CamsOff (zavře kamery)
        // → v ten moment se přehraje AppierSound + označí se jako přehraný pro tuto noc
        
        // Kontrola: Orax je na pozici KANCELAR
        bool oraxInOffice = (currentPosition == 17);
        
        // Kontrola: Hráč právě kliknul na CamsOff (zavřel kamery)
        bool playerJustClickedCamsOff = false;
        if (camsOffButton != null)
        {
            // CamsOff tlačítko je aktivní = hráč je na kamerách
            // Pokud hráč klikne na CamsOff, ukončí sledování kamer
            bool camsOffActive = camsOffButton.activeInHierarchy;
            bool playerJustClosedCameras = playerWasOnCameras && !camsOffActive;
            playerJustClickedCamsOff = playerJustClosedCameras;
        }
        
        // HLAVNÍ PODMÍNKA - AppierSound se přehraje POUZE pokud:
        // 1. Orax je v KANCELAR pozici (17)
        // 2. Hráč právě kliknul na CamsOff (zavřel kamery) 
        // 3. AppierSound se ještě nepřehrál v této noci
        if (oraxInOffice && playerJustClickedCamsOff && !appierSoundPlayedThisNight)
        {
            Debug.Log("[OryxAI] TRIGGER AppierSound - Orax in KANCELAR + Player clicked CamsOff");
            PlayAppierSoundOrax();
            appierSoundPlayedThisNight = true; // Označit jako přehraný pro tuto noc
        }
        
        // Update tracking pro příští frame
        playerWasOnCameras = IsPlayerOnCameras();
        
        // Detekce smrti od jiných animatroniků
        CheckForOtherCharacterDeath();
        
        // Kontrola změny AI levelu na 0 pro automatický reset
        CheckForAILevelReset();
    }
    
    void PlayAppierSoundOrax()
    {
        try
        {
            Debug.Log("[OryxAI] ===== PLAYING APPIER SOUND ORAX =====");
            Debug.Log("[OryxAI] Trigger conditions: Orax in KANCELAR + Player clicked CamsOff + First time this night");
            
            // Vytvoř nebo najdi AudioSource pro AppierSound - podle vzoru ThatcherAI
            if (appierSoundOraxSource == null)
            {
                Debug.Log("[OryxAI] AppierSound AudioSource is null - attempting to create/find one");
                
                appierSoundOraxSource = GetComponent<AudioSource>();
                
                if (appierSoundOraxSource == null)
                {
                    appierSoundOraxSource = gameObject.AddComponent<AudioSource>();
                    Debug.Log("[OryxAI] Created new AudioSource component for AppierSound");
                }
                else
                {
                    Debug.Log("[OryxAI] Found existing AudioSource component");
                }
            }
            
            if (appierSoundOraxSource != null && appierSoundOraxSound != null)
            {
                Debug.Log($"[OryxAI] AudioSource found: {appierSoundOraxSource.name}");
                Debug.Log($"[OryxAI] Audio clip assigned: {appierSoundOraxSound.name} (length: {appierSoundOraxSound.length:F2}s)");
                
                // Nastav AudioSource properties pro kvalitní přehrání - podle ThatcherAI vzoru
                appierSoundOraxSource.clip = appierSoundOraxSound;
                appierSoundOraxSource.pitch = 1f;
                appierSoundOraxSource.volume = 0.4f; // 40% hlasitost pro Orax AppierSound
                appierSoundOraxSource.loop = false;
                appierSoundOraxSource.playOnAwake = false;
                appierSoundOraxSource.spatialBlend = 0f; // 2D zvuk pro UI warning
                
                Debug.Log($"[OryxAI] AudioSource settings: Volume={appierSoundOraxSource.volume}, Pitch={appierSoundOraxSource.pitch}, SpatialBlend={appierSoundOraxSource.spatialBlend}");
                
                // Přehraj zvuk
                appierSoundOraxSource.Play();
                
                Debug.Log($"[OryxAI] AppierSound PLAYING: {appierSoundOraxSource.isPlaying}");
                Debug.Log($"[OryxAI] Audio playback status: Volume={appierSoundOraxSource.volume:F2}, Time={appierSoundOraxSource.time:F2}s/{appierSoundOraxSound.length:F2}s");
                
                if (!appierSoundOraxSource.isPlaying)
                {
                    Debug.LogWarning("[OryxAI] WARNING: AppierSound is NOT playing despite Play() call!");
                }
            }
            else
            {
                Debug.LogError("[OryxAI] CRITICAL: Cannot play AppierSound!");
                
                if (appierSoundOraxSound == null)
                    Debug.LogError("[OryxAI] ERROR: appierSoundOraxSound audio clip is NULL! Assign it in Inspector.");
                    
                if (appierSoundOraxSource == null)
                    Debug.LogError("[OryxAI] ERROR: appierSoundOraxSource AudioSource is NULL!");
            }
            
            Debug.Log("[OryxAI] ===== APPIER SOUND ORAX EXECUTION COMPLETE =====");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OryxAI] EXCEPTION in PlayAppierSoundOrax: {ex.Message}");
            Debug.LogError($"[OryxAI] Exception stack trace: {ex.StackTrace}");
        }
    }
    
    #region Advanced AI System Methods
    
    /// <summary>
    /// Inicializuje advanced AI systém jako u Zofaye
    /// </summary>
    void InitializeAdvancedAISystem()
    {
        // Initialize AI settings array if not set
        if (aiSettings == null || aiSettings.Length != 6)
        {
            aiSettings = new OryxNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                aiSettings[i] = new OryxNightConfiguration();
                aiSettings[i].nightName = $"Night {i + 1}";
                aiSettings[i].hourlyAI = new OryxHourlyAI();
                
                // Nastavím defaultní AI hodnoty (převzaté ze starého systému)
                switch (i + 1)
                {
                    case 1: // Night 1
                        aiSettings[i].hourlyAI.hour12AM = 0;
                        aiSettings[i].hourlyAI.hour1AM = 0;
                        aiSettings[i].hourlyAI.hour2AM = 0;
                        aiSettings[i].hourlyAI.hour3AM = 1;
                        aiSettings[i].hourlyAI.hour4AM = 1;
                        aiSettings[i].hourlyAI.hour5AM = 2;
                        break;
                    case 2: // Night 2
                        aiSettings[i].hourlyAI.hour12AM = 0;
                        aiSettings[i].hourlyAI.hour1AM = 1;
                        aiSettings[i].hourlyAI.hour2AM = 1;
                        aiSettings[i].hourlyAI.hour3AM = 2;
                        aiSettings[i].hourlyAI.hour4AM = 3;
                        aiSettings[i].hourlyAI.hour5AM = 4;
                        break;
                    case 3: // Night 3
                        aiSettings[i].hourlyAI.hour12AM = 1;
                        aiSettings[i].hourlyAI.hour1AM = 2;
                        aiSettings[i].hourlyAI.hour2AM = 3;
                        aiSettings[i].hourlyAI.hour3AM = 4;
                        aiSettings[i].hourlyAI.hour4AM = 5;
                        aiSettings[i].hourlyAI.hour5AM = 6;
                        break;
                    case 4: // Night 4
                        aiSettings[i].hourlyAI.hour12AM = 2;
                        aiSettings[i].hourlyAI.hour1AM = 3;
                        aiSettings[i].hourlyAI.hour2AM = 4;
                        aiSettings[i].hourlyAI.hour3AM = 6;
                        aiSettings[i].hourlyAI.hour4AM = 8;
                        aiSettings[i].hourlyAI.hour5AM = 10;
                        break;
                    case 5: // Night 5
                        aiSettings[i].hourlyAI.hour12AM = 3;
                        aiSettings[i].hourlyAI.hour1AM = 5;
                        aiSettings[i].hourlyAI.hour2AM = 7;
                        aiSettings[i].hourlyAI.hour3AM = 9;
                        aiSettings[i].hourlyAI.hour4AM = 12;
                        aiSettings[i].hourlyAI.hour5AM = 15;
                        break;
                    case 6: // Night 6
                        aiSettings[i].hourlyAI.hour12AM = 5;
                        aiSettings[i].hourlyAI.hour1AM = 8;
                        aiSettings[i].hourlyAI.hour2AM = 12;
                        aiSettings[i].hourlyAI.hour3AM = 15;
                        aiSettings[i].hourlyAI.hour4AM = 18;
                        aiSettings[i].hourlyAI.hour5AM = 20;
                        break;
                }
            }
            
            Debug.Log("[OryxAI] Advanced AI system initialized with default values");
        }
        
        if (useAdvancedAISystem)
        {
            Debug.Log("[OryxAI] Advanced AI System: ENABLED");
        }
        else
        {
            Debug.Log("[OryxAI] Advanced AI System: DISABLED - Using Manual AI");
        }
    }
    
    /// <summary>
    /// Automaticky najde system references
    /// </summary>
    void FindSystemReferences()
    {
        if (shiftTimer == null)
        {
            shiftTimer = FindFirstObjectByType<ShiftTimer>();
            if (shiftTimer != null)
                Debug.Log("[OryxAI] Auto-found ShiftTimer reference");
        }
        
        if (nightManager == null)
        {
            nightManager = FindFirstObjectByType<NightShiftManagement>();
            if (nightManager != null)
                Debug.Log("[OryxAI] Auto-found NightShiftManagement reference");
        }
    }
    
    /// <summary>
    /// Zpracovává advanced AI updates podle času a noci
    /// </summary>
    void HandleAdvancedAIUpdates()
    {
        aiUpdateTimer += Time.deltaTime;
        
        if (aiUpdateTimer >= aiUpdateInterval)
        {
            aiUpdateTimer = 0f;
            
            int currentNight = GetCurrentNight();
            int currentHour = GetCurrentHour();
            
            if (currentNight != lastKnownNight || currentHour != lastKnownHour)
            {
                lastKnownNight = currentNight;
                lastKnownHour = currentHour;
                
                Debug.Log($"[OryxAI] Advanced AI - Night/Hour changed to {currentNight}/{currentHour}");
                UpdateAILevel();
            }
        }
    }
    
    /// <summary>
    /// Získá aktuální noc z NightShiftManagement nebo ShiftTimer
    /// </summary>
    int GetCurrentNight()
    {
        if (nightManager != null)
        {
            // Zkusím získat current night z night managera
            try
            {
                System.Reflection.FieldInfo field = nightManager.GetType().GetField("currentNight", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    return (int)field.GetValue(nightManager);
                }
            }
            catch (System.Exception) { }
        }
        
        // Fallback na currentNight proměnnou
        return currentNight;
    }
    
    /// <summary>
    /// Získá aktuální hodinu z ShiftTimer
    /// </summary>
    int GetCurrentHour()
    {
        if (shiftTimer != null)
        {
            // Zkusím získat current hour z shift timera
            try
            {
                System.Reflection.FieldInfo field = shiftTimer.GetType().GetField("currentHour", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    return (int)field.GetValue(shiftTimer);
                }
            }
            catch (System.Exception) { }
        }
        
        // Fallback na currentHour proměnnou
        return currentHour;
    }
    
    // Nová metóda: Rozhodovanie o návrate z ventilace s upravenou šancou
    int GetVentilationReturnPosition()
    {
        Debug.Log($"[OryxAI] ===== VENTILATION RETURN LOGIC =====");
        Debug.Log($"[OryxAI] Current vent position: {currentPosition}, Previous camera position: {previousPosition}");
        
        // Nájdi základnú return kameru pre túto ventiláciu
        if (!ventToCameraMapping.ContainsKey(currentPosition))
        {
            Debug.LogWarning($"[OryxAI] No return camera mapping for vent position {currentPosition}!");
            return -1;
        }
        
        int defaultReturnCamera = ventToCameraMapping[currentPosition];
        Debug.Log($"[OryxAI] Default return camera for vent {currentPosition}: {defaultReturnCamera}");
        
        // Skontroluj či sa má vrátiť na rovnakú kameru kde bol predtým
        if (previousPosition == defaultReturnCamera)
        {
            Debug.Log($"[OryxAI] Default return camera {defaultReturnCamera} matches previous position {previousPosition}");
            
            // ZNÍŽENÁ ŠANCA na návrat na rovnakú kameru (z 100% na 80% = 20% menšia šanca)
            float returnToSameChance = Random.value;
            Debug.Log($"[OryxAI] Return to same camera chance roll: {returnToSameChance:F3}");
            
            if (returnToSameChance < 0.8f)
            {
                Debug.Log($"[OryxAI] Returning to SAME camera {defaultReturnCamera} (chance: {returnToSameChance:F3} < 0.8)");
                return defaultReturnCamera;
            }
            else
            {
                Debug.Log($"[OryxAI] AVOIDING same camera! Looking for alternative... (chance: {returnToSameChance:F3} >= 0.8)");
                
                // Nájdi alternatívnu kameru - použije GetNextPosition logiku z return kamery
                List<int> alternativeCameras = GetAlternativeCameraPositions(defaultReturnCamera);
                
                if (alternativeCameras.Count > 0)
                {
                    int chosenAlternative = alternativeCameras[Random.Range(0, alternativeCameras.Count)];
                    Debug.Log($"[OryxAI] Chose alternative camera: {chosenAlternative} (from {alternativeCameras.Count} options)");
                    return chosenAlternative;
                }
                else
                {
                    Debug.Log("[OryxAI] No alternatives found - returning to default camera");
                    return defaultReturnCamera;
                }
            }
        }
        else
        {
            Debug.Log($"[OryxAI] Default return camera {defaultReturnCamera} differs from previous {previousPosition} - returning normally");
            return defaultReturnCamera;
        }
    }
    
    // Helper metóda: Nájdi alternatívne kamery pre daný return position
    List<int> GetAlternativeCameraPositions(int fromCameraPosition)
    {
        List<int> alternatives = new List<int>();
        
        // Použije rovnakú logiku ako GetNextPosition ale bez ventilation jumps
        switch (fromCameraPosition)
        {
            case 1: // CAM 10
                alternatives.AddRange(new int[] {2, 3, 4}); // CAM 08, CAM 09, CAM 05
                break;
            case 2: // CAM 08
                alternatives.AddRange(new int[] {10, 9, 3}); // CAM 03, CAM 02/CAM 11, CAM 09
                break;
            case 3: // CAM 09
                alternatives.AddRange(new int[] {2, 4}); // CAM 08, CAM 05
                break;
            case 4: // CAM 05
                alternatives.AddRange(new int[] {3}); // CAM 09 only (CAM 04 Door removed)
                break;
            case 6: // CAM 04 Vent Entry
                alternatives.AddRange(new int[] {7}); // GLASS LR only (CAM 04 Door removed)
                break;
            case 9: // CAM 02/CAM 11
                alternatives.AddRange(new int[] {8, 10, 2}); // GLASS RL, CAM 03, CAM 08
                break;
            case 10: // CAM 03
                alternatives.AddRange(new int[] {2, 9}); // CAM 08, CAM 02/CAM 11
                break;
        }
        
        Debug.Log($"[OryxAI] Alternative cameras for position {fromCameraPosition}: [{string.Join(", ", alternatives)}]");
        return alternatives;
    }
    
    // GLASS Proximity Sound System - podle vzoru SledgeAI
    private bool isGlassProximitySoundPlaying = false;
    private float originalGlassProximityVolume = 1f;
    
    private void StartGlassProximitySound(Vector3 startPos, Vector3 endPos)
    {
        Debug.Log($"[OryxAI] Attempting to start GLASS proximity sound - Source: {(runningBehindGlassSource != null ? "OK" : "NULL")}, Clip: {(runningBehindGlassSound != null ? "OK" : "NULL")}");
        
        if (runningBehindGlassSource != null && runningBehindGlassSound != null)
        {
            // Stop any existing sound first
            if (isGlassProximitySoundPlaying)
            {
                runningBehindGlassSource.Stop();
                Debug.Log("[OryxAI] Stopped previous GLASS proximity sound");
            }
            
            float initialVolume = GetGlassProximityVolume(startPos, startPos, endPos);
            
            runningBehindGlassSource.clip = runningBehindGlassSound;
            runningBehindGlassSource.volume = initialVolume;
            runningBehindGlassSource.loop = false; // Ensure it's not looping
            runningBehindGlassSource.Play();
            isGlassProximitySoundPlaying = true;
            
            Debug.Log($"[OryxAI] ===== GLASS PROXIMITY SOUND STARTED =====");
            Debug.Log($"[OryxAI] Clip: {runningBehindGlassSound.name}");
            Debug.Log($"[OryxAI] Initial Volume: {(initialVolume * 100):F0}%");
            Debug.Log($"[OryxAI] AudioSource isPlaying: {runningBehindGlassSource.isPlaying}");
            Debug.Log($"[OryxAI] AudioSource enabled: {runningBehindGlassSource.enabled}");
            Debug.Log($"[OryxAI] AudioSource spatialBlend: {runningBehindGlassSource.spatialBlend}");
        }
        else
        {
            Debug.LogError($"[OryxAI] GLASS PROXIMITY SOUND FAILED - Source: {runningBehindGlassSource}, Clip: {runningBehindGlassSound}");
        }
    }
    
    private void UpdateGlassProximityVolume(Vector3 currentPos, Vector3 startPos, Vector3 endPos)
    {
        if (runningBehindGlassSource != null && isGlassProximitySoundPlaying)
        {
            float proximityVolume = GetGlassProximityVolume(currentPos, startPos, endPos);
            runningBehindGlassSource.volume = proximityVolume;
            
            // Debug log pouze občas aby nezahltilo console
            if (Time.frameCount % 30 == 0)
            {
                float distanceFromStart = Mathf.Abs(currentPos.x - startPos.x);
                Debug.Log($"[OryxAI] GLASS proximity update - X distance: {distanceFromStart:F1} units, Volume: {(proximityVolume * 100):F0}%");
            }
        }
    }
    
    private float GetGlassProximityVolume(Vector3 currentPos, Vector3 startPos, Vector3 endPos)
    {
        // Vypočítej progress pohybu na X-ose (0.0 = start, 1.0 = end)
        float totalDistance = Mathf.Abs(endPos.x - startPos.x);
        if (totalDistance == 0f) return 0.2f; // fallback
        
        float currentDistance = Mathf.Abs(currentPos.x - startPos.x);
        float progress = Mathf.Clamp01(currentDistance / totalDistance);
        
        // Proximity křivka: quiet start (20%) → loud middle (100%) → quiet end (20%)
        float volume;
        if (progress <= 0.5f)
        {
            // První polovina: 20% → 100%
            volume = Mathf.Lerp(0.2f, 1.0f, progress * 2f);
        }
        else
        {
            // Druhá polovina: 100% → 20%
            volume = Mathf.Lerp(1.0f, 0.2f, (progress - 0.5f) * 2f);
        }
        
        // Zkontroluj jestli je hráč na kamerách - pokud ano, sníž volume
        if (IsPlayerOnCameras())
        {
            return volume * 0.2f; // 80% snížení - ztlum na 20%
        }
        
        return volume;
    }
    
    private void StopGlassProximitySound()
    {
        if (runningBehindGlassSource != null && isGlassProximitySoundPlaying)
        {
            runningBehindGlassSource.Stop();
            runningBehindGlassSource.volume = originalGlassProximityVolume; // restore original volume
            isGlassProximitySoundPlaying = false;
            
            Debug.Log("[OryxAI] GLASS proximity sound stopped");
        }
    }
    
    #endregion
    
    #region Other Character Death Detection System
    
    /// <summary>
    /// Inicializuje sledování jiných dead panelů
    /// </summary>
    private void InitializeOtherCharacterDeathDetection()
    {
        if (!enableOtherCharacterDeathDetection || otherCharacterDeadPanels == null)
        {
            Debug.Log("[OryxAI] Other character death detection disabled or no panels assigned");
            return;
        }
        
        otherCharacterDeadPanelStates = new bool[otherCharacterDeadPanels.Length];
        
        for (int i = 0; i < otherCharacterDeadPanels.Length; i++)
        {
            if (otherCharacterDeadPanels[i] != null)
            {
                otherCharacterDeadPanelStates[i] = otherCharacterDeadPanels[i].activeSelf;
            }
        }
        
        Debug.Log($"[OryxAI] Other character death detection initialized for {otherCharacterDeadPanels.Length} panels");
    }
    
    /// <summary>
    /// Kontroluje, zda některý z jiných animatroniků zabil hráče
    /// </summary>
    private void CheckForOtherCharacterDeath()
    {
        if (!enableOtherCharacterDeathDetection || otherCharacterDeadPanels == null || otherCharacterDeadPanelStates == null)
            return;
        
        for (int i = 0; i < otherCharacterDeadPanels.Length && i < otherCharacterDeadPanelStates.Length; i++)
        {
            if (otherCharacterDeadPanels[i] == null)
                continue;
                
            bool currentState = otherCharacterDeadPanels[i].activeSelf;
            
            // Pokud se dead panel právě aktivoval (false -> true)
            if (!otherCharacterDeadPanelStates[i] && currentState)
            {
                string panelName = otherCharacterDeadPanels[i].name;
                Debug.Log($"[OryxAI] ===== OTHER CHARACTER DEATH DETECTED =====");
                Debug.Log($"[OryxAI] Panel '{panelName}' activated - another animatronic killed the player!");
                
                // Reset Orax na počáteční pozici
                ResetAfterOtherCharacterDeath(panelName);
                
                // Update stav pro další detekci
                otherCharacterDeadPanelStates[i] = currentState;
                return; // Pouze jeden death najednou
            }
            
            // Update stav
            otherCharacterDeadPanelStates[i] = currentState;
        }
    }
    
    /// <summary>
    /// NOVÁ METODA: Kontroluje menu panely a okamžitě resetuje - STEJNÁ LOGIKA JAKO PO JUMPSCARU
    /// </summary>
    private void CheckForMenuPanelReset()
    {
        if (menuPanels != null)
        {
            foreach (GameObject menuPanel in menuPanels)
            {
                if (menuPanel != null && menuPanel.activeSelf)
                {
                    // OKAMŽITÝ RESET když je menu panel aktivní - pouze pokud není už na pozici 0
                    if (currentPosition != 0)
                    {
                        Debug.Log($"[OryxAI] ===== MENU PANEL RESET - SAME AS JUMPSCARE =====");
                        Debug.Log($"[OryxAI] Menu panel '{menuPanel.name}' is active - SAME RESET LOGIC as Orax jumpscare");
                        Debug.Log($"[OryxAI] Current position: {currentPosition} -> Force reset to CAM 10 PODIUM (0)");
                        
                        // STEJNÁ LOGIKA JAKO PŘI ORAX JUMPSCARU - manuální nastavení pozic
                        // Hide all positions first
                        for (int i = 0; i < allPositions.Length; i++)
                        {
                            if (allPositions[i] != null)
                                allPositions[i].SetActive(false);
                        }
                        
                        // FORCE SET pozici 0 (CAM 10 PODIUM) - stejně jako po jumpscaru  
                        currentPosition = 0;
                        previousPosition = -1;
                        
                        // Show only CAM 10 PODIUM - stejně jako po jumpscaru
                        if (allPositions[0] != null)
                        {
                            allPositions[0].SetActive(true);
                            Debug.Log("[OryxAI] ✅ CAM 10 PODIUM ACTIVATED - MENU PANEL RESET COMPLETE (same as jumpscare)");
                        }
                        
                        // Reset všech mechanik - STEJNÉ JAKO PO JUMPSCARU
                        isMoving = false;
                        // isAttackingDoor REMOVED
                        isInOffice = false;
                        timeArrivedAtPosition = Time.time;
                        SetNextMoveTime();
                        
                        // Reset AppierSound flag
                        appierSoundPlayedThisNight = false;
                        playerWasOnCameras = false;
                        
                        // Reset Ventilation Wait System
                        isWaitingInVentilation = false;
                        isWaitingForCameras = false;
                        ventilationWaitTimer = 0f;
                        ventilationAccumulatedTime = 0f;
                        
                        // Reset Laser Defense System
                        isLaserDefenseBlocking = false;
                        laserDefenseTimer = 0f;
                        
                        // Reset GLASS Movement System
                        isMovingGlassLRToLR2 = false;
                        isMovingGlassRLToRL2 = false;
                        isAtGlassLR2Position = false;
                        isAtGlassRL2Position = false;
                        
                        // Stop GLASS movement coroutines
                        if (glassLRMovementCoroutine != null)
                        {
                            StopCoroutine(glassLRMovementCoroutine);
                            glassLRMovementCoroutine = null;
                        }
                        
                        if (glassRLMovementCoroutine != null)
                        {
                            StopCoroutine(glassRLMovementCoroutine);
                            glassRLMovementCoroutine = null;
                        }
                        
                        // Return GLASS GameObjects na původní pozice
                        if (glassLR != null && glassLROriginalPosition != Vector3.zero)
                        {
                            glassLR.transform.position = glassLROriginalPosition;
                            glassLR.SetActive(false);
                            Debug.Log("[OryxAI] GLASS LR returned to original position and deactivated");
                        }
                        
                        if (glassRL != null && glassRLOriginalPosition != Vector3.zero)
                        {
                            glassRL.transform.position = glassRLOriginalPosition;
                            glassRL.SetActive(false);
                            Debug.Log("[OryxAI] GLASS RL returned to original position and deactivated");
                        }
                        
                        Debug.Log($"[OryxAI] MENU PANEL RESET COMPLETE - Orax at CAM 10 PODIUM due to menu panel: {menuPanel.name}");
                        Debug.Log("[OryxAI] Used IDENTICAL reset logic as Orax jumpscare - no differences!");
                    }
                    // OPRAVENO: POKRAČUJ v kontrole ostatních panelů místo return
                    // return; // ❌ REMOVED: Toto zablokovalo kontrolu ostatních panelů
                }
            }
        }
    }
    
    /// <summary>
    /// UNIVERZÁLNÍ RESET SYSTÉM - kontroluje všechny možné důvody pro reset  
    /// </summary>
    private void CheckForUniversalReset()
    {
        // KRITICKÉ: Nerestartuj Orax během jumpscaru - ochrana jumpscare sekvence
        if (isInJumpscare)
        {
            return;
        }
        
        // 1. PRIORITNÍ KONTROLA: Menu panely které nastavují AI na 0 - STEJNÁ LOGIKA JAKO JUMPSCARE
        CheckForMenuPanelReset();
        
        // 2. NEPŘETRŽITÁ kontrola AI = 0 (KAŽDÝ FRAME) - OPRAVENO!
        CheckForAILevelReset();
        
        // 3. Kontrola smrti od jiných animatronics
        CheckForOtherCharacterDeath();
    }
    
    /// <summary>
    /// Kontroluje změnu AI levelu na 0 pro automatický reset - OPRAVENO: KAŽDÝ FRAME
    /// </summary>
    private void CheckForAILevelReset()
    {
        // OPRAVENO: Kontroluj KAŽDÝ FRAME, ne jen každou sekundu!
        // Pokud AI je 0 a Orax NENÍ na pozici 0, resetuj ho okamžitě
        if (currentAILevel == 0 && currentPosition != 0)
        {
            Debug.Log("[OryxAI] ===== AI LEVEL 0 DETECTED - IMMEDIATE RESET =====");
            Debug.Log($"[OryxAI] AI = 0 and current position = {currentPosition} (not CAM 10 PODIUM) - FORCING RESET");
            
            // STEJNÁ LOGIKA JAKO PŘI ORAX JUMPSCARU - manuální nastavení pozic
            // Hide all positions first
            for (int i = 0; i < allPositions.Length; i++)
            {
                if (allPositions[i] != null)
                    allPositions[i].SetActive(false);
            }
            
            // FORCE SET pozici 0 (CAM 10 PODIUM) - stejně jako po jumpscaru  
            currentPosition = 0;
            previousPosition = -1;
            
            // Show only CAM 10 PODIUM - stejně jako po jumpscaru
            if (allPositions[0] != null)
            {
                allPositions[0].SetActive(true);
                Debug.Log("[OryxAI] ✅ AI=0 RESET COMPLETE - CAM 10 PODIUM ACTIVATED");
            }
            
            // Reset všech mechanik - STEJNÉ JAKO PO JUMPSCARU
            isMoving = false;
            // isAttackingDoor REMOVED
            isInOffice = false;
            timeArrivedAtPosition = Time.time;
            SetNextMoveTime();
            
            // Reset AppierSound flag
            appierSoundPlayedThisNight = false;
            playerWasOnCameras = false;
            
            // Reset Ventilation Wait System
            isWaitingInVentilation = false;
            isWaitingForCameras = false;
            ventilationWaitTimer = 0f;
            ventilationAccumulatedTime = 0f;
            
            // Reset Laser Defense System
            isLaserDefenseBlocking = false;
            laserDefenseTimer = 0f;
            
            // Reset GLASS Movement System
            isMovingGlassLRToLR2 = false;
            isMovingGlassRLToRL2 = false;
            isAtGlassLR2Position = false;
            isAtGlassRL2Position = false;
            
            // Stop GLASS movement coroutines
            if (glassLRMovementCoroutine != null)
            {
                StopCoroutine(glassLRMovementCoroutine);
                glassLRMovementCoroutine = null;
            }
            
            if (glassRLMovementCoroutine != null)
            {
                StopCoroutine(glassRLMovementCoroutine);
                glassRLMovementCoroutine = null;
            }
            
            // Return GLASS GameObjects na původní pozice
            if (glassLR != null && glassLROriginalPosition != Vector3.zero)
            {
                glassLR.transform.position = glassLROriginalPosition;
                glassLR.SetActive(false);
                Debug.Log("[OryxAI] GLASS LR returned to original position and deactivated");
            }
            
            if (glassRL != null && glassRLOriginalPosition != Vector3.zero)
            {
                glassRL.transform.position = glassRLOriginalPosition;
                glassRL.SetActive(false);
                Debug.Log("[OryxAI] GLASS RL returned to original position and deactivated");
            }
            
            Debug.Log("[OryxAI] IMMEDIATE AI=0 RESET COMPLETE - Orax forced to CAM 10 PODIUM");
        }
        
        // Detekce změny AI levelu pro debug
        if (previousAILevel != currentAILevel)
        {
            Debug.Log($"[OryxAI] AI Level changed: {previousAILevel} -> {currentAILevel}");
            if (currentAILevel == 0)
            {
                Debug.Log("[OryxAI] AI = 0 - Orax should be at CAM 10 PODIUM only!");
            }
            previousAILevel = currentAILevel;
        }
    }
    
    /// <summary>
    /// Kontroluje návrat do hlavního menu - NYNÍ SOUČÁST CheckForMenuPanelReset()
    /// </summary>
    private void CheckForMainMenuReturn()
    {
        // Tato metoda je nyní součástí CheckForMenuPanelReset()
        // Zachováno pro kompatibilitu
    }
    
    /// <summary>
    /// Reset Orax po smrti od jiného animatroniků
    /// </summary>
    private void ResetAfterOtherCharacterDeath(string killerName)
    {
        Debug.Log($"[OryxAI] ===== RESET AFTER DEATH FROM {killerName} =====");
        Debug.Log($"[OryxAI] Orax was at position {currentPosition} when {killerName} killed the player");
        
        // KRITICKÝ RESET - Force návrat na CAM 10 PODIUM
        Debug.Log("[OryxAI] FORCING COMPLETE POSITION RESET after death from other character");
        
        // VYPNI aktuální pozici
        if (currentPosition >= 0 && currentPosition < allPositions.Length && allPositions[currentPosition] != null)
        {
            allPositions[currentPosition].SetActive(false);
            Debug.Log($"[OryxAI] Disabled current position {currentPosition}");
        }
        
        // FORCE RESET na pozici 0 (CAM 10 PODIUM)
        currentPosition = 0;
        previousPosition = -1;
        if (allPositions[0] != null)
        {
            allPositions[0].SetActive(true);
            Debug.Log("[OryxAI] FORCING COMPLETE POSITION RESET - CAM 10 PODIUM ACTIVATED");
        }
        
        // Reset všech mechanik
        isMoving = false;
        // isAttackingDoor REMOVED
        isInOffice = false;
        timeArrivedAtPosition = Time.time;
        SetNextMoveTime();
        
        // Reset všech stavů jako při jumpscaru
        // Reset AppierSound flag pro novou noc
        appierSoundPlayedThisNight = false;
        playerWasOnCameras = false;
        
        // Reset Ventilation Wait System
        isWaitingInVentilation = false;
        isWaitingForCameras = false;
        ventilationWaitTimer = 0f;
        ventilationAccumulatedTime = 0f; // Reset progress memory
        
        // Reset Laser Defense System
        isLaserDefenseBlocking = false;
        laserDefenseTimer = 0f;
        
        Debug.Log($"[OryxAI] Orax reset to CAM 10 PODIUM after death from {killerName}");
    }
    
    /// <summary>
    /// Reset sledování jiných dead panelů
    /// </summary>
    private void ResetOtherCharacterDeathDetection()
    {
        if (!enableOtherCharacterDeathDetection || otherCharacterDeadPanels == null)
            return;
        
        if (otherCharacterDeadPanelStates != null)
        {
            for (int i = 0; i < otherCharacterDeadPanelStates.Length; i++)
            {
                otherCharacterDeadPanelStates[i] = false;
            }
        }
        
        Debug.Log("[OryxAI] Other character death detection states reset");
    }
    
    /// <summary>
    /// Veřejná metoda pro reset při návratu do Main Menu
    /// </summary>
    public void OnReturnToMainMenu()
    {
        Debug.Log("[OryxAI] ===== RETURN TO MAIN MENU =====");
        Debug.Log($"[OryxAI] Orax was at position {currentPosition} before returning to main menu");
        
        // KRITICKÝ RESET - Forčování návratu na CAM 10 PODIUM
        Debug.Log("[OryxAI] FORCING COMPLETE POSITION RESET for main menu return");
        
        // STEJNÁ LOGIKA JAKO PO ORAX JUMPSCARU - manuální nastavení pozic
        Debug.Log("[OryxAI] Using SAME RESET LOGIC as Orax jumpscare");
        
        // VOLEJ STEJNOU METODU JAKO PO ORAX JUMPSCARU
        ResetToStartingPosition();
        
        Debug.Log("[OryxAI] Orax completely reset for main menu return - ready for new game");
    }
    
    /// <summary>
    /// Veřejná metoda pro reset po smrti jiného animatroniků (pro externí volání)
    /// </summary>
    public void OnOtherCharacterKilledPlayer(string killerName = "Unknown")
    {
        Debug.Log($"[OryxAI] External death notification from {killerName}");
        ResetAfterOtherCharacterDeath(killerName);
    }
    

    
    /// <summary>
    /// Komplexní reset všech stavů pro čistý start
    /// </summary>
    private void ResetAllStatesForCleanStart()
    {
        // Reset AppierSound flag
        appierSoundPlayedThisNight = false;
        playerWasOnCameras = false;
        
        // Reset Ventilation Wait System
        isWaitingInVentilation = false;
        isWaitingForCameras = false;
        ventilationWaitTimer = 0f;
        ventilationAccumulatedTime = 0f;
        
        // Reset Laser Defense System
        isLaserDefenseBlocking = false;
        laserDefenseTimer = 0f;
        
        // previousDoorClosed reset REMOVED - door attack mechanics no longer used
        
        // Reset other character death detection
        ResetOtherCharacterDeathDetection();
        
        // Reset AI level tracking
        previousAILevel = this.currentAILevel;
        
        Debug.Log("[OryxAI] All states reset for clean start");
    }
    
    #endregion
}