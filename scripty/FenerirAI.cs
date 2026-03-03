using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FenerirNightConfiguration
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

public class FenerirAI : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("=== FENERIR AI SYSTEM ===")]
    [SerializeField] private bool enableFenerirAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;
    
    [Header("Fenerir Positions (3 Positions on CAM 09)")]
    [SerializeField] private GameObject fenerirPosition1; // RIGHT position
    [SerializeField] private GameObject fenerirPosition2; // MIDDLE position  
    [SerializeField] private GameObject fenerirPosition3; // LEFT position
    
    [Header("Smokey Device GameObjects")]
    [SerializeField] private GameObject smokeyDevice1; // RIGHT device
    [SerializeField] private GameObject smokeyDevice2; // MIDDLE device
    [SerializeField] private GameObject smokeyDevice3; // LEFT device
    
    [Header("Control Buttons (Visible only on CAM 09)")]
    [SerializeField] private Button rightButton; // Controls Position 1 & Device 1
    [SerializeField] private Button middleButton; // Controls Position 2 & Device 2
    [SerializeField] private Button leftButton; // Controls Position 3 & Device 3
    
    [Header("UI Panels")]
    [SerializeField] private GameObject fenerirDeadPanel;
    [SerializeField] private UnityEngine.Video.VideoPlayer deadPanelVideoPlayer; // Video player na dead panelu
    [SerializeField] private GameObject fenerirMovingPanel;
    [SerializeField] private GameObject youLostPanel; // Shows on CAM 09 when Fenerir attacks
    
    [Header("Camera Reference")]
    [SerializeField] private GameObject cam09; // CAM 09 reference
    
    [Header("System References")]
    [SerializeField] private CameraSystem cameraSystem;
    [SerializeField] private ElectricityManagement electricitySystem;
    [SerializeField] private ZofeyeAI zofeyeAI;
    [SerializeField] private ThatcherAI thatcherAI;
    [SerializeField] private WardenAI wardenAI; // Reference na Warden pro pausing
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private GameObject camsOffButton; // Aktivní když hráč sleduje kamery
    [SerializeField] private GameObject camsOnButton;  // Aktivní když hráč NEvídí kamery
    [SerializeField] private Button camsOffButtonComponent; // Button component pro listener
    
    [Header("Audio System")]
    [SerializeField] private AudioSource errorCamAudioSource;
    [SerializeField] private AudioClip errorCamSound;
    [SerializeField] private AudioSource aggressionAudioSource;
    [SerializeField] private AudioClip fenerirAggressionSound; // Loops when angry
    [SerializeField] private AudioSource jumpcareAudioSource;
    [SerializeField] private AudioClip jumpcareSound;
    
    [Header("Light System - Vypnutí před jumpscarem")]
    [SerializeField] private GameObject mainLight; // Hlavní světlo (samostatné)
    [SerializeField] private GameObject parentLight; // Parent všech ostatních světel v kanceláři (vypne všechny děti)
    [SerializeField] private float lightOffBeforeJumpscare = 0.9f; // 0.9s před jumpscarem zhasnou světla
    [SerializeField] private float lightOnAfterDelay = 1f; // 1s po jumpscare se světla rozsvítí
    [SerializeField] private AudioSource lightOffAudioSource; // Audio source pro zvuk vypnutí světel
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí elektřiny
    [SerializeField] private AudioSource electricityGeneratorAudioSource; // Audio source generátoru (zastaví se při jumpscare)
    [SerializeField] private float gameOverDelay = 3f; // Doba zobrazení dead panelu před návratem do menu
    
    [Header("Advanced AI System - Night & Time")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private FenerirNightConfiguration[] aiSettings = new FenerirNightConfiguration[6];
    
    [Header("Menu Detection System")]
    [SerializeField] private GameObject[] menuPanels = new GameObject[0];
    [SerializeField] private bool enableMenuDetection = true;
    
    [Header("Current Status - READ ONLY")]
    [SerializeField] private int currentPosition = 0; // 1, 2, or 3
    [SerializeField] private int activeSmokey = 0; // 0 = none, 1-3 = active device
    [SerializeField] private int currentEffectiveAI = 0;
    [SerializeField] private bool isAngry = false;
    [SerializeField] private float timeAtCurrentPosition = 0f;
    [SerializeField] private float timeToResponseRequired = 0f;
    [SerializeField] private string systemStatus = "Inactive";
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Private Variables
    
    private bool isFenerirActive = false;
    private bool isPlayerWatchingCam09 = false;
    private bool menuIsActive = false;
    private bool isInJumpscare = false; // Ochrana před prerusenim jumpscare sekvence
    
    // Timing variables
    private float stayAtPositionTimer = 0f;
    private float responseTimer = 0f;
    private float angerTimer = 0f;
    private float jumpscareDelay = 0f; // Calculated once when angry starts
    
    // AI calculated values
    private float minStayTime = 30f;
    private float maxStayTime = 60f;
    private float responseTime = 40f;
    
    // System state
    private bool needsToMove = false;
    private bool waitingForResponse = false;
    private bool hasMovedSinceLastCheck = false;
    
    // Materials for smokey devices
    private Material smokeyMaterial1;
    private Material smokeyMaterial2;
    private Material smokeyMaterial3;
    
    // Button colors for highlighting
    private Color originalRightButtonColor;
    private Color originalMiddleButtonColor;
    private Color originalLeftButtonColor;
    private Color activeButtonColor = new Color(0.71f, 0.62f, 0.8f); // #B59FCC
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        InitializeFenerirSystem();
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] System initialized - Fenerir ready");
    }
    
    void Update()
    {
        if (!enableFenerirAI || !this.enabled || !this.gameObject.activeInHierarchy) 
            return;
        
        // CHECK FOR WARDEN - PAUSE ALL TIMERS IF WARDEN IS ATTACKING
        if (wardenAI != null && wardenAI.IsWardenActive())
            return;
        
        // Check menu status
        CheckMenuStatus();
        
        // Check camera viewing
        CheckCam09Viewing();
        
        // Main AI logic
        if (isFenerirActive)
        {
            HandleFenerirBehavior();
        }
        
        // Update display values
        UpdateDisplayValues();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeFenerirSystem()
    {
        // Initialize materials
        InitializeSmokyMaterials();
        
        // Initialize audio sources
        InitializeAudioSources();
        
        // Store original button colors
        StoreOriginalButtonColors();
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Calculate AI values
        CalculateAITimings();
        
        // Vždy spusť aktivitu Fenerira pokud je povolen (zobrazí se na první pozici)
        // Pohyb se řídí v Update() podle AI level
        if (enableFenerirAI)
        {
            StartFenerirActivity();
            
            if (enableDebugLogs)
                Debug.Log($"[FenerirAI] Fenerir aktivován - AI: {GetEffectiveAILevel()}");
        }
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] Fenerir system initialized");
    }
    
    private void InitializeSmokyMaterials()
    {
        if (smokeyDevice1 != null)
        {
            Renderer renderer = smokeyDevice1.GetComponent<Renderer>();
            if (renderer != null)
                smokeyMaterial1 = renderer.material;
        }
        
        if (smokeyDevice2 != null)
        {
            Renderer renderer = smokeyDevice2.GetComponent<Renderer>();
            if (renderer != null)
                smokeyMaterial2 = renderer.material;
        }
        
        if (smokeyDevice3 != null)
        {
            Renderer renderer = smokeyDevice3.GetComponent<Renderer>();
            if (renderer != null)
                smokeyMaterial3 = renderer.material;
        }
        
        // Set all to black initially
        SetAllSmokeysToBlack();
    }
    
    private void InitializeAudioSources()
    {
        if (errorCamAudioSource == null)
            errorCamAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (aggressionAudioSource == null)
            aggressionAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (jumpcareAudioSource == null)
            jumpcareAudioSource = gameObject.AddComponent<AudioSource>();
        
        // Configure audio sources
        errorCamAudioSource.playOnAwake = false;
        aggressionAudioSource.playOnAwake = false;
        aggressionAudioSource.loop = true;
        jumpcareAudioSource.playOnAwake = false;
    }
    
    private void StoreOriginalButtonColors()
    {
        // Nastavit původní barvy na bílou s plnou alpha (255)
        originalRightButtonColor = Color.white;
        originalMiddleButtonColor = Color.white;
        originalLeftButtonColor = Color.white;
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] Original button colors set to white");
    }
    
    private void SetupButtonListeners()
    {
        if (rightButton != null)
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(() => OnSmokeyButtonPressed(1));
        }
        
        if (middleButton != null)
        {
            middleButton.onClick.RemoveAllListeners();
            middleButton.onClick.AddListener(() => OnSmokeyButtonPressed(2));
        }
        
        if (leftButton != null)
        {
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(() => OnSmokeyButtonPressed(3));
        }
    }
    
    #endregion
    
    #region AI System
    
    public int GetEffectiveAILevel()
    {
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            return 0; // AI = 0 in menu
        }
        
        // Check power - AI = 20 ONLY when power is DEPLETED (not manually turned off)
        if (electricitySystem != null && electricitySystem.GetCurrentPowerPercentage() <= 0f)
        {
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Power depleted (0%) - AI set to maximum (20)");
            return 20;
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("FENERIR");
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
                    Debug.Log($"[FenerirAI] Advanced AI: Night {currentNight}, Hour {currentHour}, AI {aiLevel}");
                return aiLevel;
            }
        }
        
        return manualAILevel;
    }
    
    private void CalculateAITimings()
    {
        int aiLevel = GetEffectiveAILevel();
        
        if (aiLevel == 0)
        {
            // Inactive
            minStayTime = float.MaxValue;
            maxStayTime = float.MaxValue;
            responseTime = float.MaxValue;
            return;
        }
        
        // Linear interpolation between AI 1 and AI 20
        float aiRatio = (aiLevel - 1f) / 19f; // 0.0 at AI 1, 1.0 at AI 20
        
        // AI 1: 34-69 seconds stay, 46 seconds response (zjemněno o 15%)
        // AI 20: 20-23 seconds stay, 14 seconds response (minimální čas zvýšen + zjemněno o 15%)
        minStayTime = Mathf.Lerp(34f, 20f, aiRatio);
        maxStayTime = Mathf.Lerp(69f, 23f, aiRatio);
        responseTime = Mathf.Lerp(46f, 14f, aiRatio);
        
        if (enableDebugLogs)
            Debug.Log($"[FenerirAI] AI {aiLevel}: Stay {minStayTime:F1}-{maxStayTime:F1}s, Response {responseTime:F1}s");
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
        string activePanelName = "";
        
        foreach (GameObject panel in menuPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                anyMenuActive = true;
                activePanelName = panel.name;
                break;
            }
        }
        
        // Menu state changed
        if (anyMenuActive != menuIsActive)
        {
            menuIsActive = anyMenuActive;
            
            if (menuIsActive && !isInJumpscare)
            {
                // Menu activated - deactivate Fenerir (ale ne behem jumpscaru!)
                DeactivateFenerir();
                if (enableDebugLogs)
                    Debug.Log($"[FenerirAI] Menu activated ({activePanelName}) - Fenerir deactivated");
            }
            else
            {
                // Menu deactivated - activate Fenerir if AI > 0
                if (GetEffectiveAILevel() > 0)
                {
                    StartFenerirActivity();
                    if (enableDebugLogs)
                        Debug.Log("[FenerirAI] Menu deactivated - Fenerir activated");
                }
            }
        }
        
        return menuIsActive;
    }
    
    #endregion
    
    #region Camera Detection
    
    /// <summary>
    /// Zkontroluje zda hráč sleduje kamery (stejná logika jako u Zofaye)
    /// </summary>
    private bool IsPlayerOnCameras()
    {
        // Primární detekce pomocí CamsOff/CamsOn tlačítek
        if (camsOffButton != null && camsOnButton != null)
        {
            bool camsOffActive = camsOffButton.activeInHierarchy;   // Hráč sleduje kamery
            bool camsOnActive = camsOnButton.activeInHierarchy;     // Hráč je v kanceláři
            
            // Hráč je na kamerách pouze pokud CamsOff je aktivní A CamsOn není aktivní
            bool isPlayerOnCameras = camsOffActive && !camsOnActive;
            
            if (enableDebugLogs)
                Debug.Log($"[FenerirAI] Camera status: CamsOff={camsOffActive}, CamsOn={camsOnActive}, PlayerOnCams={isPlayerOnCameras}");
            
            return isPlayerOnCameras;
        }
        
        // Fallback - pokud tlačítka nejsou přiřazena
        if (cameraSystem != null)
        {
            return cameraSystem.AreCamerasOpen();
        }
        
        if (enableDebugLogs)
            Debug.LogWarning("[FenerirAI] Camera detection references not assigned!");
        
        return false;
    }
    
    private void CheckCam09Viewing()
    {
        bool wasWatching = isPlayerWatchingCam09;
        isPlayerWatchingCam09 = false;
        
        if (cameraSystem != null && cam09 != null)
        {
            // Check if player is viewing CAM 09 - check if cameras are open and CAM 09 is active
            isPlayerWatchingCam09 = cameraSystem.AreCamerasOpen() && cam09.activeInHierarchy;
        }
        
        // Update button visibility
        if (isPlayerWatchingCam09 != wasWatching)
        {
            UpdateButtonVisibility();
            UpdatePanelVisibility();
        }
    }
    
    private void UpdateButtonVisibility()
    {
        // KRITICKÉ: SKRYJ tlačítka pokud Warden útočí
        bool wardenActive = wardenAI != null && wardenAI.IsWardenActive();
        bool showButtons = isPlayerWatchingCam09 && isFenerirActive && !isAngry && !wardenActive;
        
        if (rightButton != null)
            rightButton.gameObject.SetActive(showButtons);
        if (middleButton != null)
            middleButton.gameObject.SetActive(showButtons);
        if (leftButton != null)
            leftButton.gameObject.SetActive(showButtons);
        
        // Při zobrazení tlačítek obnovit barvu aktivního tlačítka
        if (showButtons && activeSmokey > 0)
        {
            UpdateButtonColors();
        }
        
        if (enableDebugLogs && wardenActive && isPlayerWatchingCam09)
            Debug.Log("[FenerirAI] Buttons HIDDEN - Warden is attacking");
    }
    
    private void RegisterCamsOffButtonListener()
    {
        if (camsOffButtonComponent != null)
        {
            camsOffButtonComponent.onClick.AddListener(OnCamsOffButtonClicked);
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] ✅ CamsOff button registered");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("[FenerirAI] ⚠️ CamsOff button component is NULL!");
        }
    }
    
    private void OnCamsOffButtonClicked()
    {
        // Skryj tlačítka okamžitě při slepnutí z kamer
        if (rightButton != null)
            rightButton.gameObject.SetActive(false);
        if (middleButton != null)
            middleButton.gameObject.SetActive(false);
        if (leftButton != null)
            leftButton.gameObject.SetActive(false);
            
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] 🔴 CamsOff clicked - buttons hidden");
    }
    
    private void UpdatePanelVisibility()
    {
        // Show YOU LOST panel only on CAM 09 when Fenerir is angry
        if (youLostPanel != null)
        {
            youLostPanel.SetActive(isPlayerWatchingCam09 && isAngry);
        }
    }
    
    #endregion
    
    #region Main Behavior
    
    private void HandleFenerirBehavior()
    {
        if (isAngry)
        {
            HandleAngryBehavior();
            return;
        }
        
        if (waitingForResponse)
        {
            HandleResponseTimer();
        }
        else
        {
            HandlePositionTimer();
        }
    }
    
    private void HandlePositionTimer()
    {
        stayAtPositionTimer += Time.deltaTime;
        
        float targetStayTime = Random.Range(minStayTime, maxStayTime);
        
        if (stayAtPositionTimer >= targetStayTime)
        {
            // Time to move to new position
            MoveFenerirToNewPosition();
        }
    }
    
    private void HandleResponseTimer()
    {
        responseTimer += Time.deltaTime;
        
        if (responseTimer >= responseTime)
        {
            // Player failed to respond in time - Fenerir gets angry
            GetAngry();
        }
    }
    
    private void HandleAngryBehavior()
    {
        angerTimer += Time.deltaTime;
        
        // Use pre-calculated jumpcare delay
        if (angerTimer >= jumpscareDelay)
        {
            TriggerFenerirJumpcare();
        }
    }
    
    #endregion
    
    #region Movement System
    
    private void MoveFenerirToNewPosition()
    {
        List<int> availablePositions = new List<int>();
        
        // Add all positions except current
        for (int i = 1; i <= 3; i++)
        {
            if (i != currentPosition)
                availablePositions.Add(i);
        }
        
        if (availablePositions.Count > 0)
        {
            int newPosition = availablePositions[Random.Range(0, availablePositions.Count)];
            
            // Show moving effect
            ShowMovingEffect();
            
            // Move to new position
            SetFenerirPosition(newPosition);
            
            // Start response timer
            StartResponseTimer();
            
            if (enableDebugLogs)
                Debug.Log($"[FenerirAI] Fenerir moved from position {currentPosition} to {newPosition}");
        }
    }
    
    private void SetFenerirPosition(int position)
    {
        // Hide all positions
        if (fenerirPosition1 != null) fenerirPosition1.SetActive(false);
        if (fenerirPosition2 != null) fenerirPosition2.SetActive(false);
        if (fenerirPosition3 != null) fenerirPosition3.SetActive(false);
        
        // Show target position
        currentPosition = position;
        switch (position)
        {
            case 1:
                if (fenerirPosition1 != null) fenerirPosition1.SetActive(true);
                break;
            case 2:
                if (fenerirPosition2 != null) fenerirPosition2.SetActive(true);
                break;
            case 3:
                if (fenerirPosition3 != null) fenerirPosition3.SetActive(true);
                break;
        }
    }
    
    private void StartResponseTimer()
    {
        waitingForResponse = true;
        responseTimer = 0f;
        stayAtPositionTimer = 0f;
        hasMovedSinceLastCheck = true;
        
        timeToResponseRequired = responseTime;
        
        if (enableDebugLogs)
            Debug.Log($"[FenerirAI] Player has {responseTime:F1} seconds to activate correct smokey");
    }
    
    #endregion
    
    #region Smokey System
    
    private void OnSmokeyButtonPressed(int smokeyNumber)
    {
        if (!isFenerirActive || isAngry)
            return;
        
        if (enableDebugLogs)
            Debug.Log($"[FenerirAI] Player pressed smokey button {smokeyNumber}");
        
        // Activate the selected smokey
        ActivateSmokey(smokeyNumber);
        
        // Check if it's correct
        if (smokeyNumber == currentPosition)
        {
            // Correct response
            if (waitingForResponse)
            {
                waitingForResponse = false;
                responseTimer = 0f;
                
                if (enableDebugLogs)
                    Debug.Log("[FenerirAI] Correct smokey activated - Fenerir calmed");
            }
        }
        // Note: We don't punish incorrect responses immediately, 
        // only when timer runs out
    }
    
    private void ActivateSmokey(int smokeyNumber)
    {
        // Deactivate all smokeys first
        SetAllSmokeysToBlack();
        
        // Activate selected smokey
        activeSmokey = smokeyNumber;
        
        switch (smokeyNumber)
        {
            case 1:
                if (smokeyMaterial1 != null)
                    RestoreMaterialColor(smokeyMaterial1);
                break;
            case 2:
                if (smokeyMaterial2 != null)
                    RestoreMaterialColor(smokeyMaterial2);
                break;
            case 3:
                if (smokeyMaterial3 != null)
                    RestoreMaterialColor(smokeyMaterial3);
                break;
        }
        
        // Update button colors
        UpdateButtonColors();
        
        if (enableDebugLogs)
            Debug.Log($"[FenerirAI] Smokey {smokeyNumber} activated");
    }
    
    private void SetAllSmokeysToBlack()
    {
        if (smokeyMaterial1 != null)
            SetMaterialToBlack(smokeyMaterial1);
        if (smokeyMaterial2 != null)
            SetMaterialToBlack(smokeyMaterial2);
        if (smokeyMaterial3 != null)
            SetMaterialToBlack(smokeyMaterial3);
        
        activeSmokey = 0;
        
        // Reset all button colors to original
        ResetAllButtonColors();
    }
    
    private void SetMaterialToBlack(Material material)
    {
        if (material != null)
        {
            material.SetColor("_EmissionColor", Color.black);
        }
    }
    
    private void RestoreMaterialColor(Material material)
    {
        if (material != null)
        {
            // Restore original emission color
            Color originalColor = material.GetColor("_Color");
            material.SetColor("_EmissionColor", originalColor);
        }
    }
    
    private void UpdateButtonColors()
    {
        // Reset all buttons to original color first
        ResetAllButtonColors();
        
        // Highlight the active button
        switch (activeSmokey)
        {
            case 1:
                SetButtonColor(rightButton, activeButtonColor);
                break;
            case 2:
                SetButtonColor(middleButton, activeButtonColor);
                break;
            case 3:
                SetButtonColor(leftButton, activeButtonColor);
                break;
        }
    }
    
    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }
    }
    
    private void ResetAllButtonColors()
    {
        SetButtonColor(rightButton, originalRightButtonColor);
        SetButtonColor(middleButton, originalMiddleButtonColor);
        SetButtonColor(leftButton, originalLeftButtonColor);
    }
    
    #endregion
    
    #region Anger System
    
    private void GetAngry()
    {
        isAngry = true;
        waitingForResponse = false;
        angerTimer = 0f;
        
        // Calculate jumpcare delay once - minimum 12 seconds + random additional time
        int aiLevel = GetEffectiveAILevel();
        float minDelay = 12f; // Always minimum 12 seconds
        float maxAdditionalTime = Mathf.Lerp(8f, 3f, (aiLevel - 1f) / 19f); // 8s at AI 1, 3s at AI 20
        jumpscareDelay = minDelay + Random.Range(0f, maxAdditionalTime);
        
        // Hide all Fenerir positions
        if (fenerirPosition1 != null) fenerirPosition1.SetActive(false);
        if (fenerirPosition2 != null) fenerirPosition2.SetActive(false);
        if (fenerirPosition3 != null) fenerirPosition3.SetActive(false);
        
        // Start aggression sound loop
        PlayAggressionSound();
        
        // Update UI
        UpdateButtonVisibility();
        UpdatePanelVisibility();
        
        if (enableDebugLogs)
            Debug.Log($"[FenerirAI] Fenerir is ANGRY - Player failed to respond in time! Will attack in {jumpscareDelay:F1} seconds");
    }
    
    #endregion
    
    #region Audio System
    
    private void ShowMovingEffect()
    {
        // Zkontroluj zda má ukázat moving efekt (pouze když hráč sleduje kamery)
        bool isPlayerOnCameras = IsPlayerOnCameras();
        
        if (isPlayerOnCameras)
        {
            // Ukáž moving panel (nezávisle na konkrétní kameře)
            StartCoroutine(ShowMovingPanel());
            
            // Přehraj error sound
            PlayErrorCamSound();
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Moving effect shown - player watching cameras");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Moving effect skipped - player NOT watching cameras");
        }
    }
    
    private IEnumerator ShowMovingPanel()
    {
        if (fenerirMovingPanel != null)
        {
            fenerirMovingPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Moving panel shown");
            
            // Čekej 1.5 sekundy, ale kontroluj zda hráč stále sleduje kamery
            float elapsedTime = 0f;
            while (elapsedTime < 1.5f)
            {
                // Zkontroluj zda hráč stále sleduje kamery
                if (!IsPlayerOnCameras())
                {
                    // Okamžitě skryj panel pokud hráč opustil kamery
                    fenerirMovingPanel.SetActive(false);
                    if (enableDebugLogs)
                        Debug.Log("[FenerirAI] Player left cameras - hiding moving panel immediately!");
                    yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }
            
            // Skryj panel po uplynutí času
            fenerirMovingPanel.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Moving panel hidden after full duration");
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
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Error cam sound played");
        }
    }
    
    private void PlayAggressionSound()
    {
        if (aggressionAudioSource != null && fenerirAggressionSound != null)
        {
            aggressionAudioSource.clip = fenerirAggressionSound;
            aggressionAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Fenerir aggression sound started (looping)");
        }
    }
    
    private void StopAggressionSound()
    {
        if (aggressionAudioSource != null)
        {
            aggressionAudioSource.Stop();
        }
    }
    
    private void PlayJumpcareSound()
    {
        if (jumpcareAudioSource != null && jumpcareSound != null)
        {
            jumpcareAudioSource.clip = jumpcareSound;
            jumpcareAudioSource.Play();
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Fenerir jumpcare sound played");
        }
    }
    
    #endregion
    
    #region Death System
    
    private void TriggerFenerirJumpcare()
    {
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] Fenerir jumpcare triggered!");
        
        // Reset other characters
        ResetOtherCharacters();
        
        StartCoroutine(TriggerFenerirJumpcareWithLights());
    }
    
    private System.Collections.IEnumerator TriggerFenerirJumpcareWithLights()
    {
        // KRITICKÉ: Nastav flag pro ochranu před DeactivateFenerir() během sekvence
        isInJumpscare = true;
        
        // 1. Vypnout světla s 0.9s zpožděním před jumpscarem (zde se stopne i aggression muzika)
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
            // KRITICKÉ: Zastavit aggression muziku při vypnutí světel (fail muzika)
            StopAggressionSound();
            
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
                Debug.Log($"[FenerirAI] Lights turned OFF - waiting {lightOffBeforeJumpscare}s before jumpscare");
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
                Debug.Log("[FenerirAI] Lights turned ON after jumpscare");
        }
    }
    
    #region Video Player Management
    
    private System.Collections.IEnumerator ShowDeadPanelWithVideoDelay()
    {
        PrepareDeadPanelVideo();
        
        // Počkat 0.1s na načtení videa
        yield return new WaitForSeconds(0.1f);
        
        if (fenerirDeadPanel != null)
        {
            fenerirDeadPanel.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Dead panel activated with video");
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
                Debug.Log("[FenerirAI] Dead panel video prepared");
        }
    }
    
    private void StartDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Play();
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Dead panel video started");
        }
    }
    
    private void StopDeadPanelVideo()
    {
        if (deadPanelVideoPlayer != null)
        {
            deadPanelVideoPlayer.Stop();
            deadPanelVideoPlayer.frame = 0;
            deadPanelVideoPlayer.gameObject.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Dead panel video stopped and deactivated");
        }
    }
    
    #endregion
    
    private System.Collections.IEnumerator HandleGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] HandleGameOver called - hiding dead panel");
        
        StopDeadPanelVideo();
        
        // Hide dead panel
        if (fenerirDeadPanel != null)
        {
            fenerirDeadPanel.SetActive(false);
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Dead panel hidden");
        }
        
        // Po skrytí dead panelu zapnout světla a generátor zpět
        yield return StartCoroutine(FlickerLightsJumpscare(true));
        
        // KRITICKÉ: Uvolnit jumpscare flag PŘED návratem do menu
        isInJumpscare = false;
        
        // Return to menu
        if (nightManager != null)
        {
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Calling ForceReturnToMenu");
            nightManager.ForceReturnToMenu();
        }
        else
        {
            Debug.LogWarning("[FenerirAI] NightShiftManagement reference is null!");
        }
    }
    
    #endregion
    
    #region Character Interaction
    
    private void ResetOtherCharacters()
    {
        // Reset Zofaye
        if (zofeyeAI != null)
        {
            zofeyeAI.ResetZofayeToStart();
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Zofaye reset after Fenerir death");
        }
        
        // Reset Thatcher
        if (thatcherAI != null)
        {
            thatcherAI.ResetAfterZofayeDeath();
            if (enableDebugLogs)
                Debug.Log("[FenerirAI] Thatcher reset after Fenerir death");
        }
    }
    
    #endregion
    
    #region Public API
    
    public void StartFenerirActivity()
    {
        // OPRAVA: Vždy aktivuj a zobraz Fenerira na náhodné pozici
        // Pohyb se řídí v Update() podle AI level
        isFenerirActive = true;
        isAngry = false;
        waitingForResponse = false;
        
        // Calculate AI timings
        CalculateAITimings();
        
        // Start at random position
        int startPosition = Random.Range(1, 4);
        SetFenerirPosition(startPosition);
        
        // Reset timers
        stayAtPositionTimer = 0f;
        responseTimer = 0f;
        angerTimer = 0f;
        
        // Set all smokeys to black
        SetAllSmokeysToBlack();
        
        // Update UI
        UpdateButtonVisibility();
        UpdatePanelVisibility();
        
        if (enableDebugLogs)
            Debug.Log($"[FenerirAI] Fenerir aktivován na pozici {startPosition} - AI: {GetEffectiveAILevel()}");
    }
    
    public void DeactivateFenerir()
    {
        // KRITICKÉ: Zastav všechny coroutiny (včetně kill timeru po You Failed)
        StopAllCoroutines();
        
        isFenerirActive = false;
        isAngry = false;
        waitingForResponse = false;
        
        // Hide all positions
        if (fenerirPosition1 != null) fenerirPosition1.SetActive(false);
        if (fenerirPosition2 != null) fenerirPosition2.SetActive(false);
        if (fenerirPosition3 != null) fenerirPosition3.SetActive(false);
        
        // Set all smokeys to black
        SetAllSmokeysToBlack();
        
        // Stop sounds
        StopAggressionSound();
        
        // Hide UI
        UpdateButtonVisibility();
        UpdatePanelVisibility();
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] Fenerir deactivated - all coroutines stopped");
    }
    
    public void ResetFenerirAfterCharacterDeath()
    {
        DeactivateFenerir();
        
        // Hide panels
        if (fenerirDeadPanel != null) fenerirDeadPanel.SetActive(false);
        if (fenerirMovingPanel != null) fenerirMovingPanel.SetActive(false);
        if (youLostPanel != null) youLostPanel.SetActive(false);
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] Fenerir reset after character death");
    }
    
    public bool IsFenerirActive => isFenerirActive;
    public bool IsFenerirAngry => isAngry;
    public int CurrentPosition => currentPosition;
    public int ActiveSmokey => activeSmokey;
    
    /// <summary>
    /// KRITICKÁ METODA: Okamžitě skryje všechna Fenerir tlačítka (volá WardenAI při spawnu)
    /// </summary>
    public void HideFenerirButtons()
    {
        if (rightButton != null)
            rightButton.gameObject.SetActive(false);
        if (middleButton != null)
            middleButton.gameObject.SetActive(false);
        if (leftButton != null)
            leftButton.gameObject.SetActive(false);
        
        // Skryj také "You Lost" panel (může být viditelný na CAM 09)
        if (youLostPanel != null)
            youLostPanel.SetActive(false);
        
        if (enableDebugLogs)
            Debug.Log("[FenerirAI] ===== FORCE HIDE ALL BUTTONS AND PANELS - Called by WardenAI =====");
    }
    
    #endregion
    
    #region Utility
    
    private void UpdateDisplayValues()
    {
        currentEffectiveAI = GetEffectiveAILevel();
        timeAtCurrentPosition = stayAtPositionTimer;
        timeToResponseRequired = waitingForResponse ? (responseTime - responseTimer) : 0f;
        
        if (!isFenerirActive)
            systemStatus = "Inactive";
        else if (isAngry)
            systemStatus = "ANGRY - Attacking";
        else if (waitingForResponse)
            systemStatus = $"Waiting for response ({timeToResponseRequired:F1}s)";
        else
            systemStatus = $"At position {currentPosition} ({timeAtCurrentPosition:F1}s)";
    }
    
    #endregion
}