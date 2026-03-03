using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DokkiHourlyAI
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
public class DokkiNightConfiguration
{
    public string nightName;
    public DokkiHourlyAI hourlyAI = new DokkiHourlyAI();
}

public class DokkiAI : MonoBehaviour
{
    [Header("=== DOKKI AI SYSTEM ===")]
    [SerializeField] private bool enableDokkiAI = true;
    [Range(0, 20)] [SerializeField] private int manualAILevel = 0;

    [Header("Advanced AI System")]
    [SerializeField] private bool useAdvancedAISystem = false;
    [SerializeField] private DokkiNightConfiguration[] nightConfigurations = new DokkiNightConfiguration[6];

    [Header("READ ONLY Status")]
    [SerializeField] [ReadOnly] private int currentPosition = 1; // 1-4 positions
    [SerializeField] [ReadOnly] private int currentEffectiveAI = 0;
    [SerializeField] [ReadOnly] private bool isFinalizingAttack = false;
    [SerializeField] [ReadOnly] private string systemStatus = "Waiting";
    [SerializeField] [ReadOnly] private bool powerIsOn = true;
    
    [Header("Monitor GameObjects (9 total)")]
    [SerializeField] private GameObject normalMonitor1; // Monitor bez hlavy
    [SerializeField] private GameObject normalMonitor2;
    [SerializeField] private GameObject normalMonitor3;
    [SerializeField] private GameObject dokkiMonitor1; // Monitor s hlavou Dokki
    [SerializeField] private GameObject dokkiMonitor2;
    [SerializeField] private GameObject dokkiMonitor3;
    [SerializeField] private GameObject offMonitor1; // Černé obrazovky (vypnutá elektřina)
    [SerializeField] private GameObject offMonitor2;
    [SerializeField] private GameObject offMonitor3;
    
    [Header("UI References")]
    [SerializeField] private Button camsOnButton;
    [SerializeField] private Button camsOffButton; // Pro automatické vypnutí kamer
    [SerializeField] private GameObject dokkiHeadFinalPanel;
    
    [Header("Camera System References")]
    [SerializeField] private CameraSystem cameraSystem; // Hlavní kamerový systém
    [SerializeField] private CameryVentSystem ventCameraSystem; // Ventilační kamerový systém
    
    [Header("Audio System")]
    [SerializeField] private AudioSource phoneRingingAudioSource;
    [SerializeField] private AudioClip phoneRingingClip;
    [SerializeField] private AudioSource camsErrorAudioSource;
    [SerializeField] private AudioClip camsErrorClip;
    
    [Header("Menu Detection")]
    [SerializeField] private GameObject[] menuPanels;
    
    [Header("System References")]
    [SerializeField] private ElectricityManagement electricitySystem;
    [SerializeField] private ShiftTimer shiftTimer;
    [SerializeField] private NightShiftManagement nightManager;
    [SerializeField] private ResetNight resetNight;
    [SerializeField] private ThatcherAI thatcherAI;
    [SerializeField] private WardenAI wardenAI;
    [SerializeField] private ZofeyeAI zofeyeAI;
    [SerializeField] private SledgeAI sledgeAI;
    [SerializeField] private FenerirAI fenerirAI;
    [SerializeField] private OryxAIVerse3 oryxAI;
    
    
    // Private variables
    private float progressTimer = 0f;
    private float finalizingTimer = 0f;
    private float currentProgressInterval = 15f; // Začíná s AI 1 = 15s
    private float currentProgressChance = 0.25f; // Začíná s AI 1 = 25%
    private Color originalCamsButtonColor;
    private bool camsBlocked = false;
    private bool wasElectricityOn = true;
    private bool menuIsActive = false; // Sledování menu stavu
    
    // Constants
    private const float FINALIZING_DURATION = 10f;
    private const float MIN_INTERVAL = 6f; // AI 20 = 6 sekund
    private const float MAX_INTERVAL = 12f; // AI 1 = 12 sekund
    private const float MIN_CHANCE = 0.35f; // AI 1 = 35%
    private const float MAX_CHANCE = 0.55f; // AI 20 = 55%
    
    void Start()
    {
        InitializeDokkiAI();
    }
    
