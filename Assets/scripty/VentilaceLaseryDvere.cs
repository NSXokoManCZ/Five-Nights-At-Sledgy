using UnityEngine;
using UnityEngine.UI;

public class VentilaceLaseryDvere : MonoBehaviour
{
    [Header("Ventilace Lasery Dveře System")]
    [SerializeField] private string ventilaceVersion = "1.0";
    
    [Header("Dveře Objects")]
    [SerializeField] private GameObject dvere1; // Dveře 1 - GameObject který se aktivuje/deaktivuje
    [SerializeField] private GameObject dvere2; // Dveře 2
    [SerializeField] private GameObject dvere3; // Dveře 3
    [SerializeField] private GameObject dvere4; // Dveře 4
    [SerializeField] private GameObject dvere5; // Dveře 5
    [SerializeField] private GameObject dvere6; // Dveře 6
    
    [Header("Ovládací Tlačítka")]
    [SerializeField] private Button tlacitko1; // Tlačítko pro ovládání dveří 1
    [SerializeField] private Button tlacitko2; // Tlačítko pro ovládání dveří 2
    [SerializeField] private Button tlacitko3; // Tlačítko pro ovládání dveří 3
    [SerializeField] private Button tlacitko4; // Tlačítko pro ovládání dveří 4
    [SerializeField] private Button tlacitko5; // Tlačítko pro ovládání dveří 5
    [SerializeField] private Button tlacitko6; // Tlačítko pro ovládání dveří 6
    
    [Header("Barevné Schéma")]
    [SerializeField] private Color inactiveButtonColor = Color.green; // Zelená pro neaktivní tlačítka
    [SerializeField] private Color activeButtonColor = Color.red; // Červená pro aktivní tlačítko
    
    [Header("Audio System")]
    [SerializeField] private AudioSource buttonClickAudioSource; // AudioSource pro zvuk tlačítek
    [SerializeField] private AudioClip buttonClickSound; // Zvuk při kliknutí na tlačítko
    
    [Header("Laser Loop Audio")]
    [SerializeField] private AudioSource[] laserLoopAudioSources = new AudioSource[6]; // AudioSources pro loop zvuky každého laseru
    [SerializeField] private AudioClip[] laserLoopSounds = new AudioClip[6]; // Loop zvuky pro každý laser
    [SerializeField] private float laserLoopVolume = 0.7f; // Hlasitost laser loop zvuků
    
    [Header("Laser On/Off Audio")]
    [SerializeField] private AudioSource laserOnAudioSource; // AudioSource pro zvuk zapnutí laseru
    [SerializeField] private AudioSource laserOffAudioSource; // AudioSource pro zvuk vypnutí laseru
    [SerializeField] private AudioClip laserOnSound; // Zvuk při zapnutí laseru
    [SerializeField] private AudioClip laserOffSound; // Zvuk při vypnutí laseru
    
    [Header("Integration References")]
    [SerializeField] private CameryVentSystem ventCameraSystem; // Reference na ventilační kamerový systém
    [SerializeField] private Svetlo_Strop_Kancelar lightSystem; // Reference na světelný systém pro kontrolu elektřiny
    [SerializeField] private ElectricityManagement electricityManagement; // Reference na elektřinu management
    
    [Header("Fade Effects")]
    [SerializeField] private bool enableFadeEffects = true; // Zapnout fade efekty
    [SerializeField] private float fadeInDuration = 0.8f; // Délka fade in efektu v sekundách
    [SerializeField] private float fadeOutDuration = 0.5f; // Délka fade out efektu v sekundách
    [SerializeField] private bool enableFlickerEffect = true; // Zapnout problikání před fade in
    [SerializeField] private float flickerDuration = 0.3f; // Délka problikání
    [SerializeField] private int flickerCount = 3; // Počet probliků
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Křivka fade in
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // Křivka fade out
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Zapni/vypni debug logy
    
    // Private variables
    private GameObject[] dvereArray; // Pole všech dveří pro snadnou manipulaci
    private Button[] tlacitkaArray; // Pole všech tlačítek
    private int aktivniDvereIndex = -1; // Index aktuálně aktivních dveří (-1 = žádné aktivní)
    private bool[] laserLoopPlaying = new bool[6]; // Zda se přehrává loop pro každý laser
    
