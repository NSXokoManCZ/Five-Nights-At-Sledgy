using UnityEngine;

public class CameraAutoRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 4f; // Rychlost otáčení (stupně za sekundu)
    [SerializeField] private float minRotationAngle = -10f; // Minimální úhel rotace
    [SerializeField] private float maxRotationAngle = 10f; // Maximální úhel rotace
    [SerializeField] private float pauseTime = 2f; // Pauza na koncích (v sekundách)
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource cameraMotorAudioSource; // AudioSource pro zvuk ohybu kamery
    [SerializeField] private AudioClip cameraMotorSound; // AudioClip se zvukem motoru kamery
    
    [Header("Randomizer Settings")]
    [SerializeField] private bool randomizeStartPosition = true; // Náhodná počáteční pozice
    [SerializeField] private bool randomizeStartDirection = true; // Náhodný směr otáčení
    [SerializeField] private bool randomizeStartPause = true; // Náhodná počáteční pauza
    
    [Header("Current State - Debug")]
    [SerializeField] private float currentYRotation = 0f; // Aktuální rotace Y (pro náhled)
    [SerializeField] private bool isRotatingRight = true; // Směr otáčení (pro náhled)
    [SerializeField] private bool isPaused = false; // Stav pauzy (pro náhled)
    [SerializeField] private bool isCameraVisible = false; // Debug - zda je kamera viditelná pro hráče
    
    private float originalYRotation; // Původní Y rotace kamery
    private float pauseTimer = 0f; // Timer pro pauzu
    private bool cameraActive = false; // Zda je kamera aktivní
    private bool wasVisibleLastFrame = false; // Zda byla kamera viditelná v minulém frame
    
    void Start()
    {
        // Ulož původní rotaci kamery
        originalYRotation = transform.eulerAngles.y;
        
        // Randomizace počátečního stavu
        InitializeRandomStart();
    }
    
    void Update()
    {
        // Kontroluj, zda je kamera aktivní
        bool currentlyActive = gameObject.activeInHierarchy;
        
        // Debug - aktualizuj stav viditelnosti
        isCameraVisible = IsCameraVisibleToPlayer();
        
        // Zkontroluj změnu stavu kamery
        if (cameraActive && !currentlyActive)
        {
            // Kamera se právě deaktivovala - zastav zvuk
            StopCameraMotorSound();
        }
        
        cameraActive = currentlyActive;
        
        if (cameraActive)
        {
            HandleCameraRotation();
        }
        else
        {
            // Pokud kamera není aktivní, zajisti že zvuk je zastaven
            StopCameraMotorSound();
        }
    }
    
    private void HandleCameraRotation()
    {
        if (isPaused)
        {
            // Počítej pauzu
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
                pauseTimer = 0f;
                
                // Přehraj zvuk ohybu kamery při ukončení pauzy
                // Pouze pokud je kamera viditelná pro hráče
                if (IsCameraVisibleToPlayer())
                {
                    PlayCameraMotorSound();
                }
            }
            return;
        }
        
        // Otáčej kameru
        if (isRotatingRight)
        {
            // Otáčej doprava
            currentYRotation += rotationSpeed * Time.deltaTime;
            
            // Zkontroluj, zda jsme dosáhli maximálního úhlu
            if (currentYRotation >= maxRotationAngle)
            {
                currentYRotation = maxRotationAngle;
                isRotatingRight = false; // Změň směr
                isPaused = true; // Spusť pauzu
                pauseTimer = pauseTime;
                
                // Zastav zvuk když kamera začne pauzu
                StopCameraMotorSound();
            }
        }
        else
        {
            // Otáčej doleva
            currentYRotation -= rotationSpeed * Time.deltaTime;
            
            // Zkontroluj, zda jsme dosáhli minimálního úhlu
            if (currentYRotation <= minRotationAngle)
            {
                currentYRotation = minRotationAngle;
                isRotatingRight = true; // Změň směr
                isPaused = true; // Spusť pauzu
                pauseTimer = pauseTime;
                
                // Zastav zvuk když kamera začne pauzu
                StopCameraMotorSound();
            }
        }
        
        // Aplikuj rotaci
        Vector3 newRotation = transform.eulerAngles;
        newRotation.y = originalYRotation + currentYRotation;
        transform.eulerAngles = newRotation;
    }
    
    // Kontrola, zda je kamera viditelná pro hráče
    private bool IsCameraVisibleToPlayer()
    {
        // Kamera je viditelná, pokud je aktivní (activeInHierarchy)
        // což znamená, že hráč se právě dívá na tuto kameru
        return gameObject.activeInHierarchy;
    }
    
    // Přehrání zvuku motoru kamery
    private void PlayCameraMotorSound()
    {
        if (cameraMotorAudioSource != null && cameraMotorSound != null)
        {
            cameraMotorAudioSource.PlayOneShot(cameraMotorSound);
        }
    }
    
    // Zastavení zvuku motoru kamery
    private void StopCameraMotorSound()
    {
        if (cameraMotorAudioSource != null)
        {
            cameraMotorAudioSource.Stop();
        }
    }
    
    // VEŘEJNÁ METODA pro zastavení zvuku (volaná z CameraSystem)
    public void ForceStopCameraMotorSound()
    {
        StopCameraMotorSound();
    }
    
    // Metoda pro resetování rotace (volitelná)
    public void ResetRotation()
    {
        currentYRotation = 0f;
        isRotatingRight = true;
        isPaused = false;
        pauseTimer = 0f;
        
        Vector3 newRotation = transform.eulerAngles;
        newRotation.y = originalYRotation;
        transform.eulerAngles = newRotation;
    }
    
    // Metody pro nastavení parametrů za běhu (volitelné)
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    public void SetRotationLimits(float min, float max)
    {
        minRotationAngle = min;
        maxRotationAngle = max;
    }
    
    public void SetPauseTime(float pause)
    {
        pauseTime = pause;
    }
    
    // Metoda pro inicializaci náhodného startu
    private void InitializeRandomStart()
    {
        // Náhodná počáteční pozice
        if (randomizeStartPosition)
        {
            currentYRotation = Random.Range(minRotationAngle, maxRotationAngle);
        }
        else
        {
            currentYRotation = 0f; // Začínáme ze středu
        }
        
        // Náhodný směr otáčení
        if (randomizeStartDirection)
        {
            isRotatingRight = Random.Range(0, 2) == 0; // 50% šance pro každý směr
        }
        else
        {
            isRotatingRight = true; // Výchozí směr doprava
        }
        
        // Náhodná počáteční pauza (1.5 - 2.5 sekund) - pouze pro 50% kamer
        if (randomizeStartPause)
        {
            // 50% šance že kamera začne s pauzou, 50% že začne rovnou s pohybem
            if (Random.Range(0, 2) == 0)
            {
                isPaused = true;
                pauseTimer = Random.Range(1.5f, 2.5f);
            }
            else
            {
                isPaused = false;
                pauseTimer = 0f;
            }
        }
        else
        {
            isPaused = false;
            pauseTimer = 0f;
        }
        
        // Aplikuj počáteční rotaci
        Vector3 newRotation = transform.eulerAngles;
        newRotation.y = originalYRotation + currentYRotation;
        transform.eulerAngles = newRotation;
    }
}