    void Update()
    {
        if (!enableDokkiAI) return;
                // Check menu status - reset finalizing attack if menu opened
        CheckMenuStatus();
                // VŽDY monitoruj stav elektřiny a aktualizuj monitory (i během Warden útoku)
        UpdateElectricityStatus();
        UpdateEffectiveAI();
        
        // CHECK FOR WARDEN OR THATCHER - PAUSE ONLY PROGRESS/TIMERS IF ATTACKING
        // (ale ne vypínání monitorů při vypnutí elektřiny)
        if (wardenAI != null && wardenAI.IsWardenActive())
        {
            // Během Warden útoču: pouze aktualizuj monitory podle elektřiny, ale nepokračuj v progressu
            systemStatus = "Warden Active - Progress Paused";
            return;
        }
        
        if (thatcherAI != null && thatcherAI.IsThatcherActive())
        {
            // Během Thatcher encounter: pouze aktualizuj monitory podle elektřiny, ale nepokračuj v progressu
            systemStatus = "Thatcher Active - Progress Paused";
            return;
        }
        
        if (isFinalizingAttack)
        {
            HandleFinalizingAttack();
        }
        else if (powerIsOn && currentEffectiveAI > 0)
        {
            HandleProgressSystem();
        }
    }
    
    #region Initialization
    
    void InitializeDokkiAI()
    {
        Debug.Log("[DokkiAI] Initializing Dokki AI system");
        
        // Store original CamsOn button color
        if (camsOnButton != null)
        {
            originalCamsButtonColor = camsOnButton.image.color;
        }
        
        // Reset to starting position
        ResetDokkiProgress();
        
        // Setup CamsOn button listener
        if (camsOnButton != null)
        {
            camsOnButton.onClick.AddListener(OnCamsOnButtonClick);
        }
        
        Debug.Log("[DokkiAI] Dokki AI initialized - Ready to start");
    }
    
    #endregion
    
    #region Core Update Systems
    
    void UpdateElectricityStatus()
    {
        if (electricitySystem != null)
        {
            powerIsOn = electricitySystem.IsPowerOn();
            
            // Detect electricity state change
            if (wasElectricityOn != powerIsOn)
            {
                OnElectricityStateChanged(powerIsOn);
                wasElectricityOn = powerIsOn;
            }
        }
    }
    
    void UpdateEffectiveAI()
    {
        if (!powerIsOn)
        {
            currentEffectiveAI = 0;
            systemStatus = "Power Off - AI Disabled";
            return;
        }
        
        // KRITICKÉ: Kontrola menu má nejvyšší prioritu (včetně Custom Night panelu)
        if (nightManager != null && nightManager.IsInMainMenu())
        {
            currentEffectiveAI = 0;
            systemStatus = "Menu Active - AI Paused";
            return;
        }
        
        // KRITICKÉ: Zkontroluj, zda běží Custom Night
        if (nightManager != null && nightManager.IsCustomNightActive())
        {
            int customAI = nightManager.GetCustomNightAI("DOKKI");
            if (customAI >= 0)
            {
                currentEffectiveAI = customAI;
                systemStatus = $"Custom Night: AI {currentEffectiveAI} ({currentProgressInterval}s, {currentProgressChance:P0})";
                UpdateProgressParameters();
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
                systemStatus = $"Advanced AI: Night {currentNight}, Hour {currentHour}AM, AI {currentEffectiveAI} ({currentProgressInterval}s, {currentProgressChance:P0})";
            }
        }
        else
        {
            currentEffectiveAI = manualAILevel;
            systemStatus = $"Manual AI: {currentEffectiveAI} ({currentProgressInterval}s, {currentProgressChance:P0})";
        }
        
        UpdateProgressParameters();
    }
    
    void UpdateProgressParameters()
    {
        if (currentEffectiveAI <= 0)
        {
            currentProgressInterval = MAX_INTERVAL;
            currentProgressChance = MIN_CHANCE;
            return;
        }
        
        // Linear interpolation between AI 1 and AI 20
        float aiNormalized = Mathf.Clamp01((currentEffectiveAI - 1f) / 19f);
        
        currentProgressInterval = Mathf.Lerp(MAX_INTERVAL, MIN_INTERVAL, aiNormalized);
        currentProgressChance = Mathf.Lerp(MIN_CHANCE, MAX_CHANCE, aiNormalized);
    }
    
    #endregion
    
    #region Progress System
    
    void HandleProgressSystem()
    {
        if (currentPosition >= 4) return; // Already at final position
        
        progressTimer += Time.deltaTime;
        
        if (progressTimer >= currentProgressInterval)
        {
            progressTimer = 0f;
            AttemptProgress();
        }
    }
    
