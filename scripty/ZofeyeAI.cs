using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ZofeyeHourlyAI
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
public class ZofeyeNightConfiguration
{
    public string nightName;
    public ZofeyeHourlyAI hourlyAI = new ZofeyeHourlyAI();
}

/// <summary>
/// AI systém pro animatronic Zofaye s pohybem mezi kamerami a obranou pomocí pravých dveří
/// </summary>
public class ZofeyeAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("=== ZOFAYE AI SYSTEM ===")]
    [SerializeField] private bool enableZofayeAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Zofaye Models - 7 pozic (každá pozice = jiný model)")]
    [SerializeField] private GameObject zofayeCamera8; // Startovní pozice
    [SerializeField] private GameObject zofayeCamera7; // První pohyb
    [SerializeField] private GameObject zofayeCamera3; // Volba A z kamery 7
    [SerializeField] private GameObject zofayeCamera11; // Volba B z kamery 7, NEBO pokračování z kamery 3
    [SerializeField] private GameObject zofayeCamera2; // Předposlední pozice (POUZE z kamery 11)
    [SerializeField] private GameObject zofayeCamera1Storage; // CAM 01 (sklad) - volitelná cesta z CAM 02
    [SerializeField] private GameObject zofayeCamera1; // CAM 01 (dveře) - nebezpečná pozice
    
    [Header("Movement System")]
    [SerializeField] private float movementInterval = 5f; // 5 sekund interval
    [SerializeField] private bool enableMovementSystem = true;
    
    [Header("Defense System - Pravé dveře")]
    [SerializeField] private tlacitka_dvere_kancelar rightDoorButton; // Tlačítko pravých dveří
    [SerializeField] private dvere_kancelar rightDoorScript; // Script pravých dveří
    [SerializeField] private float doorResponseTime = 5f; // Čas na zavření dveří
    [SerializeField] private float doorClosedRequiredTime = 4f; // Jak dlouho musí být dveře zavřené aby Zofaye odešel
    [SerializeField] private bool enableDefenseSystem = true;
    
    [Header("Death System")]
    [SerializeField] private GameObject deadPanel; // Dead panel pro jumpcare
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player pro dead panel (child objektu deadPanel)
    [SerializeField] private GameObject thatcherDeadPanel; // Dead panel Thatchera - pro detekci jeho smrti
    [SerializeField] private AudioSource jumpcareAudioSource; // Audio source pro jumpcare zvuk
    [SerializeField] private AudioClip jumpcareSound; // Zvuk jumpcare
    [SerializeField] private AudioSource retreatAudioSource; // Audio source pro retreat zvuk
    [SerializeField] private AudioClip retreatSound; // Zvuk odchodu po úspěšné obraně
    
    [Header("Light System - Vypnutí před jumpscarem")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo kanceláře
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři
    [SerializeField] private float lightOffBeforeJumpscare = 0.7f; // Světlo zhasne 0.7s před jumpscarem
    [SerializeField] private float lightOnAfterDelay = 1f; // Světlo se zapne po 1s
    [SerializeField] private AudioSource lightOffAudioSource; // Audio source pro zvuk vypnutí světla
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí světla
    
    [Header("Aggressivity Modifiers")]
    [SerializeField] private float noPowerAggressivityBonus = 0.5f; // +50% při 0% energie
    [SerializeField] private float thatcherPresentPenalty = 0.3f; // -30% když je Thatcher aktivní a hráč vypne elektřinu
    
    [Header("Advanced AI System - Noc a Čas")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private bool enableRealTimeAIUpdates = true;
    [SerializeField] private float aiUpdateInterval = 1f;
    [SerializeField] private ZofeyeNightConfiguration[] aiSettings = new ZofeyeNightConfiguration[6];
    
    [Header("System References")]
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private ElectricityManagement electricitySystem;
    [SerializeField] private ThatcherAI thatcherAI; // Reference na Thatcher pro penalty systém
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pausing
    [SerializeField] private GameObject thatcherCharacter; // Reference na postavu Thatchera (GameObject)
    [SerializeField] private IanidAI ianidAI; // Reference na Ianid pro vzajemne blokovani dveri
    
    [Header("Menu Detection System")]
    [SerializeField] private GameObject[] menuPanels = new GameObject[0]; // Seznam všech menu panelů
    [SerializeField] private bool menuIsActive = false; // Indikátor zda je menu aktivní (READ ONLY)
    [SerializeField] private bool enableMenuDetection = true; // Zapnout/vypnout detekci menu
    
    [Header("Movement Effect System - FNAF 1 Style")]
    [SerializeField] private GameObject movingPanel; // Panel který se ukáže při pohybu
    [SerializeField] private AudioSource camErrorAudioSource; // Audio source pro error zvuk
    [SerializeField] private AudioClip camErrorSound; // Zvuk chyby kamery
    [SerializeField] private float movingPanelDuration = 1.5f; // Jak dlouho se panel zobrazí
    [SerializeField] private bool enableMovingEffect = true; // Zapnout/vypnout efekt
    
    [Header("Special Movement Sounds")]
    [SerializeField] private AudioSource metalDetectorAudioSource; // Audio source pro metal detektor zvuk
    [SerializeField] private AudioClip metalDetectorSound; // Zvuk metal detektoru (kamera 11 → 2)
    
    [Header("Camera System References")]
    [SerializeField] private CameraSystem cameraSystemScript; // Script klasických kamer
    [SerializeField] private CameryVentSystem ventCameraSystemScript; // Script ventilačních kamer
    [SerializeField] private GameObject camsOffButton; // Tlačítko CamsOff - aktivní pouze když hráč sleduje kamery
    [SerializeField] private GameObject camsOnButton; // Tlačítko CamsOn - aktivní když hráč NENÍ na kamerách
    
    [Header("Game Over System")]
    [SerializeField] private float gameOverDelay = 3f; // Zpoždění před návratem do menu po jumpcare
    
    [Header("Current AI Status - READ ONLY")]
    [SerializeField] private int currentEffectiveAI = 0;
    [SerializeField] private int currentNightDisplay = 1;
    [SerializeField] private int currentTimeDisplay = 0;
    [SerializeField] private string systemStatusDisplay = "Manual AI";
    [SerializeField] private string currentPositionDisplay = "Inactive";
    [SerializeField] private string menuStatusDisplay = "Menu Detection: OFF"; // Status menu detekce
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Private Variables
    
    // Movement tracking
    private int currentCameraPosition = -1; // -1 = neaktivní, 8,7,3,11,2,1 = pozice
    private float movementTimer = 0f;
    private bool isZofayeActive = false;
    private bool canZofayeMove = false; // OPRAVA: Defaultně zakázáno - povolí se až při startu hry
    
    // Defense tracking
    private bool isAtDoors = false; // True když je na kameře 1
    private bool isDoorDefenseActive = false;
    private float doorDefenseTimer = 0f;
    private float doorClosedTimer = 0f; // Čas jak dlouho jsou dveře zavřené
    private bool areDoorsClosed = false; // Sleduje stav dveří
    
    // AI system tracking
    private int lastKnownNight = 1;
    private int lastKnownHour = 0;
    private float aiUpdateTimer = 0f;
    
    // Aggressivity modifiers
    private float currentAggressivityModifier = 1f;
    
    // Possible paths from each position - OPRAVENÁ MECHANIKA
    private Dictionary<int, int[]> possibleMoves;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeZofayeSystem();
        
        // ZJEDNODUŠENO: Start() jen připraví Zofaye, pohyb se řídí automaticky v Update()
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] START() - enableZofayeAI: {enableZofayeAI}");
            
        if (enableZofayeAI)
        {
            // Vždy aktivuj a zobraz Zofaye na startovní pozici
            isZofayeActive = true;
            MoveToPosition(8);
            
            // Reset timers a stavy
            isDoorDefenseActive = false;
            isAtDoors = false;
            movementTimer = movementInterval;
            
            // Kontrola AI - pokud AI = 0, Zofaye se ZOBRAZÍ ale NEPOHYBUJE
            bool canMove = GetEffectiveAILevel() > 0;
            
            if (enableDebugLogs)
                Debug.Log($"[ZofayeAI] Zofaye aktivována na pozici 8 - AI: {GetEffectiveAILevel()}, CanMove: {canMove}");
        }
        else
        {
            isZofayeActive = false;
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Zofaye neaktivní - AI vypnutá");
        }
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye AI system initialized");
    }
    
    void OnEnable()
    {
        // Aktivace při povolení scriptu (restart noci, atd.)
        StartCoroutine(ActivateAfterFrame());
    }
    
    private System.Collections.IEnumerator ActivateAfterFrame()
    {
        // Počkej frame aby se všechno inicializovalo
        yield return new WaitForEndOfFrame();
        
        // ZJEDNODUŠENO: OnEnable jen připraví, pohyb se řídí automaticky
        if (enableZofayeAI && GetEffectiveAILevel() > 0)
        {
            isZofayeActive = true;
            
            // Ujisti se že je na startovní pozici
            if (currentCameraPosition != 8)
            {
                MoveToPosition(8);
            }
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] OnEnable: Zofaye připravena - pohyb se řídí automaticky podle ShiftTimer");
        }
    }
    
    /// <summary>
    /// Aktivuje Zofaye po krátkém zpoždění (aby se všechny systémy inicializovaly)
    /// </summary>
    /// <summary>
    /// PUBLIC: Aktivuje Zofaye když hráč vstoupí do kanceláře po middle panelu
    /// VOLAT Z NightShiftManagement po skončení middle panelu!
    /// </summary>
    public void ActivateZofayeForNight()
    {
        if (enableZofayeAI && GetEffectiveAILevel() > 0)
        {
            // Resetuj do čistého stavu a aktivuj
            ResetZofayeToStart();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Zofaye aktivovaná pro novou noc - hráč je v kanceláři!");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Zofaye NENÍ aktivovaná - AI level je 0 nebo je zakázaná");
        }
    }
    
    /// <summary>
    /// PUBLIC: Deaktivuje Zofaye (pro konec noci nebo reset)
    /// </summary>
    public void DeactivateZofaye()
    {
        isZofayeActive = false;
        canZofayeMove = false;
        HideAllZofayeModels();
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye deaktivovaná");
    }
    
    /// <summary>
    /// PUBLIC: Manuální povolení pohybu (VOLITELNÉ - pohyb se řídí automaticky podle ShiftTimer)
    /// </summary>
    public void EnableZofayeMovement()
    {
        if (enableZofayeAI && isZofayeActive && GetEffectiveAILevel() > 0)
        {
            canZofayeMove = true;
            movementTimer = movementInterval;
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Pohyb manuálně povolen (pozor: automaticky se řídí podle ShiftTimer)");
        }
    }
    
    /// <summary>
    /// PUBLIC: Manuální zakázání pohybu (VOLITELNÉ - pohyb se řídí automaticky podle ShiftTimer)
    /// </summary>
    public void DisableZofayeMovement()
    {
        canZofayeMove = false;
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Pohyb manuálně zakázán (pozor: automaticky se řídí podle ShiftTimer)");
    }
    
    void Update()
    {
        if (!enableZofayeAI) return;
        
        // CHECK FOR WARDEN - PAUSE ALL TIMERS IF WARDEN IS ATTACKING
        if (wardenAI != null && wardenAI.IsWardenActive())
            return;
        
        // KONTROLA: Detekce Thatcher smrti - reset Zofaye (pouze pokud máme reference)
        if (thatcherDeadPanel != null)
        {
            CheckThatcherDeathAndReset();
        }
        
        // OPRAVA: Automaticky řiď pohyb podle ShiftTimer A DeadPanel stavu
        bool isDeadPanelActive = (deadPanel != null && deadPanel.activeSelf);
        bool shouldMove = IsShiftTimerRunning() && GetEffectiveAILevel() > 0 && !isDeadPanelActive;
        
        // Aktualizuj canZofayeMove podle stavu timer a deadpanel
        if (shouldMove != canZofayeMove)
        {
            canZofayeMove = shouldMove;
            if (enableDebugLogs)
            {
                string reason = "";
                if (isDeadPanelActive) reason = "DeadPanel aktivní";
                else if (!IsShiftTimerRunning()) reason = "ShiftTimer zastaven";
                else if (GetEffectiveAILevel() <= 0) reason = "AI level 0";
                else reason = "Podmínky splněny";
                
                Debug.Log($"[ZofayeAI] Pohyb Zofaye {(canZofayeMove ? "POVOLEN" : "ZAKÁZÁN")} - Důvod: {reason}");
            }
        }
        
        // Update AI podle času a noci
        if (useAdvancedAISystem && enableRealTimeAIUpdates)
        {
            HandleAIUpdates();
        }
        
        // Movement system - pohyb pouze když je povolen
        if (enableMovementSystem && isZofayeActive && canZofayeMove)
        {
            HandleMovementSystem();
        }
        
        // Defense system - POUZE když není dead panel ani menu!
        if (enableDefenseSystem && isAtDoors && !isDeadPanelActive && !menuIsActive)
        {
            HandleDefenseSystem();
        }
        
        // Aggressivity modifiers
        UpdateAggressivityModifiers();
        
        // Update display values
        UpdateInspectorDisplayValues();
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Inicializuje celý Zofaye systém
    /// </summary>
    private void InitializeZofayeSystem()
    {
        // Initialize AI settings if empty
        if (aiSettings == null || aiSettings.Length != 6)
        {
            aiSettings = new ZofeyeNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                aiSettings[i] = new ZofeyeNightConfiguration
                {
                    nightName = $"Night {i + 1}",
                    hourlyAI = new ZofeyeHourlyAI()
                };
            }
        }
        
        // Initialize movement paths - OPRAVENÁ MECHANIKA
        InitializeMovementPaths();
        
        // Initialize models - všechny skryté na začátku
        HideAllZofayeModels();
        
        // Initialize audio system
        InitializeAudioSystem();
        
        // Initialize dead panel (skrytý na začátku)
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }
        
        // Auto-find system references
        FindSystemReferences();
        
        // Initialize timers
        movementTimer = movementInterval;
        aiUpdateTimer = aiUpdateInterval;
        
        // Initialize position
        currentCameraPosition = -1; // Neaktivní
        isZofayeActive = false;
        isAtDoors = false;
        
        // OPRAVA: Neautomaticky nepřepisuj uživatelská nastavení Advanced AI při startu!
        // Preset se načte jen když ho uživatel explicitně vybere přes LoadPresetConfiguration()
        
        // Initialize display values
        UpdateInspectorDisplayValues();
    }
    
    /// <summary>
    /// Inicializuje možné cesty pohybu mezi kamerami - OPRAVENÁ MECHANIKA
    /// </summary>
    private void InitializeMovementPaths()
    {
        possibleMoves = new Dictionary<int, int[]>
        {
            { -1, new int[] { 8 } },        // Start: aktivace na kameře 8
            { 8, new int[] { 7 } },         // Kamera 8 → 7
            { 7, new int[] { 3, 11 } },     // Kamera 7 → 3 nebo 11
            { 3, new int[] { 11 } },        // Kamera 3 → 11 (MUSÍ projít přes 11)
            { 11, new int[] { 2 } },        // Kamera 11 → 2 (POUZE cesta na kameru 2)
            { 2, new int[] { 101, 1 } },    // NOVÝ: Kamera 2 → 101 (sklad) NEBO 1 (dveře přímo)
            { 101, new int[] { 1 } },       // NOVÝ: Kamera 101 (sklad) → 1 (dveře)
            { 1, new int[] { } }            // Kamera 1: konečná pozice (dveře)
        };
        
        if (enableDebugLogs)
        {
            Debug.Log("[ZofayeAI] Movement paths initialized:");
            Debug.Log("Path: 8 → 7 → (3 nebo 11) → 11 → 2 → (101 nebo 1) → 1");
            Debug.Log("Note: Z kamery 2 může jít PŘÍMO na dveře (1) NEBO přes sklad (101)");
        }
    }
    
    /// <summary>
    /// Inicializuje audio systém
    /// </summary>
    private void InitializeAudioSystem()
    {
        // Vytvoř Jumpcare AudioSource pokud neexistuje
        if (jumpcareAudioSource == null)
        {
            jumpcareAudioSource = GetComponent<AudioSource>();
            if (jumpcareAudioSource == null)
            {
                jumpcareAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Nastav Jumpcare AudioSource
        if (jumpcareAudioSource != null)
        {
            jumpcareAudioSource.playOnAwake = false;
            jumpcareAudioSource.loop = false;
        }
        
        // Vytvoř Retreat AudioSource pokud neexistuje
        if (retreatAudioSource == null)
        {
            // Pokud už existuje jumpcare audio source, vytvoř druhý
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 2)
            {
                retreatAudioSource = existingSources[1]; // Použij druhý existující
            }
            else
            {
                retreatAudioSource = gameObject.AddComponent<AudioSource>(); // Vytvoř nový
            }
        }
        
        // Nastav Retreat AudioSource
        if (retreatAudioSource != null)
        {
            retreatAudioSource.playOnAwake = false;
            retreatAudioSource.loop = false;
        }
        
        // Vytvoř Camera Error AudioSource pokud neexistuje
        if (camErrorAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 3)
            {
                camErrorAudioSource = existingSources[2]; // Použij třetí existující
            }
            else
            {
                camErrorAudioSource = gameObject.AddComponent<AudioSource>(); // Vytvoř nový
            }
        }
        
        // Nastav Camera Error AudioSource
        if (camErrorAudioSource != null)
        {
            camErrorAudioSource.playOnAwake = false;
            camErrorAudioSource.loop = false;
        }
        
        // Vytvoř Metal Detector AudioSource pokud neexistuje
        if (metalDetectorAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 4)
            {
                metalDetectorAudioSource = existingSources[3]; // Použij čtvrtý existující
            }
            else
            {
                metalDetectorAudioSource = gameObject.AddComponent<AudioSource>(); // Vytvoř nový
            }
        }
        
        // Nastav Metal Detector AudioSource
        if (metalDetectorAudioSource != null)
        {
            metalDetectorAudioSource.playOnAwake = false;
            metalDetectorAudioSource.loop = false;
        }
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Audio system initialized - Jumpcare, Retreat, Camera Error and Metal Detector AudioSources ready");
    }
    
    /// <summary>
    /// Automaticky najde system references pokud nejsou přiřazené
    /// </summary>
    private void FindSystemReferences()
    {
        if (shiftTimer == null)
        {
            shiftTimer = FindFirstObjectByType<ShiftTimer>();
            if (enableDebugLogs && shiftTimer != null)
                Debug.Log("[ZofayeAI] ShiftTimer automatically found");
        }
        
        if (nightManager == null)
        {
            nightManager = FindFirstObjectByType<NightShiftManagement>();
            if (enableDebugLogs && nightManager != null)
                Debug.Log("[ZofayeAI] NightShiftManagement automatically found");
        }
        
        if (electricitySystem == null)
        {
            electricitySystem = FindFirstObjectByType<ElectricityManagement>();
            if (enableDebugLogs && electricitySystem != null)
                Debug.Log("[ZofayeAI] ElectricityManagement automatically found");
        }
        
        if (thatcherAI == null)
        {
            thatcherAI = FindFirstObjectByType<ThatcherAI>();
            if (enableDebugLogs && thatcherAI != null)
                Debug.Log("[ZofayeAI] ThatcherAI automatically found");
        }
    }
    
    #endregion
    
    #region Movement System
    
    /// <summary>
    /// Zpracovává pohybový systém Zofaye
    /// </summary>
    private void HandleMovementSystem()
    {
        movementTimer -= Time.deltaTime;
        
        if (movementTimer <= 0f)
        {
            // Reset timer
            movementTimer = movementInterval;
            
            // Pokus o pohyb
            AttemptMovement();
        }
    }
    
    /// <summary>
    /// Pokusí se pohnout Zofaye podle AI šance
    /// </summary>
    private void AttemptMovement()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        // AI 0 = neaktivní
        if (effectiveAI <= 0)
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] AI is 0 - Zofaye inactive");
            return;
        }
        
        // OPRAVA 2: Opravený výpočet šance na pohyb
        float movementChance = CalculateMovementChance(effectiveAI);
        float randomRoll = Random.Range(0f, 100f);
        
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Movement roll: {randomRoll:F1}% vs {movementChance:F1}% (AI: {effectiveAI}) - Position: {currentCameraPosition}");
        
        if (randomRoll <= movementChance)
        {
            ExecuteMovement();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[ZofayeAI] Movement failed - staying at position {currentCameraPosition}");
        }
    }
    
    /// <summary>
    /// Vypočítá šanci na pohyb podle AI levelu
    /// </summary>
    private float CalculateMovementChance(int aiLevel)
    {
        // OPRAVA 2: Správné lineární škálování AI 1-20 = 5%-100%
        float baseChance;
        
        if (aiLevel <= 0)
        {
            baseChance = 0f;
        }
        else if (aiLevel >= 20)
        {
            baseChance = 100f;
        }
        else
        {
            // Lineární škálování: AI 1 = 5%, AI 20 = 100%
            baseChance = 5f + ((aiLevel - 1) * (95f / 19f));
        }
        
        // Aplikuj aggressivity modifier
        float finalChance = baseChance * currentAggressivityModifier;
        
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] AI {aiLevel} -> Base: {baseChance:F1}%, Modifier: {currentAggressivityModifier:F2}x, Final: {finalChance:F1}%");
        
        return Mathf.Clamp(finalChance, 0f, 100f);
    }
    
    /// <summary>
    /// Provede pohyb Zofaye na další pozici
    /// </summary>
    private void ExecuteMovement()
    {
        // OPRAVA 2: Lepší handling pohybu
        if (!isZofayeActive)
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Zofaye not active - cannot move");
            return;
        }
        
        // Získej možné cesty z aktuální pozice
        if (!possibleMoves.ContainsKey(currentCameraPosition))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ZofayeAI] No possible moves from position {currentCameraPosition}");
            return;
        }
        
        int[] possiblePositions = possibleMoves[currentCameraPosition];
        
        if (possiblePositions.Length == 0)
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Zofaye reached final position - no more moves");
            return;
        }
        
        // Vyber náhodnou cestu
        int nextPosition = possiblePositions[Random.Range(0, possiblePositions.Length)];
        
        // NOVA KONTROLA: Pokud je Ianid u dveri, Zofaye nemuze jit na CAM 1 (dvere)
        if (nextPosition == 1 && ianidAI != null && ianidAI.IsAtDoors)
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] BLOCKED - Ianid is at doors, cannot move to CAM 1");
            movementTimer = 0f;
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Moving from Camera {currentCameraPosition} to Camera {nextPosition}");
        
        // Proveď pohyb
        MoveToPosition(nextPosition);
    }
    
    /// <summary>
    /// Aktivuje Zofaye na startovní pozici (kamera 8)
    /// </summary>
    private void ActivateZofaye()
    {
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Activating Zofaye on Camera 8");
            
        isZofayeActive = true;
        MoveToPosition(8);
    }
    
    /// <summary>
    /// Přesune Zofaye na specifikovanou pozici
    /// </summary>
    private void MoveToPosition(int newPosition)
    {
        // SPECIÁLNÍ ZVUK: Metal detektor při přechodu z kamery 11 na kameru 2
        if (currentCameraPosition == 11 && newPosition == 2)
        {
            PlayMetalDetectorSound();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Special transition: Camera 11 → 2 - Metal detector sound played");
        }
        
        // KONTROLA: Moving efekt POUZE pokud hráč sleduje kamery
        bool shouldShowMovingEffect = ShouldShowMovingEffect();
        bool isPlayerOnCam = IsPlayerOnCam();
        
        // MovingPanel a Error sound se přehrají POUZE pokud:
        // 1. Moving effect je povolen
        // 2. Hráč OPRAVDU sleduje kamery (CamsOff je aktivní)
        if (shouldShowMovingEffect && isPlayerOnCam)
        {
            // Ukáž moving panel a přehraj zvuk
            StartCoroutine(ShowMovingEffect());
            
            if (enableDebugLogs)
                Debug.Log($"[ZofayeAI] Moving to Camera {newPosition} - Player watching cams, showing effect");
        }
        else
        {
            if (enableDebugLogs)
            {
                string reason = !enableMovingEffect ? "Effect disabled" : "Player NOT watching cams";
                Debug.Log($"[ZofayeAI] Moving to Camera {newPosition} - No effect shown ({reason})");
            }
        }
        
        // Skryj předchozí model
        HideAllZofayeModels();
        
        // Aktivuj nový model
        GameObject targetModel = GetModelForPosition(newPosition);
        if (targetModel != null)
        {
            targetModel.SetActive(true);
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ZofayeAI] Model for Camera {newPosition} is null!");
        }
        
        // Update tracking
        currentCameraPosition = newPosition;
        isAtDoors = (newPosition == 1);
        
        // Pokud je u dveří, aktivuj defense systém
        if (isAtDoors)
        {
            StartDoorDefense();
        }
    }
    
    /// <summary>
    /// Zkontroluje, zda má ukázat moving efekt (pouze když hráč sleduje kamery)
    /// </summary>
    private bool ShouldShowMovingEffect()
    {
        if (!enableMovingEffect) return false;
        
        // JEDNODUCHÁ DETEKCE: CamsOff tlačítko je aktivní pouze když hráč sleduje kamery
        bool isPlayerOnCam = IsPlayerOnCam();
        
        if (enableDebugLogs && isPlayerOnCam)
            Debug.Log("[ZofayeAI] Player watching cameras (CamsOff active) - showing moving effect");
        
        return isPlayerOnCam;
    }
    
    /// <summary>
    /// Kontroluje zda je hráč na kamerách pomocí CamsOff a CamsOn tlačítek
    /// </summary>
    private bool IsPlayerOnCam()
    {
        // DVOJITÁ KONTROLA pro maximální bezpečnost:
        // 1. CamsOff tlačítko je aktivní (hráč sleduje kamery)
        // 2. CamsOn tlačítko NENÍ aktivní (hráč není mimo kamery)
        
        bool camsOffActive = (camsOffButton != null && camsOffButton.activeInHierarchy);
        bool camsOnActive = (camsOnButton != null && camsOnButton.activeInHierarchy);
        
        // Hráč je na kamerách POUZE pokud:
        // - CamsOff je aktivní (sleduje kamery) A ZÁROVEŇ
        // - CamsOn NENÍ aktivní (není mimo kamery)
        bool isPlayerOnCameras = camsOffActive && !camsOnActive;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ZofayeAI] Camera status check: CamsOff={camsOffActive}, CamsOn={camsOnActive}, PlayerOnCams={isPlayerOnCameras}");
        }
        
        // Pokud máme jasný výsledek z tlačítek, použij ho
        if (camsOffButton != null && camsOnButton != null)
        {
            return isPlayerOnCameras;
        }
        
        // Fallback - pokud tlačítka nejsou přiřazená, zkus camera scripty
        bool playerWatchingCameras = false;
        
        // Zkontroluj klasické kamery
        if (cameraSystemScript != null)
        {
            playerWatchingCameras = cameraSystemScript.gameObject.activeInHierarchy;
        }
        
        // Zkontroluj ventilační kamery pokud klasické nejsou aktivní
        if (!playerWatchingCameras && ventCameraSystemScript != null)
        {
            playerWatchingCameras = ventCameraSystemScript.gameObject.activeInHierarchy;
        }
        
        if (enableDebugLogs && (camsOffButton == null || camsOnButton == null))
            Debug.LogWarning("[ZofayeAI] CamsOff or CamsOn button not assigned - using fallback camera detection");
        
        return playerWatchingCameras;
    }
    
    /// <summary>
    /// Ukáže moving panel s error zvukem na 1,5 sekundy
    /// </summary>
    private System.Collections.IEnumerator ShowMovingEffect()
    {
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Showing moving effect - FNAF 1 style");
        
        // Ukáž moving panel
        if (movingPanel != null)
        {
            movingPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Moving panel shown");
        }
        else
        {
            Debug.LogWarning("[ZofayeAI] Moving panel reference is null!");
        }
        
        // Přehraj error zvuk
        PlayCamErrorSound();
        
        // NOVÁ LOGIKA: Počkej zadanou dobu, ale kontroluj stav kamer
        float elapsedTime = 0f;
        while (elapsedTime < movingPanelDuration)
        {
            // Zkontroluj zda hráč stále sleduje kamery
            if (!IsPlayerOnCam())
            {
                // Hráč opustil kamery - okamžitě skryj panel a zastav zvuk
                if (enableDebugLogs)
                    Debug.Log("[ZofayeAI] Player left cameras - hiding moving panel immediately!");
                
                // Skryj moving panel
                if (movingPanel != null)
                {
                    movingPanel.SetActive(false);
                }
                
                // Zastav error zvuk
                StopCamErrorSound();
                
                yield break; // Ukončí coroutine
            }
            
            // Počkej krátký čas a zkontroluj znovu
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }
        
        // Doba vypršela normálně - skryj moving panel
        if (movingPanel != null)
        {
            movingPanel.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Moving panel hidden after full duration");
        }
    }
    
    /// <summary>
    /// Přehraje camera error zvuk
    /// </summary>
    private void PlayCamErrorSound()
    {
        if (camErrorAudioSource != null && camErrorSound != null)
        {
            camErrorAudioSource.clip = camErrorSound;
            camErrorAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Camera error sound played");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ZofayeAI] Camera error audio source or sound clip is null!");
        }
    }
    
    /// <summary>
    /// Zastaví camera error zvuk
    /// </summary>
    private void StopCamErrorSound()
    {
        if (camErrorAudioSource != null && camErrorAudioSource.isPlaying)
        {
            camErrorAudioSource.Stop();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Camera error sound stopped");
        }
    }
    
    /// <summary>
    /// Vrátí GameObject model pro specifikovanou pozici
    /// </summary>
    private GameObject GetModelForPosition(int position)
    {
        return position switch
        {
            8 => zofayeCamera8,
            7 => zofayeCamera7,
            3 => zofayeCamera3,
            11 => zofayeCamera11,
            2 => zofayeCamera2,
            101 => zofayeCamera1Storage,  // NOVÝ: CAM 01 (sklad)
            1 => zofayeCamera1,            // CAM 01 (dveře)
            _ => null
        };
    }
    
    /// <summary>
    /// Skryje všechny Zofaye modely
    /// </summary>
    private void HideAllZofayeModels()
    {
        if (zofayeCamera8 != null) zofayeCamera8.SetActive(false);
        if (zofayeCamera7 != null) zofayeCamera7.SetActive(false);
        if (zofayeCamera3 != null) zofayeCamera3.SetActive(false);
        if (zofayeCamera11 != null) zofayeCamera11.SetActive(false);
        if (zofayeCamera2 != null) zofayeCamera2.SetActive(false);
        if (zofayeCamera1Storage != null) zofayeCamera1Storage.SetActive(false); // NOVÝ: Sklad
        if (zofayeCamera1 != null) zofayeCamera1.SetActive(false);
    }
    
    #endregion
    
    #region Defense System
    
    /// <summary>
    /// Spustí systém obrany když je Zofaye u dveří
    /// </summary>
    private void StartDoorDefense()
    {
        isDoorDefenseActive = true;
        doorDefenseTimer = doorResponseTime;
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Zofaye at doors! Player has {doorResponseTime} seconds to close right door");
    }
    
    /// <summary>
    /// Zpracovává systém obrany u dveří
    /// </summary>
    private void HandleDefenseSystem()
    {
        if (!isDoorDefenseActive) return;
        
        // Kontrola stavu dveří
        bool currentDoorState = IsRightDoorClosed();
        
        // NOVÁ LOGIKA: Zkontroluj zda je Thatcher aktivní
        bool isThatcherActive = IsThatcherCurrentlyActive();
        
        // NOVÁ LOGIKA: Zkontroluj zda jsou dveře zavřené a běží Door Closed timer
        bool areDoorsClosedAndCounting = currentDoorState && areDoorsClosed;
        
        // Defense timer se snižuje POUZE pokud:
        // 1. Thatcher NENÍ aktivní A ZÁROVEŇ
        // 2. Dveře NEJSOU zavřené (nebo ještě nezačal Door Closed timer)
        bool shouldCountDownDefense = !isThatcherActive && !areDoorsClosedAndCounting;
        
        if (shouldCountDownDefense)
        {
            doorDefenseTimer -= Time.deltaTime;
            
            if (enableDebugLogs && doorDefenseTimer % 1f < Time.deltaTime) // Log každou sekundu
                Debug.Log($"[ZofayeAI] Defense timer: {doorDefenseTimer:F1}s remaining");
        }
        else
        {
            // Defense timer se zastavil
            string reason = "";
            if (isThatcherActive) reason = "Thatcher active";
            else if (areDoorsClosedAndCounting) reason = "Doors closed";
            
            if (enableDebugLogs && doorDefenseTimer % 1f < Time.deltaTime) // Log každou sekundu
                Debug.Log($"[ZofayeAI] Defense timer PAUSED - {reason}! Timer frozen at: {doorDefenseTimer:F1}s");
        }
        
        if (currentDoorState)
        {
            // Dveře jsou zavřené
            if (!areDoorsClosed)
            {
                // Dveře se právě zavřely - začni počítat čas
                areDoorsClosed = true;
                doorClosedTimer = 0f;
                
                if (enableDebugLogs)
                    Debug.Log($"[ZofayeAI] Doors closed! Need to stay closed for {doorClosedRequiredTime} seconds");
            }
            else
            {
                // Dveře jsou stále zavřené - počítej čas
                doorClosedTimer += Time.deltaTime;
                
                if (enableDebugLogs && doorClosedTimer % 1f < Time.deltaTime) // Log každou sekundu
                    Debug.Log($"[ZofayeAI] Doors closed for {doorClosedTimer:F1}s / {doorClosedRequiredTime}s");
                
                // Zkontroluj zda jsou dveře zavřené dostatečně dlouho
                if (doorClosedTimer >= doorClosedRequiredTime)
                {
                    // Úspěšná obrana - dveře byly zavřené dostatečně dlouho
                    HandleSuccessfulDefense();
                    return;
                }
            }
        }
        else
        {
            // Dveře jsou otevřené - resetuj timer
            if (areDoorsClosed)
            {
                areDoorsClosed = false;
                doorClosedTimer = 0f;
                
                if (enableDebugLogs)
                    Debug.Log("[ZofayeAI] Doors opened - defense timer reset!");
            }
        }
        
        // Čas vypršel a dveře nejsou zavřené dostatečně dlouho - jumpcare
        if (doorDefenseTimer <= 0f)
        {
            TriggerZofayeJumpcare();
        }
    }
    
    /// <summary>
    /// Kontroluje zda je Thatcher momentálně aktivní pomocí GameObject postavy
    /// </summary>
    private bool IsThatcherCurrentlyActive()
    {
        // PŘÍMÁ KONTROLA: Thatcher postava GameObject je aktivní
        if (thatcherCharacter != null)
        {
            bool isActive = thatcherCharacter.activeInHierarchy;
            
            if (enableDebugLogs && isActive)
                Debug.Log($"[ZofayeAI] Thatcher character active: {isActive} - Defense timer paused");
            
            return isActive;
        }
        
        // Fallback - pokud GameObject není přiřazený, zkus ThatcherAI script
        if (thatcherAI == null) return false;
        
        try
        {
            // Pokus se získat IsThatcherActive property z ThatcherAI
            var thatcherActiveProperty = thatcherAI.GetType().GetProperty("IsThatcherActive");
            if (thatcherActiveProperty != null)
            {
                bool isActive = (bool)thatcherActiveProperty.GetValue(thatcherAI);
                return isActive;
            }
            
            // Pokud property neexistuje, zkus jiné názvy
            var activeField = thatcherAI.GetType().GetField("isThatcherActive");
            if (activeField != null)
            {
                bool isActive = (bool)activeField.GetValue(thatcherAI);
                return isActive;
            }
            
            // Fallback - zkontroluj zda je ThatcherAI GameObject aktivní
            return thatcherAI.gameObject.activeInHierarchy;
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ZofayeAI] Error checking Thatcher status: {ex.Message}");
            
            // Fallback při chybě
            return false;
        }
    }
    
    /// <summary>
    /// Kontroluje, zda jsou pravé dveře zavřené
    /// </summary>
    private bool IsRightDoorClosed()
    {
        // Kontrola přes door script
        if (rightDoorScript != null)
        {
            return !rightDoorScript.IsOpen; // Dveře jsou zavřené když IsOpen = false
        }
        
        // Fallback kontrola přes tlačítko
        if (rightDoorButton != null)
        {
            return rightDoorButton.gameObject.activeInHierarchy; // Upravte podle vašeho systému
        }
        
        if (enableDebugLogs)
            Debug.LogWarning("[ZofayeAI] No right door references found!");
        
        return false;
    }
    
    /// <summary>
    /// Zpracovává úspěšnou obranu proti Zofaye
    /// </summary>
    private void HandleSuccessfulDefense()
    {
        isDoorDefenseActive = false;
        isAtDoors = false;
        
        // Přehraj zvuk odchodu
        PlayRetreatSound();
        
        // Vrať Zofaye na kamerau 8 nebo 3 (náhodně)
        int returnPosition = Random.Range(0, 2) == 0 ? 8 : 3;
        MoveToPosition(returnPosition);
        
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Successful defense! Zofaye returned to Camera {returnPosition}");
    }
    
    /// <summary>
    /// Spustí Zofaye jumpcare s dead panel a zvukem
    /// </summary>
    private void TriggerZofayeJumpcare()
    {
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye jumpcare triggered!");
        
        // NOVÁ LOGIKA: Nezakázuj Zofaye, jen zastav pohyb a přesuň na start
        isDoorDefenseActive = false;
        isAtDoors = false;
        canZofayeMove = false; // Zakáži pohyb až do nové noci
        
        // Přesuň Zofaye zpátky na startovní pozici (kamera 8)
        MoveToPosition(8);
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye movement disabled and moved back to Camera 8");
        
        // Reset Thatcher po smrti od Zofaye
        ResetThatcherAfterZofayeDeath();
        
        // Spusť coroutinu s vypnutím světla před jumpscarem
        StartCoroutine(TriggerZofayeJumpcareWithLights());
        
        // POZOR: Už neresetuj Zofaye - jen je na pozici 8 s vypnutým pohybem
    }
    
    /// <summary>
    /// Zpracovává ukončení hry po jumpcare
    /// </summary>
    private System.Collections.IEnumerator HandleGameOver()
    {
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Starting game over sequence - waiting {gameOverDelay} seconds");
        
        // Počkej zadanou dobu (pro zobrazení dead panelu a zvuku)
        yield return new WaitForSeconds(gameOverDelay);
        
        // Zastav video před skrytím panelu
        StopDeadPanelVideo();
        
        // OPRAVA: Skryj dead panel před návratem do menu (stejně jako ThatcherAI)
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Dead panel hidden before returning to menu");
        }
        
        // Pokus se ukončit hru přes NightShiftManagement
        if (nightManager != null)
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Ending game via NightShiftManagement");
            
            // Použij metodu pro návrat do menu
            nightManager.ForceReturnToMenu();
        }
        else
        {
            // Fallback - pokus se najít NightShiftManagement
            NightShiftManagement foundNightManager = FindFirstObjectByType<NightShiftManagement>();
            if (foundNightManager != null)
            {
                if (enableDebugLogs)
                    Debug.Log("[ZofayeAI] Found NightShiftManagement - ending game");
                
                foundNightManager.ForceReturnToMenu();
            }
            else
            {
                // Pokud NightShiftManagement není k dispozici, zkus jiné metody
                if (enableDebugLogs)
                    Debug.LogWarning("[ZofayeAI] NightShiftManagement not found - trying alternative game over methods");
                
                // Pokus se najít a deaktivovat herní systémy
                TryAlternativeGameOver();
            }
        }
    }
    
    /// <summary>
    /// Alternativní metoda pro ukončení hry pokud NightShiftManagement není k dispozici
    /// </summary>
    private void TryAlternativeGameOver()
    {
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Attempting alternative game over methods");
        
        // Pokus se najít a zastavit ShiftTimer
        if (shiftTimer != null)
        {
            shiftTimer.Won = true; // Zastav timer
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] ShiftTimer stopped");
        }
        
        // Skryj dead panel po chvíli (fallback)
        StartCoroutine(HideDeadPanelAfterDelay(2f));
        
        // Jako poslední možnost - restartuj scénu
        StartCoroutine(RestartSceneAfterDelay(5f));
    }
    
    /// <summary>
    /// Skryje dead panel po zadaném zpoždění
    /// </summary>
    private System.Collections.IEnumerator HideDeadPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Dead panel hidden");
        }
    }
    
    /// <summary>
    /// Restartuje scénu jako poslední možnost
    /// </summary>
    private System.Collections.IEnumerator RestartSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Restarting scene as fallback");
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
    
    /// <summary>
    /// Přehraje jumpcare zvuk
    /// </summary>
    private void PlayJumpcareSound()
    {
        if (jumpcareAudioSource != null && jumpcareSound != null)
        {
            jumpcareAudioSource.clip = jumpcareSound;
            jumpcareAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Jumpcare sound played");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ZofayeAI] Jumpcare audio source or sound clip is null!");
        }
    }
    
    /// <summary>
    /// Přehraje zvuk odchodu po úspěšné obraně
    /// </summary>
    private void PlayRetreatSound()
    {
        if (retreatAudioSource != null && retreatSound != null)
        {
            retreatAudioSource.clip = retreatSound;
            retreatAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Retreat sound played");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ZofayeAI] Retreat audio source or retreat sound clip is null!");
        }
    }
    
    /// <summary>
    /// Přehraje metal detektor zvuk při přechodu z kamery 11 na 2
    /// </summary>
    private void PlayMetalDetectorSound()
    {
        // KONTROLA: Nepřehrávej metal detektor zvuk pokud je vypnutá elektřina (ať už vyčerpáním nebo manuálně)
        if (electricitySystem != null && !electricitySystem.IsPowerOn())
        {
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Metal detector sound blocked - Power is OFF (manually or depleted)");
            return;
        }
        
        if (metalDetectorAudioSource != null && metalDetectorSound != null)
        {
            metalDetectorAudioSource.clip = metalDetectorSound;
            metalDetectorAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Metal detector sound played (Camera 11 → 2)");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ZofayeAI] Metal detector audio source or sound clip is null!");
        }
    }
    
    #endregion
    
    #region Aggressivity System
    
    /// <summary>
    /// Aktualizuje aggressivity modifiers podle herních podmínek
    /// </summary>
    private void UpdateAggressivityModifiers()
    {
        float newModifier = 1f;
        
        // +50% aggressivity při 0% energie
        if (electricitySystem != null && electricitySystem.GetCurrentPowerPercentage() <= 0f)
        {
            newModifier += noPowerAggressivityBonus;
            
            if (enableDebugLogs && Mathf.Abs(currentAggressivityModifier - newModifier) > 0.01f)
                Debug.Log("[ZofayeAI] No power - aggressivity increased by 50%");
        }
        
        // -30% aggressivity když je Thatcher aktivní a hráč vypne elektřinu sám
        if (IsThatcherPenaltyActive())
        {
            newModifier -= thatcherPresentPenalty;
            
            if (enableDebugLogs && Mathf.Abs(currentAggressivityModifier - newModifier) > 0.01f)
                Debug.Log("[ZofayeAI] Thatcher penalty - aggressivity decreased by 30%");
        }
        
        currentAggressivityModifier = Mathf.Clamp(newModifier, 0.1f, 2f);
    }
    
    /// <summary>
    /// Kontroluje, zda je aktivní Thatcher penalty
    /// </summary>
    private bool IsThatcherPenaltyActive()
    {
        // Podmínky: Thatcher aktivní + Zofaye u dveří + hráč vypnul elektřinu sám
        if (thatcherAI == null || !isAtDoors) return false;
        
        // Zkontroluj zda má ThatcherAI property IsThatcherActive
        bool thatcherActive = false;
        try
        {
            // Pokus se získat IsThatcherActive property z ThatcherAI
            var thatcherActiveProperty = thatcherAI.GetType().GetProperty("IsThatcherActive");
            if (thatcherActiveProperty != null)
            {
                thatcherActive = (bool)thatcherActiveProperty.GetValue(thatcherAI);
            }
        }
        catch
        {
            // Fallback - předpokládej že Thatcher není aktivní
            thatcherActive = false;
        }
        
        bool electricityOff = electricitySystem != null && !electricitySystem.IsPowerOn();
        
        return thatcherActive && electricityOff;
    }
    
    #endregion
    
    #region Advanced AI System
    
    /// <summary>
    /// Zpracovává AI aktualizace podle času a noci
    /// </summary>
    private void HandleAIUpdates()
    {
        aiUpdateTimer -= Time.deltaTime;
        
        if (aiUpdateTimer <= 0f)
        {
            aiUpdateTimer = aiUpdateInterval;
            CheckForAIUpdates();
        }
    }
    
    /// <summary>
    /// Kontroluje a aplikuje AI změny
    /// </summary>
    private void CheckForAIUpdates()
    {
        int currentNight = GetCurrentNight();
        int currentHour = GetCurrentHour();
        
        if (currentNight != lastKnownNight || currentHour != lastKnownHour)
        {
            lastKnownNight = currentNight;
            lastKnownHour = currentHour;
            
            if (enableDebugLogs)
                Debug.Log($"[ZofayeAI] Time changed - Night: {currentNight}, Hour: {currentHour}");
        }
    }
    
    /// <summary>
    /// Kontroluje zda je hráč v main menu (jakýkoliv panel ze seznamu menuPanels je aktivní)
    /// </summary>
    private void CheckMenuStatus()
    {
        if (!enableMenuDetection)
        {
            menuIsActive = false;
            menuStatusDisplay = "Menu Detection: OFF";
            return;
        }
        
        bool previousMenuState = menuIsActive;
        menuIsActive = false;
        string activePanelName = "";
        
        // Projdi všechny panely v seznamu
        if (menuPanels != null)
        {
            for (int i = 0; i < menuPanels.Length; i++)
            {
                if (menuPanels[i] != null && menuPanels[i].activeSelf)
                {
                    menuIsActive = true;
                    activePanelName = menuPanels[i].name;
                    break; // Stačí najít první aktivní panel
                }
            }
        }
        
        // Aktualizuj display status
        if (menuIsActive)
        {
            menuStatusDisplay = $"MENU ACTIVE: {activePanelName}";
        }
        else
        {
            menuStatusDisplay = "Menu Detection: ON - Game Active";
        }
        
        // Log pouze při změně stavu
        if (previousMenuState != menuIsActive && enableDebugLogs)
        {
            Debug.Log($"[ZofayeAI] MenuIsActive: {menuIsActive} - Aktivní panel: {activePanelName}");
        }
        
        // DODATEK: Pokud MenuIsActive = false, aktivuj Zofaye na pozici 8
        if (!menuIsActive && previousMenuState != menuIsActive)
        {
            // Menu se právě deaktivovalo - aktivuj Zofaye
            if (enableZofayeAI && GetEffectiveAILevel() > 0)
            {
                isZofayeActive = true;
                MoveToPosition(8); // Nastav na cam8 (pozice 1)
                
                if (enableDebugLogs)
                    Debug.Log("[ZofayeAI] Menu deaktivováno - Zofaye aktivována na pozici 8 (cam8)");
            }
        }
        else if (menuIsActive && previousMenuState != menuIsActive)
        {
            // Menu se právě aktivovalo - deaktivuj Zofaye
            isZofayeActive = false;
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Menu aktivováno - Zofaye deaktivována");
        }
    }
    
    /// <summary>
    /// Vrátí efektivní AI level podle nastaveného systému
    /// </summary>
    public int GetEffectiveAILevel()
    {
        // KONTROLA MENU - pokud je menu aktivní, AI = 0
        CheckMenuStatus();
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return 0;
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("ZOFEYE");
            if (customAI >= 0)
                return customAI;
        }
        
        if (!useAdvancedAISystem)
        {
            // Odstraněn spam log - už se neloguje každý frame
            return manualAILevel;
        }
        
        int night = GetCurrentNight();
        int hour = GetCurrentHour();
        
        if (night >= 1 && night <= 6)
        {
            return aiSettings[night - 1].hourlyAI.GetAIForHour(hour);
        }
        
        return manualAILevel;
    }
    
    /// <summary>
    /// Získá aktuální noc ze hry
    /// </summary>
    private int GetCurrentNight()
    {
        if (nightManager != null)
        {
            return nightManager.GetCurrentNight();
        }
        return 1;
    }
    
    /// <summary>
    /// Získá aktuální hodinu ze hry
    /// </summary>
    private int GetCurrentHour()
    {
        if (shiftTimer != null)
        {
            return shiftTimer.GetCurrentHour();
        }
        return 0;
    }
    
    /// <summary>
    /// Zkontroluje, zda běží ShiftTimer (= hra je aktivní)
    /// </summary>
    private bool IsShiftTimerRunning()
    {
        if (shiftTimer == null) return false;
        
        // Kontrola zda timer běží - použij pouze dostupné public properties
        // Timer běží když není Won a pravděpodobně má nějaký aktivní stav
        bool timerActive = !shiftTimer.Won; // Základní kontrola - timer nebyl zastaven
        
        return timerActive;
    }
    
    /// <summary>
    /// Načte přednastavená konfigurace AI
    /// </summary>
    public void LoadPresetConfiguration(string presetName)
    {
        switch (presetName.ToLower())
        {
            case "easy":
                LoadEasyPreset();
                break;
            case "normal":
                LoadNormalPreset();
                break;
            case "hard":
                LoadHardPreset();
                break;
            case "nightmare":
                LoadNightmarePreset();
                break;
            default:
                if (enableDebugLogs)
                    Debug.LogWarning($"[ZofayeAI] Unknown preset: {presetName}");
                break;
        }
    }
    
    private void LoadEasyPreset()
    {
        int[][] easySettings = {
            new int[] {0,0,1,1,2,2}, // Night 1
            new int[] {0,1,1,2,2,3}, // Night 2
            new int[] {1,1,2,2,3,3}, // Night 3
            new int[] {1,2,2,3,3,4}, // Night 4
            new int[] {2,2,3,3,4,5}, // Night 5
            new int[] {2,3,3,4,5,6}  // Night 6
        };
        
        ApplyPresetSettings(easySettings);
        if (enableDebugLogs) Debug.Log("[ZofayeAI] Easy preset loaded");
    }
    
    private void LoadNormalPreset()
    {
        int[][] normalSettings = {
            new int[] {0,1,2,3,4,5}, // Night 1
            new int[] {1,2,3,4,5,6}, // Night 2
            new int[] {2,3,4,5,6,7}, // Night 3
            new int[] {3,4,5,6,7,8}, // Night 4
            new int[] {4,5,6,7,8,10}, // Night 5
            new int[] {5,6,7,8,10,12} // Night 6
        };
        
        ApplyPresetSettings(normalSettings);
        if (enableDebugLogs) Debug.Log("[ZofayeAI] Normal preset loaded");
    }
    
    private void LoadHardPreset()
    {
        int[][] hardSettings = {
            new int[] {2,3,4,5,6,7}, // Night 1
            new int[] {3,4,5,6,7,8}, // Night 2
            new int[] {4,5,6,7,8,10}, // Night 3
            new int[] {5,6,7,8,10,12}, // Night 4
            new int[] {6,7,8,10,12,15}, // Night 5
            new int[] {8,10,12,15,18,20} // Night 6
        };
        
        ApplyPresetSettings(hardSettings);
        if (enableDebugLogs) Debug.Log("[ZofayeAI] Hard preset loaded");
    }
    
    private void LoadNightmarePreset()
    {
        int[][] nightmareSettings = {
            new int[] {5,6,7,8,9,10}, // Night 1
            new int[] {6,7,8,9,10,12}, // Night 2
            new int[] {7,8,9,10,12,15}, // Night 3
            new int[] {8,9,10,12,15,17}, // Night 4
            new int[] {10,12,15,17,18,19}, // Night 5
            new int[] {12,15,17,18,19,20} // Night 6
        };
        
        ApplyPresetSettings(nightmareSettings);
        if (enableDebugLogs) Debug.Log("[ZofayeAI] Nightmare preset loaded");
    }
    
    private void ApplyPresetSettings(int[][] settings)
    {
        for (int night = 0; night < 6 && night < settings.Length; night++)
        {
            for (int hour = 0; hour < 6 && hour < settings[night].Length; hour++)
            {
                aiSettings[night].hourlyAI.SetAIForHour(hour, settings[night][hour]);
            }
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Resetuje Zofaye do neaktivního stavu
    /// </summary>
    public void ResetZofaye()
    {
        // KRITICKÉ: Zastav všechny coroutiny (včetně kill timeru u dveří)
        StopAllCoroutines();
        
        HideAllZofayeModels();
        currentCameraPosition = -1;
        isZofayeActive = false;
        isAtDoors = false;
        isDoorDefenseActive = false;
        movementTimer = movementInterval;
        doorDefenseTimer = 0f; // RESET kill timeru!
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        
        // Skryj dead panel
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye reset to inactive state - all coroutines stopped");
    }
    
    /// <summary>
    /// Aktivuje Zofaye na začátku noci - postaví na kameru 8
    /// </summary>
    public void ActivateForNewNight()
    {
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Activating Zofaye for new night");
            
        // Aktivuj Zofaye a nastav na startovní pozici
        isZofayeActive = true;
        MoveToPosition(8);
        
        // Reset timers
        movementTimer = movementInterval;
        doorClosedTimer = 0f;
        isDoorDefenseActive = false;
        isAtDoors = false;
        areDoorsClosed = false;
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye activated and moved to Camera 8");
    }
    
    /// <summary>
    /// Resetuje Zofaye do startovního stavu pro novou hru
    /// </summary>
    public void ResetZofayeToStart()
    {
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] ===== RESETTING ZOFAYE TO START =====");
            
        // Kompletní reset všech stavů
        HideAllZofayeModels();
        
        // Aktivuj Zofaye a nastav na pozici 8 (začátek cesty)
        isZofayeActive = true;
        MoveToPosition(8);
        isAtDoors = false;
        isDoorDefenseActive = false;
        movementTimer = movementInterval;
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        currentAggressivityModifier = 1f;
        
        // Reset AI tracking
        lastKnownNight = 1;
        lastKnownHour = 0;
        aiUpdateTimer = aiUpdateInterval;
        
        // Ujisti se, že dead panel je skrytý
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }
        
        // Zastavení všech audio zdrojů
        if (jumpcareAudioSource != null && jumpcareAudioSource.isPlaying)
        {
            jumpcareAudioSource.Stop();
        }
        
        if (retreatAudioSource != null && retreatAudioSource.isPlaying)
        {
            retreatAudioSource.Stop();
        }
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye completely reset to start state");
    }
    
    /// <summary>
    /// Aktualizuje display hodnoty v inspektoru
    /// </summary>
    private void UpdateInspectorDisplayValues()
    {
        currentEffectiveAI = GetEffectiveAILevel();
        currentNightDisplay = GetCurrentNight();
        currentTimeDisplay = GetCurrentHour();
        
        if (useAdvancedAISystem)
        {
            systemStatusDisplay = enableRealTimeAIUpdates ? "Advanced AI (Real-time)" : "Advanced AI (Static)";
        }
        else
        {
            systemStatusDisplay = "Manual AI";
        }
        
        if (!isZofayeActive)
        {
            currentPositionDisplay = "Inactive - Not Activated";
        }
        else if (!canZofayeMove)
        {
            currentPositionDisplay = $"Camera {currentCameraPosition} - Movement DISABLED (waiting for new night)";
        }
        else if (isAtDoors)
        {
            if (areDoorsClosed)
            {
                currentPositionDisplay = $"Camera {currentCameraPosition} (AT DOORS! - Doors closed: {doorClosedTimer:F1}s/{doorClosedRequiredTime}s)";
            }
            else
            {
                currentPositionDisplay = $"Camera {currentCameraPosition} (AT DOORS!)";
            }
        }
        else
        {
            currentPositionDisplay = $"Camera {currentCameraPosition}";
        }
    }
    
    /// <summary>
    /// Vrátí debug informace o aktuálním stavu
    /// </summary>
    public string GetZofayeDebugInfo()
    {
        return $"Zofaye AI Debug:\n" +
               $"Active: {isZofayeActive}\n" +
               $"Position: {(currentCameraPosition == -1 ? "Inactive" : $"Camera {currentCameraPosition}")}\n" +
               $"At Doors: {isAtDoors}\n" +
               $"Defense Active: {isDoorDefenseActive}\n" +
               $"Defense Timer: {doorDefenseTimer:F1}s\n" +
               $"Movement Timer: {movementTimer:F1}s\n" +
               $"Effective AI: {GetEffectiveAILevel()}\n" +
               $"Aggressivity Modifier: {currentAggressivityModifier:F2}x";
    }
    
    #endregion
    
    #region Character Interaction System
    
    /// <summary>
    /// Kontroluje zda Thatcher zabil hráče a resetuje Zofaye
    /// </summary>
    private void CheckThatcherDeathAndReset()
    {
        // Bezpečnostní null check a aktivní check
        if (thatcherDeadPanel == null || !this.enabled || !this.gameObject.activeInHierarchy) 
            return;
            
        // Detekce Thatcher smrti pomocí jeho dead panelu
        if (thatcherDeadPanel.activeSelf)
        {
            // Thatcher zabil hráče - resetuj Zofaye
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Thatcher killed player - resetting Zofaye to position 8");
            
            // 1. Vrať Zofaye na pozici 8 (bez ohledu na současnou pozici)
            ResetZofayeAfterThatcherDeath();
        }
    }
    
    /// <summary>
    /// Resetuje Zofaye po smrti od Thatchera
    /// </summary>
    private void ResetZofayeAfterThatcherDeath()
    {
        // 1. Vrať na pozici 8
        currentCameraPosition = 8;
        isZofayeActive = true; // Zůstane aktivní ale na startovní pozici
        isAtDoors = false;
        
        // Zobraz model na pozici 8, skryj ostatní
        HideAllZofayeModels();
        if (zofayeCamera8 != null)
        {
            zofayeCamera8.SetActive(true);
        }
        
        // 2. Reset Defense timers na 0
        isDoorDefenseActive = false;
        doorDefenseTimer = 0f;
        doorClosedTimer = 0f;
        areDoorsClosed = false;
        
        // Reset movement timer
        movementTimer = movementInterval;
        
        if (enableDebugLogs)
            Debug.Log("[ZofayeAI] Zofaye reset: Position 8, Defense timer reset to 0");
    }
    
    /// <summary>
    /// Resetuje Thatcher po smrti od Zofaye
    /// </summary>
    private void ResetThatcherAfterZofayeDeath()
    {
        if (thatcherAI != null)
        {
            try
            {
                // Pokus se zavolat reset metodu na ThatcherAI
                var resetMethod = thatcherAI.GetType().GetMethod("ResetAfterZofayeDeath");
                if (resetMethod != null)
                {
                    resetMethod.Invoke(thatcherAI, null);
                    
                    if (enableDebugLogs)
                        Debug.Log("[ZofayeAI] Thatcher reset called via reflection");
                }
                else
                {
                    // Fallback - pokus se deaktivovat Thatcher GameObject
                    if (thatcherCharacter != null)
                    {
                        thatcherCharacter.SetActive(false);
                        
                        if (enableDebugLogs)
                            Debug.Log("[ZofayeAI] Thatcher character deactivated as fallback");
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[ZofayeAI] Error resetting Thatcher: {ex.Message}");
            }
        }
    }
    
    #endregion
    
    #region Video Player Management
    
    /// <summary>
    /// Připraví video pro přehrání (načte první frame)
    /// </summary>
    private void PrepareDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ZofayeAI] Dead panel VideoPlayer není přiřazený!");
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
                Debug.Log("[ZofayeAI] Dead panel video připraveno (frame 0)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ZofayeAI] Chyba při přípravě videa: {e.Message}");
        }
    }
    
    /// <summary>
    /// Spustí přehrávání videa
    /// </summary>
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null) return;
        
        try
        {
            // Spusť přehrávání
            deadPanelVideoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Dead panel video spuštěno");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ZofayeAI] Chyba při spuštění videa: {e.Message}");
        }
    }
    
    /// <summary>
    /// Zastaví video a resetuje ho na začátek
    /// </summary>
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null) return;
        
        try
        {
            // Zastav video
            deadPanelVideoPlayer.Stop();
            
            // Reset na začátek
            deadPanelVideoPlayer.frame = 0;
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Dead panel video zastaveno a resetováno");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ZofayeAI] Chyba při zastavení videa: {e.Message}");
        }
    }
    
    /// <summary>
    /// Coroutina pro zobrazení dead panelu s delayem na načtení videa
    /// </summary>
    private System.Collections.IEnumerator ShowDeadPanelWithVideoDelay()
    {
        // Připrav video (aktivuje VideoPlayer, zavolá Prepare(), nastaví frame 0)
        PrepareDeadPanelVideo();
        
        // Počkej 0.1 sekundy - dá Unity čas načíst první frame videa
        yield return new WaitForSeconds(0.1f);
        
        // Teď zobraz dead panel - video je už připravené
        if (deadPanel != null)
        {
            deadPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Dead panel shown (po 0.1s video loading delay)");
            
            // Spusť video (už je načtené)
            StartDeadPanelVideo();
        }
        else
        {
            Debug.LogError("[ZofayeAI] Dead panel reference is null! Assign it in inspector.");
        }
    }
    
    /// <summary>
    /// Coroutina pro jumpcare s vypnutím světla před zobrazením
    /// </summary>
    private System.Collections.IEnumerator TriggerZofayeJumpcareWithLights()
    {
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
    /// Zapíná/vypíná světla v kanceláři
    /// </summary>
    private System.Collections.IEnumerator FlickerLights(bool turnOn)
    {
        // Při vypínání přehraj zvuk
        if (!turnOn && lightOffSound != null && lightOffAudioSource != null)
        {
            lightOffAudioSource.PlayOneShot(lightOffSound);
            
            if (enableDebugLogs)
                Debug.Log("[ZofayeAI] Playing light-off sound");
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
                Debug.Log($"[ZofayeAI] Office lights: {(turnOn ? "ON" : "OFF")}");
        }
        
        // Čekej podle timingu (0.7s při vypnutí, 0s při zapnutí)
        yield return new WaitForSeconds(turnOn ? 0f : lightOffBeforeJumpscare);
    }
    
    #endregion
    
    #region Public API
    
    public bool IsZofayeActive => isZofayeActive;
    public bool IsAtDoors => isAtDoors;
    public int CurrentPosition => currentCameraPosition;
    public int GetCurrentAILevel() => GetEffectiveAILevel();
    
    public void SetAILevel(int newLevel)
    {
        manualAILevel = Mathf.Clamp(newLevel, 0, 20);
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Manual AI level set to: {manualAILevel}");
    }
    
    public void ToggleAdvancedAISystem()
    {
        useAdvancedAISystem = !useAdvancedAISystem;
        UpdateInspectorDisplayValues();
        
        if (enableDebugLogs)
            Debug.Log($"[ZofayeAI] Advanced AI System: {(useAdvancedAISystem ? "Enabled" : "Disabled")}");
    }
    
    #endregion
}