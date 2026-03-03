using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SledgeNightConfiguration
{
    [Header("Night Configuration")]
    public int night = 1;
    
    [Header("AI Levels by Hours (0-6 AM)")]
    [Range(0, 20)] public int hour12AM = 0;
    [Range(0, 20)] public int hour1AM = 0;
    [Range(0, 20)] public int hour2AM = 0;
    [Range(0, 20)] public int hour3AM = 0;
    [Range(0, 20)] public int hour4AM = 0;
    [Range(0, 20)] public int hour5AM = 0;
    [Range(0, 20)] public int hour6AM = 0;
    
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
            6 => hour6AM,
            _ => 0
        };
    }
}

public class SledgeAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("=== SLEDGE AI SYSTEM ===")]
    [SerializeField] private bool enableSledgeAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Sledge Positions (11 Positions + GLASS Transform)")]
    [SerializeField] private GameObject sledgeStorage1;      // 1. Storage 1 (Start)
    [SerializeField] private GameObject sledgeStorage2;      // 2. Storage 2
    [SerializeField] private GameObject sledgeCam03;         // 3. CAM 03
    [SerializeField] private GameObject sledgeCam07;         // 4. CAM 07
    [SerializeField] private GameObject sledgeCam11;         // 5. CAM 11
    [SerializeField] private GameObject sledgeCam08;         // 6. CAM 08
    [SerializeField] private GameObject sledgeCam02;         // 7. CAM 02
    [SerializeField] private GameObject sledgeGlass;         // 8&9. GLASS GameObject (moves between positions)
    [SerializeField] private GameObject sledgeCam06;         // 10. CAM 06 (Cesta C)
    [SerializeField] private GameObject sledgeCam04;         // 11. CAM 04
    [SerializeField] private GameObject sledgeCam04Generator; // 12. CAM 04 Generator (Attack position)
    
    [Header("GLASS Position Settings")]
    [SerializeField] private Vector3 glassPosition1 = Vector3.zero; // GLASS 1 position (Starting position)
    // Note: glassPosition2 not needed - GLASS moves by -10 units in X-axis from glassPosition1
    
    [Header("Movement System")]
    [SerializeField] private float movementInterval = 5f;
    [SerializeField] private bool enableMovementSystem = true;
    
    [Header("Generator Attack System")]
    [SerializeField] private float attackDuration = 10f; // 10 seconds to defend
    [SerializeField] private Vector3 generatorAttackRotationX = Vector3.zero; // Original rotation
    [SerializeField] private Vector3 generatorAttackRotationY = new Vector3(0, 180, 0); // Attack rotation
    
    [Header("Special Movement Settings")]
    [Header("GLASS Movement Settings")]
    [SerializeField] private float glass1ToGlass2Speed = 2f; // Speed for X-axis movement (-10 units)
    [SerializeField] private float generatorRotationSpeed = 45f; // Degrees per second
    
    [Header("Temporary AI Disable")]
    [SerializeField] private float temporaryDisableDuration = 30f; // 30 seconds AI=0 after failed attack
    
    [Header("System References")]
    [SerializeField] private CameraSystem cameraSystem;
    [SerializeField] private CameryVentSystem ventCameraSystem;
    [SerializeField] private ElectricityManagement electricitySystem;
    [SerializeField] private FenerirAI fenerirAI;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private ResetNight resetNight;
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private ShockingSystem shockingSystem;
    [SerializeField] private Svetlo_Strop_Kancelar_Tlacitko lightSystem;
    [SerializeField] private ThatcherAI thatcherAI;
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pausing
    [SerializeField] private ZofeyeAI zofeyeAI;
    
    [Header("Camera System References")]
    [SerializeField] private GameObject camsOffButton; // Active when player watching cameras
    [SerializeField] private GameObject camsOnButton;  // Active when player in office
    
    [Header("UI Panels & Audio")]
    [SerializeField] private GameObject sledgeMovePanel;
    [SerializeField] private AudioSource errorCamAudioSource;
    [SerializeField] private AudioClip errorCamSound;
    [SerializeField] private AudioSource destructionAudioSource;
    [SerializeField] private AudioClip generatorDestructionSound;
    [SerializeField] private AudioSource metalDetectorAudioSource;
    [SerializeField] private AudioClip metalDetectorSound;
    
    [Header("Walking Audio System")]
    [SerializeField] private AudioSource walkingByAudioSource; // AudioSource pro walking by sound
    [SerializeField] private AudioClip walkingBySound; // Zvuk při přesunu GLASS 1 → GLASS 2
    [SerializeField] private AudioSource walkingOffAudioSource; // AudioSource pro walking off sound
    [SerializeField] private AudioClip walkingOffSound; // Zvuk při úspěšné obraně
    [SerializeField] private float cameraVolumeReductionWalking = 0.3f; // Snížení walking by zvuku při kamerách (70% snížení = 30%)
    [SerializeField] private float startingVolumePercent = 0.2f; // Počáteční hlasitost (20%)
    [SerializeField] private float maxVolumePercent = 1.0f; // Maximální hlasitost v polovině cesty (100%)
    
    [Header("Advanced AI System - Night & Time")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private SledgeNightConfiguration[] aiSettings = new SledgeNightConfiguration[6];
    
    [Header("Menu Detection System")]
    [SerializeField] private GameObject[] menuPanels = new GameObject[0];
    [SerializeField] private bool enableMenuDetection = true;
    
    [Header("Current Status - READ ONLY")]
    [SerializeField] private int currentPosition = 1; // 1-12
    [SerializeField] private int currentEffectiveAI = 0;
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private bool isTemporarilyDisabled = false;
    [SerializeField] private float attackTimer = 0f;
    [SerializeField] private float temporaryDisableTimer = 0f;
    [SerializeField] private string systemStatus = "Inactive";
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Private Variables
    
    private bool isSledgeActive = false;
    private bool menuIsActive = false;
    private float movementTimer = 0f;
    private bool isMovingGlass1ToGlass2 = false;
    private bool isRotatingAtGenerator = false;
    private bool wasThatcherActiveBeforeSledge = false;
    private int originalThatcherAI = 0;
    
    // Walking audio management
    private float originalWalkingByVolume = 1f; // Původní hlasitost walking by sound
    private bool isWalkingByPlaying = false; // Flag zda se přehrává walking by sound
    
    // Generator destruction tracking
    private bool isGeneratorDestroyed = false; // KRITICKÝ FLAG: True pokud Sledge zničil generátor
    
    // Movement paths dictionary
    private Dictionary<int, int[]> possibleMoves;
    
    // Route tracking system for varied path selection
    private int lastUsedRoute = -1; // Track which route was used last time (-1 = none, 0 = Route A, 1 = Route B, 2 = Route C)
    private int currentRouteInProgress = -1; // Track current route being executed
    private Dictionary<int, float> routeFatigueMultipliers; // Reduced chances for recently used routes
    
    // Route definitions for CAM 07 (position 4) branching
    private enum RouteType
    {
        RouteA = 0, // CAM 07 → CAM 11 → CAM 02 → GLASS → CAM 04
        RouteB = 1, // CAM 07 → CAM 08 → CAM 04  
        RouteC = 2  // CAM 07 → CAM 08 → CAM 06 → CAM 04
    }
    
    // Coroutines
    private Coroutine glass1ToGlass2Coroutine;
    private Coroutine generatorRotationCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine temporaryDisableCoroutine;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeSledgeSystem();
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] System initialized - Sledge ready");
    }
    
    void Update()
    {
        if (!enableSledgeAI || !this.enabled || !this.gameObject.activeInHierarchy) 
            return;
        
        // CHECK FOR WARDEN - PAUSE ALL TIMERS IF WARDEN IS ATTACKING
        if (wardenAI != null && wardenAI.IsWardenActive())
            return;
        
        // Check menu status
        CheckMenuStatus();
        
        // Main AI logic
        if (isSledgeActive && !isTemporarilyDisabled)
        {
            HandleSledgeBehavior();
        }
        
        // Handle temporary disable timer
        if (isTemporarilyDisabled)
        {
            HandleTemporaryDisable();
        }
        
        // Update walking audio based on camera state
        UpdateWalkingAudioVolume();
        
        // Update display values
        UpdateDisplayValues();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeSledgeSystem()
    {
        // Initialize movement paths
        InitializeMovementPaths();
        
        // Initialize route tracking system
        InitializeRouteTracking();
        
        // Initialize audio sources
        InitializeAudioSources();
        
        // Store original positions
        StoreOriginalPositions();
        
        // Calculate AI values
        CalculateAITimings();
        
        // Start at Storage 1
        SetSledgePosition(1);
        
        // Start activity if AI > 0
        if (GetEffectiveAILevel() > 0)
        {
            StartSledgeActivity();
        }
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Sledge system initialized");
    }
    
    private void InitializeMovementPaths()
    {
        possibleMoves = new Dictionary<int, int[]>
        {
            { 1, new int[] { 2 } },                    // Storage 1 → Storage 2
            { 2, new int[] { 3 } },                    // Storage 2 → CAM 03
            { 3, new int[] { 4 } },                    // CAM 03 → CAM 07
            { 4, new int[] { 5, 6 } },                 // CAM 07 → CAM 11 or CAM 08
            { 5, new int[] { 7 } },                    // CAM 11 → CAM 02
            { 6, new int[] { 10, 11 } },               // CAM 08 → CAM 06 (Cesta C) or CAM 04 (Cesta B)
            { 7, new int[] { 8 } },                    // CAM 02 → GLASS 1
            { 8, new int[] { 9 } },                    // GLASS 1 → GLASS 2 (visible movement)
            { 9, new int[] { 11 } },                   // GLASS 2 → CAM 04 (100% chance)
            { 10, new int[] { 11 } },                  // CAM 06 → CAM 04 (Cesta C)
            { 11, new int[] { 12 } },                  // CAM 04 → CAM 04 Generator
            { 12, new int[] { } }                      // CAM 04 Generator (attack position)
        };
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Movement paths initialized with 3 routes to CAM 04");
    }
    
    private void InitializeRouteTracking()
    {
        // Initialize route fatigue system
        routeFatigueMultipliers = new Dictionary<int, float>
        {
            { 0, 1.0f }, // Route A: Normal chance initially
            { 1, 1.0f }, // Route B: Normal chance initially  
            { 2, 1.0f }  // Route C: Normal chance initially
        };
        
        lastUsedRoute = -1; // No route used yet
        currentRouteInProgress = -1; // No route in progress
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Route tracking system initialized - all routes start with equal chances");
    }
    
    private void InitializeAudioSources()
    {
        if (errorCamAudioSource == null)
            errorCamAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (destructionAudioSource == null)
            destructionAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (metalDetectorAudioSource == null)
            metalDetectorAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (walkingByAudioSource == null)
            walkingByAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (walkingOffAudioSource == null)
            walkingOffAudioSource = gameObject.AddComponent<AudioSource>();
        
        // Configure audio sources
        errorCamAudioSource.playOnAwake = false;
        destructionAudioSource.playOnAwake = false;
        metalDetectorAudioSource.playOnAwake = false;
        walkingByAudioSource.playOnAwake = false;
        walkingOffAudioSource.playOnAwake = false;
        
        // Ulož původní hlasitost walking by sound
        if (walkingByAudioSource != null)
        {
            originalWalkingByVolume = walkingByAudioSource.volume;
        }
    }
    
    private void StoreOriginalPositions()
    {
        // Store GLASS original position (position 1) for reset
        if (sledgeGlass != null)
        {
            glassPosition1 = sledgeGlass.transform.position;
        }
        
        // Store generator original rotation
        if (sledgeCam04Generator != null)
        {
            generatorAttackRotationX = sledgeCam04Generator.transform.eulerAngles;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Stored GLASS position 1: {glassPosition1}");
    }
    
    private void CalculateAITimings()
    {
        int aiLevel = GetEffectiveAILevel();
        
        if (aiLevel == 0)
        {
            movementInterval = float.MaxValue;
            return;
        }
        
        // Linear interpolation: AI 1 = 15s, AI 20 = 10s
        float aiRatio = (aiLevel - 1f) / 19f;
        movementInterval = Mathf.Lerp(15f, 10f, aiRatio);
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] AI {aiLevel}: Movement interval {movementInterval:F1}s");
    }
    
    #endregion
    
    #region AI System
    
    public int GetEffectiveAILevel()
    {
        // KRITICKÉ: Pokud je generátor zničený, AI je VŽDY 0 (Sledge se nehýbe)
        if (isGeneratorDestroyed)
        {
            if (enableDebugLogs && Time.frameCount % 300 == 0) // Log každých 5 sekund
                Debug.Log("[SledgeAI] Generator DESTROYED - AI forced to 0 (Sledge frozen at Storage 1)");
            return 0;
        }
        
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return 0; // AI = 0 in menu
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("SLEDGE");
            if (customAI >= 0)
                return customAI;
        }
        
        // Advanced AI system
        if (useAdvancedAISystem && nightManager != null && shiftTimer != null)
        {
            int currentNight = nightManager.GetCurrentNight();
            int currentHour = shiftTimer.GetCurrentHour();
            
            if (currentNight >= 1 && currentNight <= 6 && currentNight <= aiSettings.Length)
            {
                int aiLevel = aiSettings[currentNight - 1].GetAIForHour(currentHour);
                if (enableDebugLogs)
                    Debug.Log($"[SledgeAI] Advanced AI: Night {currentNight}, Hour {currentHour}, AI {aiLevel}");
                return aiLevel;
            }
        }
        
        return manualAILevel;
    }
    
    #endregion
    
    #region Menu Detection
    
    private bool CheckMenuStatus()
    {
        if (!enableMenuDetection || menuPanels == null || menuPanels.Length == 0)
        {
            menuIsActive = false;
            return false;
        }
        
        bool anyMenuActive = false;
        foreach (GameObject panel in menuPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                anyMenuActive = true;
                break;
            }
        }
        
        if (anyMenuActive != menuIsActive)
        {
            menuIsActive = anyMenuActive;
            
            if (menuIsActive)
            {
                // KRITICKÉ: NEDEAKTIVUJ Sledge pokud zničil generátor
                if (!isGeneratorDestroyed)
                {
                    DeactivateSledge();
                    if (enableDebugLogs)
                        Debug.Log("[SledgeAI] Menu activated - Sledge deactivated");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log("[SledgeAI] Menu activated - Sledge remains ACTIVE (generator destroyed - frozen at Storage 1)");
                }
            }
            else
            {
                if (GetEffectiveAILevel() > 0)
                {
                    StartSledgeActivity();
                    if (enableDebugLogs)
                        Debug.Log("[SledgeAI] Menu deactivated - Sledge activated");
                }
            }
        }
        
        return menuIsActive;
    }
    
    #endregion
    
    #region Main Behavior
    
    private void HandleSledgeBehavior()
    {
        if (isAttacking)
        {
            HandleAttackBehavior();
            return;
        }
        
        if (isMovingGlass1ToGlass2 || isRotatingAtGenerator)
        {
            // Wait for special movements to complete
            return;
        }
        
        // Normal movement timer
        movementTimer += Time.deltaTime;
        
        if (movementTimer >= movementInterval)
        {
            AttemptMovement();
            movementTimer = 0f;
        }
    }
    
    private void HandleAttackBehavior()
    {
        attackTimer += Time.deltaTime;
        
        if (attackTimer >= attackDuration)
        {
            // Attack succeeded - destroy generator
            ExecuteGeneratorDestruction();
        }
    }
    
    private void HandleTemporaryDisable()
    {
        temporaryDisableTimer -= Time.deltaTime;
        
        if (temporaryDisableTimer <= 0f)
        {
            // Re-enable Sledge
            isTemporarilyDisabled = false;
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Temporary disable ended - Sledge re-enabled");
        }
    }
    
    #endregion
    
    #region Movement System
    
    private void AttemptMovement()
    {
        int effectiveAI = GetEffectiveAILevel();
        
        if (effectiveAI <= 0)
        {
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] AI is 0 - Sledge inactive");
            return;
        }
        
        // Calculate movement chance based on AI
        float movementChance = CalculateMovementChance(effectiveAI);
        float randomRoll = Random.Range(0f, 100f);
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Movement roll: {randomRoll:F1}% vs {movementChance:F1}% (AI: {effectiveAI}) - Position: {currentPosition}");
        
        if (randomRoll <= movementChance)
        {
            ExecuteMovement();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] Movement failed - staying at position {currentPosition}");
        }
    }
    
    private float CalculateMovementChance(int aiLevel)
    {
        if (aiLevel <= 0) return 0f;
        if (aiLevel >= 20) return 100f;
        
        // Linear scaling: AI 1 = 5%, AI 20 = 100%
        return 5f + ((aiLevel - 1) * (95f / 19f));
    }
    
    private void ExecuteMovement()
    {
        if (!possibleMoves.ContainsKey(currentPosition))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[SledgeAI] No possible moves from position {currentPosition}");
            return;
        }
        
        int[] possiblePositions = possibleMoves[currentPosition];
        
        if (possiblePositions.Length == 0)
        {
            // Reached final position or dead end
            if (currentPosition == 12) // CAM 04 Generator
            {
                StartGeneratorAttack();
            }
            return;
        }
        
        // Special case: GLASS 2 has 100% chance to move to CAM 04
        if (currentPosition == 9)
        {
            MoveToPosition(11); // Force move to CAM 04
            return;
        }
        
        // Choose next position with route intelligence
        int nextPosition;
        
        // Special route selection for route branching points
        if (currentPosition == 4) // CAM 07 - main route selection
        {
            nextPosition = SelectRouteIntelligently(possiblePositions);
        }
        else if (currentPosition == 6) // CAM 08 - Route B vs Route C selection
        {
            nextPosition = SelectBetweenRouteBAndC(possiblePositions);
        }
        else
        {
            // Normal random selection for other positions
            nextPosition = possiblePositions[Random.Range(0, possiblePositions.Length)];
        }
        
        MoveToPosition(nextPosition);
    }
    
    private int SelectRouteIntelligently(int[] possiblePositions)
    {
        // CAM 07 (position 4) has two possible routes:
        // possiblePositions[0] = 5 (CAM 11) = Route A 
        // possiblePositions[1] = 6 (CAM 08) = Route B or C
        
        if (possiblePositions.Length != 2)
        {
            // Fallback to random if not the expected branching point
            return possiblePositions[Random.Range(0, possiblePositions.Length)];
        }
        
        // Calculate weighted chances based on route fatigue
        float routeAChance = 50f * routeFatigueMultipliers[0]; // Route A (CAM 11)
        float routeBCChance = 50f * (routeFatigueMultipliers[1] + routeFatigueMultipliers[2]) / 2f; // Route B+C (CAM 08)
        
        float totalChance = routeAChance + routeBCChance;
        float randomValue = Random.Range(0f, totalChance);
        
        int selectedPosition;
        int selectedRoute;
        
        if (randomValue <= routeAChance)
        {
            // Route A selected: CAM 07 → CAM 11
            selectedPosition = 5; // CAM 11
            selectedRoute = 0; // Route A
            currentRouteInProgress = 0; // Set current route
            
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] Route A selected (CAM 07 → CAM 11) - Chance: {routeAChance:F1}%");
        }
        else
        {
            // Route B or C selected: CAM 07 → CAM 08
            selectedPosition = 6; // CAM 08
            
            // Determine if it will be Route B or C (will be decided at CAM 08)
            float routeBChance = routeFatigueMultipliers[1];
            float routeCChance = routeFatigueMultipliers[2];
            float bcTotal = routeBChance + routeCChance;
            
            if (Random.Range(0f, bcTotal) <= routeBChance)
            {
                selectedRoute = 1; // Route B
                currentRouteInProgress = 1; // Set current route
                if (enableDebugLogs)
                    Debug.Log($"[SledgeAI] Route B pre-selected (CAM 07 → CAM 08 → CAM 04) - Chance: {routeBCChance:F1}%");
            }
            else
            {
                selectedRoute = 2; // Route C  
                currentRouteInProgress = 2; // Set current route
                if (enableDebugLogs)
                    Debug.Log($"[SledgeAI] Route C pre-selected (CAM 07 → CAM 08 → CAM 06 → CAM 04) - Chance: {routeBCChance:F1}%");
            }
        }
        
        // Update route fatigue system
        UpdateRouteFatigue(selectedRoute);
        
        return selectedPosition;
    }
    
    private void UpdateRouteFatigue(int usedRoute)
    {
        // Reduce chance for the route that was just used
        routeFatigueMultipliers[usedRoute] = 0.3f; // 30% of normal chance
        
        // Restore other routes to normal or boost them slightly
        for (int i = 0; i < 3; i++)
        {
            if (i != usedRoute)
            {
                routeFatigueMultipliers[i] = Mathf.Min(1.2f, routeFatigueMultipliers[i] + 0.1f); // Boost unused routes
            }
        }
        
        lastUsedRoute = usedRoute;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SledgeAI] Route fatigue updated:");
            Debug.Log($"  Route A chance: {routeFatigueMultipliers[0] * 100:F0}%");
            Debug.Log($"  Route B chance: {routeFatigueMultipliers[1] * 100:F0}%");
            Debug.Log($"  Route C chance: {routeFatigueMultipliers[2] * 100:F0}%");
        }
    }
    
    private int SelectBetweenRouteBAndC(int[] possiblePositions)
    {
        // CAM 08 (position 6) branches to:
        // possiblePositions[0] = 10 (CAM 06) = Route C continuation
        // possiblePositions[1] = 11 (CAM 04) = Route B continuation
        
        if (possiblePositions.Length != 2)
        {
            // Fallback to random if not expected structure
            return possiblePositions[Random.Range(0, possiblePositions.Length)];
        }
        
        // Use the pre-selected route from CAM 07
        int selectedPosition;
        
        if (currentRouteInProgress == 1) // Route B: Direct to CAM 04
        {
            selectedPosition = 11; // CAM 04
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] Route B continued: CAM 08 → CAM 04 (direct)");
        }
        else if (currentRouteInProgress == 2) // Route C: Via CAM 06
        {
            selectedPosition = 10; // CAM 06
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] Route C continued: CAM 08 → CAM 06 → CAM 04");
        }
        else
        {
            // Fallback: This shouldn't happen if route was pre-selected
            selectedPosition = possiblePositions[Random.Range(0, possiblePositions.Length)];
            if (enableDebugLogs)
                Debug.LogWarning($"[SledgeAI] No pre-selected route! Using random: {selectedPosition}");
        }
        
        return selectedPosition;
    }
    
    private void MoveToPosition(int newPosition)
    {
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Moving from position {currentPosition} to position {newPosition}");
        
        // Special movements
        if (currentPosition == 8 && newPosition == 9)
        {
            // GLASS 1 → GLASS 2 (visible movement)
            StartGlass1ToGlass2Movement();
            return;
        }
        
        if (currentPosition == 11 && newPosition == 12)
        {
            // CAM 04 → CAM 04 Generator (rotation animation)
            // Show moving effect first
            ShowMovingEffect();
            
            StartGeneratorRotation();
            return;
        }
        
        // Special audio: CAM 11 → CAM 02 (Metal detector)
        if (currentPosition == 5 && newPosition == 7)
        {
            PlayMetalDetectorSound();
        }
        
        // Normal teleportation movement
        // Show moving effect (except for GLASS movements and generator rotation)
        bool shouldShowMovingEffect = !(currentPosition == 8 && newPosition == 9) && 
                                     !(currentPosition == 11 && newPosition == 12);
        
        if (shouldShowMovingEffect)
        {
            ShowMovingEffect();
        }
        
        // Handle Thatcher AI disable when reaching CAM 04 or CAM 04 Generator
        if (newPosition == 11 || newPosition == 12)
        {
            DisableThatcherTemporarily();
        }
        
        // Update position
        SetSledgePosition(newPosition);
    }
    
    private void SetSledgePosition(int position)
    {
        // Hide all positions except GLASS (which moves)
        HideAllSledgePositions();
        
        // Show target position
        currentPosition = position;
        GameObject targetPosition = GetPositionGameObject(position);
        
        if (targetPosition != null)
        {
            targetPosition.SetActive(true);
            
            // Special handling for GLASS positions
            if (position == 8) // GLASS 1
            {
                sledgeGlass.SetActive(true); // Activate GLASS object
                sledgeGlass.transform.position = glassPosition1;
                if (enableDebugLogs)
                    Debug.Log("[SledgeAI] GLASS activated and set to position 1 (right side)");
            }
            else if (position == 9) // GLASS 2
            {
                sledgeGlass.SetActive(true); // Activate GLASS object
                // Position 9 uses same base position but moved by -10 in X-axis (after movement)
                Vector3 glass2Position = new Vector3(glassPosition1.x - 10f, glassPosition1.y, glassPosition1.z);
                sledgeGlass.transform.position = glass2Position;
                if (enableDebugLogs)
                    Debug.Log($"[SledgeAI] GLASS activated and set to position 2 (X-10): {glass2Position}");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[SledgeAI] Position GameObject for position {position} is null!");
        }
    }
    
    private GameObject GetPositionGameObject(int position)
    {
        return position switch
        {
            1 => sledgeStorage1,
            2 => sledgeStorage2,
            3 => sledgeCam03,
            4 => sledgeCam07,
            5 => sledgeCam11,
            6 => sledgeCam08,
            7 => sledgeCam02,
            8 => sledgeGlass,        // GLASS 1 position
            9 => sledgeGlass,        // GLASS 2 position (same GameObject)
            10 => sledgeCam06,
            11 => sledgeCam04,
            12 => sledgeCam04Generator,
            _ => null
        };
    }
    
    private void HideAllSledgePositions()
    {
        if (sledgeStorage1 != null) sledgeStorage1.SetActive(false);
        if (sledgeStorage2 != null) sledgeStorage2.SetActive(false);
        if (sledgeCam03 != null) sledgeCam03.SetActive(false);
        if (sledgeCam07 != null) sledgeCam07.SetActive(false);
        if (sledgeCam11 != null) sledgeCam11.SetActive(false);
        if (sledgeCam08 != null) sledgeCam08.SetActive(false);
        if (sledgeCam02 != null) sledgeCam02.SetActive(false);
        // OPRAVENO: Schovej i GLASS při skrývání všech pozic (bezpečnostní opatření)
        if (sledgeGlass != null) sledgeGlass.SetActive(false);
        if (sledgeCam06 != null) sledgeCam06.SetActive(false);
        if (sledgeCam04 != null) sledgeCam04.SetActive(false);
        if (sledgeCam04Generator != null) sledgeCam04Generator.SetActive(false);
    }
    
    #endregion
    
    #region Special Movements
    
    private void StartGlass1ToGlass2Movement()
    {
        if (glass1ToGlass2Coroutine != null)
        {
            StopCoroutine(glass1ToGlass2Coroutine);
        }
        
        // Fade efekty budou spuštěny v coroutine
        glass1ToGlass2Coroutine = StartCoroutine(Glass1ToGlass2MovementCoroutine());
    }
    
    private IEnumerator Glass1ToGlass2MovementCoroutine()
    {
        isMovingGlass1ToGlass2 = true;
        
        if (sledgeGlass != null)
        {
            // Ensure GLASS is active and visible before movement
            sledgeGlass.SetActive(true);
            
            // Use direct GameObject position instead of stored positions
            Vector3 startPos = sledgeGlass.transform.position; // Current GLASS position
            Vector3 endPos = new Vector3(startPos.x - 10f, startPos.y, startPos.z); // Move -10 units in X axis only
            
            if (enableDebugLogs)
            {
                Debug.Log($"[SledgeAI] GLASS X-axis movement starting:");
                Debug.Log($"  Start Position: {startPos}");
                Debug.Log($"  End Position: {endPos} (X moved by -10)");
                Debug.Log($"  Speed: {glass1ToGlass2Speed}");
            }
            
            float journey = 0f;
            float totalDistance = 10f; // Fixed distance of 10 units in X-axis
            float totalTime = totalDistance / glass1ToGlass2Speed;
            
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] GLASS movement will take {totalTime:F2} seconds");
            
            // Spustit proximity-based walking by sound
            StartWalkingByProximitySound(startPos, endPos);
            
            // Move GLASS GameObject in X-axis only
            while (journey < totalTime)
            {
                journey += Time.deltaTime;
                float progress = journey / totalTime;
                
                // Only change X position, keep Y and Z the same
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                sledgeGlass.transform.position = currentPos;
                
                // Aktualizuj walking by sound podle pozice
                UpdateWalkingByProximityVolume(currentPos, startPos, endPos);
                
                // Debug log every 0.5 seconds during movement
                if (enableDebugLogs && journey % 0.5f < Time.deltaTime)
                {
                    Debug.Log($"[SledgeAI] GLASS X movement progress: {progress * 100:F1}% - Position: {sledgeGlass.transform.position}");
                }
                
                yield return null;
            }
            
            // Ensure final position (X moved by -10)
            sledgeGlass.transform.position = endPos;
            
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] GLASS X movement completed - Final position: {sledgeGlass.transform.position}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogError("[SledgeAI] sledgeGlass is NULL! Cannot perform GLASS movement");
        }
        
        // Update position to GLASS 2
        currentPosition = 9;
        isMovingGlass1ToGlass2 = false;
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] GLASS moved in X-axis by -10 units");
        
        // Immediately move to CAM 04 with 100% chance
        yield return new WaitForSeconds(0.5f); // Brief pause
        
        // Reset GLASS to original position and hide before moving to CAM 04
        if (sledgeGlass != null)
        {
            // Reset GLASS back to original GLASS 1 position
            sledgeGlass.transform.position = glassPosition1;
            
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] GLASS reset to original position: {glassPosition1}");
            
            // Hide GLASS after reset
            sledgeGlass.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] GLASS hidden after reset");
        }
        
        MoveToPosition(11); // Move to CAM 04
    }
    
    private void StartGeneratorRotation()
    {
        if (generatorRotationCoroutine != null)
        {
            StopCoroutine(generatorRotationCoroutine);
        }
        
        generatorRotationCoroutine = StartCoroutine(GeneratorRotationCoroutine());
    }
    
    private IEnumerator GeneratorRotationCoroutine()
    {
        isRotatingAtGenerator = true;
        
        if (sledgeCam04Generator != null)
        {
            // Hide CAM 04, show CAM 04 Generator
            if (sledgeCam04 != null) sledgeCam04.SetActive(false);
            sledgeCam04Generator.SetActive(true);
            currentPosition = 12;
            
            // Rotate from X to Y
            Vector3 startRotation = generatorAttackRotationX;
            Vector3 endRotation = generatorAttackRotationY;
            
            float rotationTime = Vector3.Angle(startRotation, endRotation) / generatorRotationSpeed;
            float journey = 0f;
            
            while (journey < rotationTime)
            {
                journey += Time.deltaTime;
                float progress = journey / rotationTime;
                
                sledgeCam04Generator.transform.eulerAngles = Vector3.Lerp(startRotation, endRotation, progress);
                
                yield return null;
            }
            
            // Ensure final rotation
            sledgeCam04Generator.transform.eulerAngles = endRotation;
        }
        
        isRotatingAtGenerator = false;
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Generator rotation completed");
        
        // Start attack immediately after rotation
        StartGeneratorAttack();
    }
    
    #endregion
    
    #region Generator Attack System
    
    private void StartGeneratorAttack()
    {
        if (isAttacking) return;
        
        // Check if Thatcher is currently attacking
        if (IsThatcherCurrentlyAttacking())
        {
            // Pause Sledge until Thatcher finishes
            StartCoroutine(WaitForThatcherAttackToEnd());
            return;
        }
        
        isAttacking = true;
        attackTimer = 0f;
        
        // Disable Thatcher AI while Sledge attacks
        DisableThatcherTemporarily();
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Generator attack started! Player has {attackDuration} seconds to defend");
        
        // Start attack coroutine
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(GeneratorAttackCoroutine());
    }
    
    private IEnumerator GeneratorAttackCoroutine()
    {
        float timer = 0f;
        
        while (timer < attackDuration && isAttacking)
        {
            // KRITICKÝ CHECK: PAUSUJ attack timer pokud Warden útočí
            if (wardenAI != null && wardenAI.IsWardenActive())
            {
                // NEPŘIČÍTEJ čas k timeru - Warden pausuje Sledge attack
                if (enableDebugLogs && Time.frameCount % 300 == 0) // Log každých 5 sekund
                    Debug.Log("[SledgeAI] Generator attack PAUSED - Warden is attacking");
                
                yield return null;
                continue; // Přeskoč zbytek loop - timer se nezvyšuje
            }
            
            // Check for shock defense
            if (shockingSystem != null && shockingSystem.WasShockTriggered())
            {
                // Player defended successfully
                HandleSuccessfulDefense();
                yield break;
            }
            
            timer += Time.deltaTime;
            attackTimer = timer;
            yield return null;
        }
        
        // If we reach here, attack succeeded
        if (isAttacking)
        {
            ExecuteGeneratorDestruction();
        }
    }
    
    private void HandleSuccessfulDefense()
    {
        isAttacking = false;
        attackTimer = 0f;
        
        // Reset generator rotation
        if (sledgeCam04Generator != null)
        {
            sledgeCam04Generator.transform.eulerAngles = generatorAttackRotationX;
        }
        
        // Re-enable Thatcher AI
        RestoreThatcherAI();
        
        // Přehraj walking off sound při úspěšné obraně
        PlayWalkingOffSound();
        
        // Return to CAM 03 or CAM 07
        int[] retreatPositions = { 3, 4 }; // CAM 03 or CAM 07
        int retreatPosition = retreatPositions[Random.Range(0, retreatPositions.Length)];
        
        SetSledgePosition(retreatPosition);
        
        // Temporarily disable Sledge for 30 seconds
        StartTemporaryDisable();
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Player defended successfully! Sledge retreated to position {retreatPosition}");
    }
    
    private void ExecuteGeneratorDestruction()
    {
        isAttacking = false;
        attackTimer = 0f;
        
        // Play all generator destruction audio effects
        PlayGeneratorDestructionAudio();
        
        // KRITICKÉ: Označ generátor jako permanentně zničený v ElectricityManagement
        // Toto ZABLOKUJE možnost zapnout elektřinu až do konce noci
        if (electricitySystem != null)
        {
            electricitySystem.MarkGeneratorAsDestroyed(); // NOVÁ METODA - centralizované řízení zničeného generátoru
            electricitySystem.StopGame(); // Stop all electrical systems permanently
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Generator DESTROYED! PERMANENT BLACKOUT - electricitySystem.MarkGeneratorAsDestroyed() called");
        }
        
        // Force disable ALL electrical systems manually
        ForceDisableAllElectricalSystems();
        
        // Force cameras off if player is currently on cameras
        if (IsPlayerOnCameras())
        {
            ForceCamerasOff();
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Player was on cameras - forcing CamsOff due to generator destruction");
        }
        
        // Reset generator rotation
        if (sledgeCam04Generator != null)
        {
            sledgeCam04Generator.transform.eulerAngles = generatorAttackRotationX;
        }
        
        // Re-enable Thatcher AI
        RestoreThatcherAI();
        
        // KRITICKÉ: Sledge zůstane AKTIVNÍ na první pozici, ale AI = 0
        // Přesun na Storage 1 BEZ deaktivace
        SetSledgePosition(1);
        
        // Reset GLASS to original position 1
        if (sledgeGlass != null)
        {
            sledgeGlass.transform.position = glassPosition1;
        }
        
        // KRITICKÉ: EXPLICITNĚ nastav isSledgeActive = true PŘED nastavením AI=0
        // Toto zajistí že Sledge zůstane viditelný i po jumpscaru
        isSledgeActive = true;
        currentPosition = 1;
        
        // KRITICKÝ FLAG: Označ generátor jako zničený (AI bude VŽDY 0 dokud se neobnoví)
        isGeneratorDestroyed = true;
        
        // Nastav AI na 0 (Sledge už se nebude hýbat, ale zůstane viditelný)
        manualAILevel = 0;
        
        // Reset attack states
        isAttacking = false;
        isTemporarilyDisabled = false;
        isMovingGlass1ToGlass2 = false;
        isRotatingAtGenerator = false;
        currentRouteInProgress = -1;
        
        // Stop walking audio
        StopWalkingBySound();
        
        // Reset timers
        movementTimer = 0f;
        attackTimer = 0f;
        temporaryDisableTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Generator DESTROYED! Sledge FORCED ACTIVE at Storage 1 with AI=0 (isSledgeActive={isSledgeActive}, visible forever)");
    }
    
    private IEnumerator WaitForThatcherAttackToEnd()
    {
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Waiting for Thatcher attack to end before starting generator attack");
        
        // Set Sledge AI to 0 and pause attack
        int originalAI = GetEffectiveAILevel();
        manualAILevel = 0;
        
        // Wait until Thatcher is no longer attacking
        while (IsThatcherCurrentlyAttacking())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Restore AI and start attack
        if (useAdvancedAISystem)
        {
            // AI will be restored from advanced system
        }
        else
        {
            manualAILevel = originalAI;
        }
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Thatcher attack ended, starting generator attack");
        
        StartGeneratorAttack();
    }
    
    #endregion
    
    #region Thatcher Interaction
    
    private bool IsThatcherCurrentlyAttacking()
    {
        if (thatcherAI != null)
        {
            // Check if Thatcher is in active attacking state
            // Using actual available ThatcherAI public methods
            return thatcherAI.IsThatcherActive() || thatcherAI.IsThatcherBlockingCameras();
        }
        return false;
    }
    
    private void DisableThatcherTemporarily()
    {
        if (thatcherAI != null)
        {
            // Store original Thatcher state
            wasThatcherActiveBeforeSledge = thatcherAI.IsThatcherActive();
            
            // Temporarily set Thatcher AI to 0
            // Note: You might need to add a public method to ThatcherAI for this
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Thatcher AI temporarily disabled");
        }
    }
    
    private void RestoreThatcherAI()
    {
        if (thatcherAI != null)
        {
            // Restore Thatcher AI based on night and time
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Thatcher AI restored");
        }
    }
    
    #endregion
    
    #region Temporary Disable System
    
    private void StartTemporaryDisable()
    {
        isTemporarilyDisabled = true;
        temporaryDisableTimer = temporaryDisableDuration;
        
        if (temporaryDisableCoroutine != null)
        {
            StopCoroutine(temporaryDisableCoroutine);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Sledge temporarily disabled for {temporaryDisableDuration} seconds");
    }
    
    #endregion
    
    #region Audio & Visual Effects
    
    private bool IsPlayerOnCameras()
    {
        if (camsOffButton != null && camsOnButton != null)
        {
            bool camsOffActive = camsOffButton.activeInHierarchy;
            bool camsOnActive = camsOnButton.activeInHierarchy;
            
            return camsOffActive && !camsOnActive;
        }
        
        // Fallback
        if (cameraSystem != null)
        {
            return cameraSystem.AreCamerasOpen();
        }
        
        return false;
    }
    
    private void ForceDisableAllElectricalSystems()
    {
        // Force disable lights with permanent power outage
        if (lightSystem != null)
        {
            lightSystem.TriggerPowerOutage(); // Force permanent power outage
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Light system - PERMANENT POWER OUTAGE triggered");
        }
        
        // Force disable shocking system
        if (shockingSystem != null)
        {
            shockingSystem.enabled = false; // Disable the entire component
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Shocking system disabled");
        }
        
        // Disable all electrical UI components
        if (electricitySystem != null)
        {
            electricitySystem.HidePowerUI(); // Hide electrical UI
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] All electrical UI hidden");
        }
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] ALL ELECTRICAL SYSTEMS FORCIBLY DISABLED - PERMANENT BLACKOUT");
    }
    
    private void ForceCamerasOff()
    {
        // Force cameras off by clicking CamsOff button
        if (camsOffButton != null)
        {
            Button buttonComponent = camsOffButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.Invoke();
                
                if (enableDebugLogs)
                    Debug.Log("[SledgeAI] Forced CamsOff button click");
            }
        }
        
        // TODO: Add direct camera system methods if needed
        // The button click should handle turning off cameras properly
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Forced cameras off through button system");
    }
    
    private void ShowMovingEffect()
    {
        bool isPlayerOnCameras = IsPlayerOnCameras();
        
        if (isPlayerOnCameras)
        {
            StartCoroutine(ShowMovingPanel());
            PlayErrorCamSound();
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Moving effect shown - player watching cameras");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Moving effect skipped - player NOT watching cameras");
        }
    }
    
    private IEnumerator ShowMovingPanel()
    {
        if (sledgeMovePanel != null)
        {
            sledgeMovePanel.SetActive(true);
            
            float elapsedTime = 0f;
            while (elapsedTime < 1.5f)
            {
                if (!IsPlayerOnCameras())
                {
                    sledgeMovePanel.SetActive(false);
                    yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }
            
            sledgeMovePanel.SetActive(false);
        }
    }
    
    private void PlayErrorCamSound()
    {
        // KRITICKÉ: Nepřehrávej Error Cam v menu nebo Custom Night panelu
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return;
        }
        
        if (errorCamAudioSource != null && errorCamSound != null)
        {
            errorCamAudioSource.clip = errorCamSound;
            errorCamAudioSource.Play();
        }
    }
    
    private void PlayMetalDetectorSound()
    {
        if (metalDetectorAudioSource != null && metalDetectorSound != null)
        {
            metalDetectorAudioSource.clip = metalDetectorSound;
            metalDetectorAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Metal detector sound played (CAM 11 → CAM 02)");
        }
    }
    
    private void PlayGeneratorDestructionAudio()
    {
        // 1. Play Sledge destruction sound
        if (destructionAudioSource != null && generatorDestructionSound != null)
        {
            destructionAudioSource.clip = generatorDestructionSound;
            destructionAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Generator destruction sound played");
        }
        
        // 2. Trigger electricity system shutdown audio (classic turn OFF sound + stop background)
        if (electricitySystem != null)
        {
            // TODO: Add TriggerGeneratorShutdown() method to ElectricityManagement
            // This should play the classic turn OFF sound and stop background generator sound
            // For now, we'll use the existing SetPower method
            // electricitySystem.TriggerGeneratorShutdown();
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Generator shutdown audio needs to be implemented in ElectricityManagement");
        }
    }
    
    // Legacy method for backwards compatibility
    private void PlayGeneratorDestructionSound()
    {
        // This method now calls the new comprehensive audio method
        PlayGeneratorDestructionAudio();
    }
    
    /// <summary>
    /// Spustí walking by sound s proximity-based hlasitostí
    /// </summary>
    /// <param name="startPos">Počáteční pozice GLASS</param>
    /// <param name="endPos">Koncová pozice GLASS</param>
    private void StartWalkingByProximitySound(Vector3 startPos, Vector3 endPos)
    {
        if (walkingByAudioSource != null && walkingBySound != null)
        {
            // Nastav počáteční hlasitost na 20%
            float initialVolume = GetProximityVolume(startPos, startPos, endPos);
            walkingByAudioSource.volume = initialVolume;
            walkingByAudioSource.clip = walkingBySound;
            walkingByAudioSource.Play();
            isWalkingByPlaying = true;
            
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] Walking by proximity sound spuštěn - počáteční hlasitost: {(initialVolume * 100):F0}%");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("[SledgeAI] Walking by audio source nebo sound není přiřazen!");
        }
    }
    
    /// <summary>
    /// Aktualizuje hlasitost walking by sound podle pozice na X-ose
    /// </summary>
    /// <param name="currentPos">Aktuální pozice GLASS</param>
    /// <param name="startPos">Počáteční pozice GLASS</param>
    /// <param name="endPos">Koncová pozice GLASS</param>
    private void UpdateWalkingByProximityVolume(Vector3 currentPos, Vector3 startPos, Vector3 endPos)
    {
        if (walkingByAudioSource != null && isWalkingByPlaying)
        {
            float proximityVolume = GetProximityVolume(currentPos, startPos, endPos);
            walkingByAudioSource.volume = proximityVolume;
            
            if (enableDebugLogs && Time.frameCount % 30 == 0) // Log every 30 frames
            {
                float distanceFromStart = Mathf.Abs(currentPos.x - startPos.x);
                Debug.Log($"[SledgeAI] Walking proximity update - X distance: {distanceFromStart:F1} units, Volume: {(proximityVolume * 100):F0}%");
            }
        }
    }
    
    /// <summary>
    /// Vypočítá hlasitost podle pozice na X-ose (proximity effect)
    /// </summary>
    /// <param name="currentPos">Aktuální pozice</param>
    /// <param name="startPos">Počáteční pozice</param>
    /// <param name="endPos">Koncová pozice</param>
    /// <returns>Hlasitost (0.0 - 1.0)</returns>
    private float GetProximityVolume(Vector3 currentPos, Vector3 startPos, Vector3 endPos)
    {
        // Výpočet vzdálenosti na X-ose
        float totalDistance = Mathf.Abs(endPos.x - startPos.x); // 10 units
        float currentDistance = Mathf.Abs(currentPos.x - startPos.x); // 0-10 units
        
        // Normalize distance (0.0 = start, 1.0 = end)
        float progress = Mathf.Clamp01(currentDistance / totalDistance);
        
        // Vytvořit proximity curve:
        // 0.0 progress (start) = 20% volume
        // 0.5 progress (middle) = 100% volume (nebo 30% pokud kamery)
        // 1.0 progress (end) = 0% volume
        
        float baseVolume;
        if (progress <= 0.5f)
        {
            // První polovina: 20% → 100%
            float localProgress = progress * 2f; // 0.0 - 1.0
            baseVolume = Mathf.Lerp(startingVolumePercent, maxVolumePercent, localProgress);
        }
        else
        {
            // Druhá polovina: 100% → 0%
            float localProgress = (progress - 0.5f) * 2f; // 0.0 - 1.0
            baseVolume = Mathf.Lerp(maxVolumePercent, 0f, localProgress);
        }
        
        // Aplikuj camera reduction pokud je hráč na kamerách
        bool camerasOpen = IsPlayerOnCameras();
        float finalVolume = camerasOpen ? (baseVolume * cameraVolumeReductionWalking) : baseVolume;
        
        // Normalizuj podle původní hlasitosti
        return finalVolume * originalWalkingByVolume;
    }
    
    /// <summary>
    /// Zastaví walking by sound (nový simplified systém)
    /// </summary>
    private void StopWalkingBySound()
    {
        if (walkingByAudioSource != null && isWalkingByPlaying)
        {
            walkingByAudioSource.Stop();
            walkingByAudioSource.volume = 0f;
            isWalkingByPlaying = false;
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Walking by sound zastaven");
        }
    }
    
    /// <summary>
    /// Přehraje walking off sound při úspěšné obraně
    /// </summary>
    private void PlayWalkingOffSound()
    {
        if (walkingOffAudioSource != null && walkingOffSound != null)
        {
            walkingOffAudioSource.PlayOneShot(walkingOffSound);
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Walking off sound přehrán při úspěšné obraně");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("[SledgeAI] Walking off audio source nebo sound není přiřazen!");
        }
    }
    
    /// <summary>
    /// Aktualizuje hlasitost walking by sound podle stavu kamer (pouze pokud neprobíhá proximity control)
    /// </summary>
    private void UpdateWalkingAudioVolume()
    {
        // Proximity systém řídí hlasitost přímo, žádné další úpravy nejsou potřeba
        // Tato metoda je zachována pro kompatibilitu
    }
    
    #endregion
    
    #region Public API
    
    public void StartSledgeActivity()
    {
        // FALLBACK: Pokud je generátor zničený při startu aktivity, automaticky ho obnov
        // (Toto by se nemělo stát pokud je správně volaná OnNewNightStarted, ale pro jistotu)
        if (isGeneratorDestroyed)
        {
            Debug.LogWarning("[SledgeAI] FALLBACK: Generator byl zničený při StartSledgeActivity - automaticky obnovuji!");
            Debug.LogWarning("[SledgeAI] Ujisti se že je SledgeAIRef správně přiřazena v ResetNight Inspectoru!");
            isGeneratorDestroyed = false;
        }
        
        if (GetEffectiveAILevel() <= 0)
            return;
        
        isSledgeActive = true;
        isAttacking = false;
        isTemporarilyDisabled = false;
        
        // Calculate AI timings
        CalculateAITimings();
        
        // Start at Storage 1
        SetSledgePosition(1);
        
        // Reset timers
        movementTimer = 0f;
        attackTimer = 0f;
        temporaryDisableTimer = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Sledge activated at Storage 1 (isGeneratorDestroyed={isGeneratorDestroyed})");
    }
    
    public void DeactivateSledge()
    {
        isSledgeActive = false;
        isAttacking = false;
        isTemporarilyDisabled = false;
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // Hide all positions
        HideAllSledgePositions();
        
        // Restore Thatcher if needed
        RestoreThatcherAI();
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] Sledge deactivated");
    }
    
    public void ResetSledgeToStart()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // KRITICKÉ: VŽDY aktivuj Storage 1 pozici při resetu
        SetSledgePosition(1);
        
        // Reset GLASS to original position 1
        if (sledgeGlass != null)
        {
            sledgeGlass.transform.position = glassPosition1;
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] GLASS reset to original position 1");
        }
        
        // Reset generator rotation
        if (sledgeCam04Generator != null)
        {
            sledgeCam04Generator.transform.eulerAngles = generatorAttackRotationX;
        }
        
        // KRITICKÉ: Reset activity flag aby se Sledge aktivoval po novém resetu
        isSledgeActive = false;
        
        // KRITICKÉ: NEPREPISUJ isGeneratorDestroyed zde - pouze OnNewNightStarted() může obnovit generátor
        // Tento reset se může volat během hry, takže musíme zachovat stav zničeného generátoru
        
        // Reset all states
        isAttacking = false;
        isTemporarilyDisabled = false;
        isMovingGlass1ToGlass2 = false;
        isRotatingAtGenerator = false;
        currentRouteInProgress = -1; // Reset route tracking
        
        // Stop walking audio
        StopWalkingBySound();
        
        // Reset timers
        movementTimer = 0f;
        attackTimer = 0f;
        temporaryDisableTimer = 0f;
        
        // Restore Thatcher if needed
        RestoreThatcherAI();
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Sledge reset to Storage 1 - isGeneratorDestroyed={isGeneratorDestroyed} (preserved)");
    }
    
    public void ResetSledgeAfterCharacterDeath()
    {
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] ===== RESET AFTER CHARACTER DEATH =====");
        
        // KRITICKÉ: Použij isGeneratorDestroyed flag pro detekci
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // KRITICKÉ: Skryj všechny pozice před nastavením Storage 1
        HideAllSledgePositions();
        
        // Zajisti že Storage 1 je aktivní
        SetSledgePosition(1);
        currentPosition = 1;
        
        // Reset GLASS
        if (sledgeGlass != null)
        {
            sledgeGlass.transform.position = glassPosition1;
        }
        
        // Reset generator rotation
        if (sledgeCam04Generator != null)
        {
            sledgeCam04Generator.transform.eulerAngles = generatorAttackRotationX;
        }
        
        // Reset attack states
        isAttacking = false;
        isTemporarilyDisabled = false;
        isMovingGlass1ToGlass2 = false;
        isRotatingAtGenerator = false;
        currentRouteInProgress = -1;
        
        // Stop walking audio
        StopWalkingBySound();
        
        // Reset timers
        movementTimer = 0f;
        attackTimer = 0f;
        temporaryDisableTimer = 0f;
        
        // KRITICKÉ: Zachovej isSledgeActive pokud Sledge zničil generátor
        // Pokud nezničil generátor, deaktivuj ho normálně
        if (!isGeneratorDestroyed)
        {
            isSledgeActive = false;
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Sledge DEACTIVATED after jumpscare (normal state)");
        }
        else
        {
            // isSledgeActive zůstává true - Sledge viditelný na Storage 1, ale NEMŮŽE se hýbat (AI=0)
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Sledge remains ACTIVE after jumpscare (generator destroyed - FROZEN at Storage 1, AI forced to 0)");
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SledgeAI] Reset complete - Position: Storage 1, isSledgeActive={isSledgeActive}, AI={manualAILevel}");
    }
    
    public bool IsSledgeActive => isSledgeActive;
    public bool IsSledgeAttacking => isAttacking;
    public int CurrentPosition => currentPosition;
    
    #endregion
    
    #region Utility
    
    private void UpdateDisplayValues()
    {
        currentEffectiveAI = GetEffectiveAILevel();
        
        if (!isSledgeActive)
            systemStatus = "Inactive";
        else if (isTemporarilyDisabled)
            systemStatus = $"Temporarily Disabled ({temporaryDisableTimer:F1}s)";
        else if (isAttacking)
            systemStatus = $"Attacking Generator ({(attackDuration - attackTimer):F1}s)";
        else if (isMovingGlass1ToGlass2)
            systemStatus = "Moving GLASS 1 → GLASS 2 (Visible)";
        else if (isRotatingAtGenerator)
            systemStatus = "Rotating at Generator";
        else
            systemStatus = $"Position {currentPosition} - {GetPositionName(currentPosition)}";
    }
    
    private string GetPositionName(int position)
    {
        return position switch
        {
            1 => "Storage 1",
            2 => "Storage 2", 
            3 => "CAM 03",
            4 => "CAM 07",
            5 => "CAM 11",
            6 => "CAM 08",
            7 => "CAM 02",
            8 => "GLASS 1",
            9 => "GLASS 2",
            10 => "CAM 06",
            11 => "CAM 04",
            12 => "CAM 04 Generator",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Called when a new night starts - resets electricity and Sledge to starting position
    /// </summary>
    public void OnNewNightStarted()
    {
        // KRITICKÉ: Obnov generátor pro novou noc (resetuj destruction flag)
        isGeneratorDestroyed = false;
        
        if (enableDebugLogs)
            Debug.Log("[SledgeAI] ===== NEW NIGHT STARTED - GENERATOR RESTORED =====");
        
        // Reset electricity system for new night
        if (electricitySystem != null)
        {
            electricitySystem.RestoreGenerator(); // KRITICKÉ: Obnov generátor před ResetPower()
            electricitySystem.ResetPower(); // Reset to 100% power
            
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] Electricity reset to 100% for new night - generator restored");
        }
        
        // Reset Sledge to starting position (includes isSledgeActive = false)
        ResetSledgeToStart();
        
        // Check AI level and activate Sledge if AI > 0
        int currentAI = GetEffectiveAILevel();
        if (currentAI > 0)
        {
            StartSledgeActivity();
            
            if (enableDebugLogs)
                Debug.Log($"[SledgeAI] New night started - Generator restored, Sledge activated with AI {currentAI}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[SledgeAI] New night started - Generator restored, Sledge inactive (AI = 0)");
        }
    }
    
    #endregion
}