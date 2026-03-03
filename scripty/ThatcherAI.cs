using UnityEngine;
using UnityEngine.UI;

// Serializable třída pro nastavení AI pro jednu hodinu
[System.Serializable]
public class HourlyAISettings
{
    [Header("Hodinové AI nastavení")]
    [SerializeField] [Range(0, 20)] public int hour12AM = 0; // 12 AM (půlnoc)
    [SerializeField] [Range(0, 20)] public int hour1AM = 0;  // 1 AM
    [SerializeField] [Range(0, 20)] public int hour2AM = 0;  // 2 AM
    [SerializeField] [Range(0, 20)] public int hour3AM = 0;  // 3 AM
    [SerializeField] [Range(0, 20)] public int hour4AM = 0;  // 4 AM
    [SerializeField] [Range(0, 20)] public int hour5AM = 0;  // 5 AM
    
    /// <summary>
    /// Získá AI level pro zadanou hodinu (0-5)
    /// </summary>
    public int GetAIForHour(int hour)
    {
        switch (hour)
        {
            case 0: return hour12AM; // 12 AM = hodina 0
            case 1: return hour1AM;
            case 2: return hour2AM;
            case 3: return hour3AM;
            case 4: return hour4AM;
            case 5: return hour5AM;
            default: return 0; // Fallback pro neplatné hodiny
        }
    }
}

// Serializable třída pro nastavení AI pro všechny noci
[System.Serializable]
public class NightlyAISettings
{
    [Header("AI nastavení podle nocí")]
    [SerializeField] public HourlyAISettings night1 = new HourlyAISettings();
    [SerializeField] public HourlyAISettings night2 = new HourlyAISettings();
    [SerializeField] public HourlyAISettings night3 = new HourlyAISettings();
    [SerializeField] public HourlyAISettings night4 = new HourlyAISettings();
    [SerializeField] public HourlyAISettings night5 = new HourlyAISettings();
    [SerializeField] public HourlyAISettings night6 = new HourlyAISettings();
    
    /// <summary>
    /// Získá HourlyAISettings pro zadanou noc (1-6)
    /// </summary>
    public HourlyAISettings GetSettingsForNight(int night)
    {
        switch (night)
        {
            case 1: return night1;
            case 2: return night2;
            case 3: return night3;
            case 4: return night4;
            case 5: return night5;
            case 6: return night6;
            default: return night1; // Fallback na noc 1 pro neplatné noci
        }
    }
    
    /// <summary>
    /// Získá AI level pro zadanou noc a hodinu
    /// </summary>
    public int GetAILevel(int night, int hour)
    {
        HourlyAISettings nightSettings = GetSettingsForNight(night);
        return nightSettings.GetAIForHour(hour);
    }
}

