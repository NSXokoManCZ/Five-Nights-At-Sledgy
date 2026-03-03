using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CaviraHourlyAI
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
public class CaviraNightConfiguration
{
    public string nightName;
    public CaviraHourlyAI hourlyAI = new CaviraHourlyAI();
}

/// <summary>
/// AI systém pro animatronic Cavira - agresivnější dvojče Zofaye s útokem na levé dveře
/// a systémem prodlužování intervalu při sledování hráčem
/// </summary>
public class CaviraAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("=== CAVIRA AI SYSTEM ===")]
    [SerializeField] private bool enableCaviraAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Cavira Models - 8 pozic")]
    [SerializeField] private GameObject caviraCamera8Start;  // CAM 8 (Start)
    [SerializeField] private GameObject caviraCamera8;       // CAM 8
    [SerializeField] private GameObject caviraCamera6;       // CAM 6
    [SerializeField] private GameObject caviraCamera4F8;     // CAM 4 (F8)
    [SerializeField] private GameObject caviraCamera5;       // CAM 5
    [SerializeField] private GameObject caviraCamera4F6;     // CAM 4 (F6)
    [SerializeField] private GameObject caviraCamera4;       // CAM 4
    [SerializeField] private GameObject caviraCamera4Doors;  // CAM 4 (Dveře) - nebezpečná pozice
    
    [Header("Movement System")]
    [SerializeField] private float normalMovementInterval = 9f; // Normální interval 9 sekund
    [SerializeField] private float aggressiveMovementInterval = 5f; // Agresivní interval 5 sekund (při zničeném generátoru)
    [SerializeField] private bool enableMovementSystem = true;
    
    [Header("Camera Watching Delay System")]
    [SerializeField] private float watchingDelayBonus = 0.5f; // +0.5 sekundy za každou sekundu sledování
    [SerializeField] private bool enableWatchingDelay = true; // Zapnout/vypnout watching delay systém
    
    [Header("Defense System - Levé dveře")]
    [SerializeField] private tlacitka_dvere_kancelar leftDoorButton; // Tlačítko levých dveří
    [SerializeField] private dvere_kancelar leftDoorScript; // Script levých dveří
    [SerializeField] private float doorResponseTime = 8f; // 8 sekund na zavření dveří
    [SerializeField] private float doorClosedRequiredTime = 15f; // 15 sekund musí být dveře zavřené
    [SerializeField] private bool enableDefenseSystem = true;
    
    [Header("Light System - Ovládání světel v kanceláři")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo (samostatné)
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři (vypne všechny děti)
    
    [Header("Death System")]
    [SerializeField] private GameObject deadPanel; // Dead panel pro jumpscare
    [SerializeField] private AudioSource jumpcareAudioSource; // Audio source pro jumpscare zvuk
    [SerializeField] private AudioClip jumpcareSound; // Zvuk jumpscare
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player na dead panelu
    [SerializeField] private AudioSource retreatAudioSource; // Audio source pro retreat zvuk
    [SerializeField] private AudioClip retreatSound; // Zvuk odchodu po úspěšné obraně
    
    [Header("Light System - Vypnutí před jumpscarem")]
    [SerializeField] private float lightOffBeforeJumpscare = 0.9f; // 0.9s před jumpscarem zhasnou světla
    [SerializeField] private float lightOnAfterDelay = 1f; // 1s po jumpscare se světla rozsvítí
    [SerializeField] private AudioSource lightOffAudioSource; // Audio source pro zvuk vypnutí světel
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí elektřiny
    [SerializeField] private AudioSource electricityGeneratorAudioSource; // Audio source generátoru (zastaví se při jumpscare)
    
    [Header("Advanced AI System - Noc a Čas")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private bool enableRealTimeAIUpdates = true;
    [SerializeField] private float aiUpdateInterval = 1f;
    [SerializeField] private CaviraNightConfiguration[] aiSettings = new CaviraNightConfiguration[6];
    
    [Header("System References")]
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private SledgeAI sledgeAI; // Reference na Sledge pro detekci zničeného generátoru
    [SerializeField] private ThatcherAI thatcherAI; // Reference na Thatcher pro pauzování při jeho útoku
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pausing
    [SerializeField] private OryxAIVerse3 oraxAI; // Reference na Orax pro blokování pozice CAM 4 (dveře)
    [SerializeField] private IanidAI ianidAI; // Reference na Ianid pro vzajemne blokovani dveri
    
    [Header("Other Animatronics - Death Detection")]
    [SerializeField] private GameObject[] otherDeadPanels; // Dead panely jiných animatroniků pro reset
    [SerializeField] private bool enableDeathDetection = true;
    
    [Header("Menu Detection System")]
    [SerializeField] private GameObject[] menuPanels = new GameObject[0];
    [SerializeField] private bool menuIsActive = false;
    [SerializeField] private bool enableMenuDetection = true;
    
    [Header("Movement Effect System")]
    [SerializeField] private GameObject movingPanel;
    [SerializeField] private AudioSource camErrorAudioSource;
    [SerializeField] private AudioClip camErrorSound;
    [SerializeField] private float movingPanelDuration = 1.5f;
    [SerializeField] private bool enableMovingEffect = true;
    
    [Header("Camera System References")]
    [SerializeField] private CameraSystem cameraSystemScript;
    [SerializeField] private CameryVentSystem ventCameraSystemScript;
    [SerializeField] private GameObject camsOffButton;
    [SerializeField] private GameObject camsOnButton;
    
    [Header("Camera Buttons for Watching Detection")]
    [SerializeField] private Button cameraButton8Start;  // Tlačítko pro CAM 8 (Start)
    [SerializeField] private Button cameraButton8;       // Tlačítko pro CAM 8
    [SerializeField] private Button cameraButton6;       // Tlačítko pro CAM 6
    [SerializeField] private Button cameraButton4F8;     // Tlačítko pro CAM 4 (F8)
    [SerializeField] private Button cameraButton5;       // Tlačítko pro CAM 5
    [SerializeField] private Button cameraButton4F6;     // Tlačítko pro CAM 4 (F6)
    [SerializeField] private Button cameraButton4;       // Tlačítko pro CAM 4
    [SerializeField] private Button cameraButton4Doors;  // Tlačítko pro CAM 4 (Dveře)
    
    [Header("Game Over System")]
    [SerializeField] private float gameOverDelay = 3f;
    
    [Header("Current AI Status - READ ONLY")]
    [SerializeField] private int currentEffectiveAI = 0;
    [SerializeField] private int currentNightDisplay = 1;
    [SerializeField] private int currentTimeDisplay = 0;
    [SerializeField] private string systemStatusDisplay = "Manual AI";
    [SerializeField] private string currentPositionDisplay = "Inactive";
    [SerializeField] private float currentIntervalDisplay = 9f; // Aktuální interval pro debug
    [SerializeField] private float watchingBonusDisplay = 0f; // Akumulovaný watching bonus pro debug
    [SerializeField] private bool isBeingWatchedDisplay = false; // Zda je právě sledována
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Private Variables
    
    // Position mapping: 81 = CAM 8 (Start), 8 = CAM 8, 6 = CAM 6, 48 = CAM 4 (F8), 5 = CAM 5, 46 = CAM 4 (F6), 4 = CAM 4, 40 = CAM 4 (Dveře)
    private int currentCameraPosition = -1;
    private float movementTimer = 0f;
    private bool isCaviraActive = false;
    private bool canCaviraMove = false;
    private bool isAggressive = false; // True když je generátor zničený
    
    // Camera watching delay system
    private float watchingDelayAccumulated = 0f; // Akumulovaný bonus čas ze sledování
    private bool isBeingWatched = false; // Zda hráč právě sleduje Caviru na kamerách
    private float lastWatchCheckTime = 0f;
    
    // Defense tracking
    private bool isAtDoors = false;
    private bool isDoorDefenseActive = false;
    private float doorDefenseTimer = 0f;
    private float doorClosedTimer = 0f;
    private bool areDoorsClosed = false;
    private bool isInJumpscare = false; // CRITICAL: Prevents multiple jumpscare triggers
    
    // AI system tracking
    private int lastKnownNight = 1;
    private int lastKnownHour = 0;
    private float aiUpdateTimer = 0f;
    
    // Reset tracking
    private bool hasBeenResetAfterDeath = false;
    
    // Movement paths dictionary
    private Dictionary<int, int[]> possibleMoves;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeCaviraSystem();
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] START() - enableCaviraAI: {enableCaviraAI}, AI Level: {GetEffectiveAILevel()}");
            
        // VŽDY aktivovat a zobrazit na první pozici (i když AI = 0)
        if (enableCaviraAI)
        {
            isCaviraActive = true;
            MoveToPosition(81); // Start na CAM 8 (Start)
            
            isDoorDefenseActive = false;
            isAtDoors = false;
            movementTimer = GetCurrentMovementInterval();
            watchingDelayAccumulated = 0f;
            
            // Pokud AI > 0, povolit pohyb
            canCaviraMove = GetEffectiveAILevel() > 0;
            
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] Cavira aktivována na pozici CAM 8 (Start) - AI: {GetEffectiveAILevel()}, CanMove: {canCaviraMove}, Model: {(caviraCamera8Start != null ? caviraCamera8Start.name : "NULL")}");
        }
        else
        {
            isCaviraActive = false;
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Cavira neaktivní - enableCaviraAI je FALSE!");
        }
    }
    
    void Update()
    {
        if (!enableCaviraAI)
            return;
            
        // CHECK FOR WARDEN - PAUSE ALL TIMERS IF WARDEN IS ATTACKING
        if (wardenAI != null && wardenAI.IsWardenActive())
            return;
            
        // Check other character deaths
        if (enableDeathDetection)
            CheckOtherDeathsAndReset();
            
        // Check menu status
        CheckMenuStatus();
        
        // Auto enable/disable movement based on ShiftTimer
        bool shouldMove = !menuIsActive && IsShiftTimerRunning() && GetEffectiveAILevel() > 0;
        
        if (shouldMove != canCaviraMove)
        {
            canCaviraMove = shouldMove;
            
            if (enableDebugLogs)
            {
                if (menuIsActive)
                    Debug.Log("[CaviraAI] Pohyb ZAKÁZÁN - menu aktivní");
                else if (!IsShiftTimerRunning())
                    Debug.Log("[CaviraAI] Pohyb ZAKÁZÁN - ShiftTimer neběží");
                else if (GetEffectiveAILevel() <= 0)
                    Debug.Log("[CaviraAI] Pohyb ZAKÁZÁN - AI = 0");
                else
                    Debug.Log("[CaviraAI] Pohyb POVOLEN - ShiftTimer běží a AI > 0");
            }
        }
        
        // Advanced AI updates
        if (useAdvancedAISystem && enableRealTimeAIUpdates)
            HandleAIUpdates();
            
        // Check aggressive mode (destroyed generator)
        CheckAggressiveMode();
        
        // Movement system
        if (enableMovementSystem && isCaviraActive && canCaviraMove)
            HandleMovementSystem();
            
        // Defense system at doors - POUZE když není aktivní menu!
        if (enableDefenseSystem && isAtDoors && !menuIsActive)
            HandleDefenseSystem();
            
        // Camera watching detection
        if (enableWatchingDelay && isCaviraActive && canCaviraMove && !isAtDoors)
            HandleCameraWatchingDetection();
            
        // Update inspector display values
        UpdateInspectorDisplayValues();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeCaviraSystem()
    {
        // Initialize AI settings
        if (aiSettings == null || aiSettings.Length != 6)
        {
            aiSettings = new CaviraNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                aiSettings[i] = new CaviraNightConfiguration
                {
                    nightName = $"Night {i + 1}",
                    hourlyAI = new CaviraHourlyAI()
                };
            }
        }
        
        // Hide dead panel
        if (deadPanel != null)
            deadPanel.SetActive(false);
            
        // Initialize movement paths
        InitializeMovementPaths();
        
        // Initialize audio
        InitializeAudioSystem();
        
        // Find system references
        FindSystemReferences();
        
        // Log system status
        if (useAdvancedAISystem)
        {
            systemStatusDisplay = "Advanced AI System";
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Using Advanced AI System (Night & Time based)");
        }
        else
        {
            systemStatusDisplay = "Manual AI";
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] Using Manual AI Level: {manualAILevel}");
        }
    }
    
    private void InitializeMovementPaths()
    {
        possibleMoves = new Dictionary<int, int[]>
        {
            { -1, new int[] { 81 } },           // Inactive → CAM 8 (Start)
            { 81, new int[] { 8 } },            // CAM 8 (Start) → CAM 8
            { 8, new int[] { 6, 48 } },         // CAM 8 → CAM 6 nebo CAM 4 (F8)
            { 6, new int[] { 5, 46 } },         // CAM 6 → CAM 5 nebo CAM 4 (F6)
            { 5, new int[] { 6, 46 } },         // CAM 5 → CAM 6 nebo CAM 4 (F6)
            { 48, new int[] { 4 } },            // CAM 4 (F8) → CAM 4
            { 46, new int[] { 4 } },            // CAM 4 (F6) → CAM 4
            { 4, new int[] { 40 } },            // CAM 4 → CAM 4 (Dveře)
            { 40, new int[] { } }               // CAM 4 (Dveře) - konečná pozice
        };
        
        if (enableDebugLogs)
            Debug.Log("[CaviraAI] Movement paths initialized - 8 pozic s větvením");
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
        
        if (retreatAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 2)
                retreatAudioSource = existingSources[1];
            else
                retreatAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (retreatAudioSource != null)
        {
            retreatAudioSource.playOnAwake = false;
            retreatAudioSource.loop = false;
        }
        
        if (camErrorAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 3)
                camErrorAudioSource = existingSources[2];
            else
                camErrorAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (camErrorAudioSource != null)
        {
            camErrorAudioSource.playOnAwake = false;
            camErrorAudioSource.loop = false;
        }
    }
    
    private void FindSystemReferences()
    {
        if (shiftTimer == null)
            shiftTimer = FindFirstObjectByType<ShiftTimer>();
            
        if (nightManager == null)
            nightManager = FindFirstObjectByType<NightShiftManagement>();
            
        if (sledgeAI == null)
            sledgeAI = FindFirstObjectByType<SledgeAI>();
            
        if (thatcherAI == null)
            thatcherAI = FindFirstObjectByType<ThatcherAI>();
            
        if (oraxAI == null)
            oraxAI = FindFirstObjectByType<OryxAIVerse3>();
    }
    
    #endregion
    
    #region Movement System
    
    private void HandleMovementSystem()
    {
        float currentInterval = GetCurrentMovementInterval() + watchingDelayAccumulated;
        
        movementTimer += Time.deltaTime;
        
        if (movementTimer >= currentInterval)
        {
            AttemptMovement();
        }
    }
    
    private float GetCurrentMovementInterval()
    {
        return isAggressive ? aggressiveMovementInterval : normalMovementInterval;
    }
    
    private void AttemptMovement()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI <= 0)
        {
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] AI is 0 - Cavira inactive");
            return;
        }
        
        float movementChance = CalculateMovementChance(effectiveAI);
        float randomRoll = Random.value;
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] Movement attempt: {randomRoll:F2} vs {movementChance:F2} (AI {effectiveAI})");
        
        if (randomRoll <= movementChance)
        {
            ExecuteMovement();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Movement attempt FAILED");
        }
        
        // Reset timer and watching bonus
        movementTimer = 0f;
        watchingDelayAccumulated = 0f;
    }
    
    private float CalculateMovementChance(int aiLevel)
    {
        float baseChance;
        
        if (aiLevel <= 0)
            baseChance = 0f;
        else if (aiLevel >= 20)
            baseChance = 1f;
        else
            baseChance = Mathf.Lerp(0.1f, 0.9f, (aiLevel - 1) / 19f);
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] Movement chance for AI {aiLevel}: {baseChance:F2} ({baseChance * 100f:F0}%)");
        
        return baseChance;
    }
    
    private void ExecuteMovement()
    {
        if (!isCaviraActive)
        {
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Cannot execute movement - Cavira inactive");
            return;
        }
        
        if (!possibleMoves.ContainsKey(currentCameraPosition))
        {
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] No possible moves from position {currentCameraPosition}");
            return;
        }
        
        int[] possiblePositions = possibleMoves[currentCameraPosition];
        
        if (possiblePositions.Length == 0)
        {
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] At final position - no more moves");
            return;
        }
        
        // Check if trying to move to CAM 4 (Dveře) - block if Orax is there
        if (currentCameraPosition == 4)
        {
            if (IsOraxAtDoors())
            {
                if (enableDebugLogs)
                    Debug.Log("[CaviraAI] BLOCKED - Orax is at CAM 4 (Dveře), cannot move there");
                
                // Reset timer and try again later
                movementTimer = 0f;
                watchingDelayAccumulated = 0f;
                return;
            }
            
            // NOVA KONTROLA: Pokud je Ianid u dveri, Cavira nemuze jit na CAM 4 (Dvere)
            if (IsIanidAtDoors())
            {
                if (enableDebugLogs)
                    Debug.Log("[CaviraAI] BLOCKED - Ianid is at doors, cannot move to CAM 4 (Dvere)");
                
                // Reset timer and try again later
                movementTimer = 0f;
                watchingDelayAccumulated = 0f;
                return;
            }
        }
        
        int nextPosition = possiblePositions[Random.Range(0, possiblePositions.Length)];
        MoveToPosition(nextPosition);
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] Moving from position {currentCameraPosition} to {nextPosition}");
    }
    
    private void MoveToPosition(int newPosition)
    {
        bool shouldShowMovingEffect = ShouldShowMovingEffect();
        bool isPlayerOnCam = IsPlayerOnCam();
        
        if (shouldShowMovingEffect && isPlayerOnCam)
        {
            StartCoroutine(ShowMovingEffect());
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Showing moving effect - player on cameras");
        }
        
        HideAllCaviraModels();
        
        GameObject targetModel = GetModelForPosition(newPosition);
        
        if (targetModel != null)
        {
            targetModel.SetActive(true);
            currentCameraPosition = newPosition;
            
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] Model activated: {targetModel.name} for position {newPosition} ({GetPositionName(newPosition)})");
            
            // Check if at doors
            if (newPosition == 40) // CAM 4 (Dveře)
            {
                isAtDoors = true;
                StartDoorDefense();
            }
            else
            {
                isAtDoors = false;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] Moved to position {newPosition} ({GetPositionName(newPosition)})");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] ERROR: No model for position {newPosition}");
        }
    }
    
    private string GetPositionName(int position)
    {
        return position switch
        {
            81 => "CAM 8 (Start)",
            8 => "CAM 8",
            6 => "CAM 6",
            48 => "CAM 4 (F8)",
            5 => "CAM 5",
            46 => "CAM 4 (F6)",
            4 => "CAM 4",
            40 => "CAM 4 (Dveře)",
            _ => "Unknown"
        };
    }
    
    private bool ShouldShowMovingEffect()
    {
        if (!enableMovingEffect)
            return false;
            
        return IsPlayerOnCam();
    }
    
    private bool IsPlayerOnCam()
    {
        bool playerWatchingCameras = false;
        
        if (camsOffButton != null && camsOnButton != null)
            playerWatchingCameras = camsOffButton.activeInHierarchy && !camsOnButton.activeInHierarchy;
            
        if (!playerWatchingCameras && cameraSystemScript != null)
            playerWatchingCameras = cameraSystemScript.AreCamerasOpen();
            
        if (!playerWatchingCameras && ventCameraSystemScript != null)
            playerWatchingCameras = ventCameraSystemScript.AreVentCamerasOpen();
            
        return playerWatchingCameras;
    }
    
    private System.Collections.IEnumerator ShowMovingEffect()
    {
        if (movingPanel != null)
            movingPanel.SetActive(true);
            
        PlayCamErrorSound();
        
        float elapsedTime = 0f;
        
        while (elapsedTime < movingPanelDuration)
        {
            if (!IsPlayerOnCam())
            {
                if (movingPanel != null)
                    movingPanel.SetActive(false);
                    
                StopCamErrorSound();
                yield break;
            }
            
            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (movingPanel != null)
            movingPanel.SetActive(false);
            
        StopCamErrorSound();
    }
    
    private void PlayCamErrorSound()
    {
        if (camErrorAudioSource != null && camErrorSound != null)
            camErrorAudioSource.PlayOneShot(camErrorSound);
    }
    
    private void StopCamErrorSound()
    {
        if (camErrorAudioSource != null && camErrorAudioSource.isPlaying)
            camErrorAudioSource.Stop();
    }
    
    private GameObject GetModelForPosition(int position)
    {
        return position switch
        {
            81 => caviraCamera8Start,
            8 => caviraCamera8,
            6 => caviraCamera6,
            48 => caviraCamera4F8,
            5 => caviraCamera5,
            46 => caviraCamera4F6,
            4 => caviraCamera4,
            40 => caviraCamera4Doors,
            _ => null
        };
    }
    
    private void HideAllCaviraModels()
    {
        if (caviraCamera8Start != null) caviraCamera8Start.SetActive(false);
        if (caviraCamera8 != null) caviraCamera8.SetActive(false);
        if (caviraCamera6 != null) caviraCamera6.SetActive(false);
        if (caviraCamera4F8 != null) caviraCamera4F8.SetActive(false);
        if (caviraCamera5 != null) caviraCamera5.SetActive(false);
        if (caviraCamera4F6 != null) caviraCamera4F6.SetActive(false);
        if (caviraCamera4 != null) caviraCamera4.SetActive(false);
        if (caviraCamera4Doors != null) caviraCamera4Doors.SetActive(false);
    }
    
    #endregion
    
    #region Camera Watching Delay System
    
    private void HandleCameraWatchingDetection()
    {
        bool currentlyWatched = IsPlayerWatchingCavira();
        
        if (currentlyWatched && IsPlayerOnCam())
        {
            if (!isBeingWatched)
            {
                isBeingWatched = true;
                lastWatchCheckTime = Time.time;
                
                if (enableDebugLogs)
                    Debug.Log($"[CaviraAI] Player STARTED watching Cavira at position {GetPositionName(currentCameraPosition)}");
            }
            
            // Accumulate watching bonus (+0.5s per 1s of watching)
            float deltaTime = Time.time - lastWatchCheckTime;
            lastWatchCheckTime = Time.time;
            
            watchingDelayAccumulated += deltaTime * watchingDelayBonus;
            
            if (enableDebugLogs && Time.frameCount % 60 == 0) // Log každou sekundu
                Debug.Log($"[CaviraAI] Watching bonus accumulated: +{watchingDelayAccumulated:F1}s (total interval: {GetCurrentMovementInterval() + watchingDelayAccumulated:F1}s)");
        }
        else
        {
            if (isBeingWatched)
            {
                isBeingWatched = false;
                
                if (enableDebugLogs)
                    Debug.Log($"[CaviraAI] Player STOPPED watching Cavira - total bonus: +{watchingDelayAccumulated:F1}s");
            }
        }
    }
    
    private bool IsPlayerWatchingCavira()
    {
        if (!IsPlayerOnCam())
            return false;
            
        // Simplified: If player is on cameras and Cavira is visible, assume they're watching
        // This is conservative but ensures delay system activates when needed
        return true;
    }
    
    #endregion
    
    #region Defense System
    
    private void StartDoorDefense()
    {
        isDoorDefenseActive = true;
        doorDefenseTimer = doorResponseTime;
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] Door defense STARTED - {doorResponseTime}s to close left door");
    }
    
    private void HandleDefenseSystem()
    {
        if (!isDoorDefenseActive)
            return;
            
        // Check if Thatcher is active and blocking Cavira
        bool isThatcherBlocking = IsThatcherCurrentlyActive();
        
        if (isThatcherBlocking)
        {
            // Pause all timers while Thatcher is active
            if (enableDebugLogs && doorDefenseTimer % 2f < Time.deltaTime)
                Debug.Log($"[CaviraAI] Defense PAUSED - Thatcher active, remaining time: {doorDefenseTimer:F1}s");
                
            return;
        }
        
        // Normal defense countdown
        bool currentDoorState = IsLeftDoorClosed();
        
        if (!currentDoorState)
        {
            // Doors are OPEN - countdown to attack
            doorDefenseTimer -= Time.deltaTime;
            
            if (enableDebugLogs && doorDefenseTimer % 1f < Time.deltaTime)
                Debug.Log($"[CaviraAI] Doors OPEN - attack in {doorDefenseTimer:F1}s");
        }
        
        // Handle door state changes
        if (currentDoorState)
        {
            if (!areDoorsClosed)
            {
                areDoorsClosed = true;
                doorClosedTimer = 0f;
                
                if (enableDebugLogs)
                    Debug.Log("[CaviraAI] Left door CLOSED - starting defense timer");
            }
            else
            {
                doorClosedTimer += Time.deltaTime;
                
                if (enableDebugLogs && doorClosedTimer % 1f < Time.deltaTime)
                    Debug.Log($"[CaviraAI] Door closed for {doorClosedTimer:F1}s / {doorClosedRequiredTime}s");
                    
                if (doorClosedTimer >= doorClosedRequiredTime)
                {
                    HandleSuccessfulDefense();
                    return;
                }
            }
        }
        else
        {
            if (areDoorsClosed)
            {
                areDoorsClosed = false;
                doorClosedTimer = 0f;
                
                if (enableDebugLogs)
                    Debug.Log("[CaviraAI] Left door OPENED - defense progress lost!");
            }
        }
        
        // Check for attack
        if (doorDefenseTimer <= 0f)
        {
            if (!isInJumpscare)
            {
                TriggerCaviraJumpscare();
            }
        }
    }
    
    private bool IsThatcherCurrentlyActive()
    {
        if (thatcherAI != null)
            return thatcherAI.IsThatcherActive();
            
        return false;
    }
    
    private bool IsLeftDoorClosed()
    {
        if (leftDoorScript != null)
            return !leftDoorScript.IsOpen;
            
        return false;
    }
    
    private void HandleSuccessfulDefense()
    {
        isDoorDefenseActive = false;
        isAtDoors = false;
        
        PlayRetreatSound();
        
        // Return to random safe position
        int[] retreatPositions = new int[] { 81, 5, 6, 48 }; // CAM 8 (Start), CAM 5, CAM 6, CAM 4 (F8)
        int retreatPosition = retreatPositions[Random.Range(0, retreatPositions.Length)];
        
        MoveToPosition(retreatPosition);
        
        // Reset timers
        movementTimer = 0f;
        watchingDelayAccumulated = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] Successful defense! Cavira retreated to {GetPositionName(retreatPosition)}");
    }
    
    private void TriggerCaviraJumpscare()
    {
        if (isInJumpscare)
        {
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Jumpscare already in progress - ignoring duplicate trigger");
            return;
        }
        
        isInJumpscare = true;
        isDoorDefenseActive = false;
        isAtDoors = false; // CRITICAL: Immediately mark as not at doors
        canCaviraMove = false; // CRITICAL: Stop all movement
        
        if (enableDebugLogs)
            Debug.Log("[CaviraAI] Cavira jumpscare triggered!");
        
        StartCoroutine(TriggerCaviraJumpcareWithLights());
    }
    
    private System.Collections.IEnumerator TriggerCaviraJumpcareWithLights()
    {
        // 1. Vypnout světla s 0.9s zpožděním před jumpscarem
        yield return StartCoroutine(FlickerLightsJumpscare(false));
        
        // 2. Zobrazit dead panel s videem (0.1s delay pro načtení)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // 3. Přehrát jumpscare sound
        PlayJumpcareSound();
        
        // 4. Spustit game over sekvenci (generátor a světla zůstanou vypnuté po celou dobu)
        StartCoroutine(HandleGameOver());
    }
    
    private System.Collections.IEnumerator FlickerLightsJumpscare(bool turnOn)
    {
        if (!turnOn)
        {
            // Zastavit zvuk generátoru při vypnutí světel
            if (electricityGeneratorAudioSource != null && electricityGeneratorAudioSource.isPlaying)
            {
                electricityGeneratorAudioSource.Stop();
            }
            
            // Před vypnutím světel přehrát zvuk elektřiny
            if (lightOffAudioSource != null && lightOffSound != null)
            {
                lightOffAudioSource.PlayOneShot(lightOffSound);
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
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] Lights turned OFF - waiting {lightOffBeforeJumpscare}s before jumpscare");
            yield return new WaitForSeconds(lightOffBeforeJumpscare);
        }
        else
        {
            // Znovu spustit zvuk generátoru při zapnutí světel
            if (electricityGeneratorAudioSource != null && !electricityGeneratorAudioSource.isPlaying)
            {
                electricityGeneratorAudioSource.Play();
            }
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Lights turned ON after jumpscare");
        }
    }
    
    #region Video Player Management
    
    private System.Collections.IEnumerator ShowDeadPanelWithVideoDelay()
    {
        PrepareDeadPanelVideo();
        
        // Počkat 0.1s na načtení videa
        yield return new WaitForSeconds(0.1f);
        
        if (deadPanel != null)
        {
            deadPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Dead panel activated with video");
        }
        
        StartDeadPanelVideo();
    }
    
    private void PrepareDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.gameObject.SetActive(true);
            deadPanelVideoPlayer.Prepare();
            deadPanelVideoPlayer.frame = 0;
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Dead panel video prepared");
        }
    }
    
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Dead panel video started");
        }
    }
    
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Stop();
            deadPanelVideoPlayer.frame = 0;
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Dead panel video stopped");
        }
    }
    
    #endregion
    
    private System.Collections.IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        
        StopDeadPanelVideo();
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
        
        // Po skrytí dead panelu zapnout světla a generátor zpět
        yield return StartCoroutine(FlickerLightsJumpscare(true));
        
        isInJumpscare = false; // Reset flag after game over sequence
        
        if (nightManager != null)
        {
            nightManager.ForceReturnToMenu();
        }
    }
    
    private void PlayJumpcareSound()
    {
        if (jumpcareAudioSource != null && jumpcareSound != null)
            jumpcareAudioSource.PlayOneShot(jumpcareSound);
    }
    
    private void PlayRetreatSound()
    {
        if (retreatAudioSource != null && retreatSound != null)
            retreatAudioSource.PlayOneShot(retreatSound);
    }
    
    #endregion
    
    #region Aggressive Mode (Destroyed Generator)
    
    private void CheckAggressiveMode()
    {
        bool shouldBeAggressive = false;
        
        if (sledgeAI != null)
        {
            // Check if generator is destroyed via reflection
            System.Reflection.FieldInfo generatorField = sledgeAI.GetType().GetField("isGeneratorDestroyed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (generatorField != null)
            {
                shouldBeAggressive = (bool)generatorField.GetValue(sledgeAI);
            }
        }
        
        if (shouldBeAggressive != isAggressive)
        {
            isAggressive = shouldBeAggressive;
            
            if (isAggressive)
            {
                if (enableDebugLogs)
                    Debug.Log($"[CaviraAI] AGGRESSIVE MODE ACTIVATED - interval changed to {aggressiveMovementInterval}s");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[CaviraAI] Aggressive mode deactivated - interval restored to {normalMovementInterval}s");
            }
        }
    }
    
    #endregion
    
    #region Orax Interaction System
    
    private bool IsOraxAtDoors()
    {
        if (oraxAI == null)
            return false;
            
        // Check if Orax is at CAM 04 Door position using reflection
        System.Reflection.FieldInfo posField = oraxAI.GetType().GetField("currentPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (posField != null)
        {
            int oraxPosition = (int)posField.GetValue(oraxAI);
            return oraxPosition == 5; // Position 5 in Orax = CAM 04 (Door)
        }
        
        return false;
    }
    
    private bool IsIanidAtDoors()
    {
        if (ianidAI == null)
            return false;
            
        return ianidAI.IsAtDoors;
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
            
            if (enableDebugLogs)
                Debug.Log($"[CaviraAI] AI updated for Night {currentNight}, Hour {currentHour}AM");
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
            int customAI = nightManager.GetCustomNightAI("CAVIRA");
            if (customAI >= 0)
                return customAI;
        }
            
        if (!useAdvancedAISystem)
            return manualAILevel;
            
        int night = GetCurrentNight();
        int hour = GetCurrentHour();
        
        if (night >= 1 && night <= 6)
        {
            int aiLevel = aiSettings[night - 1].hourlyAI.GetAIForHour(hour);
            return aiLevel;
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
    
    private bool IsShiftTimerRunning()
    {
        if (shiftTimer == null)
            return false;
            
        return shiftTimer.enabled && !shiftTimer.Won;
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
        
        // Check own dead panel first
        if (deadPanel != null && deadPanel.activeInHierarchy)
        {
            // CRITICAL: Don't reset during active jumpscare
            if (isCaviraActive && !isInJumpscare)
            {
                ResetCaviraAfterDeath();
                
                if (enableDebugLogs)
                    Debug.Log("[CaviraAI] Own jumpscare detected - resetting Cavira");
            }
            return;
        }
        
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
        
        if (previousMenuState != menuIsActive && enableDebugLogs)
            Debug.Log($"[CaviraAI] Menu status changed: {(menuIsActive ? "ACTIVE" : "INACTIVE")}");
            
        if (!menuIsActive && previousMenuState)
        {
            // Menu was closed - VŽDY vrať Caviru na startovní pozici
            if (enableCaviraAI)
            {
                isCaviraActive = true;
                MoveToPosition(81); // Reset na CAM 8 (Start)
                
                // Reset všech stavů
                isAtDoors = false;
                isDoorDefenseActive = false;
                movementTimer = GetCurrentMovementInterval();
                watchingDelayAccumulated = 0f;
                doorDefenseTimer = 0f;
                doorClosedTimer = 0f;
                areDoorsClosed = false;
                canCaviraMove = GetEffectiveAILevel() > 0; // Povolit pohyb jen pokud AI > 0
                
                if (enableDebugLogs)
                    Debug.Log("[CaviraAI] Menu closed - Cavira resetována na startovní pozici CAM 8 (Start)");
            }
        }
        else if (menuIsActive && !previousMenuState)
        {
            // Menu was opened - set canMove to false
            canCaviraMove = false;
        }
    }
    
    #endregion
    
    #region Death Detection System
    
    private void CheckOtherDeathsAndReset()
    {
        if (otherDeadPanels == null || otherDeadPanels.Length == 0)
        {
            hasBeenResetAfterDeath = false; // Reset flag když žádné dead panely nejsou aktivní
            return;
        }
            
        bool anyDeadPanelActive = false;
        foreach (GameObject deadPanelCheck in otherDeadPanels)
        {
            if (deadPanelCheck != null && deadPanelCheck.activeInHierarchy)
            {
                anyDeadPanelActive = true;
                break;
            }
        }
        
        if (anyDeadPanelActive && !hasBeenResetAfterDeath)
        {
            ResetCaviraAfterDeath();
            hasBeenResetAfterDeath = true; // Označ že reset již proběhl
        }
        else if (!anyDeadPanelActive)
        {
            hasBeenResetAfterDeath = false; // Reset flag když už žádný panel není aktivní
        }
    }
    
    private void ResetCaviraAfterDeath()
    {
        // OPRAVA: Neschovávej modely! Vrať Caviru na startovní pozici a zastav pohyb
        if (enableCaviraAI)
        {
            isCaviraActive = true;
            MoveToPosition(81); // Vrať na CAM 8 (Start) - nastaví i currentCameraPosition
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] Reset - Cavira vrácena na pozici 81 (CAM 8 Start)");
        }
        else
        {
            HideAllCaviraModels();
            isCaviraActive = false;
            currentCameraPosition = -1;
        }
        
        // Reset všech stavů VČetně DOOR DEFENSE!
        isAtDoors = false;
        isDoorDefenseActive = false;
        isInJumpscare = false; // Reset jumpscare flag
        movementTimer = GetCurrentMovementInterval();
        watchingDelayAccumulated = 0f;
        isBeingWatched = false;
        doorDefenseTimer = 0f; // RESET door timer!
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        canCaviraMove = false;
        
        if (enableDebugLogs)
            Debug.Log("[CaviraAI] Reset completed - door defense stopped, movement stopped");
    }
    
    #endregion
    
    #region Reset System
    
    public void ResetCavira()
    {
        // KRITICKÉ: Zastav všechny coroutiny (včetně kill timeru u dveří)
        StopAllCoroutines();
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
        
        // OPRAVA: Stejné chování jako ResetCaviraAfterDeath - vrať na pozici 81
        if (enableCaviraAI)
        {
            isCaviraActive = true;
            MoveToPosition(81); // Vrať na CAM 8 (Start)
            
            if (enableDebugLogs)
                Debug.Log("[CaviraAI] ResetCavira - Cavira vrácena na pozici 81 (CAM 8 Start)");
        }
        else
        {
            HideAllCaviraModels();
            isCaviraActive = false;
            currentCameraPosition = -1;
        }
        
        // Reset všech stavů včetně door defense!
        isAtDoors = false;
        isDoorDefenseActive = false;
        isInJumpscare = false; // Reset jumpscare flag
        movementTimer = GetCurrentMovementInterval();
        watchingDelayAccumulated = 0f;
        isBeingWatched = false;
        doorDefenseTimer = 0f;
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        isAggressive = false;
        canCaviraMove = false; // Pohyb se povolí v Update() pokud AI > 0
        hasBeenResetAfterDeath = false;
        
        if (enableDebugLogs)
            Debug.Log("[CaviraAI] ResetCavira complete - all states reset, door defense stopped, coroutines stopped");
    }
    
    public void ActivateForNewNight()
    {
        // ResetCavira() již aktivuje a zobrazuje Caviru na startovní pozici
        ResetCavira();
        
        if (enableDebugLogs)
            Debug.Log($"[CaviraAI] ActivateForNewNight - Cavira ready at start position, AI: {GetEffectiveAILevel()}");
    }
    
    #endregion
    
    #region Utility Methods
    
    private void UpdateInspectorDisplayValues()
    {
        currentEffectiveAI = GetEffectiveAILevel();
        currentNightDisplay = GetCurrentNight();
        currentTimeDisplay = GetCurrentHour();
        currentIntervalDisplay = GetCurrentMovementInterval() + watchingDelayAccumulated;
        watchingBonusDisplay = watchingDelayAccumulated;
        isBeingWatchedDisplay = isBeingWatched;
        
        if (useAdvancedAISystem)
            systemStatusDisplay = $"Advanced AI (Night {currentNightDisplay}, {currentTimeDisplay}AM)";
        else
            systemStatusDisplay = "Manual AI";
            
        if (!isCaviraActive)
            currentPositionDisplay = "Inactive";
        else if (isAtDoors)
            currentPositionDisplay = $"{GetPositionName(currentCameraPosition)} - AT DOORS ({doorDefenseTimer:F1}s)";
        else
            currentPositionDisplay = $"{GetPositionName(currentCameraPosition)}";
    }
    
    #endregion
    
    #region Public API
    
    public bool IsCaviraActive => isCaviraActive;
    public bool IsAtDoors => isAtDoors;
    public int CurrentPosition => currentCameraPosition;
    public int GetCurrentPosition() => currentCameraPosition; // Alternative method for CurrentPosition
    public int GetCurrentAILevel() => GetEffectiveAILevel();
    
    #endregion
}
