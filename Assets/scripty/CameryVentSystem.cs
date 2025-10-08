using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameryVentSystem : MonoBehaviour
{
    [SerializeField] private GameObject[] VentCameras;
    [SerializeField] private int CurrentVentCam;
    [SerializeField] private KeyCode OpenVentCameras;
    [SerializeField] private bool VentCamerasOpen;

    [SerializeField] private GameObject MainCamera;
    [SerializeField] private GameObject VentCameraSystemUI;
    [SerializeField] private GameObject VentMapImage;  // Reference na image s mapou

    [Header("UI Buttons")]
    [SerializeField] private GameObject OpenVentCamerasButton;  // Tlačítko pro otevření ventilačních kamer
    [SerializeField] private GameObject CloseVentCamerasButton; // Tlačítko pro zavření ventilačních kamer
    [SerializeField] private GameObject RoomTransferButton; // Tlačítko pro přesun do jiné místnosti
    [SerializeField] private GameObject SwitchVentModeButton; // Tlačítko pro přepnutí zpět na klasické kamery

    [Header("Rotation Settings")]
    [SerializeField] private float minRotationY = -10f;  // Minimální rotace Y pro zobrazení tlačítka
    [SerializeField] private float maxRotationY = 10f;   // Maximální rotace Y pro zobrazení tlačítka

    [Header("Vent Camera Map Buttons")]
    [SerializeField] private Button[] ventCameraMapButtons; // Tlačítka na mapě pro výběr ventilačních kamer
    [SerializeField] private float blinkInterval = 1f; // Interval blikání v sekundách
    [SerializeField] private Color highlightColor = Color.yellow; // Barva zvýraznění
    [SerializeField] private Color normalColor = Color.white; // Normální barva

    [Header("Audio")]
    [SerializeField] private AudioSource ventCameraSwitchAudioSource; // AudioSource pro zvuk přepínání ventilačních kamer
    [SerializeField] private AudioClip ventCameraSwitchSound; // Zvuk při přepnutí ventilační kamery
    [SerializeField] private AudioClip switchToClassicSound; // Zvuk při přepnutí na klasické kamery
    [SerializeField] private float currentVentAudioVolume = 0f; // Aktuální hlasitost (pro náhled v inspektoru)

    [SerializeField] private float CoolDownTime = 0.5f;
    [SerializeField] private float CoolDownTimer;

    private bool originalRoomTransferButtonStateVent; // Uložení původního stavu tlačítka
    
    [Header("Camera System Reference")]
    [SerializeField] private CameraSystem mainCameraSystem; // Reference na hlavní kamerový systém
    [SerializeField] private VentilaceLaseryDvere laserDoorsSystem; // Reference na laser doors systém
    [SerializeField] private ElectricityManagement electricityManagement; // Reference na elektřinu management
    
    private Coroutine currentVentBlinkCoroutine; // Reference na aktuální blikání ventilačních kamer

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < VentCameras.Length; i++)
        {
            VentCameras[i].SetActive(false);
        }
        VentCameraSystemUI.SetActive(false);
        VentMapImage.SetActive(false);  // Skryj mapu na začátku
        CloseVentCamerasButton.SetActive(false); // Skryj tlačítko pro zavření na začátku
        
        // Ulož původní stav tlačítka pro přesun místnosti
        if (RoomTransferButton != null)
        {
            originalRoomTransferButtonStateVent = RoomTransferButton.activeSelf;
        }
        
        // Najdi laser doors systém pokud není přiřazen
        if (laserDoorsSystem == null)
        {
            laserDoorsSystem = FindObjectOfType<VentilaceLaseryDvere>();
            if (laserDoorsSystem != null)
            {
                Debug.Log("CameryVentSystem: VentilaceLaseryDvere automaticky nalezen");
            }
        }
        
        // Nastav hlasitost AudioSource na 0% na začátku
        if (ventCameraSwitchAudioSource != null)
        {
            ventCameraSwitchAudioSource.volume = 0f;
            currentVentAudioVolume = 0f;
        }
        
        MainCamera.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        // Kontrola rotace pro zobrazení tlačítka "Otevřít ventilační kamery"
        if (!VentCamerasOpen)
        {
            CheckRotationForButton();
        }

        if (Input.GetKeyDown(OpenVentCameras))
        {
            // Při prvním otevření ventilačních kamer nastav hlasitost na 100%
            if (ventCameraSwitchAudioSource != null && currentVentAudioVolume == 0f && !VentCamerasOpen)
            {
                ventCameraSwitchAudioSource.volume = 1f;
                currentVentAudioVolume = 1f;
            }
            
            VentCamerasOpen = !VentCamerasOpen;
            ShowVentCamera();
            
            // Informuj ElectricityManagement o změně stavu kamer
            if (electricityManagement == null)
            {
                electricityManagement = FindObjectOfType<ElectricityManagement>();
            }
            if (electricityManagement != null)
            {
                electricityManagement.OnCameraStateChanged();
            }
        }

        if (CoolDownTimer <= 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                if (VentCamerasOpen)
                {
                    VentCameras[CurrentVentCam].SetActive(false);
                    CurrentVentCam = CurrentVentCam + 1;
                    if (CurrentVentCam >= VentCameras.Length)
                    {
                        CurrentVentCam = 0;
                    }
                    GoToVentCamera(CurrentVentCam);
                    CoolDownTimer = CoolDownTime;
                    
                    // Informuj laser doors systém o změně kamery
                    if (laserDoorsSystem != null)
                    {
                        laserDoorsSystem.OnVentCameraChanged();
                    }
                }
            }
            else if (Input.GetAxis("Horizontal") < 0)
            {
                if (VentCamerasOpen)
                {
                    VentCameras[CurrentVentCam].SetActive(false);
                    CurrentVentCam = CurrentVentCam - 1;
                    if (CurrentVentCam < 0)
                    {
                        CurrentVentCam = VentCameras.Length - 1;
                    }
                    GoToVentCamera(CurrentVentCam);
                    CoolDownTimer = CoolDownTime;
                    
                    // Informuj laser doors systém o změně kamery
                    if (laserDoorsSystem != null)
                    {
                        laserDoorsSystem.OnVentCameraChanged();
                    }
                }
            }
        }
        else
        {
            CoolDownTimer -= Time.deltaTime;
        }
    }
    
    private void ShowVentCamera()
    {
        if (VentCamerasOpen)
        {
            VentCameras[CurrentVentCam].SetActive(true);
            VentCameraSystemUI.SetActive(true);
            VentMapImage.SetActive(true);  // Zobraz mapu když jsou ventilační kamery otevřené
            OpenVentCamerasButton.SetActive(false);  // Skryj tlačítko "Otevřít"
            CloseVentCamerasButton.SetActive(true);   // Zobraz tlačítko "Zavřít"
            
            // Zobraz tlačítko pro přepnutí zpět na klasické kamery
            if (SwitchVentModeButton != null)
            {
                SwitchVentModeButton.SetActive(true);
            }
            
            // Skryj tlačítko pro přesun místnosti když jsou ventilační kamery otevřené
            if (RoomTransferButton != null)
            {
                RoomTransferButton.SetActive(false);
            }
            
            // Skryj tlačítko pro otevření hlavních kamer
            if (mainCameraSystem != null)
            {
                mainCameraSystem.HideOpenCamerasButton();
            }
            
            MainCamera.SetActive(false);
        }
        else
        {
            VentCameras[CurrentVentCam].SetActive(false);
            VentCameraSystemUI.SetActive(false);
            VentMapImage.SetActive(false);  // Skryj mapu když jsou ventilační kamery zavřené
            OpenVentCamerasButton.SetActive(false);   // Bude se řídit rotací v Update()
            CloseVentCamerasButton.SetActive(false);  // Skryj tlačítko "Zavřít"
            
            // Skryj tlačítko pro přepnutí zpět na klasické kamery
            if (SwitchVentModeButton != null)
            {
                SwitchVentModeButton.SetActive(false);
            }
            
            // Obnov původní stav tlačítka pro přesun místnosti
            if (RoomTransferButton != null)
            {
                RoomTransferButton.SetActive(originalRoomTransferButtonStateVent);
            }
            
            MainCamera.SetActive(true);
            
            // Zastavit blikání pokud jsou ventilační kamery zavřené
            if (currentVentBlinkCoroutine != null)
            {
                StopCoroutine(currentVentBlinkCoroutine);
                currentVentBlinkCoroutine = null;
            }
        }
        
        
        UpdateVentCameraButtonHighlight();
        
        // Informuj laser doors systém o změně stavu
        if (laserDoorsSystem != null)
        {
            laserDoorsSystem.OnVentCameraChanged();
        }
    }
    
    public void GoToVentCamera(int Progression)
    {
        if (VentCamerasOpen)
        {
            VentCameras[CurrentVentCam].SetActive(false);
            CurrentVentCam = Progression;
            VentCameras[CurrentVentCam].SetActive(true);
            UpdateVentCameraButtonHighlight();
            PlayVentCameraSwitchSound();
            
            // Informuj laser doors systém o změně kamery
            if (laserDoorsSystem != null)
            {
                laserDoorsSystem.OnVentCameraChanged();
            }
        }
    }

    // Kontrola rotace pro zobrazení tlačítka "Otevřít ventilační kamery"
    private void CheckRotationForButton()
    {
        // Získej rotaci Main Camera
        float currentRotationY = MainCamera.transform.eulerAngles.y;
        
        // Převeď na rozsah -180 až 180
        if (currentRotationY > 180f)
            currentRotationY -= 360f;

        // Zkontroluj, zda je rotace v povoleném rozsahu
        bool isInRange = currentRotationY >= minRotationY && currentRotationY <= maxRotationY;
        
        // Zkontroluj, zda nejsou otevřené hlavní kamery
        bool mainCamerasOpen = false;
        if (mainCameraSystem != null)
        {
            mainCamerasOpen = mainCameraSystem.AreCamerasOpen();
        }
        
        // Zobraz/skryj tlačítko podle rotace a stavu hlavních kamer
        if (OpenVentCamerasButton != null)
        {
            OpenVentCamerasButton.SetActive(isInRange && !mainCamerasOpen);
        }
    }

    // Metoda pro kliknutí na tlačítko "Otevřít ventilační kamery"
    public void OnOpenVentCamerasButtonClick()
    {
        // Při prvním otevření ventilačních kamer nastav hlasitost na 100%
        if (ventCameraSwitchAudioSource != null && currentVentAudioVolume == 0f)
        {
            ventCameraSwitchAudioSource.volume = 1f;
            currentVentAudioVolume = 1f;
        }
        
        VentCamerasOpen = true;
        ShowVentCamera();
        
        // Informuj ElectricityManagement o změně stavu kamer
        if (electricityManagement == null)
        {
            electricityManagement = FindObjectOfType<ElectricityManagement>();
        }
        if (electricityManagement != null)
        {
            electricityManagement.OnCameraStateChanged();
        }
    }

    // Metoda pro kliknutí na tlačítko "Zavřít ventilační kamery"  
    public void OnCloseVentCamerasButtonClick()
    {
        VentCamerasOpen = false;
        ShowVentCamera();
        
        // Informuj ElectricityManagement o změně stavu kamer
        if (electricityManagement == null)
        {
            electricityManagement = FindObjectOfType<ElectricityManagement>();
        }
        if (electricityManagement != null)
        {
            electricityManagement.OnCameraStateChanged();
        }
        
        // Informuj laser doors systém o zavření ventilačních kamer
        if (laserDoorsSystem != null)
        {
            laserDoorsSystem.OnVentCameraChanged();
        }
    }

    // Metoda volaná z CameraSystem pro zavření ventilačních kamer při zavření hlavních kamer
    public void ForceCloseVentCameras()
    {
        if (VentCamerasOpen)
        {
            VentCamerasOpen = false;
            ShowVentCamera();
            
            // Informuj ElectricityManagement o změně stavu kamer
            if (electricityManagement == null)
            {
                electricityManagement = FindObjectOfType<ElectricityManagement>();
            }
            if (electricityManagement != null)
            {
                electricityManagement.OnCameraStateChanged();
            }
            
            // Informuj laser doors systém o zavření ventilačních kamer
            if (laserDoorsSystem != null)
            {
                laserDoorsSystem.OnVentCameraChanged();
            }
        }
    }

    // Metoda pro zjištění stavu ventilačních kamer
    public bool AreVentCamerasOpen()
    {
        return VentCamerasOpen;
    }
    
    // Metoda pro získání aktuální ventilační kamery
    public int GetCurrentVentCam()
    {
        return CurrentVentCam;
    }
    
    // METODY PRO BLIKÁNÍ TLAČÍTEK NA MAPĚ VENTILAČNÍCH KAMER
    
    // Aktualizuj zvýraznění tlačítek podle aktuální ventilační kamery
    private void UpdateVentCameraButtonHighlight()
    {
        if (ventCameraMapButtons == null || ventCameraMapButtons.Length == 0) return;
        
        // Zastavit předchozí blikání
        if (currentVentBlinkCoroutine != null)
        {
            StopCoroutine(currentVentBlinkCoroutine);
            currentVentBlinkCoroutine = null;
        }
        
        // Resetovat všechna tlačítka na normální barvu
        for (int i = 0; i < ventCameraMapButtons.Length; i++)
        {
            if (ventCameraMapButtons[i] != null)
            {
                Image buttonImage = ventCameraMapButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = normalColor;
                }
            }
        }
        
        // Spustit blikání pro aktuální ventilační kameru pokud jsou kamery otevřené
        if (VentCamerasOpen && CurrentVentCam >= 0 && CurrentVentCam < ventCameraMapButtons.Length)
        {
            if (ventCameraMapButtons[CurrentVentCam] != null)
            {
                currentVentBlinkCoroutine = StartCoroutine(BlinkVentCameraButton(CurrentVentCam));
            }
        }
    }
    
    // Coroutine pro blikání tlačítka ventilační kamery
    private IEnumerator BlinkVentCameraButton(int cameraIndex)
    {
        if (ventCameraMapButtons == null || cameraIndex < 0 || cameraIndex >= ventCameraMapButtons.Length)
            yield break;
            
        Button button = ventCameraMapButtons[cameraIndex];
        if (button == null) yield break;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) yield break;
        
        while (VentCamerasOpen && CurrentVentCam == cameraIndex)
        {
            // Změň na zvýrazněnou barvu
            buttonImage.color = highlightColor;
            yield return new WaitForSeconds(blinkInterval);
            
            // Změň na normální barvu
            buttonImage.color = normalColor;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    
    // Metoda pro výběr ventilační kamery z mapy (připojit na OnClick tlačítek)
    public void SelectVentCameraFromMap(int cameraIndex)
    {
        if (VentCamerasOpen && cameraIndex >= 0 && cameraIndex < VentCameras.Length)
        {
            GoToVentCamera(cameraIndex);
            PlayVentCameraSwitchSound();
            
            // Informuj laser doors systém o změně kamery
            if (laserDoorsSystem != null)
            {
                laserDoorsSystem.OnVentCameraChanged();
            }
        }
    }
    
    // METODY PRO PŘEPÍNÁNÍ MEZI KAMERAMI
    
    // Metoda pro kliknutí na tlačítko přepnutí zpět na klasické kamery
    public void OnSwitchVentModeClick()
    {
        // Funkce pouze pokud jsou otevřené ventilační kamery
        if (VentCamerasOpen)
        {
            SwitchToClassicCameras();
        }
    }
    
    // Přepnutí zpět na klasické kamery
    public void SwitchToClassicCameras()
    {
        if (mainCameraSystem != null && VentCamerasOpen)
        {
            // Přehraj zvuk přepnutí na klasické kamery
            PlaySwitchToClassicSound();
            
            // Zavři ventilační kamery
            VentCamerasOpen = false;
            
            // Informuj ElectricityManagement o změně stavu kamer
            if (electricityManagement == null)
            {
                electricityManagement = FindObjectOfType<ElectricityManagement>();
            }
            if (electricityManagement != null)
            {
                electricityManagement.OnCameraStateChanged();
            }
            
            // Skryj ventilační UI
            VentCameras[CurrentVentCam].SetActive(false);
            VentCameraSystemUI.SetActive(false);
            VentMapImage.SetActive(false);
            CloseVentCamerasButton.SetActive(false);
            if (SwitchVentModeButton != null)
            {
                SwitchVentModeButton.SetActive(false);
            }
            
            // Zastavit blikání ventilačních kamer
            if (currentVentBlinkCoroutine != null)
            {
                StopCoroutine(currentVentBlinkCoroutine);
                currentVentBlinkCoroutine = null;
            }
            
            // Obnov původní stav tlačítka pro přesun místnosti
            if (RoomTransferButton != null)
            {
                RoomTransferButton.SetActive(originalRoomTransferButtonStateVent);
            }
            
            // Informuj laser doors systém o zavření ventilačních kamer
            if (laserDoorsSystem != null)
            {
                laserDoorsSystem.OnVentCameraChanged();
            }
            
            // Otevři klasické kamery
            mainCameraSystem.OpenCamerasFromSwitch();
        }
    }
    
    // Metoda pro otevření ventilačních kamer z přepínače (volaná z CameraSystem)
    public void OpenVentCamerasFromSwitch()
    {
        VentCamerasOpen = true;
        ShowVentCamera();
        
        // Informuj ElectricityManagement o změně stavu kamer
        if (electricityManagement == null)
        {
            electricityManagement = FindObjectOfType<ElectricityManagement>();
        }
        if (electricityManagement != null)
        {
            electricityManagement.OnCameraStateChanged();
        }
        
        // Informuj laser doors systém o otevření ventilačních kamer
        if (laserDoorsSystem != null)
        {
            laserDoorsSystem.OnVentCameraChanged();
        }
    }
    
    // METODY PRO AUDIO
    
    // Přehrání zvuku při přepnutí ventilační kamery
    private void PlayVentCameraSwitchSound()
    {
        if (ventCameraSwitchAudioSource != null && ventCameraSwitchSound != null)
        {
            ventCameraSwitchAudioSource.PlayOneShot(ventCameraSwitchSound);
        }
    }
    
    // Přehrání zvuku při přepnutí na klasické kamery
    private void PlaySwitchToClassicSound()
    {
        if (ventCameraSwitchAudioSource != null && switchToClassicSound != null)
        {
            ventCameraSwitchAudioSource.PlayOneShot(switchToClassicSound);
        }
    }
    
    // Metoda pro tichý reset ventilačních kamer (bez zvuků)
    public void SilentResetVentCameras()
    {
        // Zastav laser loop audio před resetem
        if (laserDoorsSystem != null)
        {
            laserDoorsSystem.SilentResetSystem();
        }
        
        // Okamžitě zastav všechny ventilační camera audio efekty
        if (ventCameraSwitchAudioSource != null && ventCameraSwitchAudioSource.isPlaying)
        {
            ventCameraSwitchAudioSource.Stop();
        }
        
        // Vyvolej zavření ventilačních kamer (bez zvuků)
        if (VentCamerasOpen)
        {
            OnCloseVentCamerasButtonClick();
            Debug.Log("Hráč slez z ventilačních kamer při resetu");
        }
        
        Debug.Log("Ventilační kamery tiše resetovány");
    }
}
