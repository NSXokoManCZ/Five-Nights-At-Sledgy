using UnityEngine;

public class ShockingSystem : MonoBehaviour
{
    [Header("Shocking System")]
    [SerializeField] private string systemVersion = "1.0";
    
    [Header("Button Settings")]
    [SerializeField] private float cooldownTime = 5f; // Doba cooldownu v sekundách
    [SerializeField] private float maxClickDistance = 10f; // Maximální vzdálenost hráče pro aktivaci tlačítka
    [SerializeField] private Transform player; // Reference na hráče (automaticky se najde Main Camera)
    
    [Header("Visual Effects")]
    [SerializeField] private Renderer buttonRenderer; // Renderer tlačítka pro změnu barvy
    [SerializeField] private Material buttonMaterial; // Materiál tlačítka
    [SerializeField] private float darkenAmount = 0.5f; // O kolik procent ztmavit tlačítko (0-1)
    [SerializeField] private float powerOffDarkenAmount = 0.4f; // Ztmavení při vypnutí elektřiny (40%)
    
    [Header("Power System Integration")]
    [SerializeField] private Svetlo_Strop_Kancelar lightSystem; // Reference na světelný systém pro kontrolu elektřiny
    [SerializeField] private ElectricityManagement electricityManagement; // Reference na elektřinu management
    
    [Header("Audio System")]
    [SerializeField] private AudioSource shockAudioSource; // AudioSource pro zvuk elektřiny
    [SerializeField] private AudioClip shockSound; // Zvuk elektřiny
    [SerializeField] private AudioSource beepAudioSource; // AudioSource pro pípání
    [SerializeField] private AudioClip beepSound; // Zvuk pípání během cooldownu
    [SerializeField] private float beepDelay = 1f; // Zpoždění před začátkem pípání (v sekundách)
    
    [Header("Future Animatronic Integration")]
    [SerializeField] private bool shockTriggered = false; // Flag pro budoucí animatroniky
    [SerializeField] private float shockRange = 10f; // Dosah šoku pro budoucí animatroniky
    [SerializeField] private LayerMask animatronicLayer = -1; // Layer mask pro animatroniky
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Zapni/vypni debug logy
    [SerializeField] private bool showShockRange = false; // Zobraz dosah šoku v Scene view
    
    // Private variables
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private Color originalButtonColor;
    private Color darkenedButtonColor;
    private Color powerOffButtonColor; // Barva při vypnutí elektřiny
    private bool beepingActive = false;
    private bool hasTriggeredBeep = false;
    private bool isPowerOff = false; // Flag zda je elektřina vypnuta
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("ShockingSystem inicializován - verze: " + systemVersion);
        
