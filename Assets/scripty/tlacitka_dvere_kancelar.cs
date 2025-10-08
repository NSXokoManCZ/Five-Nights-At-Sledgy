using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tlacitka_dvere_kancelar : MonoBehaviour
{
    [SerializeField] private dvere_kancelar Door;
    
    [Header("Vzdálenostní omezení")]
    [SerializeField] private float maxDistance = 3f; // Maximální vzdálenost pro aktivaci
    [SerializeField] private Transform player; // Reference na hráče (kameru)
    
    [Header("Blokování tlačítka")]
    private bool isBlocked = false; // Zda je tlačítko zablokované
    [SerializeField] private float cooldownTime = 0.5f; // Doba zablokování po použití
    private float lastUsedTime = 0f; // Čas posledního použití
    
    [Header("Power System Integration")]
    [SerializeField] private Svetlo_Strop_Kancelar lightSystem; // Reference na světelný systém pro kontrolu elektřiny
    [SerializeField] private ElectricityManagement electricityManagement; // Reference na elektřinu management
    private bool isPowerOff = false; // Flag zda je elektřina vypnuta
    
    [Header("Visual Effects")]
    [SerializeField] private Renderer buttonRenderer; // Renderer tlačítka pro změnu barvy
    [SerializeField] private float powerOffDarkenAmount = 0.4f; // Ztmavení při vypnutí elektřiny (40%)
    
    // Private variables
    private Color originalButtonColor;
    private Color powerOffButtonColor;

    private void OnMouseDown()
    {
        // Zkontroluj, jestli je tlačítko zablokované
        if (isBlocked)
            return;

        // Zkontroluj zda je elektřina zapnutá
        if (isPowerOff || (lightSystem != null && !lightSystem.IsLightOn))
        {
            Debug.Log("tlacitka_dvere_kancelar: Tlačítko ignorováno - elektřina je vypnuta");
            return;
        }

        // Zkontroluj cooldown - zda už uplynula doba od posledního použití
        if (Time.time - lastUsedTime < cooldownTime)
            return;

        // Zkontroluj vzdálenost před aktivací
        if (!IsPlayerInRange())
            return;

        Door.IsOpen = !Door.IsOpen;
        
        // Informuj ElectricityManagement o změně stavu dveří
        if (electricityManagement == null)
        {
            electricityManagement = FindObjectOfType<ElectricityManagement>();
        }
        if (electricityManagement != null)
        {
            electricityManagement.OnDoorStateChanged();
        }
        
        // Nastav čas posledního použití
        lastUsedTime = Time.time;
    }

    // Metoda pro zablokování/odblokování tlačítka
    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
    }

    private bool IsPlayerInRange()
    {
        // Pokud není přiřazen hráč, pokus se najít Main Camera
        if (player == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                player = mainCamera.transform;
        }

        // Pokud stále není hráč nalezen, vrať false
        if (player == null)
            return false;

        // Zkontroluj vzdálenost mezi hráčem a tlačítkem
        float distance = Vector3.Distance(player.position, transform.position);
        return distance <= maxDistance;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Najdi light system pokud není přiřazen
        FindLightSystemIfNeeded();
        
        // Inicializuj barvy tlačítka
        InitializeButtonColors();
    }
    
    /// <summary>
    /// Nastaví stav vypnutí elektřiny
    /// </summary>
    public void SetPowerOff(bool powerOff)
    {
        isPowerOff = powerOff;
        
        if (isPowerOff)
        {
            SetButtonColorToPowerOff();
            Debug.Log("tlacitka_dvere_kancelar: Elektřina vypnuta - tlačítko deaktivováno a ztmaveno");
        }
        else
        {
            RestoreButtonColor();
            Debug.Log("tlacitka_dvere_kancelar: Elektřina zapnuta - tlačítko aktivováno");
        }
    }
    
    /// <summary>
    /// Najde light system pokud není přiřazen
    /// </summary>
    private void FindLightSystemIfNeeded()
    {
        if (lightSystem == null)
        {
            lightSystem = FindObjectOfType<Svetlo_Strop_Kancelar>();
            
            if (lightSystem != null)
                Debug.Log("tlacitka_dvere_kancelar: Light system automaticky nalezen");
            else
                Debug.LogWarning("tlacitka_dvere_kancelar: Light system nenalezen!");
        }
    }
    
    /// <summary>
    /// Inicializuje barvy tlačítka
    /// </summary>
    private void InitializeButtonColors()
    {
        if (buttonRenderer != null && buttonRenderer.material != null)
        {
            originalButtonColor = buttonRenderer.material.color;
            powerOffButtonColor = originalButtonColor * (1f - powerOffDarkenAmount);
            
            Debug.Log("tlacitka_dvere_kancelar: Barvy tlačítka inicializovány");
        }
        else
        {
            Debug.LogWarning("tlacitka_dvere_kancelar: Button Renderer nebo Material není přiřazen!");
        }
    }
    
    /// <summary>
    /// Nastaví barvu tlačítka pro vypnutou elektřinu
    /// </summary>
    private void SetButtonColorToPowerOff()
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = powerOffButtonColor;
            Debug.Log("tlacitka_dvere_kancelar: Tlačítko ztmaveno kvůli vypnuté elektřině");
        }
    }
    
    /// <summary>
    /// Obnoví původní barvu tlačítka
    /// </summary>
    private void RestoreButtonColor()
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = originalButtonColor;
            Debug.Log("tlacitka_dvere_kancelar: Původní barva tlačítka obnovena");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
