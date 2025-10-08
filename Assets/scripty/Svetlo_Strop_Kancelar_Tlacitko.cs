using UnityEngine;

public class Svetlo_Strop_Kancelar_Tlacitko : MonoBehaviour
{
    [Header("Reference na světlo")]
    [SerializeField] private Svetlo_Strop_Kancelar svetlo;
    
    [Header("Reference na dveře")]
    [SerializeField] private dvere_kancelar dvere;
    [SerializeField] private tlacitka_dvere_kancelar tlacitka_dveri;
    [SerializeField] private dvere_kancelar dvere2;
    [SerializeField] private tlacitka_dvere_kancelar tlacitka_dveri2;
    
    [Header("Reference na kamerový systém")]
    [SerializeField] private CameraSystem cameraSystem; // Nová reference
    [SerializeField] private VentilaceLaseryDvere laserDoorsSystem; // Reference na laser doors systém
    [SerializeField] private ShockingSystem shockingSystem; // Reference na shocking systém
    [SerializeField] private ElectricityManagement electricityManagement; // Reference na elektřinu management
    
    // Pole pro všechna tlačítka dveří
    private tlacitka_dvere_kancelar[] allDoorButtons;
    
    [Header("Zvuky světla")]
    [SerializeField] private AudioSource lightOnAudioSource; // AudioSource pro zvuk zapnutí
    [SerializeField] private AudioClip lightOnSound; // Zvuk zapnutí světla
    [SerializeField] private AudioSource lightOffAudioSource; // AudioSource pro zvuk vypnutí
    [SerializeField] private AudioClip lightOffSound; // Zvuk vypnutí světla
    
    [Header("Pozadí zvuk")]
    [SerializeField] private AudioSource backgroundAudioSource; // AudioSource pro pozadí zvuk (na cube)
    
    [Header("Vzdálenostní omezení")]
    [SerializeField] private float maxDistance = 1f; // Maximální vzdálenost pro aktivaci
    [SerializeField] private Transform player; // Reference na hráče (kameru)
    
    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 0.1f; // Doba zablokování tlačítka
    private float lastUsedTime = 0f;
    
    [Header("Power Outage Blocking")]
    [SerializeField] private bool isBlockedByPowerOutage = false; // Zablokováno kvůli výpadku elektřiny

    private void OnMouseDown()
    {
        // Zkontroluj cooldown
        if (Time.time - lastUsedTime < cooldownTime)
            return;
        
        // Zkontroluj blokování kvůli výpadku elektřiny
        if (isBlockedByPowerOutage)
        {
            Debug.Log("Světlo tlačítko: Zablokováno kvůli výpadku elektřiny!");
            return;
        }

        // Zkontroluj vzdálenost před aktivací
        if (!IsPlayerInRange())
            return;

        if (svetlo != null)
        {
            // Nastav čas posledního použití
            lastUsedTime = Time.time;
            
            // PŘED změnou stavu zjistíme aktuální stav světla
            bool wasLightOn = svetlo.IsLightOn;
            
            // Nyní změníme stav světla
            svetlo.ToggleLight();
            
            // Informuj ElectricityManagement o změně stavu světla
            if (electricityManagement == null)
            {
                electricityManagement = FindObjectOfType<ElectricityManagement>();
            }
            if (electricityManagement != null)
            {
                electricityManagement.OnLightToggled(svetlo.IsLightOn);
            }
            
            // Přehrání zvuků a ovládání pozadí podle PŮVODNÍHO stavu
            if (wasLightOn) // Pokud bylo světlo ZAPNUTÉ (nyní se vypnulo)
            {
                // Zastav zvuk zapnutí, pokud se přehrává
                if (lightOnAudioSource != null && lightOnAudioSource.isPlaying)
                {
                    lightOnAudioSource.Stop();
                }
                
                // Přehraj zvuk vypnutí
                if (lightOffAudioSource != null && lightOffSound != null)
                {
                    lightOffAudioSource.clip = lightOffSound;
                    lightOffAudioSource.Play();
                }
                
                // Zastav pozadí zvuk
                if (backgroundAudioSource != null)
                {
                    backgroundAudioSource.Stop();
                }
                
                // NOVÉ: Vypni kamery a nastav je jako nefunkční
                if (cameraSystem != null)
                {
                    cameraSystem.SetPowerOff();
                }
                
                // Ovládání dveří - světlo se vypnulo
                if (dvere != null)
                {
                    dvere.ForceOpen();
                }
                if (dvere2 != null)
                {
                    dvere2.ForceOpen();
                }
                if (tlacitka_dveri != null)
                {
                    tlacitka_dveri.SetBlocked(true);
                }
                if (tlacitka_dveri2 != null)
                {
                    tlacitka_dveri2.SetBlocked(true);
                }
                
                // Reset laser doors při vypnutí elektřiny
                if (laserDoorsSystem != null)
                {
                    laserDoorsSystem.PowerOffReset();
                }
                
                // Reset shocking systému při vypnutí elektřiny
                if (shockingSystem != null)
                {
                    shockingSystem.SetPowerOff(true);
                }
                
                // Vypni tlačítka dveří při vypnutí elektřiny
                SetDoorButtonsPowerOff(true);
            }
            else // Pokud bylo světlo VYPNUTÉ (nyní se zapnulo)
            {
                // Zastav zvuk vypnutí, pokud se přehrává
                if (lightOffAudioSource != null && lightOffAudioSource.isPlaying)
                {
                    lightOffAudioSource.Stop();
                }
                
                // Přehraj zvuk zapnutí
                if (lightOnAudioSource != null && lightOnSound != null)
                {
                    lightOnAudioSource.clip = lightOnSound;
                    lightOnAudioSource.Play();
                }
                
                // Spusť pozadí zvuk
                if (backgroundAudioSource != null)
                {
                    backgroundAudioSource.Play();
                }
                
                // NOVÉ: Zapni kamery a obnov jejich funkčnost
                if (cameraSystem != null)
                {
                    cameraSystem.SetPowerOn();
                }
                
                // Ovládání dveří - světlo se zapnulo
                if (tlacitka_dveri != null)
                {
                    tlacitka_dveri.SetBlocked(false);
                }
                if (tlacitka_dveri2 != null)
                {
                    tlacitka_dveri2.SetBlocked(false);
                }
                
                // Reset laser doors při zapnutí elektřiny (zůstanou vypnuté)
                if (laserDoorsSystem != null)
                {
                    laserDoorsSystem.SilentResetSystem();
                }
                
                // Obnov shocking systém při zapnutí elektřiny
                if (shockingSystem != null)
                {
                    shockingSystem.SetPowerOff(false);
                }
                
                // Zapni tlačítka dveří při zapnutí elektřiny
                SetDoorButtonsPowerOff(false);
            }
        }
    }

