using UnityEngine;

public class Svetlo_Strop_Kancelar : MonoBehaviour
{
    [Header("Světlo nastavení")]
    [SerializeField] private Light lightComponent;
    [SerializeField] private bool isLightOn = true; // Výchozí stav světla
    
    [Header("Debug - Inspector ovládání")]
    [SerializeField] private bool toggleLight = false; // Checkbox pro zapnutí/vypnutí v Inspectoru

    void Start()
    {
        // Pokud není přiřazena komponenta Light, pokus se ji najít
        if (lightComponent == null)
            lightComponent = GetComponent<Light>();
        
        // Nastav počáteční stav světla
        UpdateLightState();
    }

    void Update()
    {
        // Inspector ovládání - při změně checkboxu
        UpdateLightState();
    }

    void OnValidate()
    {
        // Volá se při změně hodnot v Inspectoru během editace
        if (toggleLight)
        {
            ToggleLight();
            toggleLight = false; // Reset checkboxu
        }
    }

    public void ToggleLight()
    {
        isLightOn = !isLightOn;
        UpdateLightState();
    }

    public void SetLight(bool state)
    {
        isLightOn = state;
        UpdateLightState();
    }
    
    // Metoda pro tichý reset elektřiny (bez zvuků a efektů)
    public void SilentSetLight(bool state)
    {
        isLightOn = state;
        UpdateLightState();
        // Bez dalších efektů - pouze nastaví světlo
    }

    private void UpdateLightState()
    {
        if (lightComponent != null)
        {
            lightComponent.enabled = isLightOn;
        }
    }

    // Vlastnosti pro čtení stavu
    public bool IsLightOn => isLightOn;
}
