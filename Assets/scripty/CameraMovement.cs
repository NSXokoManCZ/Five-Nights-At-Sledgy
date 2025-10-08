using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float CameraSenska = 100f;
    [SerializeField] private float MinLookDist;
    [SerializeField] private float MaxLookDist;
    [SerializeField, Range(0f, 0.5f)] private float screenEdgeThreshold = 0.2f;
    [SerializeField] private float smoothTime = 0.2f;


    [SerializeField] private GameObject buttonToShow;    // Tla��tko, kter� bude� p�id�vat p�es Inspector
    [SerializeField] private float buttonShowMin = -11f;  // Minim�ln� �hel, kdy se tla��tko zobraz�
    [SerializeField] private float buttonShowMax = 11f;   // Maxim�ln� �hel, kdy se tla��tko zobraz�

    [SerializeField] private GameObject buttonToShow2;    // Tla��tko, kter� bude� p�id�vat p�es Inspector
    [SerializeField] private float buttonShowMin2 = -36f;  // Minim�ln� �hel, kdy se tla��tko zobraz�
    [SerializeField] private float buttonShowMax2 = -21f;   // Maxim�ln� �hel, kdy se tla��tko zobraz�

    [Header("Utility Room Limits")]
    [SerializeField] private float utilityRoomMinLookDist = -80f;  // Minimální úhel pro strojírnu
    [SerializeField] private float utilityRoomMaxLookDist = -21f;  // Maximální úhel pro strojírnu

    [Header("Camera Settings")]
    [SerializeField] private bool isMainPlayerCamera = false;  // Zaškrtni pouze na Main Camera

    [Header("Game State Control")]
    [SerializeField] private NightShiftManagement gameManager;  // Reference na game manager pro kontrolu stavu hry

    float camlookDistance;
    float targetLookDistance;
    float currentVelocity;
    
    // Kontrola, zda je script aktivní pro rotaci
    private bool isRotationEnabled = true;

    // Záložní kopie výchozích limitů
    float defaultMinLookDist;
    float defaultMaxLookDist;

    void Start()
    {
        // Přepočet úhlu z 360 do -180 až 180
        float y = transform.localRotation.eulerAngles.y;
        if (y > 180f) y -= 360f;

        camlookDistance = y;
        targetLookDistance = camlookDistance;

        // Uložíme výchozí rozsah
        defaultMinLookDist = MinLookDist;
        defaultMaxLookDist = MaxLookDist;

        // Najdi NightShiftManagement pokud není přiřazené
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<NightShiftManagement>();
            if (gameManager == null)
            {
                Debug.LogWarning("CameraMovement: NightShiftManagement nebyl nalezen ve scéně!");
            }
        }
    }

    void Update()
    {
        // Pokud je rotace vypnutá (během přesunu kamery), neděláme nic
        if (!isRotationEnabled)
            return;

        // NOVÉ: Kontrola stavu hry - pohyb myší pouze během hraní
        if (gameManager != null)
        {
            // Pokud jsme v main menu, nepovoluj pohyb myší
            if (gameManager.IsInMainMenu())
            {
                // Neaplikuj mouse input, ale stále aktualizuj rotaci a tlačítka
                targetLookDistance = Mathf.Clamp(targetLookDistance, MinLookDist, MaxLookDist);
                camlookDistance = Mathf.SmoothDamp(camlookDistance, targetLookDistance, ref currentVelocity, smoothTime);
                transform.localRotation = Quaternion.Euler(0f, camlookDistance, 0f);
                
                // Aktualizuj tlačítka i v menu (pro případné debugování)
                UpdateButtonVisibility();
                return; // Skoč ze metody bez zpracování mouse inputu
            }
        }
            
        // Původní mouse input logika - pouze během hry
        float mouseX = Input.mousePosition.x;
        float screenWidth = Screen.width;

        if (mouseX < screenWidth * screenEdgeThreshold)
        {
            targetLookDistance -= CameraSenska * Time.deltaTime;
        }
        else if (mouseX > screenWidth * (1 - screenEdgeThreshold))
        {
            targetLookDistance += CameraSenska * Time.deltaTime;
        }

        targetLookDistance = Mathf.Clamp(targetLookDistance, MinLookDist, MaxLookDist);
        camlookDistance = Mathf.SmoothDamp(camlookDistance, targetLookDistance, ref currentVelocity, smoothTime);
        transform.localRotation = Quaternion.Euler(0f, camlookDistance, 0f);

        // Aktualizuj viditelnost tlačítek
        UpdateButtonVisibility();
    }

    // Nová metoda pro aktualizaci viditelnosti tlačítek
    private void UpdateButtonVisibility()
    {
        // P�evod y �hlu do rozsahu -180 a� 180
        float yAngle = transform.localEulerAngles.y;
        if (yAngle > 180f) yAngle -= 360f;

        // Podm�nka pro zobrazen� tla��tka - pouze na hlavní kameře hráče
        if (isMainPlayerCamera)
        {
            // Tlačítka zobrazuj pouze během hry, ne v menu
            bool shouldShowButtons = gameManager == null || !gameManager.IsInMainMenu();
            
            bool isLookingWithinRange = camlookDistance >= buttonShowMin && camlookDistance <= buttonShowMax;
            if (buttonToShow != null)
                buttonToShow.SetActive(shouldShowButtons && isLookingWithinRange);

            // Podm�nka pro zobrazen� tla��tka
            bool isLookingWithinRange2 = camlookDistance >= buttonShowMin2 && camlookDistance <= buttonShowMax2;
            if (buttonToShow2 != null)
                buttonToShow2.SetActive(shouldShowButtons && isLookingWithinRange2);
        }
        else
        {
            // Na ostatních kamerách skryj tlačítka
            if (buttonToShow != null)
                buttonToShow.SetActive(false);
            if (buttonToShow2 != null)
                buttonToShow2.SetActive(false);
        }
    }

    // Veřejné metody pro nastavení rozsahu rotace podle místnosti
    public void SetUtilityRoomLimits()
    {
        MinLookDist = utilityRoomMinLookDist;
        MaxLookDist = utilityRoomMaxLookDist;
        
        // Aktualizuj camlookDistance podle současné rotace kamery
        UpdateCameraRotationFromTransform();
        
        // Ujisti se, že současná rotace je v novém rozsahu
        targetLookDistance = Mathf.Clamp(targetLookDistance, MinLookDist, MaxLookDist);
    }

    public void SetMainRoomLimits()
    {
        MinLookDist = defaultMinLookDist;
        MaxLookDist = defaultMaxLookDist;
        
        // Aktualizuj camlookDistance podle současné rotace kamery
        UpdateCameraRotationFromTransform();
        
        // Ujisti se, že současná rotace je v novém rozsahu
        targetLookDistance = Mathf.Clamp(targetLookDistance, MinLookDist, MaxLookDist);
    }
    
    // Pomocná metoda pro synchronizaci rotace
    private void UpdateCameraRotationFromTransform()
    {
        float y = transform.localRotation.eulerAngles.y;
        if (y > 180f) y -= 360f;
        
        camlookDistance = y;
        targetLookDistance = y;
    }
    
    // Metody pro ovládání rotace během přesunu kamery
    public void DisableRotation()
    {
        isRotationEnabled = false;
    }
    
    public void EnableRotation()
    {
        isRotationEnabled = true;
        // Synchronizuj rotaci po opětovném zapnutí
        UpdateCameraRotationFromTransform();
    }
}