    // Fade effect variables
    private Renderer[][] laserRenderers = new Renderer[6][]; // Renderery pro každý laser (možnost více objektů)
    private float[][] originalAlphaValues = new float[6][]; // Originální alpha hodnoty pro každý renderer
    private bool[] isFading = new bool[6]; // Zda probíhá fade pro každý laser
    private Coroutine[] fadeCoroutines = new Coroutine[6]; // Reference na běžící fade coroutines
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere inicializován - verze: " + ventilaceVersion);
        
        // Inicializuj systém
        InitializeSystem();
        
        // Nastav počáteční stav (všechny dveře deaktivované)
        SetInitialState();
    }
    
    #region Initialization
    
    private void InitializeSystem()
    {
        // Vytvoř pole dveří pro snadnou manipulaci
        dvereArray = new GameObject[]
        {
            dvere1, dvere2, dvere3, dvere4, dvere5, dvere6
        };
        
        // Vytvoř pole tlačítek
        tlacitkaArray = new Button[]
        {
            tlacitko1, tlacitko2, tlacitko3, tlacitko4, tlacitko5, tlacitko6
        };
        
        // Nastav event listenery pro všechna tlačítka
        SetupButtonListeners();
        
        // Inicializuj audio systém
        InitializeAudioSystem();
        
        // Inicializuj fade efekty
        InitializeFadeSystem();
        
        // Najdi ventilační kamerový systém pokud není přiřazen
        FindVentCameraSystem();
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Systém inicializován s 6 dveřmi, tlačítky a audio");
    }
    
    private void SetupButtonListeners()
    {
        for (int i = 0; i < tlacitkaArray.Length; i++)
        {
            if (tlacitkaArray[i] != null)
            {
                int buttonIndex = i; // Capture pro closure
                tlacitkaArray[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
                
                if (enableDebugLogs)
                    Debug.Log($"VentilaceLaseryDvere: Event listener nastaven pro tlačítko {buttonIndex + 1}");
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning($"VentilaceLaseryDvere: Tlačítko {i + 1} není přiřazeno!");
            }
        }
    }
    
    private void SetInitialState()
    {
        // Deaktivuj všechny dveře
        DeactivateAllDoors();
        
        // Nastav všechna tlačítka na neaktivní stav (zelená)
        UpdateAllButtonColors();
        
        // Zastavit všechny laser loop zvuky
        StopAllLaserLoops();
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Počáteční stav nastaven - všechny dveře deaktivované");
    }
    
    private void InitializeAudioSystem()
    {
        // Nastav laser loop audio sources
        for (int i = 0; i < laserLoopAudioSources.Length; i++)
        {
            if (laserLoopAudioSources[i] != null)
            {
                laserLoopAudioSources[i].loop = true;
                laserLoopAudioSources[i].volume = laserLoopVolume;
                laserLoopAudioSources[i].playOnAwake = false;
                
                if (i < laserLoopSounds.Length && laserLoopSounds[i] != null)
                {
                    laserLoopAudioSources[i].clip = laserLoopSounds[i];
                }
                
                if (enableDebugLogs)
                    Debug.Log($"VentilaceLaseryDvere: Laser {i + 1} AudioSource inicializován");
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning($"VentilaceLaseryDvere: Laser {i + 1} AudioSource není přiřazen!");
            }
        }
        
        // Nastav button click audio source
        if (buttonClickAudioSource != null)
        {
            buttonClickAudioSource.loop = false;
            buttonClickAudioSource.playOnAwake = false;
            
            if (enableDebugLogs)
                Debug.Log("VentilaceLaseryDvere: Button click AudioSource inicializován");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("VentilaceLaseryDvere: Button click AudioSource není přiřazen!");
        }
        
        // Nastav laser on/off audio sources
        if (laserOnAudioSource != null)
        {
            laserOnAudioSource.loop = false;
            laserOnAudioSource.playOnAwake = false;
            laserOnAudioSource.clip = laserOnSound;
            
            if (enableDebugLogs)
                Debug.Log("VentilaceLaseryDvere: Laser ON AudioSource inicializován");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("VentilaceLaseryDvere: Laser ON AudioSource není přiřazen!");
        }
        
        if (laserOffAudioSource != null)
        {
            laserOffAudioSource.loop = false;
            laserOffAudioSource.playOnAwake = false;
            laserOffAudioSource.clip = laserOffSound;
            
            if (enableDebugLogs)
                Debug.Log("VentilaceLaseryDvere: Laser OFF AudioSource inicializován");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("VentilaceLaseryDvere: Laser OFF AudioSource není přiřazen!");
        }
    }
    
    private void FindVentCameraSystem()
    {
        if (ventCameraSystem == null)
        {
            ventCameraSystem = FindObjectOfType<CameryVentSystem>();
            
            if (ventCameraSystem != null && enableDebugLogs)
            {
                Debug.Log("VentilaceLaseryDvere: CameryVentSystem automaticky nalezen");
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("VentilaceLaseryDvere: CameryVentSystem nebyl nalezen!");
            }
        }
        
        // Najdi světelný systém pokud není přiřazen
        if (lightSystem == null)
        {
            lightSystem = FindObjectOfType<Svetlo_Strop_Kancelar>();
            
            if (lightSystem != null && enableDebugLogs)
            {
                Debug.Log("VentilaceLaseryDvere: Svetlo_Strop_Kancelar automaticky nalezen");
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("VentilaceLaseryDvere: Svetlo_Strop_Kancelar nebyl nalezen!");
            }
        }
    }
    
    #endregion
    
    #region Button Events
    
    private void OnButtonClicked(int buttonIndex)
    {
        if (enableDebugLogs)
            Debug.Log($"VentilaceLaseryDvere: Tlačítko {buttonIndex + 1} stisknuto");
        
        // Zkontroluj zda je elektřina zapnutá
        if (lightSystem != null && !lightSystem.IsLightOn)
        {
            if (enableDebugLogs)
                Debug.Log("VentilaceLaseryDvere: Tlačítko ignorováno - elektřina je vypnutá");
            return;
        }
        
        // Přehraj zvuk tlačítka
        PlayButtonClickSound();
        
        // Zkontroluj zda jsou tyto dveře již aktivní
        if (aktivniDvereIndex == buttonIndex)
        {
            // Dveře jsou již aktivní - deaktivuj je
            DeactivateDoors(buttonIndex);
            
            // Přehraj zvuk vypnutí
            PlayLaserDeactivationSound(buttonIndex);
            
            if (enableDebugLogs)
                Debug.Log($"VentilaceLaseryDvere: Dveře {buttonIndex + 1} deaktivovány");
        }
        else
        {
            // Aktivuj tyto dveře (automaticky deaktivuje ostatní)
            ActivateDoors(buttonIndex);
            
            // Přehraj zvuk zapnutí
            PlayLaserActivationSound(buttonIndex);
            
            if (enableDebugLogs)
                Debug.Log($"VentilaceLaseryDvere: Dveře {buttonIndex + 1} aktivovány");
        }
        
        // Aktualizuj laser loop audio
        UpdateLaserLoopAudio();
    }
    
    #endregion
    
    #region Door Management
    
    private void ActivateDoors(int doorIndex)
    {
        // Validace indexu
        if (doorIndex < 0 || doorIndex >= dvereArray.Length)
        {
            Debug.LogError($"VentilaceLaseryDvere: Neplatný index dveří: {doorIndex}");
            return;
        }
        
        // Pokud nějaké dveře už jsou aktivní, přehraj zvuk vypnutí před deaktivací
        if (aktivniDvereIndex >= 0)
        {
            PlayLaserDeactivationSound(aktivniDvereIndex);
            
            // Fade out předchozí laser
            if (enableFadeEffects)
            {
                StartFadeOut(aktivniDvereIndex);
            }
            else
            {
                // Okamžitě deaktivuj
                if (dvereArray[aktivniDvereIndex] != null)
                    dvereArray[aktivniDvereIndex].SetActive(false);
            }
        }
        else
        {
            // Žádné dveře nejsou aktivní, deaktivuj všechny pro jistotu
            DeactivateAllDoorsImmediate();
        }
        
        // Aktivuj vybrané dveře
        if (dvereArray[doorIndex] != null)
        {
            aktivniDvereIndex = doorIndex;
            
            // Informuj ElectricityManagement o změně stavu laserů
            NotifyElectricityManagementChange();
            
            if (enableFadeEffects)
            {
                // Aktivuj objekt a začni fade in
                dvereArray[doorIndex].SetActive(true);
                StartFadeIn(doorIndex);
            }
            else
            {
                // Okamžitě aktivuj s originální alpha hodnotou
                dvereArray[doorIndex].SetActive(true);
                SetLaserAlphaNormalized(doorIndex, 1f);
            }
            
            if (enableDebugLogs)
                Debug.Log($"VentilaceLaseryDvere: Dveře {doorIndex + 1} aktivovány s fade efektem");
        }
        else
        {
            Debug.LogWarning($"VentilaceLaseryDvere: Dveře {doorIndex + 1} nejsou přiřazeny!");
        }
        
        // Aktualizuj barvy tlačítek
        UpdateAllButtonColors();
    }
    
    private void DeactivateDoors(int doorIndex)
    {
        // Validace indexu
        if (doorIndex < 0 || doorIndex >= dvereArray.Length)
        {
            Debug.LogError($"VentilaceLaseryDvere: Neplatný index dveří: {doorIndex}");
            return;
        }
        
        // Deaktivuj konkrétní dveře
        if (dvereArray[doorIndex] != null && aktivniDvereIndex == doorIndex)
        {
            if (enableFadeEffects)
            {
                // Začni fade out
                StartFadeOut(doorIndex);
            }
            else
            {
                // Okamžitě deaktivuj
                dvereArray[doorIndex].SetActive(false);
            }
            
            if (enableDebugLogs)
                Debug.Log($"VentilaceLaseryDvere: Dveře {doorIndex + 1} deaktivovány s fade efektem");
        }
        
        // Reset aktivního indexu
        aktivniDvereIndex = -1;
        
        // Informuj ElectricityManagement o změně stavu laserů
        NotifyElectricityManagementChange();
        
        // Aktualizuj barvy tlačítek
        UpdateAllButtonColors();
    }
    
    private void DeactivateAllDoors()
    {
        for (int i = 0; i < dvereArray.Length; i++)
        {
            if (dvereArray[i] != null)
            {
                if (enableFadeEffects && dvereArray[i].activeInHierarchy)
                {
                    // Fade out pokud je objekt aktivní
                    StartFadeOut(i);
                }
                else
                {
                    // Okamžitě deaktivuj
                    dvereArray[i].SetActive(false);
                }
            }
        }
        
        aktivniDvereIndex = -1;
        
        // Informuj ElectricityManagement o změně stavu laserů
        NotifyElectricityManagementChange();
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Všechny dveře deaktivovány s fade efekty");
    }
    
    private void DeactivateAllDoorsImmediate()
    {
        for (int i = 0; i < dvereArray.Length; i++)
        {
            if (dvereArray[i] != null)
            {
                dvereArray[i].SetActive(false);
            }
        }
        
        // Reset aktivního indexu na -1 (žádné aktivní dveře)
        aktivniDvereIndex = -1;
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Všechny dveře okamžitě deaktivovány");
    }
    
    #endregion
    
    #region Button Visual Management
    
    private void UpdateAllButtonColors()
    {
        for (int i = 0; i < tlacitkaArray.Length; i++)
        {
            UpdateButtonColor(i);
        }
    }
    
    private void UpdateButtonColor(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= tlacitkaArray.Length || tlacitkaArray[buttonIndex] == null)
        {
            return;
        }
        
        Button button = tlacitkaArray[buttonIndex];
        Image buttonImage = button.GetComponent<Image>();
        
        if (buttonImage != null)
        {
            // Nastav barvu podle stavu
            Color targetColor = (aktivniDvereIndex == buttonIndex) ? activeButtonColor : inactiveButtonColor;
            buttonImage.color = targetColor;
            
            if (enableDebugLogs)
            {
                string colorName = (aktivniDvereIndex == buttonIndex) ? "červená (aktivní)" : "zelená (neaktivní)";
                Debug.Log($"VentilaceLaseryDvere: Tlačítko {buttonIndex + 1} nastaveno na {colorName}");
            }
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"VentilaceLaseryDvere: Tlačítko {buttonIndex + 1} nemá Image komponent!");
        }
    }
    
    #endregion
    
    #region Audio Management
    
    private void PlayButtonClickSound()
    {
        if (buttonClickAudioSource != null && buttonClickSound != null)
        {
            buttonClickAudioSource.PlayOneShot(buttonClickSound);
            
            if (enableDebugLogs)
                Debug.Log("VentilaceLaseryDvere: Button click zvuk přehrán");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("VentilaceLaseryDvere: Button click audio není nastaveno!");
        }
    }
    
    private void PlayLaserActivationSound(int laserIndex)
    {
        // Zkontroluj jestli jsme na správné kameře
        if (ventCameraSystem != null && ventCameraSystem.GetCurrentVentCam() == laserIndex)
        {
            if (laserOnAudioSource != null && laserOnSound != null)
            {
                laserOnAudioSource.PlayOneShot(laserOnSound);
                
                if (enableDebugLogs)
                    Debug.Log($"VentilaceLaseryDvere: Přehráván zvuk aktivace laseru {laserIndex + 1}");
            }
        }
        else if (enableDebugLogs)
        {
            int currentCam = ventCameraSystem != null ? ventCameraSystem.GetCurrentVentCam() : -1;
            Debug.Log($"VentilaceLaseryDvere: Zvuk aktivace laseru {laserIndex + 1} nepřehrán - jsme na kameře {currentCam + 1}");
        }
    }
    
    private void PlayLaserDeactivationSound(int laserIndex)
    {
        // Zkontroluj jestli jsme na správné kameře
        if (ventCameraSystem != null && ventCameraSystem.GetCurrentVentCam() == laserIndex)
        {
            if (laserOffAudioSource != null && laserOffSound != null)
            {
                laserOffAudioSource.PlayOneShot(laserOffSound);
                
                if (enableDebugLogs)
                    Debug.Log($"VentilaceLaseryDvere: Přehráván zvuk deaktivace laseru {laserIndex + 1}");
            }
        }
        else if (enableDebugLogs)
        {
            int currentCam = ventCameraSystem != null ? ventCameraSystem.GetCurrentVentCam() : -1;
            Debug.Log($"VentilaceLaseryDvere: Zvuk deaktivace laseru {laserIndex + 1} nepřehrán - jsme na kameře {currentCam + 1}");
        }
    }
    
    private void UpdateLaserLoopAudio()
    {
        if (ventCameraSystem == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("VentilaceLaseryDvere: CameryVentSystem není dostupný pro audio update");
            return;
        }
        
        // Získej aktuální ventilační kameru
        int currentVentCam = GetCurrentVentCamera();
        
        // Aktualizuj laser loop audio pro všechny lasery
        for (int i = 0; i < laserLoopAudioSources.Length; i++)
        {
            bool shouldPlay = (aktivniDvereIndex == i) && (currentVentCam == i) && ventCameraSystem.AreVentCamerasOpen();
            
            if (enableDebugLogs && aktivniDvereIndex == i)
            {
                Debug.Log($"VentilaceLaseryDvere: Laser {i + 1} - aktivni: {aktivniDvereIndex == i}, ventCam: {currentVentCam == i}, ventCamerasOpen: {ventCameraSystem.AreVentCamerasOpen()}, shouldPlay: {shouldPlay}");
            }
            
            if (shouldPlay && !laserLoopPlaying[i])
            {
                // Začni přehrávat laser loop
                StartLaserLoop(i);
            }
            else if (!shouldPlay && laserLoopPlaying[i])
            {
                // Zastav laser loop
                StopLaserLoop(i);
            }
        }
    }
    
    private void StartLaserLoop(int laserIndex)
    {
        if (laserIndex < 0 || laserIndex >= laserLoopAudioSources.Length)
            return;
            
        if (laserLoopAudioSources[laserIndex] != null)
        {
            laserLoopAudioSources[laserIndex].Play();
            laserLoopPlaying[laserIndex] = true;
            
            if (enableDebugLogs)
                Debug.Log($"VentilaceLaseryDvere: Laser {laserIndex + 1} loop audio spuštěn");
        }
    }
    
    private void StopLaserLoop(int laserIndex)
    {
        if (laserIndex < 0 || laserIndex >= laserLoopAudioSources.Length)
            return;
            
        if (laserLoopAudioSources[laserIndex] != null)
        {
            laserLoopAudioSources[laserIndex].Stop();
            laserLoopPlaying[laserIndex] = false;
            
            if (enableDebugLogs)
                Debug.Log($"VentilaceLaseryDvere: Laser {laserIndex + 1} loop audio zastaven");
        }
    }
    
    private void StopAllLaserLoops()
    {
        for (int i = 0; i < laserLoopAudioSources.Length; i++)
        {
            StopLaserLoop(i);
        }
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Všechny laser loop audio zastaveny");
    }
    
    private int GetCurrentVentCamera()
    {
        if (ventCameraSystem != null)
        {
            // Použij reflection nebo přímý přístup k CurrentVentCam
            try
            {
                var field = ventCameraSystem.GetType().GetField("CurrentVentCam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    return (int)field.GetValue(ventCameraSystem);
                }
            }
            catch (System.Exception e)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"VentilaceLaseryDvere: Chyba při získávání CurrentVentCam: {e.Message}");
            }
        }
        
        return -1; // Vrátí -1 pokud není možné získat kameru
    }
    
    // Veřejná metoda pro externí aktualizaci laser audio (volaná z CameryVentSystem)
    public void OnVentCameraChanged()
    {
        UpdateLaserLoopAudio();
    }
    
    #endregion
    
    #region Public Methods
    
    // Metody pro externí ovládání systému
    
    /// <summary>
    /// Aktivuje konkrétní dveře programaticky (1-6)
    /// </summary>
    public void ActivateDoorsById(int doorId)
    {
        int doorIndex = doorId - 1; // Převod z 1-6 na 0-5
        ActivateDoors(doorIndex);
    }
    
    /// <summary>
    /// Deaktivuje všechny dveře programaticky
    /// </summary>
    public void DeactivateAllDoorsExternal()
    {
        DeactivateAllDoors();
        UpdateAllButtonColors();
    }
    
    /// <summary>
    /// Vrací ID aktuálně aktivních dveří (1-6) nebo 0 pokud žádné nejsou aktivní
    /// </summary>
    public int GetActiveDoorId()
    {
        return (aktivniDvereIndex >= 0) ? aktivniDvereIndex + 1 : 0;
    }
    
    /// <summary>
    /// Zkontroluje zda jsou konkrétní dveře aktivní (1-6)
    /// </summary>
    public bool IsDoorActive(int doorId)
    {
        int doorIndex = doorId - 1;
        return aktivniDvereIndex == doorIndex;
    }
    
    /// <summary>
    /// Zkontroluje zda jsou nějaké dveře aktivní
    /// </summary>
    public bool IsAnyDoorActive()
    {
        return aktivniDvereIndex >= 0;
    }
    
    /// <summary>
    /// Simuluje kliknutí na tlačítko programaticky (1-6)
    /// </summary>
    public void SimulateButtonClick(int buttonId)
    {
        int buttonIndex = buttonId - 1;
        if (buttonIndex >= 0 && buttonIndex < tlacitkaArray.Length)
        {
            OnButtonClicked(buttonIndex);
        }
        else
        {
            Debug.LogError($"VentilaceLaseryDvere: Neplatné ID tlačítka: {buttonId}");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Kompletní reset systému na výchozí stav
    /// </summary>
    public void ResetSystem()
    {
        // Zastav všechny fade efekty
        StopAllFadeEffects();
        
        // Deaktivuj s fade efekty pokud jsou povoleny
        DeactivateAllDoors();
        
        // Aktualizuj barvy tlačítek
        UpdateAllButtonColors();
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Systém resetován na výchozí stav");
    }
    
    /// <summary>
    /// Reset systému bez debug zpráv (pro tiché resetování)
    /// </summary>
    public void SilentResetSystem()
    {
        // Zastav všechny fade efekty
        StopAllFadeEffects();
        
        // Okamžitě deaktivuj všechny dveře
        DeactivateAllDoorsImmediate();
        
        // Aktualizuj barvy tlačítek
        UpdateAllButtonColors();
        
        // Zastav všechny laser loops
        StopAllLaserLoops();
    }
    
    /// <summary>
    /// Reset systému při vypnutí elektřiny (úplný reset)
    /// </summary>
    public void PowerOffReset()
    {
        // Zastav všechny fade efekty okamžitě
        StopAllFadeEffects();
        
        // Zastav všechny laser loop audio
        StopAllLaserLoops();
        
        // Okamžitě deaktivuj všechny lasery (bez fade efektů)
        DeactivateAllDoorsImmediate();
        
        // Aktualizuj barvy tlačítek (všechny na zelenou)
        UpdateAllButtonColors();
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Systém resetován kvůli vypnutí elektřiny");
    }
    
    /// <summary>
    /// Získá informace o stavu systému pro debug
    /// </summary>
    public string GetSystemStatus()
    {
        string status = $"VentilaceLaseryDvere Status:\n";
        status += $"Aktivní dveře: {(aktivniDvereIndex >= 0 ? (aktivniDvereIndex + 1).ToString() : "Žádné")}\n";
        status += $"Fade efekty: {(enableFadeEffects ? "Zapnuty" : "Vypnuty")}\n";
        
        for (int i = 0; i < dvereArray.Length; i++)
        {
            string doorStatus = dvereArray[i] != null ? (dvereArray[i].activeInHierarchy ? "Aktivní" : "Neaktivní") : "Není přiřazeno";
            string buttonStatus = tlacitkaArray[i] != null ? "Přiřazeno" : "Není přiřazeno";
            string fadeStatus = isFading[i] ? "Fade probíhá" : "Bez fade";
            status += $"Dveře {i + 1}: {doorStatus}, Tlačítko {i + 1}: {buttonStatus}, {fadeStatus}\n";
        }
        
        return status;
    }
    
    #endregion
    
    #region Validation
    
    void OnValidate()
    {
        // Validace barev
        if (inactiveButtonColor == activeButtonColor)
        {
            Debug.LogWarning("VentilaceLaseryDvere: Barvy aktivního a neaktivního tlačítka jsou stejné!");
        }
    }
    
    #endregion
    
    #region Fade Effect System
    
    private void InitializeFadeSystem()
    {
        // Inicializuj renderery pro každý laser
        for (int i = 0; i < dvereArray.Length; i++)
        {
            if (dvereArray[i] != null)
            {
                // Najdi všechny renderery v tomto laser objektu a jeho potomcích
                Renderer[] renderers = dvereArray[i].GetComponentsInChildren<Renderer>();
                laserRenderers[i] = renderers;
                
                // Ulož originální alpha hodnoty
                originalAlphaValues[i] = new float[renderers.Length];
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (renderers[j] != null && renderers[j].material != null)
                    {
                        originalAlphaValues[i][j] = renderers[j].material.color.a;
                    }
                    else
                    {
                        originalAlphaValues[i][j] = 1f; // Fallback hodnota
                    }
                }
                
                if (enableDebugLogs)
                    Debug.Log($"VentilaceLaseryDvere: Laser {i + 1} - nalezeno {renderers.Length} rendererů, alpha hodnoty uloženy");
            }
            else
            {
                laserRenderers[i] = new Renderer[0];
                originalAlphaValues[i] = new float[0];
            }
        }
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Fade systém inicializován s originálními alpha hodnotami");
    }
    
    private void StartFadeIn(int laserIndex)
    {
        if (laserIndex < 0 || laserIndex >= dvereArray.Length)
            return;
            
        // Zastav předchozí fade coroutine
        if (fadeCoroutines[laserIndex] != null)
        {
            StopCoroutine(fadeCoroutines[laserIndex]);
        }
        
        // Nastav počáteční průhlednost na 0 (normalizovaně)
        SetLaserAlphaNormalized(laserIndex, 0f);
        
        // Spusť fade in coroutine
        fadeCoroutines[laserIndex] = StartCoroutine(FadeInCoroutine(laserIndex));
        
        if (enableDebugLogs)
            Debug.Log($"VentilaceLaseryDvere: Fade in spuštěn pro laser {laserIndex + 1}");
    }
    
    private void StartFadeOut(int laserIndex)
    {
        if (laserIndex < 0 || laserIndex >= dvereArray.Length)
            return;
            
        // Zastav předchozí fade coroutine
        if (fadeCoroutines[laserIndex] != null)
        {
            StopCoroutine(fadeCoroutines[laserIndex]);
        }
        
        // Spusť fade out coroutine
        fadeCoroutines[laserIndex] = StartCoroutine(FadeOutCoroutine(laserIndex));
        
        if (enableDebugLogs)
            Debug.Log($"VentilaceLaseryDvere: Fade out spuštěn pro laser {laserIndex + 1}");
    }
    
    private System.Collections.IEnumerator FadeInCoroutine(int laserIndex)
    {
        isFading[laserIndex] = true;
        
        // Flicker efekt před fade in
        if (enableFlickerEffect)
        {
            yield return StartCoroutine(FlickerCoroutine(laserIndex));
        }
        
        // Fade in efekt
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            float normalizedAlpha = fadeInCurve.Evaluate(progress);
            
            // Nastav alpha na interpolaci mezi 0 a originální alpha
            SetLaserAlphaNormalized(laserIndex, normalizedAlpha);
            
            yield return null;
        }
        
        // Ujisti se, že je alpha nastavena na originální hodnotu
        SetLaserAlphaNormalized(laserIndex, 1f);
        
        isFading[laserIndex] = false;
        fadeCoroutines[laserIndex] = null;
        
        if (enableDebugLogs)
            Debug.Log($"VentilaceLaseryDvere: Fade in dokončen pro laser {laserIndex + 1}");
    }
    
    private System.Collections.IEnumerator FadeOutCoroutine(int laserIndex)
    {
        isFading[laserIndex] = true;
        
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            float normalizedAlpha = fadeOutCurve.Evaluate(progress);
            
            SetLaserAlphaNormalized(laserIndex, normalizedAlpha);
            
            yield return null;
        }
        
        // Ujisti se, že je alpha nastavena na 0 a deaktivuj objekt
        SetLaserAlphaNormalized(laserIndex, 0f);
        
        if (dvereArray[laserIndex] != null)
        {
            dvereArray[laserIndex].SetActive(false);
        }
        
        isFading[laserIndex] = false;
        fadeCoroutines[laserIndex] = null;
        
        if (enableDebugLogs)
            Debug.Log($"VentilaceLaseryDvere: Fade out dokončen pro laser {laserIndex + 1}");
    }
    
    private System.Collections.IEnumerator FlickerCoroutine(int laserIndex)
    {
        float flickerInterval = flickerDuration / (flickerCount * 2f); // Čas pro zapnutí a vypnutí
        
        for (int i = 0; i < flickerCount; i++)
        {
            // Zapni (originální alpha)
            SetLaserAlphaNormalized(laserIndex, 1f);
            yield return new WaitForSeconds(flickerInterval);
            
            // Vypni (alpha 0)
            SetLaserAlphaNormalized(laserIndex, 0f);
            yield return new WaitForSeconds(flickerInterval);
        }
        
        if (enableDebugLogs)
            Debug.Log($"VentilaceLaseryDvere: Flicker efekt dokončen pro laser {laserIndex + 1}");
    }
    
    private void SetLaserAlpha(int laserIndex, float alpha)
    {
        if (laserIndex < 0 || laserIndex >= laserRenderers.Length)
            return;
            
        if (laserRenderers[laserIndex] != null)
        {
            foreach (Renderer renderer in laserRenderers[laserIndex])
            {
                if (renderer != null && renderer.material != null)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
            }
        }
    }
    
    /// <summary>
    /// Nastaví alpha s respektováním původních alpha hodnot
    /// </summary>
    /// <param name="laserIndex">Index laseru</param>
    /// <param name="normalizedAlpha">Normalizovaná alpha (0-1), kde 1 = původní alpha hodnota</param>
    private void SetLaserAlphaNormalized(int laserIndex, float normalizedAlpha)
    {
        if (laserIndex < 0 || laserIndex >= laserRenderers.Length)
            return;
            
        if (laserRenderers[laserIndex] != null && originalAlphaValues[laserIndex] != null)
        {
            for (int i = 0; i < laserRenderers[laserIndex].Length; i++)
            {
                Renderer renderer = laserRenderers[laserIndex][i];
                if (renderer != null && renderer.material != null && i < originalAlphaValues[laserIndex].Length)
                {
                    Color color = renderer.material.color;
                    
                    // Interpoluj mezi 0 a původní alpha hodnotou
                    float targetAlpha = originalAlphaValues[laserIndex][i] * normalizedAlpha;
                    color.a = targetAlpha;
                    
                    renderer.material.color = color;
                }
            }
        }
    }
    
    /// <summary>
    /// Okamžitě zastaví všechny fade efekty
    /// </summary>
    private void StopAllFadeEffects()
    {
        for (int i = 0; i < fadeCoroutines.Length; i++)
        {
            if (fadeCoroutines[i] != null)
            {
                StopCoroutine(fadeCoroutines[i]);
                fadeCoroutines[i] = null;
            }
            isFading[i] = false;
        }
        
        if (enableDebugLogs)
            Debug.Log("VentilaceLaseryDvere: Všechny fade efekty zastaveny");
    }
    
    /// <summary>
    /// Zkontroluje zda probíhá fade efekt pro konkrétní laser
    /// </summary>
    public bool IsLaserFading(int laserIndex)
    {
        if (laserIndex < 0 || laserIndex >= isFading.Length)
            return false;
            
        return isFading[laserIndex];
    }
    
    #endregion
    
    #region ElectricityManagement Integration
    
    /// <summary>
    /// Informuje ElectricityManagement o změně stavu laserů
    /// </summary>
    private void NotifyElectricityManagementChange()
    {
        if (electricityManagement == null)
        {
            electricityManagement = FindObjectOfType<ElectricityManagement>();
        }
        if (electricityManagement != null)
        {
            electricityManagement.OnLaserStateChanged();
        }
    }
    
    #endregion
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintStatus()
    {
        Debug.Log(GetSystemStatus());
    }
    
    #endregion
}