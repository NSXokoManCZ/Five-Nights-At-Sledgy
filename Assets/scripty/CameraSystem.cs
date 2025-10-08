using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private GameObject[] Cameras;
    [SerializeField] private int CurrentCam;
    [SerializeField] private KeyCode OpenCameras;
    [SerializeField] private bool CamerasOpen;

    [SerializeField] private GameObject MainCamera;
    [SerializeField] private GameObject CameraSystemUI;
    [SerializeField] private GameObject MapImage;  // Reference na image s mapou

    [Header("UI Buttons")]
    [SerializeField] private GameObject OpenCamerasButton;  // Tlačítko pro otevření kamer
    [SerializeField] private GameObject CloseCamerasButton; // Tlačítko pro zavření kamer
    [SerializeField] private GameObject RoomTransferButton; // Tlačítko pro přesun do jiné místnosti
    [SerializeField] private GameObject SwitchCamerasModeButton; // Tlačítko pro přepnutí na ventilační kamery

    [Header("Rotation Settings")]
    [SerializeField] private float minRotationY = -10f;  // Minimální rotace Y pro zobrazení tlačítka
    [SerializeField] private float maxRotationY = 10f;   // Maximální rotace Y pro zobrazení tlačítka

    [Header("Camera Map Buttons")]
    [SerializeField] private Button[] cameraMapButtons; // Tlačítka na mapě pro výběr kamer
    [SerializeField] private float blinkInterval = 1f; // Interval blikání v sekundách
    [SerializeField] private Color highlightColor = Color.yellow; // Barva zvýraznění
    [SerializeField] private Color normalColor = Color.white; // Normální barva

    [Header("Power System")]
    [SerializeField] private bool isPowerOn = true;  // Stav elektřiny

    [Header("Audio")]
    [SerializeField] private AudioSource cameraSwitchAudioSource; // AudioSource pro zvuk přepínání
    [SerializeField] private AudioClip cameraSwitchSound; // Zvuk při přepnutí kamery
    [SerializeField] private AudioClip switchToVentSound; // Zvuk při přepnutí na ventilační kamery
    [SerializeField] private float currentAudioVolume = 0f; // Aktuální hlasitost (pro náhled v inspektoru)
    
    [Header("Power Denied Sound")]
    [SerializeField] private AudioSource powerDeniedAudioSource; // AudioSource pro zvuk zamítnutí při vypnuté elektřině
    [SerializeField] private AudioClip powerDeniedSound; // Zvuk při pokusu o zapnutí kamer bez elektřiny
    
    [Header("Background Music Control")]
    [SerializeField] private AudioSource backgroundMusicAudioSource; // AudioSource s pozadí hudbou
    [SerializeField] private float cameraVolumeReduction = 0.4f; // Snížení hlasitosti při otevření kamer (60% snížení = 0.4)
    private float originalBackgroundVolume = 1f; // Původní hlasitost pozadí hudby
    
    [Header("Electricity Sound Control")]
    [SerializeField] private AudioSource electricitySoundAudioSource; // AudioSource se zvukem elektřiny
    [SerializeField] private float electricityVolumeReduction = 0.1f; // Snížení na 10% (0.1)
    private float originalElectricityVolume = 1f; // Původní hlasitost zvuku elektřiny

    [SerializeField] private float CoolDownTime = 0.5f;
    [SerializeField] private float CoolDownTimer;

    private bool originalRoomTransferButtonState; // Uložení původního stavu tlačítka
    private Color originalButtonColor; // Původní barva tlačítka
    private Image openCamerasButtonImage; // Reference na Image komponentu tlačítka

    [SerializeField] private CameryVentSystem ventCameraSystem;
    [SerializeField] private ElectricityManagement electricityManagement; // Reference na elektřinu management

    private Coroutine currentBlinkCoroutine; // Reference na aktuální blikání

    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < Cameras.Length; i++)
        {
            Cameras[i].SetActive(false);
        }
        CameraSystemUI.SetActive(false);
        MapImage.SetActive(false);  // Skryj mapu na začátku
        CloseCamerasButton.SetActive(false); // Skryj tlačítko pro zavření na začátku
        
        // Ulož původní stav tlačítka pro přesun místnosti
        if (RoomTransferButton != null)
        {
            originalRoomTransferButtonState = RoomTransferButton.activeSelf;
        }

        // Ulož původní barvu tlačítka pro kamery
        if (OpenCamerasButton != null)
        {
            openCamerasButtonImage = OpenCamerasButton.GetComponent<Image>();
            if (openCamerasButtonImage != null)
            {
                originalButtonColor = openCamerasButtonImage.color;
            }
        }
        
        // Nastav hlasitost AudioSource na 0% na začátku
        if (cameraSwitchAudioSource != null)
        {
            cameraSwitchAudioSource.volume = 0f;
            currentAudioVolume = 0f;
        }
        
        // Ulož původní hlasitost pozadí hudby
        if (backgroundMusicAudioSource != null)
        {
            originalBackgroundVolume = backgroundMusicAudioSource.volume;
        }
        
        // Ulož původní hlasitost zvuku elektřiny
        if (electricitySoundAudioSource != null)
        {
            originalElectricityVolume = electricitySoundAudioSource.volume;
        }
        
        MainCamera.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        // Kontrola rotace pro zobrazení tlačítka "Otevřít kamery"
        if (!CamerasOpen)
        {
            CheckRotationForButton();
        }

        if (Input.GetKeyDown(OpenCameras))
        {
            // Povolit klávesové ovládání pouze pokud je elektřina zapnutá a nejsou otevřené ventilační kamery
            bool ventCamerasOpen = false;
            if (ventCameraSystem != null)
            {
                ventCamerasOpen = ventCameraSystem.AreVentCamerasOpen();
            }
            
            if (isPowerOn && !ventCamerasOpen)
            {
                // Při prvním otevření kamer nastav hlasitost na 100%
                if (cameraSwitchAudioSource != null && currentAudioVolume == 0f && !CamerasOpen)
                {
                    cameraSwitchAudioSource.volume = 1f;
                    currentAudioVolume = 1f;
                }
                
                CamerasOpen = !CamerasOpen;
                ShowCamera();
            }
        }

        if (CoolDownTimer <= 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                if (CamerasOpen && isPowerOn)
                {
                    Cameras[CurrentCam].SetActive(false);
                    CurrentCam = CurrentCam + 1;
                    if (CurrentCam >= Cameras.Length)
                    {
                        CurrentCam = 0;
                    }
                    GoToCamera(CurrentCam);
                    CoolDownTimer = CoolDownTime;
                }
            }
            else if (Input.GetAxis("Horizontal") < 0)
            {
                if (CamerasOpen && isPowerOn)
                {
                    Cameras[CurrentCam].SetActive(false);
                    CurrentCam = CurrentCam - 1;
                    if (CurrentCam < 0)
                    {
                        CurrentCam = Cameras.Length - 1;
                    }
                    GoToCamera(CurrentCam);
                    CoolDownTimer = CoolDownTime;
                }
            }
        }
        else
        {
            CoolDownTimer -= Time.deltaTime;
        }
    }
    
    private void ShowCamera()
    {
        if (CamerasOpen)
        {
            Cameras[CurrentCam].SetActive(true);
            CameraSystemUI.SetActive(true);
            MapImage.SetActive(true);  // Zobraz mapu když jsou kamery otevřené
            OpenCamerasButton.SetActive(false);  // Skryj tlačítko "Otevřít"
            CloseCamerasButton.SetActive(true);   // Zobraz tlačítko "Zavřít"
            
            // Zobraz tlačítko pro přepnutí na ventilační kamery
            if (SwitchCamerasModeButton != null)
            {
                SwitchCamerasModeButton.SetActive(true);
            }
            
            // Skryj tlačítko pro přesun místnosti když jsou kamery otevřené
            if (RoomTransferButton != null)
            {
                RoomTransferButton.SetActive(false);
            }
            
            // Při otevření kamery snížit hlasitost pozadí hudby
            if (backgroundMusicAudioSource != null)
            {
                backgroundMusicAudioSource.volume = originalBackgroundVolume * cameraVolumeReduction;
            }
            
            // Při otevření kamery snížit hlasitost zvuku elektřiny na 10%
            if (electricitySoundAudioSource != null)
            {
                electricitySoundAudioSource.volume = originalElectricityVolume * electricityVolumeReduction;
            }
            
            MainCamera.SetActive(false);
        }
        else
        {
            Cameras[CurrentCam].SetActive(false);
            CameraSystemUI.SetActive(false);
            MapImage.SetActive(false);  // Skryj mapu když jsou kamery zavřené
            OpenCamerasButton.SetActive(false);   // Bude se řídit rotací v Update()
            CloseCamerasButton.SetActive(false);  // Skryj tlačítko "Zavřít"
            
            // Skryj tlačítko pro přepnutí na ventilační kamery
            if (SwitchCamerasModeButton != null)
            {
                SwitchCamerasModeButton.SetActive(false);
            }
            
            // Obnov původní stav tlačítka pro přesun místnosti
            if (RoomTransferButton != null)
            {
                RoomTransferButton.SetActive(originalRoomTransferButtonState);
            }
            
            // Při zavření kamery obnovit původní hlasitost pozadí hudby
            if (backgroundMusicAudioSource != null)
            {
                backgroundMusicAudioSource.volume = originalBackgroundVolume;
            }
            
            // Při zavření kamery obnovit původní hlasitost zvuku elektřiny (100%)
            if (electricitySoundAudioSource != null)
            {
                electricitySoundAudioSource.volume = originalElectricityVolume;
            }
            
            MainCamera.SetActive(true);
            
            // Zastavit blikání pokud jsou kamery zavřené
            if (currentBlinkCoroutine != null)
            {
                StopCoroutine(currentBlinkCoroutine);
                currentBlinkCoroutine = null;
            }
        }
        
        UpdateCameraButtonHighlight();
    }
    
    public void GoToCamera(int Progression)
    {
        if (CamerasOpen && isPowerOn)
        {
            // Zastav zvuk na aktuální kameře před přepnutím
            StopCameraMotorSoundOnCamera(CurrentCam);
            
            Cameras[CurrentCam].SetActive(false);
            CurrentCam = Progression;
            Cameras[CurrentCam].SetActive(true);
            UpdateCameraButtonHighlight();
            PlayCameraSwitchSound();
        }
    }

    // Kontrola rotace pro zobrazení tlačítka "Otevřít kamery"
    private void CheckRotationForButton()
    {
        // Získej rotaci Main Camera
        float currentRotationY = MainCamera.transform.eulerAngles.y;
        
        // Převeď na rozsah -180 až 180
        if (currentRotationY > 180f)
            currentRotationY -= 360f;

        // Zkontroluj, zda je rotace v povoleném rozsahu
        bool isInRange = currentRotationY >= minRotationY && currentRotationY <= maxRotationY;
        
        // Zkontroluj, zda nejsou otevřené ventilační kamery
        bool ventCamerasOpen = false;
        if (ventCameraSystem != null)
        {
            ventCamerasOpen = ventCameraSystem.AreVentCamerasOpen();
        }
        
        // Zobraz/skryj tlačítko podle rotace a stavu ventilačních kamer
        if (OpenCamerasButton != null)
        {
            OpenCamerasButton.SetActive(isInRange && !ventCamerasOpen);
        }
    }

    // Metoda pro kliknutí na tlačítko "Otevřít kamery"
    public void OnOpenCamerasButtonClick()
    {
        // Povolit funkci pouze pokud je elektřina zapnutá a nejsou otevřené ventilační kamery
        bool ventCamerasOpen = false;
        if (ventCameraSystem != null)
        {
            ventCamerasOpen = ventCameraSystem.AreVentCamerasOpen();
        }
        
        if (isPowerOn && !ventCamerasOpen)
        {
            // Při prvním otevření kamer nastav hlasitost na 100%
            if (cameraSwitchAudioSource != null && currentAudioVolume == 0f)
            {
                cameraSwitchAudioSource.volume = 1f;
                currentAudioVolume = 1f;
            }
            
            CamerasOpen = true;
            ShowCamera();
            
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
        else
        {
            // Pokud je elektřina vypnutá nebo jsou otevřené ventilační kamery, přehraj zvuk zamítnutí
            if (!isPowerOn)
            {
                PlayPowerDeniedSound();
            }
        }
    }

    // Metoda pro kliknutí na tlačítko "Zavřít kamery"  
    public void OnCloseCamerasButtonClick()
{
    CamerasOpen = false;
    ShowCamera();
    
    // Informuj ElectricityManagement o změně stavu kamer
    if (electricityManagement == null)
    {
        electricityManagement = FindObjectOfType<ElectricityManagement>();
    }
    if (electricityManagement != null)
    {
        electricityManagement.OnCameraStateChanged();
    }
    
    // Zavři také ventilační kamery pokud jsou otevřené
    if (ventCameraSystem != null)
    {
        ventCameraSystem.ForceCloseVentCameras();
    }
}

    // NOVÉ METODY PRO OVLÁDÁNÍ ELEKTŘINY
    public void SetPowerOff()
    {
        isPowerOn = false;
        
        // Zavři kamery pokud jsou otevřené
        if (CamerasOpen)
        {
            CamerasOpen = false;
            ShowCamera();
        }
        
        // Změň barvu tlačítka na červenou
        if (openCamerasButtonImage != null)
        {
            openCamerasButtonImage.color = Color.red;
        }
    }

    public void SetPowerOn()
    {
        isPowerOn = true;
        
        // Vrať původní barvu tlačítka
        if (openCamerasButtonImage != null)
        {
            openCamerasButtonImage.color = originalButtonColor;
        }
    }

    // Metoda pro skrytí tlačítka otevření kamer (volaná z VentSystem)
    public void HideOpenCamerasButton()
    {
        if (OpenCamerasButton != null)
        {
            OpenCamerasButton.SetActive(false);
        }
    }

    // Metoda pro zjištění stavu hlavních kamer
    public bool AreCamerasOpen()
    {
        return CamerasOpen;
    }
    
    // METODY PRO BLIKÁNÍ TLAČÍTEK NA MAPĚ
    
    // Aktualizuj zvýraznění tlačítek podle aktuální kamery
    private void UpdateCameraButtonHighlight()
    {
        if (cameraMapButtons == null || cameraMapButtons.Length == 0) return;
        
        // Zastavit předchozí blikání
        if (currentBlinkCoroutine != null)
        {
            StopCoroutine(currentBlinkCoroutine);
            currentBlinkCoroutine = null;
        }
        
        // Resetovat všechna tlačítka na normální barvu
        for (int i = 0; i < cameraMapButtons.Length; i++)
        {
            if (cameraMapButtons[i] != null)
            {
                Image buttonImage = cameraMapButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = normalColor;
                }
            }
        }
        
        // Spustit blikání pro aktuální kameru pokud jsou kamery otevřené
        if (CamerasOpen && CurrentCam >= 0 && CurrentCam < cameraMapButtons.Length)
        {
            if (cameraMapButtons[CurrentCam] != null)
            {
                currentBlinkCoroutine = StartCoroutine(BlinkCameraButton(CurrentCam));
            }
        }
    }
    
    // Coroutine pro blikání tlačítka kamery
    private IEnumerator BlinkCameraButton(int cameraIndex)
    {
        if (cameraMapButtons == null || cameraIndex < 0 || cameraIndex >= cameraMapButtons.Length)
            yield break;
            
        Button button = cameraMapButtons[cameraIndex];
        if (button == null) yield break;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) yield break;
        
        while (CamerasOpen && CurrentCam == cameraIndex)
        {
            // Změň na zvýrazněnou barvu
            buttonImage.color = highlightColor;
            yield return new WaitForSeconds(blinkInterval);
            
            // Změň na normální barvu
            buttonImage.color = normalColor;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    
    // Metoda pro výběr kamery z mapy (připojit na OnClick tlačítek)
    public void SelectCameraFromMap(int cameraIndex)
    {
        if (CamerasOpen && isPowerOn && cameraIndex >= 0 && cameraIndex < Cameras.Length)
        {
            // Zastav zvuk na aktuální kameře před přepnutím
            StopCameraMotorSoundOnCamera(CurrentCam);
            
            GoToCamera(cameraIndex);
            PlayCameraSwitchSound();
        }
    }
    
    // POMOCNÁ METODA pro zastavení zvuku na konkrétní kameře
    private void StopCameraMotorSoundOnCamera(int cameraIndex)
    {
        if (cameraIndex >= 0 && cameraIndex < Cameras.Length && Cameras[cameraIndex] != null)
        {
            CameraAutoRotation cameraRotation = Cameras[cameraIndex].GetComponent<CameraAutoRotation>();
            if (cameraRotation != null)
            {
                cameraRotation.ForceStopCameraMotorSound();
            }
        }
    }
    
    // METODY PRO PŘEPÍNÁNÍ MEZI KAMERAMI
    
    // Metoda pro kliknutí na tlačítko přepnutí na ventilační kamery
    public void OnSwitchCamerasModeClick()
    {
        // Funkce pouze pokud jsou otevřené klasické kamery
        if (CamerasOpen)
        {
            SwitchToVentCameras();
        }
    }
    
    // Přepnutí na ventilační kamery
    public void SwitchToVentCameras()
    {
        if (ventCameraSystem != null && CamerasOpen)
        {
            // Zastav zvuk na aktuální kameře před přepnutím
            StopCameraMotorSoundOnCamera(CurrentCam);
            
            // Přehraj zvuk přepnutí na ventilační kamery
            PlaySwitchToVentSound();
            
            // Zavři klasické kamery
            CamerasOpen = false;
            
            // Informuj ElectricityManagement o změně stavu kamer
            if (electricityManagement == null)
            {
                electricityManagement = FindObjectOfType<ElectricityManagement>();
            }
            if (electricityManagement != null)
            {
                electricityManagement.OnCameraStateChanged();
            }
            
            // Skryj klasické UI
            Cameras[CurrentCam].SetActive(false);
            CameraSystemUI.SetActive(false);
            MapImage.SetActive(false);
            CloseCamerasButton.SetActive(false);
            if (SwitchCamerasModeButton != null)
            {
                SwitchCamerasModeButton.SetActive(false);
            }
            
            // Zastavit blikání klasických kamer
            if (currentBlinkCoroutine != null)
            {
                StopCoroutine(currentBlinkCoroutine);
                currentBlinkCoroutine = null;
            }
            
            // Otevři ventilační kamery
            ventCameraSystem.OpenVentCamerasFromSwitch();
        }
    }
    
    // Metoda pro otevření klasických kamer z přepínače (volaná z CameryVentSystem)
    public void OpenCamerasFromSwitch()
    {
        CamerasOpen = true;
        ShowCamera();
        
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
    
    // METODY PRO AUDIO
    
    // Přehrání zvuku při přepnutí kamery
    private void PlayCameraSwitchSound()
    {
        if (cameraSwitchAudioSource != null && cameraSwitchSound != null)
        {
            cameraSwitchAudioSource.PlayOneShot(cameraSwitchSound);
        }
    }
    
    // Přehrání zvuku při přepnutí na ventilační kamery
    private void PlaySwitchToVentSound()
    {
        if (cameraSwitchAudioSource != null && switchToVentSound != null)
        {
            cameraSwitchAudioSource.PlayOneShot(switchToVentSound);
        }
    }
    
    // Přehrání zvuku zamítnutí při vypnuté elektřině
    private void PlayPowerDeniedSound()
    {
        if (powerDeniedAudioSource != null && powerDeniedSound != null)
        {
            powerDeniedAudioSource.PlayOneShot(powerDeniedSound);
        }
    }
    
    // Metoda pro tichý reset kamer (bez zvuků)
    public void SilentResetCameras()
    {
        // Okamžitě zastav všechny camera audio efekty
        if (cameraSwitchAudioSource != null && cameraSwitchAudioSource.isPlaying)
        {
            cameraSwitchAudioSource.Stop();
        }
        if (powerDeniedAudioSource != null && powerDeniedAudioSource.isPlaying)
        {
            powerDeniedAudioSource.Stop();
        }
        
        // Vyvolej CamsOff (slezení z kamer) bez zvuků
        if (CamerasOpen)
        {
            OnCloseCamerasButtonClick();
            Debug.Log("Hráč slez z kamer při resetu");
        }
        
        // NOVĚ: Resetuj také ventilační kamery
        if (ventCameraSystem != null)
        {
            ventCameraSystem.SilentResetVentCameras();
        }
        
        // Obnov původní hlasitost pozadí hudby (kamery jsou vypnuté)
        if (backgroundMusicAudioSource != null)
        {
            backgroundMusicAudioSource.volume = originalBackgroundVolume;
        }
        
        // Obnov původní hlasitost elektřiny
        if (electricitySoundAudioSource != null)
        {
            electricitySoundAudioSource.volume = originalElectricityVolume;
        }
        
        Debug.Log("Kamery tiše resetovány - hráč je zpět v kanceláři");
    }
}
