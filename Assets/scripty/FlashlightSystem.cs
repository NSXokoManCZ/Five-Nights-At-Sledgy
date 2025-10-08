using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight; // Hlavní světlo baterky
    [SerializeField] private GameObject flashlightVisual; // UI obrázek baterky pro vizuální efekt
    [SerializeField] private Camera playerCamera; // Reference na kameru hráče
    
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
    
    void Start()
    {
        // Najdi kameru pokud není přiřazena
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Nastav počáteční stav
        SetupFlashlight();
        
        // Skryj baterku na začátku
        SetFlashlightActive(false);
    }
    
    void Update()
    {
        // Zkontroluj stav elektřiny
        CheckPowerStatus();
        
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
    
    private void CheckPowerStatus()
    {
        bool newPowerStatus = false;
        
        // Zkontroluj stav elektřiny ze světla
        if (svetlo != null)
        {
            newPowerStatus = !svetlo.IsLightOn; // Baterka je aktivní když je světlo vypnuté
        }
        
        // Pokud se stav změnil
        if (newPowerStatus != isPowerOff)
        {
            isPowerOff = newPowerStatus;
            
            // Nastav sledování kurzoru podle stavu
            if (isPowerOff)
            {
                // Elektřina se vypnula - přestaň sledovat kurzor okamžitě
                shouldLightFollowCursor = false;
            }
            else
            {
                // Elektřina se zapnula - začni sledovat kurzor až do vypnutí světla
                shouldLightFollowCursor = true;
            }
            
            // Zruš předchozí čekající změnu
            if (hasPendingFlashlightChange)
            {
                StopCoroutine("DelayedFlashlightChange");
                hasPendingFlashlightChange = false;
            }
            
            // Spusť zpožděnou změnu baterky
            StartCoroutine(DelayedFlashlightChange(isPowerOff));
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
        
        // Nastav viditelnost světla v inspectoru
        if (flashlight != null)
        {
            flashlight.enabled = isFlashlightActive;
        }
        
        // Nastav viditelnost UI efektu
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
        
        // Spusť coroutinu pro zpožděné nastavení isFlashlightActive
        StartCoroutine(DelayedFlashlightStateUpdate());
        
        Debug.Log("Světlo baterky okamžitě vypnuto, stav se aktualizuje za 3 sekundy");
    }
    
    // Coroutina pro zpožděné aktualizování stavu baterky
    private System.Collections.IEnumerator DelayedFlashlightStateUpdate()
    {
        // Počkej 3 sekundy se ztlumenými zvuky
        yield return new WaitForSeconds(3f);
        
        // Nastav isFlashlightActive na false (bez zapnutí světla - to je už vypnuté)
        isFlashlightActive = false;
        
        // Po dalších 0.5 sekundách obnov zvuky
        yield return new WaitForSeconds(0.5f);
        audioMuted = false;
        
        Debug.Log("Stav baterky aktualizován (isFlashlightActive = false)");
    }
    
    /// <summary>
    /// Okamžitě zastaví všechny flickering zvuky a vypne baterku (pro ESC menu)
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
        
        // OKAMŽITĚ vypni světlo a vizuální efekty
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }
        if (flashlightVisual != null)
        {
            flashlightVisual.SetActive(false);
        }
        
        // Reset flickering stavu
        isFlickering = false;
        
        Debug.Log("Flickering zvuky okamžitě zastaveny při ESC");
    }
}