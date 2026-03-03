using UnityEngine;

/// <summary>
/// Helper script pro detekci stavu main menu
/// Připojte tento script k main menu panelu nebo objektu
/// </summary>
public class MainMenuDetector : MonoBehaviour
{
    [Header("Main Menu State")]
    [SerializeField] private bool isMainMenuActive = true;
    
    [Header("Auto Detection")]
    [SerializeField] private bool autoDetectByGameObject = true; // Detekovat podle activeInHierarchy tohoto objektu
    [SerializeField] private bool autoDetectByNightShift = true; // Detekovat podle NightShiftManagement
    
    [Header("Manual Control")]
    [SerializeField] private bool allowManualControl = true; // Povolit manuální ovládání
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Events pro notifikaci jiných systémů
    public System.Action<bool> OnMainMenuStateChanged;
    
    private NightShiftManagement nightShiftManagement;
    private bool lastDetectedState = true;
    
    void Start()
    {
        // Najdi NightShiftManagement
        if (autoDetectByNightShift && nightShiftManagement == null)
        {
            nightShiftManagement = FindFirstObjectByType<NightShiftManagement>();
        }
        
        // Inicializuj stav
        UpdateMainMenuState();
        
        if (enableDebugLogs)
            Debug.Log("[MainMenuDetector] Initialized");
    }
    
    void Update()
    {
        UpdateMainMenuState();
    }
    
    /// <summary>
    /// Aktualizuje stav main menu podle nastavených metod detekce
    /// </summary>
    private void UpdateMainMenuState()
    {
        bool currentState = DetectMainMenuState();
        
        // Pokud se stav změnil, notifikuj
        if (currentState != lastDetectedState)
        {
            isMainMenuActive = currentState;
            lastDetectedState = currentState;
            
            // Vyvolej event
            OnMainMenuStateChanged?.Invoke(isMainMenuActive);
            
            if (enableDebugLogs)
                Debug.Log($"[MainMenuDetector] Main menu state changed: {isMainMenuActive}");
        }
    }
    
    /// <summary>
    /// Detekuje stav main menu podle nastavených metod
    /// </summary>
    private bool DetectMainMenuState()
    {
        // Auto detekce podle GameObject
        if (autoDetectByGameObject)
        {
            bool objectActive = gameObject.activeInHierarchy;
            if (objectActive != isMainMenuActive && enableDebugLogs)
                Debug.Log($"[MainMenuDetector] GameObject detection: {objectActive}");
            return objectActive;
        }
        
        // Auto detekce podle NightShiftManagement
        if (autoDetectByNightShift && nightShiftManagement != null)
        {
            bool nightShiftState = nightShiftManagement.IsInMainMenu();
            if (nightShiftState != isMainMenuActive && enableDebugLogs)
                Debug.Log($"[MainMenuDetector] NightShift detection: {nightShiftState}");
            return nightShiftState;
        }
        
        // Fallback na manuální stav
        return isMainMenuActive;
    }
    
    /// <summary>
    /// Manuálně nastav stav main menu (pro externí ovládání)
    /// </summary>
    public void SetMainMenuActive(bool active)
    {
        if (!allowManualControl)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[MainMenuDetector] Manual control is disabled");
            return;
        }
        
        isMainMenuActive = active;
        OnMainMenuStateChanged?.Invoke(isMainMenuActive);
        
        if (enableDebugLogs)
            Debug.Log($"[MainMenuDetector] Manually set main menu state: {isMainMenuActive}");
    }
    
    /// <summary>
    /// Zapne main menu
    /// </summary>
    public void ShowMainMenu()
    {
        SetMainMenuActive(true);
    }
    
    /// <summary>
    /// Vypne main menu
    /// </summary>
    public void HideMainMenu()
    {
        SetMainMenuActive(false);
    }
    
    // Public properties
    public bool IsMainMenuActive => isMainMenuActive;
    
    // Event metody pro přihlášení/odhlášení
    public void RegisterForStateChanges(System.Action<bool> callback)
    {
        OnMainMenuStateChanged += callback;
    }
    
    public void UnregisterForStateChanges(System.Action<bool> callback)
    {
        OnMainMenuStateChanged -= callback;
    }
    
    void OnEnable()
    {
        if (autoDetectByGameObject)
        {
            UpdateMainMenuState();
        }
    }
    
    void OnDisable()
    {
        if (autoDetectByGameObject)
        {
            UpdateMainMenuState();
        }
    }
}