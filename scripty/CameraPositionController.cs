using UnityEngine;

public class CameraPositionController : MonoBehaviour
{
    [Header("Camera Positions")]
    [SerializeField] private Transform[] cameraPositions; // 0 = MainRoom, 1 = UtilityRoom
    private Transform targetPosition;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Human-like Movement Effects")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private bool enableRandomSway = true;
    [SerializeField] private bool enableNervousMovement = false;
    [SerializeField] private bool enableBreathing = true;

    [Header("Head Bob Settings")]
    [SerializeField] private float bobIntensity = 0.1f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Random Sway Settings")]
    [SerializeField] private float swayIntensity = 0.05f;
    [SerializeField] private float swaySpeed = 1f;

    [Header("Nervous Movement Settings")]
    [SerializeField] private float nervousIntensity = 0.2f;
    [SerializeField] private float nervousSpeed = 8f;
    [Header("Breathing Settings")]
    [SerializeField] private float breathingIntensity = 0.02f;
    [SerializeField] private float breathingSpeed = 0.3f;
    [Header("Camera Reset Settings")]
    [SerializeField] private bool autoResetRotation = true;
    [SerializeField] private float rotationResetDelay = 0.2f; // Zpoždění před resetem rotace
    [SerializeField] private float rotationResetSpeed = 3f;   // Rychlost návratu k původní rotaci
    [Tooltip("Poznámka: Kamera se nyní automaticky nastaví na rotaci z inspektoru po příchodu do místnosti")]
    [SerializeField] private bool showInfo = true;
    // Private variables
    private Vector3 originalPosition;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float movementProgress = 0f;
    private bool isMoving = false;
    private float bobOffset = 0f;
    private Vector3 swayOffset = Vector3.zero;
    private Vector3 nervousOffset = Vector3.zero;
    private float breathingOffset = 0f;
    // Rotation reset variables
    private bool shouldResetRotation = false;
    private float rotationResetTimer = 0f;
    private Quaternion originalTargetRotation;
    private bool isResettingRotation = false;
    private bool hasPlayerMovedCamera = false;