    void AttemptProgress()
    {
        float randomChance = Random.Range(0f, 1f);
        
        Debug.Log($"[DokkiAI] Progress attempt: {randomChance:F2} vs {currentProgressChance:F2} (AI {currentEffectiveAI}) - Interval: {currentProgressInterval}s");
        
        if (randomChance <= currentProgressChance)
        {
            AdvancePosition();
        }
        else
        {
            Debug.Log("[DokkiAI] Progress failed - staying at current position");
        }
    }
    
    void AdvancePosition()
    {
        if (currentPosition >= 4) return;
        
        currentPosition++;
        Debug.Log($"[DokkiAI] Advanced to position {currentPosition}");
        
        UpdateMonitorStates();
        
        if (currentPosition >= 4)
        {
            StartFinalizingAttack();
        }
    }
    
    #endregion
    
    #region Monitor Management
    
    void UpdateMonitorStates()
    {
        // Deactivate all monitors first
        SetMonitorActive(normalMonitor1, false);
        SetMonitorActive(normalMonitor2, false);
        SetMonitorActive(normalMonitor3, false);
        SetMonitorActive(dokkiMonitor1, false);
        SetMonitorActive(dokkiMonitor2, false);
        SetMonitorActive(dokkiMonitor3, false);
        SetMonitorActive(offMonitor1, false);
        SetMonitorActive(offMonitor2, false);
        SetMonitorActive(offMonitor3, false);
        
        if (!powerIsOn)
        {
            // Power off - show black screens
            SetMonitorActive(offMonitor1, true);
            SetMonitorActive(offMonitor2, true);
            SetMonitorActive(offMonitor3, true);
            Debug.Log("[DokkiAI] All monitors OFF - power down");
            return;
        }
        
        // Activate monitors based on current position
        switch (currentPosition)
        {
            case 1: // No Dokki heads
                SetMonitorActive(normalMonitor1, true);
                SetMonitorActive(normalMonitor2, true);
                SetMonitorActive(normalMonitor3, true);
                Debug.Log("[DokkiAI] Position 1: All normal monitors active");
                break;
                
            case 2: // Dokki on monitor 1
                SetMonitorActive(dokkiMonitor1, true);
                SetMonitorActive(normalMonitor2, true);
                SetMonitorActive(normalMonitor3, true);
                Debug.Log("[DokkiAI] Position 2: Dokki on monitor 1");
                break;
                
            case 3: // Dokki on monitors 1 & 2
                SetMonitorActive(dokkiMonitor1, true);
                SetMonitorActive(dokkiMonitor2, true);
                SetMonitorActive(normalMonitor3, true);
                Debug.Log("[DokkiAI] Position 3: Dokki on monitors 1 & 2");
                break;
                
            case 4: // Dokki on all monitors (finalizing attack)
                SetMonitorActive(dokkiMonitor1, true);
                SetMonitorActive(dokkiMonitor2, true);
                SetMonitorActive(dokkiMonitor3, true);
                Debug.Log("[DokkiAI] Position 4: Dokki on all monitors - FINALIZING ATTACK");
                break;
        }
    }
    
    void SetMonitorActive(GameObject monitor, bool active)
    {
        if (monitor != null)
        {
            monitor.SetActive(active);
        }
    }
    
    #endregion
    
    #region Finalizing Attack
    
    void StartFinalizingAttack()
    {
        isFinalizingAttack = true;
        finalizingTimer = 0f;
        camsBlocked = true;
        systemStatus = "FINALIZING ATTACK - Cameras Blocked";
        
        Debug.Log("[DokkiAI] ===== FINALIZING ATTACK STARTED =====");
        
        // NOVÉ: Automaticky vypni kamery pokud jsou zapnuté
        ForceCloseCameras();
        
        // Show Dokki head panel
        if (dokkiHeadFinalPanel != null)
        {
            dokkiHeadFinalPanel.SetActive(true);
        }
        
        // Block cameras and change button color
        if (camsOnButton != null)
        {
            camsOnButton.interactable = false;
            camsOnButton.image.color = Color.red;
        }
        
        // Start phone ringing sound (loop)
        if (phoneRingingAudioSource != null && phoneRingingClip != null)
        {
            phoneRingingAudioSource.clip = phoneRingingClip;
            phoneRingingAudioSource.loop = true;
            phoneRingingAudioSource.Play();
        }
        
        Debug.Log("[DokkiAI] Finalizing attack setup complete - 10 seconds countdown");
    }
    
