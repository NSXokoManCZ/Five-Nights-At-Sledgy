using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WardenHourlyAI
{
    [Range(0, 20)] public int hour12AM = 0;
    [Range(0, 20)] public int hour1AM = 0;
    [Range(0, 20)] public int hour2AM = 0;
    [Range(0, 20)] public int hour3AM = 0;
    [Range(0, 20)] public int hour4AM = 0;
    [Range(0, 20)] public int hour5AM = 0;

    public int GetAIForHour(int hour)
    {
        return hour switch
        {
            0 => hour12AM,
            1 => hour1AM,
            2 => hour2AM,
            3 => hour3AM,
            4 => hour4AM,
            5 => hour5AM,
            _ => 0
        };
    }
}

[System.Serializable]
public class WardenNightConfiguration
{
    public string nightName;
    public WardenHourlyAI hourlyAI = new WardenHourlyAI();
}

[System.Serializable]
public class WardenPosition
{
    public GameObject positionCube; // Fyzický Cube GameObject
    public GameObject headObject; // Hlava na této pozici
    public string positionName = "Position"; // Název pozice pro debug
}

/// <summary>
/// AI systém pro animatronic Warden - FNAC3 The Rat style
/// 4 fyzické pozice (Cube objekty) mezi kterými se teleportuje
/// Zastavuje všechny ostatní animatroniky (kromě Thatchera) během útoku
/// </summary>
public class WardenAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("=== WARDEN AI SYSTEM ===")]
    [SerializeField] private bool enableWardenAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Warden Positions (4 útočné pozice)")]
    [SerializeField] private WardenPosition[] wardenPositions = new WardenPosition[4];
    
    [Header("Head Shaking Settings")]
    [SerializeField] private float headShakeSpeed = 20f; // Rychlost třesení hlavy
    [SerializeField] private float headShakeMinAngle = -15f; // Minimální úhel otočení (vlevo)
    [SerializeField] private float headShakeMaxAngle = 15f; // Maximální úhel otočení (vpravo)
    [SerializeField] private bool enableHeadShake = true;
    
    [Header("Flashlight Detection")]
    [SerializeField] private FlashlightSystem flashlightSystem;
    [SerializeField] private Light flashlightSpotlight; // Spot Light - Baterka (Light) pro přímé ovládání
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float detectionGridSize = 80f; // Poloměr detekce kolem kurzoru v pixelech
    [SerializeField] private float lightRange = 100f; // Maximální vzdálenost raycastu
    [SerializeField] private LayerMask raycastLayers = -1;
    [SerializeField] private int raycastGridCount = 3; // 3x3 grid raycastů
    
    [Header("Defense Timing - AI Scaling")]
    [Header("AI 1: 2-4s stay, 8-12 moves")]
    [SerializeField] private float ai1MinStayTime = 2f;
    [SerializeField] private float ai1MaxStayTime = 4f;
    [SerializeField] private int ai1MinMoves = 8;
    [SerializeField] private int ai1MaxMoves = 12;
    
    [Header("AI 10: 1-2s stay, 12-16 moves")]
    [SerializeField] private float ai10MinStayTime = 1f;
    [SerializeField] private float ai10MaxStayTime = 2f;
    [SerializeField] private int ai10MinMoves = 12;
    [SerializeField] private int ai10MaxMoves = 16;
    
    [Header("AI 20: 0.2-0.8s stay, 16-20 moves")]
    [SerializeField] private float ai20MinStayTime = 0.2f;
    [SerializeField] private float ai20MaxStayTime = 0.8f;
    [SerializeField] private int ai20MinMoves = 16;
    [SerializeField] private int ai20MaxMoves = 20;
    
    [Header("Tolerance Settings")]
    [SerializeField] private float electricityTurnOffTolerance = 5f; // Čas na vypnutí energie po spawnu
    [SerializeField] private float firstPositionTolerance = 4f; // Tolerance na první pozici (po vypnutí energie)
    [SerializeField] private float normalTolerance = 0.8f; // Tolerance na dalších pozicích
    [SerializeField] private float fakePositionChance = 0.25f; // 25% šance na fake pozici
    [SerializeField] private float fakePositionDuration = 0.4f; // Jak dlouho trvá fake pozice
    [SerializeField] private float flashlightDisableTime = 0.2f; // Vypnutí baterky při přesunu
    [SerializeField] private float offHeadPenaltyTime = 0.3f; // Čas na návrat baterky na hlavu (sekundy)
    
    [Header("Advanced AI System - Noc a Čas")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private bool enableRealTimeAIUpdates = true;
    [SerializeField] private float aiUpdateInterval = 1f;
    [SerializeField] private WardenNightConfiguration[] aiSettings = new WardenNightConfiguration[6];
    
    [Header("System References")]
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private Svetlo_Strop_Kancelar lightSystem;
    [SerializeField] private ElectricityManagement electricitySystem;
    
    [Header("Other Animatronics References - For Pausing")]
    [SerializeField] private DokkiAI dokkiAI;
    [SerializeField] private ZofeyeAI zofeyeAI;
    [SerializeField] private CaviraAI caviraAI;
    [SerializeField] private OryxAIVerse3 oraxAI;
    [SerializeField] private SledgeAI sledgeAI;
    [SerializeField] private IanidAI ianidAI;
    [SerializeField] private FenerirAI fenerirAI;
    [SerializeField] private AraniAI araniAI;
    [SerializeField] private ThatcherAI thatcherAI;
    
    [Header("Camera System References")]
    [SerializeField] private CameraSystem cameraSystem;
    [SerializeField] private CameryVentSystem ventCameraSystem;
    [SerializeField] private Button camsOffButton;
    [SerializeField] private Button camsOnButton;
    
    [Header("Death System")]
    [SerializeField] private GameObject deadPanel;
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player pro dead panel (child objektu deadPanel)
    [SerializeField] private AudioSource jumpcareAudioSource;
    [SerializeField] private AudioClip jumpcareSound;
    
    [Header("Light System - Vypnutí před jumpscarem")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo kanceláře
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři
    [SerializeField] private float lightOffBeforeJumpscare = 0.9f; // Světlo zhasne 0.9s před jumpscarem
    [SerializeField] private float lightOnAfterDelay = 1f; // Světlo se zapne po 1s
    [SerializeField] private AudioSource lightOffAudioSource; // Audio source pro zvuk vypnutí světla
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí světla
    
    [Header("Audio System")]
    [SerializeField] private AudioSource headShakeAudioSource;
    [SerializeField] private AudioClip headShakeSound;
    [SerializeField] private AudioSource walkOffAudioSource;
    [SerializeField] private AudioClip walkOffSound;
    
    [Header("Fake Position Sound")]
    [SerializeField] private AudioSource fakePositionAudioSource;
    [SerializeField] private AudioClip fakePositionSound;
    
    [Header("Appear Sound")]
    [SerializeField] private AudioSource appearAudioSource;
    [SerializeField] private AudioClip appearSound;
    
    [Header("Menu Detection System")]
    [SerializeField] private GameObject[] menuPanels = new GameObject[0];
    [SerializeField] private bool menuIsActive = false;
    [SerializeField] private bool enableMenuDetection = true;
    
    [Header("Other Animatronics - Death Detection")]
    [SerializeField] private GameObject[] otherDeadPanels;
    [SerializeField] private bool enableDeathDetection = true;
    
    [Header("Game Over System")]
    [SerializeField] private float gameOverDelay = 3f;
    
    [Header("Current AI Status - READ ONLY")]
    [SerializeField] private int currentEffectiveAI = 0;
    [SerializeField] private int currentNightDisplay = 1;
    [SerializeField] private int currentTimeDisplay = 0;
    [SerializeField] private string systemStatusDisplay = "Manual AI";
    
    [Header("Attack Status - READ ONLY")]
    [SerializeField] private int currentPositionIndex = -1;
    [SerializeField] private int movesCompleted = 0;
    [SerializeField] private int movesRemaining = 0;
    [SerializeField] private int totalMovesRequired = 0;
    
    [Header("Timing Status - READ ONLY")]
    [SerializeField] private bool waitingForElectricityOff = false;
    [SerializeField] private float electricityToleranceRemaining = 0f;
    [SerializeField] private float currentStayTime = 0f;
    [SerializeField] private float positionTimerDisplay = 0f;
    [SerializeField] private float currentToleranceTime = 0f;
    [SerializeField] private float toleranceRemaining = 0f;
    [SerializeField] private float defendProgress = 0f;
    [SerializeField] private float defendTimeRequired = 0f;
    
    [Header("Detection Status - READ ONLY")]
    [SerializeField] private bool isFlashlightOnHead = false;
    [SerializeField] private bool isFakePosition = false;
    [SerializeField] private bool isOnHead = false; // Je hráč aktivně na hlavě (pro off-head penalty)
    [SerializeField] private float offHeadTimer = 0f; // Čas od opuštění hlavy
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showDetectionGizmos = true;
    [SerializeField] private bool forceSpawnForTesting = false;
    
    #endregion
    
    #region Private Variables
    
    private bool isWardenActive = false;
    private bool isWardenAttacking = false;
    private bool isFirstPosition = true;
    private bool hasEverSpawned = false; // Ochrana před resetem před prvním spawnem
    
    private bool isWaitingForElectricityOff = true; // Čeká na vypnutí energie
    private float electricityToleranceTimer = 0f; // Timer pro tolerance vypnutí energie
    
    private bool isFakePositionSoundPlayed = false; // Zda byl fake sound již přehrán
    
    private List<int> availablePositions = new List<int>();
    
    private float currentRequiredDefendTime = 0f;
    private bool hasPlayerStartedDefending = false;
    private float positionTimer = 0f;
    private float toleranceTimer = 0f;
    private float defendTimer = 0f;
    
    private float headShakeTime = 0f;
    private float[] originalHeadRotations = new float[4];
    
    private int lastKnownNight = 1;
    private int lastKnownHour = 0;
    private float aiUpdateTimer = 0f;
    
    // Off-head penalty system variables
    private bool wasOnHeadLastFrame = false; // Byl hráč na hlavě v minulém frame?
    private float offHeadPenaltyTimer = 0f; // Timer pro off-head penalty
    private bool isOffHeadPenaltyActive = false; // Je aktivní off-head penalty countdown?
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeWardenSystem();
    }
    
    void Update()
    {
        if (!enableWardenAI)
            return;
            
        if (enableDeathDetection)
            CheckOtherDeathsAndReset();
            
        CheckMenuStatus();
        
        if (useAdvancedAISystem && enableRealTimeAIUpdates)
            HandleAIUpdates();
            
        if (isWardenAttacking)
            HandleWardenAttack();
            
        UpdateInspectorDisplayValues();
    }
    
    void OnDrawGizmos()
    {
        if (!showDetectionGizmos || !isWardenAttacking || currentPositionIndex < 0 || currentPositionIndex >= wardenPositions.Length)
            return;
            
        WardenPosition currentPos = wardenPositions[currentPositionIndex];
        if (currentPos == null || currentPos.headObject == null)
            return;
            
        Gizmos.color = isFlashlightOnHead ? Color.green : Color.red;
        Gizmos.DrawWireSphere(currentPos.headObject.transform.position, detectionGridSize / 100f); // Vizualizace (grid v pixelech)
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeWardenSystem()
    {
        if (aiSettings == null || aiSettings.Length != 6)
        {
            aiSettings = new WardenNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                aiSettings[i] = new WardenNightConfiguration
                {
                    nightName = $"Night {i + 1}",
                    hourlyAI = new WardenHourlyAI()
                };
            }
        }
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
            
        InitializeAudioSystem();
        FindSystemReferences();
        RegisterCamsOffButtonListener();
        InitializePositions();
        HideAllPositions();
        
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Warden system initialized");
    }
    
    private void InitializePositions()
    {
        for (int i = 0; i < wardenPositions.Length; i++)
        {
            if (wardenPositions[i] != null && wardenPositions[i].headObject != null)
            {
                originalHeadRotations[i] = wardenPositions[i].headObject.transform.localEulerAngles.y;
                
                if (enableDebugLogs)
                    Debug.Log($"[WardenAI] Position {i + 1} ({wardenPositions[i].positionName}) - Head Y: {originalHeadRotations[i]}");
            }
        }
    }
    
    private void HideAllPositions()
    {
        for (int i = 0; i < wardenPositions.Length; i++)
        {
            if (wardenPositions[i] != null && wardenPositions[i].positionCube != null)
            {
                wardenPositions[i].positionCube.SetActive(false);
            }
        }
    }
    
    private void ShowPosition(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= 4) // Pouze útočné pozice 0-3
            return;
            
        HideAllPositions();
        
        if (wardenPositions[positionIndex] != null && wardenPositions[positionIndex].positionCube != null)
        {
            wardenPositions[positionIndex].positionCube.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log($"[WardenAI] Showing attack position {positionIndex} (0-3): {wardenPositions[positionIndex].positionName}");
        }
    }
    
    private void InitializeAudioSystem()
    {
        if (jumpcareAudioSource == null)
            jumpcareAudioSource = gameObject.AddComponent<AudioSource>();
            
        if (jumpcareAudioSource != null)
        {
            jumpcareAudioSource.playOnAwake = false;
            jumpcareAudioSource.loop = false;
        }
        
        if (headShakeAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 2)
                headShakeAudioSource = existingSources[1];
            else
                headShakeAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (headShakeAudioSource != null)
        {
            headShakeAudioSource.playOnAwake = false;
            headShakeAudioSource.loop = true;
            headShakeAudioSource.volume = 1.4f; // 40% hlasitější
        }
        
        if (walkOffAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 3)
                walkOffAudioSource = existingSources[2];
            else
                walkOffAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (walkOffAudioSource != null)
        {
            walkOffAudioSource.playOnAwake = false;
            walkOffAudioSource.loop = false;
        }
        
        // Fake Position Audio
        if (fakePositionAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 4)
                fakePositionAudioSource = existingSources[3];
            else
                fakePositionAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (fakePositionAudioSource != null)
        {
            fakePositionAudioSource.playOnAwake = false;
            fakePositionAudioSource.loop = false;
        }
        
        // Appear Audio
        if (appearAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 5)
                appearAudioSource = existingSources[4];
            else
                appearAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (appearAudioSource != null)
        {
            appearAudioSource.playOnAwake = false;
            appearAudioSource.loop = false;
        }
    }
    
    private void FindSystemReferences()
    {
        if (shiftTimer == null)
            shiftTimer = FindFirstObjectByType<ShiftTimer>();
            
        if (nightManager == null)
            nightManager = FindFirstObjectByType<NightShiftManagement>();
            
        if (flashlightSystem == null)
            flashlightSystem = FindFirstObjectByType<FlashlightSystem>();
            
        if (lightSystem == null)
            lightSystem = FindFirstObjectByType<Svetlo_Strop_Kancelar>();
            
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Pokus o automatické nalezení flashlight spotlight
        if (flashlightSpotlight == null && flashlightSystem != null)
        {
            Light[] lights = flashlightSystem.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Spot)
                {
                    flashlightSpotlight = light;
                    if (enableDebugLogs)
                        Debug.Log($"[WardenAI] Auto-found flashlight spotlight: {light.name}");
                    break;
                }
            }
        }
    }
    
    private void RegisterCamsOffButtonListener()
    {
        if (camsOffButton != null)
        {
            camsOffButton.onClick.AddListener(OnCamsOffButtonClicked);
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] ✅ CamsOff button registered");
        }
        else
        {
            Debug.LogError("[WardenAI] ❌ CamsOff button is NULL!");
        }
    }
    
    private void OnCamsOffButtonClicked()
    {
        // DEV MODE: Přeskoč Fenerir blocking
        bool devModeActive = (nightManager != null && nightManager.IsDevModeForceSpawnBothActive());
        
        // BLOCKING: Fenerir is angry (You Failed panel active) - kromě DEV mode
        if (!devModeActive && fenerirAI != null && fenerirAI.IsFenerirAngry)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] ❌ Fenerir is angry (You Failed) - Warden spawn blocked");
            return;
        }
        
        if (!isWardenActive && !isWardenAttacking)
        {
            AttemptWardenSpawn();
        }
    }
    
    #endregion
    
    #region Spawn System
    
    private void AttemptWardenSpawn()
    {
        // DEV MODE: Force spawn Warden (přeskočí všechny kontroly)
        if (nightManager != null && nightManager.IsDevModeForceSpawnBothActive())
        {
            Debug.Log("[WardenAI] 🔧 DEV MODE: FORCE SPAWNING WARDEN");
            SpawnWarden();
            return;
        }
        
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI <= 0)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] ❌ AI is 0 - no spawn");
            return;
        }
        
        if (IsOraxInOffice())
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] ❌ Orax in office - blocked");
            return;
        }
        
        // BLOCKING: Zofeye, Cavira nebo Ianid za dveřmi
        if (IsDoorAnimatronicAtDoor())
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] ❌ Door animatronic at door - blocked");
            return;
        }
        
        // BLOCKING: Sledge na CAM 04 (generator)
        if (IsSledgeAtGenerator())
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] ❌ Sledge at generator (CAM 04) - blocked");
            return;
        }
        
        float spawnChance = CalculateExponentialSpawnChance(effectiveAI);
        float randomRoll = forceSpawnForTesting ? 0f : Random.value;
        
        if (enableDebugLogs)
            Debug.Log($"[WardenAI] Roll: {randomRoll:F4} vs {spawnChance:F4} (AI {effectiveAI})");
        
        if (randomRoll <= spawnChance)
        {
            SpawnWarden();
        }
    }
    
    private float CalculateExponentialSpawnChance(int aiLevel)
    {
        if (aiLevel <= 0)
            return 0f;
            
        float baseChance = 0.1f;
        float maxChance = 0.32f; // Sníženo o 5% (bylo 0.4)
        float normalizedAI = Mathf.Clamp01((aiLevel - 1f) / 19f);
        
        return Mathf.Lerp(baseChance, maxChance, Mathf.Pow(normalizedAI, 0.7f));
    }
    
    private bool IsOraxInOffice()
    {
        if (oraxAI != null)
            return oraxAI.GetCurrentPosition() == 17;
            
        return false;
    }
    
    private bool IsDoorAnimatronicAtDoor()
    {
        // Zofeye - check if at door position (GetCurrentPosition specifické pro každého)
        if (zofeyeAI != null)
        {
            int zofeyePos = zofeyeAI.CurrentPosition;
            if (zofeyePos == 6) // Pozice dveří pro Zofeye
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Zofeye at door (pos 6)");
                return true;
            }
        }
        
        // Cavira - check if at door position
        if (caviraAI != null)
        {
            int caviraPos = caviraAI.CurrentPosition;
            if (caviraPos == 7) // Pozice dveří pro Cavira
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Cavira at door (pos 7)");
                return true;
            }
        }
        
        // Ianid - check if at door position
        if (ianidAI != null)
        {
            int ianidPos = ianidAI.GetCurrentPosition();
            if (ianidPos == 6) // Pozice dveří pro Ianid
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Ianid at door (pos 6)");
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsSledgeAtGenerator()
    {
        if (sledgeAI != null)
        {
            int sledgePos = sledgeAI.CurrentPosition;
            if (sledgePos == 4) // CAM 04 - generator pozice
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Sledge at generator (CAM 04)");
                return true;
            }
        }
        
        return false;
    }
    
    private void SpawnWarden()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== WARDEN SPAWNED =====");
        
        // Přehraj appear sound
        PlayAppearSound();
        
        hasEverSpawned = true; // První spawn proběhl
        isWardenActive = true;
        isWardenAttacking = true;
        isFirstPosition = true;
        
        // ⚡ OKAMŽITÁ KONTROLA: Pokud se Warden spawnuje zatímco Thatcher je aktivní, OKAMŽITĚ zablokuj Thatchera
        if (thatcherAI != null && thatcherAI.IsThatcherActive())
        {
            thatcherAI.SetBothActiveState(true, true); // areBothActive = true, isThatcherBlocked = true
            Debug.Log("[WardenAI] 🔴🔴⚡ INSTANT BLOCK - Thatcher already active, BLOCKED immediately from Warden spawn!");
        }
        
        // FÁZE 1: Čekání na vypnutí energie (5s tolerance)
        isWaitingForElectricityOff = true;
        electricityToleranceTimer = 0f;
        
        CalculateMovesRequired();
        PauseAllAnimatronics();
        
        // Registruj CamsOn button listener - klik zabíje hráče s 0.3s delayem
        if (camsOnButton != null)
        {
            camsOnButton.onClick.AddListener(OnCamsOnClickedDuringWarden);
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] CamsOn button listener registered - clicking will cause death with 0.3s delay");
        }
        
        // Rovnoměrné rozdělení 25% pro každou ze 4 útočných pozic (0-3)
        int startPosition = Random.Range(0, 4);
        TeleportToPosition(startPosition, false);
        
        currentToleranceTime = firstPositionTolerance;
        toleranceTimer = 0f;
        positionTimer = 0f;
        
        hasPlayerStartedDefending = false;
        defendTimer = 0f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[WardenAI] ==========================================");
            Debug.Log($"[WardenAI] SPAWN INFO:");
            Debug.Log($"[WardenAI] - Starting position: {currentPositionIndex} ({wardenPositions[currentPositionIndex].positionName})");
            Debug.Log($"[WardenAI] - Total moves required: {totalMovesRequired}");
            Debug.Log($"[WardenAI] - AI Level: {GetEffectiveAILevel()}");
            Debug.Log($"[WardenAI] ==========================================");
            Debug.Log($"[WardenAI] PHASE 1: Waiting for electricity OFF ({electricityTurnOffTolerance}s tolerance)");
        }
    }
    
    private void CalculateMovesRequired()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI <= 0)
        {
            totalMovesRequired = 0;
            return;
        }
        
        int minMoves, maxMoves;
        
        if (effectiveAI <= 10)
        {
            float t = (effectiveAI - 1f) / 9f;
            minMoves = Mathf.RoundToInt(Mathf.Lerp(ai1MinMoves, ai10MinMoves, t));
            maxMoves = Mathf.RoundToInt(Mathf.Lerp(ai1MaxMoves, ai10MaxMoves, t));
        }
        else
        {
            float t = (effectiveAI - 10f) / 10f;
            minMoves = Mathf.RoundToInt(Mathf.Lerp(ai10MinMoves, ai20MinMoves, t));
            maxMoves = Mathf.RoundToInt(Mathf.Lerp(ai10MaxMoves, ai20MaxMoves, t));
        }
        
        totalMovesRequired = Random.Range(minMoves, maxMoves + 1);
        movesCompleted = 0;
        movesRemaining = totalMovesRequired;
    }
    
    private void TeleportToPosition(int positionIndex, bool allowFake)
    {
        // VALIDATION: Pouze útočné pozice 0-3, NIKDY ne pozice 4 (start/čekací)
        if (positionIndex < 0 || positionIndex >= 4)
        {
            if (enableDebugLogs)
                Debug.LogError($"[WardenAI] ⚠️ INVALID TELEPORT: Attempted to teleport to position {positionIndex}! Only 0-3 allowed!");
            
            // Fallback - vygeneruj náhodnou platnou pozici
            positionIndex = Random.Range(0, 4);
            
            if (enableDebugLogs)
                Debug.LogError($"[WardenAI] ⚠️ Forcing fallback to position {positionIndex}");
        }
        
        currentPositionIndex = positionIndex;
        
        ShowPosition(positionIndex);
        CalculateStayTime();
        
        if (allowFake && !isFirstPosition)
        {
            isFakePosition = Random.value < fakePositionChance;
            
            if (isFakePosition)
            {
                currentStayTime = fakePositionDuration;
                
                if (enableDebugLogs)
                    Debug.Log($"[WardenAI] 🎭 FAKE position {positionIndex + 1} - {fakePositionDuration}s");
            }
        }
        else
        {
            isFakePosition = false;
        }
        
        // Reset fake sound flag pro novou pozici
        isFakePositionSoundPlayed = false;
        
        positionTimer = 0f;
        
        if (enableDebugLogs && !isFakePosition)
            Debug.Log($"[WardenAI] Position {positionIndex + 1} - {currentStayTime:F2}s");
    }
    
    private void CalculateStayTime()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI <= 0)
        {
            currentStayTime = 0f;
            return;
        }
        
        float minTime, maxTime;
        
        if (effectiveAI <= 10)
        {
            float t = (effectiveAI - 1f) / 9f;
            minTime = Mathf.Lerp(ai1MinStayTime, ai10MinStayTime, t);
            maxTime = Mathf.Lerp(ai1MaxStayTime, ai10MaxStayTime, t);
        }
        else
        {
            float t = (effectiveAI - 10f) / 10f;
            minTime = Mathf.Lerp(ai10MinStayTime, ai20MinStayTime, t);
            maxTime = Mathf.Lerp(ai20MaxStayTime, ai20MaxStayTime, t);
        }
        
        currentStayTime = Random.Range(minTime, maxTime);
        currentRequiredDefendTime = currentStayTime;
    }
    
    #endregion
    
    #region Attack Handler
    
    private void HandleWardenAttack()
    {
        // SAFETY CHECK: Zkontroluj, zda je aktuální pozice viditelná
        if (currentPositionIndex >= 0 && currentPositionIndex < 4)
        {
            WardenPosition currentPos = wardenPositions[currentPositionIndex];
            if (currentPos != null && currentPos.positionCube != null)
            {
                if (!currentPos.positionCube.activeInHierarchy)
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogError($"[WardenAI] ⚠️ BUG: Position {currentPositionIndex} ({currentPos.positionName}) is NOT VISIBLE!");
                        Debug.LogError($"[WardenAI] Forcing position re-activation...");
                    }
                    
                    // Núzová re-aktivace pozice
                    ShowPosition(currentPositionIndex);
                }
            }
        }
        
        // POZNÁMKA: Camera death detekce přesunuta na CamsOn button listener s 0.3s delayem
        
        // FÁZE 1: Čekání na vypnutí energie (5s tolerance)
        if (isWaitingForElectricityOff)
        {
            if (electricitySystem != null && !electricitySystem.IsPowerOn())
            {
                // Hráč vypnul energii včas!
                isWaitingForElectricityOff = false;
                electricityToleranceTimer = 0f;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[WardenAI] ==========================================");
                    Debug.Log($"[WardenAI] ✅ ELECTRICITY TURNED OFF!");
                    Debug.Log($"[WardenAI] PHASE 2: Defense phase started");
                    Debug.Log($"[WardenAI] - First position tolerance: {firstPositionTolerance}s");
                    Debug.Log($"[WardenAI] - Required defend time: {currentRequiredDefendTime:F2}s");
                    Debug.Log($"[WardenAI] - Stay time on position: {currentStayTime:F2}s");
                    Debug.Log($"[WardenAI] ==========================================");
                }
            }
            else
            {
                // Stále čeká na vypnutí energie
                electricityToleranceTimer += Time.deltaTime;
                
                if (electricityToleranceTimer >= electricityTurnOffTolerance)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[WardenAI] Failed to turn OFF electricity in {electricityTurnOffTolerance}s - DEATH!");
                    TriggerWardenJumpscare();
                    return;
                }
                
                // Nekontroluj nic dalšího, čekáme na vypnutí energie
                return;
            }
        }
        
        // FÁZE 2: Normální obrana - elektřina MUSÍ zůstat vypnutá
        if (electricitySystem != null && electricitySystem.IsPowerOn())
        {
            // OKAMŽitě vypni elektřinu zpátky (aby se nezaplo UI/monitory)
            electricitySystem.OnLightToggled(false);
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Turned ON electricity during defense - forcing OFF and triggering DEATH!");
            
            TriggerWardenJumpscare();
            return;
        }
        
        positionTimer += Time.deltaTime;
        toleranceTimer += Time.deltaTime;
        
        bool flashlightOnHead = IsFlashlightPointingAtCurrentHead();
        isFlashlightOnHead = flashlightOnHead;
        
        // OFF-HEAD PENALTY SYSTEM
        HandleOffHeadPenalty(flashlightOnHead);
        
        // Detekce fake pozice - přehraj sound pokud svítí na fake hlavu
        if (flashlightOnHead && isFakePosition && !isFakePositionSoundPlayed)
        {
            PlayFakePositionSound();
            isFakePositionSoundPlayed = true;
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] 🎭 Player detected FAKE position!");
        }
        
        if (enableHeadShake && currentPositionIndex >= 0 && currentPositionIndex < wardenPositions.Length)
            HandleHeadShake(currentPositionIndex, flashlightOnHead);
        
        if (flashlightOnHead && !isFakePosition)
        {
            if (!hasPlayerStartedDefending)
            {
                hasPlayerStartedDefending = true;
                
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Player started defending!");
            }
            
            defendTimer += Time.deltaTime;
            toleranceTimer = 0f;
            
            if (defendTimer >= currentRequiredDefendTime)
            {
                OnPositionDefended();
            }
        }
        else if (hasPlayerStartedDefending)
        {
            if (toleranceTimer > currentToleranceTime)
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Tolerance expired - DEATH!");
                TriggerWardenJumpscare();
            }
        }
        else
        {
            if (toleranceTimer > currentToleranceTime)
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Failed to defend in time - DEATH!");
                TriggerWardenJumpscare();
            }
        }
        
        if (isFakePosition && positionTimer >= fakePositionDuration)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Fake position expired - moving");
            MoveToNextPosition();
        }
    }
    
    private bool IsFlashlightPointingAtCurrentHead()
    {
        if (currentPositionIndex < 0 || currentPositionIndex >= wardenPositions.Length)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Invalid position index");
            return false;
        }
            
        WardenPosition currentPos = wardenPositions[currentPositionIndex];
        if (currentPos == null || currentPos.headObject == null)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Head object is null");
            return false;
        }
            
        if (playerCamera == null)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Player camera is null");
            return false;
        }
            
        if (flashlightSystem == null)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Flashlight system is null");
            return false;
        }
            
        if (!flashlightSystem.IsFlashlightActive())
        {
            return false; // Baterka není zapnutá - tichý return
        }
        
        Vector3 mouseScreenPos = Input.mousePosition;
        bool hitHead = false;
        int totalHits = 0;
        int totalRays = raycastGridCount * raycastGridCount;
        
        // Pixel-based offset pro grid (stejně jako Thatcher)
        float offsetStep = detectionGridSize / 2f; // Např. 80 / 2 = 40 pixelů
        
        // 3x3 grid raycastů kolem pozice myši
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Vypočítej pozici s offsetem v pixelech
                Vector3 offsetScreenPos = mouseScreenPos + new Vector3(x * offsetStep, y * offsetStep, 0);
                
                // Zkontroluj, že pozice je stále na obrazovce
                if (offsetScreenPos.x < 0 || offsetScreenPos.x > Screen.width || 
                    offsetScreenPos.y < 0 || offsetScreenPos.y > Screen.height)
                    continue;
                
                // Vytvoř ray z offset pozice
                Ray ray = playerCamera.ScreenPointToRay(offsetScreenPos);
                
                // Proveď raycast
                RaycastHit[] hits = Physics.RaycastAll(ray, lightRange, raycastLayers);
                
                // Projdi všechny zasažené objekty
                foreach (RaycastHit hit in hits)
                {
                    // Zkontroluj, zda raycast zasáhl hlavu nebo její potomky
                    Transform hitTransform = hit.transform;
                    while (hitTransform != null)
                    {
                        if (hitTransform.gameObject == currentPos.headObject)
                        {
                            hitHead = true;
                            totalHits++;
                            
                            if (enableDebugLogs && x == 0 && y == 0) // Log pouze pro střední ray
                                Debug.Log($"[WardenAI] Flashlight hit head at [{x},{y}] through: {hit.transform.name}");
                            
                            break;
                        }
                        hitTransform = hitTransform.parent;
                    }
                    
                    if (hitHead) break;
                }
                
                if (hitHead) break;
            }
            
            if (hitHead) break;
        }
        
        // Detekce úspěšná pokud alespoň 1 ray trefil hlavu
        if (hitHead && enableDebugLogs)
        {
            Debug.Log($"[WardenAI] ✅ Flashlight detected on head! Hits: {totalHits}/{totalRays}");
        }
        
        return hitHead;
    }
    
    private void HandleHeadShake(int positionIndex, bool shouldShake)
    {
        if (positionIndex < 0 || positionIndex >= wardenPositions.Length)
            return;
            
        WardenPosition pos = wardenPositions[positionIndex];
        if (pos == null || pos.headObject == null)
            return;
            
        if (shouldShake)
        {
            headShakeTime += Time.deltaTime * headShakeSpeed;
            
            // Použij nastavitelný rozsah úhlů (min/max)
            float normalizedShake = Mathf.Sin(headShakeTime); // -1 až 1
            float shakeOffset = Mathf.Lerp(headShakeMinAngle, headShakeMaxAngle, (normalizedShake + 1f) / 2f);
            
            Vector3 currentRotation = pos.headObject.transform.localEulerAngles;
            currentRotation.y = originalHeadRotations[positionIndex] + shakeOffset;
            pos.headObject.transform.localEulerAngles = currentRotation;
            
            // Audio playback s debug logem
            if (headShakeAudioSource != null && headShakeSound != null)
            {
                if (!headShakeAudioSource.isPlaying)
                {
                    headShakeAudioSource.clip = headShakeSound;
                    headShakeAudioSource.loop = true; // Ujisti se, že je loop zapnutý
                    headShakeAudioSource.Play();
                    
                    if (enableDebugLogs)
                        Debug.Log($"[WardenAI] 🔊 Head shake audio started: {headShakeSound.name}");
                }
            }
            else if (enableDebugLogs)
            {
                if (headShakeAudioSource == null)
                    Debug.LogWarning("[WardenAI] ⚠️ headShakeAudioSource is NULL!");
                if (headShakeSound == null)
                    Debug.LogWarning("[WardenAI] ⚠️ headShakeSound AudioClip is NULL!");
            }
        }
        else
        {
            Vector3 currentRotation = pos.headObject.transform.localEulerAngles;
            currentRotation.y = Mathf.LerpAngle(currentRotation.y, originalHeadRotations[positionIndex], Time.deltaTime * 10f);
            pos.headObject.transform.localEulerAngles = currentRotation;
            
            if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            {
                headShakeAudioSource.Stop();
                
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] 🔇 Head shake audio stopped");
            }
        }
    }
    
    private void OnPositionDefended()
    {
        movesCompleted++;
        movesRemaining = totalMovesRequired - movesCompleted;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[WardenAI] ==========================================");
            Debug.Log($"[WardenAI] ✅ POSITION DEFENDED!");
            Debug.Log($"[WardenAI] - Progress: {movesCompleted}/{totalMovesRequired} moves");
            Debug.Log($"[WardenAI] - Defended in: {defendTimer:F2}s (required: {currentRequiredDefendTime:F2}s)");
            Debug.Log($"[WardenAI] ==========================================");
        }
        
        if (movesCompleted >= totalMovesRequired)
        {
            OnPlayerSucceeded();
        }
        else
        {
            MoveToNextPosition();
        }
    }
    
    private void MoveToNextPosition()
    {
        // Reset off-head penalty při přesunu na novou pozici
        ResetOffHeadPenalty();
        
        StartCoroutine(TemporaryFlashlightDisable());
        
        availablePositions.Clear();
        // Pouze útočné pozice 0-3 (ne domovská 4)
        for (int i = 0; i < 4; i++)
        {
            if (i != currentPositionIndex)
                availablePositions.Add(i);
        }
        
        // SAFETY CHECK: Zkontroluj, zda máme kam se přesunout
        if (availablePositions.Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogError("[WardenAI] ⚠️ BUG: No available positions! Forcing random position...");
            
            // Fallback - vygeneruj náhodnou pozici 0-3 různou od aktuální
            int fallbackPosition;
            do {
                fallbackPosition = Random.Range(0, 4);
            } while (fallbackPosition == currentPositionIndex && currentPositionIndex >= 0 && currentPositionIndex < 4);
            
            // Extra safety: Ujisti se, že je 0-3
            fallbackPosition = Mathf.Clamp(fallbackPosition, 0, 3);
            
            availablePositions.Add(fallbackPosition);
        }
        
        int previousPosition = currentPositionIndex;
        int nextPosition = availablePositions[Random.Range(0, availablePositions.Count)];
        
        // Double-check next position is valid (0-3)
        if (nextPosition < 0 || nextPosition >= 4)
        {
            if (enableDebugLogs)
                Debug.LogError($"[WardenAI] ⚠️ CRITICAL BUG: nextPosition = {nextPosition} (outside 0-3 range)!");
            
            nextPosition = Mathf.Clamp(nextPosition, 0, 3);
        }
        
        isFirstPosition = false;
        
        TeleportToPosition(nextPosition, true);
        
        hasPlayerStartedDefending = false;
        defendTimer = 0f;
        
        toleranceTimer = 0f;
        currentToleranceTime = normalTolerance;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[WardenAI] ==========================================");
            Debug.Log($"[WardenAI] 🔄 MOVED TO NEXT POSITION");
            Debug.Log($"[WardenAI] - From: Position {previousPosition} ({wardenPositions[previousPosition].positionName})");
            Debug.Log($"[WardenAI] - To: Position {currentPositionIndex} ({wardenPositions[currentPositionIndex].positionName})");
            Debug.Log($"[WardenAI] - Is fake: {isFakePosition}");
            Debug.Log($"[WardenAI] - Stay time: {currentStayTime:F2}s");
            Debug.Log($"[WardenAI] - Defend time required: {currentRequiredDefendTime:F2}s");
            Debug.Log($"[WardenAI] - Tolerance: {normalTolerance}s");
            Debug.Log($"[WardenAI] - Flashlight disabled for: {flashlightDisableTime}s");
            Debug.Log($"[WardenAI] ==========================================");
        }
    }
    
    private IEnumerator TemporaryFlashlightDisable()
    {
        if (flashlightSystem != null)
        {
            flashlightSystem.ForceEnableFlashlightComponents();
            
            // Přímé vypnutí světla baterky
            if (flashlightSpotlight != null)
            {
                flashlightSpotlight.enabled = false;
                
                if (enableDebugLogs)
                    Debug.Log($"[WardenAI] Flashlight spotlight disabled for {flashlightDisableTime}s");
            }
            
            yield return new WaitForSeconds(flashlightDisableTime);
            
            // Přímé zapnutí světla baterky
            if (flashlightSpotlight != null)
            {
                flashlightSpotlight.enabled = true;
                
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Flashlight spotlight re-enabled");
            }
            
            flashlightSystem.RestoreFlashlightManually();
        }
    }
    
    #endregion
    
    #region Success & Failure
    
    private void OnPlayerSucceeded()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== PLAYER SUCCEEDED - WARDEN DEFEATED =====");
        
        // Reset off-head penalty při vítězství
        ResetOffHeadPenalty();
        
        isWardenAttacking = false;
        
        if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            headShakeAudioSource.Stop();
            
        PlayWalkOffSound();
        
        StartCoroutine(TemporaryFlashlightDisable());
        
        // Skryj všechny pozice
        HideAllPositions();
        
        // ODBLOKUJ THATCHERA - hráč úspěšně porazil Wardena!
        if (thatcherAI != null)
        {
            thatcherAI.ForceUnblockThatcher();
            Debug.Log("[WardenAI] 🔓 Thatcher UNBLOCKED - hráč úspěšně porazil Wardena!");
        }
        
        // Pokud je Thatcher aktivní, spusť jeho zpožděný odchod
        if (thatcherAI != null && thatcherAI.IsThatcherActive())
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] 🔔 Triggering delayed Thatcher retreat after Warden defeat");
            
            thatcherAI.TriggerDelayedRetreatAfterWarden();
        }

        UnpauseAllAnimatronics();
        
        isWardenActive = false;
        currentPositionIndex = -1;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[WardenAI] ==========================================");
            Debug.Log($"[WardenAI] Warden defeated! All positions hidden.");
            Debug.Log($"[WardenAI] Waiting for next CamsOff spawn attempt");
            Debug.Log($"[WardenAI] ==========================================");
        }
    }
    
    private void TriggerWardenJumpscare()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== JUMPSCARE (normal) =====");
        
        // Reset off-head penalty při jumpscaru
        ResetOffHeadPenalty();
        
        isWardenAttacking = false;
        
        if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            headShakeAudioSource.Stop();
        
        // Skryj všechny pozice
        HideAllPositions();
        
        UnpauseAllAnimatronics();
        
        // Spusť coroutinu s vypnutím světla před jumpscarem
        StartCoroutine(TriggerJumpscareWithLights());
    }
    
    private void TriggerWardenCameraJumpscare()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== CAMERA JUMPSCARE (no light flicker) =====");
        
        // Reset off-head penalty při jumpscaru
        ResetOffHeadPenalty();
        
        isWardenAttacking = false;
        
        if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            headShakeAudioSource.Stop();
        
        // Skryj všechny pozice
        HideAllPositions();
        
        UnpauseAllAnimatronics();
        
        // Spusť coroutinu BEZ light efektu - pouze video loading
        StartCoroutine(TriggerCameraJumpscareWithoutLights());
    }
    
    private IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        
        // Zastav video před skrytím panelu
        StopDeadPanelVideo();
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
            
        if (nightManager != null)
            nightManager.ForceReturnToMenu();
    }
    
    /// <summary>
    /// Coroutina pro jumpcare s vypnutím světla před zobrazením
    /// </summary>
    private System.Collections.IEnumerator TriggerJumpscareWithLights()
    {
        // ZASTAVENÍ THATCHER ATTACK TIMERŮ - Warden zabíjí první!
        if (thatcherAI != null)
        {
            thatcherAI.StopAllThatcherAttackTimers();
            thatcherAI.HideThatcher();
            Debug.Log("[WardenAI] ⏹️ Thatcher attack timers STOPPED + Thatcher HIDDEN - Warden kills first!");
        }
        
        // Vypni světla 0.7s před jumpscarem (s přehráním zvuku)
        yield return StartCoroutine(FlickerLights(false));
        
        // Zobraz dead panel s video delayem
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // Přehraj jumpcare zvuk
        PlayJumpcareSound();
        
        // Počkej 1 sekundu a zapni světla zpátky
        yield return new WaitForSeconds(lightOnAfterDelay);
        yield return StartCoroutine(FlickerLights(true));
        
        // Spusť game over sekvenci
        StartCoroutine(HandleGameOver());
    }
    
    /// <summary>
    /// Coroutina pro camera jumpscare BEZ vypnutí světla
    /// </summary>
    private System.Collections.IEnumerator TriggerCameraJumpscareWithoutLights()
    {
        // ZASTAVENÍ THATCHER ATTACK TIMERŮ - Warden zabíjí první!
        if (thatcherAI != null)
        {
            thatcherAI.StopAllThatcherAttackTimers();
            thatcherAI.HideThatcher();
            Debug.Log("[WardenAI] ⏹️ Thatcher attack timers STOPPED + Thatcher HIDDEN - Warden kills first!");
        }
        
        // OKAMŽITĚ zastav všechny Warden mechaniky
        isWardenAttacking = false;
        isOffHeadPenaltyActive = false;
        offHeadPenaltyTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Camera death (no lights) - Warden mechaniky zastavené");
        
        // ŽÁDNÉ light flicker - světla zůstávají zapnutá!
        
        // Zobraz dead panel s video delayem (0.1s)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // Přehraj jumpcare zvuk
        PlayJumpcareSound();
        
        // Počkej na zobrazení dead panelu (0.1s)
        yield return new WaitForSeconds(0.1f);
        
        // Spusť game over sekvenci (světla zůstávají zapnutá po celou dobu)
        StartCoroutine(HandleGameOver());
    }
    
    /// <summary>
    /// Coroutina pro camera jumpscare se světly - vypne OKAMŽITĚ, zapne po 1s
    /// </summary>
    private System.Collections.IEnumerator TriggerCameraJumpscareWithInstantLights()
    {
        // ZASTAVENÍ THATCHER ATTACK TIMERŮ - Warden zabíjí první!
        if (thatcherAI != null)
        {
            thatcherAI.StopAllThatcherAttackTimers();
            thatcherAI.HideThatcher();
            Debug.Log("[WardenAI] ⏹️ Thatcher attack timers STOPPED + Thatcher HIDDEN - Warden kills first!");
        }
        
        // OKAMŽITĚ zastav všechny Warden mechaniky - aby světla neovlivňovala off-head penalty!
        isWardenAttacking = false;
        isOffHeadPenaltyActive = false;
        offHeadPenaltyTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Camera death - Warden mechaniky zastavené, světla nebudou ovlivňovat gameplay");
        
        // OKAMŽITĚ vypni světlo se zvukem
        yield return StartCoroutine(FlickerLightsInstant(false));
        
        // Počkej 0.9s před zobrazením dead panel
        yield return new WaitForSeconds(lightOffBeforeJumpscare);
        
        // Zobraz dead panel s video delayem (0.1s)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // Přehraj jumpcare zvuk
        PlayJumpcareSound();
        
        // Počkej 1 sekundu a zapni světla zpátky
        yield return new WaitForSeconds(lightOnAfterDelay);
        yield return StartCoroutine(FlickerLightsInstant(true));
        
        // Spusť game over sekvenci
        StartCoroutine(HandleGameOver());
    }
    
    /// <summary>
    /// Coroutina pro zobrazení dead panelu s delayem na načtení videa
    /// </summary>
    private System.Collections.IEnumerator ShowDeadPanelWithVideoDelay()
    {
        // Připrav video (aktivuje VideoPlayer, zavolá Prepare(), nastaví frame 0)
        PrepareDeadPanelVideo();
        
        // Počkej 0.1 sekundy - dá Unity čas načíst první frame videa
        yield return new WaitForSeconds(0.2f);
        
        // Teď zobraz dead panel - video je už připravené
        if (deadPanel != null)
        {
            deadPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Dead panel shown (po 0.1s video loading delay)");
            
            // Spusť video (už je načtené)
            StartDeadPanelVideo();
        }
        else
        {
            Debug.LogError("[WardenAI] Dead panel reference is null! Assign it in inspector.");
        }
    }
    
    /// <summary>
    /// Zapíná/vypíná světla v kanceláři
    /// </summary>
    private System.Collections.IEnumerator FlickerLights(bool turnOn)
    {
        // Při vypínání přehraj zvuk
        if (!turnOn && lightOffSound != null && lightOffAudioSource != null)
        {
            lightOffAudioSource.PlayOneShot(lightOffSound);
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Playing light-off sound");
        }
        
        // Vypni/zapni hlavní světlo
        if (mainLight != null)
        {
            mainLight.SetActive(turnOn);
        }
        
        // Vypni/zapni parent světel (všechna ostatní světla)
        if (parentLight != null)
        {
            parentLight.SetActive(turnOn);
            
            if (enableDebugLogs)
                Debug.Log($"[WardenAI] Office lights: {(turnOn ? "ON" : "OFF")}");
        }
        
        // OPRAVA: Po zapnutí lights znovu enabled flashlight pokud byl active
        if (turnOn && flashlightSystem != null && flashlightSpotlight != null)
        {
            // Pokud je flashlight system active, zajisti že spotlight je enabled
            if (flashlightSystem.IsFlashlightActive())
            {
                flashlightSpotlight.enabled = true;
                
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Re-enabled flashlight spotlight after lights ON");
            }
        }
        
        // Čekej podle timingu (0.7s při vypnutí, 0s při zapnutí)
        yield return new WaitForSeconds(turnOn ? 0f : lightOffBeforeJumpscare);
    }
    
    /// <summary>
    /// Zapíná/vypíná světla OKAMŽITĚ bez delay (pro camera death)
    /// </summary>
    private System.Collections.IEnumerator FlickerLightsInstant(bool turnOn)
    {
        // Při vypínání přehraj zvuk
        if (!turnOn && lightOffSound != null && lightOffAudioSource != null)
        {
            lightOffAudioSource.PlayOneShot(lightOffSound);
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Playing light-off sound (instant)");
        }
        
        // Vypni/zapni hlavní světlo
        if (mainLight != null)
        {
            mainLight.SetActive(turnOn);
        }
        
        // Vypni/zapni parent světel (všechna ostatní světla)
        if (parentLight != null)
        {
            parentLight.SetActive(turnOn);
            
            if (enableDebugLogs)
                Debug.Log($"[WardenAI] Office lights INSTANT: {(turnOn ? "ON" : "OFF")}");
        }
        
        // OPRAVA: Po zapnutí lights znovu enabled flashlight pokud byl active
        if (turnOn && flashlightSystem != null && flashlightSpotlight != null)
        {
            // Pokud je flashlight system active, zajisti že spotlight je enabled
            if (flashlightSystem.IsFlashlightActive())
            {
                flashlightSpotlight.enabled = true;
                
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Re-enabled flashlight spotlight after lights ON (instant)");
            }
        }
        
        yield return null; // Žádný delay
    }
    
    #endregion
    
    #region Camera Death System
    
    /// <summary>
    /// Callback pro CamsOn tlačítko během Warden útoku
    /// </summary>
    private void OnCamsOnClickedDuringWarden()
    {
        if (isWardenAttacking)
        {
            // OKAMŽitě zavři kamery - hráč je neuvidí
            if (camsOffButton != null)
            {
                if (enableDebugLogs)
                    Debug.Log("[WardenAI] Force closing cameras immediately!");
                    
                camsOffButton.onClick.Invoke();
            }
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] CamsOn clicked during Warden attack - IMMEDIATE DEATH WITH LIGHTS!");
            
            // Spusť camera jumpscare SE SVĚTLY (vypne hned, zapne po 1s)
            StartCoroutine(TriggerCameraJumpscareWithInstantLights());
        }
    }
    
    #endregion
    
    #region Animatronic Pausing System
    
    private void PauseAllAnimatronics()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== PAUSING ALL ANIMATRONICS =====");
            
        // KRITICKÉ: Skryj Fenerir tlačítka pokud jsou viditelná na CAM 09
        if (fenerirAI != null)
        {
            fenerirAI.HideFenerirButtons();
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Hiding Fenerir buttons");
        }
            
        // Zastav attack/defend timery pro dveřní animatroniky
        if (zofeyeAI != null && zofeyeAI.CurrentPosition == 6)
        {
            // Zofeye má pravděpodobně PauseDoorAttack() nebo podobnou metodu
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Pausing Zofeye door attack");
        }
        
        if (caviraAI != null && caviraAI.CurrentPosition == 7)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Pausing Cavira door attack");
        }
        
        if (ianidAI != null && ianidAI.GetCurrentPosition() == 6)
        {
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Pausing Ianid door attack");
        }
    }
    
    private void UnpauseAllAnimatronics()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== UNPAUSING ALL ANIMATRONICS =====");
    }
    
    public bool IsWardenActive()
    {
        return isWardenAttacking;
    }
    
    #endregion
    
    #region Audio System
    
    private void PlayJumpcareSound()
    {
        if (jumpcareAudioSource != null && jumpcareSound != null)
            jumpcareAudioSource.PlayOneShot(jumpcareSound);
    }
    
    private void PlayWalkOffSound()
    {
        if (walkOffAudioSource != null && walkOffSound != null)
            walkOffAudioSource.PlayOneShot(walkOffSound);
    }
    
    private void PlayFakePositionSound()
    {
        if (fakePositionAudioSource != null && fakePositionSound != null)
        {
            fakePositionAudioSource.PlayOneShot(fakePositionSound);
            
            if (enableDebugLogs)
                Debug.Log($"[WardenAI] 🔊 Fake position sound played: {fakePositionSound.name}");
        }
        else if (enableDebugLogs)
        {
            if (fakePositionAudioSource == null)
                Debug.LogWarning("[WardenAI] ⚠️ fakePositionAudioSource is NULL!");
            if (fakePositionSound == null)
                Debug.LogWarning("[WardenAI] ⚠️ fakePositionSound AudioClip is NULL!");
        }
    }
    
    private void PlayAppearSound()
    {
        if (appearAudioSource != null && appearSound != null)
        {
            appearAudioSource.PlayOneShot(appearSound);
            
            if (enableDebugLogs)
                Debug.Log($"[WardenAI] 🔊 Appear sound played: {appearSound.name}");
        }
        else if (enableDebugLogs)
        {
            if (appearAudioSource == null)
                Debug.LogWarning("[WardenAI] ⚠️ appearAudioSource is NULL!");
            if (appearSound == null)
                Debug.LogWarning("[WardenAI] ⚠️ appearSound AudioClip is NULL!");
        }
    }
    
    #endregion
    
    #region Video Player Management
    
    /// <summary>
    /// Připraví video pro přehrávání - aktivuje VideoPlayer a načte první frame
    /// </summary>
    private void PrepareDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[WardenAI] Dead panel VideoPlayer není přiřazený!");
            return;
        }
        
        try
        {
            // Aktivuj VideoPlayer GameObject pokud je neaktivní
            if (!deadPanelVideoPlayer.gameObject.activeInHierarchy)
            {
                deadPanelVideoPlayer.gameObject.SetActive(true);
            }
            
            // Připrav video (načte první frame)
            deadPanelVideoPlayer.Prepare();
            
            // Nastav video na začátek
            deadPanelVideoPlayer.frame = 0;
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Dead panel video připraveno (frame 0)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WardenAI] Chyba při přípravě videa: {e.Message}");
        }
    }
    
    /// <summary>
    /// Spustí přehrávání videa
    /// </summary>
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
            return;
            
        try
        {
            deadPanelVideoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Dead panel video started");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WardenAI] Chyba při spuštění videa: {e.Message}");
        }
    }
    
    /// <summary>
    /// Zastaví video a resetuje ho na začátek
    /// </summary>
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
            return;
            
        try
        {
            deadPanelVideoPlayer.Stop();
            deadPanelVideoPlayer.frame = 0;
            
            if (enableDebugLogs)
                Debug.Log("[WardenAI] Dead panel video zastaveno a resetováno");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WardenAI] Chyba při zastavení videa: {e.Message}");
        }
    }
    
    #endregion
    
    #region Advanced AI System
    
    private void HandleAIUpdates()
    {
        aiUpdateTimer += Time.deltaTime;
        
        if (aiUpdateTimer >= aiUpdateInterval)
        {
            aiUpdateTimer = 0f;
            CheckForAIUpdates();
        }
    }
    
    private void CheckForAIUpdates()
    {
        int currentNight = GetCurrentNight();
        int currentHour = GetCurrentHour();
        
        if (currentNight != lastKnownNight || currentHour != lastKnownHour)
        {
            lastKnownNight = currentNight;
            lastKnownHour = currentHour;
        }
    }
    
    public int GetEffectiveAILevel()
    {
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
            return 0;
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("WARDEN");
            if (customAI >= 0)
                return customAI;
        }
            
        if (!useAdvancedAISystem)
            return manualAILevel;
            
        int night = GetCurrentNight();
        int hour = GetCurrentHour();
        
        if (night >= 1 && night <= 6)
        {
            return aiSettings[night - 1].hourlyAI.GetAIForHour(hour);
        }
        
        return 0;
    }
    
    private int GetCurrentNight()
    {
        if (nightManager != null)
            return nightManager.GetCurrentNight();
        return 1;
    }
    
    private int GetCurrentHour()
    {
        if (shiftTimer != null)
            return shiftTimer.GetCurrentHour();
        return 0;
    }
    
    #endregion
    
    #region Camera Detection
    
    private bool IsPlayerOnCameras()
    {
        bool playerWatchingCameras = false;
        
        if (camsOffButton != null && camsOnButton != null)
            playerWatchingCameras = camsOffButton.gameObject.activeInHierarchy && !camsOnButton.gameObject.activeInHierarchy;
            
        if (!playerWatchingCameras && cameraSystem != null)
            playerWatchingCameras = cameraSystem.AreCamerasOpen();
            
        if (!playerWatchingCameras && ventCameraSystem != null)
            playerWatchingCameras = ventCameraSystem.AreVentCamerasOpen();
            
        return playerWatchingCameras;
    }
    
    #endregion
    
    #region Menu Detection System
    
    private void CheckMenuStatus()
    {
        if (!enableMenuDetection)
        {
            menuIsActive = false;
            return;
        }
        
        bool previousMenuState = menuIsActive;
        menuIsActive = false;
        
        if (menuPanels != null)
        {
            foreach (GameObject panel in menuPanels)
            {
                if (panel != null && panel.activeInHierarchy)
                {
                    menuIsActive = true;
                    break;
                }
            }
        }
        
        if (menuIsActive && isWardenActive)
        {
            ResetWardenAfterInterruption();
        }
    }
    
    #endregion
    
    #region Death Detection System
    
    private void CheckOtherDeathsAndReset()
    {
        // Nekontroluj, pokud Warden nikdy nespawnul (ochrana domovské pozice)
        if (!hasEverSpawned)
            return;
            
        if (otherDeadPanels == null || otherDeadPanels.Length == 0)
            return;
            
        if (!isWardenActive && !isWardenAttacking)
            return;
            
        foreach (GameObject deadPanelCheck in otherDeadPanels)
        {
            if (deadPanelCheck != null && deadPanelCheck.activeInHierarchy)
            {
                if (enableDebugLogs)
                    Debug.Log($"[WardenAI] Other animatronic death detected: {deadPanelCheck.name}");
                    
                ResetWardenAfterInterruption();
                break;
            }
        }
    }
    
    private void ResetWardenAfterInterruption()
    {
        if (!isWardenActive && !isWardenAttacking)
            return;
            
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Reset after interruption");
        
        // Reset off-head penalty při přerušení
        ResetOffHeadPenalty();
        
        // Odstraň CamsOn button listener
        if (camsOnButton != null)
        {
            camsOnButton.onClick.RemoveListener(OnCamsOnClickedDuringWarden);
        }
        
        isWardenActive = false;
        isWardenAttacking = false;
        isWaitingForElectricityOff = false;
        electricityToleranceTimer = 0f;
        
        if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            headShakeAudioSource.Stop();
            
        HideAllPositions();
        
        UnpauseAllAnimatronics();
        
        currentPositionIndex = -1;
    }
    
    #endregion
    
    #region Reset System
    
    public void ResetWarden()
    {
        // Reset off-head penalty při veřejném resetu
        ResetOffHeadPenalty();
        
        // Odstraň CamsOn button listener
        if (camsOnButton != null)
        {
            camsOnButton.onClick.RemoveListener(OnCamsOnClickedDuringWarden);
        }
        
        isWardenActive = false;
        isWardenAttacking = false;
        isWaitingForElectricityOff = false;
        electricityToleranceTimer = 0f;
        
        if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            headShakeAudioSource.Stop();
            
        HideAllPositions();
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
            
        UnpauseAllAnimatronics();
        
        currentPositionIndex = -1;
        
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Reset");
    }
    
    public void ActivateForNewNight()
    {
        ResetWarden();
    }
    
    #endregion
    
    #region Utility Methods
    
    private void UpdateInspectorDisplayValues()
    {
        currentEffectiveAI = GetEffectiveAILevel();
        currentNightDisplay = GetCurrentNight();
        currentTimeDisplay = GetCurrentHour();
        
        if (useAdvancedAISystem)
            systemStatusDisplay = $"Advanced AI (Night {currentNightDisplay}, {currentTimeDisplay}AM)";
        else
            systemStatusDisplay = "Manual AI";
            
        // Update attack status displays
        if (isWardenAttacking)
        {
            waitingForElectricityOff = isWaitingForElectricityOff;
            electricityToleranceRemaining = Mathf.Max(0, electricityTurnOffTolerance - electricityToleranceTimer);
            positionTimerDisplay = positionTimer;
            toleranceRemaining = Mathf.Max(0, currentToleranceTime - toleranceTimer);
            defendProgress = defendTimer;
            defendTimeRequired = currentRequiredDefendTime;
        }
        else
        {
            waitingForElectricityOff = false;
            electricityToleranceRemaining = 0f;
            positionTimerDisplay = 0f;
            toleranceRemaining = 0f;
            defendProgress = 0f;
            defendTimeRequired = 0f;
        }
    }
    
    #endregion
    
    #region Off-Head Penalty System
    
    /// <summary>
    /// Zpracovává off-head penalty - pokud hráč sjede z hlavy, má 0.3s na návrat
    /// </summary>
    private void HandleOffHeadPenalty(bool flashlightOnHead)
    {
        // Update isOnHead status
        isOnHead = flashlightOnHead && !isFakePosition;
        
        // Detekce změny stavu: hráč byl na hlavě a teď není
        if (wasOnHeadLastFrame && !flashlightOnHead && hasPlayerStartedDefending)
        {
            // Hráč sjel z hlavy během obrany!
            if (!isOffHeadPenaltyActive)
            {
                StartOffHeadPenalty();
            }
        }
        // Detekce návratu: hráč nebyl na hlavě a teď je
        else if (!wasOnHeadLastFrame && flashlightOnHead && hasPlayerStartedDefending)
        {
            // Hráč se vrátil na hlavu - zruš penalty
            if (isOffHeadPenaltyActive)
            {
                CancelOffHeadPenalty();
            }
        }
        // Hráč stále není na hlavě a penalty běží
        else if (!flashlightOnHead && isOffHeadPenaltyActive)
        {
            offHeadPenaltyTimer += Time.deltaTime;
            offHeadTimer = offHeadPenaltyTimer; // Pro inspector display
            
            // Čas vypršel - SMRT!
            if (offHeadPenaltyTimer >= offHeadPenaltyTime)
            {
                if (enableDebugLogs)
                    Debug.Log($"[WardenAI] ⚠️ OFF-HEAD PENALTY EXPIRED! Player failed to return to head in {offHeadPenaltyTime}s - DEATH!");
                
                TriggerWardenJumpscare();
                return;
            }
        }
        
        // Update last frame status
        wasOnHeadLastFrame = flashlightOnHead;
    }
    
    /// <summary>
    /// Spustí off-head penalty countdown
    /// </summary>
    private void StartOffHeadPenalty()
    {
        isOffHeadPenaltyActive = true;
        offHeadPenaltyTimer = 0f;
        offHeadTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[WardenAI] ⚠️ OFF-HEAD PENALTY STARTED! Player has {offHeadPenaltyTime}s to return to head!");
    }
    
    /// <summary>
    /// Zruší off-head penalty (hráč se vrátil včas)
    /// </summary>
    private void CancelOffHeadPenalty()
    {
        if (enableDebugLogs)
            Debug.Log($"[WardenAI] ✅ OFF-HEAD PENALTY CANCELLED! Player returned to head in time ({offHeadPenaltyTimer:F2}s / {offHeadPenaltyTime}s)");
        
        isOffHeadPenaltyActive = false;
        offHeadPenaltyTimer = 0f;
        offHeadTimer = 0f;
    }
    
    /// <summary>
    /// Resetuje off-head penalty systém
    /// </summary>
    private void ResetOffHeadPenalty()
    {
        isOffHeadPenaltyActive = false;
        offHeadPenaltyTimer = 0f;
        offHeadTimer = 0f;
        wasOnHeadLastFrame = false;
        isOnHead = false;
        
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Off-head penalty system reset");
    }
    
    #endregion
    
    #region Public API
    
    public bool IsWardenAttacking => isWardenAttacking;
    public int CurrentPositionIndex => currentPositionIndex;
    public int GetCurrentAILevel() => GetEffectiveAILevel();
    
    /// <summary>
    /// Reset při konci noci (6AM) - KRITICKÉ pro zastavení Warden útoku
    /// </summary>
    public void OnNightEnd()
    {
        if (enableDebugLogs)
            Debug.Log("[WardenAI] ===== NIGHT END RESET - STOPPING WARDEN ATTACK =====");
        
        // KRITICKÉ: Zastav VŠECHNY coroutiny
        StopAllCoroutines();
        
        // Deaktivuj Warden okamžitě - skryj všechny pozice
        HideAllPositions();
        
        // Zastav všechny audio
        if (headShakeAudioSource != null && headShakeAudioSource.isPlaying)
            headShakeAudioSource.Stop();
        
        // Odstraň dead panel pokud je aktivní
        if (deadPanel != null)
            deadPanel.SetActive(false);
        
        // Unpause animatroniky pokud byly pausnuté
        UnpauseAllAnimatronics();
        
        // Reset všech timerů
        positionTimer = 0f;
        toleranceTimer = 0f;
        defendTimer = 0f;
        electricityToleranceTimer = 0f;
        offHeadTimer = 0f;
        offHeadPenaltyTimer = 0f;
        
        // Reset všech flags
        isWardenActive = false;
        isWardenAttacking = false;
        isFirstPosition = true;
        isWaitingForElectricityOff = true;
        hasPlayerStartedDefending = false;
        isOffHeadPenaltyActive = false;
        wasOnHeadLastFrame = false;
        isFakePositionSoundPlayed = false;
        currentPositionIndex = -1;
        
        // Reset off-head penalty
        ResetOffHeadPenalty();
        
        // Odstraň CamsOn button listener
        if (camsOnButton != null)
        {
            camsOnButton.onClick.RemoveListener(OnCamsOnClickedDuringWarden);
        }
        
        if (enableDebugLogs)
            Debug.Log("[WardenAI] Night end reset complete - Warden deactivated and all timers stopped");
    }
    
    #endregion
}
