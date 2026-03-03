using UnityEngine;

public class PhoneGuy : MonoBehaviour
{
    [Header("Phone Guy Audio System")]
    [SerializeField] private string phoneGuyVersion = "1.0";
    
    [Header("Audio Components")]
    [SerializeField] private AudioSource phoneGuyAudioSource; // AudioSource pro přehrávání Phone Guy nahrávek
    [SerializeField] private AudioSource hangUpAudioSource; // Samostatný AudioSource pro hang-up zvuk
    
    [Header("Phone Guy Recordings")]
    [SerializeField] private AudioClip[] nightRecordings = new AudioClip[6]; // Pole pro 6 nahrávek (Night 1-6)
    
    [Header("Playback Settings")]
    [SerializeField] private float startDelay = 1f; // Pauza před spuštěním nahrávky (v sekundách)
    [SerializeField] private float fadeInDuration = 1f; // Doba fade in při spuštění nahrávky
    [SerializeField] private float fadeOutDuration = 1f; // Doba fade out při ukončení nahrávky
    [SerializeField] private bool autoFadeOut = true; // Automaticky fade out na konci nahrávky
    [SerializeField] private bool stopOnNightEnd = true; // Zastav nahrávku při ukončení noci
    
    [Header("Game Integration")]
    [SerializeField] private NightShiftManagement gameManager; // Reference na game manager
    
    [Header("MUTE Button UI")]
    [SerializeField] private Canvas muteCanvas; // Canvas s MUTE tlačítkem
    [SerializeField] private UnityEngine.UI.Button muteButton; // MUTE tlačítko
    [SerializeField] private AudioClip hangUpSound; // Zvuk pokládání telefonu
    
    [Header("MUTE Timing Settings")]
    [SerializeField] private float muteButtonShowDelay = 1.5f; // Zpoždění zobrazení MUTE tlačítka po začátku noci (1.5s)
    [SerializeField] private float muteButtonVisibleDuration = 15f; // Doba viditelnosti MUTE tlačítka (15s)
    [SerializeField] private float muteButtonAutoHideDelay = 15f; // Po kolika sekundách se MUTE tlačítko automaticky skryje
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Zapni/vypni debug logy
    
    // Private variables
    private bool isPlaying = false; // Zda se právě přehrává nahrávka
    private bool isFading = false; // Zda probíhá fade efekt
    private int currentNight = -1; // Aktuální noc pro kterou se přehrává nahrávka
    private bool isMuteButtonVisible = false; // Zda je MUTE tlačítko viditelné
    private System.Collections.IEnumerator muteButtonCoroutine; // Reference na coroutinu MUTE tlačítka
    
