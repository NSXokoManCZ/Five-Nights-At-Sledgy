using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NightShiftManagement : MonoBehaviour
{
    [Header("Night Shift Management")]
    [SerializeField] private string nightShiftVersion = "1.0";
    
    [Header("Night Progress")]
    [SerializeField] private int currentNight = 1; // Aktuální noc (1-6)
    [SerializeField] private int maxNights = 6; // Maximální počet nocí (5 základních + 6. noc)
    [SerializeField] private bool isStoryCompleted = false; // Zda byla dokončena základní dějová linka (5 nocí)
    [SerializeField] private int savedNightBeforeSixth = 1; // Uložená noc před spuštěním 6. noci
    
    [Header("Game State")]
    [SerializeField] private bool isGameActive = false; // Zda hra běží
    [SerializeField] private bool isInMainMenu = true; // Zda jsme v main menu
    
    [Header("UI References")]
    [SerializeField] private GameObject MainMenuPanel; // Panel hlavního menu
    [SerializeField] private Button StartNewGameButton; // Tlačítko pro novou hru
    [SerializeField] private Button ContinueGameButton; // Tlačítko pro pokračování
    [SerializeField] private Button SixthNightButton; // Tlačítko pro 6. noc (objeví se po dokončení 5 nocí)
    [SerializeField] private GameObject CompletionStar; // Hvězda za dokončení základní dějové linky (5 nocí)
    [SerializeField] private Button QuitGameButton; // Tlačítko pro ukončení
    [SerializeField] private TextMeshProUGUI NightDisplayText; // Text pro zobrazení aktuální noci (např. "NIGHT 1")
    
    [Header("Transition Panels")]
    [SerializeField] private GameObject PanelStart; // Panel pro začátek hry
    [SerializeField] private GameObject PanelTransition; // Panel pro přechody mezi nocemi
    [SerializeField] private GameObject PanelEndgame; // Panel pro konec hry
    [SerializeField] private GameObject PanelEndBackground; // Černý pozadí panel pro end game (skryje kancelář)
    
    [Header("Transition Panel UI")]
    [SerializeField] private TextMeshProUGUI TransitionTimeText; // Text "12:00 AM" na přechodovém panelu
    [SerializeField] private TextMeshProUGUI TransitionNightText; // Text "NIGHT X" na přechodovém panelu
    
    [Header("Start Panel UI")]
    [SerializeField] private TextMeshProUGUI StartTimeText; // Text "12:00 AM" na start panelu
    [SerializeField] private TextMeshProUGUI StartNightText; // Text "NIGHT 1" na start panelu
    
    [Header("Endgame Panel UI")]
    [SerializeField] private TextMeshProUGUI EndgameText; // Text na endgame panelu
    
    [Header("Game Systems")]
    [SerializeField] private ShiftTimer ShiftTimerRef; // Reference na časovač směny
    [SerializeField] private ResetNight ResetNightRef; // Reference na reset systém
    [SerializeField] private VentilaceLaseryDvere LaserDoorsRef; // Reference na laser doors systém
    [SerializeField] private ElectricityManagement ElectricityRef; // Reference na elektřinu management systém
    [SerializeField] private PhoneGuy PhoneGuyRef; // Reference na Phone Guy systém
    [SerializeField] private FlashlightSystem FlashlightRef; // Reference na Flashlight systém
    
    [Header("Menu Control Systems")]
    [SerializeField] private Canvas[] GameCanvases; // Canvasy které se deaktivují v menu
    [SerializeField] private AudioSource[] GameAudioSources; // Audio zdroje které se deaktivují v menu
    
    [Header("Menu Background Music")]
    [SerializeField] private AudioSource MenuBackgroundMusic; // Hudba na pozadí v menu
    [SerializeField] private AudioClip MenuMusicClip; // Audio clip pro menu hudbu
    
    [Header("Night End Settings")]
    [SerializeField] private float autoReturnToMenuDelay = 10f; // Zpoždění návratu do menu po skončení noci
    
    [Header("Transition Audio")]
    [SerializeField] private AudioSource TransitionAudioSource; // AudioSource pro přechodové zvuky
    [SerializeField] private AudioClip StartPanelSound; // Zvuk pro panel začátku hry
    [SerializeField] private AudioClip NightTransitionSound; // Zvuk pro přechod mezi nocemi
    [SerializeField] private AudioClip EndGamePanelSound; // Zvuk pro panel konce hry
    
    [Header("Ending Audio")]
    [SerializeField] private AudioSource EndingAudioSource; // Samostatný AudioSource pro ending zvuk
    [SerializeField] private AudioClip EndingSound; // Zvuk pro ending (po 5. noci)
    
    [Header("Transition Timing")]
    [SerializeField] private float startPanelDuration = 9f; // Doba zobrazení start panelu (9 sekund)
    [SerializeField] private float transitionPanelDuration = 5f; // Doba zobrazení přechodového panelu (5 sekund)
    [SerializeField] private float endGamePanelDuration = 10f; // Doba zobrazení end game panelu (10 sekund)
    [SerializeField] private float endVideoDelay = 8f; // Doba čekání na end video před transition panelem (8 sekund)
    [SerializeField] private float fifthNightEndVideoDelay = 7.6f; // Doba end videa + zvuku po 5. noci (7,6 sekund)
    
    [Header("Video System")]
    [SerializeField] private GameObject VideoCanvas; // Canvas s end video které se musí skrýt po přehrání
    
    [Header("Fade Transitions")]
    [SerializeField] private CanvasGroup MainMenuCanvasGroup; // CanvasGroup pro fade main menu
    [SerializeField] private CanvasGroup StartPanelCanvasGroup; // CanvasGroup pro fade start panel
    [SerializeField] private CanvasGroup EndPanelCanvasGroup; // CanvasGroup pro fade end panel
    [SerializeField] private float fadeDuration = 1f; // Doba fade přechodu (1 sekunda)
    [SerializeField] private float endPanelFadeDuration = 2f; // Doba fade pro end panel (2 sekundy)
    
    [Header("Progress Reset")]
    [SerializeField] private float resetHoldDuration = 3f; // Doba držení Delete pro reset (3 sekundy)
    private float deleteKeyHoldTime = 0f; // Čas držení Delete klávesy
    private bool isHoldingDelete = false; // Zda je Delete klávesa držena
    
    // Events pro komunikaci s ostatními systémy
    public System.Action<int> OnNightChanged; // Event při změně noci
    public System.Action OnGameStarted; // Event při spuštění hry
    public System.Action OnGameEnded; // Event při ukončení hry
    
    void Start()
    {
        Debug.Log("NightShiftManagement inicializován - verze: " + nightShiftVersion);
        
        // Nastav UI tlačítka
        SetupUIButtons();
        
        // Načti uložený progress
        LoadNightProgress();
        
        // Nastav počáteční stav
        SetMainMenuState();
        
        // Nastav menu hudbu
        SetupMenuMusic();
        
        // Aktualizuj UI podle stavu
        UpdateUI();
    }
    
    void Update()
    {
        // Kontroluj stav hry pouze pokud hra běží
        if (isGameActive && !isInMainMenu)
        {
            CheckNightCompletion();
            
            // Kontroluj ESC klávesu pro návrat do menu během noci
            CheckEscapeKeyForMenu();
        }
        
        // Kontroluj Delete klávesy pro reset progressu (pouze v main menu)
        if (isInMainMenu)
        {
            CheckDeleteKeyForReset();
        }
    }
    
    #region UI Setup and Control
    
    private void SetupUIButtons()
    {
        if (StartNewGameButton != null)
        {
            StartNewGameButton.onClick.AddListener(StartNewGame);
        }
        
        if (ContinueGameButton != null)
        {
            ContinueGameButton.onClick.AddListener(ContinueGame);
            
            // Přidej hover efekt pro Continue tlačítko
            SetupContinueButtonHover();
        }
        
        if (SixthNightButton != null)
        {
            SixthNightButton.onClick.AddListener(StartSixthNight);
        }
        
        if (QuitGameButton != null)
        {
            QuitGameButton.onClick.AddListener(QuitGame);
        }
    }
    
    private void UpdateUI()
    {
        // Aktualizuj dostupnost tlačítka Continue podle progressu
        if (ContinueGameButton != null)
        {
            // Continue je dostupné pokud: máme progress (noc > 1) NEBO byla dokončena 5. noc
            bool canContinue = (currentNight > 1 && currentNight <= 5) || isStoryCompleted;
            ContinueGameButton.interactable = canContinue;
            
            // Nastav barvu textu tlačítka podle dostupnosti
            TextMeshProUGUI buttonText = ContinueGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                if (currentNight == 1)
                {
                    // Při 1. noci - tmavší text o 40%
                    textColor.a = 0.05f; // 100% - 40% = 60%
                }
                else
                {
                    // Při vyšších nocích - normální text
                    textColor.a = 1.0f;
                }
                buttonText.color = textColor;
                
                // Nastav správný text tlačítka podle aktuální noci
                if (currentNight == 1)
                {
                    buttonText.text = "CONTINUE";
                }
                else if (currentNight >= 2 && currentNight <= 6)
                {
                    buttonText.text = $"CONTINUE ({currentNight})";
                }
            }
            
            // Nastav vizuální styl tlačítka podle dostupnosti
            Image buttonImage = ContinueGameButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color buttonColor = buttonImage.color;
                buttonColor.a = canContinue ? 1.0f : 0.6f; // Tmavší pokud není dostupné
                buttonImage.color = buttonColor;
            }
        }
        
        // Aktualizuj dostupnost 6th Night tlačítka a hvězdy
        if (SixthNightButton != null)
        {
            SixthNightButton.gameObject.SetActive(isStoryCompleted);
            SixthNightButton.interactable = isStoryCompleted;
        }
        
        if (CompletionStar != null)
        {
            CompletionStar.SetActive(isStoryCompleted);
        }
        
        // Aktualizuj text noci
        UpdateNightDisplayText();
        
        Debug.Log($"UI aktualizováno - Aktuální noc: {currentNight}, Continue dostupné: {currentNight > 1}, 6th Night: {isStoryCompleted}");
    }
    
    private void UpdateNightDisplayText()
    {
        // Aktualizuj text zobrazující aktuální noc
        // DŮLEŽITÉ: Ujisti se, že NightDisplayText NENÍ text Continue tlačítka!
        if (NightDisplayText != null)
        {
            // Kontrola: Pokud je NightDisplayText součástí Continue tlačítka, nevykonávej změnu
            if (ContinueGameButton != null)
            {
                TextMeshProUGUI continueButtonText = ContinueGameButton.GetComponentInChildren<TextMeshProUGUI>();
                if (continueButtonText == NightDisplayText)
                {
                    Debug.LogWarning("CHYBA: NightDisplayText je přiřazený k Continue tlačítku! Neprovádím změnu textu.");
                    return; // Zastav vykonávání, aby se nezměnil text tlačítka
                }
            }
            
            NightDisplayText.text = $"NIGHT {currentNight}";
            Debug.Log($"Text noci aktualizován na: NIGHT {currentNight}");
        }
    }
    
    private void ShowNightDisplayText()
    {
        // Zobraz text noci
        if (NightDisplayText != null)
        {
            NightDisplayText.gameObject.SetActive(true);
            UpdateNightDisplayText();
            Debug.Log("Text noci zobrazen");
        }
    }
    
    private void HideNightDisplayText()
    {
        // Skryj text noci
        if (NightDisplayText != null)
        {
            NightDisplayText.gameObject.SetActive(false);
            Debug.Log("Text noci skryt");
        }
    }
    
    private void SetupContinueButtonHover()
    {
        if (ContinueGameButton != null)
        {
            // Přidej EventTrigger komponent pokud neexistuje
            EventTrigger trigger = ContinueGameButton.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = ContinueGameButton.gameObject.AddComponent<EventTrigger>();
            }
            
            // Přidej hover enter event
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { OnContinueButtonHoverEnter(); });
            trigger.triggers.Add(pointerEnter);
            
            // Přidej hover exit event
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnContinueButtonHoverExit(); });
            trigger.triggers.Add(pointerExit);
        }
    }
    
    private void OnContinueButtonHoverEnter()
    {
        if (ContinueGameButton != null && currentNight == 1)
        {
            // Pouze při 1. noci - ztmav tlačítko při hover
            Image buttonImage = ContinueGameButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color buttonColor = buttonImage.color;
                buttonColor.a = 0.3f; // Ještě tmavší při hover
                buttonImage.color = buttonColor;
                Debug.Log("Continue tlačítko ztmaveno při hover (1. noc)");
            }
        }
        // Pro noci 2-6 už není potřeba měnit text - text je nastaven v UpdateUI()
    }
    
    private void OnContinueButtonHoverExit()
    {
        if (ContinueGameButton != null && currentNight == 1)
        {
            // Pouze při 1. noci - vrať původní alpha hodnotu
            Image buttonImage = ContinueGameButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color buttonColor = buttonImage.color;
                buttonColor.a = 0.6f; // Základní tmavá pro 1. noc
                buttonImage.color = buttonColor;
                Debug.Log("Continue tlačítko obnoveno po hover (1. noc)");
            }
        }
        // Pro noci 2-6 není potřeba nic dělat - text zůstává "CONTINUE (X)"
    }
    
    #endregion
    
    #region Game Flow Control
    
    public void StartNewGame()
    {
        Debug.Log("Spouštění nové hry...");
        
        // Reset pouze na první noc (zachovej story completed a odměny)
        currentNight = 1;
        SaveNightProgress();
        
        // Zobraz startovací panel místo přímého spuštění
        StartCoroutine(ShowStartTransition());
    }
    
    public void ContinueGame()
    {
        Debug.Log($"Pokračování ve hře - Noc {currentNight}");
        
        // Zobraz transition panel místo přímého spuštění
        StartCoroutine(ShowContinueTransition());
    }
    
    public void StartSixthNight()
    {
        Debug.Log("Spouštění 6. noci...");
        
        // Ulož aktuální noc před spuštěním 6. noci
        savedNightBeforeSixth = currentNight;
        
        // Nastav na 6. noc
        currentNight = 6;
        SaveNightProgress();
        
        // Zobraz transition panel místo přímého spuštění
        StartCoroutine(ShowSixthNightTransition());
    }
    
    public void QuitGame()
    {
        Debug.Log("Ukončování aplikace...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void StartGameNight()
    {
        StartGameNight(currentNight);
    }
    
    private void StartGameNight(int nightNumber)
    {
        Debug.Log($"Začíná noc {nightNumber}");
        
        // Nastav aktuální noc
        currentNight = nightNumber;
        
        // Skryj všechny přechodové panely
        HideAllPanels();
        
        // Aktivuj video canvas pro novou noc
        if (VideoCanvas != null)
        {
            VideoCanvas.SetActive(true);
        }
        
        // Nastav stav hry
        isGameActive = true;
        isInMainMenu = false;
        
        // Skryj main menu
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(false);
        }
        
        // Zastav menu hudbu
        StopMenuMusic();
        
        // Aktivuj všechny herní systémy
        ActivateGameSystems();
        
        // Resetuj všechny herní systémy pro novou noc
        ResetAllGameSystems();
        
        // DŮLEŽITÉ: Spusť časovač až teď, kdy je hráč v kanceláři
        if (ShiftTimerRef != null)
        {
            ShiftTimerRef.enabled = true;
            // Resetuj časovač na začátek noci
            ShiftTimerRef.ResetGame();
        }
        
        // Spusť elektřinu management systém
        if (ElectricityRef != null)
        {
            ElectricityRef.StartGame();
            Debug.Log("ElectricityManagement spuštěn pro novou noc");
        }
        else
        {
            Debug.LogWarning("ElectricityManagement reference není přiřazena!");
        }
        
        // Vyvolej event
        OnGameStarted?.Invoke();
        OnNightChanged?.Invoke(currentNight);
        
        // Zobraz text noci při začátku hry
        ShowNightDisplayText();
        
        Debug.Log($"Noc {currentNight} byla spuštěna");
    }
    
    private void CheckNightCompletion()
    {
        // Zkontroluj, zda noc skončila
        if (ShiftTimerRef != null && ShiftTimerRef.Won)
        {
            Debug.Log($"Noc {currentNight} dokončena!");
            OnNightCompleted();
        }
    }
    
    private void CheckEscapeKeyForMenu()
    {
        // Kontroluj ESC klávesu pro okamžitý návrat do menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC stisknuto - vracím se do main menu bez uložení postupu noci");
            EscapeToMainMenu();
        }
    }
    
    private void EscapeToMainMenu()
    {
        Debug.Log("Escape do main menu - noc se neuloží, zůstane na aktuální úrovni");
        
        // Zastav všechny herní systémy okamžitě
        isGameActive = false;
        
        // Zastav elektřinu management systém
        if (ElectricityRef != null)
        {
            ElectricityRef.StopGame();
            Debug.Log("ElectricityManagement zastaven při ESC");
        }
        
        // Skryj Phone Guy MUTE tlačítko
        if (PhoneGuyRef != null)
        {
            PhoneGuyRef.HideMuteButton();
            Debug.Log("Phone Guy MUTE tlačítko skryto při ESC");
        }
        
        // Reset baterky (zastaví flickering zvuky okamžitě)
        if (FlashlightRef != null)
        {
            FlashlightRef.ImmediateStopFlickering();
            Debug.Log("Baterka okamžitě zastavena při ESC - flickering zvuky ukončeny");
        }
        
        // Zastav časovač
        if (ShiftTimerRef != null)
        {
            ShiftTimerRef.enabled = false;
        }
        
        // Skryj text noci
        HideNightDisplayText();
        
        // Skryj video canvas pokud je aktivní
        if (VideoCanvas != null)
        {
            VideoCanvas.SetActive(false);
        }
        
        // Bezpečně zastav ending zvuk pokud hraje
        try
        {
            if (EndingAudioSource != null && EndingAudioSource.isPlaying)
            {
                EndingAudioSource.Stop();
                Debug.Log("Ending zvuk zastaven při ESC");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Chyba při zastavování ending zvuku v ESC: {e.Message}");
        }
        
        // NEUKLÁDEJ postup - currentNight zůstává stejná
        // (hráč bude moci opakovat stejnou noc)
        
        // Vrať se okamžitě do main menu
        ReturnToMainMenu();
    }
    
    private void CheckDeleteKeyForReset()
    {
        // Kontroluj zda je Delete klávesa stisknuta
        if (Input.GetKey(KeyCode.Delete))
        {
            if (!isHoldingDelete)
            {
                isHoldingDelete = true;
                deleteKeyHoldTime = 0f;
                Debug.Log("Delete klávesa stisknuta - začínám odpočítávání reset...");
            }
            
            deleteKeyHoldTime += Time.deltaTime;
            
            // Pokud je klávesa držena po stanovenou dobu, proveď reset
            if (deleteKeyHoldTime >= resetHoldDuration)
            {
                Debug.Log($"Delete držena {resetHoldDuration} sekund - resetuji kompletní progress!");
                ResetCompleteProgress();
                isHoldingDelete = false;
                deleteKeyHoldTime = 0f;
            }
        }
        else
        {
            // Reset časovače pokud klávesa není držena
            if (isHoldingDelete)
            {
                Debug.Log("Delete klávesa uvolněna - ruším reset.");
                isHoldingDelete = false;
                deleteKeyHoldTime = 0f;
            }
        }
    }
    
    private void OnNightCompleted()
    {
        Debug.Log($"Zpracovávání konce noci {currentNight}");
        
        // Zastav hru
        isGameActive = false;
        
        // Skryj text noci během video přehrávání
        HideNightDisplayText();
        
        // Zastav časovač
        if (ShiftTimerRef != null)
        {
            ShiftTimerRef.enabled = false;
        }
        
        // Reset laser doors systému při konci noci
        if (LaserDoorsRef != null)
        {
            LaserDoorsRef.SilentResetSystem();
            Debug.Log("Laser doors resetovány při konci noci");
        }
        
        // Zkontroluj speciální případy pro dokončení
        if (currentNight == 5)
        {
            // Dokončena 5. noc - odemkni 6. noc a zobraz end game panel
            Debug.Log("5. noc dokončena! Odemykám 6. noc a hvězdu.");
            isStoryCompleted = true;
            // NEZMĚŇ currentNight - zůstaň na 5. noci pro možnost opakování
            SaveNightProgress();
            
            // Vyvolej event
            OnGameEnded?.Invoke();
            
            // Zobraz end game panel místo návratu do menu
            StartCoroutine(ShowEndGameTransition());
        }
        else if (currentNight == 6)
        {
            // Dokončena 6. noc - vrať se na původní progress před 6. nocí
            Debug.Log($"6. noc dokončena! Vracím na původní noc: {savedNightBeforeSixth}");
            currentNight = savedNightBeforeSixth;
            SaveNightProgress();
            
            // Vyvolej event
            OnGameEnded?.Invoke();
            
            // Pro 6. noc se vrať standardně do menu
            Invoke("ReturnToMainMenu", autoReturnToMenuDelay);
        }
        else
        {
            // Standardní postup na další noc (1-4)
            currentNight++;
            if (currentNight > maxNights)
            {
                Debug.Log("Všechny noci dokončeny! Hra kompletně dokončena!");
                OnGameCompleted();
                
                // Vyvolej event
                OnGameEnded?.Invoke();
                
                // Návrat do menu pro dokončení všech nocí
                Invoke("ReturnToMainMenu", autoReturnToMenuDelay);
            }
            else
            {
                Debug.Log($"Postupuji na noc {currentNight}");
                SaveNightProgress();
                
                // Vyvolej event
                OnGameEnded?.Invoke();
                
                // Zobraz přechodový panel místo návratu do menu
                StartCoroutine(ShowNightTransition());
            }
        }
    }
    
    private void OnGameCompleted()
    {
        Debug.Log("Gratulace! Hra byla kompletně dokončena!");
        // Zde můžeme přidat speciální ukončení pro dokončení všech nocí
        
        // Reset progress pro možnost hrát znovu
        currentNight = 1;
        SaveNightProgress();
    }
    
    private IEnumerator ShowNightTransition()
    {
        Debug.Log($"Čekám na dokončení end videa před zobrazením transition panelu pro noc {currentNight}");
        
        // 1. ČEKEJ na dokončení end videa (8 sekund)
        yield return new WaitForSeconds(endVideoDelay);
        
        Debug.Log($"End video dokončeno, skrývám video canvas a zobrazuji transition panel pro noc {currentNight}");
        
        // 2. Skryj video canvas aby nepřekrýval transition panel
        if (VideoCanvas != null)
        {
            VideoCanvas.SetActive(false);
        }
        
        // 3. Skryj všechny panely
        HideAllPanels();
        
        // 4. Zobraz přechodový panel s textem aktuální noci
        if (PanelTransition != null)
        {
            PanelTransition.SetActive(true);
            
            // Nastav text přes specifické reference
            if (TransitionTimeText != null)
            {
                TransitionTimeText.text = "12:00 AM";
            }
            
            if (TransitionNightText != null)
            {
                TransitionNightText.text = $"NIGHT {currentNight}";
            }
        }
        
        // 5. Přehraj zvuk přechodu
        if (TransitionAudioSource != null && NightTransitionSound != null)
        {
            TransitionAudioSource.PlayOneShot(NightTransitionSound);
        }
        
        // 6. Čekej na zobrazení transition panelu (5 sekund)
        yield return new WaitForSeconds(transitionPanelDuration);
        
        // 7. Automaticky pokračuj na další noc
        StartGameNight(currentNight);
    }
    
    private IEnumerator ShowEndGameTransition()
    {
        Debug.Log("Zobrazuji end game sekvenci po 5. noci");
        
        // 1. ČEKEJ na dokončení end videa + zvuku (7,6 sekund)
        Debug.Log("End video + zvuk se přehrává...");
        yield return new WaitForSeconds(fifthNightEndVideoDelay);
        
        // 2. Skryj video canvas po dokončení end videa
        Debug.Log("End video dokončeno, skrývám video canvas");
        if (VideoCanvas != null)
        {
            VideoCanvas.SetActive(false);
        }
        
        // 3. Skryj všechny panely a připrav end game panel
        HideAllPanels();
        
        // Aktivuj černé pozadí pro skrytí kanceláře
        if (PanelEndBackground != null)
        {
            PanelEndBackground.SetActive(true);
            Debug.Log("Černé pozadí pro end panel aktivováno");
        }
        
        if (PanelEndgame != null)
        {
            PanelEndgame.SetActive(true);
            
            // Nastav text přes specifickou referenci
            if (EndgameText != null)
            {
                EndgameText.text = "CONGRATULATIONS!\nYOU SURVIVED 5 NIGHTS!";
            }
            
            // Nastav alpha na 0 pro fade in
            if (EndPanelCanvasGroup != null)
            {
                EndPanelCanvasGroup.alpha = 0f;
            }
        }
        
        // 4. Přehraj ending zvuk přes samostatný AudioSource (bezpečně)
        Debug.Log("Spouštím ending zvuk...");
        try
        {
            if (EndingAudioSource != null && EndingSound != null)
            {
                EndingAudioSource.PlayOneShot(EndingSound);
                Debug.Log("Ending zvuk úspěšně spuštěn");
            }
            else
            {
                Debug.LogWarning($"Ending audio nedostupné - AudioSource: {EndingAudioSource != null}, Sound: {EndingSound != null}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Chyba při spouštění ending zvuku: {e.Message}");
        }
        
        // 5. FADE IN end panel po dobu 2 sekund
        Debug.Log("Začínám fade in end panelu (2 sekundy)");
        yield return StartCoroutine(FadeEndPanel(true));
        
        // 6. Zobrazuj end panel po zbývající dobu (celková doba - fade in - fade out)
        float remainingDisplayTime = endGamePanelDuration - (2 * endPanelFadeDuration);
        if (remainingDisplayTime > 0)
        {
            Debug.Log($"End panel zobrazen po dobu {remainingDisplayTime} sekund");
            yield return new WaitForSeconds(remainingDisplayTime);
        }
        
        // 7. FADE OUT end panel po dobu 2 sekund
        Debug.Log("Začínám fade out end panelu (2 sekundy)");
        yield return StartCoroutine(FadeEndPanel(false));
        
        // 8. Bezpečně zastav ending zvuk
        Debug.Log("Zastavuji ending zvuk");
        try
        {
            if (EndingAudioSource != null && EndingAudioSource.isPlaying)
            {
                EndingAudioSource.Stop();
                Debug.Log("Ending zvuk zastaven");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Chyba při zastavování ending zvuku: {e.Message}");
        }
        
        // 9. 1sekundová pauza pouze s černým pozadím
        Debug.Log("1sekundová pauza s černým pozadím před návratem do menu");
        yield return new WaitForSeconds(1f);
        
        // 10. Vrať se do menu
        Debug.Log("End game sekvence dokončena, vracím se do main menu");
        ReturnToMainMenu();
    }
    
    private IEnumerator ShowStartTransition()
    {
        Debug.Log("Zobrazuji startovací panel s fade přechodem");
        
        // 1. Připrav start panel (menu hudba pokračuje)
        HideAllPanels();
        
        if (PanelStart != null)
        {
            PanelStart.SetActive(true);
            
            // Nastav text přes specifické reference
            if (StartTimeText != null)
            {
                StartTimeText.text = "12:00 AM";
            }
            
            if (StartNightText != null)
            {
                StartNightText.text = "NIGHT 1";
            }
            
            // Nastav alpha na 0 pro fade in
            if (StartPanelCanvasGroup != null)
            {
                StartPanelCanvasGroup.alpha = 0f;
            }
        }
        
        // 2. FADE IN start panel přes main menu (menu hudba pokračuje)
        yield return StartCoroutine(SimultaneousFade());
        
        // 3. Přehraj start zvuk
        if (TransitionAudioSource != null && StartPanelSound != null)
        {
            TransitionAudioSource.PlayOneShot(StartPanelSound);
        }
        
        // 4. Čekaj na dokončení start panelu (menu hudba pokračuje)
        float waitTime = startPanelDuration - fadeDuration;
        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }
        
        // 5. Zastav menu hudbu před transition panelem
        StopMenuMusic();
        
        // 6. Přejdi na transition panel pro NIGHT 1
        HideAllPanels();
        
        // Zobraz transition panel s textem první noci
        if (PanelTransition != null)
        {
            PanelTransition.SetActive(true);
            
            // Nastav text přes specifické reference
            if (TransitionTimeText != null)
            {
                TransitionTimeText.text = "12:00 AM";
            }
            
            if (TransitionNightText != null)
            {
                TransitionNightText.text = "NIGHT 1";
            }
        }
        
        // 7. Přehraj transition zvuk
        if (TransitionAudioSource != null && NightTransitionSound != null)
        {
            TransitionAudioSource.PlayOneShot(NightTransitionSound);
        }
        
        // 8. Čekaj na zobrazení transition panelu (5 sekund)
        yield return new WaitForSeconds(transitionPanelDuration);
        
        // 9. Spusť první noc
        StartGameNight(1);
    }
    
    private void HideAllPanels()
    {
        if (PanelStart != null) PanelStart.SetActive(false);
        if (PanelTransition != null) PanelTransition.SetActive(false);
        if (PanelEndgame != null) PanelEndgame.SetActive(false);
        if (PanelEndBackground != null) PanelEndBackground.SetActive(false);
    }
    
    private void ReturnToMainMenu()
    {
        Debug.Log("Návrat do hlavního menu");
        
        // Reset laser doors před návratem do menu
        if (LaserDoorsRef != null)
        {
            LaserDoorsRef.SilentResetSystem();
            Debug.Log("Laser doors resetovány při návratu do menu");
        }
        
        // Skryj přechodové panely
        HideAllPanels();
        
        // Nastav stav main menu
        SetMainMenuState();
        
        // Aktualizuj UI
        UpdateUI();
    }
    
    private void SetMainMenuState()
    {
        isInMainMenu = true;
        isGameActive = false;
        
        // Zastav elektřinu management systém při návratu do menu
        if (ElectricityRef != null)
        {
            ElectricityRef.StopGame();
            Debug.Log("ElectricityManagement zastaven při návratu do menu");
        }
        
        // Zobraz main menu
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(true);
        }
        
        // Nastav plnou viditelnost main menu
        if (MainMenuCanvasGroup != null)
        {
            MainMenuCanvasGroup.alpha = 1f;
        }
        
        // Zastav časovač v menu
        if (ShiftTimerRef != null)
        {
            ShiftTimerRef.enabled = false;
        }
        
        // Deaktivuj všechny herní systémy
        DeactivateGameSystems();
        
        // Spusť menu hudbu
        StartMenuMusic();
    }
    
    #endregion
    
    #region Game Systems Reset
    
    private void ResetAllGameSystems()
    {
        Debug.Log("Resetuji všechny herní systémy...");
        
        // Použij reset systém pro obnovení všech komponent
        if (ResetNightRef != null)
        {
            ResetNightRef.ResetNightToOriginalState();
        }
        
        // Reset laser doors systému
        if (LaserDoorsRef != null)
        {
            LaserDoorsRef.SilentResetSystem();
            Debug.Log("Laser doors resetovány");
        }
        
        // Reset elektřiny management systému
        if (ElectricityRef != null)
        {
            ElectricityRef.ResetPower();
            Debug.Log("ElectricityManagement resetován");
        }
        
        // Reset baterky
        if (FlashlightRef != null)
        {
            FlashlightRef.SilentResetFlashlight();
            Debug.Log("Baterka resetována");
        }
        
        Debug.Log("Reset herních systémů dokončen");
    }
    
    #endregion
    
    #region Menu Control Systems
    
    private void DeactivateGameSystems()
    {
        Debug.Log("Deaktivuji herní systémy pro main menu...");
        
        // Deaktivuj herní canvasy
        if (GameCanvases != null)
        {
            foreach (Canvas canvas in GameCanvases)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                    Debug.Log($"Canvas deaktivován: {canvas.name}");
                }
            }
        }
        
        // Zastav a deaktivuj herní audio zdroje
        if (GameAudioSources != null)
        {
            foreach (AudioSource audioSource in GameAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.enabled = false;
                    Debug.Log($"AudioSource deaktivován: {audioSource.name}");
                }
            }
        }
        
        // Zastav elektřinu management systém
        if (ElectricityRef != null)
        {
            ElectricityRef.StopGame();
            Debug.Log("ElectricityManagement zastaven při deaktivaci herních systémů");
        }
        
        Debug.Log("Herní systémy deaktivovány pro main menu");
    }
    
    private void ActivateGameSystems()
    {
        Debug.Log("Aktivuji herní systémy pro gameplay...");
        
        // Najdi ElectricityManagement reference pokud není přiřazena
        if (ElectricityRef == null)
        {
            ElectricityRef = FindObjectOfType<ElectricityManagement>();
            if (ElectricityRef != null)
                Debug.Log("ElectricityManagement reference nalezena automaticky");
        }
        
        // Aktivuj herní canvasy
        if (GameCanvases != null)
        {
            foreach (Canvas canvas in GameCanvases)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                    Debug.Log($"Canvas aktivován: {canvas.name}");
                }
            }
        }
        
        // Aktivuj herní audio zdroje
        if (GameAudioSources != null)
        {
            foreach (AudioSource audioSource in GameAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.enabled = true;
                    // Audio se spustí automaticky podle svého nastavení
                    Debug.Log($"AudioSource aktivován: {audioSource.name}");
                }
            }
        }
        
        Debug.Log("Herní systémy aktivovány pro gameplay");
    }
    
    #endregion
    
    #region Menu Background Music
    
    private void SetupMenuMusic()
    {
        if (MenuBackgroundMusic != null)
        {
            // Nastav audio clip pokud je přiřazen
            if (MenuMusicClip != null)
            {
                MenuBackgroundMusic.clip = MenuMusicClip;
            }
            
            // Nastav loop pro nepřerušované přehrávání
            MenuBackgroundMusic.loop = true;
            
            // Nastav aby se nespouštěla automaticky
            MenuBackgroundMusic.playOnAwake = false;
            
            Debug.Log("Menu hudba nastavena");
        }
    }
    
    private void StartMenuMusic()
    {
        if (MenuBackgroundMusic != null && MenuMusicClip != null)
        {
            // Spusť hudbu pouze pokud již nehraje
            if (!MenuBackgroundMusic.isPlaying)
            {
                MenuBackgroundMusic.Play();
                Debug.Log("Menu hudba spuštěna");
            }
        }
    }
    
    private void StopMenuMusic()
    {
        if (MenuBackgroundMusic != null)
        {
            // Zastav hudbu s fade-outem pro plynulý přechod
            if (MenuBackgroundMusic.isPlaying)
            {
                MenuBackgroundMusic.Stop();
                Debug.Log("Menu hudba zastavena");
            }
        }
    }
    
    #endregion
    
    #region Save/Load System
    
    private void SaveNightProgress()
    {
        PlayerPrefs.SetInt("CurrentNight", currentNight);
        PlayerPrefs.SetInt("StoryCompleted", isStoryCompleted ? 1 : 0);
        PlayerPrefs.SetInt("SavedNightBeforeSixth", savedNightBeforeSixth);
        PlayerPrefs.Save();
        Debug.Log($"Progress uložen - Aktuální noc: {currentNight}, Story dokončena: {isStoryCompleted}");
    }
    
    private void LoadNightProgress()
    {
        currentNight = PlayerPrefs.GetInt("CurrentNight", 1); // Default je 1. noc
        isStoryCompleted = PlayerPrefs.GetInt("StoryCompleted", 0) == 1; // Default je false
        savedNightBeforeSixth = PlayerPrefs.GetInt("SavedNightBeforeSixth", 1); // Default je 1. noc
        Debug.Log($"Progress načten - Aktuální noc: {currentNight}, Story dokončena: {isStoryCompleted}");
        
        // Zajisti, aby noc byla v povoleném rozsahu
        currentNight = Mathf.Clamp(currentNight, 1, maxNights);
    }
    
    #endregion
    
    #region Public Methods
    
    // Metody pro externí přístup k informacím
    public int GetCurrentNight() => currentNight;
    public bool IsGameActive() => isGameActive;
    public bool IsInMainMenu() => isInMainMenu;
    
    // Metoda pro manuální návrat do menu (pro debug)
    public void ForceReturnToMenu()
    {
        ReturnToMainMenu();
    }
    
    // Metoda pro kompletní reset progressu
    public void ResetCompleteProgress()
    {
        Debug.Log("=== KOMPLETNÍ RESET PROGRESSU ===");
        
        // Reset všech progress proměnných
        currentNight = 1;
        isStoryCompleted = false;
        savedNightBeforeSixth = 1;
        
        // Vymaž všechny uložené data
        PlayerPrefs.DeleteKey("CurrentNight");
        PlayerPrefs.DeleteKey("StoryCompleted");
        PlayerPrefs.DeleteKey("SavedNightBeforeSixth");
        PlayerPrefs.Save();
        
        // Aktualizuj UI pro skrytí všech odměn
        UpdateUI();
        
        Debug.Log("Progress kompletně resetován na výchozí stav.");
    }
    
    #endregion
    
    #region Menu Transition Methods
    
    private IEnumerator ShowContinueTransition()
    {
        Debug.Log($"Zobrazuji transition panel pro continue - noc {currentNight}");
        
        // Zastav menu hudbu
        StopMenuMusic();
        
        // Skryj všechny panely
        HideAllPanels();
        
        // Zobraz přechodový panel s textem aktuální noci
        if (PanelTransition != null)
        {
            PanelTransition.SetActive(true);
            
            // Nastav text přes specifické reference
            if (TransitionTimeText != null)
            {
                TransitionTimeText.text = "12:00 AM";
            }
            
            if (TransitionNightText != null)
            {
                TransitionNightText.text = $"NIGHT {currentNight}";
            }
        }
        
        // Přehraj zvuk přechodu
        if (TransitionAudioSource != null && NightTransitionSound != null)
        {
            TransitionAudioSource.PlayOneShot(NightTransitionSound);
        }
        
        // Čekej na zobrazení transition panelu (5 sekund)
        yield return new WaitForSeconds(transitionPanelDuration);
        
        // Spusť aktuální noc
        StartGameNight(currentNight);
    }
    
    private IEnumerator ShowSixthNightTransition()
    {
        Debug.Log("Zobrazuji transition panel pro 6. noc");
        
        // Zastav menu hudbu
        StopMenuMusic();
        
        // Skryj všechny panely
        HideAllPanels();
        
        // Zobraz přechodový panel s textem 6. noci
        if (PanelTransition != null)
        {
            PanelTransition.SetActive(true);
            
            // Nastav text přes specifické reference
            if (TransitionTimeText != null)
            {
                TransitionTimeText.text = "12:00 AM";
            }
            
            if (TransitionNightText != null)
            {
                TransitionNightText.text = "NIGHT 6";
            }
        }
        
        // Přehraj zvuk přechodu
        if (TransitionAudioSource != null && NightTransitionSound != null)
        {
            TransitionAudioSource.PlayOneShot(NightTransitionSound);
        }
        
        // Čekej na zobrazení transition panelu (5 sekund)
        yield return new WaitForSeconds(transitionPanelDuration);
        
        // Spusť 6. noc
        StartGameNight(6);
    }
    
    #endregion
    
    #region Fade Transitions
    
    private IEnumerator FadeMainMenu(bool fadeIn)
    {
        if (MainMenuCanvasGroup == null) yield break;
        
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            MainMenuCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }
        
        MainMenuCanvasGroup.alpha = endAlpha;
        
        // Pokud fadeujeme ven, deaktivuj main menu panel
        if (!fadeIn && MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(false);
        }
    }
    
    private IEnumerator FadeStartPanel(bool fadeIn)
    {
        if (StartPanelCanvasGroup == null) yield break;
        
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            StartPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }
        
        StartPanelCanvasGroup.alpha = endAlpha;
    }
    
    private IEnumerator SimultaneousFade()
    {
        // Pouze fade in start panel, main menu zůstane viditelné na pozadí
        if (StartPanelCanvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            
            // Pouze start panel fade in (0 -> 1)
            // Main menu zůstává na pozadí s alpha = 1
            StartPanelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            
            yield return null;
        }
        
        // Ujisti se, že konečná hodnota je správná
        StartPanelCanvasGroup.alpha = 1f;
        
        // Main menu zůstává aktivní a viditelné na pozadí
        // NEDEAKTIVUJEMOS MainMenuPanel - zůstane viditelné
    }
    
    private IEnumerator FadeEndPanel(bool fadeIn)
    {
        if (EndPanelCanvasGroup == null) 
        {
            Debug.LogWarning("EndPanelCanvasGroup není přiřazena - fade nebude fungovat");
            yield break;
        }
        
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;
        
        Debug.Log($"Fade end panel - směr: {(fadeIn ? "IN" : "OUT")}, doba: {endPanelFadeDuration}s");
        
        while (elapsedTime < endPanelFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / endPanelFadeDuration;
            EndPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }
        
        EndPanelCanvasGroup.alpha = endAlpha;
        Debug.Log($"Fade end panel dokončen - konečná alpha: {endAlpha}");
    }
    
    #endregion
}