    void Start()
    {
        // Na začátku se teleportuj do výchozí pozice
        transform.position = cameraPositions[0].position;
        transform.rotation = cameraPositions[0].rotation;
        targetPosition = cameraPositions[0];
        originalPosition = transform.position;

        // Randomizuj počáteční offsety pro přirozenější pohyb
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        swayOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

        // Na začátku hráč nepohyboval myší, takže animace mohou běžet
        hasPlayerMovedCamera = false;
        
        // DŮLEŽITÉ: Nastav počáteční limity pro MainRoom
        CameraMovement cameraMovement = GetComponent<CameraMovement>();
        if (cameraMovement != null)
        {
            cameraMovement.SetMainRoomLimits();
        }
    }
    void Update()
    {
        // Zkontroluj, jestli se hráč pokusil otočet kamerou
        CheckForPlayerCameraMovement();

        // Aktualizuj pohyb mezi pozicemi
        UpdateMovement();

        // Zpracuj reset rotace
        HandleRotationReset();

        // Přidej lidské efekty
        ApplyHumanLikeEffects();
    }
    private void CheckForPlayerCameraMovement()
    {
        // Kontrola vstupu myši pro otočení kamery
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            hasPlayerMovedCamera = true;
            // Pokud probíhá reset, zruš ho
            if (isResettingRotation)
            {
                shouldResetRotation = false;
                isResettingRotation = false;
            }
        }
        else if (hasPlayerMovedCamera && !isMoving && !isResettingRotation)
        {
            // Po krátké pauze bez pohybu myši se efekty znovu zapnou
            // Počkej trochu a pak znovu zapni animace
            hasPlayerMovedCamera = false;
        }
    }

    private void UpdateMovement()
    {
        if (isMoving && targetPosition != null)
        {
            movementProgress += moveSpeed * Time.deltaTime;

            if (movementProgress >= 1f)
            {
                // Pohyb dokončen
                movementProgress = 1f;
                isMoving = false;
                transform.position = targetPosition.position;
                transform.rotation = targetPosition.rotation;
                originalPosition = transform.position;
                // Reset rotace už není potřeba - kamera je už ve správné rotaci
                shouldResetRotation = false;
                isResettingRotation = false;

                // Po dokončení pohybu resetuj flag pro pohyb hráče
                hasPlayerMovedCamera = false;
                
                // DŮLEŽITÉ: Znovu zapni CameraMovement rotaci po dokončení přesunu
                CameraMovement cameraMovement = GetComponent<CameraMovement>();
                if (cameraMovement != null)
                {
                    cameraMovement.EnableRotation();
                }
            }
            else
            {
                // Plynulý pohyb s animační křivkou
                float curveValue = movementCurve.Evaluate(movementProgress);
                transform.position = Vector3.Lerp(startPosition, targetPosition.position, curveValue);
                transform.rotation = Quaternion.Lerp(startRotation, targetPosition.rotation, curveValue);
                originalPosition = transform.position;
            }
        }
    }
    private void HandleRotationReset()
    {
        if (shouldResetRotation && !isMoving && !hasPlayerMovedCamera)
        {
            rotationResetTimer += Time.deltaTime;

            if (rotationResetTimer >= rotationResetDelay && !isResettingRotation)
            {
                // Začni reset rotace zpět na původní pozici
                isResettingRotation = true;
            }

            if (isResettingRotation)
            {
                // Plynule nastav rotaci zpět na rotaci pozice z inspektoru
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    originalTargetRotation,
                    rotationResetSpeed * Time.deltaTime
                );

                // Zkontroluj, jestli je rotace dostatečně blízko cílovë
                if (Quaternion.Angle(transform.rotation, originalTargetRotation) < 1f)
                {
                    transform.rotation = originalTargetRotation;
                    shouldResetRotation = false;
                    isResettingRotation = false;
                    // Po dokončení resetu znovu zapni efekty
                    hasPlayerMovedCamera = false;
                }
            }
        }
    }

    private void ApplyHumanLikeEffects()
    {
        Vector3 effectsOffset = Vector3.zero;

        // Head Bob (houpání při chůzi)
        if (enableHeadBob && isMoving)
        {
            float bobValue = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobIntensity;
            effectsOffset.y += bobValue;
        }

        // Random Sway (náhodné kolísání) - běží stále
        if (enableRandomSway && !isResettingRotation)
        {
            Vector3 sway = new Vector3(
                Mathf.PerlinNoise(Time.time * swaySpeed, 0f) - 0.5f,
                Mathf.PerlinNoise(Time.time * swaySpeed, 1000f) - 0.5f,
                Mathf.PerlinNoise(Time.time * swaySpeed, 2000f) - 0.5f
            ) * swayIntensity;
            effectsOffset += sway;
        }

        // Nervous Movement (nervózní pohyb) - běží stále
        if (enableNervousMovement && !isResettingRotation)
        {
            Vector3 nervous = new Vector3(
                Mathf.Sin(Time.time * nervousSpeed) * 0.5f,
                Mathf.Sin(Time.time * nervousSpeed * 1.3f) * 0.3f,
                Mathf.Sin(Time.time * nervousSpeed * 0.8f) * 0.4f
            ) * nervousIntensity;
            effectsOffset += nervous;
        }

        // Breathing (dýchání) - běží stále
        if (enableBreathing && !isResettingRotation)
        {
            float breathing = Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
            effectsOffset.y += breathing;
        }

        // Aplikuj všechny efekty - animace běží vždy (kromě reset rotace)
        if (!isResettingRotation)
        {
            if (!isMoving)
            {
                // Když kamera stojí, aplikuj plné efekty
                transform.position = originalPosition + effectsOffset;
            }
            else
            {
                // Během pohybu aplikuj efekty také (s menší intenzitou)
                transform.position += effectsOffset * 0.5f;
            }
        }
    }
    public void MoveToPosition(int index)
    {
        if (index >= 0 && index < cameraPositions.Length && targetPosition != cameraPositions[index])
        {
            // Nastav novou cílovou pozici
            targetPosition = cameraPositions[index];

            // Připrav pohyb
            startPosition = transform.position;
            startRotation = transform.rotation;
            movementProgress = 0f;
            isMoving = true;
            
            // Aktualizuj originální pozici pro efekty
            originalPosition = startPosition;
            
            // Zruš případný probíhající reset rotace a resetuj flag pro pohyb hráče
            shouldResetRotation = false;
            isResettingRotation = false;
            hasPlayerMovedCamera = false;

            // Nastav originalTargetRotation pro případný pozdější reset
            originalTargetRotation = targetPosition.rotation;

            // KLÍČOVÉ: Vypni CameraMovement rotaci během přesunu a nastav limity
            CameraMovement cameraMovement = GetComponent<CameraMovement>();
            if (cameraMovement != null)
            {
                // Vypni rotaci během přesunu pro plynulou interpolaci
                cameraMovement.DisableRotation();
                
                if (index == 1) // UtilityRoom (strojírna)
                {
                    cameraMovement.SetUtilityRoomLimits();
                }
                else if (index == 0) // MainRoom (kancelář)
                {
                    cameraMovement.SetMainRoomLimits();
                }
            }
        }
    }

    // Pomocné metody pro ladění v inspektoru
    [System.Serializable]
    public class MovementSettings
    {
        [Header("Quick Presets")]
        [Space]
        public bool calmMovement = false;
        public bool nervousMovement = false;
        public bool drunkMovement = false;

        void OnValidate()
        {
            // Můžeš přidat logiku pro rychlé presety
        }
    }
    
    // Public property pro kontrolu zda probíhá přesun
    public bool IsMoving => isMoving;
    
    // Public property pro kontrolu zda jsme v Utility Room (pozice 1)
    public bool IsInUtilityRoom => targetPosition == cameraPositions[1];
}
