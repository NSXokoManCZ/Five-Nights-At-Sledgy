using UnityEngine;

public class dvere_kancelar : MonoBehaviour
{

    [SerializeField] private Vector3 OpenPos;
    [SerializeField] private Vector3 ClosePos;
    [SerializeField] private float DoorSpeed = 2f;

    [Header("Zvuky dveří")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound; // Zvuk otevření dveří
    [SerializeField] private AudioClip closeSound; // Zvuk zavření dveří

    public bool IsOpen;
    public bool IsOn;

    // Private proměnné pro sledování změn stavu
    private bool wasOpen;
    private bool soundPlayed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = OpenPos;
        IsOpen = true;
        wasOpen = IsOpen;
        soundPlayed = false;

        // Pokud není přiřazen AudioSource, pokus se ho najít
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // Zkontroluj změnu stavu a přehraj zvuk
        CheckAndPlaySound();

        if (IsOpen)
        {
            if (transform.position != OpenPos)
            {
                if (Vector3.Distance(transform.position, OpenPos) <= 0.01f)
                {
                    transform.position = OpenPos;
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, OpenPos, DoorSpeed * Time.deltaTime);
                }
            }
        }
        else
        {
            if (transform.position != ClosePos)
            {
                if (Vector3.Distance(transform.position, ClosePos) <= 0.01f)
                {
                    transform.position = ClosePos;
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, ClosePos, DoorSpeed * Time.deltaTime);
                }
            }
        }
    }

    // Kontrola změny stavu a přehrání zvuku
    private void CheckAndPlaySound()
    {
        // Pokud se stav změnil
        if (IsOpen != wasOpen && !soundPlayed)
        {
            if (IsOpen && openSound != null && audioSource != null)
            {
                // Dveře se otevírají - přehraj zvuk otevření
                audioSource.PlayOneShot(openSound);
            }
            else if (!IsOpen && closeSound != null && audioSource != null)
            {
                // Dveře se zavírají - přehraj zvuk zavření
                audioSource.PlayOneShot(closeSound);
            }
            
            soundPlayed = true;
        }

        // Resetuj flag, když se stav ustálí
        if (IsOpen == wasOpen)
        {
            soundPlayed = false;
        }

        // Aktualizuj předchozí stav
        wasOpen = IsOpen;
    }

    // Metoda pro vynucení otevření dveří (při vypnutí světla)
    public void ForceOpen()
    {
        IsOpen = true;
    }
    
    // Metoda pro tichý reset dveří (bez zvuku)
    public void SilentForceOpen()
    {
        IsOpen = true;
        wasOpen = true; // Nastav wasOpen také na true, aby se zvuk nepřehrál
        soundPlayed = true; // Nastav soundPlayed na true, aby se zvuk přeskočil
    }
}
