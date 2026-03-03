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
    [SerializeField] private bool isSixthNightCompleted = false; // Zda byla dokončena 6. noc (druhá hvězda)
    [SerializeField] private bool isSeventhNightCompleted = false; // Zda byla dokončena 7. noc (Custom Night)
    [SerializeField] private bool isSeventhNightAllMax = false; // Zda byla 7. noc dokončena se všemi AI na 20 (červená hvězda)
    [SerializeField] private int savedNightBeforeSixth = 1; // Uložená noc před spuštěním 6. noci
    [SerializeField] private int savedNightBeforeCustom = 1; // Uložená noc před spuštěním Custom Night
    [SerializeField] private bool isCustomNightActive = false; // Zda právě běží Custom Night (7. noc)
    
    [Header("Developer Mode")]
    [SerializeField] private bool devModeUnlock6thNight = false; // DEV: Zobrazí 6. noc tlačítko bez nutnosti dokončit 5 nocí
    [SerializeField] private bool devModeUnlock6thNightStar = false; // DEV: Odemkne 2. hvězdu za 6. noc bez hraní
    [SerializeField] private bool devModeUnlockGoldenSiege = false; // DEV: Odemkne GOLDEN SIEGE (10/20 mode) pro testování
    [SerializeField] private bool devModeForceSpawnBothAnimatronics = false; // DEV: Force spawn Thatcher + Warden při každém CamsOff (testování double jumpscare)
    
    [Header("Developer Mode - Challenge Completion Testing")]
    [SerializeField] private bool devModeChallenge1Completed = false; // DEV: Strong Bodies
    [SerializeField] private bool devModeChallenge2Completed = false; // DEV: Panic Mode
    [SerializeField] private bool devModeChallenge3Completed = false; // DEV: Utility Masters
    [SerializeField] private bool devModeChallenge4Completed = false; // DEV: Roamer Squad
    [SerializeField] private bool devModeChallenge5Completed = false; // DEV: Breach & Smash
    [SerializeField] private bool devModeChallenge6Completed = false; // DEV: Men's Night
    [SerializeField] private bool devModeChallenge7Completed = false; // DEV: Ladies Night
    [SerializeField] private bool devModeChallenge8Completed = false; // DEV: Top Shelf
    [SerializeField] private bool devModeChallenge9Completed = false; // DEV: Bottom Shelf
    
    [Header("Game State")]
    [SerializeField] private bool isGameActive = false; // Zda hra běží
    [SerializeField] private bool isInMainMenu = true; // Zda jsme v main menu
    
    [Header("UI References")]
    [SerializeField] private GameObject MainMenuPanel; // Panel hlavního menu
    [SerializeField] private GameObject PanelBlackPozadi; // Černé pozadí pro menu
    [SerializeField] private Button StartNewGameButton; // Tlačítko pro novou hru
    [SerializeField] private Button ContinueGameButton; // Tlačítko pro pokračování
    [SerializeField] private Button SixthNightButton; // Tlačítko pro 6. noc (objeví se po dokončení 5 nocí)
    [SerializeField] private Button CustomNightButton; // Tlačítko pro Custom Night / 7. noc (objeví se po dokončení 6. noci)
    [SerializeField] private Button CreditsButton; // Tlačítko pro Credits
    [SerializeField] private GameObject CompletionStar; // Hvězda za dokončení základní dějové linky (5 nocí)
    [SerializeField] private GameObject SixthNightCompletionStar; // Hvězda za dokončení 6. noci
    [SerializeField] private Button QuitGameButton; // Tlačítko pro ukončení
    [SerializeField] private TextMeshProUGUI NightDisplayText; // Text pro zobrazení aktuální noci (např. "NIGHT 1")
    
    [Header("Credits Panel")]
    [SerializeField] private GameObject Credits_Panel; // Panel pro credits
    [SerializeField] private Button CreditsBackButton; // Tlačítko "Back" v Credits panelu
    
    [Header("Custom Night Panel")]
    [SerializeField] private GameObject CustomNightPanel; // Panel pro Custom Night nastavení
    [SerializeField] private Button CustomNightBackButton; // Tlačítko "Back" v Custom Night panelu
    [SerializeField] private Button SeventhNightStartButton; // Tlačítko "7th Night" pro spuštění Custom Night
    [SerializeField] private GameObject SeventhNightCompletionStar; // Červená hvězda za dokončení 7. noci se všemi AI na 20
    
    // Dictionary pro uložení původních scale hodnot tlačítek
    private System.Collections.Generic.Dictionary<Button, Vector3> originalButtonScales = new System.Collections.Generic.Dictionary<Button, Vector3>();
    
    [Header("Custom Night Challenge Stars (9 stars)")]
    [SerializeField] private GameObject ChallengeStar1; // Strong Bodies
    [SerializeField] private GameObject ChallengeStar2; // Panic Mode
    [SerializeField] private GameObject ChallengeStar3; // Utility Masters
    [SerializeField] private GameObject ChallengeStar4; // Roamer Squad
    [SerializeField] private GameObject ChallengeStar5; // Breach & Smash
    [SerializeField] private GameObject ChallengeStar6; // Men's Night
    [SerializeField] private GameObject ChallengeStar7; // Ladies Night
    [SerializeField] private GameObject ChallengeStar8; // Top Shelf
    [SerializeField] private GameObject ChallengeStar9; // Bottom Shelf
    
    [Header("Custom Night - Animatronic AI Settings (1-10)")]
    [SerializeField] private Image araniImage; // (1) ARANI
    [SerializeField] private TextMeshProUGUI araniAIText;
    [SerializeField] private Button araniLeftButton;
    [SerializeField] private Button araniRightButton;
    [Range(0, 20)] [SerializeField] private int araniAI = 0;
    
    [SerializeField] private Image fenerirImage; // (2) FENERIR
    [SerializeField] private TextMeshProUGUI fenerirAIText;
    [SerializeField] private Button fenerirLeftButton;
    [SerializeField] private Button fenerirRightButton;
    [Range(0, 20)] [SerializeField] private int fenerirAI = 0;
    
    [SerializeField] private Image sledgeImage; // (3) SLEDGY
    [SerializeField] private TextMeshProUGUI sledgeAIText;
    [SerializeField] private Button sledgeLeftButton;
    [SerializeField] private Button sledgeRightButton;
    [Range(0, 20)] [SerializeField] private int sledgeAI = 0;
    
    [SerializeField] private Image oraxImage; // (4) ORAX
    [SerializeField] private TextMeshProUGUI oraxAIText;
    [SerializeField] private Button oraxLeftButton;
    [SerializeField] private Button oraxRightButton;
    [Range(0, 20)] [SerializeField] private int oraxAI = 0;
    
    [SerializeField] private Image thatcherImage; // (5) THETHER
    [SerializeField] private TextMeshProUGUI thatcherAIText;
    [SerializeField] private Button thatcherLeftButton;
    [SerializeField] private Button thatcherRightButton;
    [Range(0, 20)] [SerializeField] private int thatcherAI = 0;
    
    [SerializeField] private Image dokkiImage; // (6) DOKKI
    [SerializeField] private TextMeshProUGUI dokkiAIText;
    [SerializeField] private Button dokkiLeftButton;
    [SerializeField] private Button dokkiRightButton;
    [Range(0, 20)] [SerializeField] private int dokkiAI = 0;
    
    [SerializeField] private Image caviraImage; // (7) CEVEIRA
    [SerializeField] private TextMeshProUGUI caviraAIText;
    [SerializeField] private Button caviraLeftButton;
    [SerializeField] private Button caviraRightButton;
    [Range(0, 20)] [SerializeField] private int caviraAI = 0;
    
    [SerializeField] private Image ianidImage; // (8) IANID
    [SerializeField] private TextMeshProUGUI ianidAIText;
    [SerializeField] private Button ianidLeftButton;
    [SerializeField] private Button ianidRightButton;
    [Range(0, 20)] [SerializeField] private int ianidAI = 0;
    
    [SerializeField] private Image zofeyeImage; // (9) ZOFEYE
    [SerializeField] private TextMeshProUGUI zofeyeAIText;
    [SerializeField] private Button zofeyeLeftButton;
    [SerializeField] private Button zofeyeRightButton;
    [Range(0, 20)] [SerializeField] private int zofeyeAI = 0;
    
    [SerializeField] private Image wardenImage; // (10) WERDEN
    [SerializeField] private TextMeshProUGUI wardenAIText;
    [SerializeField] private Button wardenLeftButton;
    [SerializeField] private Button wardenRightButton;
    [Range(0, 20)] [SerializeField] private int wardenAI = 0;
    
    // Hold-to-repeat pro Custom Night tlačítka
    private Coroutine currentHoldCoroutine = null;
    [SerializeField] private float holdRepeatDelay = 0.06f; // Delay mezi změnami při držení tlačítka (rychlé)
    [SerializeField] private float holdStartDelay = 0.15f; // Čas před začátkem opakování
    
    [Header("Custom Night Audio")]
    [SerializeField] private AudioSource customNightAudioSource; // Audio source pro Custom Night zvuky
    [SerializeField] private AudioClip customNightClickSound; // Zvuk pro změnu AI
    
    [Header("Button Hover Audio")]
    [SerializeField] private AudioSource hoverAudioSource; // Audio source pro hover zvuky tlačítek
    [SerializeField] private AudioClip hoverSound; // Zvuk při najetí na tlačítko
    
    [Header("Custom Night Challenges")]
    [SerializeField] private TextMeshProUGUI ChallengeText; // Text zobrazující název aktuální challenge
    [SerializeField] private Button ChallengeLeftButton; // Tlačítko pro předchozí challenge
    [SerializeField] private Button ChallengeRightButton; // Tlačítko pro následující challenge
    
    // Challenge systém
    private int currentChallengeIndex = 0; // 0-9 = challenges, 10 = CUSTOM
    private bool isManuallyModified = false; // Flag pro ruční úpravu AI (přepne na CUSTOM)
    
    // Challenge completion tracking (9 challenges, index 0-8)
    private bool[] challengeCompleted = new bool[9];
    
    // Color constants for challenge text
    private readonly Color normalColor = Color.white;
    private readonly Color completedColor = new Color(1f, 0.92f, 0.016f); // Žlutá
    private readonly Color lockedColor = new Color(0.4f, 0.4f, 0.4f); // Tmavě šedá
    
    // Challenge presets (10 challenges + 1 CUSTOM)
    private readonly string[] challengeNames = new string[]
    {
        "Strong Bodies",    // 0
        "Panic Mode",       // 1
        "Utility Masters",  // 2
        "Roamer Squad",     // 3
        "Breach & Smash",   // 4
        "Men's Night",      // 5
        "Ladies Night",     // 6
        "Top Shelf",        // 7
        "Bottom Shelf",     // 8
        "GOLDEN SIEGE",     // 9
        "CUSTOM"            // 10
    };
    
    // Challenge AI presets [challengeIndex][animatronicIndex]
    // Animatronic order: Arani, Fenerir, Sledge, Orax, Thatcher, Dokki, Cavira, Ianid, Zofeye, Warden
    private readonly int[,] challengePresets = new int[,]
    {
        // 0: Strong Bodies - Arani(20), Sledge(20), Orax(20), Cavira(20)
        { 20, 0, 20, 20, 0, 0, 20, 0, 0, 0 },
        
        // 1: Panic Mode - Fenerir(20), Orax(20), Dokki(20), Cavira(20), Ianid(20), Zofeye(20)
        { 0, 20, 0, 20, 0, 20, 20, 20, 20, 0 },
        
        // 2: Utility Masters - Arani(20), Fenerir(20), Thatcher(20), Dokki(20), Zofeye(20), Warden(20)
        { 20, 20, 0, 0, 20, 20, 0, 0, 20, 20 },
        
        // 3: Roamer Squad - Arani(20), Orax(20), Cavira(20), Warden(20)
        { 20, 0, 0, 20, 0, 0, 20, 0, 0, 20 },
        
        // 4: Breach & Smash - Arani(20), Sledge(20), Orax(20), Thatcher(20), Dokki(20), Zofeye(20)
        { 20, 0, 20, 20, 20, 20, 0, 0, 20, 0 },
        
        // 5: Men's Night - Fenerir(20), Sledge(20), Orax(20), Thatcher(20), Warden(20)
        { 0, 20, 20, 20, 20, 0, 0, 0, 0, 20 },
        
        // 6: Ladies Night - Arani(20), Dokki(20), Cavira(20), Ianid(20), Zofeye(20)
        { 20, 0, 0, 0, 0, 20, 20, 20, 20, 0 },
        
        // 7: Top Shelf - První 5 animatroniků na 20
        { 20, 20, 20, 20, 20, 0, 0, 0, 0, 0 },
        
        // 8: Bottom Shelf - Druhá 5 animatroniků na 20
        { 0, 0, 0, 0, 0, 20, 20, 20, 20, 20 },
        
        // 9: GOLDEN SIEGE (10/20) - Všichni na 20
        { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 }
    };
    
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
    
    [Header("6th Night End Panel")]
    [SerializeField] private GameObject Panel6thNightEnd; // Panel pro konec 6. noci
    [SerializeField] private TextMeshProUGUI SixthNightEndText; // Text na 6th night end panelu
    [SerializeField] private CanvasGroup SixthNightEndPanelCanvasGroup; // CanvasGroup pro fade 6th night end panel
    [SerializeField] private AudioSource SixthNightEndingAudioSource; // AudioSource pro 6th night ending music
    [SerializeField] private AudioClip SixthNightEndingSound; // Zvuk/hudba pro ending 6. noci
    [SerializeField] private float sixthNightEndPanelDuration = 25f; // Celková doba zobrazení 6th night end panelu (25 sekund)
    [SerializeField] private float sixthNightEndFadeInDuration = 5f; // Doba fade in (5 sekund)
    [SerializeField] private float sixthNightEndFadeOutDuration = 5f; // Doba fade out (5 sekund)
    [SerializeField] private float sixthNightEndVideoDelay = 8f; // Doba čekání na end video 6. noci (8 sekund)
    
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
    
    // Unity Editor callback - volá se při změně hodnot v inspectoru
    private void OnValidate()
    {
        // V editoru aktualizuj UI při změně dev toggles
        if (!Application.isPlaying)
        {
            return;
        }
        
        // Aplikuj dev mode challenge completion flags
        ApplyDevModeChallengeFlags();
        
        // Okamžitě aktualizuj UI když se změní dev módy
        UpdateUI();
    }
    
    void Start()
    {
        Debug.Log("NightShiftManagement inicializován - verze: " + nightShiftVersion);
        
        // Nastav UI tlačítka
        SetupUIButtons();
        
        // Načti uložený progress
        LoadNightProgress();
        
        // Načti dokončené challenge
        LoadChallengeCompletionFlags();
        
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
            SetupButtonHover(StartNewGameButton);
        }
        
        if (ContinueGameButton != null)
        {
            ContinueGameButton.onClick.AddListener(ContinueGame);
            SetupButtonHover(ContinueGameButton, true); // true = speciální chování pro Continue
        }
        
        if (SixthNightButton != null)
        {
            SixthNightButton.onClick.AddListener(StartSixthNight);
            SetupButtonHover(SixthNightButton);
        }
        
        if (CustomNightButton != null)
        {
            CustomNightButton.onClick.AddListener(OpenCustomNightPanel);
            SetupButtonHover(CustomNightButton);
        }
        
        if (CreditsButton != null)
        {
            CreditsButton.onClick.AddListener(OpenCreditsPanel);
            SetupButtonHover(CreditsButton, false, false, new Vector3(0.59343f, 0.59343f, 1f)); // Původní scale 0.59343
        }
        
        if (CustomNightBackButton != null)
        {
            CustomNightBackButton.onClick.AddListener(CloseCustomNightPanel);
            SetupButtonHover(CustomNightBackButton);
        }
        
        if (CreditsBackButton != null)
        {
            CreditsBackButton.onClick.AddListener(CloseCreditsPanel);
            SetupButtonHover(CreditsBackButton);
        }
        
        if (SeventhNightStartButton != null)
        {
            SeventhNightStartButton.onClick.AddListener(StartCustomNight);
            SetupButtonHover(SeventhNightStartButton, false, true); // true = speciální chování pro 7th Night
        }
        
        if (QuitGameButton != null)
        {
            QuitGameButton.onClick.AddListener(QuitGame);
            SetupButtonHover(QuitGameButton);
        }
        
        // Setup Custom Night AI buttons
        SetupCustomNightButtons();
        
        // Setup Challenge buttons
        SetupChallengeButtons();
    }
    
    private void UpdateUI()
    {
        // Aktualizuj dostupnost tlačítka Continue podle progressu
        if (ContinueGameButton != null)
        {
            // Continue je dostupné pokud: máme progress (noc > 1) A noc není 6 (6. noc má vlastní tlačítko)
            bool canContinue = (currentNight > 1 && currentNight <= 5);
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
                
                // Nastav správný text tlačítka podle aktuální noci (maximálně noc 5)
                if (currentNight == 1)
                {
                    buttonText.text = "CONTINUE";
                }
                else if (currentNight >= 2 && currentNight <= 5)
                {
                    buttonText.text = $"CONTINUE ({currentNight})";
                }
                else if (currentNight == 6)
                {
                    // Pro 6. noc zobraz poslední dostupnou noc (5)
                    buttonText.text = "CONTINUE (5)";
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
        
        // Aktualizuj dostupnost 6th Night tlačítka (zobraz pokud je dokončena story NEBO dev mód)
        bool show6thNightButton = isStoryCompleted || devModeUnlock6thNight;
        if (SixthNightButton != null)
        {
            SixthNightButton.gameObject.SetActive(show6thNightButton);
            SixthNightButton.interactable = show6thNightButton;
        }
        
        // Aktualizuj dostupnost Custom Night tlačítka (zobraz pokud je dokončena 6. noc)
        bool showCustomNightButton = isSixthNightCompleted || devModeUnlock6thNightStar; // Dev mód pro testování
        if (CustomNightButton != null)
        {
            CustomNightButton.gameObject.SetActive(showCustomNightButton);
            CustomNightButton.interactable = showCustomNightButton;
        }
        
        // Zobraz hvězdy pouze pokud je main menu aktivní (NEBO dev mód - pro testování)
        bool shouldShowStars = (MainMenuPanel != null && MainMenuPanel.activeSelf);
        
        // Třetí hvězda (7. noc - červená) - zobrazí se pouze pokud byla 7. noc dokončena se VŠEMI AI na 20
        bool showSeventhNightStar = isSeventhNightAllMax && shouldShowStars;
        if (SeventhNightCompletionStar != null)
        {
            SeventhNightCompletionStar.SetActive(showSeventhNightStar);
        }
        
        // Aktualizuj viditelnost challenge hvězd (9 hvězd)
        UpdateChallengeStarsVisibility();
        
        // První hvězda (5 nocí)
        if (CompletionStar != null)
        {
            CompletionStar.SetActive(isStoryCompleted && shouldShowStars);
        }
        
        // Druhá hvězda (6. noc) - zobrazí se pokud je dokončena 6. noc NEBO dev mód
        // V dev módu se zobrazí i bez hlavního menu (pro testování v inspectoru)
        bool showSixthNightStar = (isSixthNightCompleted || devModeUnlock6thNightStar);
        if (devModeUnlock6thNightStar)
        {
            // Dev mód - zobraz vždy
            if (SixthNightCompletionStar != null)
            {
                SixthNightCompletionStar.SetActive(true);
            }
        }
        else
        {
            // Normální mód - pouze když je main menu aktivní
            if (SixthNightCompletionStar != null)
            {
                SixthNightCompletionStar.SetActive(showSixthNightStar && shouldShowStars);
            }
        }
        
        // Aktualizuj text noci
        UpdateNightDisplayText();
        
        Debug.Log($"UI aktualizováno - Aktuální noc: {currentNight}, Continue dostupné: {(currentNight > 1 && currentNight <= 5)}, 6th Night tlačítko: {(isStoryCompleted || devModeUnlock6thNight)}, Druhá hvězda (dev): {devModeUnlock6thNightStar}, Druhá hvězda (completed): {isSixthNightCompleted}");
    }
    
    #endregion
    
    #region Custom Night Panel Management
    
    private void OpenCustomNightPanel()
    {
        Debug.Log("Otevírám Custom Night panel");
        
        // Přehraj zvuk pro otevření panelu
        if (customNightAudioSource != null && customNightClickSound != null)
        {
            customNightAudioSource.PlayOneShot(customNightClickSound);
        }
        
        // Deaktivuj hlavní menu
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(false);
        }
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(false);
        }
        
        // Aktivuj Custom Night panel
        if (CustomNightPanel != null)
        {
            CustomNightPanel.SetActive(true);
        }
        
        // Načti uloženou challenge a aktualizuj UI
        LoadChallengeIndex();
        UpdateChallengeText();
        
        // Aktualizuj stav 7th Night tlačítka (pro zamčený GOLDEN SIEGE)
        UpdateSeventhNightButtonState();
    }
    
    private void CloseCustomNightPanel()
    {
        Debug.Log("Zavírám Custom Night panel");
        
        // ODBLOKUJ + SCHOVAT THATCHERA při návratu do menu
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[CloseCustomNightPanel] Thatcher force unblocked + hidden");
        }
        
        // Přehraj zvuk pro zavření panelu
        if (customNightAudioSource != null && customNightClickSound != null)
        {
            customNightAudioSource.PlayOneShot(customNightClickSound);
        }
        
        // Deaktivuj Custom Night panel
        if (CustomNightPanel != null)
        {
            CustomNightPanel.SetActive(false);
        }
        
        // Aktivuj hlavní menu zpět
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(true);
        }
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(true);
        }
        
        // Aktualizuj UI (obnoví viditelnost hvězd)
        UpdateUI();
    }
    
    #endregion
    
    #region Credits Panel Management
    
    private void OpenCreditsPanel()
    {
        Debug.Log("Otevírám Credits panel");
        
        // Přehraj zvuk pro otevření panelu
        if (customNightAudioSource != null && customNightClickSound != null)
        {
            customNightAudioSource.PlayOneShot(customNightClickSound);
        }
        
        // Deaktivuj hlavní menu
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(false);
        }
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(false);
        }
        
        // Aktivuj Credits panel
        if (Credits_Panel != null)
        {
            Credits_Panel.SetActive(true);
        }
    }
    
    private void CloseCreditsPanel()
    {
        Debug.Log("Zavírám Credits panel");
        
        // Přehraj zvuk pro zavření panelu
        if (customNightAudioSource != null && customNightClickSound != null)
        {
            customNightAudioSource.PlayOneShot(customNightClickSound);
        }
        
        // ODBLOKUJ + SCHOVAT THATCHERA při návratu do menu
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[CloseCreditsPanel] Thatcher force unblocked + hidden");
        }
        
        // Deaktivuj Credits panel
        if (Credits_Panel != null)
        {
            Credits_Panel.SetActive(false);
        }
        
        // Aktivuj hlavní menu zpět
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(true);
        }
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(true);
        }
    }
    
    #endregion
    
    #region Custom Night AI Button Setup
    
    private void SetupCustomNightButtons()
    {
        Debug.Log("[CUSTOM NIGHT] Za\u010d\u00edn\u00e1m setupovat Custom Night tla\u010d\u00edtka...");
        
        // ARANI (1)
        if (araniLeftButton != null)
        {
            araniLeftButton.onClick.AddListener(() => ModifyCustomNightAI(0, -1));
            Debug.Log("[CUSTOM NIGHT] Arani LEFT tla\u010d\u00edtko p\u0159ipojeno");
        }
        else
        {
            Debug.LogWarning("[CUSTOM NIGHT] Arani LEFT tla\u010d\u00edtko JE NULL!");
        }
        
        if (araniRightButton != null)
        {
            araniRightButton.onClick.AddListener(() => ModifyCustomNightAI(0, 1));
            Debug.Log("[CUSTOM NIGHT] Arani RIGHT tla\u010d\u00edtko p\u0159ipojeno");
        }
        else
        {
            Debug.LogWarning("[CUSTOM NIGHT] Arani RIGHT tla\u010d\u00edtko JE NULL!");
        }
        
        // FENERIR (2)
        if (fenerirLeftButton != null) fenerirLeftButton.onClick.AddListener(() => ModifyCustomNightAI(1, -1));
        if (fenerirRightButton != null) fenerirRightButton.onClick.AddListener(() => ModifyCustomNightAI(1, 1));
        
        // SLEDGE (3)
        if (sledgeLeftButton != null) sledgeLeftButton.onClick.AddListener(() => ModifyCustomNightAI(2, -1));
        if (sledgeRightButton != null) sledgeRightButton.onClick.AddListener(() => ModifyCustomNightAI(2, 1));
        
        // ORAX (4)
        if (oraxLeftButton != null) oraxLeftButton.onClick.AddListener(() => ModifyCustomNightAI(3, -1));
        if (oraxRightButton != null) oraxRightButton.onClick.AddListener(() => ModifyCustomNightAI(3, 1));
        
        // THATCHER (5)
        if (thatcherLeftButton != null) thatcherLeftButton.onClick.AddListener(() => ModifyCustomNightAI(4, -1));
        if (thatcherRightButton != null) thatcherRightButton.onClick.AddListener(() => ModifyCustomNightAI(4, 1));
        
        // DOKKI (6)
        if (dokkiLeftButton != null) dokkiLeftButton.onClick.AddListener(() => ModifyCustomNightAI(5, -1));
        if (dokkiRightButton != null) dokkiRightButton.onClick.AddListener(() => ModifyCustomNightAI(5, 1));
        
        // CAVIRA (7)
        if (caviraLeftButton != null) caviraLeftButton.onClick.AddListener(() => ModifyCustomNightAI(6, -1));
        if (caviraRightButton != null) caviraRightButton.onClick.AddListener(() => ModifyCustomNightAI(6, 1));
        
        // IANID (8)
        if (ianidLeftButton != null) ianidLeftButton.onClick.AddListener(() => ModifyCustomNightAI(7, -1));
        if (ianidRightButton != null) ianidRightButton.onClick.AddListener(() => ModifyCustomNightAI(7, 1));
        
        // ZOFEYE (9)
        if (zofeyeLeftButton != null) zofeyeLeftButton.onClick.AddListener(() => ModifyCustomNightAI(8, -1));
        if (zofeyeRightButton != null) zofeyeRightButton.onClick.AddListener(() => ModifyCustomNightAI(8, 1));
        
        // WARDEN (10)
        if (wardenLeftButton != null) wardenLeftButton.onClick.AddListener(() => ModifyCustomNightAI(9, -1));
        if (wardenRightButton != null) wardenRightButton.onClick.AddListener(() => ModifyCustomNightAI(9, 1));

        // Přidej hold-to-repeat funkcionalitu na všechna tlačítka
        SetupHoldToRepeat();
        
        // Přidej hover zvuky na všechna Custom Night tlačítka
        SetupCustomNightButtonHover();

        // Načti uložené hodnoty a aktualizuj UI
        LoadCustomNightSettings();
        UpdateAllCustomNightUI();
    }
    
    private void SetupHoldToRepeat()
    {
        // ARANI
        if (araniLeftButton != null) AddHoldToRepeat(araniLeftButton, 0, -1);
        if (araniRightButton != null) AddHoldToRepeat(araniRightButton, 0, 1);
        
        // FENERIR
        if (fenerirLeftButton != null) AddHoldToRepeat(fenerirLeftButton, 1, -1);
        if (fenerirRightButton != null) AddHoldToRepeat(fenerirRightButton, 1, 1);
        
        // SLEDGE
        if (sledgeLeftButton != null) AddHoldToRepeat(sledgeLeftButton, 2, -1);
        if (sledgeRightButton != null) AddHoldToRepeat(sledgeRightButton, 2, 1);
        
        // ORAX
        if (oraxLeftButton != null) AddHoldToRepeat(oraxLeftButton, 3, -1);
        if (oraxRightButton != null) AddHoldToRepeat(oraxRightButton, 3, 1);
        
        // THATCHER
        if (thatcherLeftButton != null) AddHoldToRepeat(thatcherLeftButton, 4, -1);
        if (thatcherRightButton != null) AddHoldToRepeat(thatcherRightButton, 4, 1);
        
        // DOKKI
        if (dokkiLeftButton != null) AddHoldToRepeat(dokkiLeftButton, 5, -1);
        if (dokkiRightButton != null) AddHoldToRepeat(dokkiRightButton, 5, 1);
        
        // CAVIRA
        if (caviraLeftButton != null) AddHoldToRepeat(caviraLeftButton, 6, -1);
        if (caviraRightButton != null) AddHoldToRepeat(caviraRightButton, 6, 1);
        
        // IANID
        if (ianidLeftButton != null) AddHoldToRepeat(ianidLeftButton, 7, -1);
        if (ianidRightButton != null) AddHoldToRepeat(ianidRightButton, 7, 1);
        
        // ZOFEYE
        if (zofeyeLeftButton != null) AddHoldToRepeat(zofeyeLeftButton, 8, -1);
        if (zofeyeRightButton != null) AddHoldToRepeat(zofeyeRightButton, 8, 1);
        
        // WARDEN
        if (wardenLeftButton != null) AddHoldToRepeat(wardenLeftButton, 9, -1);
        if (wardenRightButton != null) AddHoldToRepeat(wardenRightButton, 9, 1);
    }
    
    private void AddHoldToRepeat(Button button, int animatronicIndex, int change)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // PointerDown - začni držení
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { OnButtonHoldStart(animatronicIndex, change); });
        trigger.triggers.Add(pointerDownEntry);
        
        // PointerUp - ukonči držení
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { OnButtonHoldEnd(); });
        trigger.triggers.Add(pointerUpEntry);
        
        // PointerExit - ukonči držení (pokud myš opustí tlačítko)
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnButtonHoldEnd(); });
        trigger.triggers.Add(pointerExitEntry);
    }
    
    private void OnButtonHoldStart(int animatronicIndex, int change)
    {
        // Zastav předchozí coroutine pokud běží
        if (currentHoldCoroutine != null)
        {
            StopCoroutine(currentHoldCoroutine);
        }
        
        // Spusť novou coroutine pro hold-to-repeat
        currentHoldCoroutine = StartCoroutine(HoldRepeatCoroutine(animatronicIndex, change));
    }
    
    private void OnButtonHoldEnd()
    {
        // Zastav coroutine
        if (currentHoldCoroutine != null)
        {
            StopCoroutine(currentHoldCoroutine);
            currentHoldCoroutine = null;
        }
    }
    
    private void SetupCustomNightButtonHover()
    {
        // Přidej hover efekt a zvuk na všechna AI tlačítka
        // ARANI
        if (araniLeftButton != null) SetupButtonHover(araniLeftButton);
        if (araniRightButton != null) SetupButtonHover(araniRightButton);
        
        // FENERIR
        if (fenerirLeftButton != null) SetupButtonHover(fenerirLeftButton);
        if (fenerirRightButton != null) SetupButtonHover(fenerirRightButton);
        
        // SLEDGE
        if (sledgeLeftButton != null) SetupButtonHover(sledgeLeftButton);
        if (sledgeRightButton != null) SetupButtonHover(sledgeRightButton);
        
        // ORAX
        if (oraxLeftButton != null) SetupButtonHover(oraxLeftButton);
        if (oraxRightButton != null) SetupButtonHover(oraxRightButton);
        
        // THATCHER
        if (thatcherLeftButton != null) SetupButtonHover(thatcherLeftButton);
        if (thatcherRightButton != null) SetupButtonHover(thatcherRightButton);
        
        // DOKKI
        if (dokkiLeftButton != null) SetupButtonHover(dokkiLeftButton);
        if (dokkiRightButton != null) SetupButtonHover(dokkiRightButton);
        
        // CAVIRA
        if (caviraLeftButton != null) SetupButtonHover(caviraLeftButton);
        if (caviraRightButton != null) SetupButtonHover(caviraRightButton);
        
        // IANID
        if (ianidLeftButton != null) SetupButtonHover(ianidLeftButton);
        if (ianidRightButton != null) SetupButtonHover(ianidRightButton);
        
        // ZOFEYE
        if (zofeyeLeftButton != null) SetupButtonHover(zofeyeLeftButton);
        if (zofeyeRightButton != null) SetupButtonHover(zofeyeRightButton);
        
        // WARDEN
        if (wardenLeftButton != null) SetupButtonHover(wardenLeftButton);
        if (wardenRightButton != null) SetupButtonHover(wardenRightButton);
    }
    
    private IEnumerator HoldRepeatCoroutine(int animatronicIndex, int change)
    {
        // Počkej na začáteční delay
        yield return new WaitForSeconds(holdStartDelay);
        
        // Opakuj dokud je tlačítko drženo
        while (true)
        {
            ModifyCustomNightAI(animatronicIndex, change);
            yield return new WaitForSeconds(holdRepeatDelay);
        }
    }
    
    private void ModifyCustomNightAI(int animatronicIndex, int change)
    {
        int oldValue = 0;
        int newValue = 0;
        
        switch (animatronicIndex)
        {
            case 0: // ARANI
                oldValue = araniAI;
                araniAI = Mathf.Clamp(araniAI + change, 0, 20);
                newValue = araniAI;
                break;
            case 1: // FENERIR
                oldValue = fenerirAI;
                fenerirAI = Mathf.Clamp(fenerirAI + change, 0, 20);
                newValue = fenerirAI;
                break;
            case 2: // SLEDGE
                oldValue = sledgeAI;
                sledgeAI = Mathf.Clamp(sledgeAI + change, 0, 20);
                newValue = sledgeAI;
                break;
            case 3: // ORAX
                oldValue = oraxAI;
                oraxAI = Mathf.Clamp(oraxAI + change, 0, 20);
                newValue = oraxAI;
                break;
            case 4: // THATCHER
                oldValue = thatcherAI;
                thatcherAI = Mathf.Clamp(thatcherAI + change, 0, 20);
                newValue = thatcherAI;
                break;
            case 5: // DOKKI
                oldValue = dokkiAI;
                dokkiAI = Mathf.Clamp(dokkiAI + change, 0, 20);
                newValue = dokkiAI;
                break;
            case 6: // CAVIRA
                oldValue = caviraAI;
                caviraAI = Mathf.Clamp(caviraAI + change, 0, 20);
                newValue = caviraAI;
                break;
            case 7: // IANID
                oldValue = ianidAI;
                ianidAI = Mathf.Clamp(ianidAI + change, 0, 20);
                newValue = ianidAI;
                break;
            case 8: // ZOFEYE
                oldValue = zofeyeAI;
                zofeyeAI = Mathf.Clamp(zofeyeAI + change, 0, 20);
                newValue = zofeyeAI;
                break;
            case 9: // WARDEN
                oldValue = wardenAI;
                wardenAI = Mathf.Clamp(wardenAI + change, 0, 20);
                newValue = wardenAI;
                break;
        }
        
        // Přehraj zvuk POUZE pokud se hodnota skutečně změnila
        if (oldValue != newValue)
        {
            if (customNightAudioSource != null && customNightClickSound != null)
            {
                customNightAudioSource.PlayOneShot(customNightClickSound);
            }
            
            // Označ jako ručně upravené a přepni na CUSTOM
            isManuallyModified = true;
            currentChallengeIndex = 10; // Index pro CUSTOM
            UpdateChallengeText();
            
            // Aktualizuj stav 7th Night tlačítka (pro případ 10/20 bez GOLDEN SIEGE)
            UpdateSeventhNightButtonState();
        }
        
        UpdateCustomNightUI(animatronicIndex);
        SaveCustomNightSettings();
    }
    
    private void UpdateCustomNightUI(int animatronicIndex)
    {
        switch (animatronicIndex)
        {
            case 0: // ARANI
                if (araniAIText != null) araniAIText.text = araniAI.ToString();
                UpdateImageAlpha(araniImage, araniAI);
                break;
            case 1: // FENERIR
                if (fenerirAIText != null) fenerirAIText.text = fenerirAI.ToString();
                UpdateImageAlpha(fenerirImage, fenerirAI);
                break;
            case 2: // SLEDGE
                if (sledgeAIText != null) sledgeAIText.text = sledgeAI.ToString();
                UpdateImageAlpha(sledgeImage, sledgeAI);
                break;
            case 3: // ORAX
                if (oraxAIText != null) oraxAIText.text = oraxAI.ToString();
                UpdateImageAlpha(oraxImage, oraxAI);
                break;
            case 4: // THATCHER
                if (thatcherAIText != null) thatcherAIText.text = thatcherAI.ToString();
                UpdateImageAlpha(thatcherImage, thatcherAI);
                break;
            case 5: // DOKKI
                if (dokkiAIText != null) dokkiAIText.text = dokkiAI.ToString();
                UpdateImageAlpha(dokkiImage, dokkiAI);
                break;
            case 6: // CAVIRA
                if (caviraAIText != null) caviraAIText.text = caviraAI.ToString();
                UpdateImageAlpha(caviraImage, caviraAI);
                break;
            case 7: // IANID
                if (ianidAIText != null) ianidAIText.text = ianidAI.ToString();
                UpdateImageAlpha(ianidImage, ianidAI);
                break;
            case 8: // ZOFEYE
                if (zofeyeAIText != null) zofeyeAIText.text = zofeyeAI.ToString();
                UpdateImageAlpha(zofeyeImage, zofeyeAI);
                break;
            case 9: // WARDEN
                if (wardenAIText != null) wardenAIText.text = wardenAI.ToString();
                UpdateImageAlpha(wardenImage, wardenAI);
                break;
        }
    }
    
    private void UpdateAllCustomNightUI()
    {
        for (int i = 0; i < 10; i++)
        {
            UpdateCustomNightUI(i);
        }
        UpdateChallengeText();
    }
    
    private void UpdateImageAlpha(Image image, int aiLevel)
    {
        if (image == null) return;
        
        Color color = image.color;
        color.a = (aiLevel == 0) ? 0.15f : 1.0f; // 15% pokud AI = 0, jinak 100%
        image.color = color;
    }
    
    private void StartCustomNight()
    {
        Debug.Log("========== STARTING CUSTOM NIGHT (7TH NIGHT) ==========");
        Debug.Log($"AI Levels: Arani={araniAI}, Fenerir={fenerirAI}, Sledge={sledgeAI}, Orax={oraxAI}, Thatcher={thatcherAI}, Dokki={dokkiAI}, Cavira={caviraAI}, Ianid={ianidAI}, Zofeye={zofeyeAI}, Warden={wardenAI}");
        
        // ODBLOKUJ + SCHOVAT THATCHERA před spuštěním Custom Night
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[StartCustomNight] Thatcher force unblocked + hidden");
        }
        
        // Nastav Custom Night flag
        isCustomNightActive = true;
        
        // KRITICKÉ: Ulož současnou noc před spuštěním Custom Night
        savedNightBeforeCustom = currentNight;
        Debug.Log($"Ukládám současnou noc před Custom Night: {savedNightBeforeCustom}");
        
        // Ulož Custom Night nastavení
        SaveCustomNightSettings();
        
        // Zavři Custom Night panel
        if (CustomNightPanel != null)
        {
            CustomNightPanel.SetActive(false);
        }
        
        // Nastav noc na 7
        currentNight = 7;
        SaveNightProgress();
        
        // Spusť pouze 7. noc (ne celou hru od začátku)
        StartCoroutine(ShowCustomNightTransition());
    }
    
    private void SaveCustomNightSettings()
    {
        PlayerPrefs.SetInt("CustomNight_Arani", araniAI);
        PlayerPrefs.SetInt("CustomNight_Fenerir", fenerirAI);
        PlayerPrefs.SetInt("CustomNight_Sledge", sledgeAI);
        PlayerPrefs.SetInt("CustomNight_Orax", oraxAI);
        PlayerPrefs.SetInt("CustomNight_Thatcher", thatcherAI);
        PlayerPrefs.SetInt("CustomNight_Dokki", dokkiAI);
        PlayerPrefs.SetInt("CustomNight_Cavira", caviraAI);
        PlayerPrefs.SetInt("CustomNight_Ianid", ianidAI);
        PlayerPrefs.SetInt("CustomNight_Zofeye", zofeyeAI);
        PlayerPrefs.SetInt("CustomNight_Warden", wardenAI);
        PlayerPrefs.SetInt("CustomNight_ChallengeIndex", currentChallengeIndex);
        PlayerPrefs.Save();
    }
    
    private void LoadCustomNightSettings()
    {
        araniAI = PlayerPrefs.GetInt("CustomNight_Arani", 0);
        fenerirAI = PlayerPrefs.GetInt("CustomNight_Fenerir", 0);
        sledgeAI = PlayerPrefs.GetInt("CustomNight_Sledge", 0);
        oraxAI = PlayerPrefs.GetInt("CustomNight_Orax", 0);
        thatcherAI = PlayerPrefs.GetInt("CustomNight_Thatcher", 0);
        dokkiAI = PlayerPrefs.GetInt("CustomNight_Dokki", 0);
        caviraAI = PlayerPrefs.GetInt("CustomNight_Cavira", 0);
        ianidAI = PlayerPrefs.GetInt("CustomNight_Ianid", 0);
        zofeyeAI = PlayerPrefs.GetInt("CustomNight_Zofeye", 0);
        wardenAI = PlayerPrefs.GetInt("CustomNight_Warden", 0);
    }
    
    public int GetCustomNightAI(string animatronicName)
    {
        // KRITICKÉ: Vrať Custom Night AI pouze pokud je Custom Night aktivní
        if (!isCustomNightActive)
            return -1; // Signalizuje, že Custom Night není aktivní
        
        return animatronicName.ToUpper() switch
        {
            "ARANI" => araniAI,
            "FENERIR" => fenerirAI,
            "SLEDGE" => sledgeAI,
            "ORAX" => oraxAI,
            "THATCHER" => thatcherAI,
            "DOKKI" => dokkiAI,
            "CAVIRA" => caviraAI,
            "IANID" => ianidAI,
            "ZOFEYE" => zofeyeAI,
            "WARDEN" => wardenAI,
            _ => -1
        };
    }
    
    public bool IsCustomNightActive()
    {
        return isCustomNightActive;
    }
    
    /// <summary>
    /// Public API pro zjištění zda je aktivní DEV mode force spawn obou animatroniků
    /// </summary>
    public bool IsDevModeForceSpawnBothActive()
    {
        return devModeForceSpawnBothAnimatronics;
    }
    
    private void SetupChallengeButtons()
    {
        if (ChallengeLeftButton != null)
        {
            ChallengeLeftButton.onClick.AddListener(ChallengeLeft);
            SetupButtonHover(ChallengeLeftButton); // Přidej hover efekt a zvuk
            Debug.Log("[CHALLENGE] Left button připojeno");
        }
        
        if (ChallengeRightButton != null)
        {
            ChallengeRightButton.onClick.AddListener(ChallengeRight);
            SetupButtonHover(ChallengeRightButton); // Přidej hover efekt a zvuk
            Debug.Log("[CHALLENGE] Right button připojeno");
        }
    }
    
    private void ChallengeLeft()
    {
        // Přehraj zvuk
        if (customNightAudioSource != null && customNightClickSound != null)
        {
            customNightAudioSource.PlayOneShot(customNightClickSound);
        }
        
        // Cykluj doleva (s wraparoundem)
        currentChallengeIndex--;
        if (currentChallengeIndex < 0)
        {
            currentChallengeIndex = 10; // Wrap na CUSTOM (poslední)
        }
        
        Debug.Log($"[CHALLENGE] Left - index: {currentChallengeIndex}, název: {challengeNames[currentChallengeIndex]}");
        
        // Aktualizuj stav 7th Night tlačítka
        UpdateSeventhNightButtonState();
        
        // Aplikuj vybranou challenge (nebo načti CUSTOM preset)
        ApplyChallenge(currentChallengeIndex);
    }
    
    private void ChallengeRight()
    {
        // Přehraj zvuk
        if (customNightAudioSource != null && customNightClickSound != null)
        {
            customNightAudioSource.PlayOneShot(customNightClickSound);
        }
        
        // Cykluj doprava (s wraparoundem)
        currentChallengeIndex++;
        if (currentChallengeIndex > 10)
        {
            currentChallengeIndex = 0; // Wrap na začátek
        }
        
        Debug.Log($"[CHALLENGE] Right - index: {currentChallengeIndex}, název: {challengeNames[currentChallengeIndex]}");
        
        // Aktualizuj stav 7th Night tlačítka
        UpdateSeventhNightButtonState();
        
        // Aplikuj vybranou challenge (nebo načti CUSTOM preset)
        ApplyChallenge(currentChallengeIndex);
    }
    
    private void ApplyChallenge(int challengeIndex)
    {
        isManuallyModified = false; // Reset manual flag - není to ruční změna
        
        if (challengeIndex == 10) // CUSTOM
        {
            // Načti uložený CUSTOM preset
            LoadCustomNightSettings();
            Debug.Log("[CHALLENGE] Načten CUSTOM preset z PlayerPrefs");
        }
        else if (challengeIndex >= 0 && challengeIndex < 10)
        {
            // Aplikuj preset z challenge
            araniAI = challengePresets[challengeIndex, 0];
            fenerirAI = challengePresets[challengeIndex, 1];
            sledgeAI = challengePresets[challengeIndex, 2];
            oraxAI = challengePresets[challengeIndex, 3];
            thatcherAI = challengePresets[challengeIndex, 4];
            dokkiAI = challengePresets[challengeIndex, 5];
            caviraAI = challengePresets[challengeIndex, 6];
            ianidAI = challengePresets[challengeIndex, 7];
            zofeyeAI = challengePresets[challengeIndex, 8];
            wardenAI = challengePresets[challengeIndex, 9];
            
            Debug.Log($"[CHALLENGE] Aplikován preset: {challengeNames[challengeIndex]}");
        }
        
        // Aktualizuj UI
        UpdateAllCustomNightUI();
        
        // Ulož aktuální challenge index
        SaveChallengeIndex();
    }
    
    private void UpdateChallengeText()
    {
        if (ChallengeText != null && currentChallengeIndex >= 0 && currentChallengeIndex < challengeNames.Length)
        {
            ChallengeText.text = challengeNames[currentChallengeIndex];
            
            // Nastav barvu textu podle stavu challenge
            if (currentChallengeIndex == 9) // GOLDEN SIEGE
            {
                // GOLDEN SIEGE - žlutá pokud DOKONČENÁ, tmavě šedá pokud zamčená (neodemčená), bílá pokud odemčená ale nedokončená
                if (isSeventhNightAllMax) // Dokončená (10/20 splněno)
                {
                    ChallengeText.color = completedColor;
                }
                else if (!IsGoldenSiegeUnlocked()) // Zamčená (9 challenges nesplněno)
                {
                    ChallengeText.color = lockedColor;
                }
                else // Odemčená ale nedokončená
                {
                    ChallengeText.color = normalColor;
                }
            }
            else if (currentChallengeIndex >= 0 && currentChallengeIndex < 9)
            {
                // Challenges 1-9 - žlutá pokud dokončená, bílá pokud ne
                ChallengeText.color = challengeCompleted[currentChallengeIndex] ? completedColor : normalColor;
            }
            else // CUSTOM
            {
                ChallengeText.color = normalColor;
            }
            
            Debug.Log($"[CHALLENGE] Text aktualizován: {challengeNames[currentChallengeIndex]}");
        }
    }
    
    private void SaveChallengeIndex()
    {
        PlayerPrefs.SetInt("CustomNight_ChallengeIndex", currentChallengeIndex);
        PlayerPrefs.Save();
        Debug.Log($"[CHALLENGE] Index uložen: {currentChallengeIndex}");
    }
    
    private void LoadChallengeIndex()
    {
        currentChallengeIndex = PlayerPrefs.GetInt("CustomNight_ChallengeIndex", 0);
        
        // Zajisti validní index
        if (currentChallengeIndex < 0 || currentChallengeIndex > 10)
        {
            currentChallengeIndex = 0;
        }
        
        Debug.Log($"[CHALLENGE] Index načten: {currentChallengeIndex}, název: {challengeNames[currentChallengeIndex]}");
        
        // Aplikuj načtenou challenge (bez uložení - již je uložená)
        if (currentChallengeIndex == 10) // CUSTOM
        {
            LoadCustomNightSettings();
        }
        else
        {
            ApplyChallenge(currentChallengeIndex);
        }
    }
    
    #endregion
    
    #region UI Display Updates
    
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
    
    // Nastavení hover efektu pro tlačítko (změna barvy textu a velikosti)
    private void SetupButtonHover(Button button, bool isContinueButton = false, bool isSeventhNightButton = false, Vector3? customOriginalScale = null)
    {
        if (button == null) return;
        
        // Ulož původní scale tlačítka
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector3 originalScale = customOriginalScale ?? rectTransform.localScale;
            originalButtonScales[button] = originalScale;
        }
        
        // Přidej EventTrigger komponent pokud neexistuje
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Přidej hover enter event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnButtonHoverEnter(button, isContinueButton, isSeventhNightButton); });
        trigger.triggers.Add(pointerEnter);
        
        // Přidej hover exit event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnButtonHoverExit(button, isContinueButton, isSeventhNightButton); });
        trigger.triggers.Add(pointerExit);
    }
    
    private void OnButtonHoverEnter(Button button, bool isContinueButton, bool isSeventhNightButton)
    {
        if (button == null) return;
        
        // Pro Continue tlačítko kontroluj, zda je interaktivní
        if (isContinueButton && !button.interactable)
        {
            return; // Neaplikuj hover efekt pokud je Continue neaktivní
        }
        
        // Pro 7th Night tlačítko kontroluj, zda je GOLDEN SIEGE odemčená nebo tlačítko není locked
        if (isSeventhNightButton)
        {
            // Zkontroluj, zda je tlačítko locked (GOLDEN SIEGE challenge nebo CUSTOM 10/20 bez unlock)
            bool isLocked = false;
            
            // GOLDEN SIEGE challenge (index 9)
            if (currentChallengeIndex == 9 && !IsGoldenSiegeUnlocked())
            {
                isLocked = true;
            }
            // CUSTOM mode (index 10) s all AI == 20
            else if (currentChallengeIndex == 10 && !IsGoldenSiegeUnlocked())
            {
                bool allAIMax = (araniAI == 20 && fenerirAI == 20 && sledgeAI == 20 && oraxAI == 20 && 
                                thatcherAI == 20 && dokkiAI == 20 && caviraAI == 20 && ianidAI == 20 && 
                                zofeyeAI == 20 && wardenAI == 20);
                if (allAIMax)
                {
                    isLocked = true;
                }
            }
            
            if (isLocked)
            {
                return; // Neaplikuj hover efekt pokud je 7th Night locked
            }
        }
        
        // Přehraj hover zvuk
        if (hoverAudioSource != null && hoverSound != null)
        {
            hoverAudioSource.PlayOneShot(hoverSound);
        }
        
        // Změna barvy textu na tmavě šedou (#9A9A9A)
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = new Color(0.604f, 0.604f, 0.604f); // #9A9A9A
        }
        
        // Zmenšení tlačítka o 2% z původní scale
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null && originalButtonScales.ContainsKey(button))
        {
            Vector3 originalScale = originalButtonScales[button];
            rectTransform.localScale = new Vector3(originalScale.x * 0.98f, originalScale.y * 0.98f, originalScale.z);
        }
    }
    
    private void OnButtonHoverExit(Button button, bool isContinueButton, bool isSeventhNightButton)
    {
        if (button == null) return;
        
        // Pro Continue tlačítko kontroluj, zda je interaktivní
        if (isContinueButton && !button.interactable)
        {
            return; // Neaplikuj hover efekt pokud je Continue neaktivní
        }
        
        // Pro 7th Night tlačítko kontroluj, zda je GOLDEN SIEGE odemčená nebo tlačítko není locked
        if (isSeventhNightButton)
        {
            // Zkontroluj, zda je tlačítko locked (GOLDEN SIEGE challenge nebo CUSTOM 10/20 bez unlock)
            bool isLocked = false;
            
            // GOLDEN SIEGE challenge (index 9)
            if (currentChallengeIndex == 9 && !IsGoldenSiegeUnlocked())
            {
                isLocked = true;
            }
            // CUSTOM mode (index 10) s all AI == 20
            else if (currentChallengeIndex == 10 && !IsGoldenSiegeUnlocked())
            {
                bool allAIMax = (araniAI == 20 && fenerirAI == 20 && sledgeAI == 20 && oraxAI == 20 && 
                                thatcherAI == 20 && dokkiAI == 20 && caviraAI == 20 && ianidAI == 20 && 
                                zofeyeAI == 20 && wardenAI == 20);
                if (allAIMax)
                {
                    isLocked = true;
                }
            }
            
            if (isLocked)
            {
                return; // Neaplikuj hover efekt pokud je 7th Night locked
            }
        }
        
        // Vrať barvu textu na bílou
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = Color.white;
        }
        
        // Vrať původní velikost tlačítka
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null && originalButtonScales.ContainsKey(button))
        {
            rectTransform.localScale = originalButtonScales[button];
        }
    }
    
    #endregion
    
    #region Game Flow Control
    
    public void StartNewGame()
    {
        Debug.Log("Spouštění nové hry...");
        
        // ODBLOKUJ + SCHOVAT THATCHERA před spuštěním nové hry
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[StartNewGame] Thatcher force unblocked + hidden");
        }
        
        // Reset pouze na první noc (zachovej story completed a odměny)
        currentNight = 1;
        SaveNightProgress();
        
        // Zobraz startovací panel místo přímého spuštění
        StartCoroutine(ShowStartTransition());
    }
    
    public void ContinueGame()
    {
        // ODBLOKUJ + SCHOVAT THATCHERA před pokračováním
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[ContinueGame] Thatcher force unblocked + hidden");
        }
        
        // Zajisti, že Continue nikdy nespustí 6. noc (max je 5)
        int nightToPlay = Mathf.Min(currentNight, 5);
        Debug.Log($"Pokračování ve hře - Noc {nightToPlay} (aktuální currentNight: {currentNight})");
        
        // Nastav aktuální noc na maximálně 5 pro continue
        currentNight = nightToPlay;
        
        // Zobraz transition panel místo přímého spuštění
        StartCoroutine(ShowContinueTransition());
    }
    
    public void StartSixthNight()
    {
        Debug.Log("Spouštění 6. noci...");
        
        // ODBLOKUJ + SCHOVAT THATCHERA před spuštěním 6. noci
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[StartSixthNight] Thatcher force unblocked + hidden");
        }
        
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
        
        // KRITICKÉ: Reset Custom Night flagu pro noci 1-6
        if (nightNumber >= 1 && nightNumber <= 6)
        {
            isCustomNightActive = false;
            Debug.Log($"Custom Night deaktivován - spouští se normální noc {nightNumber} s tabulkovými AI hodnotami");
        }
        else if (nightNumber == 7)
        {
            isCustomNightActive = true;
            Debug.Log("Custom Night aktivován - používají se manuální AI hodnoty z Custom Night panelu");
        }
        
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
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(false);
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
        
        // ODBLOKUJ + SCHOVAT THATCHERA při ESC
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[ESC] Thatcher force unblocked + hidden");
        }
        
        // Zastav všechny herní systémy okamžitě
        isGameActive = false;
        
        // Zastav elektřinu management systém
        if (ElectricityRef != null)
        {
            ElectricityRef.StopGame();
            Debug.Log("ElectricityManagement zastaven při ESC");
        }
        
        // Skryj Phone Guy MUTE tlačítko a zastav všechny Phone Guy procesy
        if (PhoneGuyRef != null)
        {
            PhoneGuyRef.ImmediateStopAllProcesses();
            Debug.Log("Phone Guy všechny procesy zastaveny při ESC");
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
        // VÝJIMKA: Pokud je aktuální noc 6, vrať se na původní noc před 6. nocí
        if (currentNight == 6)
        {
            Debug.Log($"ESC během 6. noci - vracím se na původní noc: {savedNightBeforeSixth}");
            currentNight = savedNightBeforeSixth;
            SaveNightProgress();
        }
        else if (currentNight == 7)
        {
            // ESC během Custom Night - vrať se zpět na původní noc
            Debug.Log($"ESC během Custom Night - vracím se na původní noc: {savedNightBeforeCustom}");
            currentNight = savedNightBeforeCustom;
            isCustomNightActive = false;
            SaveNightProgress();
        }
        
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
        
        // KRITICKÉ: Reset všech animatronik systémů - zastaví všechny útoky
        ResetAllAnimatronicsForNightEnd();
        
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
            // Dokončena 6. noc - odemkni druhou hvězdu a zobraz end panel
            Debug.Log($"6. noc dokončena! Odemykám 2. hvězdu a zobrazuji 6th night end panel");
            isSixthNightCompleted = true; // Odemkni druhou hvězdu
            currentNight = savedNightBeforeSixth; // Vrať noc zpět pro možnost opakování
            SaveNightProgress();
            
            // Vyvolej event
            OnGameEnded?.Invoke();
            
            // Zobraz 6th night end panel místo návratu do menu
            StartCoroutine(ShowSixthNightEndTransition());
        }
        else if (isCustomNightActive)
        {
            // Dokončena Custom Night (7. noc)!
            Debug.Log("========== CUSTOM NIGHT COMPLETED! ==========");
            isSeventhNightCompleted = true;
            
            // Zkontroluj dokončení challenge (pokud nebyla ruční úprava)
            CheckChallengeCompletion();
            
            // Zkontroluj, zda všichni měli AI = 20 pro červenou hvězdu
            bool allMaxAI = (araniAI == 20 && fenerirAI == 20 && sledgeAI == 20 && oraxAI == 20 && 
                            thatcherAI == 20 && dokkiAI == 20 && caviraAI == 20 && ianidAI == 20 && 
                            zofeyeAI == 20 && wardenAI == 20);
            
            if (allMaxAI)
            {
                Debug.Log("🔴 RED STAR UNLOCKED! All animatronics were set to AI 20!");
                isSeventhNightAllMax = true;
            }
            
            // Reset Custom Night flag a vrať noc zpět
            isCustomNightActive = false;
            currentNight = savedNightBeforeCustom; // Vrať noc zpět na hodnotu před Custom Night
            Debug.Log($"Custom Night dokončena - vracím noc zpět na: {currentNight}");
            
            SaveNightProgress();
            
            // Vyvolej event
            OnGameEnded?.Invoke();
            
            // Vrať se do menu
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
    
    private IEnumerator ShowSixthNightEndTransition()
    {
        Debug.Log("Zobrazuji 6th night end sekvenci po 6. noci");
        
        // 1. ČEKEJ na dokončení end videa 6. noci
        Debug.Log("6th night end video se přehrává...");
        yield return new WaitForSeconds(sixthNightEndVideoDelay);
        
        // 2. Skryj video canvas po dokončení end videa
        Debug.Log("6th night end video dokončeno, skrývám video canvas");
        if (VideoCanvas != null)
        {
            VideoCanvas.SetActive(false);
        }
        
        // 3. Skryj všechny panely a připrav 6th night end panel
        HideAllPanels();
        
        // Aktivuj černé pozadí pro skrytí kanceláře (KRITICKÉ - musí být aktivní během celého fade)
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(true);
            Debug.Log("Černé pozadí pro 6th night end panel aktivováno");
        }
        
        if (PanelEndBackground != null)
        {
            PanelEndBackground.SetActive(true);
            Debug.Log("End background pro 6th night panel aktivováno");
        }
        
        if (Panel6thNightEnd != null)
        {
            Panel6thNightEnd.SetActive(true);
            
            // Nastav text přes specifickou referenci
            if (SixthNightEndText != null)
            {
                SixthNightEndText.text = "6TH NIGHT COMPLETED!\nYOU ARE A LEGEND!";
            }
            
            // Nastav alpha na 0 pro fade in
            if (SixthNightEndPanelCanvasGroup != null)
            {
                SixthNightEndPanelCanvasGroup.alpha = 0f;
            }
        }
        
        // 4. Přehraj 6th night ending music přes samostatný AudioSource
        Debug.Log("Spouštím 6th night ending music...");
        try
        {
            if (SixthNightEndingAudioSource != null && SixthNightEndingSound != null)
            {
                SixthNightEndingAudioSource.PlayOneShot(SixthNightEndingSound);
                Debug.Log("6th night ending music úspěšně spuštěna");
            }
            else
            {
                Debug.LogWarning($"6th night ending audio nedostupné - AudioSource: {SixthNightEndingAudioSource != null}, Sound: {SixthNightEndingSound != null}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Chyba při spouštění 6th night ending music: {e.Message}");
        }
        
        // 5. FADE IN 6th night end panel po dobu 5 sekund
        Debug.Log($"Začínám fade in 6th night end panelu ({sixthNightEndFadeInDuration} sekund)");
        yield return StartCoroutine(FadeSixthNightEndPanel(true));
        
        // 6. Zobrazuj 6th night end panel po zbývající dobu (celková doba - fade in - fade out)
        float remainingDisplayTime = sixthNightEndPanelDuration - sixthNightEndFadeInDuration - sixthNightEndFadeOutDuration;
        if (remainingDisplayTime > 0)
        {
            Debug.Log($"6th night end panel zobrazen po dobu {remainingDisplayTime} sekund");
            yield return new WaitForSeconds(remainingDisplayTime);
        }
        
        // 7. FADE OUT 6th night end panel po dobu 5 sekund
        Debug.Log($"Začínám fade out 6th night end panelu ({sixthNightEndFadeOutDuration} sekund)");
        yield return StartCoroutine(FadeSixthNightEndPanel(false));
        
        // 8. Bezpečně zastav 6th night ending music
        Debug.Log("Zastavuji 6th night ending music");
        try
        {
            if (SixthNightEndingAudioSource != null && SixthNightEndingAudioSource.isPlaying)
            {
                SixthNightEndingAudioSource.Stop();
                Debug.Log("6th night ending music zastavena");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Chyba při zastavování 6th night ending music: {e.Message}");
        }
        
        // 9. 1sekundová pauza pouze s černým pozadím
        Debug.Log("1sekundová pauza s černým pozadím před návratem do menu");
        yield return new WaitForSeconds(1f);
        
        // 10. Vrať se do menu
        Debug.Log("6th night end sekvence dokončena, vracím se do main menu");
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
        if (Panel6thNightEnd != null) Panel6thNightEnd.SetActive(false);
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
        
        // ODBLOKUJ + SCHOVAT THATCHERA při nastavování menu state
        ThatcherAI thatcherAIComponent = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAIComponent != null)
        {
            thatcherAIComponent.ForceUnblockThatcher();
            thatcherAIComponent.HideThatcher();
            Debug.Log("[SetMainMenuState] Thatcher force unblocked + hidden - menu is active");
        }
        
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
        if (PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(true);
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
    
    /// <summary>
    /// Reset všech animatronik systémů při konci noci - KRITICKÉ pro zastavení útočných mechanismů
    /// </summary>
    private void ResetAllAnimatronicsForNightEnd()
    {
        Debug.Log("[NightShiftManagement] ===== RESETTING ALL ANIMATRONICS FOR NIGHT END =====");
        
        // Najdi všechny AI scripty ve scéně a volej OnNightEnd() nebo reset metody
        // Thatcher AI
        ThatcherAI thatcherAI = FindFirstObjectByType<ThatcherAI>();
        if (thatcherAI != null)
        {
            thatcherAI.OnNightEnd();
            Debug.Log("[NightShiftManagement] ThatcherAI night end reset called");
        }
        
        // Warden AI
        WardenAI wardenAI = FindFirstObjectByType<WardenAI>();
        if (wardenAI != null)
        {
            wardenAI.OnNightEnd();
            Debug.Log("[NightShiftManagement] WardenAI night end reset called");
        }
        
        // Oryx AI
        OryxAIVerse3 oryxAI = FindFirstObjectByType<OryxAIVerse3>();
        if (oryxAI != null)
        {
            oryxAI.OnNightEnd();
            Debug.Log("[NightShiftManagement] OryxAI night end reset called");
        }
        
        // Zofaye AI
        ZofeyeAI zofayeAI = FindFirstObjectByType<ZofeyeAI>();
        if (zofayeAI != null)
        {
            zofayeAI.ResetZofaye();
            Debug.Log("[NightShiftManagement] ZofayeAI night end reset called");
        }
        
        // Sledge AI
        SledgeAI sledgeAI = FindFirstObjectByType<SledgeAI>();
        if (sledgeAI != null)
        {
            sledgeAI.OnNewNightStarted();
            Debug.Log("[NightShiftManagement] SledgeAI night end reset called");
        }
        
        // Fenerir AI
        FenerirAI fenerirAI = FindFirstObjectByType<FenerirAI>();
        if (fenerirAI != null)
        {
            fenerirAI.DeactivateFenerir();
            Debug.Log("[NightShiftManagement] FenerirAI night end reset called");
        }
        
        // Dokki AI
        DokkiAI dokkiAI = FindFirstObjectByType<DokkiAI>();
        if (dokkiAI != null)
        {
            dokkiAI.TriggerUniversalReset("Night End");
            Debug.Log("[NightShiftManagement] DokkiAI night end reset called");
        }
        
        // Ianid AI
        IanidAI ianidAI = FindFirstObjectByType<IanidAI>();
        if (ianidAI != null)
        {
            ianidAI.OnNewNightStarted();
            Debug.Log("[NightShiftManagement] IanidAI night end reset called");
        }
        
        // Cavira AI
        CaviraAI caviraAI = FindFirstObjectByType<CaviraAI>();
        if (caviraAI != null)
        {
            caviraAI.ResetCavira();
            Debug.Log("[NightShiftManagement] CaviraAI night end reset called");
        }
        
        // Arani AI
        AraniAI araniAI = FindFirstObjectByType<AraniAI>();
        if (araniAI != null)
        {
            araniAI.ResetDestroyedCameras("Night End");
            Debug.Log("[NightShiftManagement] AraniAI night end reset called");
        }
        
        Debug.Log("[NightShiftManagement] All animatronics reset for night end complete");
    }
    
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
        
        // Reset DokkiAI pro novou noc
        DokkiAI dokkiAI = FindFirstObjectByType<DokkiAI>();
        if (dokkiAI != null)
        {
            dokkiAI.TriggerUniversalReset("New Night Start");
            Debug.Log("DokkiAI resetována pro novou noc");
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
            ElectricityRef = FindFirstObjectByType<ElectricityManagement>();
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
        PlayerPrefs.SetInt("SixthNightCompleted", isSixthNightCompleted ? 1 : 0);
        PlayerPrefs.SetInt("SeventhNightCompleted", isSeventhNightCompleted ? 1 : 0);
        PlayerPrefs.SetInt("SeventhNightAllMax", isSeventhNightAllMax ? 1 : 0);
        PlayerPrefs.SetInt("SavedNightBeforeSixth", savedNightBeforeSixth);
        PlayerPrefs.SetInt("SavedNightBeforeCustom", savedNightBeforeCustom);
        PlayerPrefs.Save();
        Debug.Log($"Progress uložen - Aktuální noc: {currentNight}, Story dokončena: {isStoryCompleted}, 6. noc dokončena: {isSixthNightCompleted}, 7. noc dokončena: {isSeventhNightCompleted}, Červená hvězda: {isSeventhNightAllMax}");
    }
    
    private void LoadNightProgress()
    {
        currentNight = PlayerPrefs.GetInt("CurrentNight", 1); // Default je 1. noc
        isStoryCompleted = PlayerPrefs.GetInt("StoryCompleted", 0) == 1; // Default je false
        isSixthNightCompleted = PlayerPrefs.GetInt("SixthNightCompleted", 0) == 1; // Default je false
        isSeventhNightCompleted = PlayerPrefs.GetInt("SeventhNightCompleted", 0) == 1; // Default je false
        isSeventhNightAllMax = PlayerPrefs.GetInt("SeventhNightAllMax", 0) == 1; // Default je false
        savedNightBeforeSixth = PlayerPrefs.GetInt("SavedNightBeforeSixth", 1); // Default je 1. noc
        savedNightBeforeCustom = PlayerPrefs.GetInt("SavedNightBeforeCustom", 1); // Default je 1. noc
        Debug.Log($"Progress načten - Aktuální noc: {currentNight}, Story dokončena: {isStoryCompleted}, 6. noc dokončena: {isSixthNightCompleted}, 7. noc dokončena: {isSeventhNightCompleted}, Červená hvězda: {isSeventhNightAllMax}");
        
        // Zajisti, aby noc byla v povoleném rozsahu
        currentNight = Mathf.Clamp(currentNight, 1, maxNights);
    }
    
    #endregion
    
    #region Public Methods
    
    // Metody pro externí přístup k informacím
    public int GetCurrentNight() => currentNight;
    public bool IsGameActive() => isGameActive;
    public bool IsInMainMenu()
    {
        // Vrať true pokud je hlavní menu aktivní NEBO pokud je custom night panel aktivní
        bool mainMenuActive = isInMainMenu || (MainMenuPanel != null && MainMenuPanel.activeSelf);
        bool customNightPanelActive = (CustomNightPanel != null && CustomNightPanel.activeSelf);
        
        return mainMenuActive || customNightPanelActive;
    }
    
    // Metoda pro manuální návrat do menu (pro debug a smrt animatronika)
    public void ForceReturnToMenu()
    {
        // Reset baterky před návratem do menu (zastaví všechny efekty)
        if (FlashlightRef != null)
        {
            FlashlightRef.ImmediateStopFlickering();
            Debug.Log("[NightShiftManagement] Flashlight reset before returning to menu after death");
        }
        
        ReturnToMainMenu();
    }
    
    // Metoda pro kompletní reset progressu
    public void ResetCompleteProgress()
    {
        Debug.Log("=== KOMPLETNÍ RESET PROGRESSU ===");
        
        // Reset všech progress proměnných
        currentNight = 1;
        isStoryCompleted = false;
        isSixthNightCompleted = false;
        isSeventhNightCompleted = false;
        isSeventhNightAllMax = false;
        isCustomNightActive = false;
        savedNightBeforeSixth = 1;
        savedNightBeforeCustom = 1;
        
        // Reset všech challenge completion flags
        for (int i = 0; i < 9; i++)
        {
            challengeCompleted[i] = false;
        }
        
        // Reset všech dev mode challenge checkboxů
        devModeChallenge1Completed = false;
        devModeChallenge2Completed = false;
        devModeChallenge3Completed = false;
        devModeChallenge4Completed = false;
        devModeChallenge5Completed = false;
        devModeChallenge6Completed = false;
        devModeChallenge7Completed = false;
        devModeChallenge8Completed = false;
        devModeChallenge9Completed = false;
        
        // Vymaž všechny uložené data
        PlayerPrefs.DeleteKey("CurrentNight");
        PlayerPrefs.DeleteKey("StoryCompleted");
        PlayerPrefs.DeleteKey("SixthNightCompleted");
        PlayerPrefs.DeleteKey("SeventhNightCompleted");
        PlayerPrefs.DeleteKey("SeventhNightAllMax");
        PlayerPrefs.DeleteKey("SavedNightBeforeSixth");
        PlayerPrefs.DeleteKey("SavedNightBeforeCustom");
        
        // Vymaž všechny challenge completion flagy
        for (int i = 0; i < 9; i++)
        {
            PlayerPrefs.DeleteKey($"Challenge_{i}_Completed");
        }
        
        // Resetuj Custom Night AI hodnoty (ale neukladej je - hráč si je nastaví znovu)
        araniAI = 0;
        fenerirAI = 0;
        sledgeAI = 0;
        oraxAI = 0;
        thatcherAI = 0;
        dokkiAI = 0;
        caviraAI = 0;
        ianidAI = 0;
        zofeyeAI = 0;
        wardenAI = 0;
        
        PlayerPrefs.Save();
        
        // Aktualizuj UI pro skrytí všech odměn
        UpdateUI();
        UpdateAllCustomNightUI();
        UpdateChallengeStarsVisibility();
        
        Debug.Log("Progress kompletně resetován na výchozí stav (včetně challenges a dev mode checkboxů).");
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
    
    private IEnumerator ShowCustomNightTransition()
    {
        Debug.Log("Zobrazuji transition panel pro 7. noc (Custom Night)");
        
        // Zastav menu hudbu
        StopMenuMusic();
        
        // Skryj všechny panely
        HideAllPanels();
        
        // Zobraz přechodový panel s textem 7. noci
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
                TransitionNightText.text = "NIGHT 7";
            }
        }
        
        // Přehraj zvuk přechodu
        if (TransitionAudioSource != null && NightTransitionSound != null)
        {
            TransitionAudioSource.PlayOneShot(NightTransitionSound);
        }
        
        // Čekej na zobrazení transition panelu (5 sekund)
        yield return new WaitForSeconds(transitionPanelDuration);
        
        // Spusť 7. noc s Custom Night AI
        StartGameNight(7);
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
        if (!fadeIn && PanelBlackPozadi != null)
        {
            PanelBlackPozadi.SetActive(false);
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
    
    private IEnumerator FadeSixthNightEndPanel(bool fadeIn)
    {
        if (SixthNightEndPanelCanvasGroup == null) 
        {
            Debug.LogWarning("SixthNightEndPanelCanvasGroup není přiřazena - fade nebude fungovat");
            yield break;
        }
        
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float fadeDuration = fadeIn ? sixthNightEndFadeInDuration : sixthNightEndFadeOutDuration;
        float elapsedTime = 0f;
        
        Debug.Log($"Fade 6th night end panel - směr: {(fadeIn ? "IN" : "OUT")}, doba: {fadeDuration}s");
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            SixthNightEndPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }
        
        SixthNightEndPanelCanvasGroup.alpha = endAlpha;
        Debug.Log($"Fade 6th night end panel dokončen - konečná alpha: {endAlpha}");
    }
    
    #endregion
    
    #region Challenge Reward System
    
    // Zkontroluje, zda je GOLDEN SIEGE (10/20) odemčená (všech 9 challenges dokončeno)
    private bool IsGoldenSiegeUnlocked()
    {
        // Dev mód bypass
        if (devModeUnlockGoldenSiege)
            return true;
        
        for (int i = 0; i < 9; i++)
        {
            if (!challengeCompleted[i])
            {
                return false;
            }
        }
        return true;
    }
    
    // Zkontroluje, zda aktuální AI odpovídá challenge presetu a označí ji jako dokončenou
    private void CheckChallengeCompletion()
    {
        // Kontroluj pouze pokud je vybraná challenge 0-8 (ne CUSTOM ani GOLDEN SIEGE)
        if (currentChallengeIndex >= 0 && currentChallengeIndex < 9 && !isManuallyModified)
        {
            // Ověř, že AI odpovídá presetu
            bool matches = true;
            
            int[] currentAI = { araniAI, fenerirAI, sledgeAI, oraxAI, thatcherAI, dokkiAI, caviraAI, ianidAI, zofeyeAI, wardenAI };
            
            for (int i = 0; i < 10; i++)
            {
                if (currentAI[i] != challengePresets[currentChallengeIndex, i])
                {
                    matches = false;
                    break;
                }
            }
            
            if (matches)
            {
                if (!challengeCompleted[currentChallengeIndex])
                {
                    challengeCompleted[currentChallengeIndex] = true;
                    SaveChallengeCompletionFlags();
                    Debug.Log($"⭐ CHALLENGE COMPLETED: {challengeNames[currentChallengeIndex]}");
                    
                    // Zkontroluj, zda se odemkla GOLDEN SIEGE
                    if (IsGoldenSiegeUnlocked())
                    {
                        Debug.Log("🏆 GOLDEN SIEGE UNLOCKED! All 9 challenges completed!");
                    }
                }
            }
        }
        else if (currentChallengeIndex == 9 && !isManuallyModified)
        {
            // GOLDEN SIEGE dokončení (není třeba ukládat jako challenge completion)
            Debug.Log("🏆 GOLDEN SIEGE COMPLETED!");
        }
    }
    
    // Uloží flagy dokončených challenges do PlayerPrefs
    private void SaveChallengeCompletionFlags()
    {
        for (int i = 0; i < 9; i++)
        {
            PlayerPrefs.SetInt($"Challenge_{i}_Completed", challengeCompleted[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
        Debug.Log("[CHALLENGE] Completion flags uloženy");
    }
    
    // Načte flagy dokončených challenges z PlayerPrefs
    private void LoadChallengeCompletionFlags()
    {
        for (int i = 0; i < 9; i++)
        {
            challengeCompleted[i] = PlayerPrefs.GetInt($"Challenge_{i}_Completed", 0) == 1;
        }
        Debug.Log("[CHALLENGE] Completion flags načteny");
        
        // Aplikuj dev mode flags po načtení
        ApplyDevModeChallengeFlags();
    }
    
    // Aplikuje dev mode challenge completion flags (přepíše challengeCompleted array)
    private void ApplyDevModeChallengeFlags()
    {
        // Nastav flags podle dev mode checkboxů (true i false)
        if (devModeChallenge1Completed) challengeCompleted[0] = true; else challengeCompleted[0] = PlayerPrefs.GetInt("Challenge_0_Completed", 0) == 1;
        if (devModeChallenge2Completed) challengeCompleted[1] = true; else challengeCompleted[1] = PlayerPrefs.GetInt("Challenge_1_Completed", 0) == 1;
        if (devModeChallenge3Completed) challengeCompleted[2] = true; else challengeCompleted[2] = PlayerPrefs.GetInt("Challenge_2_Completed", 0) == 1;
        if (devModeChallenge4Completed) challengeCompleted[3] = true; else challengeCompleted[3] = PlayerPrefs.GetInt("Challenge_3_Completed", 0) == 1;
        if (devModeChallenge5Completed) challengeCompleted[4] = true; else challengeCompleted[4] = PlayerPrefs.GetInt("Challenge_4_Completed", 0) == 1;
        if (devModeChallenge6Completed) challengeCompleted[5] = true; else challengeCompleted[5] = PlayerPrefs.GetInt("Challenge_5_Completed", 0) == 1;
        if (devModeChallenge7Completed) challengeCompleted[6] = true; else challengeCompleted[6] = PlayerPrefs.GetInt("Challenge_6_Completed", 0) == 1;
        if (devModeChallenge8Completed) challengeCompleted[7] = true; else challengeCompleted[7] = PlayerPrefs.GetInt("Challenge_7_Completed", 0) == 1;
        if (devModeChallenge9Completed) challengeCompleted[8] = true; else challengeCompleted[8] = PlayerPrefs.GetInt("Challenge_8_Completed", 0) == 1;
    }
    
    // Aktualizuje viditelnost 9 challenge hvězd v main menu
    private void UpdateChallengeStarsVisibility()
    {
        bool shouldShow = (MainMenuPanel != null && MainMenuPanel.activeSelf);
        
        GameObject[] stars = { ChallengeStar1, ChallengeStar2, ChallengeStar3, ChallengeStar4, ChallengeStar5, 
                               ChallengeStar6, ChallengeStar7, ChallengeStar8, ChallengeStar9 };
        
        for (int i = 0; i < 9; i++)
        {
            if (stars[i] != null)
            {
                stars[i].SetActive(shouldShow && challengeCompleted[i]);
            }
        }
    }
    
    // Aktualizuje stav 7th Night tlačítka (zamče pokud je vybrán zamčený GOLDEN SIEGE nebo manuální 10/20)
    private void UpdateSeventhNightButtonState()
    {
        if (SeventhNightStartButton != null)
        {
            bool isLocked = false;
            
            // Zamkni pokud je vybraný GOLDEN SIEGE (index 9) a není odemčený
            if (currentChallengeIndex == 9 && !IsGoldenSiegeUnlocked())
            {
                isLocked = true;
            }
            // Zamkni pokud je CUSTOM (index 10) a všech 10 AI je na 20, ale není odemčený GOLDEN SIEGE
            else if (currentChallengeIndex == 10 && !IsGoldenSiegeUnlocked())
            {
                // Zkontroluj, zda jsou všichni animatronici na AI 20
                bool isAll20 = (araniAI == 20 && fenerirAI == 20 && sledgeAI == 20 && oraxAI == 20 && 
                               thatcherAI == 20 && dokkiAI == 20 && caviraAI == 20 && ianidAI == 20 && 
                               zofeyeAI == 20 && wardenAI == 20);
                
                if (isAll20)
                {
                    isLocked = true;
                    Debug.Log("[NightShift] 10/20 CUSTOM mode blocked - GOLDEN SIEGE not unlocked yet");
                }
            }
            
            SeventhNightStartButton.interactable = !isLocked;
            
            // Změň barvu textu tlačítka - tmavě šedá pokud zamčené, bílá pokud odemčené
            TextMeshProUGUI buttonText = SeventhNightStartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = isLocked ? lockedColor : Color.white;
            }
        }
    }
    
    #endregion
}