        InitializeSystem();
    }
    
    void Update()
    {
        // Aktualizuj cooldown timer
        UpdateCooldown();
        
        // Automaticky najdi hráče pokud není přiřazen
        FindPlayerIfNeeded();
    }
    
    #region Initialization
    
    private void InitializeSystem()
    {
        // Najdi button renderer pokud není přiřazen
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
        }
        
        // Ulož původní barvu tlačítka
        if (buttonRenderer != null && buttonRenderer.material != null)
        {
            originalButtonColor = buttonRenderer.material.color;
            darkenedButtonColor = originalButtonColor * (1f - darkenAmount);
            powerOffButtonColor = originalButtonColor * (1f - powerOffDarkenAmount);
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Původní barva tlačítka uložena");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("ShockingSystem: Button Renderer nebo Material není přiřazen!");
        }
        
        // Nastav audio sources
        SetupAudioSources();
        
        // Najdi hráče
        FindPlayerIfNeeded();
        
        // Najdi light system
        FindLightSystemIfNeeded();
        
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Systém inicializován");
    }
    
    private void SetupAudioSources()
    {
        // Nastav shock audio source
        if (shockAudioSource != null)
        {
            shockAudioSource.loop = false;
            shockAudioSource.playOnAwake = false;
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Shock AudioSource nastaven");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("ShockingSystem: Shock AudioSource není přiřazen!");
        }
        
        // Nastav beep audio source
        if (beepAudioSource != null)
        {
            beepAudioSource.loop = true; // Pípání bude ve smyčce
            beepAudioSource.playOnAwake = false;
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Beep AudioSource nastaven");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("ShockingSystem: Beep AudioSource není přiřazen!");
        }
    }
    
    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                player = mainCamera.transform;
                
                if (enableDebugLogs)
                    Debug.Log("ShockingSystem: Hráč (Main Camera) automaticky nalezen");
            }
        }
    }
    
    #endregion
    
    #region Button Interaction
    
    void OnMouseDown()
    {
        // Debug info o vzdálenosti a pozicích
        if (enableDebugLogs)
        {
            if (player != null)
            {
                float distance = Vector3.Distance(player.position, transform.position);
                Debug.Log($"ShockingSystem: Vzdálenost hráče: {distance:F2}m (limit: {maxClickDistance}m)");
                Debug.Log($"ShockingSystem: Pozice hráče: {player.position}");
                Debug.Log($"ShockingSystem: Pozice tlačítka: {transform.position}");
            }
            else
            {
                Debug.LogError("ShockingSystem: Player reference je null!");
            }
        }
        
        // Zkontroluj zda je elektřina zapnutá
        if (isPowerOff || (lightSystem != null && !lightSystem.IsLightOn))
        {
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Tlačítko ignorováno - elektřina je vypnutá");
            return;
        }
        
        // Zkontroluj zda je hráč v dosahu
        if (!IsPlayerInRange())
        {
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Hráč není v dosahu tlačítka");
            return;
        }
        
        // Zkontroluj zda není tlačítko na cooldownu
        if (isOnCooldown)
        {
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Tlačítko je na cooldownu");
            return;
        }
        
        // Spusť šok
        TriggerShock();
    }
    
    private bool IsPlayerInRange()
    {
        if (player == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("ShockingSystem: Player reference je null - nelze zkontrolovat vzdálenost!");
            return false;
        }
        
        float distance = Vector3.Distance(player.position, transform.position);
        bool inRange = distance <= maxClickDistance;
        
        if (enableDebugLogs)
        {
            Debug.Log($"ShockingSystem: Kontrola dosahu - Vzdálenost: {distance:F2}m, Limit: {maxClickDistance}m, V dosahu: {inRange}");
        }
        
        return inRange;
    }
    
    #endregion
    
    #region Shock System
    
    private void TriggerShock()
    {
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Šok spuštěn!");
        
        // Nastav cooldown
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        hasTriggeredBeep = false;
        
        // Změň barvu tlačítka
        SetButtonDarkened(true);
        
        // Přehraj zvuk elektřiny
        PlayShockSound();
        
        // Spotřebuj energii přes ElectricityManagement
        if (electricityManagement == null)
        {
            electricityManagement = FindObjectOfType<ElectricityManagement>();
        }
        if (electricityManagement != null)
        {
            electricityManagement.ConsumeShockPower();
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("ShockingSystem: ElectricityManagement reference nebyla nalezena!");
        }
        
        // Nastav flag pro budoucí animatroniky
        shockTriggered = true;
        
        // Zpracuj budoucí animatroniky (připraveno pro rozšíření)
        ProcessAnimatronicsInRange();
        
        // Spusť cooldown timer pro pípání
        Invoke(nameof(StartBeepSound), beepDelay);
        
        if (enableDebugLogs)
            Debug.Log($"ShockingSystem: Šok aktivován, cooldown: {cooldownTime}s");
    }
    
    private void PlayShockSound()
    {
        if (shockAudioSource != null && shockSound != null)
        {
            shockAudioSource.PlayOneShot(shockSound);
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Zvuk elektřiny přehrán");
        }
    }
    
    private void StartBeepSound()
    {
        if (isOnCooldown && beepAudioSource != null && beepSound != null)
        {
            beepAudioSource.clip = beepSound;
            beepAudioSource.Play();
            beepingActive = true;
            hasTriggeredBeep = true;
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Pípání spuštěno");
        }
    }
    
    private void StopBeepSound()
    {
        if (beepAudioSource != null && beepingActive)
        {
            beepAudioSource.Stop();
            beepingActive = false;
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Pípání zastaveno");
        }
    }
    
    #endregion
    
    #region Visual Effects
    
    private void SetButtonDarkened(bool darkened)
    {
        if (buttonRenderer != null && buttonRenderer.material != null)
        {
            Color targetColor;
            
            // Priorita: power off > darkened > original
            if (isPowerOff)
            {
                targetColor = powerOffButtonColor;
            }
            else if (darkened)
            {
                targetColor = darkenedButtonColor;
            }
            else
            {
                targetColor = originalButtonColor;
            }
            
            buttonRenderer.material.color = targetColor;
            
            if (enableDebugLogs)
            {
                string colorState = isPowerOff ? "power off (40% ztmavení)" : 
                                   darkened ? "ztmavena (50% ztmavení)" : "původní";
                Debug.Log($"ShockingSystem: Barva tlačítka změněna na {colorState}");
            }
        }
    }
    
    #endregion
    
    #region Cooldown Management
    
    private void UpdateCooldown()
    {
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            
            // Spusť pípání pokud ještě nebylo spuštěno a uplynul delay
            if (!hasTriggeredBeep && cooldownTimer <= (cooldownTime - beepDelay))
            {
                StartBeepSound();
            }
            
            // Zkontroluj zda skončil cooldown
            if (cooldownTimer <= 0f)
            {
                EndCooldown();
            }
        }
    }
    
    private void EndCooldown()
    {
        isOnCooldown = false;
        cooldownTimer = 0f;
        shockTriggered = false;
        
        // Obnov původní barvu tlačítka
        SetButtonDarkened(false);
        
        // Zastav pípání
        StopBeepSound();
        
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Cooldown skončen, tlačítko je znovu použitelné");
    }
    
    #endregion
    
    #region Future Animatronic Integration
    
    private void ProcessAnimatronicsInRange()
    {
        // Připraveno pro budoucí implementaci s animatroniky
        
        if (enableDebugLogs)
            Debug.Log($"ShockingSystem: Hledám animatroniky v dosahu {shockRange}m...");
        
        // Zde bude v budoucnu logika pro detekci a ovlivnění animatroniků
        // Collider[] animatronicsInRange = Physics.OverlapSphere(transform.position, shockRange, animatronicLayer);
        
        // foreach (Collider animatronic in animatronicsInRange)
        // {
        //     // Pošli animatronika zpět na původní pozici
        //     AnimatronicController controller = animatronic.GetComponent<AnimatronicController>();
        //     if (controller != null)
        //     {
        //         controller.ReturnToOriginalPosition();
        //         Debug.Log($"ShockingSystem: {animatronic.name} byl shocknut a vrácen na původní pozici");
        //     }
        // }
        
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Animatronik systém připraven pro budoucí implementaci");
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Zkontroluje zda je tlačítko na cooldownu
    /// </summary>
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    /// <summary>
    /// Vrátí zbývající čas cooldownu
    /// </summary>
    public float GetRemainingCooldown()
    {
        return isOnCooldown ? cooldownTimer : 0f;
    }
    
    /// <summary>
    /// Zkontroluje zda byl nedávno spuštěn šok
    /// </summary>
    public bool WasShockTriggered()
    {
        return shockTriggered;
    }
    
    /// <summary>
    /// Ručně spustí šok (pro debugging nebo externí volání)
    /// </summary>
    public void ManualTriggerShock()
    {
        if (!isOnCooldown)
        {
            TriggerShock();
        }
        else if (enableDebugLogs)
        {
            Debug.Log("ShockingSystem: Nelze spustit manuální šok - cooldown aktivní");
        }
    }
    
    /// <summary>
    /// Reset systému do výchozího stavu
    /// </summary>
    public void ResetSystem()
    {
        // Zastav cooldown
        isOnCooldown = false;
        cooldownTimer = 0f;
        shockTriggered = false;
        
        // Obnov původní barvu
        SetButtonDarkened(false);
        
        // Zastav všechny zvuky
        if (shockAudioSource != null && shockAudioSource.isPlaying)
        {
            shockAudioSource.Stop();
        }
        
        StopBeepSound();
        
        // Reset flagů
        beepingActive = false;
        hasTriggeredBeep = false;
        
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Systém resetován do výchozího stavu");
    }
    
    #endregion
    
    #region Debug & Gizmos
    
    void OnDrawGizmosSelected()
    {
        if (showShockRange)
        {
            // Zobraz dosah šoku pro animatroniky
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, shockRange);
            
            // Zobraz dosah kliknutí
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, maxClickDistance);
        }
    }
    
    /// <summary>
    /// Debug metoda pro výpis stavu systému
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintStatus()
    {
        string status = $"ShockingSystem Status:\n";
        status += $"Cooldown: {(isOnCooldown ? $"{cooldownTimer:F1}s zbývá" : "Neaktivní")}\n";
        status += $"Shock Triggered: {shockTriggered}\n";
        status += $"Beeping: {beepingActive}\n";
        status += $"Player Distance: {(player != null ? Vector3.Distance(player.position, transform.position).ToString("F1") + "m" : "Hráč nenalezen")}";
        
        Debug.Log(status);
    }
    
    #endregion
    
    #region Power System Integration
    
    /// <summary>
    /// Nastaví stav vypnutí elektřiny
    /// </summary>
    /// <param name="powerOff">True pokud je elektřina vypnutá</param>
    public void SetPowerOff(bool powerOff)
    {
        isPowerOff = powerOff;
        
        if (isPowerOff)
        {
            // Zastav všechny zvuky při vypnutí elektřiny
            StopAllSounds();
            
            // Reset cooldownu při vypnutí elektřiny
            ResetCooldownState();
            
            // Nastav tmavě tlačítko
            SetButtonColorToPowerOff();
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Elektřina vypnuta - systém deaktivován, cooldown resetován");
        }
        else
        {
            // Obnov správnou barvu tlačítka při zapnutí elektřiny
            RestoreProperButtonColor();
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Elektřina zapnuta - systém aktivován");
        }
    }
    
    /// <summary>
    /// Najde light system pokud není přiřazen
    /// </summary>
    private void FindLightSystemIfNeeded()
    {
        if (lightSystem == null)
        {
            lightSystem = FindObjectOfType<Svetlo_Strop_Kancelar>();
            
            if (lightSystem != null && enableDebugLogs)
                Debug.Log("ShockingSystem: Light system automaticky nalezen");
            else if (enableDebugLogs)
                Debug.LogWarning("ShockingSystem: Light system nenalezen!");
        }
    }
    
    /// <summary>
    /// Zastavi všechny zvuky
    /// </summary>
    private void StopAllSounds()
    {
        // Zastav shock audio
        if (shockAudioSource != null && shockAudioSource.isPlaying)
        {
            shockAudioSource.Stop();
        }
        
        // Zastav beep audio
        if (beepAudioSource != null && beepAudioSource.isPlaying)
        {
            beepAudioSource.Stop();
        }
        
        // Zruš invoke pro beep sound pokud čeká
        CancelInvoke(nameof(StartBeepSound));
        
        // Reset audio flagů
        beepingActive = false;
        hasTriggeredBeep = false;
        
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Všechny zvuky zastaveny a invoke zrušen");
    }
    
    /// <summary>
    /// Nastaví barvu tlačítka pro vypnutou elektřinu
    /// </summary>
    private void SetButtonColorToPowerOff()
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = powerOffButtonColor;
            
            if (enableDebugLogs)
                Debug.Log("ShockingSystem: Tlačítko ztmaveno kvůli vypnuté elektřině");
        }
    }
    
    /// <summary>
    /// Resetuje cooldown stav bez vizuálních efektů
    /// </summary>
    private void ResetCooldownState()
    {
        // Zruš cooldown timer
        isOnCooldown = false;
        cooldownTimer = 0f;
        shockTriggered = false;
        
        // Zruš invoke pro beep sound pokud čeká
        CancelInvoke(nameof(StartBeepSound));
        
        // Reset audio flagů
        beepingActive = false;
        hasTriggeredBeep = false;
        
        if (enableDebugLogs)
            Debug.Log("ShockingSystem: Cooldown stav resetován kvůli vypnutí elektřiny");
    }
    
    /// <summary>
    /// Obnoví správnou barvu tlačítka podle aktuálního stavu
    /// </summary>
    private void RestoreProperButtonColor()
    {
        if (buttonRenderer != null)
        {
            // Pokud je cooldown aktivní, použij darkened barvu
            if (isOnCooldown)
            {
                buttonRenderer.material.color = darkenedButtonColor;
                
                if (enableDebugLogs)
                    Debug.Log("ShockingSystem: Obnovena darkened barva (cooldown stále aktivní)");
            }
            else
            {
                buttonRenderer.material.color = originalButtonColor;
                
                if (enableDebugLogs)
                    Debug.Log("ShockingSystem: Obnovena původní barva tlačítka");
            }
        }
    }
    
    #endregion
}