    private bool IsPlayerInRange()
    {
        // Pokud není přiřazen hráč, pokus se najít Main Camera
        if (player == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                player = mainCamera.transform;
        }

        // Pokud stále není hráč nalezen, vrať false
        if (player == null)
            return false;

        // Zkontroluj vzdálenost mezi hráčem a tlačítkem
        float distance = Vector3.Distance(player.position, transform.position);
        return distance <= maxDistance;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Spuštění pozadí hudby při startu (pokud je světlo zapnuté)
        if (backgroundAudioSource != null && svetlo != null && svetlo.IsLightOn)
        {
            backgroundAudioSource.Play();
        }
        
        // Najdi laser doors systém pokud není přiřazen
        if (laserDoorsSystem == null)
        {
            laserDoorsSystem = FindObjectOfType<VentilaceLaseryDvere>();
            if (laserDoorsSystem != null)
            {
                Debug.Log("Svetlo_Strop_Kancelar_Tlacitko: VentilaceLaseryDvere automaticky nalezen");
            }
        }
        
        // Najdi shocking systém pokud není přiřazen
        if (shockingSystem == null)
        {
            shockingSystem = FindObjectOfType<ShockingSystem>();
            if (shockingSystem != null)
            {
                Debug.Log("Svetlo_Strop_Kancelar_Tlacitko: ShockingSystem automaticky nalezen");
            }
        }
        
        // Inicializuj pole door buttons
        InitializeDoorButtons();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Metoda pro tichý reset elektřiny (bez zvuků)
    public void SilentResetPower()
    {
        if (svetlo != null)
        {
            // Nastav světlo na zapnuté (bez zvuků)
            svetlo.SilentSetLight(true);
            
            // Zastav všechny audio efekty
            if (lightOnAudioSource != null && lightOnAudioSource.isPlaying)
            {
                lightOnAudioSource.Stop();
            }
            if (lightOffAudioSource != null && lightOffAudioSource.isPlaying)
            {
                lightOffAudioSource.Stop();
            }
            
            // Tiše spusť pozadí zvuk
            if (backgroundAudioSource != null)
            {
                backgroundAudioSource.Play();
            }
            
            // Zapni kamery (bez audio efektů)
            if (cameraSystem != null)
            {
                cameraSystem.SetPowerOn();
            }
            
            // Obnov ovládání dveří
            if (tlacitka_dveri != null)
            {
                tlacitka_dveri.SetBlocked(false);
            }
            if (tlacitka_dveri2 != null)
            {
                tlacitka_dveri2.SetBlocked(false);
            }
            
            // Tiše resetuj laser doors (zůstanou vypnuté)
            if (laserDoorsSystem != null)
            {
                laserDoorsSystem.SilentResetSystem();
            }
            
            // Tiše obnov shocking systém
            if (shockingSystem != null)
            {
                shockingSystem.SetPowerOff(false);
            }
            
            // Tiše zapni tlačítka dveří
            SetDoorButtonsPowerOff(false);
            
            Debug.Log("Elektřina tiše resetována na zapnutou");
        }
    }
    
    /// <summary>
    /// Inicializuje pole tlačítek dveří
    /// </summary>
    private void InitializeDoorButtons()
    {
        // Vytvořím seznam všech door buttons
        var doorButtonsList = new System.Collections.Generic.List<tlacitka_dvere_kancelar>();
        
        if (tlacitka_dveri != null)
            doorButtonsList.Add(tlacitka_dveri);
            
        if (tlacitka_dveri2 != null)
            doorButtonsList.Add(tlacitka_dveri2);
        
        // Najdi všechna tlačítka dveří ve scéně
        tlacitka_dvere_kancelar[] foundButtons = FindObjectsOfType<tlacitka_dvere_kancelar>();
        foreach (var button in foundButtons)
        {
            if (!doorButtonsList.Contains(button))
                doorButtonsList.Add(button);
        }
        
        allDoorButtons = doorButtonsList.ToArray();
        
        Debug.Log($"Svetlo_Strop_Kancelar_Tlacitko: Nalezeno {allDoorButtons.Length} tlačítek dveří");
    }
    
    /// <summary>
    /// Nastaví power off stav všech tlačítek dveří
    /// </summary>
    private void SetDoorButtonsPowerOff(bool powerOff)
    {
        if (allDoorButtons != null)
        {
            foreach (var doorButton in allDoorButtons)
            {
                if (doorButton != null)
                {
                    doorButton.SetPowerOff(powerOff);
                }
            }
            
            string state = powerOff ? "vypnuta" : "zapnuta";
            Debug.Log($"Svetlo_Strop_Kancelar_Tlacitko: Všechna tlačítka dveří - elektřina {state}");
        }
    }
    
    /// <summary>
    /// Vypne elektřinu a zablokuje tlačítko při výpadku elektřiny (volané z ElectricityManagement)
    /// </summary>
    public void TriggerPowerOutage()
    {
        if (svetlo != null && svetlo.IsLightOn)
        {
            // Násilně vypni světlo bez možnosti ho znovu zapnout
            svetlo.SilentSetLight(false);
            
            // Zablokuj tlačítko
            isBlockedByPowerOutage = true;
            
            // Zastav všechny audio efekty
            if (lightOnAudioSource != null && lightOnAudioSource.isPlaying)
            {
                lightOnAudioSource.Stop();
            }
            if (backgroundAudioSource != null)
            {
                backgroundAudioSource.Stop();
            }
            
            // Přehraj zvuk vypnutí
            if (lightOffAudioSource != null && lightOffSound != null)
            {
                lightOffAudioSource.clip = lightOffSound;
                lightOffAudioSource.Play();
            }
            
            // Vypni všechny systémy
            if (cameraSystem != null)
            {
                cameraSystem.SetPowerOff();
            }
            
            if (dvere != null)
            {
                dvere.ForceOpen();
            }
            if (dvere2 != null)
            {
                dvere2.ForceOpen();
            }
            if (tlacitka_dveri != null)
            {
                tlacitka_dveri.SetBlocked(true);
            }
            if (tlacitka_dveri2 != null)
            {
                tlacitka_dveri2.SetBlocked(true);
            }
            
            if (laserDoorsSystem != null)
            {
                laserDoorsSystem.PowerOffReset();
            }
            
            if (shockingSystem != null)
            {
                shockingSystem.SetPowerOff(true);
            }
            
            SetDoorButtonsPowerOff(true);
            
            Debug.Log("Svetlo_Strop_Kancelar_Tlacitko: Výpadek elektřiny - tlačítko zablokováno!");
        }
    }
    
    /// <summary>
    /// Odblokuje tlačítko pro elektřinu (volat při resetu hry)
    /// </summary>
    public void UnblockLightButton()
    {
        isBlockedByPowerOutage = false;
        Debug.Log("Svetlo_Strop_Kancelar_Tlacitko: Tlačítko odblokováno");
    }
}
