using UnityEngine;
using UnityEngine.UI;

public class ResetNight : MonoBehaviour
{
    [Header("Player Reset")]
    [SerializeField] private Transform PlayerTransform; // Reference na hráče pro reset pozice
    [SerializeField] private Transform PlayerCamera; // Reference na kameru hráče
    [SerializeField] private MonoBehaviour CameraController; // Reference na kamerový controller (CameraMovement apod.)
    [SerializeField] private Vector3 PlayerStartPosition; // Počáteční pozice hráče
    [SerializeField] private Vector3 PlayerStartRotation; // Počáteční rotace hráče
    [SerializeField] private Vector3 CameraStartPosition; // Počáteční pozice kamery
    [SerializeField] private Vector3 CameraStartRotation; // Počáteční rotace kamery
    
    [Header("Door Systems")]
    [SerializeField] private dvere_kancelar[] DoorsToOpen; // Dveře k otevření
    
    [Header("Camera Systems")]
    [SerializeField] private CameraSystem CameraSystemRef; // Reference na CameraSystem pro vypnutí kamer
    [SerializeField] private CameraPositionController CameraPositionController; // Reference na CameraPositionController pro reset pozice
    
    [Header("Power Systems")]
    [SerializeField] private Svetlo_Strop_Kancelar_Tlacitko PowerButton; // Reference na tlačítko elektřiny
    [SerializeField] private FlashlightSystem FlashlightSystem; // Reference na systém baterky
    
    [Header("Reset Control")]
    [SerializeField] private Button ResetButton; // Tlačítko pro manuální reset
    [SerializeField] private VentilaceLaseryDvere LaserDoorsSystem; // Reference na laser doors systém
    
    void Start()
    {
        // Nastav reset tlačítko
        if (ResetButton != null)
        {
            ResetButton.onClick.AddListener(ResetNightToOriginalState);
        }
    }
    
    
    
    // Veřejná metoda pro kompletní reset noci
    public void ResetNightToOriginalState()
    {
        Debug.Log("Začíná reset noci do původního stavu...");
        
        // Nejdřív slez z kamer (pokud na nich hráč je)
        SilentCameraExit();
        
        // Reset pozice hráče
        ResetPlayerPosition();
        
       
        
        // Reset dveří
        ResetDoors();
        
        // Reset kamer
        ResetCameras();
        
        // Reset elektřiny a baterky
        ResetPowerSystems();
        
        // Reset laser doors systému
        ResetLaserDoors();
        
        Debug.Log("Reset noci dokončen!");
    }
    
    private void ResetPlayerPosition()
    {
        Debug.Log("=== CAMERA TELEPORT RESET ===");
        
        // Přímý teleport kamery na původní pozici bez animace
        if (PlayerCamera != null)
        {
            PlayerCamera.position = CameraStartPosition;
            PlayerCamera.eulerAngles = CameraStartRotation;
            Debug.Log("Kamera teleportována na pozici: " + CameraStartPosition + " s rotací: " + CameraStartRotation);
        }
        else
        {
            Debug.LogWarning("PlayerCamera není přiřazen!");
        }
        
        Debug.Log("Camera teleport reset dokončen");
    }
    
    private void SilentCameraExit()
    {
        // Tichý reset kamer - slez z kamer bez zvuků
        if (CameraSystemRef != null)
        {
            CameraSystemRef.SilentResetCameras();
            Debug.Log("Hráč tiše slez z kamer");
        }
        else
        {
            Debug.LogWarning("CameraSystemRef není přiřazen!");
        }
    }
    

    
    private void ResetDoors()
    {
        if (DoorsToOpen != null && DoorsToOpen.Length > 0)
        {
            foreach (dvere_kancelar door in DoorsToOpen)
            {
                if (door != null)
                {
                    door.IsOpen = true;
                    door.SilentForceOpen(); // Použij tichý reset místo ForceOpen()
                    Debug.Log("Dveře tiše otevřeny: " + door.name);
                }
            }
        }
        else
        {
            Debug.LogWarning("Žádné dveře nejsou přiřazeny k resetu!");
        }
    }
    
    private void ResetCameras()
    {
        // Vypni kamery
        if (CameraSystemRef != null)
        {
            if (CameraSystemRef.gameObject.activeInHierarchy)
            {
                // Pokus se zavolat metody pro vypnutí kamer
                CameraSystemRef.SendMessage("CamsOff", SendMessageOptions.DontRequireReceiver);
                CameraSystemRef.SendMessage("CloseCameras", SendMessageOptions.DontRequireReceiver);
                Debug.Log("Kamery vypnuty");
            }
            else
            {
                Debug.LogWarning("CameraSystem není aktivní!");
            }
        }
        else
        {
            Debug.LogWarning("CameraSystemRef není přiřazen!");
        }
        
        // Reset pozice kamery do kanceláře pomocí CameraPositionController
        if (CameraPositionController != null)
        {
            CameraPositionController.MoveToPosition(0); // Index 0 = kancelář
            Debug.Log("Kamera přesunuta do kanceláře přes CameraPositionController");
        }
        else
        {
            Debug.LogWarning("CameraPositionController není přiřazen!");
        }
    }
    
    private void ResetPowerSystems()
    {
        // Reset elektřiny (bez zvuků)
        if (PowerButton != null)
        {
            PowerButton.SilentResetPower();
            Debug.Log("Elektřina tiše resetována na zapnutou");
        }
        else
        {
            Debug.LogWarning("PowerButton není přiřazen!");
        }
        
        // Reset baterky (bez zvuků)
        if (FlashlightSystem != null)
        {
            FlashlightSystem.SilentResetFlashlight();
            Debug.Log("Baterka tiše resetována");
        }
        else
        {
            Debug.LogWarning("FlashlightSystem není přiřazen!");
        }
    }
    
    private void ResetLaserDoors()
    {
        // Reset laser doors (tiché)
        if (LaserDoorsSystem != null)
        {
            LaserDoorsSystem.SilentResetSystem();
            Debug.Log("Laser doors tiše resetovány");
        }
        else
        {
            Debug.LogWarning("LaserDoorsSystem není přiřazen!");
        }
    }
    
    
    
    // Veřejné metody pro individuální reset (volitelné)
    public void ResetPlayerOnly()
    {
        ResetPlayerPosition();
    }
    

    public void ResetDoorsOnly()
    {
        ResetDoors();
    }
    
    public void ResetCamerasOnly()
    {
        ResetCameras();
    }
    
    public void ResetPowerSystemsOnly()
    {
        ResetPowerSystems();
    }
    
    public void SilentCameraExitOnly()
    {
        SilentCameraExit();
    }
    
}