    void HandleFinalizingAttack()
    {
        finalizingTimer += Time.deltaTime;
        
        if (finalizingTimer >= FINALIZING_DURATION)
        {
            EndFinalizingAttack();
        }
    }
    
    void EndFinalizingAttack()
    {
        Debug.Log("[DokkiAI] ===== FINALIZING ATTACK ENDED =====");
        
        isFinalizingAttack = false;
        camsBlocked = false;
        systemStatus = "Attack Completed - Resetting";
        
        // Hide Dokki head panel
        if (dokkiHeadFinalPanel != null)
        {
            dokkiHeadFinalPanel.SetActive(false);
        }
        
        // Restore cameras button
        if (camsOnButton != null)
        {
            camsOnButton.interactable = true;
            camsOnButton.image.color = originalCamsButtonColor;
        }
        
        // Stop phone ringing sound
        if (phoneRingingAudioSource != null)
        {
            phoneRingingAudioSource.Stop();
            phoneRingingAudioSource.loop = false;
        }
        
        // Reset progress
        ResetDokkiProgress();
        
        Debug.Log("[DokkiAI] Dokki progress reset - ready for next cycle");
    }
    
    #endregion
    
    #region Menu Detection
    
    private void CheckMenuStatus()
    {
        bool previousMenuState = menuIsActive;
        menuIsActive = false;
        
        // Check if any menu panel is active
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
        
        // If menu was just opened and finalizing attack is active - stop it!
        if (menuIsActive && !previousMenuState && isFinalizingAttack)
        {
            Debug.Log("[DokkiAI] Menu opened during finalizing attack - stopping ringing and hiding panel");
            
            // Stop phone ringing
            if (phoneRingingAudioSource != null)
            {
                phoneRingingAudioSource.Stop();
                phoneRingingAudioSource.loop = false;
            }
            
            // Hide panel
            if (dokkiHeadFinalPanel != null)
            {
                dokkiHeadFinalPanel.SetActive(false);
            }
            
            // Reset progress completely
            ResetDokkiProgress();
        }
    }
    
    #endregion
    
    #region Reset System
    
    /// <summary>
    /// Veřejná metoda pro resetování Dokki progressu - volá se z ThatcherAI při vypnutí elektřiny
    /// </summary>
    public void ResetDokkiProgress()
    {
        Debug.Log("[DokkiAI] ===== RESETTING DOKKI PROGRESS =====");
        
        currentPosition = 1;
        progressTimer = 0f;
        finalizingTimer = 0f;
        isFinalizingAttack = false;
        camsBlocked = false;
        
        // Stop finalizing attack if active
        if (phoneRingingAudioSource != null)
        {
            phoneRingingAudioSource.Stop();
            phoneRingingAudioSource.loop = false;
        }
        
        // Hide final panel
        if (dokkiHeadFinalPanel != null)
        {
            dokkiHeadFinalPanel.SetActive(false);
        }
        
        // Restore cameras button
        if (camsOnButton != null)
        {
            camsOnButton.interactable = true;
            camsOnButton.image.color = originalCamsButtonColor;
        }
        
        UpdateMonitorStates();
        systemStatus = "Reset Complete - Position 1";
        
        Debug.Log("[DokkiAI] Reset complete - Dokki back to position 1");
    }
    
    void OnElectricityStateChanged(bool powerOn)
    {
        Debug.Log($"[DokkiAI] Electricity state changed: {powerOn}");
        
        if (!powerOn)
        {
            // OPRAVENO: Pokud probíhá finalizující útok, NEPŘERUŠUJ ho!
            if (isFinalizingAttack)
            {
                Debug.Log("[DokkiAI] Power off during finalizing attack - CONTINUING attack (no reset)!");
                systemStatus = "Power Off - Attack Continues";
                // Pouze aktualizuj monitory na černé obrazovky, ale NERESΕΤUJ progress
                UpdateMonitorStates();
                return;
            }
            
            // Power turned off - reset progress and show black screens (jen pokud NENÍ útok)
            ResetDokkiProgress();
            systemStatus = "Power Off - Reset Triggered";
        }
        else
        {
            // NOVÉ: Při zapnutí elektřiny VŽDY resetuj Dokki progress
            // (ochrana proti postupu během Warden/Thatcher útoču nebo po něm)
            if ((wardenAI != null && (wardenAI.IsWardenActive() || wardenAI.IsWardenAttacking)) ||
                (thatcherAI != null && thatcherAI.IsThatcherActive()))
            {
                Debug.Log("[DokkiAI] Power restored during/after Warden/Thatcher - RESETTING Dokki progress!");
                ResetDokkiProgress();
                systemStatus = "Power Restored - Warden/Thatcher Protection Reset";
            }
            else
            {
                // Power turned on - ensure proper monitor states
                UpdateMonitorStates();
                systemStatus = "Power Restored";
            }
        }
    }
    