    void Start()
    {
        try
        {
            if (enableDebugLogs)
                Debug.Log("PhoneGuy inicializován - verze: " + phoneGuyVersion);
            
            // Inicializuj audio source
            InitializeAudioSource();
            
            // Najdi game manager pokud není přiřazený
            FindGameManager();
            
            // Připoj se k eventům game managera
            SubscribeToGameEvents();
            
            // Inicializuj MUTE tlačítko
            InitializeMuteButton();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PhoneGuy: Chyba při inicializaci: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        // Odpoj se od eventů při zničení objektu
        UnsubscribeFromGameEvents();
    }
    
    #region Initialization
    
    private void InitializeAudioSource()
    {
        // Pokud není přiřazený AudioSource, zkus ho najít na tomto objektu
        if (phoneGuyAudioSource == null)
        {
            phoneGuyAudioSource = GetComponent<AudioSource>();
            
            if (phoneGuyAudioSource == null)
            {
                Debug.LogWarning("PhoneGuy: AudioSource není přiřazený a nebyl nalezen na objektu!");
                return;
            }
        }
        
        // Nastav základní vlastnosti AudioSource
        phoneGuyAudioSource.playOnAwake = false;
        phoneGuyAudioSource.loop = false;
        
        // Inicializuj hang-up AudioSource
        if (hangUpAudioSource != null)
        {
            hangUpAudioSource.playOnAwake = false;
            hangUpAudioSource.loop = false;
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("PhoneGuy: Hang-up AudioSource není přiřazený!");
        }
        
        if (enableDebugLogs)
            Debug.Log("PhoneGuy AudioSource inicializován");
    }
    
    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<NightShiftManagement>();
            
            if (gameManager == null)
            {
                Debug.LogError("PhoneGuy: NightShiftManagement nebyl nalezen ve scéně!");
            }
            else if (enableDebugLogs)
            {
                Debug.Log("PhoneGuy: NightShiftManagement automaticky nalezen");
            }
        }
    }
    
    private void InitializeMuteButton()
    {
        // Skryj MUTE canvas na začátku
        if (muteCanvas != null)
        {
            muteCanvas.gameObject.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: MUTE Canvas skryt na začátku");
        }
        
        // Nastav MUTE tlačítko
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(OnMuteButtonClicked);
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: MUTE tlačítko inicializováno");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("PhoneGuy: MUTE tlačítko není přiřazeno!");
        }
    }
    
    #endregion
    
    #region Event Management
    
    private void SubscribeToGameEvents()
    {
        if (gameManager != null)
        {
            // Připoj se k eventu spuštění hry
            gameManager.OnGameStarted += OnGameStarted;
            
            // Připoj se k eventu ukončení hry
            gameManager.OnGameEnded += OnGameEnded;
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: Připojen k game events");
        }
    }
    
    private void UnsubscribeFromGameEvents()
    {
        if (gameManager != null)
        {
            // Odpoj se od eventů
            gameManager.OnGameStarted -= OnGameStarted;
            gameManager.OnGameEnded -= OnGameEnded;
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: Odpojen od game events");
        }
    }
    
    #endregion
    
    #region Game Event Handlers
    
    private void OnGameStarted()
    {
        if (gameManager != null)
        {
            int nightNumber = gameManager.GetCurrentNight();
            
            if (enableDebugLogs)
                Debug.Log($"PhoneGuy: Hra spuštěna - Noc {nightNumber}, čekám {startDelay}s před spuštěním nahrávky");
            
            // Spusť Phone Guy nahrávku s pauzou
            StartCoroutine(PlayRecordingWithDelay(nightNumber));
            
            // Spusť MUTE tlačítko timing
            StartCoroutine(ManageMuteButtonTiming());
        }
    }
    
    private void OnGameEnded()
    {
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Hra ukončena - zastavuji všechny procesy");
        
        // Použij novou metodu pro kompletní zastavení
        ImmediateStopAllProcesses();
    }
    
    #endregion
    
    #region MUTE Button Management
    
    private System.Collections.IEnumerator ManageMuteButtonTiming()
    {
        // Nejprve čekej až se spustí nahrávka (startDelay) + dodatečné zpoždění pro MUTE tlačítko
        float totalDelay = startDelay + muteButtonShowDelay;
        
        if (enableDebugLogs)
            Debug.Log($"PhoneGuy: Čekám {totalDelay}s (nahrávka {startDelay}s + MUTE zpoždění {muteButtonShowDelay}s) před zobrazením MUTE tlačítka");
        
        yield return new WaitForSeconds(totalDelay);
        
        // Kontrola: Zobraz MUTE tlačítko pouze pokud se nahrávka přehrává a hra je stále aktivní
        if (isPlaying && gameManager != null && gameManager.IsGameActive())
        {
            ShowMuteButton();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: MUTE tlačítko nebylo zobrazeno - nahrávka se nepřehrává nebo hra není aktivní");
            yield break;
        }
        
        // Čekej na automatické skrytí
        if (enableDebugLogs)
            Debug.Log($"PhoneGuy: MUTE tlačítko bude viditelné {muteButtonAutoHideDelay}s před automatickým skrytím");
        
        yield return new WaitForSeconds(muteButtonAutoHideDelay);
        
        // Automaticky skryj MUTE tlačítko (ale pouze pokud je hra stále aktivní)
        if (isMuteButtonVisible && gameManager != null && gameManager.IsGameActive())
        {
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: Automatické skrytí MUTE tlačítka");
            
            HideMuteButton();
        }
    }
    
    private void ShowMuteButton()
    {
        if (muteCanvas != null && !isMuteButtonVisible)
        {
            muteCanvas.gameObject.SetActive(true);
            isMuteButtonVisible = true;
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: MUTE tlačítko zobrazeno");
        }
    }
    
    public void HideMuteButton()
    {
        if (muteCanvas != null && isMuteButtonVisible)
        {
            muteCanvas.gameObject.SetActive(false);
            isMuteButtonVisible = false;
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: MUTE tlačítko skryto");
        }
    }
    
    private void OnMuteButtonClicked()
    {
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: MUTE tlačítko stisknuto - ukončuji hovor");
        
        // Přehraj hang-up zvuk
        PlayHangUpSound();
        
        // Zastav Phone Guy nahrávku
        StopRecordingImmediate();
        
        // Skryj MUTE tlačítko
        HideMuteButton();
    }
    
    private void PlayHangUpSound()
    {
        if (hangUpSound != null && hangUpAudioSource != null)
        {
            // Přehraj hang-up zvuk přes samostatný AudioSource
            hangUpAudioSource.PlayOneShot(hangUpSound);
            
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: Přehrávám hang-up zvuk přes samostatný AudioSource");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"PhoneGuy: Hang-up zvuk nebo AudioSource není dostupný - Sound: {hangUpSound != null}, AudioSource: {hangUpAudioSource != null}");
        }
    }
    
    #endregion
    
    #region Audio Playback
    
    private System.Collections.IEnumerator PlayRecordingWithDelay(int nightNumber)
    {
        if (enableDebugLogs)
            Debug.Log($"PhoneGuy: Čekám {startDelay}s před spuštěním nahrávky pro noc {nightNumber}");
        
        // Čekej nastavenou dobu před spuštěním
        yield return new WaitForSeconds(startDelay);
        
        // Spusť nahrávku po delay
        if (enableDebugLogs)
            Debug.Log($"PhoneGuy: Pauza dokončena, spouštím nahrávku pro noc {nightNumber}");
        
        PlayNightRecording(nightNumber);
    }
    
    public void PlayNightRecording(int nightNumber)
    {
        // Validace čísla noci
        if (nightNumber < 1 || nightNumber > 6)
        {
            Debug.LogWarning($"PhoneGuy: Neplatné číslo noci: {nightNumber}. Podporovány jsou noci 1-6.");
            return;
        }
        
        // Kontrola AudioSource
        if (phoneGuyAudioSource == null)
        {
            Debug.LogError("PhoneGuy: AudioSource není přiřazený!");
            return;
        }
        
        // Index v poli (noci 1-6 = indexy 0-5)
        int recordingIndex = nightNumber - 1;
        
        // Kontrola dostupnosti nahrávky
        if (recordingIndex >= nightRecordings.Length || nightRecordings[recordingIndex] == null)
        {
            Debug.LogWarning($"PhoneGuy: Nahrávka pro noc {nightNumber} není přiřazena!");
            return;
        }
        
        // Zastav předchozí nahrávku pokud hraje
        if (isPlaying)
        {
            StopRecording();
        }
        
        // Nastav novou nahrávku
        AudioClip recordingToPlay = nightRecordings[recordingIndex];
        phoneGuyAudioSource.clip = recordingToPlay;
        currentNight = nightNumber;
        
        if (enableDebugLogs)
            Debug.Log($"PhoneGuy: Spouštím nahrávku pro noc {nightNumber} (délka: {recordingToPlay.length:F1}s)");
        
        // Spusť přehrávání s fade in efektem
        StartCoroutine(PlayWithFadeIn());
    }
    
    public void StopRecording()
    {
        if (!isPlaying)
            return;
        
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Zastavuji nahrávku");
        
        // Zastav s fade out efektem
        StartCoroutine(StopWithFadeOut());
    }
    
    public void StopRecordingImmediate()
    {
        if (phoneGuyAudioSource != null)
        {
            phoneGuyAudioSource.Stop();
            phoneGuyAudioSource.volume = 1f; // Reset volume
        }
        
        isPlaying = false;
        isFading = false;
        currentNight = -1;
        
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Nahrávka okamžitě zastavena");
    }
    
    #endregion
    
    #region Fade Effects
    
    private System.Collections.IEnumerator PlayWithFadeIn()
    {
        if (phoneGuyAudioSource == null) yield break;
        
        // Připrav fade in
        phoneGuyAudioSource.volume = 0f;
        phoneGuyAudioSource.Play();
        isPlaying = true;
        isFading = true;
        
        // Fade in efekt
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration && isPlaying)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;
            phoneGuyAudioSource.volume = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }
        
        // Ujisti se, že volume je na maximum
        if (phoneGuyAudioSource != null && isPlaying)
        {
            phoneGuyAudioSource.volume = 1f;
        }
        
        isFading = false;
        
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Fade in dokončen");
        
        // Pokud je zapnutý auto fade out, čekej na konec nahrávky
        if (autoFadeOut && isPlaying)
        {
            StartCoroutine(WaitForRecordingEnd());
        }
    }
    
    private System.Collections.IEnumerator StopWithFadeOut()
    {
        if (phoneGuyAudioSource == null || !isPlaying) yield break;
        
        isFading = true;
        float startVolume = phoneGuyAudioSource.volume;
        
        // Fade out efekt
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration && isPlaying)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            phoneGuyAudioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }
        
        // Zastav přehrávání a resetuj
        if (phoneGuyAudioSource != null)
        {
            phoneGuyAudioSource.Stop();
            phoneGuyAudioSource.volume = 0.7f; // Reset volume pro příští přehrání
        }
        
        isPlaying = false;
        isFading = false;
        currentNight = -1;
        
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Fade out dokončen");
    }
    
    private System.Collections.IEnumerator WaitForRecordingEnd()
    {
        if (phoneGuyAudioSource == null || !isPlaying) yield break;
        
        // Čekej až nahrávka skončí (minus doba fade out)
        float recordingLength = phoneGuyAudioSource.clip.length;
        float waitTime = recordingLength - fadeOutDuration;
        
        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }
        
        // Pokud stále hraje, spusť fade out
        if (isPlaying && !isFading)
        {
            if (enableDebugLogs)
                Debug.Log("PhoneGuy: Spouštím auto fade out");
            
            yield return StartCoroutine(StopWithFadeOut());
        }
    }
    
    #endregion
    
    #region Public Methods
    
    // Metody pro externí ovládání
    public bool IsPlaying() => isPlaying;
    public bool IsFading() => isFading;
    public int GetCurrentNight() => currentNight;
    
    // Metoda pro manuální spuštění nahrávky (pro debug)
    public void PlayRecordingForNight(int nightNumber)
    {
        PlayNightRecording(nightNumber);
    }
    
    // Metoda pro nastavení volume
    public void SetVolume(float volume)
    {
        if (phoneGuyAudioSource != null)
        {
            phoneGuyAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    // Metoda pro získání aktuální volume
    public float GetVolume()
    {
        return phoneGuyAudioSource != null ? phoneGuyAudioSource.volume : 0f;
    }
    
    // Metoda pro okamžité zastavení všech PhoneGuy procesů (pro ESC)
    public void ImmediateStopAllProcesses()
    {
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Okamžité zastavení všech procesů (ESC nebo ukončení hry)");
        
        // Zastav okamžitě všechny coroutiny na tomto objektu
        StopAllCoroutines();
        
        // Okamžitě zastav nahrávku
        StopRecordingImmediate();
        
        // Skryj MUTE tlačítko
        HideMuteButton();
        
        // Reset všech stavů
        isPlaying = false;
        isFading = false;
        currentNight = -1;
        isMuteButtonVisible = false;
        muteButtonCoroutine = null;
        
        if (enableDebugLogs)
            Debug.Log("PhoneGuy: Všechny procesy zastaveny a stavy resetovány");
    }
    
    #endregion
    
    #region Validation
    
    void OnValidate()
    {
        // Ujisti se, že pole má správnou velikost
        if (nightRecordings == null || nightRecordings.Length != 6)
        {
            nightRecordings = new AudioClip[6];
        }
        
        // Validace timing hodnot
        if (startDelay < 0f) startDelay = 0f;
        if (fadeInDuration < 0f) fadeInDuration = 0f;
        if (fadeOutDuration < 0f) fadeOutDuration = 0f;
        if (muteButtonShowDelay < 0f) muteButtonShowDelay = 0f;
        if (muteButtonVisibleDuration < 1f) muteButtonVisibleDuration = 1f;
        if (muteButtonAutoHideDelay < 1f) muteButtonAutoHideDelay = 1f;
    }
    
    #endregion
}