public class ThatcherAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("Thatcher Settings")]
    [SerializeField] private GameObject thatcherCube; // Cube object pro Thatchera
    [SerializeField] [Range(0, 20)] private int aiLevel = 10; // AI Level (0 = 0%, 20 = 50%) - POUZE PRO MANUÁLNÍ OVERRIDE
    [Space]
    [Header("Appearance Chance Info (Read-Only)")]
    [SerializeField] [TextArea(3, 5)] private string appearanceChanceInfo = "Exponenciální šance na spawnutí:\nAI 0 = 0% | AI 1 = 10% | AI 5 = 12%\nAI 10 = 16% | AI 15 = 25% | AI 20 = 40%\n\nŠance roste exponenciálně!"; // Informace o šancích
    
    [Header("Advanced AI System - Noc a Čas")]
    [SerializeField] private bool useAdvancedAISystem = true; // Zapnout/vypnout pokročilý AI systém
    [SerializeField] private NightlyAISettings aiSettings = new NightlyAISettings(); // Nastavení AI podle noci a času
    [SerializeField] private bool enableRealTimeAIUpdates = true; // Aktualizovat AI v reálném čase podle herního času
    [SerializeField] private float aiUpdateInterval = 1f; // Jak často kontrolovat změny času (sekundy)
    
    [Header("Current AI Status (Read-Only - Pro zobrazení)")]
    [SerializeField] private int currentEffectiveAI = 0; // Aktuální efektivní AI level (pouze pro zobrazení)
    [SerializeField] private int currentNightDisplay = 1; // Aktuální noc (pouze pro zobrazení)
    [SerializeField] private string currentTimeDisplay = "12 AM"; // Aktuální čas (pouze pro zobrazení)
    [SerializeField] private string systemStatusDisplay = "Manual AI"; // Status systému (pouze pro zobrazení)
    
    [Header("Movement Opportunity System")]
    [SerializeField] private float movementInterval = 4.0f; // Interval mezi movement opportunities (FNAF style)
    [SerializeField] private bool enableMovementOpportunities = true; // Zapnout/vypnout movement opportunities
    [SerializeField] private bool requireCameraOffForMovement = true; // Pohyb pouze při slezení z kamer
    
    [Header("Game System References")]
    [SerializeField] private ShiftTimer shiftTimer; // Reference na shift timer pro získání aktuálního času
    [SerializeField] private NightShiftManagement nightManagement; // Reference na night management pro získání aktuální noci
    
    [Header("Camera System References")]
    [SerializeField] private CameraSystem cameraSystem; // Reference na hlavní kamery
    [SerializeField] private CameryVentSystem ventCameraSystem; // Reference na ventilační kamery
    [SerializeField] private Button camsOffButton; // Tlačítko pro CamsOff
    [SerializeField] private Button camsOnButton; // Tlačítko pro CamsOn
    
    [Header("Death System")]
    [SerializeField] private GameObject deadPanel; // Dead panel pro jumpscare
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player pro dead panel (child objektu deadPanel)
    [SerializeField] private GameObject deadPanelZofeye; // Dead panel Zofaye - pro detekci její smrti
    [SerializeField] private NightShiftManagement nightShiftManagement; // Reference na night management
    [SerializeField] private float gameOverDelay = 5f; // Delay před návratem do menu po smrti
    
    [Header("Light System - Vypnutí před jumpscarem")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo kanceláře
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři
    [SerializeField] private float lightOffBeforeJumpscare = 0.9f; // Světlo zhasne 0.9s před jumpscarem
    [SerializeField] private float lightOnAfterDelay = 1f; // Světlo se zapne po 1s
    [SerializeField] private AudioSource lightOffAudioSource; // Audio source pro zvuk vypnutí světla
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí světla
    
    [Header("Audio")]
    [SerializeField] private AudioSource deathAudioSource; // AudioSource pro death zvuk
    [SerializeField] private AudioClip deathSound; // Death zvuk clip
    [SerializeField] private AudioSource appearanceAudioSource; // AudioSource pro zvuk objevení Thatchera
    [SerializeField] private AudioClip appearanceSound; // Zvuk objevení Thatchera (jako FNAF 1 Chica)
    
    [Header("Audio Management During Death")]
    [SerializeField] private AudioSource[] audioSourcesToMute; // Seznam AudioSource komponent k utlumení při smrti
    [SerializeField] private float mutedVolume = 0f; // Hlasitost při utlumení (0 = vypnuto)
    
    [Header("Direct Flashlight Control")]
    [SerializeField] private Light flashlightSpotlight; // Přímý odkaz na spotlight baterky
    [SerializeField] private CanvasGroup flashlightCanvasGroup; // Přímý odkaz na canvas group flashlight image
    [SerializeField] private float batteryDrainBeforeRetreat = 0.5f; // Jak dlouho před odchodem se má baterka vypnout
    [SerializeField] private AudioSource flashlightFlickerAudioSource; // AudioSource pro flickering zvuk při zapínání baterky
    [SerializeField] private AudioClip flashlightFlickerSound; // Zvuk flickering při zapínání baterky
    
    [Header("Flashlight Detection")]
    [SerializeField] private FlashlightSystem flashlightSystem; // Reference na flashlight systém
    [SerializeField] private Svetlo_Strop_Kancelar lightSystem; // Reference na světelný systém
    [SerializeField] private ElectricityManagement electricitySystem; // Reference na electricity management (pro force vypnutí)
    [SerializeField] private float lightRange = 10f; // Dosah světla baterky
    [SerializeField] private float detectionRadius = 80f; // Poloměr detekce kolem cursoru v pixelech na obrazovce
    [SerializeField] private int raycastCount = 9; // Počet raycastů pro pokrytí oblasti (3x3 grid)
    [SerializeField] private LayerMask raycastLayers = -1; // Vrstvy pro raycast
    [SerializeField] private string[] ignoreObjectNames = {"Window", "Glass", "Okno"}; // Objekty které se mají ignorovat při raycast
    
    [Header("Light Timeout System")]
    [SerializeField] private float lightTimeoutDuration = 3f; // Doba do smrti pokud zůstanou světla zapnutá (sekundy)
    [SerializeField] private bool enableLightTimeout = true; // Zapnout/vypnout light timeout mechaniku
    
    [Header("Defense System")]
    [SerializeField] private float defenseTime = 5f; // Doba v sekundách, jak dlouho musí být elektřina vypnutá pro úspěšnou obranu
    [SerializeField] private AudioSource thatcherLeaveAudioSource; // AudioSource pro zvuk odchodu Thatchera
    [SerializeField] private AudioClip thatcherLeaveSound; // Zvuk odchodu Thatchera
    [SerializeField] private bool enableDefenseSystem = true; // Zapnout/vypnout defense systém
    
    [Header("Darkness Effect")]
    [SerializeField] private GameObject darknessOverlay; // UI overlay pro tmavící efekt
    [SerializeField] private float darknessEffectDuration = 2f; // Doba tmavícího efektu v sekundách
    [SerializeField] private bool enableDarknessEffect = true; // Zapnout/vypnout tmavící efekt
    
    [Header("Eye Warning System")]
    [SerializeField] private GameObject leftEyeCube; // Levé oko cube object
    [SerializeField] private GameObject rightEyeCube; // Pravé oko cube object
    [SerializeField] private Color normalEyeColor = Color.white; // Normální barva očí
    [SerializeField] private Color angryEyeColor = Color.red; // Červená barva při hněvu
    [SerializeField] private bool enableEyeWarning = true; // Zapnout/vypnout vizuální varování očí
    
    [Header("Menu Detection System")]
    [SerializeField] private GameObject[] menuPanels = new GameObject[0]; // Seznam menu panelů pro detekci - AI = 0 když jsou aktivní
    [SerializeField] private bool enableMenuDetection = true; // Zapnout/vypnout menu detection
    
    [Header("Warden Interaction System")]
    [SerializeField] private WardenAI wardenAI; // Reference na Warden AI pro koordinaci odchodu
    [SerializeField] private float delayedRetreatAfterWarden = 1f; // Zpoždění odchodu po porážce Wardena (sekundy)
    
    [Header("Warden Double Jumpscare Prevention")]
    [SerializeField] private GameObject wardenDeadPanel; // Warden dead panel pro blokování dvojitého jumpscaru
    [SerializeField] private UnityEngine.Video.VideoPlayer wardenVideoPlayer; // Warden video player pro deaktivaci
    [SerializeField] private AudioSource wardenJumpscareAudioSource; // Warden jumpscare audio source pro vypnutí
    
    [Header("Fenerir Interaction System")]
    [SerializeField] private FenerirAI fenerirAI; // Reference na Fenerir AI pro blokování spawnu při You Failed
    
    [Header("Dokki Interaction System")]
    [SerializeField] private DokkiAI dokkiAI; // Reference na Dokki AI pro resetování progress
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool testCamsOff = false; // Tlačítko pro testování CamsOff v inspektoru
    
    #endregion
    
    #region Private Variables
    
    // Private variables pro uložení původních hlasitostí
    private float[] originalVolumes; // Původní hlasitosti audio zdrojů
    private bool isFlashlightPointingAtThatcher = false; // Zda baterka svítí na Thatchera
    private float flashlightStartTime = 0f; // Kdy začala baterka svítit na Thatchera
    private float currentToleranceTime = 0f; // Aktuální tolerance čas pro AI level
    
    // Movement Opportunity System variables
    private float movementTimer = 0f; // Timer pro movement opportunities
    private bool wasOnCamerasLastFrame = false; // Sleduje předchozí stav kamer pro detekci slezení
    
    // Light Timeout System variables
    private float lightTimeoutTimer = 0f; // Timer pro light timeout
    private bool isLightTimeoutActive = false; // Zda je light timeout timer aktivní
    
    // Defense System variables
    private float defenseTimer = 0f; // Timer pro defense systém
    private bool isDefenseActive = false; // Zda je defense timer aktivní
    private bool isThatcherLeaving = false; // Zda Thatcher právě odchází (pro blokování jiných akcí)
    
    // General state variables
    private bool lastCameraState = false; // Předchozí stav kamer
    private bool isThatcherActive = false; // Zda je Thatcher aktivní
    private bool lastElectricityState = true; // Předchozí stav elektřiny pro detekci změn
    private bool legitimateElectricityRestore = false; // Flag pro legitimní zapnutí elektřiny po obraně
    private bool electricityTrackingEnabled = false; // Zapnout/vypnout sledování elektřiny
    
    // BothActive System - Prevence dvojitého jumpscaru
    private bool areBothActive = false; // TRUE když jsou oba animatronici spawnutí zároveň
    private bool isMonitoringWardenPanel = false; // TRUE když aktivně sledujeme Warden dead panel
    private bool isThatcherBlocked = false; // TRUE když je Thatcher BLOKOVANÝ (nemůže zabít hráče)
    private float thatcherBlockTimer = 0f; // Timer pro automatické odblokování Thatchera (4s)
    private const float THATCHER_BLOCK_TIMEOUT = 4f; // 4s timeout - pak se Thatcher odblokuje automaticky
    
    // Advanced AI System variables
    private int currentDynamicAILevel = 0; // Aktuální AI level podle noci a času
    private int lastHour = -1; // Poslední zpracovaná herní hodina (-1 = žádná)
    private int lastNight = -1; // Poslední zpracovaná noc (-1 = žádná)
    private float aiUpdateTimer = 0f; // Timer pro aktualizace AI levelu
    
    // Eye warning system variables
    private Material leftEyeMaterial; // Materiál levého oka
    private Material rightEyeMaterial; // Materiál pravého oka
    private float eyeAngerProgress = 0f; // Progress zbarvení očí (0 = normální, 1 = červené)
    private bool isEyeWarningActive = false; // Zda je aktuálně aktivní varování očí
    
    // Camera Time Accumulation System - nový systém akumulace šance
    private float cameraTimeAccumulated = 0f; // Celkový čas strávený na kamerách (sekundy)
    private bool wasOnCamerasLastUpdate = false; // Sledování předchozího stavu kamer pro reset
    private float cameraTimeBonusChance = 0f; // Bonus šance z času na kamerách
    private const float CAMERA_TIME_BONUS_INTERVAL = 5f; // Každých 5 sekund = +3%
    private const float CAMERA_TIME_BONUS_PERCENT = 3f; // +3% za každých 5 sekund
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        // Najdi reference na kamerové systémy pokud nejsou přiřazeny
        if (cameraSystem == null)
            cameraSystem = FindFirstObjectByType<CameraSystem>();
            
        if (ventCameraSystem == null)
            ventCameraSystem = FindFirstObjectByType<CameryVentSystem>();
        
        // Přidej listener na CamsOff tlačítko
        if (camsOffButton != null)
        {
            camsOffButton.onClick.AddListener(OnCamsOffButtonClick);
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] CamsOff button listener added");
        }
        else
        {
            Debug.LogWarning("[ThatcherAI] CamsOff button is not assigned!");
        }
        
        // Přidej listener na CamsOn tlačítko
        if (camsOnButton != null)
        {
            camsOnButton.onClick.AddListener(OnCamsOnButtonClick);
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] CamsOn button listener added");
        }
        else
        {
            Debug.LogWarning("[ThatcherAI] CamsOn button is not assigned!");
        }
        
        // Najdi NightShiftManagement pokud není přiřazen
        if (nightShiftManagement == null)
            nightShiftManagement = FindFirstObjectByType<NightShiftManagement>();
        
        // Najdi FlashlightSystem pokud není přiřazen
        if (flashlightSystem == null)
            flashlightSystem = FindFirstObjectByType<FlashlightSystem>();
            
        // Najdi světelný systém pokud není přiřazen
        if (lightSystem == null)
            lightSystem = FindFirstObjectByType<Svetlo_Strop_Kancelar>();
        
        // Ujisti se, že Thatcher cube je na začátku skrytý
        if (thatcherCube != null)
            thatcherCube.SetActive(false);
        
        // Obnov audio na začátku noci (pro případ resetu)
        RestoreAudioSources();
        
        // Inicializuj movement opportunity timer
        movementTimer = movementInterval;
        wasOnCamerasLastFrame = false;
        
        // Electricity tracking vypnut na začátku - aktivuje se až při encounter
        electricityTrackingEnabled = false;
        
        // Inicializuj eye warning systém
        InitializeEyeMaterials();
        
        // Inicializace pokročilého AI systému
        if (useAdvancedAISystem)
        {
            InitializeAdvancedAISystem();
        }
        else
        {
            // I když není advanced systém, aktualizuj display hodnoty
            UpdateInspectorDisplayValues();
        }
        
        // Inicializace menu detection systému
        if (menuPanels == null)
        {
            menuPanels = new GameObject[0];
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Menu panels array initialized");
        }
            
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Thatcher AI initialized");
    }

    void Update()
    {
        // Menu Detection System - AI = 0 když je aktivní jakékolive menu
        if (IsMenuActive())
        {
            return; // Stop all AI activity when menu is active
        }
        
        // BothActive System - Thatcher block timer (odblokování po 4s)
        if (isThatcherBlocked && areBothActive)
        {
            HandleThatcherBlockTimer();
        }
        
        // Universal Character Death Detection - reset při jumpscaru jakékoliv postavy
        CheckForAnyCharacterDeath();
        
        // Character Interaction System - kontrola Zofaye smrti (pouze pokud máme reference)
        if (deadPanelZofeye != null)
        {
            CheckZofayeDeathAndReset();
        }
        
        // Testovací tlačítko v inspektoru
        if (testCamsOff)
        {
            testCamsOff = false; // Resetuj tlačítko
            OnCamerasOff(); // Simuluj sundání kamer
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Manual CamsOff test triggered!");
        }
        
        // Kontroluj stav kamer pro movement opportunity a flashlight detection
        bool currentCameraState = IsCameraSystemActive();
        
        // Movement Opportunity System (FNAF style)
        int effectiveAI = GetEffectiveAILevel();
        if (enableMovementOpportunities && effectiveAI > 0)
        {
            HandleMovementOpportunities(currentCameraState);
        }
        
        // Detekuj změnu stavu kamer (z aktivní na neaktivní = CamsOff) - starý systém jako backup
        if (lastCameraState && !currentCameraState && !enableMovementOpportunities)
        {
            OnCamerasOff();
        }
        
        // Camera Time Accumulation System - sledování času na kamerách
        HandleCameraTimeAccumulation(currentCameraState);
        
        lastCameraState = currentCameraState;
        wasOnCamerasLastFrame = currentCameraState;
        
        // Pokročilý AI systém - dynamické aktualizace AI podle času
        if (useAdvancedAISystem && enableRealTimeAIUpdates)
        {
            HandleAILevelUpdates();
        }
        
        // Nová mechanika: Kontrola baterky na Thatcher cube
        int effectiveAILevel = GetEffectiveAILevel();
        if (isThatcherActive && effectiveAILevel > 0) // Pouze pokud je Thatcher aktivní a AI > 0
        {
            CheckFlashlightOnThatcher();
        }
        
        // Light Timeout System - kontrola světel když je Thatcher aktivní
        if (isThatcherActive && enableLightTimeout)
        {
            HandleLightTimeout();
        }
        
        // Defense System - kontrola obrany proti Thatcherovi
        if (isThatcherActive && enableDefenseSystem && !isThatcherLeaving)
        {
            HandleDefenseSystem();
        }
        
        // NOVÁ MECHANIKA: Kontrola předčasného zapnutí elektřiny
        // Spouští se pouze když je tracking aktivní
        if (electricityTrackingEnabled && isThatcherActive && !isThatcherLeaving)
        {
            HandleElectricityReactivation();
        }
    }
    
    /// <summary>
    /// Kontroluje, zda je aktivní nějaký kamerový systém
    /// </summary>
    private bool IsCameraSystemActive()
    {
        bool mainCamerasActive = false;
        bool ventCamerasActive = false;
        
        // Kontrola hlavních kamer
        if (cameraSystem != null)
        {
            // Použij reflexi k získání CamerasOpen field
            var camerasOpenField = cameraSystem.GetType().GetField("CamerasOpen", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camerasOpenField != null)
            {
                mainCamerasActive = (bool)camerasOpenField.GetValue(cameraSystem);
            }
        }
        
        // Kontrola ventilačních kamer
        if (ventCameraSystem != null)
        {
            var ventCamerasOpenField = ventCameraSystem.GetType().GetField("VentCamerasOpen",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ventCamerasOpenField != null)
            {
                ventCamerasActive = (bool)ventCamerasOpenField.GetValue(ventCameraSystem);
            }
        }
        
        return mainCamerasActive || ventCamerasActive;
    }
    
    /// <summary>
    /// Volá se když je kliknuto na CamsOff tlačítko
    /// </summary>
    private void OnCamsOffButtonClick()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] 🔵 CamsOff BUTTON CLICKED - scheduling BothActive check in 1 second...");
        
        // Spusť OnCamerasOff normálně
        OnCamerasOff();
        
        // BothActive System - zkontroluj za 1 sekundu zda jsou oba animatronici spawnutí
        Invoke(nameof(CheckBothActiveCondition), 1f);
    }
    
    /// <summary>
    /// Volá se když je kliknuto na CamsOn tlačítko
    /// </summary>
    private void OnCamsOnButtonClick()
    {
        Debug.Log("[ThatcherAI] OnCamsOnButtonClick() called");
        
        // 🔴🔴 TOP PRIORITY: Pokud jsou OBA aktivní, Thatcher NEZABÍJÍ!
        if (areBothActive)
        {
            Debug.Log("[ThatcherAI] 🚫🚫🚫 OnCamsOnButtonClick BLOCKED - areBothActive is TRUE!");
            return; // FORCE STOP - Warden má prioritu!
        }
        
        // 🔴 PRIORITY: Pokud je Warden útočící, Thatcher NEZABÍJÍ!
        if (wardenAI != null && wardenAI.IsWardenAttacking)
        {
            Debug.Log("[ThatcherAI] 🚫🚫🚫 OnCamsOnButtonClick BLOCKED - Warden is attacking!");
            return; // FORCE STOP
        }
        
        // 🔴 Pokud je Thatcher blokovaný, NEZABÍJÍ!
        if (isThatcherBlocked)
        {
            Debug.Log("[ThatcherAI] 🚫 OnCamsOnButtonClick BLOCKED - isThatcherBlocked is TRUE!");
            return;
        }
        
        // Pokud je Thatcher aktivní
        if (isThatcherActive)
        {
            // OKAMŽitě zavři kamery - hráč je neuvidí
            if (camsOffButton != null)
            {
                Debug.Log("[ThatcherAI] Force closing cameras immediately!");
                    
                camsOffButton.onClick.Invoke();
            }
            
            Debug.Log("[ThatcherAI] CamsOn clicked during Thatcher attack - IMMEDIATE DEATH WITH LIGHTS!");
            
            // Spusť camera death SE SVĚTLY (vypne hned, zapne po 1s)
            StartCoroutine(TriggerCameraDeathWithLights());
        }
    }
    
    /// <summary>
    /// Kontrola, jestli Thatcher blokuje zapnutí kamer
    /// 🔴 FORCE PRIORITY: Pokud je Warden aktivní/útočí, Thatcher NEBLOKUJE! (vrací FALSE)
    /// </summary>
    public bool IsThatcherBlockingCameras()
    {
        // 🔴 TOP PRIORITY: Pokud jsou OBA aktivní, Thatcher NEBLOKUJE kamery!
        if (areBothActive)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] 🚫 BOTH ACTIVE - Thatcher does NOT block cameras (Warden priority)!");
            return false; // FORCE FALSE - Warden má prioritu!
        }
        
        // 🔴 TOP PRIORITY: Pokud Warden útočí, Thatcher NEBLOKUJE kamery!
        if (wardenAI != null && wardenAI.IsWardenAttacking)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] 🚫 WARDEN IS ATTACKING - Thatcher does NOT block cameras!");
            return false; // FORCE FALSE - Warden má prioritu!
        }
        
        // Normální blocking když je pouze Thatcher aktivní
        if (isThatcherActive && enableDebugLogs)
            Debug.Log("[ThatcherAI] ✅ Thatcher IS blocking cameras (only Thatcher active)");
            
        return isThatcherActive;
    }
    
    /// <summary>
    /// Veřejná metoda pro triggerování smrti (volá CameraSystem při pokusu o zapnutí kamer)
    /// </summary>
    public void TriggerDeathFromCameraAttempt()
    {
        Debug.Log("[ThatcherAI] ========== TriggerDeathFromCameraAttempt() CALLED ==========");
        Debug.Log($"[ThatcherAI] isThatcherActive: {isThatcherActive}");
        Debug.Log($"[ThatcherAI] areBothActive: {areBothActive}");
        Debug.Log($"[ThatcherAI] isThatcherBlocked: {isThatcherBlocked}");
        Debug.Log($"[ThatcherAI] Warden attacking: {(wardenAI != null ? wardenAI.IsWardenAttacking.ToString() : "NULL")}");
        
        // OPRAVA PROBLÉMU 1: Pokud je elektřina vypnutá, nech CameraSystem zpracovat denial sound
        if (lightSystem != null && !lightSystem.IsLightOn)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Electricity is OFF - allowing CameraSystem to handle denial sound");
            return; // Nechci zabít hráče, ale nech CameraSystem přehrát denial sound
        }
        
        // Pouze pokud je elektřina ZAPNUTÁ, kontroluj Thatchera
        if (isThatcherActive)
        {
            // 🔴 MULTI-LAYER PROTECTION: Pokud jsou oba aktivní, VŮBEC NEZABÍJEJ!
            if (areBothActive)
            {
                Debug.Log("[ThatcherAI] 🚫🚫🚫 BOTH ACTIVE - Thatcher CANNOT kill! Warden has absolute priority!");
                return; // FORCE STOP!
            }
            
            // 🔴 PRIORITNÍ KONTROLA: Je Warden také aktivní a útočí? (stejně jako u elektřiny!)
            if (wardenAI != null && wardenAI.IsWardenAttacking)
            {
                Debug.Log("[ThatcherAI] 🚫🚫🚫 WARDEN IS ATTACKING - Thatcher camera death CANCELLED!");
                return; // ZASTAV SE! Warden má prioritu!
            }
            
            // BLOKOVÁNÍ: Pokud je Thatcher blokovaný (oba animatronici aktivní), NEPOVOLUJ smrt!
            if (isThatcherBlocked)
            {
                Debug.Log("[ThatcherAI] 🚫 Thatcher is BLOCKED - cannot kill player (Warden has priority)!");
                return;
            }
            
            // Thatcher není blokovaný - normální immediate death
            Debug.Log("[ThatcherAI] ⚠️⚠️⚠️ THATCHER KILLING PLAYER via cameras!");
            TriggerDeath();
        }
        else
        {
            Debug.Log("[ThatcherAI] Thatcher is NOT active - no death triggered");
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro zastavení všech Thatcher attack timerů (volá Warden když zabíjí)
    /// NEODBLOKUJE Thatchera - Warden potřebuje čas na jumpscare!
    /// </summary>
    public void StopAllThatcherAttackTimers()
    {
        // Zastav light timeout
        StopLightTimeout();
        
        // Zastav attack
        isThatcherActive = false;
        
        // NEODBLOKUJ - Warden si ukáže svůj jumpscare a POTOM odblokuje
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] ⏹️ ALL ATTACK TIMERS STOPPED - Warden zabíjí!");
    }
    
    /// <summary>
    /// OKAMŽITÉ odblokování Thatchera - volá se při úspěšné obraně proti Wardenovi nebo při menu
    /// </summary>
    public void ForceUnblockThatcher()
    {
        CancelInvoke(nameof(UnblockThatcherDelayed));
        isThatcherBlocked = false;
        thatcherBlockTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] 🔓 FORCE UNBLOCK - Thatcher je nyní odblokovaný!");
    }
    
    /// <summary>
    /// Zpožděné odblokování Thatchera (2s) - volá se když Warden ukáže dead panel
    /// </summary>
    public void UnblockThatcherAfterWardenDeath()
    {
        Invoke(nameof(UnblockThatcherDelayed), 2f);
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] ⏰ Scheduled DELAYED UNBLOCK in 2 seconds...");
    }
    
    /// <summary>
    /// Privátní callback pro zpožděné odblokování
    /// </summary>
    private void UnblockThatcherDelayed()
    {
        isThatcherBlocked = false;
        thatcherBlockTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] 🔓 THATCHER UNBLOCKED after 2s - Warden jumpscare completed!");
    }
    
    /// <summary>
    /// Vypočítá exponenciální šanci na objevení podle AI levelu + camera time bonus
    /// AI 0 = 0%, AI 1 = 10%, AI 20 = 40%
    /// + Camera Time Bonus: každých 5 sekund na kamerách = +1%
    /// Exponenciální křivka pro realističtější obtížnost
    /// </summary>
    private float CalculateExponentialAppearanceChance(int aiLevel)
    {
        if (aiLevel <= 0)
            return 0f; // AI 0 = žádná šance (ani camera time bonus)
        
        // Exponenciální vzorec pro plynulý přechod od 10% do 40%
        // Použijeme upravenou exponenciální funkci
        
        // Parametry pro exponenciální křivku
        float minChance = 10f;   // AI 1 = 10%
        float maxChance = 40f;   // AI 20 = 40%
        float aiRange = 19f;     // AI 1-20 = 19 kroků
        
        // Exponenciální faktor (určuje, jak strmá je křivka)
        float exponentialFactor = 2.2f; // Experimentálně nastaveno pro dobrou křivku
        
        // Normalizuj AI level na rozsah 0-1
        float normalizedAI = (aiLevel - 1f) / aiRange;
        
        // Vypočítej exponenciální hodnotu
        float exponentialValue = Mathf.Pow(normalizedAI, exponentialFactor);
        
        // Mapuj na požadovaný rozsah (10% - 40%)
        float baseAppearanceChance = minChance + (exponentialValue * (maxChance - minChance));
        
        // Přičti camera time bonus
        float totalAppearanceChance = baseAppearanceChance + cameraTimeBonusChance;
        
        if (enableDebugLogs && cameraTimeBonusChance > 0f)
        {
            Debug.Log($"[ThatcherAI] Appearance chance: Base {baseAppearanceChance:F1}% + Camera time bonus {cameraTimeBonusChance:F1}% = {totalAppearanceChance:F1}%");
        }
        
        return Mathf.Clamp(totalAppearanceChance, 0f, 100f);
    }
    
    /// <summary>
    /// Volá se když hráč sundá kamery
    /// </summary>
    private void OnCamerasOff()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Cameras turned off - checking appearance chance");
        
        // DEV MODE: Force spawn Thatcher (přeskočí všechny kontroly)
        if (nightShiftManagement != null && nightShiftManagement.IsDevModeForceSpawnBothActive())
        {
            if (!isThatcherActive)
            {
                Debug.Log("[ThatcherAI] 🔧 DEV MODE: FORCE SPAWNING THATCHER");
                AppearThatcher();
            }
            
            // Reset camera time accumulation po slezení z kamer
            ResetCameraTimeAccumulation();
            return; // Přeskoč normální spawn logiku
        }
            
        // Pokud už je Thatcher aktivní, nekontroluj znovu
        if (isThatcherActive)
            return;
        
        // BLOCKING: Fenerir is angry (You Failed panel active)
        if (fenerirAI != null && fenerirAI.IsFenerirAngry)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] ❌ Fenerir is angry (You Failed) - Thatcher spawn blocked");
            ResetCameraTimeAccumulation(); // Resetuj camera time i při blokaci
            return;
        }
            
        int effectiveAI = GetEffectiveAILevel();
        
        // VÝJIMKA: AI 0 = žádné appearance šance
        if (effectiveAI <= 0)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] AI = 0 - no appearance check performed");
            ResetCameraTimeAccumulation(); // Stejně resetuj camera time
            return;
        }
        
        // Nový exponenciální systém: AI 1 = 3%, AI 20 = 40%
        float appearanceChance = CalculateExponentialAppearanceChance(effectiveAI);
            
        // Generuj náhodné číslo 0-100
        float randomValue = Random.Range(0f, 100f);
        
        if (randomValue <= appearanceChance)
        {
            // Úspěch - objeví se Thatcher
            AppearThatcher();
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Thatcher appeared! AI Level: {effectiveAI}, Base+Bonus Chance: {appearanceChance:F1}%, Random: {randomValue:F1}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Thatcher did not appear. AI Level: {effectiveAI}, Base+Bonus Chance: {appearanceChance:F1}%, Random: {randomValue:F1}");
        }
        
        // Reset camera time accumulation po slezení z kamer
        ResetCameraTimeAccumulation();
    }
    
    /// <summary>
    /// Objeví Thatcher cube
    /// </summary>
    private void AppearThatcher()
    {
        if (thatcherCube != null)
        {
            thatcherCube.SetActive(true);
            isThatcherActive = true;
            
            // Reset legitimní zapnutí elektřiny pro nový encounter
            legitimateElectricityRestore = false;
            
            // AKTIVUJ sledování elektřiny pro encounter
            electricityTrackingEnabled = true;
            if (lightSystem != null)
            {
                lastElectricityState = lightSystem.IsLightOn;
            }
            
            // Přehraj zvuk objevení
            PlayAppearanceSound();
            
            // Spusť light timeout timer
            StartLightTimeout();
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Thatcher cube appeared - electricity tracking ENABLED!");
            
            // ⚡ OKAMŽITÁ KONTROLA: Pokud se Thatcher spawnuje zatímco Warden už útočí, OKAMŽITĚ zablokuj Thatchera
            if (wardenAI != null && wardenAI.IsWardenAttacking)
            {
                areBothActive = true;
                isThatcherBlocked = true;
                thatcherBlockTimer = 0f;
                isMonitoringWardenPanel = true;
                Debug.Log("[ThatcherAI] 🔴🔴⚡ INSTANT BLOCK - Warden already attacking, Thatcher BLOCKED immediately!");
            }
        }
        else
        {
            Debug.LogWarning("[ThatcherAI] Thatcher cube is not assigned!");
        }
    }
    
    /// <summary>
    /// Skryje Thatcher cube
    /// </summary>
    public void HideThatcher()
    {
        if (thatcherCube != null)
        {
            thatcherCube.SetActive(false);
            isThatcherActive = false;
            
            // Reset flashlight detekce
            isFlashlightPointingAtThatcher = false;
            
            // Zastaví light timeout timer
            StopLightTimeout();
            
            // VYPNI sledování elektřiny - návrat k normálnímu chování
            electricityTrackingEnabled = false;
            legitimateElectricityRestore = false;
            
            // Reset BothActive systému (Thatcher zmizel)
            ResetBothActiveSystem();
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Thatcher cube hidden - electricity tracking DISABLED (normal behavior restored)!");
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro získání stavu Thatchera
    /// </summary>
    public bool IsThatcherActive()
    {
        return isThatcherActive;
    }
    
    /// <summary>
    /// Veřejná metoda pro nastavení AI levelu (0-20)
    /// </summary>
    public void SetAILevel(int level)
    {
        aiLevel = Mathf.Clamp(level, 0, 20);
        
        // Aktualizuj display hodnoty
        UpdateInspectorDisplayValues();
        
        if (enableDebugLogs)
        {
            if (useAdvancedAISystem)
            {
                Debug.LogWarning($"[ThatcherAI] Manual AI Level set to: {aiLevel}, but Advanced AI System is active! This value will only be used when Advanced AI System is disabled.");
            }
            else
            {
                Debug.Log($"[ThatcherAI] AI Level set to: {aiLevel} ({((aiLevel / 20f) * 50f):F1}% chance)");
            }
        }
    }
    
    /// <summary>
    /// Získá aktuální manuální AI level (ne efektivní!)
    /// </summary>
    public int GetAILevel()
    {
        return aiLevel;
    }
    
    /// <summary>
    /// Získá aktuální šanci v procentech (pro debug)
    /// </summary>
    public float GetAppearanceChancePercent()
    {
        int effectiveAI = GetEffectiveAILevel();
        return CalculateExponentialAppearanceChance(effectiveAI);
    }
    
    /// <summary>
    /// Spustí death sekvenci
    /// </summary>
    private void TriggerDeath()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Triggering death sequence");
        
        // Zastav Thatcher mechaniky
        isThatcherActive = false;
        
        // Utlum ostatní zvuky
        MuteAudioSources();
        
        // Reset Thatcher
        HideThatcher();
        
        // Spusť death sekvenci se světly (0.9s vypnutí před dead panel)
        StartCoroutine(TriggerDeathWithLights());
    }
    
    /// <summary>
    /// Coroutina pro normální death se světly - vypne 0.9s před dead panel
    /// </summary>
    private System.Collections.IEnumerator TriggerDeathWithLights()
    {
        // Vypni světla 0.9s před dead panel
        yield return StartCoroutine(FlickerLights(false));
        
        // Zobraz dead panel s video delayem (0.1s)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // Přehraj death zvuk
        PlayDeathSound();
        
        // Počkej 1 sekundu a zapni světla zpátky
        yield return new WaitForSeconds(lightOnAfterDelay);
        yield return StartCoroutine(FlickerLights(true));
        
        // Návrat do menu po gameOverDelay
        Invoke(nameof(ReturnToMenu), gameOverDelay);
    }
    
    /// <summary>
    /// Návrat do menu (volá se po gameOverDelay sekundách přes Invoke)
    /// </summary>
    private void ReturnToMenu()
    {
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Returning to menu after {gameOverDelay} seconds");
        
        // Zastav video před skrytím panelu
        StopDeadPanelVideo();
        
        // Skryj dead panel
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }
        
        // Návrat do main menu stejně jako ESC
        if (nightShiftManagement != null)
        {
            var escapeMethod = nightShiftManagement.GetType().GetMethod("EscapeToMainMenu", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (escapeMethod != null)
            {
                escapeMethod.Invoke(nightShiftManagement, null);
                
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] Returned to main menu using EscapeToMainMenu");
            }
            else
            {
                Debug.LogWarning("[ThatcherAI] EscapeToMainMenu method not found!");
            }
        }
        else
        {
            Debug.LogWarning("[ThatcherAI] NightShiftManagement is not assigned!");
        }
    }
    
    /// <summary>
    /// Coroutina pro camera death se světly - vypne OKAMŽITĚ, zapne po 1s
    /// </summary>
    private System.Collections.IEnumerator TriggerCameraDeathWithLights()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Camera death - Thatcher mechaniky zastavené");
        
        // Zastav Thatcher mechaniky
        isThatcherActive = false;
        
        // Utlum ostatní zvuky
        MuteAudioSources();
        
        // OKAMŽITĚ vypni světlo se zvukem (bez delay)
        yield return StartCoroutine(FlickerLights(false));
        
        // Počkej 0.9s před zobrazením dead panel
        yield return new WaitForSeconds(lightOffBeforeJumpscare);
        
        // Zobraz dead panel s video delayem (0.1s)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // Přehraj death zvuk
        PlayDeathSound();
        
        // Reset Thatcher
        HideThatcher();
        
        // Počkej 1 sekundu a zapni světla zpátky
        yield return new WaitForSeconds(lightOnAfterDelay);
        yield return StartCoroutine(FlickerLights(true));
        
        // Návrat do menu po gameOverDelay
        Invoke(nameof(ReturnToMenu), gameOverDelay);
    }
    
    /// <summary>
    /// Coroutina pro zobrazení dead panelu s delayem na načtení videa
    /// </summary>
    private System.Collections.IEnumerator ShowDeadPanelWithVideoDelay()
    {
        // Připrav video
        PrepareDeadPanelVideo();
        
        // Počkej 0.1 sekundy - dá Unity čas načíst první frame videa
        yield return new WaitForSeconds(0.1f);
        
        // Teď zobraz dead panel - video je už připravené
        if (deadPanel != null)
        {
            deadPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Dead panel shown (po 0.1s video loading delay)");
            
            // Spusť video (už je načtené)
            StartDeadPanelVideo();
        }
        else
        {
            Debug.LogError("[ThatcherAI] Dead panel reference is null! Assign it in inspector.");
        }
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
                Debug.Log("[ThatcherAI] Playing light-off sound (instant)");
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
                Debug.Log($"[ThatcherAI] Office lights INSTANT: {(turnOn ? "ON" : "OFF")}");
        }
        
        // Po zapnutí lights znovu enabled flashlight pokud byl active
        if (turnOn && flashlightSystem != null && flashlightSpotlight != null)
        {
            // Pokud je flashlight system active, zajisti že spotlight je enabled
            if (flashlightSystem.IsFlashlightActive())
            {
                flashlightSpotlight.enabled = true;
                
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] Re-enabled flashlight spotlight after lights ON (instant)");
            }
        }
        
        yield return null; // Žádný delay
    }
    
    /// <summary>
    /// Zapíná/vypíná světla s delayem (pro normální death)
    /// </summary>
    private System.Collections.IEnumerator FlickerLights(bool turnOn)
    {
        // Při vypínání přehraj zvuk
        if (!turnOn && lightOffSound != null && lightOffAudioSource != null)
        {
            lightOffAudioSource.PlayOneShot(lightOffSound);
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Playing light-off sound");
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
                Debug.Log($"[ThatcherAI] Office lights: {(turnOn ? "ON" : "OFF")}");
        }
        
        // Po zapnutí lights znovu enabled flashlight pokud byl active
        if (turnOn && flashlightSystem != null && flashlightSpotlight != null)
        {
            // Pokud je flashlight system active, zajisti že spotlight je enabled
            if (flashlightSystem.IsFlashlightActive())
            {
                flashlightSpotlight.enabled = true;
                
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] Re-enabled flashlight spotlight after lights ON");
            }
        }
        
        // Čekej podle timingu (0s při vypnutí, 0s při zapnutí - delay je mimo tuto funkci)
        yield return null;
    }
    
    #region Video Player Management
    
    /// <summary>
    /// Připraví video pro přehrávání - aktivuje VideoPlayer a načte první frame
    /// </summary>
    private void PrepareDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ThatcherAI] Dead panel VideoPlayer není přiřazený!");
            return;
        }
        
        // Aktivuj VideoPlayer GameObject
        deadPanelVideoPlayer.gameObject.SetActive(true);
        
        // Připrav video (načte první frame)
        deadPanelVideoPlayer.Prepare();
        
        // Ujisti se, že začne od začátku
        deadPanelVideoPlayer.frame = 0;
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Video prepared - frame set to 0");
    }
    
    /// <summary>
    /// Spustí přehrávání videa
    /// </summary>
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
            return;
        
        // Spusť video pouze pokud je připravené
        if (deadPanelVideoPlayer.isPrepared)
        {
            deadPanelVideoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Video started playing");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ThatcherAI] Video is not prepared yet!");
        }
    }
    
    /// <summary>
    /// Zastaví video a resetuje ho na začátek
    /// </summary>
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer == null)
            return;
        
        // Zastav video pokud hraje
        if (deadPanelVideoPlayer.isPlaying)
        {
            deadPanelVideoPlayer.Stop();
        }
        
        // Reset na začátek
        deadPanelVideoPlayer.frame = 0;
        
        // Deaktivuj VideoPlayer GameObject
        deadPanelVideoPlayer.gameObject.SetActive(false);
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Video stopped and reset");
    }
    
    #endregion
    
    /// <summary>
    /// Přehraje death zvuk
    /// </summary>
    private void PlayDeathSound()
    {
        try
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Playing death sound");
            
            // Vytvoř nebo najdi AudioSource
            if (deathAudioSource == null)
            {
                deathAudioSource = GetComponent<AudioSource>();
                
                if (deathAudioSource == null)
                {
                    deathAudioSource = gameObject.AddComponent<AudioSource>();
                    if (enableDebugLogs)
                        Debug.Log("[ThatcherAI] Created new AudioSource for death sound");
                }
            }
            
            if (deathAudioSource != null && deathSound != null)
            {
                // Nastav AudioSource properties pro kvalitní přehrání
                deathAudioSource.clip = deathSound;
                deathAudioSource.volume = 1f;
                deathAudioSource.pitch = 1f;
                deathAudioSource.loop = false;
                deathAudioSource.playOnAwake = false;
                
                // Přehraj zvuk
                deathAudioSource.Play();
                
                if (enableDebugLogs)
                    Debug.Log($"[ThatcherAI] Death sound playing: {deathAudioSource.isPlaying}");
            }
            else
            {
                if (deathSound == null)
                    Debug.LogWarning("[ThatcherAI] deathSound is null! Assign audio clip in inspector.");
                if (deathAudioSource == null)
                    Debug.LogWarning("[ThatcherAI] deathAudioSource is null!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ThatcherAI] Error playing death sound: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Přehraje zvuk objevení Thatchera (jako FNAF 1 Chica)
    /// </summary>
    private void PlayAppearanceSound()
    {
        try
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Playing appearance sound");
            
            // Vytvoř nebo najdi AudioSource pro objevení
            if (appearanceAudioSource == null)
            {
                appearanceAudioSource = GetComponent<AudioSource>();
                
                if (appearanceAudioSource == null)
                {
                    appearanceAudioSource = gameObject.AddComponent<AudioSource>();
                    if (enableDebugLogs)
                        Debug.Log("[ThatcherAI] Created new AudioSource for appearance sound");
                }
            }
            
            if (appearanceAudioSource != null && appearanceSound != null)
            {
                // Nastav AudioSource properties pro kvalitní přehrání (zachovej původní volume)
                appearanceAudioSource.clip = appearanceSound;
                // Volume se nezměňuje - zůstane jak je nastavena v inspektoru
                appearanceAudioSource.pitch = 1f;
                appearanceAudioSource.loop = false;
                appearanceAudioSource.playOnAwake = false;
                
                // Přehraj zvuk
                appearanceAudioSource.Play();
                
                if (enableDebugLogs)
                    Debug.Log($"[ThatcherAI] Appearance sound playing: {appearanceAudioSource.isPlaying} (volume: {appearanceAudioSource.volume:F2})");
            }
            else
            {
                if (appearanceSound == null)
                    Debug.LogWarning("[ThatcherAI] appearanceSound is null! Assign audio clip in inspector.");
                if (appearanceAudioSource == null)
                    Debug.LogWarning("[ThatcherAI] appearanceAudioSource is null!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ThatcherAI] Error playing appearance sound: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Utlumí všechny AudioSource komponenty v seznamu
    /// </summary>
    private void MuteAudioSources()
    {
        if (audioSourcesToMute == null || audioSourcesToMute.Length == 0)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] No audio sources to mute");
            return;
        }
        
        // Inicializuj pole pro uložení původních hlasitostí
        if (originalVolumes == null || originalVolumes.Length != audioSourcesToMute.Length)
        {
            originalVolumes = new float[audioSourcesToMute.Length];
        }
        
        // Ulož původní hlasitosti a utlum audio zdroje
        for (int i = 0; i < audioSourcesToMute.Length; i++)
        {
            if (audioSourcesToMute[i] != null)
            {
                originalVolumes[i] = audioSourcesToMute[i].volume;
                audioSourcesToMute[i].volume = mutedVolume;
                
                if (enableDebugLogs)
                    Debug.Log($"[ThatcherAI] Muted audio source {audioSourcesToMute[i].name} (was {originalVolumes[i]:F2}, now {mutedVolume:F2})");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Muted {audioSourcesToMute.Length} audio sources");
    }
    
    /// <summary>
    /// Obnoví původní hlasitosti všech AudioSource komponent
    /// </summary>
    private void RestoreAudioSources()
    {
        if (audioSourcesToMute == null || originalVolumes == null)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] No audio sources to restore");
            return;
        }
        
        // Obnov původní hlasitosti
        for (int i = 0; i < audioSourcesToMute.Length && i < originalVolumes.Length; i++)
        {
            if (audioSourcesToMute[i] != null)
            {
                audioSourcesToMute[i].volume = originalVolumes[i];
                
                if (enableDebugLogs)
                    Debug.Log($"[ThatcherAI] Restored audio source {audioSourcesToMute[i].name} to volume {originalVolumes[i]:F2}");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Restored {audioSourcesToMute.Length} audio sources");
    }
    
    /// <summary>
    /// Veřejná metoda pro obnovení audio při resetu noci (volá se z vnějšku)
    /// </summary>
    public void RestoreAudioOnNightReset()
    {
        RestoreAudioSources();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Audio restored for new night");
    }
    
    /// <summary>
    /// Vypočítá tolerance čas pro aktuální AI level
    /// </summary>
    private float CalculateToleranceTime()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI == 0)
            return float.MaxValue; // Nekonečný čas pro AI 0
        
        // AI 1 = 2 sekundy, AI 20 = 0.1 sekundy
        // Lineární interpolace: (2 - 0.1) / (20 - 1) = 1.9 / 19 = 0.1 per AI level
        float timeRange = 2f - 0.1f; // 1.9 sekund rozsah
        float aiRange = 20f - 1f; // 19 AI levels rozsah
        
        // Přepočítej AI level (1-20) na tolerance time (2-0.1)
        float toleranceTime = 2f - ((effectiveAI - 1f) / aiRange) * timeRange;
        
        return Mathf.Clamp(toleranceTime, 0.1f, 2f);
    }
    
    /// <summary>
    /// Kontroluje, zda objekt má být ignorován při raycast (okna, sklo)
    /// </summary>
    private bool ShouldIgnoreObject(GameObject obj)
    {
        if (obj == null || ignoreObjectNames == null) return false;
        
        string objName = obj.name.ToLower();
        foreach (string ignoreName in ignoreObjectNames)
        {
            if (objName.Contains(ignoreName.ToLower()))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Kontroluje, zda baterka svítí na Thatcher cube s pokrytím celého světelného kuželu
    /// </summary>
    private void CheckFlashlightOnThatcher()
    {
        // Zkontroluj, zda je světlo vypnuté (nutné pro baterku)
        bool isLightOff = (lightSystem != null && !lightSystem.IsLightOn);
        
        // Zkontroluj, zda je baterka aktivní
        bool isFlashlightActive = (flashlightSystem != null && flashlightSystem.IsFlashlightActive());
        
        if (!isLightOff || !isFlashlightActive || thatcherCube == null)
        {
            // Reset detekce pokud nejsou splněny podmínky
            if (isFlashlightPointingAtThatcher)
            {
                isFlashlightPointingAtThatcher = false;
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] Flashlight detection reset - conditions not met");
            }
            return;
        }
        
        // Získej kameru a pozici myši
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;
        
        Vector3 mouseScreenPos = Input.mousePosition;
        
        bool hitThatcher = false;
        
        // Jednoduché rozložení raycastů kolem cursoru s fixním offsetem v pixelech
        float offsetStep = detectionRadius / 2f; // Rozdělení oblasti na grid
        
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
                
                // Vytvořte ray z offset pozice
                Ray ray = playerCamera.ScreenPointToRay(offsetScreenPos);
                
                // Proveď raycast s možností průchodu skrz ignorované objekty
                RaycastHit[] hits = Physics.RaycastAll(ray, lightRange, raycastLayers);
                
                // Projdi všechny zasažené objekty
                foreach (RaycastHit hit in hits)
                {
                    // Ignoruj objekty jako okna, sklo
                    if (ShouldIgnoreObject(hit.transform.gameObject))
                    {
                        if (enableDebugLogs && x == 0 && y == 0) // Log pouze pro střední ray
                            Debug.Log($"[ThatcherAI] Ignoring object: {hit.transform.name}");
                        continue;
                    }
                    
                    // Zkontroluj, zda raycast zasáhl Thatcher cube nebo jeho potomky
                    Transform hitTransform = hit.transform;
                    while (hitTransform != null)
                    {
                        if (hitTransform.gameObject == thatcherCube)
                        {
                            hitThatcher = true;
                            if (enableDebugLogs && x == 0 && y == 0) // Log pouze pro střední ray
                                Debug.Log($"[ThatcherAI] Flashlight area hit Thatcher at offset [{x},{y}] through: {hit.transform.name}");
                            break;
                        }
                        hitTransform = hitTransform.parent;
                    }
                    
                    if (hitThatcher) break;
                }
                
                if (hitThatcher) break;
            }
            if (hitThatcher) break;
        }
        
        // Detekce začátku svícení na Thatchera
        if (hitThatcher && !isFlashlightPointingAtThatcher)
        {
            isFlashlightPointingAtThatcher = true;
            flashlightStartTime = Time.time;
            currentToleranceTime = CalculateToleranceTime();
            
            // Spustit eye warning na dobu tolerance
            StartEyeWarning(currentToleranceTime);
            
            // ODBLOKOVÁNÍ THATCHERA: Pokud jsou oba aktivní a hráč svítí na Thatchera, odblokuj ho!
            if (isThatcherBlocked && areBothActive)
            {
                isThatcherBlocked = false;
                Debug.Log("[ThatcherAI] 🔓 THATCHER UNBLOCKED by FLASHLIGHT! Player is pointing at Thatcher!");
                Debug.Log("[ThatcherAI] ⚠️ Thatcher can now kill via flashlight timeout!");
            }
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Flashlight started pointing at Thatcher! Tolerance: {currentToleranceTime:F2}s (AI: {aiLevel})");
        }
        // Detekce konce svícení na Thatchera
        else if (!hitThatcher && isFlashlightPointingAtThatcher)
        {
            isFlashlightPointingAtThatcher = false;
            
            // Zastavit eye warning a resetovat barvy
            StopEyeWarning();
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Flashlight stopped pointing at Thatcher");
        }
        // Kontrola času pokud stále svítí na Thatchera
        else if (hitThatcher && isFlashlightPointingAtThatcher)
        {
            float elapsedTime = Time.time - flashlightStartTime;
            
            if (elapsedTime >= currentToleranceTime)
            {
                // Čas vypršel - triggeruj smrt
                if (enableDebugLogs)
                    Debug.Log($"[ThatcherAI] Flashlight tolerance exceeded! Time: {elapsedTime:F2}s / {currentToleranceTime:F2}s (AI: {aiLevel})");
                
                // Reset detekce před smrtí
                isFlashlightPointingAtThatcher = false;
                
                // Triggeruj smrt
                TriggerDeath();
            }
        }
    }
    
    /// <summary>
    /// FNAF-style Movement Opportunity System
    /// </summary>
    private void HandleMovementOpportunities(bool currentCameraState)
    {
        // Odpočítávej movement timer
        movementTimer -= Time.deltaTime;
        
        // Pokud timer vypršel, spusť movement opportunity
        if (movementTimer <= 0f)
        {
            // Reset timer na další opportunity
            movementTimer = movementInterval;
            
            // Proveď movement opportunity check
            ProcessMovementOpportunity(currentCameraState);
        }
    }
    
    /// <summary>
    /// Zpracuje jednotlivou movement opportunity
    /// </summary>
    private void ProcessMovementOpportunity(bool currentCameraState)
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Movement opportunity triggered! AI Level: {effectiveAI}");
        
        // FNAF AI Check: Roll 1-20 vs AI Level
        int roll = Random.Range(1, 21); // 1-20 (včetně 20)
        bool movementSuccess = roll <= effectiveAI;
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Movement roll: {roll} vs AI {effectiveAI} = {(movementSuccess ? "SUCCESS" : "FAIL")}");
        
        // Pokud movement neuspěl, nic se neděje
        if (!movementSuccess)
            return;
        
        // Movement uspěl - zkontroluj podmínky
        bool canMove = true;
        
        // Pokud je vyžadováno slezení z kamer
        if (requireCameraOffForMovement)
        {
            // Thatcher se může pohnout pouze pokud hráč právě slezl z kamer
            bool justGotOffCameras = wasOnCamerasLastFrame && !currentCameraState;
            canMove = justGotOffCameras;
            
            if (enableDebugLogs && !canMove)
                Debug.Log("[ThatcherAI] Movement blocked - player didn't just get off cameras");
        }
        
        // Pokud už je Thatcher aktivní, nemůže se "pohnout" znovu
        if (isThatcherActive)
        {
            canMove = false;
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Movement blocked - Thatcher already active");
        }
        
        // Proveď movement pokud je možný
        if (canMove)
        {
            ExecuteMovement();
        }
    }
    
    /// <summary>
    /// Provede Thatcher movement (objevení)
    /// </summary>
    private void ExecuteMovement()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] MOVEMENT EXECUTED - Thatcher is moving!");
        
        // Stejná logika jako v původní OnCamerasOff metodě
        AppearThatcher();
    }
    
    #endregion
    
    #region Light Timeout System Implementation
    
    /// <summary>
    /// Spustí light timeout timer
    /// </summary>
    private void StartLightTimeout()
    {
        if (enableLightTimeout)
        {
            isLightTimeoutActive = true;
            lightTimeoutTimer = 0f;
            
            // Spusť eye warning pro light timeout (5 sekund)
            StartEyeWarning(lightTimeoutDuration);
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Light timeout started - player has {lightTimeoutDuration} seconds to turn off lights");
        }
    }
    
    /// <summary>
    /// Zastaví light timeout timer
    /// </summary>
    private void StopLightTimeout()
    {
        isLightTimeoutActive = false;
        lightTimeoutTimer = 0f;
        
        // Zastaví eye warning
        StopEyeWarning();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Light timeout stopped");
    }
    
    /// <summary>
    /// Zpracovává light timeout logiku
    /// </summary>
    private void HandleLightTimeout()
    {
        if (!isLightTimeoutActive || !enableLightTimeout)
            return;
            
        // Inkrementace timer
        lightTimeoutTimer += Time.deltaTime;
        
        // Kontrola stavu světel v kanceláři
        bool lightsOn = false;
        if (lightSystem != null)
        {
            lightsOn = lightSystem.IsLightOn;
        }
        
        // Pokud hráč zhasne světla, zastaví timeout
        if (!lightsOn)
        {
            StopLightTimeout();
            return;
        }
        
        // Pokud uplynul čas a světla jsou stále zapnutá, zabij hráče
        if (lightTimeoutTimer >= lightTimeoutDuration)
        {
            // BLOKOVÁNÍ: Pokud je Thatcher blokovaný (oba animatronici aktivní), NEPOVOLUJ smrt!
            if (isThatcherBlocked)
            {
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] 🚫 Light timeout expired but Thatcher is BLOCKED - cannot kill (Warden has priority)!");
                
                // Ale timer běží dál - po 4s se Thatcher odblokuje a může zabít
                // (to řeší HandleThatcherBlockTimer())
                return;
            }
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Light timeout expired - killing player!");
            
            TriggerDeath();
            StopLightTimeout();
        }
    }
    
    #endregion
    
    #region Defense System Implementation
    
    /// <summary>
    /// Zpracovává defense systém - hráč může zahnat Thatchera vypnutím elektřiny
    /// </summary>
    private void HandleDefenseSystem()
    {
        if (!enableDefenseSystem || isThatcherLeaving)
            return;
            
        // Kontrola podmínek pro úspěšnou obranu
        bool lightsOff = (lightSystem != null && !lightSystem.IsLightOn);
        bool flashlightNotPointingAtThatcher = !isFlashlightPointingAtThatcher;
        
        // Pokud jsou splněny podmínky pro obranu
        if (lightsOff && flashlightNotPointingAtThatcher)
        {
            if (!isDefenseActive)
            {
                // Spusť defense timer
                StartDefenseTimer();
            }
            else
            {
                // Pokračuj v odpočítávání
                defenseTimer += Time.deltaTime;
                
                if (enableDebugLogs && defenseTimer % 1f < Time.deltaTime) // Log každou sekundu
                    Debug.Log($"[ThatcherAI] Defense progress: {defenseTimer:F1}s / {defenseTime:F1}s");
                
                // Pokud uplynul defense čas, zahání Thatchera
                if (defenseTimer >= defenseTime)
                {
                    // Pokud je Warden v útoku, blokuj odchod Thatchera
                    if (wardenAI != null && wardenAI.IsWardenAttacking)
                    {
                        if (enableDebugLogs)
                            Debug.Log("[ThatcherAI] ⚠️ Retreat blocked - Warden is attacking. Waiting for Warden outcome.");
                        // Nezahajuj odchod, počká se na volání z WardenAI po vítězství hráče
                    }
                    else
                    {
                        StartThatcherRetreat();
                    }
                }
            }
        }
        else
        {
            // Podmínky nejsou splněny - resetuj defense timer
            if (isDefenseActive)
            {
                StopDefenseTimer();
            }
        }
    }
    
    /// <summary>
    /// Spustí defense timer
    /// </summary>
    private void StartDefenseTimer()
    {
        isDefenseActive = true;
        defenseTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Defense started - player has {defenseTime} seconds to hide!");
    }
    
    /// <summary>
    /// Zastaví defense timer
    /// </summary>
    private void StopDefenseTimer()
    {
        isDefenseActive = false;
        defenseTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Defense interrupted - conditions not met");
    }
    
    /// <summary>
    /// Zahájí proces odchodu Thatchera
    /// </summary>
    private void StartThatcherRetreat()
    {
        if (isThatcherLeaving)
            return;
            
        isThatcherLeaving = true;
        StopDefenseTimer();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Thatcher retreating - defense successful!");
        
        // Přehraj zvuk odchodu
        PlayThatcherLeaveSound();
        
        // Spusť nový FNAC3-style battery drain effect místo darkness efektu
        StartCoroutine(FlashlightBatteryDrainEffect());
    }
    
    /// <summary>
    /// Přehraje zvuk odchodu Thatchera
    /// </summary>
    private void PlayThatcherLeaveSound()
    {
        if (thatcherLeaveAudioSource != null && thatcherLeaveSound != null)
        {
            thatcherLeaveAudioSource.clip = thatcherLeaveSound;
            thatcherLeaveAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Playing Thatcher leave sound");
        }
    }

    /// <summary>
    /// Spustí zpožděný odchod Thatchera po porážce Wardena.
    /// Volá se z WardenAI po OnPlayerSucceeded().
    /// </summary>
    public void TriggerDelayedRetreatAfterWarden()
    {
        if (!isThatcherActive)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Delayed retreat requested but Thatcher is not active - ignoring");
            return;
        }
        
        if (isThatcherLeaving)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Delayed retreat requested but Thatcher already leaving - ignoring");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] 🔄 Warden defeated - Thatcher will retreat in {delayedRetreatAfterWarden}s (skipping waiting time)");
        
        StartCoroutine(DelayedThatcherRetreat());
    }
    
    /// <summary>
    /// Coroutina pro zpožděný odchod Thatchera po porážce Wardena.
    /// </summary>
    private System.Collections.IEnumerator DelayedThatcherRetreat()
    {
        yield return new WaitForSeconds(delayedRetreatAfterWarden);
        
        // Bez ohledu na defenseTimer zahaj odchod (přeskoč waiting time)
        StopDefenseTimer();
        StartThatcherRetreat();
    }
    
    /// <summary>
    /// Nastaví pozici a směr flashlight spotlight podle pozice cursoru
    /// </summary>
    private void UpdateFlashlightToCursor()
    {
        if (flashlightSpotlight == null) return;
        
        // Najdi hlavní kameru
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        
        if (playerCamera == null)
        {
            Debug.LogWarning("[ThatcherAI] No camera found for flashlight positioning");
            return;
        }
        
        // Nastav pozici flashlight na pozici kamery
        flashlightSpotlight.transform.position = playerCamera.transform.position;
        
        // Převeď pozici myši na světové souřadnice
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // Vzdálenost od kamery
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // Nastav rotaci světla tak, aby svítilo směrem k myši
        Vector3 direction = (mouseWorldPos - flashlightSpotlight.transform.position).normalized;
        flashlightSpotlight.transform.rotation = Quaternion.LookRotation(direction);
        
        // Aktualizuj UI pozici flashlight image na pozici cursoru
        if (flashlightCanvasGroup != null)
        {
            RectTransform rectTransform = flashlightCanvasGroup.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.position = Input.mousePosition;
            }
        }
        
        // Fallback - aktualizuj přes FlashlightSystem pokud je dostupný
        if (flashlightSystem != null)
        {
            // FlashlightSystem má vlastní UpdateVisualEffects která sleduje cursor
            // Takže jen zajistíme, že je vše správně nastaveno
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Flashlight positioned to cursor at screen pos: {Input.mousePosition}");
    }

    /// <summary>
    /// Obnoví baterku PLYNULE s flickering efektem a zvukem
    /// </summary>
    private System.Collections.IEnumerator RestoreFlashlightDirectly()
    {
        Debug.Log("[ThatcherAI] RestoreFlashlightDirectly - ZAČÁTEK PLYNULÉHO ZAPNUTÍ!");
        
        yield return new WaitForSeconds(0.1f); // Krátká pauza
        
        Debug.Log($"[ThatcherAI] RestoreFlashlightDirectly - Components check:");
        Debug.Log($"  - flashlightSpotlight: {(flashlightSpotlight != null ? "OK" : "NULL")}");
        Debug.Log($"  - flashlightCanvasGroup: {(flashlightCanvasGroup != null ? "OK" : "NULL")}");
        Debug.Log($"  - flashlightFlickerAudioSource: {(flashlightFlickerAudioSource != null ? "OK" : "NULL")}");
        Debug.Log($"  - flashlightSystem: {(flashlightSystem != null ? "OK" : "NULL")}");
        Debug.Log($"  - lightSystem.IsLightOn: {(lightSystem != null ? lightSystem.IsLightOn.ToString() : "lightSystem is NULL")}");
        
        // Přehraj flickering zvuk na začátku
        if (flashlightFlickerAudioSource != null && flashlightFlickerSound != null)
        {
            flashlightFlickerAudioSource.PlayOneShot(flashlightFlickerSound);
            Debug.Log("[ThatcherAI] Flickering sound started for flashlight restoration");
        }
        
        // Plynulé zapnutí s flickering efektem (0.5s)
        Debug.Log("[ThatcherAI] Starting smooth flickering restore effect...");
        
        float flickerDuration = 0.5f;
        float timer = 0f;
        
        while (timer < flickerDuration)
        {
            // Náhodné blikání během zapínání
            bool shouldBeOn = Random.value > 0.3f; // 70% šance na zapnutí
            
            // Aplikuj na spotlight
            if (flashlightSpotlight != null)
            {
                flashlightSpotlight.enabled = shouldBeOn;
                
                // Nastav pozici a směr světla podle cursoru
                if (shouldBeOn)
                {
                    UpdateFlashlightToCursor();
                }
            }
            
            // Aplikuj na canvas group
            if (flashlightCanvasGroup != null)
            {
                flashlightCanvasGroup.alpha = shouldBeOn ? 1f : 0.1f;
            }
            
            // Kratší intervaly na začátku, delší na konci
            float intervalLength = Mathf.Lerp(0.05f, 0.15f, timer / flickerDuration);
            yield return new WaitForSeconds(intervalLength);
            timer += intervalLength;
        }
        
        // Finální zapnutí - FORCE enable všech komponent
        Debug.Log("[ThatcherAI] Final FORCE enable of all components");
        
        if (flashlightSpotlight != null)
        {
            flashlightSpotlight.enabled = true;
            // Finální nastavení pozice na cursor
            UpdateFlashlightToCursor();
            Debug.Log("[ThatcherAI] Spotlight FINAL enabled and positioned to cursor");
        }
        
        if (flashlightCanvasGroup != null)
        {
            flashlightCanvasGroup.alpha = 1f;
            Debug.Log("[ThatcherAI] Canvas Group FINAL enabled");
        }
        
        // Force enable přes FlashlightSystem jako backup
        if (flashlightSystem != null)
        {
            flashlightSystem.ForceEnableFlashlightComponents();
            Debug.Log("[ThatcherAI] ForceEnableFlashlightComponents called as backup");
        }
        
        Debug.Log("[ThatcherAI] RestoreFlashlightDirectly - PLYNULÉ ZAPNUTÍ DOKONČENO!");
    }

    /// <summary>
    /// Obnoví baterku po battery drain efektu
    /// </summary>
    private System.Collections.IEnumerator RestoreFlashlightAfterDrain()
    {
        yield return new WaitForSeconds(0.1f); // Krátká pauza
        
        if (flashlightSystem != null)
        {
            // Zapni baterku zpět pouze pokud je světlo vypnuté
            if (lightSystem != null && !lightSystem.IsLightOn)
            {
                if (!flashlightSystem.IsFlashlightActive())
                {
                    flashlightSystem.RestoreFlashlightManually();
                    
                    if (enableDebugLogs)
                        Debug.Log("[ThatcherAI] Flashlight restored after battery drain effect");
                }
            }
        }
    }

    /// <summary>
    /// FNAC3-style battery drain effect - baterka začne blikat a pak se vypne
    /// </summary>
    private System.Collections.IEnumerator FlashlightBatteryDrainEffect()
    {
        Debug.Log($"[ThatcherAI] *** FlashlightBatteryDrainEffect ZAČÍNÁ *** (drain before retreat: {batteryDrainBeforeRetreat}s)");
        
        // Fáze 1: Baterka se vypne s nastavitelným časem před zvukem odchodu
        if (flashlightSystem != null && flashlightSystem.IsFlashlightActive())
        {
            Debug.Log($"[ThatcherAI] Phase 1: Battery flickering and draining ({batteryDrainBeforeRetreat}s before retreat sound)");
            
            // Spusť progressive battery drain effect s nastavitelnou dobou
            yield return StartCoroutine(flashlightSystem.ProgressiveBatteryDrainEffect(batteryDrainBeforeRetreat));
        }
        else
        {
            Debug.Log("[ThatcherAI] Phase 1 SKIPPED - flashlight not active or system null");
        }
        
        // Fáze 2: Thatcher zmizí OKAMŽITĚ na začátku (před zvukem odchodu)
        Debug.Log("[ThatcherAI] Phase 2: Thatcher disappearing immediately");
        
        // Thatcher zmizí HNED na začátku
        HideThatcher();
        isThatcherLeaving = false;
        
        // Reset movement timer pro nový cyklus
        movementTimer = movementInterval;
        
        // Fáze 3: Baterka zůstane vypnutá 2 sekundy
        Debug.Log("[ThatcherAI] Phase 3: Battery off for 2 seconds - STOPPING all flickering sounds");
        
        // ZASTAV flickering zvuky během vypnuté baterky
        if (flashlightSystem != null)
        {
            flashlightSystem.ImmediateStopFlickering();
            Debug.Log("[ThatcherAI] Flickering sounds stopped during battery outage");
        }
        
        yield return new WaitForSeconds(2f);
        
        // Fáze 4: Znovu zapni baterku PŘÍMO přes komponenty
        Debug.Log("[ThatcherAI] Phase 4: *** SPOUŠTÍM RestoreFlashlightDirectly ***");
        if (lightSystem != null && !lightSystem.IsLightOn)
        {
            // Zapni baterku přímo přes komponenty
            StartCoroutine(RestoreFlashlightDirectly());
            
            Debug.Log("[ThatcherAI] Phase 4: RestoreFlashlightDirectly spuštěno");
        }
        else
        {
            Debug.Log($"[ThatcherAI] Phase 4 SKIPPED - Light system: {(lightSystem != null ? "OK" : "NULL")}, Light on: {(lightSystem != null ? lightSystem.IsLightOn.ToString() : "N/A")}");
        }
        
        Debug.Log("[ThatcherAI] *** FlashlightBatteryDrainEffect DOKONČEN ***");
    }
    
    /// <summary>
    /// Coroutina pro odchod Thatchera s tmavícím efektem
    /// </summary>
    private System.Collections.IEnumerator ThatcherRetreatWithDarknessEffect()
    {
        // Aktivuj tmavící overlay
        if (darknessOverlay != null)
        {
            darknessOverlay.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Darkness effect started for {darknessEffectDuration} seconds");
        }
        
        // Čekej po dobu tmavícího efektu
        yield return new WaitForSeconds(darknessEffectDuration);
        
        // Deaktivuj tmavící overlay
        if (darknessOverlay != null)
        {
            darknessOverlay.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Darkness effect ended");
        }
        
        // Dokončí retreat
        CompleteThatcherRetreat();
    }
    
    /// <summary>
    /// Dokončí odchod Thatchera a resetuje systém
    /// </summary>
    private void CompleteThatcherRetreat()
    {
        // Reset všech systémů
        HideThatcher();
        isThatcherLeaving = false;
        
        // Reset movement timer pro nový cyklus
        movementTimer = movementInterval;
        
        // Označit legitimní zapnutí elektřiny po úspěšné obraně
        legitimateElectricityRestore = true;
        
        // Reset electricity state tracking po úspěšné obraně
        if (lightSystem != null)
        {
            lastElectricityState = lightSystem.IsLightOn;
        }
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Thatcher retreat completed - legitimate electricity restore allowed");
    }
    
    /// <summary>
    /// Sleduje předčasné zapnutí elektřiny během Thatcher encounter
    /// </summary>
    private void HandleElectricityReactivation()
    {
        if (lightSystem == null || !electricityTrackingEnabled) return;
        
        bool currentElectricityState = lightSystem.IsLightOn;
        
        // Detekce zapnutí elektřiny (z vypnuto na zapnuto)
        if (!lastElectricityState && currentElectricityState)
        {
            // Zkontroluj zda je to legitimní zapnutí po úspěšné obraně
            if (legitimateElectricityRestore)
            {
                // Legitimní zapnutí - resetuj flag a neprovádej akci
                legitimateElectricityRestore = false;
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] Legitimate electricity restore after successful defense - no death");
            }
            else
            {
                // Předčasné zapnutí elektřiny během obrany - ZABIJ HRÁČE!
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] ⚠️ PREMATURE ELECTRICITY REACTIVATION - KILLING PLAYER!");
                
                // OKAMŽITĚ force vypni elektřinu ZPĚT (JEŠTĚ PŘED coroutinou!)
                if (electricitySystem != null)
                {
                    electricitySystem.OnLightToggled(false);
                    
                    if (enableDebugLogs)
                        Debug.Log("[ThatcherAI] ✅ IMMEDIATE forced electricity OFF via ElectricityManagement");
                }
                
                // OKAMŽITĚ resetuj Dokki progress (blocks monitor UI)
                if (dokkiAI != null)
                {
                    // Force trigger Unity recompilation
                    dokkiAI.ResetDokkiProgress();
                    
                    if (enableDebugLogs)
                        Debug.Log("[ThatcherAI] ✅ IMMEDIATE Dokki progress reset (blocks monitors)");
                }
                
                // Teprve nyní spusť death sekvenci
                StartCoroutine(TriggerElectricityPenaltyDeath());
            }
        }
        
        // Aktualizuj předchozí stav pouze pokud je tracking aktivní
        lastElectricityState = currentElectricityState;
    }
    
    /// <summary>
    /// Coroutina pro death při předčasném zapnutí elektřiny během Thatcher encounter
    /// Vizuální světla vypne a zabije hráče
    /// ELEKTŘINA A DOKKI JIŽ JSOU FORCE VYPNUTY V HandleElectricityReactivation()!
    /// </summary>
    private System.Collections.IEnumerator TriggerElectricityPenaltyDeath()
    {
        // BLOKOVÁNÍ: Pokud je Thatcher blokovaný (oba animatronici aktivní), NEPOVOLUJ smrt!
        if (isThatcherBlocked)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] 🚫 Electricity penalty death but Thatcher is BLOCKED - death cancelled (Warden has priority)!");
            yield break; // Ukonči coroutine
        }
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] ⚠️ Electricity penalty death - starting death sequence");
        
        // Zastav Thatcher mechaniky
        isThatcherActive = false;
        
        // Utlum ostatní zvuky
        MuteAudioSources();
        
        // OKAMŽITĚ vypni vizuální světla (mainLight/parentLight) se zvukem
        yield return StartCoroutine(FlickerLights(false));
        
        // Počkej 0.9s před zobrazením dead panel
        yield return new WaitForSeconds(lightOffBeforeJumpscare);
        
        // Zobraz dead panel s video delayem (0.1s)
        StartCoroutine(ShowDeadPanelWithVideoDelay());
        
        // Přehraj death zvuk
        PlayDeathSound();
        
        // Reset Thatcher
        HideThatcher();
        
        // Počkej 1 sekundu a zapni světla zpátky
        yield return new WaitForSeconds(lightOnAfterDelay);
        yield return StartCoroutine(FlickerLights(true));
        
        // Návrat do menu po gameOverDelay
        Invoke(nameof(ReturnToMenu), gameOverDelay);
    }
    
    #endregion
    
    #region BothActive System - Double Jumpscare Prevention
    
    /// <summary>
    /// Kontroluje po slezení z kamer, zda jsou oba animatronici spawnutí současně.
    /// Pokud ano, aktivuje blokování Warden dead panelu.
    /// </summary>
    private void CheckBothActiveCondition()
    {
        // Zkontroluj zda jsou oba animatronici aktivní
        bool thatcherActive = isThatcherActive;
        bool wardenActive = (wardenAI != null && wardenAI.IsWardenAttacking);
        
        // DEBUG: Vždy loguj stav obou animatroniků
        if (thatcherActive && wardenActive)
        {
            Debug.Log("[ThatcherAI] 🟠🟠 BOTH SPAWNED - Thatcher: TRUE, Warden: TRUE");
        }
        else if (thatcherActive)
        {
            Debug.Log("[ThatcherAI] 🟡 ONLY THATCHER - Thatcher: TRUE, Warden: FALSE");
        }
        else if (wardenActive)
        {
            Debug.Log("[ThatcherAI] 🔴 ONLY WARDEN - Thatcher: FALSE, Warden: TRUE");
        }
        else
        {
            Debug.Log("[ThatcherAI] ⚪ NONE SPAWNED - Thatcher: FALSE, Warden: FALSE");
        }
        
        if (thatcherActive && wardenActive)
        {
            // Oba jsou spawnutí - aktivuj BothActive systém
            areBothActive = true;
            isMonitoringWardenPanel = true;
            
            // OKAMŽITĚ zablokuj Thatchera (Warden má prioritu)
            isThatcherBlocked = true;
            thatcherBlockTimer = 0f; // Resetuj timer
            
            Debug.Log("[ThatcherAI] 🔴🔴 BOTH ACTIVE DETECTED - Thatcher is now BLOCKED! (Warden has priority)");
            Debug.Log("[ThatcherAI] 🔓 Thatcher can be unblocked by: 1) Flashlight, 2) 4s timeout if lights stay on");
        }
        else
        {
            // Alespoň jeden není spawnutý
            areBothActive = false;
            isMonitoringWardenPanel = false;
            isThatcherBlocked = false;
            thatcherBlockTimer = 0f;
            
            if (enableDebugLogs && (thatcherActive || wardenActive))
                Debug.Log($"[ThatcherAI] Only one active - normal behavior");
        }
    }
    
    /// <summary>
    /// Zpracovává Thatcher block timer - po 4 sekundách odblokuje Thatchera pokud hráč nezhasne světla
    /// </summary>
    private void HandleThatcherBlockTimer()
    {
        // Zkontroluj zda jsou stále oba aktivní
        bool thatcherActive = isThatcherActive;
        bool wardenActive = (wardenAI != null && wardenAI.IsWardenAttacking);
        
        if (!thatcherActive || !wardenActive)
        {
            // Jeden zmizel - vypni BothActive systém
            areBothActive = false;
            isMonitoringWardenPanel = false;
            isThatcherBlocked = false;
            thatcherBlockTimer = 0f;
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] ✅ BothActive DISABLED - One disappeared (Thatcher: {thatcherActive}, Warden: {wardenActive})");
            return;
        }
        
        // Zkontroluj zda jsou světla zapnutá
        bool lightsOn = false;
        if (lightSystem != null)
        {
            lightsOn = lightSystem.IsLightOn;
        }
        
        // Pokud hráč zhasne světla, NEPOVOLUJ odblokování Thatchera
        if (!lightsOn)
        {
            if (enableDebugLogs && thatcherBlockTimer > 0.1f)
                Debug.Log("[ThatcherAI] 💡 Lights OFF - Thatcher block timer PAUSED (správná obrana!)");
            return;
        }
        
        // Světla jsou stále zapnutá - inkrementuj timer
        thatcherBlockTimer += Time.deltaTime;
        
        // Po 4 sekundách odblokuj Thatchera
        if (thatcherBlockTimer >= THATCHER_BLOCK_TIMEOUT)
        {
            isThatcherBlocked = false;
            
            Debug.Log("[ThatcherAI] 🔓 THATCHER UNBLOCKED after 4s timeout! (lights stayed on too long)");
            Debug.Log("[ThatcherAI] ⚠️ Thatcher can now kill! Warden no longer has exclusive priority!");
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro okamžité nastavení BothActive stavu (voláno z jiných AI skriptů, např. WardenAI)
    /// </summary>
    public void SetBothActiveState(bool bothActive, bool thatcherBlocked)
    {
        areBothActive = bothActive;
        isThatcherBlocked = thatcherBlocked;
        
        if (bothActive)
        {
            isMonitoringWardenPanel = true;
            thatcherBlockTimer = 0f;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] SetBothActiveState called - areBothActive: {bothActive}, isThatcherBlocked: {thatcherBlocked}");
    }
    
    /// <summary>
    /// Resetuje BothActive systém (volá se při resetu hry, smrti, 6AM)
    /// </summary>
    private void ResetBothActiveSystem()
    {
        isThatcherBlocked = false;
        thatcherBlockTimer = 0f;
        
        if (areBothActive || isMonitoringWardenPanel)
        {
            areBothActive = false;
            isMonitoringWardenPanel = false;
            
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] BothActive system reset (včetně pending delayed deaths)");
        }
    }
    
    #endregion
    
    #region Eye Warning System Implementation
    
    /// <summary>
    /// Inicializuje materiály očí při startu
    /// </summary>
    private void InitializeEyeMaterials()
    {
        if (!enableEyeWarning) return;
        
        // Získej materiály z cube objektů
        if (leftEyeCube != null)
        {
            Renderer leftRenderer = leftEyeCube.GetComponent<Renderer>();
            if (leftRenderer != null)
            {
                leftEyeMaterial = leftRenderer.material; // Vytvoří kopii materiálu
            }
        }
        
        if (rightEyeCube != null)
        {
            Renderer rightRenderer = rightEyeCube.GetComponent<Renderer>();
            if (rightRenderer != null)
            {
                rightEyeMaterial = rightRenderer.material; // Vytvoří kopii materiálu
            }
        }
        
        // Nastav počáteční barvy
        ResetEyeColors();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Eye warning system initialized");
    }
    
    /// <summary>
    /// Aktualizuje barvu očí podle anger progress
    /// </summary>
    private void UpdateEyeColors()
    {
        if (!enableEyeWarning || (!leftEyeMaterial && !rightEyeMaterial)) return;
        
        // Interpoluj mezi normální a červenou barvou s rychlejší červenou
        // Při 50% času (0.5) bude červená už na 65% mixu
        float acceleratedProgress = Mathf.Pow(eyeAngerProgress, 0.7f);
        Color currentColor = Color.Lerp(normalEyeColor, angryEyeColor, acceleratedProgress);
        
        // Aktualizuj Base Map barvu
        if (leftEyeMaterial != null)
        {
            leftEyeMaterial.color = currentColor;
            // Aktualizuj Emission Map barvu
            leftEyeMaterial.SetColor("_EmissionColor", currentColor * 30);
        }
        
        if (rightEyeMaterial != null)
        {
            rightEyeMaterial.color = currentColor;
            // Aktualizuj Emission Map barvu
            rightEyeMaterial.SetColor("_EmissionColor", currentColor * 30);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Eye colors updated - Progress: {eyeAngerProgress:F2}, Color: {currentColor}");
    }
    
    /// <summary>
    /// Resetuje barvy očí na normální
    /// </summary>
    private void ResetEyeColors()
    {
        eyeAngerProgress = 0f;
        isEyeWarningActive = false;
        UpdateEyeColors();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Eye colors reset to normal");
    }
    
    /// <summary>
    /// Spustí varování očí s danou dobou do maxima
    /// </summary>
    private void StartEyeWarning(float duration)
    {
        if (!enableEyeWarning) return;
        
        isEyeWarningActive = true;
        eyeAngerProgress = 0f;
        
        StartCoroutine(EyeWarningCoroutine(duration));
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Eye warning started - Duration: {duration}s");
    }
    
    /// <summary>
    /// Coroutina pro plynulé zbarvování očí
    /// </summary>
    private System.Collections.IEnumerator EyeWarningCoroutine(float duration)
    {
        float timer = 0f;
        
        while (timer < duration && isEyeWarningActive)
        {
            timer += Time.deltaTime;
            eyeAngerProgress = Mathf.Clamp01(timer / duration);
            
            UpdateEyeColors();
            
            yield return null;
        }
        
        // Zajisti, že oči jsou plně červené na konci
        if (isEyeWarningActive)
        {
            eyeAngerProgress = 1f;
            UpdateEyeColors();
        }
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Eye warning completed");
    }
    
    /// <summary>
    /// Zastaví varování očí
    /// </summary>
    private void StopEyeWarning()
    {
        isEyeWarningActive = false;
        StopCoroutine("EyeWarningCoroutine");
        ResetEyeColors();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Eye warning stopped");
    }
    
    #endregion
    
    #region Advanced AI System Implementation
    
    /// <summary>
    /// Inicializace pokročilého AI systému
    /// </summary>
    private void InitializeAdvancedAISystem()
    {
        // Najdi potřebné reference pokud nejsou přiřazeny
        if (shiftTimer == null)
            shiftTimer = FindFirstObjectByType<ShiftTimer>();
            
        if (nightManagement == null)
            nightManagement = FindFirstObjectByType<NightShiftManagement>();
        
        // Validace referencí
        if (shiftTimer == null)
        {
            Debug.LogWarning("[ThatcherAI] ShiftTimer reference not found! Advanced AI system may not work properly.");
        }
        
        if (nightManagement == null)
        {
            Debug.LogWarning("[ThatcherAI] NightShiftManagement reference not found! Advanced AI system may not work properly.");
        }
        
        // Resetuj AI tracking variables
        lastHour = -1;
        lastNight = -1;
        aiUpdateTimer = 0f;
        
        // Nastav počáteční AI level
        UpdateAILevelBasedOnTime();
        
        // Aktualizuj display hodnoty
        UpdateInspectorDisplayValues();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Advanced AI System initialized");
    }
    
    /// <summary>
    /// Zpracovává aktualizace AI levelu v reálném čase
    /// </summary>
    private void HandleAILevelUpdates()
    {
        aiUpdateTimer += Time.deltaTime;
        
        if (aiUpdateTimer >= aiUpdateInterval)
        {
            aiUpdateTimer = 0f;
            UpdateAILevelBasedOnTime();
        }
    }
    
    /// <summary>
    /// Aktualizuje AI level podle aktuálního času a noci
    /// </summary>
    private void UpdateAILevelBasedOnTime()
    {
        int currentNight = GetCurrentNight();
        int currentHour = GetCurrentHour();
        
        // Kontrola, zda došlo ke změně času nebo noci
        if (currentNight != lastNight || currentHour != lastHour)
        {
            lastNight = currentNight;
            lastHour = currentHour;
            
            // Získej nové AI level z nastavení
            int newAILevel = aiSettings.GetAILevel(currentNight, currentHour);
            
            if (newAILevel != currentDynamicAILevel)
            {
                currentDynamicAILevel = newAILevel;
                
                // Aktualizuj display hodnoty v inspektoru
                UpdateInspectorDisplayValues();
                
                if (enableDebugLogs)
                {
                    string timeString = GetTimeString(currentHour);
                    Debug.Log($"[ThatcherAI] AI Level updated: Night {currentNight}, {timeString} -> AI Level {currentDynamicAILevel}");
                }
            }
        }
        
        // Aktualizuj display hodnoty i když se AI nezměnilo (pro změny času/noci)
        if (currentNight != lastNight || currentHour != lastHour)
        {
            UpdateInspectorDisplayValues();
        }
    }
    
    /// <summary>
    /// Získá aktuální noc ze systému
    /// </summary>
    private int GetCurrentNight()
    {
        if (nightManagement != null)
        {
            return nightManagement.GetCurrentNight();
        }
        
        // Fallback na noc 1 pokud není reference
        return 1;
    }
    
    /// <summary>
    /// Získá aktuální herní hodinu ze shift timeru
    /// </summary>
    private int GetCurrentHour()
    {
        if (shiftTimer == null)
            return 0; // Fallback na 12 AM
        
        // Získej timer ze ShiftTimer skriptu pomocí reflexe (protože Timer je private)
        var timerField = typeof(ShiftTimer).GetField("Timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (timerField != null)
        {
            float timer = (float)timerField.GetValue(shiftTimer);
            int hour = Mathf.FloorToInt(timer / 60f); // Převod minut na hodiny
            
            // Omez na rozsah 0-5 (12 AM - 5 AM)
            return Mathf.Clamp(hour, 0, 5);
        }
        
        // Fallback pokud reflexe selže
        return 0;
    }
    
    /// <summary>
    /// Převede herní hodinu na čitelný string
    /// </summary>
    private string GetTimeString(int hour)
    {
        switch (hour)
        {
            case 0: return "12 AM";
            case 1: return "1 AM";
            case 2: return "2 AM";
            case 3: return "3 AM";
            case 4: return "4 AM";
            case 5: return "5 AM";
            default: return "Unknown";
        }
    }
    
    /// <summary>
    /// Získá efektivní AI level - buď z pokročilého systému nebo z manuálního nastavení
    /// </summary>
    private int GetEffectiveAILevel()
    {
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightShiftManagement != null && nightShiftManagement.IsInMainMenu())
        {
            return 0;
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightShiftManagement != null && nightShiftManagement.IsCustomNightActive())
        {
            int customAI = nightShiftManagement.GetCustomNightAI("THATCHER");
            if (customAI >= 0)
                return customAI;
        }
        
        if (useAdvancedAISystem)
        {
            return currentDynamicAILevel;
        }
        else
        {
            return aiLevel; // Použij manuální AI level
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro získání aktuálního AI levelu (pro externí použití)
    /// </summary>
    public int GetCurrentAILevel()
    {
        return GetEffectiveAILevel();
    }
    
    /// <summary>
    /// Veřejná metoda pro manuální nastavení AI levelu pro konkrétní noc a čas
    /// </summary>
    public void SetAILevelForNightAndTime(int night, int hour, int aiLevel)
    {
        if (night < 1 || night > 6)
        {
            Debug.LogWarning($"[ThatcherAI] Invalid night: {night}. Must be between 1-6.");
            return;
        }
        
        if (hour < 0 || hour > 5)
        {
            Debug.LogWarning($"[ThatcherAI] Invalid hour: {hour}. Must be between 0-5 (12 AM - 5 AM).");
            return;
        }
        
        aiLevel = Mathf.Clamp(aiLevel, 0, 20);
        
        HourlyAISettings nightSettings = aiSettings.GetSettingsForNight(night);
        
        switch (hour)
        {
            case 0: nightSettings.hour12AM = aiLevel; break;
            case 1: nightSettings.hour1AM = aiLevel; break;
            case 2: nightSettings.hour2AM = aiLevel; break;
            case 3: nightSettings.hour3AM = aiLevel; break;
            case 4: nightSettings.hour4AM = aiLevel; break;
            case 5: nightSettings.hour5AM = aiLevel; break;
        }
        
        if (enableDebugLogs)
        {
            string timeString = GetTimeString(hour);
            Debug.Log($"[ThatcherAI] Manually set AI Level: Night {night}, {timeString} -> AI Level {aiLevel}");
        }
        
        // Aktualizuj současné AI pokud se jedná o aktuální čas
        if (GetCurrentNight() == night && GetCurrentHour() == hour)
        {
            UpdateAILevelBasedOnTime();
        }
    }
    
    /// <summary>
    /// Získá kompletní AI nastavení pro debug účely
    /// </summary>
    public string GetAISettingsDebugInfo()
    {
        var debug = new System.Text.StringBuilder();
        debug.AppendLine("=== ThatcherAI Settings ===");
        debug.AppendLine($"Advanced AI System: {(useAdvancedAISystem ? "ON" : "OFF")}");
        debug.AppendLine($"Real-time Updates: {(enableRealTimeAIUpdates ? "ON" : "OFF")}");
        debug.AppendLine($"Update Interval: {aiUpdateInterval}s");
        debug.AppendLine($"Current AI Level: {GetEffectiveAILevel()}");
        debug.AppendLine($"Manual AI Level: {aiLevel}");
        debug.AppendLine($"Current Night: {GetCurrentNight()}");
        debug.AppendLine($"Current Hour: {GetTimeString(GetCurrentHour())}");
        debug.AppendLine();
        
        for (int night = 1; night <= 6; night++)
        {
            debug.AppendLine($"Night {night}:");
            HourlyAISettings settings = aiSettings.GetSettingsForNight(night);
            debug.AppendLine($"  12 AM: {settings.hour12AM}");
            debug.AppendLine($"  1 AM:  {settings.hour1AM}");
            debug.AppendLine($"  2 AM:  {settings.hour2AM}");
            debug.AppendLine($"  3 AM:  {settings.hour3AM}");
            debug.AppendLine($"  4 AM:  {settings.hour4AM}");
            debug.AppendLine($"  5 AM:  {settings.hour5AM}");
            debug.AppendLine();
        }
        
        return debug.ToString();
    }
    
    /// <summary>
    /// Přepne pokročilý AI systém ON/OFF
    /// </summary>
    public void ToggleAdvancedAISystem()
    {
        useAdvancedAISystem = !useAdvancedAISystem;
        
        // Aktualizuj display hodnoty
        UpdateInspectorDisplayValues();
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Advanced AI System: {(useAdvancedAISystem ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Rychlé nastavení přednastavených AI konfigurací
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
                    Debug.LogWarning($"[ThatcherAI] Unknown preset: {presetName}");
                break;
        }
    }
    
    /// <summary>
    /// Přednastavení pro lehkou obtížnost
    /// </summary>
    private void LoadEasyPreset()
    {
        // Noc 1: postupný nárůst od 0 do 2
        SetMultipleAILevels(1, new int[] { 0, 0, 1, 1, 2, 2 });
        // Noc 2: mírný nárůst
        SetMultipleAILevels(2, new int[] { 0, 1, 1, 2, 3, 3 });
        // Noc 3: střední obtížnost
        SetMultipleAILevels(3, new int[] { 1, 2, 3, 4, 5, 5 });
        // Noc 4: vyšší obtížnost
        SetMultipleAILevels(4, new int[] { 2, 3, 4, 6, 7, 8 });
        // Noc 5: vysoká obtížnost
        SetMultipleAILevels(5, new int[] { 3, 5, 7, 9, 11, 12 });
        // Noc 6: extrémní obtížnost
        SetMultipleAILevels(6, new int[] { 5, 8, 10, 13, 15, 17 });
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Loaded EASY preset configuration");
    }
    
    /// <summary>
    /// Přednastavení pro normální obtížnost
    /// </summary>
    private void LoadNormalPreset()
    {
        // Noc 1: postupný nárůst
        SetMultipleAILevels(1, new int[] { 0, 1, 2, 3, 4, 5 });
        // Noc 2: mírné zvýšení
        SetMultipleAILevels(2, new int[] { 1, 2, 4, 5, 6, 7 });
        // Noc 3: střední obtížnost
        SetMultipleAILevels(3, new int[] { 2, 4, 6, 7, 8, 9 });
        // Noc 4: vyšší obtížnost
        SetMultipleAILevels(4, new int[] { 3, 6, 8, 10, 12, 13 });
        // Noc 5: vysoká obtížnost
        SetMultipleAILevels(5, new int[] { 5, 8, 11, 13, 16, 18 });
        // Noc 6: maximální obtížnost
        SetMultipleAILevels(6, new int[] { 8, 12, 15, 17, 19, 20 });
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Loaded NORMAL preset configuration");
    }
    
    /// <summary>
    /// Přednastavení pro těžkou obtížnost
    /// </summary>
    private void LoadHardPreset()
    {
        // Noc 1: rychlejší nárůst
        SetMultipleAILevels(1, new int[] { 1, 3, 5, 6, 7, 8 });
        // Noc 2: vyšší počáteční AI
        SetMultipleAILevels(2, new int[] { 3, 5, 7, 9, 10, 11 });
        // Noc 3: vysoká obtížnost
        SetMultipleAILevels(3, new int[] { 5, 8, 10, 12, 14, 15 });
        // Noc 4: velmi vysoká obtížnost
        SetMultipleAILevels(4, new int[] { 7, 10, 13, 15, 17, 18 });
        // Noc 5: extrémní obtížnost
        SetMultipleAILevels(5, new int[] { 10, 13, 16, 18, 19, 20 });
        // Noc 6: maximální obtížnost po celou dobu
        SetMultipleAILevels(6, new int[] { 15, 17, 19, 20, 20, 20 });
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Loaded HARD preset configuration");
    }
    
    /// <summary>
    /// Přednastavení pro nightmare obtížnost
    /// </summary>
    private void LoadNightmarePreset()
    {
        // Všechny noci mají extrémně vysoké AI
        for (int night = 1; night <= 6; night++)
        {
            SetMultipleAILevels(night, new int[] { 15, 17, 18, 19, 20, 20 });
        }
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Loaded NIGHTMARE preset configuration");
    }
    
    /// <summary>
    /// Pomocná metoda pro nastavení AI pro všechny hodiny v noci
    /// </summary>
    private void SetMultipleAILevels(int night, int[] aiLevels)
    {
        if (aiLevels.Length != 6)
        {
            Debug.LogError("[ThatcherAI] AI levels array must contain exactly 6 values for hours 12AM-5AM");
            return;
        }
        
        for (int hour = 0; hour < 6; hour++)
        {
            SetAILevelForNightAndTime(night, hour, aiLevels[hour]);
        }
    }
    
    /// <summary>
    /// Debug metoda pro zobrazení všech appearance šancí
    /// </summary>
    public string GetAppearanceChancesDebugInfo()
    {
        var debug = new System.Text.StringBuilder();
        debug.AppendLine("=== Thatcher Appearance Chances (Exponential) ===");
        debug.AppendLine("AI Level → Spawn Chance:");
        
        for (int ai = 0; ai <= 20; ai++)
        {
            float chance = CalculateExponentialAppearanceChance(ai);
            debug.AppendLine($"AI {ai,2} → {chance,5:F1}%");
        }
        
        debug.AppendLine();
        debug.AppendLine("Current Settings:");
        debug.AppendLine($"Current AI: {GetEffectiveAILevel()}");
        debug.AppendLine($"Current Chance: {GetAppearanceChancePercent():F1}%");
        
        return debug.ToString();
    }
    
    /// <summary>
    /// Aktualizuje hodnoty zobrazované v inspektoru
    /// </summary>
    private void UpdateInspectorDisplayValues()
    {
        // Aktualizuj efektivní AI level
        currentEffectiveAI = GetEffectiveAILevel();
        
        // Aktualizuj aktuální noc
        currentNightDisplay = GetCurrentNight();
        
        // Aktualizuj aktuální čas
        currentTimeDisplay = GetTimeString(GetCurrentHour());
        
        // Aktualizuj status systému
        if (useAdvancedAISystem)
        {
            systemStatusDisplay = enableRealTimeAIUpdates ? "Advanced AI (Real-time)" : "Advanced AI (Static)";
        }
        else
        {
            systemStatusDisplay = "Manual AI";
        }
    }
    
    #endregion
    
    #region Character Interaction System
    
    /// <summary>
    /// Univerzální detekce smrti od jakékoliv postavy
    /// </summary>
    private void CheckForAnyCharacterDeath()
    {
        // Prozatím jednoduché řešení - disable to dokud nebudeme mít správnou detekci
        // TODO: Implementovat detekci přes Dead Panel monitoring nebo Game State
        return;
    }
    
    /// <summary>
    /// Kontroluje zda Zofaye zabila hráče a resetuje Thatcher
    /// </summary>
    private void CheckZofayeDeathAndReset()
    {
        // Bezpečnostní null check a aktivní check
        if (deadPanelZofeye == null || !this.enabled || !this.gameObject.activeInHierarchy) 
            return;
            
        // Detekce Zofaye smrti pomocí jejího dead panelu
        if (deadPanelZofeye.activeSelf)
        {
            // Zofaye zabila hráče - resetuj Thatcher
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Zofaye killed player - resetting Thatcher");
            
            // Resetuj Thatcher
            ResetAfterZofayeDeath();
        }
    }
    
    /// <summary>
    /// Resetuje Thatcher po smrti od Zofaye (volané z ZofeyeAI)
    /// </summary>
    public void ResetAfterZofayeDeath()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Resetting Thatcher after Zofaye death");
        
        // 1. Deaktivuj Thatcher cube
        if (thatcherCube != null && isThatcherActive)
        {
            thatcherCube.SetActive(false);
            isThatcherActive = false;
        }
        
        // 2. Reset všechny timery
        movementTimer = movementInterval;
        defenseTimer = 0f;
        lightTimeoutTimer = 0f;
        
        // 3. Reset flags
        isDefenseActive = false;
        isFlashlightPointingAtThatcher = false;
        wasOnCamerasLastFrame = false;
        isLightTimeoutActive = false;
        isThatcherLeaving = false;
        
        // 4. Reset ocí zpět na normální barvu
        if (enableEyeWarning)
        {
            ResetEyeColors();
        }
        
        // 5. Reset tmavícího efektu
        if (enableDarknessEffect && darknessOverlay != null)
        {
            darknessOverlay.SetActive(false);
        }
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Thatcher reset complete - all timers and flags reset");
    }
    
    /// <summary>
    /// Reset po smrti od jakékoliv postavy
    /// </summary>
    public void ResetAfterAnyCharacterDeath()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Universal character death reset");
        
        // Reset BothActive systému
        ResetBothActiveSystem();
        
        // Reset camera time accumulation při smrti jakékoliv postavy
        ResetCameraTimeAccumulation();
        
        // Použij stejnou logiku jako reset po Zofaye
        ResetAfterZofayeDeath();
    }
    
    /// <summary>
    /// Veřejný reset pro externí volání (6AM, ESC menu, atd.)
    /// </summary>
    public void ResetThatcherState()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] External reset call - stopping all coroutines");
        
        // KRITICKÉ: Zastav VŠECHNY coroutiny (včetně útočných timerů)
        StopAllCoroutines();
        
        // Reset BothActive systému
        ResetBothActiveSystem();
        
        // Reset camera time accumulation při resetu noci
        ResetCameraTimeAccumulation();
        
        ResetAfterZofayeDeath();
    }
    
    /// <summary>
    /// Reset při konci noci (6AM) - KRITICKÉ pro zastavení všech útočných mechanismů
    /// </summary>
    public void OnNightEnd()
    {
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] ===== NIGHT END RESET - STOPPING ALL ATTACK MECHANISMS =====");
        
        // KRITICKÉ: Zastav VŠECHNY coroutiny (včetně útočných timerů)
        StopAllCoroutines();
        
        // Reset BothActive systému
        ResetBothActiveSystem();
        
        // Reset všech attack/defense timerů
        movementTimer = movementInterval;
        defenseTimer = 0f;
        lightTimeoutTimer = 0f;
        
        // Reset všech attack flags
        isDefenseActive = false;
        isFlashlightPointingAtThatcher = false;
        wasOnCamerasLastFrame = false;
        isLightTimeoutActive = false;
        isThatcherLeaving = false;
        
        // Deaktivuj Thatcher cube
        if (thatcherCube != null && isThatcherActive)
        {
            thatcherCube.SetActive(false);
            isThatcherActive = false;
        }
        
        // Reset očí a darkness efektu
        if (enableEyeWarning)
        {
            ResetEyeColors();
        }
        
        if (enableDarknessEffect && darknessOverlay != null)
        {
            darknessOverlay.SetActive(false);
        }
        
        // Reset camera time accumulation
        ResetCameraTimeAccumulation();
        
        if (enableDebugLogs)
            Debug.Log("[ThatcherAI] Night end reset complete - all attack mechanisms stopped");
    }
    
    #endregion
    
    #region Camera Time Accumulation System
    
    /// <summary>
    /// Zpracovává akumulaci času stráveného na kamerách
    /// VÝJIMKA: Pokud AI = 0, žádná akumulace se nepočítá!
    /// </summary>
    private void HandleCameraTimeAccumulation(bool currentCameraState)
    {
        // VÝJIMKA: AI 0 = žádná camera time akumulace
        int effectiveAI = GetEffectiveAILevel();
        if (effectiveAI <= 0)
        {
            // Reset akumulace pokud AI kleslo na 0 během hry
            if (cameraTimeAccumulated > 0f || cameraTimeBonusChance > 0f)
            {
                if (enableDebugLogs)
                    Debug.Log("[ThatcherAI] AI = 0 - resetting camera time accumulation");
                ResetCameraTimeAccumulation();
            }
            return; // Žádná akumulace při AI 0
        }
        
        // Detekuj přechod z "ne na kamerách" na "na kamerách" = začátek sledování
        if (!wasOnCamerasLastUpdate && currentCameraState)
        {
            if (enableDebugLogs)
                Debug.Log("[ThatcherAI] Player started watching cameras - beginning time accumulation");
        }
        
        // Akumuluj čas pokud je hráč na kamerách
        if (currentCameraState)
        {
            cameraTimeAccumulated += Time.deltaTime;
            
            // Vypočítej bonus šanci (každých 5 sekund = +1%)
            float newBonusChance = Mathf.Floor(cameraTimeAccumulated / CAMERA_TIME_BONUS_INTERVAL) * CAMERA_TIME_BONUS_PERCENT;
            
            // Pokud se bonus šance změnila, zaloguj to
            if (newBonusChance != cameraTimeBonusChance && enableDebugLogs)
            {
                Debug.Log($"[ThatcherAI] Camera time bonus updated: {cameraTimeBonusChance:F1}% -> {newBonusChance:F1}% (time: {cameraTimeAccumulated:F1}s)");
            }
            
            cameraTimeBonusChance = newBonusChance;
        }
        
        wasOnCamerasLastUpdate = currentCameraState;
    }
    
    /// <summary>
    /// Resetuje akumulaci času a bonus šanci z kamer
    /// </summary>
    private void ResetCameraTimeAccumulation()
    {
        if (cameraTimeAccumulated > 0f || cameraTimeBonusChance > 0f)
        {
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Resetting camera time accumulation - was {cameraTimeAccumulated:F1}s (bonus: {cameraTimeBonusChance:F1}%)");
        }
        
        cameraTimeAccumulated = 0f;
        cameraTimeBonusChance = 0f;
        wasOnCamerasLastUpdate = false;
    }
    
    /// <summary>
    /// Veřejná metoda pro získání aktuálního camera time bonus (pro debug)
    /// </summary>
    public float GetCameraTimeBonusPercent()
    {
        return cameraTimeBonusChance;
    }
    
    /// <summary>
    /// Veřejná metoda pro získání času stráveného na kamerách (pro debug)
    /// </summary>
    public float GetCameraTimeAccumulated()
    {
        return cameraTimeAccumulated;
    }
    
    #endregion
    
    #region Menu Detection System
    
    /// <summary>
    /// Kontroluje, zda je aktivní nějaký menu panel - AI = 0 když je menu aktivní
    /// </summary>
    bool IsMenuActive()
    {
        // Bezpečnostní kontroly
        if (!enableMenuDetection)
            return false;
            
        if (menuPanels == null)
        {
            // Inicializuj prázdný array pokud je null
            menuPanels = new GameObject[0];
            return false;
        }
        
        if (menuPanels.Length == 0)
            return false;
        
        // Kontrola každého menu panelu
        try
        {
            foreach (GameObject menuPanel in menuPanels)
            {
                if (menuPanel != null && menuPanel.activeInHierarchy)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[ThatcherAI] Menu panel '{menuPanel.name}' is active - AI disabled");
                    return true;
                }
            }
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
                Debug.LogError($"[ThatcherAI] Error in IsMenuActive: {ex.Message}");
            return false;
        }
        
        return false; // Žádné menu není aktivní
    }
    
    /// <summary>
    /// Veřejná metoda pro přidání menu panelu do sledování
    /// </summary>
    public void AddMenuPanel(GameObject panel)
    {
        if (panel == null) 
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ThatcherAI] Cannot add null panel to menu detection");
            return;
        }
        
        // Inicializuj array pokud je null
        if (menuPanels == null)
        {
            menuPanels = new GameObject[0];
        }
        
        // Kontrola zda panel už není v seznamu
        for (int i = 0; i < menuPanels.Length; i++)
        {
            if (menuPanels[i] == panel)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ThatcherAI] Panel '{panel.name}' is already being monitored");
                return; // Panel už je v seznamu
            }
        }
        
        // Přidej panel do array
        try
        {
            GameObject[] newArray = new GameObject[menuPanels.Length + 1];
            for (int i = 0; i < menuPanels.Length; i++)
            {
                newArray[i] = menuPanels[i];
            }
            newArray[menuPanels.Length] = panel;
            menuPanels = newArray;
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Added menu panel '{panel.name}' to monitoring");
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
                Debug.LogError($"[ThatcherAI] Error adding menu panel: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro odebrání menu panelu ze sledování
    /// </summary>
    public void RemoveMenuPanel(GameObject panel)
    {
        if (panel == null || menuPanels == null) return;
        
        // Najdi index panelu
        int indexToRemove = -1;
        for (int i = 0; i < menuPanels.Length; i++)
        {
            if (menuPanels[i] == panel)
            {
                indexToRemove = i;
                break;
            }
        }
        
        if (indexToRemove >= 0)
        {
            // Vytvoř nový array bez tohoto panelu
            GameObject[] newArray = new GameObject[menuPanels.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < menuPanels.Length; i++)
            {
                if (i != indexToRemove)
                {
                    newArray[newIndex] = menuPanels[i];
                    newIndex++;
                }
            }
            menuPanels = newArray;
            
            if (enableDebugLogs)
                Debug.Log($"[ThatcherAI] Removed menu panel '{panel.name}' from monitoring");
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro zapnutí/vypnutí menu detection
    /// </summary>
    public void SetMenuDetectionEnabled(bool enabled)
    {
        enableMenuDetection = enabled;
        
        if (enableDebugLogs)
            Debug.Log($"[ThatcherAI] Menu detection {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Veřejná metoda pro získání seznamu menu panelů (pro debug)
    /// </summary>
    public string GetMenuPanelsDebugInfo()
    {
        if (menuPanels == null || menuPanels.Length == 0)
            return "[ThatcherAI] No menu panels configured";
        
        var debug = new System.Text.StringBuilder();
        debug.AppendLine($"[ThatcherAI] Menu Detection: {(enableMenuDetection ? "ON" : "OFF")}");
        debug.AppendLine($"Monitored panels ({menuPanels.Length}):");
        
        for (int i = 0; i < menuPanels.Length; i++)
        {
            GameObject panel = menuPanels[i];
            if (panel != null)
            {
                string status = panel.activeInHierarchy ? "ACTIVE" : "inactive";
                debug.AppendLine($"  {i + 1}. {panel.name} - {status}");
            }
            else
            {
                debug.AppendLine($"  {i + 1}. [NULL PANEL]");
            }
        }
        
        return debug.ToString();
    }
    
    #endregion
}