    #endregion
    
    #region Button Interaction
    
    void OnCamsOnButtonClick()
    {
        if (camsBlocked && isFinalizingAttack)
        {
            // Play error sound when trying to open cameras during finalizing attack
            if (camsErrorAudioSource != null && camsErrorClip != null)
            {
                camsErrorAudioSource.PlayOneShot(camsErrorClip);
            }
            
            Debug.Log("[DokkiAI] Cameras blocked - error sound played");
        }
    }
    
    /// <summary>
    /// OPRAVENÁ METODA: Automaticky vypne kamery když Dokki útočí - používá CamsOff tlačítko
    /// </summary>
    void ForceCloseCameras()
    {
        Debug.Log("[DokkiAI] Checking if player is on cameras...");
        
        // Pro teď používej jen CamsOff tlačítko - univerzální řešení pro všechny typy kamer
        if (camsOffButton != null)
        {
            Debug.Log("[DokkiAI] Player was on cameras - FORCING CamsOff due to Dokki attack!");
            camsOffButton.onClick.Invoke();
            Debug.Log("[DokkiAI] CamsOff button invoked - player returned to office");
        }
        else
        {
            Debug.Log("[DokkiAI] WARNING: CamsOff button not assigned - cannot force camera close");
        }
        
        // TODO: Přidat detekci stavu kamer když budou známé správné metody
        // if (cameraSystem != null && cameraSystem.IsActive()) { ... }
        // if (ventCameraSystem != null && ventCameraSystem.IsActive()) { ... }
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
    
    #region Universal Reset Integration
    
    public void TriggerUniversalReset(string reason)
    {
        Debug.Log($"[DokkiAI] Universal reset triggered: {reason}");
        ResetDokkiProgress();
    }
    
    void CheckForUniversalResetTriggers()
    {
        // Check for other animatronic jumpscares
        if (IsAnyAnimatronicJumpscaring())
        {
            TriggerUniversalReset("Other Animatronic Jumpscare");
            return;
        }
        
        // Check for ESC menu
        if (IsMenuActive())
        {
            // AI already set to 0, no additional reset needed
            return;
        }
    }
    
    bool IsAnyAnimatronicJumpscaring()
    {
        // Check other animatronics for jumpscare state
        // This would need specific implementation based on other AI scripts
        
        if (thatcherAI != null && thatcherAI.enabled)
        {
            // Check Thatcher jumpscare state if available
        }
        
        if (zofeyeAI != null && zofeyeAI.enabled)
        {
            // Check Zofeye jumpscare state if available
        }
        
        // Add other animatronic checks as needed
        
        return false; // Placeholder
    }
    
    #endregion
    
    #region Public Interface
    
    public int GetCurrentPosition()
    {
        return currentPosition;
    }
    
    public bool IsAttacking()
    {
        return isFinalizingAttack;
    }
    
    public bool AreCamerasBlocked()
    {
        return camsBlocked;
    }
    
    public int GetEffectiveAI()
    {
        return currentEffectiveAI;
    }
    
    #endregion
    
    #region Debug
    
    void OnValidate()
    {
        // Ensure night configurations array has 6 elements
        if (nightConfigurations == null || nightConfigurations.Length != 6)
        {
            nightConfigurations = new DokkiNightConfiguration[6];
            for (int i = 0; i < 6; i++)
            {
                if (nightConfigurations[i] == null)
                {
                    nightConfigurations[i] = new DokkiNightConfiguration();
                    nightConfigurations[i].nightName = $"Night {i + 1}";
                }
            }
        }
    }
    
    #endregion
}

// ReadOnly attribute for inspector
public class ReadOnlyAttribute : PropertyAttribute { }