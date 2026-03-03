using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class IanidHourlyAI
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
public class IanidNightConfiguration
{
    public string nightName;
    public IanidHourlyAI hourlyAI = new IanidHourlyAI();
}

/// <summary>
/// AI systém pro animatronic Ianid - mechanika pravé/falešné postavy
/// Hráč musí identifikovat pravou Ianid pomocí kamer a zavřít správné dveře
/// </summary>
public class IanidAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("=== IANID AI SYSTEM ===")]
    [SerializeField] private bool enableIanidAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Ianid Models - 4 GameObjects (3 pozice + 1 skrytá)")]
    [SerializeField] private GameObject ianidPodiumCAM8;      // Pozice 1: Podium CAM 8
    [SerializeField] private GameObject ianidHidden;          // Pozice 2: Skrytá pozice (neviditelná)
    [SerializeField] private GameObject ianidRightDoor;       // Pozice 3A: Pravé dveře
    [SerializeField] private GameObject ianidLeftDoor;        // Pozice 3B: Levé dveře
    
    [Header("Movement System")]
    [SerializeField] private float movementInterval = 25f; // 25 sekund interval
    [SerializeField] private bool enableMovementSystem = true;
    
    [Header("Defense System - Dveře")]
    [SerializeField] private tlacitka_dvere_kancelar rightDoorButton; // Tlačítko pravých dveří
    [SerializeField] private dvere_kancelar rightDoorScript; // Script pravých dveří
    [SerializeField] private tlacitka_dvere_kancelar leftDoorButton; // Tlačítko levých dveří
    [SerializeField] private dvere_kancelar leftDoorScript; // Script levých dveří
    [SerializeField] private float doorResponseTime = 15f; // 15 sekund na zavření správných dveří
    [SerializeField] private bool enableDefenseSystem = true;
    
    [Header("Light System - Blikání při spawnu/odchodu")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo (samostatné)
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři (vypne všechny děti)
    [SerializeField] private float lightFlickerDuration = 1f; // Doba bliknutí světla (1s - původně 0.5s)
    
    [Header("Death System")]
    [SerializeField] private GameObject deadPanel; // Dead panel pro jumpscare
    [SerializeField] private AudioSource jumpcareAudioSource; // Audio source pro jumpscare zvuk
    [SerializeField] private AudioClip jumpcareSound; // Zvuk jumpscare
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player na dead panelu
    
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
    [SerializeField] private IanidNightConfiguration[] aiSettings = new IanidNightConfiguration[6];
    
    [Header("System References")]
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private ElectricityManagement electricitySystem; // Pro kontrolu elektřiny
    [SerializeField] private SledgeAI sledgeAI; // Pro kontrolu zničeného generátoru
    [SerializeField] private ThatcherAI thatcherAI; // Reference na Thatcher pro pauzování timeru
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pauzování timeru
    [SerializeField] private ZofeyeAI zofeyeAI; // Reference na Zofaye pro blokování CAM 1
    [SerializeField] private OryxAIVerse3 oraxAI; // Reference na Orax pro blokování CAM 4
    [SerializeField] private CaviraAI caviraAI; // Reference na Cavira pro blokování CAM 4
    
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
    
    [Header("Ianid Specific Audio")]
    [SerializeField] private AudioSource blikaniZarovekAudioSource; // Audio source pro blikání žárovek
    [SerializeField] private AudioClip[] blikaniZarovekSounds = new AudioClip[5]; // Seznam 5 zvuků blikání žárovek
    [SerializeField] private AudioSource walkingOffIanidAudioSource; // Audio source pro odchod Ianid
    [SerializeField] private AudioClip walkingOffIanidSound; // Zvuk odchodu Ianid
    
    [Header("Camera System References")]
    [SerializeField] private CameraSystem cameraSystemScript;
    [SerializeField] private CameryVentSystem ventCameraSystemScript;
    [SerializeField] private Button camsOffButton; // Aktivní když hráč sleduje kamery
    [SerializeField] private Button camsOnButton;  // Aktivní když hráč je v kanceláři
    
    [Header("Game Over System")]
    [SerializeField] private float gameOverDelay = 3f;
    
    [Header("Current AI Status - READ ONLY")]
    [SerializeField] private int currentEffectiveAI = 0;
    [SerializeField] private int currentNightDisplay = 1;
    [SerializeField] private int currentTimeDisplay = 0;
    [SerializeField] private string systemStatusDisplay = "Manual AI";
    [SerializeField] private string currentPositionDisplay = "Inactive";
    [SerializeField] private bool isRealIanidOnRight = false; // True = pravá je real, False = levá je real
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Private Variables
    
    // Position tracking: 1 = Podium CAM 8, 2 = Hidden, 3 = Doors
    private int currentPosition = 1;
    private float movementTimer = 0f;
    private bool isIanidActive = false;
    private bool canIanidMove = false;
    
    // Defense tracking
    private bool isAtDoors = false;
    private bool isDoorDefenseActive = false;
    private float doorDefenseTimer = 0f; // Countdown do smrti když jsou dveře OTEVŘENÉ
    private float doorClosedTimer = 0f; // Čas jak dlouho jsou SPRÁVNÉ dveře zavřené
    private float requiredDoorClosedTime = 5f; // 5 sekund musí být zavřené
    private bool hasPlayerClosedCorrectDoor = false; // Zda hráč zavřel správné dveře
    private bool correctDoorClosed = false; // Zda jsou správné dveře zavřené
    private float wrongDoorClosedTimer = 0f; // Timer pro smrt při zavření špatných dveří
    
    // AI system tracking
    private int lastKnownNight = 1;
    private int lastKnownHour = 0;
    private float aiUpdateTimer = 0f;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeIanidSystem();
        
        if (enableDebugLogs)
            Debug.Log($"[IanidAI] START() - enableIanidAI: {enableIanidAI}, AI Level: {GetEffectiveAILevel()}");
            
        if (enableIanidAI)
        {
            // Vždy aktivuj a zobraz Ianid na startovní pozici
            isIanidActive = true;
            SetIanidPosition(1); // Start na Podium CAM 8
            
            isDoorDefenseActive = false;
            isAtDoors = false;
            movementTimer = movementInterval;
            
            // canIanidMove se nastaví automaticky v Update() podle AI level, Warden a Thatcher
            if (enableDebugLogs)
                Debug.Log($"[IanidAI] Ianid aktivována na pozici Podium CAM 8 - AI: {GetEffectiveAILevel()}, Model: {(ianidPodiumCAM8 != null ? ianidPodiumCAM8.name : "NULL")}");
        }
        else
        {
            isIanidActive = false;
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Ianid neaktivní - enableIanidAI je FALSE!");
        }
    }
    
    void Update()
    {
        if (!enableIanidAI)
            return;
            
        // Check other character deaths
        if (enableDeathDetection)
            CheckOtherDeathsAndReset();
            
        // Check menu status
        CheckMenuStatus();
        
        // BLOCKING: Warden útok - zastavit timer
        bool isWardenBlocking = IsWardenAttacking();
        
        // BLOCKING: Thatcher spawn - zastavit timer
        bool isThatcherBlocking = IsThatcherActive();
        
        // Auto enable/disable movement based on ShiftTimer + Warden + Thatcher
        bool shouldMove = !menuIsActive && !isWardenBlocking && !isThatcherBlocking && IsShiftTimerRunning() && GetEffectiveAILevel() > 0;
        
        if (shouldMove != canIanidMove)
        {
            canIanidMove = shouldMove;
            
            if (enableDebugLogs)
            {
                if (menuIsActive)
                    Debug.Log("[IanidAI] Pohyb ZAKÁZÁN - menu aktivní");
                else if (isWardenBlocking)
                    Debug.Log("[IanidAI] Pohyb ZAKÁZÁN - Warden útočí");
                else if (isThatcherBlocking)
                    Debug.Log("[IanidAI] Pohyb ZAKÁZÁN - Thatcher je aktivní");
                else if (!IsShiftTimerRunning())
                    Debug.Log("[IanidAI] Pohyb ZAKÁZÁN - ShiftTimer neběží");
                else if (GetEffectiveAILevel() <= 0)
                    Debug.Log("[IanidAI] Pohyb ZAKÁZÁN - AI = 0");
                else
                    Debug.Log("[IanidAI] Pohyb POVOLEN - ShiftTimer běží a AI > 0");
            }
        }
        
        // Advanced AI updates
        if (useAdvancedAISystem && enableRealTimeAIUpdates)
            HandleAIUpdates();
            
        // Movement system
        if (enableMovementSystem && isIanidActive && canIanidMove && !isAtDoors)
            HandleMovementSystem();
            
        // Defense system at doors - BLOCKING: Zastavit během Warden/Thatcher útoku a v menu!
        if (enableDefenseSystem && isAtDoors && !isWardenBlocking && !isThatcherBlocking && !menuIsActive)
            HandleDefenseSystem();
        
        // WARDEN/THATCHER BLOCKING: Obnovit viditelnost OBĚ Ianid u dveří
        if (isAtDoors && (isWardenBlocking || isThatcherBlocking))
            EnsureBothIanidVisibleDuringBlocking();
            
        // Update inspector display values
        UpdateInspectorDisplayValues();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeIanidSystem()
    {
        // Initialize AI settings
        if (aiSettings == null || aiSettings.Length != 6)
        {
            aiSettings = new IanidNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                aiSettings[i] = new IanidNightConfiguration
                {
                    nightName = $"Night {i + 1}",
                    hourlyAI = new IanidHourlyAI()
                };
            }
        }
        
        // Hide dead panel
        if (deadPanel != null)
            deadPanel.SetActive(false);
            
        // Initialize audio
        InitializeAudioSystem();
        
        // Find system references
        FindSystemReferences();
        
        // Hide all Ianid models
        HideAllIanidModels();
        
        // Log system status
        if (useAdvancedAISystem)
        {
            systemStatusDisplay = "Advanced AI System";
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Using Advanced AI System (Night & Time based)");
        }
        else
        {
            systemStatusDisplay = "Manual AI";
            if (enableDebugLogs)
                Debug.Log($"[IanidAI] Using Manual AI Level: {manualAILevel}");
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
        
        if (blikaniZarovekAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 2)
                blikaniZarovekAudioSource = existingSources[1];
            else
                blikaniZarovekAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (blikaniZarovekAudioSource != null)
        {
            blikaniZarovekAudioSource.playOnAwake = false;
            blikaniZarovekAudioSource.loop = false;
        }
        
        if (walkingOffIanidAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 3)
                walkingOffIanidAudioSource = existingSources[2];
            else
                walkingOffIanidAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (walkingOffIanidAudioSource != null)
        {
            walkingOffIanidAudioSource.playOnAwake = false;
            walkingOffIanidAudioSource.loop = false;
        }
        
        if (camErrorAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 4)
                camErrorAudioSource = existingSources[3];
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
            
        if (wardenAI == null)
            wardenAI = FindFirstObjectByType<WardenAI>();
            
        if (thatcherAI == null)
            thatcherAI = FindFirstObjectByType<ThatcherAI>();
            
        if (zofeyeAI == null)
            zofeyeAI = FindFirstObjectByType<ZofeyeAI>();
            
        if (oraxAI == null)
            oraxAI = FindFirstObjectByType<OryxAIVerse3>();
            
        if (caviraAI == null)
            caviraAI = FindFirstObjectByType<CaviraAI>();
    }
    
    #endregion
    
    #region Movement System
    
    private void HandleMovementSystem()
    {
        // AI 0 = no movement at all
        if (GetEffectiveAILevel() <= 0)
        {
            if (enableDebugLogs && movementTimer % 5f < Time.deltaTime)
                Debug.Log("[IanidAI] AI is 0 - movement interval not updating");
            return;
        }
        
        movementTimer -= Time.deltaTime;
        
        if (movementTimer <= 0f)
        {
            AttemptMovement();
        }
    }
    
    private void AttemptMovement()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI <= 0)
        {
            if (enableDebugLogs)
                Debug.Log("[IanidAI] AI is 0 - Ianid inactive");
            movementTimer = movementInterval;
            return;
        }
        
        float movementChance = CalculateMovementChance(effectiveAI);
        float randomRoll = Random.value;
        
        if (enableDebugLogs)
            Debug.Log($"[IanidAI] Movement attempt: {randomRoll:F2} vs {movementChance:F2} (AI {effectiveAI})");
        
        if (randomRoll <= movementChance)
        {
            ExecuteMovement();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Movement attempt FAILED");
            movementTimer = movementInterval;
        }
    }
    
    private float CalculateMovementChance(int aiLevel)
    {
        // AI 0 = 0%, AI 1 = 20%, AI 20 = 50%
        if (aiLevel <= 0)
            return 0f;
        else if (aiLevel >= 20)
            return 0.5f;
        else
            return Mathf.Lerp(0.2f, 0.5f, (aiLevel - 1) / 19f);
    }
    
    private void ExecuteMovement()
    {
        if (currentPosition == 1)
        {
            // Z Podium CAM 8 → Hidden
            MoveToHidden();
        }
        else if (currentPosition == 2)
        {
            // Z Hidden → Doors (pokud nejsou blokovány)
            if (CanMoveToDoorsPosition())
            {
                MoveToDoors();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("[IanidAI] Cannot move to doors - other animatronics blocking");
                movementTimer = movementInterval;
            }
        }
    }
    
    private bool CanMoveToDoorsPosition()
    {
        // Check if electricity is OFF or generator is destroyed
        if (IsElectricityOff() || IsGeneratorDestroyed())
        {
            if (enableDebugLogs)
                Debug.Log("[IanidAI] BLOCKED - Electricity is OFF or generator destroyed");
            return false;
        }
        
        // Check if Zofaye is at CAM 1 (dveře)
        if (zofeyeAI != null && zofeyeAI.IsAtDoors)
        {
            if (enableDebugLogs)
                Debug.Log("[IanidAI] BLOCKED - Zofaye is at CAM 1 (dveře)");
            return false;
        }
        
        // Check if Orax is at CAM 4 (dveře)
        if (oraxAI != null && oraxAI.IsAtCAM04Door)
        {
            if (enableDebugLogs)
                Debug.Log("[IanidAI] BLOCKED - Orax is at CAM 4 (dveře)");
            return false;
        }
        
        // Check if Cavira is at CAM 4 (dveře)
        if (caviraAI != null && caviraAI.IsAtDoors)
        {
            if (enableDebugLogs)
                Debug.Log("[IanidAI] BLOCKED - Cavira is at CAM 4 (dveře)");
            return false;
        }
        
        return true;
    }
    
    private void MoveToHidden()
    {
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Moving from Podium CAM 8 → Hidden");
        
        // Show moving effect if player is on cameras
        if (ShouldShowMovingEffect())
        {
            StartCoroutine(ShowMovingEffect());
        }
        
        SetIanidPosition(2); // Hidden position
        movementTimer = movementInterval;
    }
    
    private void MoveToDoors()
    {
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Moving from Hidden → Doors");
        
        // Show moving effect if player is on cameras
        if (ShouldShowMovingEffect())
        {
            StartCoroutine(ShowMovingEffect());
        }
        
        // Start light flicker + sounds
        StartCoroutine(SpawnIanidAtDoorsWithFlicker());
    }
    
    private IEnumerator SpawnIanidAtDoorsWithFlicker()
    {
        // Play blikani zarovek sound
        PlayBlikaniZarovekSound();
        
        // Flicker lights OFF
        yield return StartCoroutine(FlickerLights(false));
        
        // Decide which Ianid is real (50/50 chance)
        isRealIanidOnRight = Random.value > 0.5f;
        
        if (enableDebugLogs)
            Debug.Log($"[IanidAI] Real Ianid is on: {(isRealIanidOnRight ? "RIGHT" : "LEFT")} door");
        
        // Teleport Ianid to doors during flicker
        SetIanidPosition(3);
        isAtDoors = true;
        
        // Flicker lights ON
        yield return StartCoroutine(FlickerLights(true));
        
        // Start defense timer
        StartDoorDefense();
    }
    
    private IEnumerator FlickerLights(bool turnOn)
    {
        if (mainLight != null)
        {
            mainLight.SetActive(turnOn);
        }
        
        if (parentLight != null)
        {
            parentLight.SetActive(turnOn);
            
            if (enableDebugLogs)
                Debug.Log($"[IanidAI] Office lights flickered: {(turnOn ? "ON" : "OFF")}");
        }
        
        yield return new WaitForSeconds(lightFlickerDuration);
    }
    
    private void SetIanidPosition(int position)
    {
        HideAllIanidModels();
        
        currentPosition = position;
        
        switch (position)
        {
            case 1: // Podium CAM 8
                if (ianidPodiumCAM8 != null)
                {
                    ianidPodiumCAM8.SetActive(true);
                    if (enableDebugLogs)
                        Debug.Log("[IanidAI] Ianid visible at Podium CAM 8");
                }
                break;
                
            case 2: // Hidden (invisible)
                if (ianidHidden != null)
                {
                    ianidHidden.SetActive(false); // Always invisible
                    if (enableDebugLogs)
                        Debug.Log("[IanidAI] Ianid moved to Hidden position (invisible)");
                }
                break;
                
            case 3: // Doors (both visible in office, only real visible on cameras)
                if (ianidRightDoor != null)
                {
                    ianidRightDoor.SetActive(true);
                    if (enableDebugLogs)
                        Debug.Log("[IanidAI] Right door Ianid visible");
                }
                if (ianidLeftDoor != null)
                {
                    ianidLeftDoor.SetActive(true);
                    if (enableDebugLogs)
                        Debug.Log("[IanidAI] Left door Ianid visible");
                }
                break;
        }
    }
    
    private void HideAllIanidModels()
    {
        if (ianidPodiumCAM8 != null) ianidPodiumCAM8.SetActive(false);
        if (ianidHidden != null) ianidHidden.SetActive(false);
        if (ianidRightDoor != null) ianidRightDoor.SetActive(false);
        if (ianidLeftDoor != null) ianidLeftDoor.SetActive(false);
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
            playerWatchingCameras = camsOffButton.gameObject.activeInHierarchy && !camsOnButton.gameObject.activeInHierarchy;
            
        if (!playerWatchingCameras && cameraSystemScript != null)
            playerWatchingCameras = cameraSystemScript.AreCamerasOpen();
            
        if (!playerWatchingCameras && ventCameraSystemScript != null)
            playerWatchingCameras = ventCameraSystemScript.AreVentCamerasOpen();
            
        return playerWatchingCameras;
    }
    
    private IEnumerator ShowMovingEffect()
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
    
    #endregion
    
    #region Defense System
    
    private void StartDoorDefense()
    {
        isDoorDefenseActive = true;
        doorDefenseTimer = doorResponseTime; // 15s countdown do jumpscare
        doorClosedTimer = 0f;
        requiredDoorClosedTime = 5f; // 5 sekund musí být zavřené
        hasPlayerClosedCorrectDoor = false;
        correctDoorClosed = false;
        wrongDoorClosedTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[IanidAI] Door defense STARTED - {doorResponseTime}s to close correct door for {requiredDoorClosedTime}s");
    }
    
    private void HandleDefenseSystem()
    {
        if (!isDoorDefenseActive)
            return;
            
        // Handle camera viewing - hide fake Ianid when player watches cameras
        HandleCameraVisibility();
            
        // Check if Thatcher is active and blocking Ianid
        bool isThatcherBlocking = IsThatcherCurrentlyActive();
        
        if (isThatcherBlocking)
        {
            // Pause ALL timers while Thatcher is active
            if (enableDebugLogs && doorDefenseTimer % 2f < Time.deltaTime)
                Debug.Log($"[IanidAI] Defense PAUSED - Thatcher active");
                
            return;
        }
        
        // Check door states
        bool rightDoorClosed = IsRightDoorClosed();
        bool leftDoorClosed = IsLeftDoorClosed();
        
        // Determine which door should be closed
        bool correctDoorIsNowClosed = isRealIanidOnRight ? rightDoorClosed : leftDoorClosed;
        bool wrongDoorIsNowClosed = isRealIanidOnRight ? leftDoorClosed : rightDoorClosed;
        
        // WRONG DOOR CLOSED = DEATH COUNTDOWN (ale pouze když správné dveře NEJSOU zavřené)
        if (wrongDoorIsNowClosed && !correctDoorIsNowClosed)
        {
            wrongDoorClosedTimer += Time.deltaTime;
            
            if (enableDebugLogs && wrongDoorClosedTimer % 0.2f < Time.deltaTime)
                Debug.Log($"[IanidAI] WRONG door is closed (correct door OPEN)! Death in {1f - wrongDoorClosedTimer:F1}s");
            
            // After 1 second of wrong door being closed = INSTANT DEATH
            if (wrongDoorClosedTimer >= 1f)
            {
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] WRONG door was closed - DEATH! Real was on {(isRealIanidOnRight ? "RIGHT" : "LEFT")}, player closed {(isRealIanidOnRight ? "LEFT" : "RIGHT")}");
                
                TriggerIanidJumpscare();
                return;
            }
        }
        else
        {
            // Wrong door was opened OR correct door is closed - reset timer
            wrongDoorClosedTimer = 0f;
        }
        
        // CORRECT DOOR LOGIC
        if (correctDoorIsNowClosed)
        {
            // Player closed correct door
            if (!hasPlayerClosedCorrectDoor)
            {
                hasPlayerClosedCorrectDoor = true;
                
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] CORRECT door closed! Starting {requiredDoorClosedTime}s timer");
            }
            
            // Correct door is closed - count up doorClosedTimer
            doorClosedTimer += Time.deltaTime;
            correctDoorClosed = true;
            
            if (enableDebugLogs && doorClosedTimer % 0.5f < Time.deltaTime)
                Debug.Log($"[IanidAI] Correct door closed for {doorClosedTimer:F1}s / {requiredDoorClosedTime}s");
            
            // Check if player held door long enough
            if (doorClosedTimer >= requiredDoorClosedTime)
            {
                HandleSuccessfulDefense();
                return;
            }
            
            // While correct door is closed, PAUSE doorDefenseTimer (death countdown)
            // Don't decrement doorDefenseTimer here
        }
        else
        {
            // Correct door is OPEN
            if (correctDoorClosed)
            {
                // Door was just opened - player failed to hold it long enough
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] Door opened too early! Progress lost: {doorClosedTimer:F1}s / {requiredDoorClosedTime}s");
                
                doorClosedTimer = 0f; // Reset progress
                correctDoorClosed = false;
            }
            
            // Countdown to death continues when correct door is OPEN
            doorDefenseTimer -= Time.deltaTime;
            
            if (enableDebugLogs && doorDefenseTimer % 1f < Time.deltaTime)
                Debug.Log($"[IanidAI] Correct door OPEN - death in {doorDefenseTimer:F1}s");
            
            // Timer expired - player failed
            if (doorDefenseTimer <= 0f)
            {
                if (enableDebugLogs)
                    Debug.Log("[IanidAI] Defense timer expired - player failed!");
                TriggerIanidJumpscare();
            }
        }
    }
    
    /// <summary>
    /// Handles visibility of fake Ianid based on camera state
    /// Fake Ianid is HIDDEN when CamsOff is active (player on cameras)
    /// </summary>
    private void HandleCameraVisibility()
    {
        if (!isAtDoors)
            return;
            
        bool playerOnCameras = IsPlayerOnCam();
        
        // Get fake Ianid GameObject
        GameObject fakeIanid = isRealIanidOnRight ? ianidLeftDoor : ianidRightDoor;
        GameObject realIanid = isRealIanidOnRight ? ianidRightDoor : ianidLeftDoor;
        
        if (playerOnCameras)
        {
            // Player is watching cameras - HIDE fake Ianid, SHOW only real
            if (fakeIanid != null && fakeIanid.activeSelf)
            {
                fakeIanid.SetActive(false);
                
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] Cameras ON - hiding FAKE Ianid on {(isRealIanidOnRight ? "LEFT" : "RIGHT")}");
            }
            
            if (realIanid != null && !realIanid.activeSelf)
            {
                realIanid.SetActive(true);
                
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] Cameras ON - showing REAL Ianid on {(isRealIanidOnRight ? "RIGHT" : "LEFT")}");
            }
        }
        else
        {
            // Player is in office - SHOW both Ianid (player can't tell which is real)
            if (fakeIanid != null && !fakeIanid.activeSelf)
            {
                fakeIanid.SetActive(true);
                
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] In office - showing FAKE Ianid on {(isRealIanidOnRight ? "LEFT" : "RIGHT")}");
            }
            
            if (realIanid != null && !realIanid.activeSelf)
            {
                realIanid.SetActive(true);
                
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] In office - showing REAL Ianid on {(isRealIanidOnRight ? "RIGHT" : "LEFT")}");
            }
        }
    }
    
    private bool IsThatcherCurrentlyActive()
    {
        if (thatcherAI != null)
            return thatcherAI.IsThatcherActive();
            
        return false;
    }
    
    private bool IsElectricityOff()
    {
        if (electricitySystem != null)
            return !electricitySystem.IsPowerOn();
            
        return false;
    }
    
    private bool IsGeneratorDestroyed()
    {
        if (sledgeAI != null)
        {
            // Check if generator is destroyed via reflection
            System.Reflection.FieldInfo generatorField = sledgeAI.GetType().GetField("isGeneratorDestroyed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (generatorField != null)
            {
                return (bool)generatorField.GetValue(sledgeAI);
            }
        }
        
        return false;
    }
    
    private bool IsRightDoorClosed()
    {
        if (rightDoorScript != null)
            return !rightDoorScript.IsOpen;
            
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
        
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Successful defense! Ianid retreating...");
        
        // Play retreat sounds + light flicker
        StartCoroutine(RetreatIanidWithFlicker());
    }
    
    private IEnumerator RetreatIanidWithFlicker()
    {
        // Show moving effect if player is on cameras
        if (ShouldShowMovingEffect())
        {
            StartCoroutine(ShowMovingEffect());
        }
        
        // Play blikani zarovek sound
        PlayBlikaniZarovekSound();
        
        // Play walking off sound
        PlayWalkingOffIanidSound();
        
        // Flicker lights OFF
        yield return StartCoroutine(FlickerLights(false));
        
        // Teleport Ianid back to Podium CAM 8 during flicker
        SetIanidPosition(1);
        
        // Flicker lights ON
        yield return StartCoroutine(FlickerLights(true));
        
        // Reset timers
        movementTimer = movementInterval;
        
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Ianid returned to Podium CAM 8");
    }
    
    private void TriggerIanidJumpscare()
    {
        isDoorDefenseActive = false;
        
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Ianid jumpscare triggered!");
        
        StartCoroutine(TriggerIanidJumpcareWithLights());
    }
    
    private System.Collections.IEnumerator TriggerIanidJumpcareWithLights()
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
                Debug.Log($"[IanidAI] Lights turned OFF - waiting {lightOffBeforeJumpscare}s before jumpscare");
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
                Debug.Log("[IanidAI] Lights turned ON after jumpscare");
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
                Debug.Log("[IanidAI] Dead panel activated with video");
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
                Debug.Log("[IanidAI] Dead panel video prepared");
        }
    }
    
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Dead panel video started");
        }
    }
    
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Stop();
            deadPanelVideoPlayer.frame = 0;
            
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Dead panel video stopped");
        }
    }
    
    #endregion
    
    private IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        
        StopDeadPanelVideo();
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
        
        // Po skrytí dead panelu zapnout světla a generátor zpět
        yield return StartCoroutine(FlickerLightsJumpscare(true));
        
        if (nightManager != null)
        {
            nightManager.ForceReturnToMenu();
        }
    }
    
    #endregion
    
    #region Audio System
    
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
    
    private void PlayJumpcareSound()
    {
        if (jumpcareAudioSource != null && jumpcareSound != null)
            jumpcareAudioSource.PlayOneShot(jumpcareSound);
    }
    
    private void PlayBlikaniZarovekSound()
    {
        if (blikaniZarovekAudioSource != null && blikaniZarovekSounds != null && blikaniZarovekSounds.Length > 0)
        {
            // Vyber náhodný zvuk ze seznamu
            AudioClip randomSound = blikaniZarovekSounds[Random.Range(0, blikaniZarovekSounds.Length)];
            
            if (randomSound != null)
            {
                blikaniZarovekAudioSource.PlayOneShot(randomSound);
                
                if (enableDebugLogs)
                    Debug.Log($"[IanidAI] Playing BlikaniZarovek sound (random from {blikaniZarovekSounds.Length} sounds)");
            }
        }
    }
    
    private void PlayWalkingOffIanidSound()
    {
        if (walkingOffIanidAudioSource != null && walkingOffIanidSound != null)
        {
            walkingOffIanidAudioSource.PlayOneShot(walkingOffIanidSound);
            
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Playing WalkingOffIanid sound");
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
            
            if (enableDebugLogs)
                Debug.Log($"[IanidAI] AI updated for Night {currentNight}, Hour {currentHour}AM");
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
            int customAI = nightManager.GetCustomNightAI("IANID");
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
            if (isIanidActive)
            {
                ResetIanidAfterDeath();
                
                if (enableDebugLogs)
                    Debug.Log("[IanidAI] Own jumpscare detected - resetting Ianid");
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
            Debug.Log($"[IanidAI] Menu status changed: {(menuIsActive ? "ACTIVE" : "INACTIVE")}");
            
        if (!menuIsActive && previousMenuState)
        {
            // Menu was closed - VŽDY vrať Ianid na startovní pozici
            if (enableIanidAI)
            {
                isIanidActive = true;
                SetIanidPosition(1); // Reset na Podium CAM 8
                
                // Reset všech stavů
                isAtDoors = false;
                isDoorDefenseActive = false;
                movementTimer = movementInterval;
                doorDefenseTimer = 0f;
                doorClosedTimer = 0f;
                hasPlayerClosedCorrectDoor = false;
                correctDoorClosed = false;
                wrongDoorClosedTimer = 0f;
                canIanidMove = GetEffectiveAILevel() > 0; // Povolit pohyb jen pokud AI > 0
                
                if (enableDebugLogs)
                    Debug.Log("[IanidAI] Menu closed - Ianid resetována na startovní pozici Podium CAM 8");
            }
        }
        else if (menuIsActive && !previousMenuState)
        {
            // Menu was opened - RESET Ianid kompletně!
            if (isIanidActive || isAtDoors)
            {
                if (enableDebugLogs)
                    Debug.Log("[IanidAI] Menu opened - resetting Ianid completely");
                
                ResetIanidAfterDeath();
            }
            
            canIanidMove = false;
        }
    }
    
    #endregion
    
    #region Death Detection System
    
    private void CheckOtherDeathsAndReset()
    {
        if (otherDeadPanels == null || otherDeadPanels.Length == 0)
            return;
            
        foreach (GameObject deadPanelCheck in otherDeadPanels)
        {
            if (deadPanelCheck != null && deadPanelCheck.activeInHierarchy)
            {
                ResetIanidAfterDeath();
                break;
            }
        }
    }
    
    private void ResetIanidAfterDeath()
    {
        // OPRAVA: Neschovej modely! Vrať Ianid na startovní pozici a zastav pohyb
        if (enableIanidAI)
        {
            isIanidActive = true;
            SetIanidPosition(1); // Vrať na Podium CAM 8
        }
        else
        {
            HideAllIanidModels();
            isIanidActive = false;
        }
        
        isAtDoors = false;
        isDoorDefenseActive = false;
        movementTimer = movementInterval;
        doorDefenseTimer = 0f;
        doorClosedTimer = 0f;
        hasPlayerClosedCorrectDoor = false;
        correctDoorClosed = false;
        wrongDoorClosedTimer = 0f;
        canIanidMove = false;
        
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Reset - Ianid vrácena na startovní pozici, pohyb zastaven");
    }
    
    #endregion
    
    #region Reset System
    
    public void ResetIanid()
    {
        // KRITICKÉ: Zastav všechny coroutiny (včetně kill timeru u dveří)
        StopAllCoroutines();
        
        HideAllIanidModels();
        
        if (deadPanel != null)
            deadPanel.SetActive(false);
            
        currentPosition = 1;
        isIanidActive = false;
        isAtDoors = false;
        isDoorDefenseActive = false;
        movementTimer = 0f;
        doorDefenseTimer = 0f;
        doorClosedTimer = 0f;
        hasPlayerClosedCorrectDoor = false;
        correctDoorClosed = false;
        wrongDoorClosedTimer = 0f;
        
        // Show Ianid at starting position (Podium CAM 8)
        if (ianidPodiumCAM8 != null)
        {
            ianidPodiumCAM8.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Ianid reset - visible at Podium CAM 8");
        }
        
        if (enableDebugLogs)
            Debug.Log("[IanidAI] Ianid reset to starting position - all coroutines stopped");
    }
    
    public void OnNewNightStarted()
    {
        ResetIanid();
        
        if (enableIanidAI && GetEffectiveAILevel() > 0)
        {
            isIanidActive = true;
            SetIanidPosition(1);
            movementTimer = movementInterval;
            
            if (enableDebugLogs)
                Debug.Log("[IanidAI] Ianid activated for new night at Podium CAM 8");
        }
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
            
        if (!isIanidActive)
            currentPositionDisplay = "Inactive";
        else if (isAtDoors)
            currentPositionDisplay = $"At Doors ({doorDefenseTimer:F1}s) - Real on {(isRealIanidOnRight ? "RIGHT" : "LEFT")}";
        else
            currentPositionDisplay = GetPositionName(currentPosition);
    }
    
    private string GetPositionName(int position)
    {
        return position switch
        {
            1 => "Podium CAM 8",
            2 => "Hidden (Invisible)",
            3 => "At Doors",
            _ => "Unknown"
        };
    }
    
    #endregion
    
    #region Warden/Thatcher Blocking - Ianid Visibility Fix
    
    /// <summary>
    /// Během Warden/Thatcher útoku zajistí, že jsou OBĚ Ianid u dveří viditelné
    /// (aby hráč neviděl, které dveře jsou správné)
    /// </summary>
    private void EnsureBothIanidVisibleDuringBlocking()
    {
        if (!isAtDoors)
            return;
        
        // Ujisti se, že OBĚ Ianid jsou viditelné
        bool leftWasHidden = false;
        bool rightWasHidden = false;
        
        if (ianidLeftDoor != null && !ianidLeftDoor.activeSelf)
        {
            ianidLeftDoor.SetActive(true);
            leftWasHidden = true;
        }
        
        if (ianidRightDoor != null && !ianidRightDoor.activeSelf)
        {
            ianidRightDoor.SetActive(true);
            rightWasHidden = true;
        }
        
        if (enableDebugLogs && (leftWasHidden || rightWasHidden))
            Debug.Log($"[IanidAI] Warden/Thatcher blocking - restored visibility of both Ianid (Left: {leftWasHidden}, Right: {rightWasHidden})");
    }
    
    #endregion
    
    #region Warden & Thatcher Detection
    
    /// <summary>
    /// Kontroluje, zda Warden útočí
    /// </summary>
    private bool IsWardenAttacking()
    {
        if (wardenAI != null)
            return wardenAI.IsWardenAttacking;
        
        return false;
    }
    
    /// <summary>
    /// Kontroluje, zda je Thatcher aktivní
    /// </summary>
    private bool IsThatcherActive()
    {
        if (thatcherAI != null)
            return thatcherAI.IsThatcherActive();
        
        return false;
    }
    
    #endregion
    
    #region Public API
    
    public bool IsIanidActive => isIanidActive;
    public bool IsAtDoors => isAtDoors;
    public int CurrentPosition => currentPosition;
    public int GetCurrentPosition() => currentPosition; // Alternative method for CurrentPosition
    public int GetCurrentAILevel() => GetEffectiveAILevel();
    public bool IsRealOnRight => isRealIanidOnRight;
    
    #endregion
}
