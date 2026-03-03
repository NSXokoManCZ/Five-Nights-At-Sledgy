using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ElectricityManagement : MonoBehaviour
{
    [Header("Electricity Management System")]
    [SerializeField] private string systemVersion = "1.0";
    
    [Header("Power Settings")]
    [SerializeField] private float totalPower = 100f; // Celková energie na začátku (100%)
    [SerializeField] private float currentPower = 100f; // Aktuální energie
    [SerializeField] private float nightDuration = 450f; // 7.5 minut v sekundách (450s)
    
    [Header("Power Consumption Rates")]
    [SerializeField] private float usage1BaseMultiplier = 1.0f; // Základní multiplier pro usage 1 (nastavitelný pro ladění rychlosti)
    [SerializeField] private float door1ConsumptionRate = 0.15f; // Snížená spotřeba dveří 1 za sekundu
    [SerializeField] private float door2ConsumptionRate = 0.15f; // Snížená spotřeba dveří 2 za sekundu
    [SerializeField] private float cameraConsumptionRate = 0.12f; // Snížená spotřeba kamer za sekundu
    [SerializeField] private float laserConsumptionRate = 0.08f; // Snížená spotřeba laserů za sekundu
    [SerializeField] private float shockConsumptionMin = 0.3f; // Minimální spotřeba šoku
    [SerializeField] private float shockConsumptionMax = 1.2f; // Maximální spotřeba šoku
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI powerLeftText; // Text "Power Left"
    [SerializeField] private TextMeshProUGUI powerPercentageText; // Text s procentem energie
    [SerializeField] private Image[] usageImages = new Image[6]; // 6 obrázků pro usage indikaci (0-5)
    [SerializeField] private GameObject powerUIPanel; // Panel s elektřinou UI
    
    [Header("System References")]
    [SerializeField] private Svetlo_Strop_Kancelar_Tlacitko lightSystem; // Reference na světelný systém
    [SerializeField] private CameraSystem cameraSystem; // Reference na kamerový systém
    [SerializeField] private CameryVentSystem ventCameraSystem; // Reference na ventilační kamery
    [SerializeField] private VentilaceLaseryDvere laserSystem; // Reference na laser systém
    [SerializeField] private ShockingSystem shockingSystem; // Reference na shocking systém
    [SerializeField] private dvere_kancelar door1System; // Reference na dveře 1
    [SerializeField] private dvere_kancelar door2System; // Reference na dveře 2
    [SerializeField] private tlacitka_dvere_kancelar door1Button; // Reference na tlačítko dveří 1
    [SerializeField] private tlacitka_dvere_kancelar door2Button; // Reference na tlačítko dveří 2
    
    [Header("Additional Lighting System")]
    [SerializeField] private GameObject additionalLightsParent; // Rodičovský objekt pro dodatečná světla (vypne/zapne všechna světla najednou)
    
    [Header("Game State")]
    [SerializeField] private bool isGameActive = false; // Zda hra běží
    [SerializeField] private bool isLightOn = true; // Zda je elektřina zapnutá
    [SerializeField] private bool aggressive = false; // Agresivní režim při vynucené výpisu elektřiny
    [SerializeField] private int currentUsage = 1; // Aktuální usage (0-5)
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Zapni/vypni debug logy
    [SerializeField] private bool showPowerInInspector = true; // Zobraz aktuální energii v inspektoru
    
    // Private variables
    private float powerConsumptionPerSecond = 0f; // Aktuální spotřeba za sekundu
    private bool powerOutageTriggered = false; // Zda už byl spuštěn výpadek elektřiny
    private bool isGeneratorPermanentlyDestroyed = false; // KRITICKÝ FLAG: Generátor byl permanentně zničen Sledge (nelze znovu zapnout elektřinu)
    private Coroutine powerDrainCoroutine; // Reference na coroutinu spotřeby energie
    private float baseConsumption; // Základní spotřeba elektřiny (vypočítává se z délky noci)
    
    // Events
    public System.Action<float> OnPowerChanged; // Event při změně energie
    public System.Action OnPowerOutage; // Event při výpadku elektřiny
    public System.Action<int> OnUsageChanged; // Event při změně usage
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement inicializován - verze: " + systemVersion);
        
        InitializeSystem();
    }
    
    void Update()
    {
        // Zobraz aktuální energii v inspektoru pro debug
        if (showPowerInInspector)
        {
            // Hodnota se aktualizuje automaticky díky [SerializeField]
        }
        
        // Automaticky najdi reference pokud nejsou přiřazeny
        FindSystemReferencesIfNeeded();
    }
    
    #region Initialization
    
    private void InitializeSystem()
    {
        // Vypočítej základní spotřebu (100% / 450s = cca 0.2222%/s)
        baseConsumption = totalPower / nightDuration;
        
        // Nastav počáteční stav
        currentPower = totalPower;
        isLightOn = true;
        aggressive = false;
        powerOutageTriggered = false;
        
        // Inicializuj dodatečná světla (zapnuta na začátku)
        if (additionalLightsParent != null)
        {
            additionalLightsParent.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Dodatečná světla inicializována jako zapnutá");
        }
        
        // Nastav UI
        SetupUI();
        
        // Najdi system references
        FindSystemReferencesIfNeeded();
        
        // Spusť základní spotřebu elektřiny
        CalculateUsageAndStartDrain();
        
        if (enableDebugLogs)
            Debug.Log($"ElectricityManagement: Systém inicializován - základní spotřeba: {baseConsumption:F4}%/s");
    }
    
    private void SetupUI()
    {
        // Nastav text power left
        if (powerLeftText != null)
        {
            powerLeftText.text = "Power Left";
        }
        
        // Aktualizuj UI
        UpdatePowerUI();
        UpdateUsageUI();
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: UI nastaveno");
    }
    
    private void FindSystemReferencesIfNeeded()
    {
        // Najdi light system
        if (lightSystem == null)
        {
            lightSystem = FindFirstObjectByType<Svetlo_Strop_Kancelar_Tlacitko>();
        }
        
        // Najdi camera systems
        if (cameraSystem == null)
        {
            cameraSystem = FindFirstObjectByType<CameraSystem>();
        }
        
        if (ventCameraSystem == null)
        {
            ventCameraSystem = FindFirstObjectByType<CameryVentSystem>();
        }
        
        // Najdi laser system
        if (laserSystem == null)
        {
            laserSystem = FindFirstObjectByType<VentilaceLaseryDvere>();
        }
        
        // Najdi shocking system
        if (shockingSystem == null)
        {
            shockingSystem = FindFirstObjectByType<ShockingSystem>();
        }
        
        // Najdi door systems
        if (door1System == null || door2System == null)
        {
            dvere_kancelar[] doors = FindObjectsByType<dvere_kancelar>(FindObjectsSortMode.None);
            if (doors.Length >= 2)
            {
                door1System = doors[0];
                door2System = doors[1];
            }
        }
        
        // Najdi door buttons
        if (door1Button == null || door2Button == null)
        {
            tlacitka_dvere_kancelar[] doorButtons = FindObjectsByType<tlacitka_dvere_kancelar>(FindObjectsSortMode.None);
            if (doorButtons.Length >= 2)
            {
                door1Button = doorButtons[0];
                door2Button = doorButtons[1];
            }
        }
    }
    
    #endregion
    
    #region Power Management
    
    public void StartGame()
    {
        isGameActive = true;
        
        // Reset energie na 100%
        currentPower = totalPower;
        powerOutageTriggered = false;
        
        // Odblokuj tlačítko elektřiny pro novou hru
        if (lightSystem != null)
        {
            lightSystem.UnblockLightButton();
        }
        
        // Zkontroluj skutečný stav světla ze světelného systému
        if (lightSystem != null && lightSystem.GetComponent<Svetlo_Strop_Kancelar>() != null)
        {
            isLightOn = lightSystem.GetComponent<Svetlo_Strop_Kancelar>().IsLightOn;
        }
        else
        {
            isLightOn = true; // Výchozí stav
        }
        aggressive = false;
        
        // Spusť spotřebu elektřiny
        CalculateUsageAndStartDrain();
        
        // Force start power drain pokud je světlo zapnuté
        if (isGameActive && isLightOn && powerDrainCoroutine == null)
        {
            powerDrainCoroutine = StartCoroutine(PowerDrainCoroutine());
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Force start power drain coroutine");
        }
        
        // Aktualizuj UI
        UpdatePowerUI();
        UpdateUsageUI();
        
        if (enableDebugLogs)
            Debug.Log($"ElectricityManagement: Hra spuštěna s 100% energie, světlo: {(isLightOn ? "zapnuto" : "vypnuto")}");
    }
    
    public void StopGame()
    {
        isGameActive = false;
        
        // Zastav spotřebu elektřiny
        if (powerDrainCoroutine != null)
        {
            StopCoroutine(powerDrainCoroutine);
            powerDrainCoroutine = null;
        }
        
        // KRITICKÉ: Pokud je generátor permanentně zničený, ZABLOKUJ tlačítko
        if (isGeneratorPermanentlyDestroyed && lightSystem != null)
        {
            lightSystem.TriggerPowerOutage(); // Zablokuj tlačítko permanentně
            
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Hra zastavena - generátor ZNIČENÝ, tlačítko zablokováno");
        }
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: Hra zastavena");
    }
    
    public void ResetPower()
    {
        // KRITICKÉ: Pokud je generátor permanentně zničený, NEPOVOLUJ reset
        if (isGeneratorPermanentlyDestroyed)
        {
            // Zajisti že elektřina zůstane vypnutá a tlačítko zablokované
            currentPower = 0f;
            isLightOn = false;
            aggressive = true;
            powerOutageTriggered = true;
            
            // Znovu zablokuj tlačítko (pro jistotu)
            if (lightSystem != null)
            {
                lightSystem.TriggerPowerOutage();
            }
            
            // Vypni dodatečná světla
            if (additionalLightsParent != null)
            {
                additionalLightsParent.SetActive(false);
            }
            
            // Aktualizuj UI
            UpdatePowerUI();
            UpdateUsageUI();
            
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: ResetPower() BLOKOVÁN - generátor je PERMANENTNĚ ZNIČENÝ!");
            
            return; // NEKONČÍ reset - generátor je zničený
        }
        
        // Reset energie na 100%
        currentPower = totalPower;
        isLightOn = true;
        aggressive = false;
        powerOutageTriggered = false;
        
        // Odblokuj tlačítko elektřiny
        if (lightSystem != null)
        {
            lightSystem.UnblockLightButton();
        }
        
        // Zapni dodatečná světla při resetu
        if (additionalLightsParent != null)
        {
            additionalLightsParent.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Dodatečná světla zapnuta při resetu elektřiny");
        }
        
        // Aktualizuj UI
        UpdatePowerUI();
        UpdateUsageUI();
        
        // Restart power drain pokud je hra aktivní
        if (isGameActive)
        {
            CalculateUsageAndStartDrain();
        }
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: Energie resetována na 100%, tlačítko odblokováno");
    }
    
    public void OnLightToggled(bool lightOn)
    {
        isLightOn = lightOn;
        
        // Ovládání dodatečných světel
        if (additionalLightsParent != null)
        {
            additionalLightsParent.SetActive(lightOn);
            
            if (enableDebugLogs)
                Debug.Log($"ElectricityManagement: Dodatečná světla {(lightOn ? "zapnuta" : "vypnuta")}");
        }
        
        // Pokud hráč vypne elektřinu, neaktivuj agresivní režim
        if (!lightOn && currentPower > 0f)
        {
            aggressive = false;
            
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Hráč vypnul elektřinu - aggressive = false");
        }
        
        // Přepočítej usage a spotřebu
        CalculateUsageAndStartDrain();
        UpdateUsageUI();
    }
    
    #endregion
    
    #region Usage Calculation
    
    private void CalculateUsageAndStartDrain()
    {
        // Zastavit předchozí coroutinu
        if (powerDrainCoroutine != null)
        {
            StopCoroutine(powerDrainCoroutine);
        }
        
        // Spočítej usage a spotřebu
        CalculateCurrentUsage();
        
        // Spusť novou coroutinu pokud je hra aktivní
        if (isGameActive && isLightOn)
        {
            powerDrainCoroutine = StartCoroutine(PowerDrainCoroutine());
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Power drain coroutine spuštěna");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"ElectricityManagement: Power drain coroutine nespuštěna - isGameActive: {isGameActive}, isLightOn: {isLightOn}");
        }
    }
    
    private void CalculateCurrentUsage()
    {
        int usage = 0;
        float totalConsumption = baseConsumption * usage1BaseMultiplier; // Základní spotřeba s nastavitelným multiplierem
        
        if (!isLightOn)
        {
            // Elektřina vypnutá = usage 0, žádná spotřeba
            usage = 0;
            totalConsumption = 0f;
        }
        else
        {
            // Elektřina zapnutá = usage minimálně 1
            usage = 1;
            
            // Přidej spotřebu podle skutečně aktivních systémů
            // Všechny další multipliers jsou závislé na základní spotřebě s usage1BaseMultiplier
            
            if (IsDoor1Closed())
            {
                usage++;
                totalConsumption += door1ConsumptionRate * baseConsumption * usage1BaseMultiplier;
                if (enableDebugLogs)
                    Debug.Log("ElectricityManagement: Dveře 1 zavřené - usage +1");
            }
            
            if (IsDoor2Closed())
            {
                usage++;
                totalConsumption += door2ConsumptionRate * baseConsumption * usage1BaseMultiplier;
                if (enableDebugLogs)
                    Debug.Log("ElectricityManagement: Dveře 2 zavřené - usage +1");
            }
            
            if (AreCamerasActive())
            {
                usage++;
                totalConsumption += cameraConsumptionRate * baseConsumption * usage1BaseMultiplier;
                if (enableDebugLogs)
                    Debug.Log("ElectricityManagement: Kamery aktivní - usage +1");
            }
            
            if (AreLasersActive())
            {
                usage++;
                totalConsumption += laserConsumptionRate * baseConsumption * usage1BaseMultiplier;
                if (enableDebugLogs)
                    Debug.Log("ElectricityManagement: Lasery aktivní - usage +1");
            }
        }
        
        // Maximální usage je 5 (elektřina + 4 systémy)
        usage = Mathf.Clamp(usage, 0, 5);
        
        // Aktualizuj hodnoty
        currentUsage = usage;
        powerConsumptionPerSecond = totalConsumption;
        
        // Vyvolej event
        OnUsageChanged?.Invoke(currentUsage);
        
        if (enableDebugLogs)
            Debug.Log($"ElectricityManagement: Usage: {currentUsage}/5, Spotřeba: {powerConsumptionPerSecond:F4}%/s");
    }
    
    #endregion
    
    #region System State Checking
    
    private bool IsDoor1Closed()
    {
        if (door1System != null)
        {
            return !door1System.IsOpen; // Dveře jsou zavřené
        }
        return false;
    }
    
    private bool IsDoor2Closed()
    {
        if (door2System != null)
        {
            return !door2System.IsOpen; // Dveře jsou zavřené
        }
        return false;
    }
    
    private bool AreCamerasActive()
    {
        bool classicCameras = false;
        bool ventCameras = false;
        
        if (cameraSystem != null)
        {
            classicCameras = cameraSystem.AreCamerasOpen();
        }
        
        if (ventCameraSystem != null)
        {
            ventCameras = ventCameraSystem.AreVentCamerasOpen();
        }
        
        return classicCameras || ventCameras;
    }
    
    private bool AreLasersActive()
    {
        if (laserSystem != null)
        {
            return laserSystem.IsAnyDoorActive();
        }
        return false;
    }
    
    #endregion
    
    #region Power Drain
    
    private IEnumerator PowerDrainCoroutine()
    {
        while (isGameActive && isLightOn && currentPower > 0f)
        {
            // Odečti energii
            currentPower -= powerConsumptionPerSecond;
            
            // Zajisti, že energie neklesne pod 0
            currentPower = Mathf.Max(currentPower, 0f);
            
            // Aktualizuj UI
            UpdatePowerUI();
            
            // Vyvolej event
            OnPowerChanged?.Invoke(currentPower);
            
            // Zkontroluj výpadek elektřiny
            if (currentPower <= 0f && !powerOutageTriggered)
            {
                TriggerPowerOutage();
            }
            
            // Přepočítej usage (pro případ změn během hry)
            CalculateCurrentUsage();
            UpdateUsageUI();
            
            yield return new WaitForSeconds(1f); // Aktualizuj každou sekundu
        }
    }
    
    private void TriggerPowerOutage()
    {
        powerOutageTriggered = true;
        aggressive = true; // Nastav agresivní režim při vynucené výpisu
        isLightOn = false;
        currentUsage = 0;
        
        // Vypni elektřinu přes light system a zablokuj tlačítko
        if (lightSystem != null)
        {
            lightSystem.TriggerPowerOutage(); // Vypni elektřinu a zablokuj tlačítko
        }
        
        // Vypni dodatečná světla při výpadku elektřiny
        if (additionalLightsParent != null)
        {
            additionalLightsParent.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("ElectricityManagement: Dodatečná světla vypnuta při výpadku elektřiny");
        }
        
        // Aktualizuj UI
        UpdatePowerUI();
        UpdateUsageUI();
        
        // Vyvolej event
        OnPowerOutage?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: VÝPADEK ELEKTŘINY! Aggressive režim aktivován, tlačítko zablokováno");
    }
    
    #endregion
    
    #region Shock Consumption
    
    public void ConsumeShockPower()
    {
        if (isGameActive && isLightOn && currentPower > 0f)
        {
            // Náhodná spotřeba mezi 0.3% - 1.2%
            float shockConsumption = Random.Range(shockConsumptionMin, shockConsumptionMax);
            
            // Odečti energii
            currentPower -= shockConsumption;
            currentPower = Mathf.Max(currentPower, 0f);
            
            // Aktualizuj UI
            UpdatePowerUI();
            
            // Vyvolej event
            OnPowerChanged?.Invoke(currentPower);
            
            if (enableDebugLogs)
                Debug.Log($"ElectricityManagement: Šok spotřeboval {shockConsumption:F1}% energie");
            
            // Zkontroluj výpadek elektřiny
            if (currentPower <= 0f && !powerOutageTriggered)
            {
                TriggerPowerOutage();
            }
        }
    }
    
    /// <summary>
    /// Volá se při použití šoku (integration method)
    /// </summary>
    public void OnShockUsed()
    {
        ConsumeShockPower();
    }
    
    #endregion
    
    #region UI Management
    
    private void UpdatePowerUI()
    {
        if (powerPercentageText != null)
        {
            // Zobraz 0% pouze když je elektřina skutečně vypnutá
            // Jinak zaokrouhli nahoru aby se neobjevilo 0% když elektřina ještě funguje
            float displayPower;
            
            if (powerOutageTriggered || !isLightOn)
            {
                // Elektřina je vypnutá - zobraz skutečnou hodnotu (může být 0%)
                displayPower = Mathf.Round(currentPower);
            }
            else if (currentPower <= 0.5f)
            {
                // Pokud je energie velmi nízká ale elektřina funguje, zobraz minimálně 1%
                displayPower = 1f;
            }
            else
            {
                // Normální zaokrouhlení nahoru pro hodnoty nad 0.5%
                displayPower = Mathf.Ceil(currentPower);
            }
            
            powerPercentageText.text = $"{displayPower:F0}%";
        }
    }
    
    private void UpdateUsageUI()
    {
        // Aktualizuj usage obrázky (6 obrázků pro usage 0-5)
        for (int i = 0; i < usageImages.Length; i++)
        {
            if (usageImages[i] != null)
            {
                // Zobraz obrázek pokud odpovídá aktuálnímu usage
                // Obrázek 1 (index 0) = usage 0
                // Obrázek 2 (index 1) = usage 1
                // Obrázek 3 (index 2) = usage 2
                // Obrázek 4 (index 3) = usage 3
                // Obrázek 5 (index 4) = usage 4
                // Obrázek 6 (index 5) = usage 5
                usageImages[i].gameObject.SetActive(i == currentUsage);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"ElectricityManagement: UI aktualizováno - Usage: {currentUsage}/5, Zobrazen obrázek {currentUsage + 1}/6");
    }
    
    public void ShowPowerUI()
    {
        if (powerUIPanel != null)
        {
            powerUIPanel.SetActive(true);
        }
    }
    
    public void HidePowerUI()
    {
        if (powerUIPanel != null)
        {
            powerUIPanel.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Vrátí aktuální procento energie
    /// </summary>
    public float GetCurrentPowerPercentage()
    {
        return currentPower;
    }
    
    /// <summary>
    /// Vrátí aktuální usage (0-5)
    /// </summary>
    public int GetCurrentUsage()
    {
        return currentUsage;
    }
    
    /// <summary>
    /// Zkontroluje zda je elektřina zapnutá
    /// </summary>
    public bool IsPowerOn()
    {
        return isLightOn && currentPower > 0f;
    }
    
    /// <summary>
    /// Zkontroluje zda je aktivní agresivní režim
    /// </summary>
    public bool IsAggressive()
    {
        return aggressive;
    }
    
    /// <summary>
    /// Zkontroluje zda byl spuštěn výpadek elektřiny
    /// </summary>
    public bool IsPowerOutageTriggered()
    {
        return powerOutageTriggered;
    }
    
    /// <summary>
    /// Manuálně přepočítá usage (volat při změnách systémů)
    /// </summary>
    public void RecalculateUsage()
    {
        CalculateUsageAndStartDrain();
        UpdateUsageUI();
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: Usage ručně přepočítán");
    }
    
    /// <summary>
    /// Nastav energii na specifickou hodnotu (pro debugging)
    /// </summary>
    public void SetPower(float powerPercentage)
    {
        currentPower = Mathf.Clamp(powerPercentage, 0f, totalPower);
        UpdatePowerUI();
        
        if (enableDebugLogs)
            Debug.Log($"ElectricityManagement: Energie nastavena na {currentPower:F1}%");
    }
    
    /// <summary>
    /// KRITICKÁ METODA: Označ generátor jako permanentně zničený (volá Sledge při úspěšném útoku)
    /// Toto ZABLOKUJE možnost zapnout elektřinu až do konce noci
    /// </summary>
    public void MarkGeneratorAsDestroyed()
    {
        isGeneratorPermanentlyDestroyed = true;
        currentPower = 0f;
        isLightOn = false;
        aggressive = true;
        powerOutageTriggered = true;
        currentUsage = 0;
        
        // PERMANENTNĚ zablokuj tlačítko
        if (lightSystem != null)
        {
            lightSystem.TriggerPowerOutage();
        }
        
        // Zastav power drain coroutine
        if (powerDrainCoroutine != null)
        {
            StopCoroutine(powerDrainCoroutine);
            powerDrainCoroutine = null;
        }
        
        // Vypni dodatečná světla
        if (additionalLightsParent != null)
        {
            additionalLightsParent.SetActive(false);
        }
        
        // Aktualizuj UI
        UpdatePowerUI();
        UpdateUsageUI();
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: Generátor PERMANENTNĚ ZNIČEN - elektřina nelze znovu zapnout!");
    }
    
    /// <summary>
    /// Obnov generátor pro novou noc (volá se pouze při startu nové noci)
    /// </summary>
    public void RestoreGenerator()
    {
        isGeneratorPermanentlyDestroyed = false;
        
        if (enableDebugLogs)
            Debug.Log("ElectricityManagement: Generátor OBNOVEN pro novou noc - elektřina funguje normálně");
    }
    
    /// <summary>
    /// Zkontroluje zda je generátor permanentně zničený
    /// </summary>
    public bool IsGeneratorDestroyed()
    {
        return isGeneratorPermanentlyDestroyed;
    }
    
    #endregion
    
    #region Events & Integration
    
    /// <summary>
    /// Volá se při změně stavu dveří
    /// </summary>
    public void OnDoorStateChanged()
    {
        RecalculateUsage();
    }
    
    /// <summary>
    /// Volá se při změně stavu kamer
    /// </summary>
    public void OnCameraStateChanged()
    {
        RecalculateUsage();
    }
    
    /// <summary>
    /// Volá se při změně stavu laserů
    /// </summary>
    public void OnLaserStateChanged()
    {
        RecalculateUsage();
    }
    
    #endregion
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintStatus()
    {
        string status = $"ElectricityManagement Status:\n";
        status += $"Energie: {currentPower:F1}%\n";
        status += $"Usage: {currentUsage}/5\n";
        status += $"Usage 1 Multiplier: {usage1BaseMultiplier:F2}x\n";
        status += $"Spotřeba: {powerConsumptionPerSecond:F4}%/s\n";
        status += $"Elektřina: {(isLightOn ? "Zapnutá" : "Vypnutá")}\n";
        status += $"Agresivní: {aggressive}\n";
        status += $"Výpadek: {powerOutageTriggered}\n";
        status += $"Hra aktivní: {isGameActive}";
        
        Debug.Log(status);
    }
    
    /// <summary>
    /// Debug metoda pro výpis detailního usage
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintDetailedUsage()
    {
        string usage = $"Detailní Usage Analysis:\n";
        usage += $"Usage 1 Base Multiplier: {usage1BaseMultiplier:F2}x\n";
        usage += $"Základní elektřina: {(isLightOn ? "1" : "0")}\n";
        usage += $"Dveře 1: {(IsDoor1Closed() ? "+1" : "0")}\n";
        usage += $"Dveře 2: {(IsDoor2Closed() ? "+1" : "0")}\n";
        usage += $"Kamery: {(AreCamerasActive() ? "+1" : "0")}\n";
        usage += $"Lasery: {(AreLasersActive() ? "+1" : "0")}\n";
        usage += $"Celkový Usage: {currentUsage}/5\n";
        usage += $"Spotřeba: {powerConsumptionPerSecond:F4}%/s\n";
        usage += $"Základní spotřeba: {baseConsumption:F4}%/s";
        
        Debug.Log(usage);
    }
    
    #endregion
    
    void OnValidate()
    {
        // Validace hodnot v inspektoru
        totalPower = Mathf.Max(totalPower, 1f);
        currentPower = Mathf.Clamp(currentPower, 0f, totalPower);
        nightDuration = Mathf.Max(nightDuration, 60f);
        
        // Přepočítej základní spotřebu
        if (nightDuration > 0)
            baseConsumption = totalPower / nightDuration;
        
        // Validace consumption rates
        usage1BaseMultiplier = Mathf.Max(usage1BaseMultiplier, 0.1f); // Minimálně 0.1 pro použitelnost
        door1ConsumptionRate = Mathf.Max(door1ConsumptionRate, 0f);
        door2ConsumptionRate = Mathf.Max(door2ConsumptionRate, 0f);
        cameraConsumptionRate = Mathf.Max(cameraConsumptionRate, 0f);
        laserConsumptionRate = Mathf.Max(laserConsumptionRate, 0f);
        shockConsumptionMin = Mathf.Max(shockConsumptionMin, 0f);
        shockConsumptionMax = Mathf.Max(shockConsumptionMax, shockConsumptionMin);
        
        // Ujisti se, že máme přesně 6 usage images (pro usage 0-5)
        if (usageImages != null && usageImages.Length != 6)
        {
            Debug.LogWarning("ElectricityManagement: Usage Images musí mít přesně 6 prvků (pro usage 0-5)!");
        }
    }
}
