using UnityEngine;
using UnityEngine.UI;

public class FlashlightSystem : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight; // Hlavní světlo baterky
    [SerializeField] private Light spotLightComponent; // Přímý odkaz na Light komponent ze Spot Light
    [SerializeField] private GameObject flashlightVisual; // UI obrázek baterky pro vizuální efekt
    [SerializeField] private UnityEngine.UI.Image flashlightImage; // Přímý odkaz na Image komponent světla
    [SerializeField] private Camera playerCamera; // Reference na kameru hráče
    
    [Header("Flashlight Physical Models")]
    [SerializeField] private GameObject flashlightOnTable; // Baterka na stole (viditelná když je elektřina ZAPNUTÁ)
    [SerializeField] private GameObject flashlightInHand; // Baterka v ruce (viditelná když je elektřina VYPNUTÁ)
    
    [Header("Light Properties")]
    [SerializeField] private float lightRange = 10f; // Dosah světla
    [SerializeField] private float lightIntensity = 2f; // Intenzita světla
    [SerializeField] private float spotAngle = 30f; // Úhel kuželu světla
    [SerializeField] private Color lightColor = Color.white; // Barva světla
    
    [Header("Visual Effect")]
    [SerializeField] private RectTransform flashlightUI; // UI prvek pro vizuální efekt
    [SerializeField] private float uiScale = 1f; // Velikost UI efektu
    [SerializeField] private float flickerIntensity = 0.3f; // Intenzita blikání
    [SerializeField] private float minFlickerInterval = 1f; // Minimální interval mezi bliknutími
    [SerializeField] private float maxFlickerInterval = 3f; // Maximální interval mezi bliknutími
    [SerializeField] private float dimChance = 0.15f; // Šance na ztmavení (15%)
    
    [Header("Power System Reference")]
    [SerializeField] private Svetlo_Strop_Kancelar svetlo; // Reference na světlo pro kontrolu elektřiny
    
    [Header("Audio")]
    [SerializeField] private AudioSource flashlightOnAudioSource;
    [SerializeField] private AudioSource flashlightOffAudioSource;
    [SerializeField] private AudioClip flashlightOnSound;
    [SerializeField] private AudioClip flashlightOffSound;
    
    [Header("Flicker Audio")]
    [SerializeField] private AudioSource flickerAudioSource; // AudioSource pro blikací zvuky
    [SerializeField] private AudioClip[] flickerSounds = new AudioClip[5]; // Pole pro zvuky A, B, C, D, E
    [SerializeField] private float flickerSoundVolume = 0.5f; // Hlasitost blikacích zvuků
    
    [Header("Delay Settings")]
    [SerializeField] private float flashlightDelay = 0.7f; // Zpoždění zapnutí/vypnutí baterky
    
    [Header("UI Integration")]
    [SerializeField] private Button phoneGuyMuteButton; // Reference na Phone Guy mute tlačítko
    [SerializeField] private float phoneGuyCheckInterval = 0.1f; // Jak často kontrolovat Phone Guy UI
    
    // Private variables
    private bool isFlashlightActive = false;
    private bool isPowerOff = false;
    private float flickerTimer = 0f;
    private bool wasFlashlightActiveLastFrame = false;
    private float originalLightIntensity;
    private float originalUIAlpha;
    private float nextFlickerTime;
    private bool isFlickering = false;
    private float flickerDuration;
    private bool hasPendingFlashlightChange = false;
    private bool shouldLightFollowCursor = false; // Určuje, jestli má světlo sledovat kurzor
    private bool lastFlickerState = false; // Sleduje předchozí stav blikání pro detekci změny
    private bool audioMuted = false; // Pro blokování zvuků během reset
    private bool isManuallyForced = false; // Flag pro blokování automatického vypnutí po force enable
    
    // Phone Guy UI protection variables
    private bool isPhoneGuyUIReady = false; // Flag pro Phone Guy UI
    private float lastPowerChangeTime = 0f;
    private float minimumChangeInterval = 0.3f;
    
    void Start()
    {
        // Najdi kameru pokud není přiřazena
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Nastav počáteční stav
        SetupFlashlight();
        
        // Nastav počáteční viditelnost fyzických modelů baterky
        InitializeFlashlightModels();
        
        // Skryj baterku na začátku
        SetFlashlightActive(false);
        
        // Phone Guy UI protection initialization
        lastPowerChangeTime = Time.time;
        isPhoneGuyUIReady = false;
        
        // Spusť kontrolu Phone Guy UI
        InvokeRepeating(nameof(CheckPhoneGuyUI), 0.1f, phoneGuyCheckInterval);
        
        Debug.Log("[FlashlightSystem] Flashlight system initialized - Phone Guy protection active");
    }
    
    void Update()
    {
        // Zkontroluj stav elektřiny
        CheckPowerStatus();
        
        // DEBUG: Log když se mění kritické stavy
        if (Time.frameCount % 60 == 0) // Každou sekundu (při 60 FPS)
        {
            Debug.Log($"[FlashlightSystem] isPowerOff={isPowerOff}, isFlashlightActive={isFlashlightActive}, shouldLightFollow={shouldLightFollowCursor}, phoneGuyReady={isPhoneGuyUIReady}");
        }
        
        // Pokud je elektřina vypnutá nebo světlo má sledovat kurzor
        if (isPowerOff || shouldLightFollowCursor)
        {
            if (shouldLightFollowCursor || isFlashlightActive)
            {
                UpdateFlashlightDirection();
            }
            
            // Aktualizuj vizuální efekty (včetně UI pozice) pokud je baterka aktivní nebo sleduje kurzor
            if (isPowerOff || shouldLightFollowCursor)
            {
                UpdateVisualEffects();
            }
        }
        
        // Přehraj zvuky při změně stavu
        HandleAudioEffects();
    }
    
    private void SetupFlashlight()
    {
        if (flashlight != null)
        {
            flashlight.type = LightType.Spot;
            flashlight.range = lightRange;
            flashlight.intensity = lightIntensity;
            flashlight.spotAngle = spotAngle;
            flashlight.color = lightColor;
            flashlight.shadows = LightShadows.Soft; // Pro realistické stíny
            
            // Ulož původní intenzitu
            originalLightIntensity = lightIntensity;
        }
        
        // Nastav UI efekt
        if (flashlightUI != null)
        {
            flashlightUI.localScale = Vector3.one * uiScale;
        }
        
        // Ulož původní alpha UI
        if (flashlightVisual != null)
        {
            CanvasGroup canvasGroup = flashlightVisual.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                originalUIAlpha = canvasGroup.alpha;
            }
        }
        
        // Nastav první bliknutí
        SetNextFlickerTime();
    }
    
    /// <summary>
    /// Inicializuje počáteční stav fyzických modelů baterky
    /// </summary>
    private void InitializeFlashlightModels()
    {
        // Na začátku je elektřina ZAPNUTÁ → baterka NA STOLE
        if (flashlightOnTable != null)
        {
            flashlightOnTable.SetActive(true);
            Debug.Log("[FlashlightSystem] Flashlight ON TABLE - visible (power ON)");
        }
        
        if (flashlightInHand != null)
        {
            flashlightInHand.SetActive(false);
            Debug.Log("[FlashlightSystem] Flashlight IN HAND - hidden (power ON)");
        }
    }
    
    /// <summary>
    /// Aktualizuje viditelnost fyzických modelů baterky podle stavu elektřiny
    /// </summary>
    private void UpdateFlashlightModels()
    {
        if (isPowerOff)
        {
            // Elektřina VYPNUTÁ → baterka V RUCE
            if (flashlightOnTable != null)
            {
                flashlightOnTable.SetActive(false);
                Debug.Log("[FlashlightSystem] Flashlight ON TABLE - hidden (power OFF)");
            }
            
            if (flashlightInHand != null)
            {
                flashlightInHand.SetActive(true);
                Debug.Log("[FlashlightSystem] Flashlight IN HAND - visible (power OFF)");
            }
        }
        else
        {
            // Elektřina ZAPNUTÁ → baterka NA STOLE
            if (flashlightOnTable != null)
            {
                flashlightOnTable.SetActive(true);
                Debug.Log("[FlashlightSystem] Flashlight ON TABLE - visible (power ON)");
            }
            
            if (flashlightInHand != null)
            {
                flashlightInHand.SetActive(false);
                Debug.Log("[FlashlightSystem] Flashlight IN HAND - hidden (power ON)");
            }
        }
    }
    
    /// <summary>
    /// Kontroluje, zda je Phone Guy UI připravené
    /// </summary>
    private void CheckPhoneGuyUI()
    {
        // Pokud už je Phone Guy UI připravené, zastav kontrolu
        if (isPhoneGuyUIReady) 
        {
            CancelInvoke(nameof(CheckPhoneGuyUI));
            return;
        }
        
        // Kontroluj různé způsoby detekce Phone Guy UI
        bool phoneGuyReady = false;
        
        // Metoda 1: Přímá reference na mute button
        if (phoneGuyMuteButton != null)
        {
            // Kontroluj, zda je button aktivní a viditelný
            phoneGuyReady = phoneGuyMuteButton.gameObject.activeInHierarchy && 
                           phoneGuyMuteButton.interactable;
        }
        
        // Metoda 2: Fallback - hledej "mute" button podle jména
        if (!phoneGuyReady && phoneGuyMuteButton == null)
        {
            GameObject muteButtonObj = GameObject.Find("MuteButton") ?? 
                                     GameObject.Find("Mute") ?? 
                                     GameObject.Find("PhoneGuyMute");
            
            if (muteButtonObj != null)
            {
                Button foundButton = muteButtonObj.GetComponent<Button>();
                if (foundButton != null && foundButton.gameObject.activeInHierarchy)
                {
                    phoneGuyMuteButton = foundButton; // Uložíme pro příští použití
                    phoneGuyReady = true;
                }
            }
        }
        
        // Metoda 3: Časový fallback - po 3 sekundách předpokládej, že je ready
        if (!phoneGuyReady && Time.time > 3f)
        {
            phoneGuyReady = true;
            Debug.Log("[FlashlightSystem] Phone Guy UI assumed ready after 3 seconds timeout");
        }
        
        if (phoneGuyReady)
        {
            isPhoneGuyUIReady = true;
            CancelInvoke(nameof(CheckPhoneGuyUI));
            
            Debug.Log($"[FlashlightSystem] ===== PHONE GUY BUTTON APPEARED ===== Time:{Time.time:F2}, Frame:{Time.frameCount}");
            Debug.Log($"[FlashlightSystem] Current state: isPowerOff={isPowerOff}, isFlashlightActive={isFlashlightActive}, shouldFollow={shouldLightFollowCursor}");
        }
    }
    
    private void CheckPowerStatus()
    {
        bool newPowerStatus = false;
        
        // Zkontroluj stav elektřiny ze světla
        if (svetlo != null)
        {
            newPowerStatus = !svetlo.IsLightOn; // Baterka je aktivní když je světlo vypnuté
        }
        
        // NOVÝ PŘÍSTUP: VŽDY udržuj aktuální stav, blokuj jenom AUDIO a DELAY efekty
        bool stateChanged = (newPowerStatus != isPowerOff);
        
        if (stateChanged)
        {
            // Stav se OPRAVDU změnil - aktualizuj všechno
            isPowerOff = newPowerStatus;
            shouldLightFollowCursor = !isPowerOff;
            
            // Aktualizuj fyzické modely
            UpdateFlashlightModels();
            
            // Aktualizuj stav baterky OKAMŽITĚ (bez delay)
            bool shouldBeActive = isPowerOff;
            SetFlashlightActive(shouldBeActive);
            
            // Aktivuj/deaktivuj UI okamžitě
            if (flashlightVisual != null)
            {
                flashlightVisual.SetActive(shouldBeActive);
            }
            
            // JENOM pokud je Phone Guy UI ready, zaloguj změnu
            if (isPhoneGuyUIReady)
            {
                lastPowerChangeTime = Time.time;
                Debug.Log($"[FlashlightSystem] Power state changed: isPowerOff={isPowerOff}");
            }
            else
            {
                // Během Phone Guy init - tichá změna
                Debug.Log($"[FlashlightSystem] Silent power change: isPowerOff={isPowerOff}");
            }
        }
    }
    
    private void UpdateFlashlightDirection()
    {
        if (isFlashlightActive && playerCamera != null && flashlight != null)
        {
            // Nastav pozici baterky podle kamery
            flashlight.transform.position = playerCamera.transform.position;
            
            // Převeď pozici myši na světové souřadnice
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 10f; // Vzdálenost od kamery
            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(mouseScreenPos);
            
            // Nastav rotaci světla tak, aby svítilo směrem k myši
            Vector3 direction = (mouseWorldPos - flashlight.transform.position).normalized;
            flashlight.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    private void UpdateVisualEffects()
    {
        if (flashlightVisual != null && flashlightUI != null)
        {
            // Aktualizuj pozici UI efektu podle myši - sleduj kurzor když je baterka aktivní NEBO světlo sleduje kurzor
            if (isFlashlightActive || shouldLightFollowCursor)
            {
                Vector3 mousePos = Input.mousePosition;
                flashlightUI.position = mousePos;
            }
            
            // Horror-style nepravidelné blikání - pouze když je baterka aktivní
            if (isFlashlightActive)
            {
                // Zkontroluj jestli je čas na bliknutí
                if (Time.time >= nextFlickerTime)
                {
                    if (!isFlickering)
                    {
                        // Začni bliknutí - náhodná délka
                        isFlickering = true;
                        flickerDuration = Random.Range(0.05f, 0.25f);
                        nextFlickerTime = Time.time + flickerDuration;
                        
                        // Přehraj blikací zvuk v momentě začátku bliknutí
                        PlayFlickerSound();
                    }
                    else
                    {
                        // Ukonči bliknutí
                        isFlickering = false;
                        SetNextFlickerTime();
                    }
                }
                
                // Nastav intenzitu světla
                float currentIntensity = originalLightIntensity;
                float currentAlpha = originalUIAlpha;
                
                if (isFlickering)
                {
                    // Během bliknutí - náhodná intenzita nebo úplné vypnutí
                    if (Random.value < dimChance)
                    {
                        // Úplně zhasni na krátkou chvíli
                        currentIntensity = 0f;
                        currentAlpha = 0.1f;
                    }
                    else
                    {
                        // Nebo jen ztlum
                        float flickerValue = Random.Range(0.2f, 0.8f);
                        currentIntensity *= flickerValue;
                        currentAlpha *= flickerValue;
                    }
                }
                
                // Aplikuj na světlo
                if (flashlight != null)
                {
                    flashlight.intensity = currentIntensity;
                }
                
                // Aplikuj na UI
                if (flashlightVisual != null)
                {
                    CanvasGroup canvasGroup = flashlightVisual.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = currentAlpha;
                    }
                }
            }
        }
    }
    
    private void SetNextFlickerTime()
    {
        // Nastav náhodný interval pro další bliknutí
        float interval = Random.Range(minFlickerInterval, maxFlickerInterval);
        nextFlickerTime = Time.time + interval;
    }
    
    private void PlayFlickerSound()
    {
        // Zkontroluj, jestli máme AudioSource a zvuky
        if (flickerAudioSource != null && flickerSounds != null && flickerSounds.Length > 0)
        {
            // Vyfiltruj pouze neprázdné zvuky
            System.Collections.Generic.List<AudioClip> validSounds = new System.Collections.Generic.List<AudioClip>();
            
            for (int i = 0; i < flickerSounds.Length; i++)
            {
                if (flickerSounds[i] != null)
                {
                    validSounds.Add(flickerSounds[i]);
                }
            }
            
            // Pokud máme alespoň jeden platný zvuk
            if (validSounds.Count > 0)
            {
                // Vyber náhodný zvuk ze seznamu
                AudioClip randomSound = validSounds[Random.Range(0, validSounds.Count)];
                
                // Nastav hlasitost a přehraj zvuk
                flickerAudioSource.volume = flickerSoundVolume;
                flickerAudioSource.PlayOneShot(randomSound);
            }
        }
    }
    
    private System.Collections.IEnumerator DelayedFlashlightChange(bool shouldTurnOn)
    {
        hasPendingFlashlightChange = true;
        
        // Čekej zpožděný čas
        yield return new WaitForSeconds(flashlightDelay);
        
        if (shouldTurnOn)
        {
            // Elektřina se vypnula - aktivuj baterku až po zpoždění
            EnableFlashlight();
        }
        else
        {
            // Elektřina se zapnula - deaktivuj baterku až po zpoždění
            DisableFlashlight();
            // Přestaň sledovat kurzor až po vypnutí světla
            shouldLightFollowCursor = false;
        }
        
        hasPendingFlashlightChange = false;
    }
    
    private void HandleAudioEffects()
    {
        // Přehraj zvuk při zapnutí/vypnutí baterky (pouze pokud nejsou zvuky ztlumené)
        if (isFlashlightActive != wasFlashlightActiveLastFrame && !audioMuted)
        {
            if (isFlashlightActive && flashlightOnAudioSource != null && flashlightOnSound != null)
            {
                // Zapnutí - použij ON AudioSource
                flashlightOnAudioSource.PlayOneShot(flashlightOnSound);
            }
            else if (!isFlashlightActive && flashlightOffAudioSource != null && flashlightOffSound != null)
            {
                // Vypnutí - použij OFF AudioSource
                flashlightOffAudioSource.PlayOneShot(flashlightOffSound);
            }
        }
        
        wasFlashlightActiveLastFrame = isFlashlightActive;
    }
    
    private void EnableFlashlight()
    {
        // Kurzor zůstane viditelný
        Cursor.visible = true;
        
        // Aktivuj baterku
        SetFlashlightActive(true);
        
        // Aktivuj UI efekt
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(true);
        }
    }
    
    private void DisableFlashlight()
    {
        // Vrať normální kurzor
        Cursor.visible = true;
        
        // Deaktivuj baterku
        SetFlashlightActive(false);
        
        // Skryj UI efekt
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(false);
        }
    }
    
    private void SetFlashlightActive(bool active)
    {
        isFlashlightActive = active && isPowerOff; // Baterka funguje pouze při vypnuté elektřině
        
        // Nastav viditelnost hlavního světla
        if (flashlight != null)
        {
            flashlight.enabled = isFlashlightActive;
        }
        
        // Nastav viditelnost spot light přes přímý odkaz na Light komponent
        if (spotLightComponent != null)
        {
            spotLightComponent.enabled = isFlashlightActive;
        }
        
        // Nastav viditelnost UI efektu přes přímý odkaz na Image
        if (flashlightImage != null)
        {
            flashlightImage.enabled = isFlashlightActive;
            Color imageColor = flashlightImage.color;
            imageColor.a = isFlashlightActive ? 1f : 0f;
            flashlightImage.color = imageColor;
        }
        
        // Fallback přes Canvas Group (pokud je nastaven)
        if (flashlightVisual != null)
        {
            CanvasGroup canvasGroup = flashlightVisual.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isFlashlightActive ? 1f : 0f;
            }
        }
    }
    
    // Veřejné metody pro externí ovládání
    public bool IsFlashlightActive()
    {
        return isFlashlightActive;
    }
    
    public void SetFlashlightIntensity(float intensity)
    {
        lightIntensity = intensity;
        if (flashlight != null)
        {
            flashlight.intensity = lightIntensity;
        }
    }
    
    // Metoda pro tichý reset baterky (bez zvuků)
    public void SilentResetFlashlight()
    {
        // ZASTAVIT jakoukoli běžící delayed coroutinu
        StopAllCoroutines();
        
        // Okamžitě ztlum všechny audio efekty a zastav přehrávání
        audioMuted = true;
        
        if (flashlightOnAudioSource != null && flashlightOnAudioSource.isPlaying)
        {
            flashlightOnAudioSource.Stop();
        }
        if (flashlightOffAudioSource != null && flashlightOffAudioSource.isPlaying)
        {
            flashlightOffAudioSource.Stop();
        }
        
        // OKAMŽITĚ vypni světlo a vizuální efekty (bez čekání)
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(false);
        }
        
        // Ihned nastav stav (bez čekání 3 sekundy!)
        isFlashlightActive = false;
        
        // RESET fyzických modelů baterky - baterka NA STOLE
        InitializeFlashlightModels();
        
        // Po 0.5s obnov zvuky
        StartCoroutine(RestoreAudioAfterReset());
        
        Debug.Log("[FlashlightSystem] Silent reset completed - flashlight inactive, audio will restore in 0.5s");
    }
    
    // Pomocná coroutina pro obnovu zvuků
    private System.Collections.IEnumerator RestoreAudioAfterReset()
    {
        yield return new WaitForSeconds(0.5f);
        audioMuted = false;
        Debug.Log("[FlashlightSystem] Audio restored after silent reset");
    }
    
    /// <summary>
    /// Okamžitě zastaví všechny flickering zvuky a vypne baterku (pro ESC menu a smrt)
    /// </summary>
    public void ImmediateStopFlickering()
    {
        // OKAMŽITĚ zastav všechny flickering zvuky
        audioMuted = true;
        
        // Zastav flickering AudioSource
        if (flickerAudioSource != null && flickerAudioSource.isPlaying)
        {
            flickerAudioSource.Stop();
        }
        
        // Zastav všechny ostatní flashlight audio
        if (flashlightOnAudioSource != null && flashlightOnAudioSource.isPlaying)
        {
            flashlightOnAudioSource.Stop();
        }
        if (flashlightOffAudioSource != null && flashlightOffAudioSource.isPlaying)
        {
            flashlightOffAudioSource.Stop();
        }
        
        // OKAMŽITĚ vypni baterku
        isFlashlightActive = false;
        
        // OKAMŽITĚ vypni všechny světelné komponenty
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }
        
        // KRITICKÉ: Vypni spotlight komponent (spot light - baterka)
        if (spotLightComponent != null)
        {
            spotLightComponent.enabled = false;
        }
        
        // KRITICKÉ: Vypni flashlight image (canvas obrázek)
        if (flashlightImage != null)
        {
            flashlightImage.enabled = false;
        }
        
        // Vypni vizuální efekty
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(false);
        }
        
        // Reset flickering stavu
        isFlickering = false;
        
        // Reset manual force flag
        isManuallyForced = false;
        
        // Zastav všechny coroutiny baterky
        StopAllCoroutines();
        
        Debug.Log("[FlashlightSystem] Complete flashlight reset - all components disabled");
    }
    
    /// <summary>
    /// Veřejná metoda pro manuální zapnutí baterky (pro ThatcherAI)
    /// </summary>
    public void RestoreFlashlightManually()
    {
        if (isPowerOff) // Pouze pokud je světlo vypnuté
        {
            // Zruš předchozí čekající změnu
            if (hasPendingFlashlightChange)
            {
                StopCoroutine("DelayedFlashlightChange");
                hasPendingFlashlightChange = false;
            }
            
            // Okamžitě zapni baterku bez zpoždění
            EnableFlashlight();
            Debug.Log("FlashlightSystem: Flashlight manually restored");
        }
    }
    
    /// <summary>
    /// Veřejná metoda pro přímé zapnutí spotlight a canvas group (pro ThatcherAI)
    /// </summary>
    public void ForceEnableFlashlightComponents()
    {
        Debug.Log("FlashlightSystem: ForceEnableFlashlightComponents - ZAČÁTEK");
        
        // Nastav flag pro blokování automatického vypnutí
        isManuallyForced = true;
        
        // FORCE zapnutí všech komponent bez podmínek - IGNORUJ isPowerOff
        if (flashlight != null)
        {
            flashlight.enabled = true;
            Debug.Log("FlashlightSystem: Main flashlight FORCE enabled");
        }
        
        // FORCE zapnutí spot light přes přímý odkaz na Light komponent
        if (spotLightComponent != null)
        {
            spotLightComponent.enabled = true;
            Debug.Log("FlashlightSystem: Spot light FORCE enabled");
        }
        
        // Zapni flashlight image přímo
        if (flashlightImage != null)
        {
            flashlightImage.enabled = true;
            Color imageColor = flashlightImage.color;
            imageColor.a = 1f;
            flashlightImage.color = imageColor;
            Debug.Log("FlashlightSystem: Flashlight image FORCE enabled");
        }
        
        // Fallback přes Canvas Group
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(true);
            CanvasGroup canvasGroup = flashlightVisual.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
        
        // FORCE nastav interní stav bez podmínky isPowerOff
        isFlashlightActive = true;
        
        // Spusť coroutinu pro reset flagu po 5 sekundách
        StartCoroutine(ResetManualForceFlag());
        
        Debug.Log("FlashlightSystem: ALL flashlight components FORCE enabled (manual force flag set)");
    }
    
    /// <summary>
    /// Reset manual force flag po určité době
    /// </summary>
    private System.Collections.IEnumerator ResetManualForceFlag()
    {
        yield return new WaitForSeconds(5f); // Čekej 5 sekund
        isManuallyForced = false;
        Debug.Log("FlashlightSystem: Manual force flag reset - automatic control restored");
    }

    /// <summary>
    /// FNAC3-style progressive battery drain effect s postupným zhoršováním blikání
    /// </summary>
    public System.Collections.IEnumerator ProgressiveBatteryDrainEffect(float totalDuration)
    {
        if (!isFlashlightActive)
        {
            Debug.Log("FlashlightSystem: Progressive drain effect skipped - flashlight not active");
            yield break;
        }
        
        Debug.Log($"FlashlightSystem: Starting progressive battery drain effect for {totalDuration} seconds");
        
        float timer = 0f;
        bool lastFlickerState = true; // Začínáme se zapnutou baterkou
        
        while (timer < totalDuration)
        {
            // Výpočet progress (0 = začátek, 1 = konec)
            float progress = timer / totalDuration;
            
            // Postupně zhoršuj blikání
            // Na začátku: dlouhé intervaly (0.3s), na konci: rychlé blikání (0.05s)
            float flickerSpeed = Mathf.Lerp(0.3f, 0.05f, progress);
            
            // Postupně zvyšuj šanci na delší vypnutí
            float offChance = Mathf.Lerp(0.2f, 0.7f, progress); // 20% -> 70% šance na vypnutí
            
            // Rozhodni o dalším stavu
            bool shouldBeOn = Random.value > offChance;
            
            if (shouldBeOn != lastFlickerState)
            {
                // Změna stavu - aplikuj na světlo
                if (flashlight != null)
                {
                    flashlight.enabled = shouldBeOn && isPowerOff;
                }
                
                if (flashlightVisual != null)
                {
                    CanvasGroup canvasGroup = flashlightVisual.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = shouldBeOn ? originalUIAlpha : 0.1f;
                    }
                }
                
                // Přehraj flickering zvuk při změně stavu
                if (shouldBeOn != lastFlickerState)
                {
                    PlayFlickerSound();
                }
                
                lastFlickerState = shouldBeOn;
            }
            
            yield return new WaitForSeconds(flickerSpeed);
            timer += flickerSpeed;
        }
        
        // Na konci úplně vypni baterku
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(false);
        }
        
        Debug.Log("FlashlightSystem: Progressive battery drain effect completed - flashlight fully drained");
    }
}