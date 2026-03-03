using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class ShiftTimer : MonoBehaviour
{
    [SerializeField] private float Timer;
    [SerializeField] private int ShiftEndTime = 6;
    [SerializeField] private string DigitalClock;
    [SerializeField] private float TimeMultiplier = 1.2f;
    [SerializeField] private TextMeshProUGUI ClockText;
    [SerializeField] private GameObject WinScreen;
    [SerializeField] public bool Won; // Změněno na public pro NightShiftManagement
    
    [Header("End Game Elements")]
    [SerializeField] private Canvas[] CanvasesToHide; // Seznam canvasů k schovani
    [SerializeField] private AudioSource GeneratorAudioSource;
    [SerializeField] private AudioSource CameraAudioSource;
    [SerializeField] private AudioSource[] AdditionalAudioSources;
    [SerializeField] private CameraPositionController CameraPositionController; // Pro přesun do strojírny během ending
    
    [Header("End Game Audio")]
    [SerializeField] private AudioSource EndGameAudioSource; // AudioSource pro konec noci
    [SerializeField] private AudioClip EndGameSound; // Zvuk konce noci
    
    [Header("End Game Video")]
    [SerializeField] private VideoPlayer EndGameVideoPlayer; // VideoPlayer komponenta
    [SerializeField] private VideoClip EndGameVideoClip; // Video soubor
    [SerializeField] private RawImage VideoRawImage; // RawImage pro zobrazení videa
    
    [Header("Pre-End Warning Audio")]
    [SerializeField] private AudioSource WarningAudioSource; // AudioSource pro varovný zvuk
    [SerializeField] private AudioClip WarningSound; // Varovný zvuk 16s před koncem
    [SerializeField] private float WarningTime = 16f; // Čas před koncem kdy začne varovný zvuk
    
    [Header("Reset Options")]
    [SerializeField] private bool AutoResetAfterWin = false; // Automaticky resetuj po výhře
    [SerializeField] private Button ResetButton; // Tlačítko pro manuální reset
    
    private bool warningPlayed = false; // Flag zda už byl varovný zvuk přehrán

    // Start je volán před prvním snímkem aktualizace
    void Start()
    {
        DigitalClock = "";
        
        // Nastav reset tlačítko
        if (ResetButton != null)
        {
            ResetButton.onClick.AddListener(ResetGame);
        }
        
        // Nastav VideoPlayer při startu
        if (EndGameVideoPlayer != null)
        {
            EndGameVideoPlayer.playOnAwake = false;
            EndGameVideoPlayer.isLooping = false;
            EndGameVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            // Nastav video clip pokud je přiřazen
            if (EndGameVideoClip != null)
            {
                EndGameVideoPlayer.clip = EndGameVideoClip;
            }
            
            // Vytvořit RenderTexture pro video pokud není nastavena
            if (EndGameVideoPlayer.targetTexture == null && VideoRawImage != null)
            {
                RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
                EndGameVideoPlayer.targetTexture = renderTexture;
                VideoRawImage.texture = renderTexture;
            }
        }
        
        // Skryj video RawImage na začátku
        if (VideoRawImage != null)
        {
            VideoRawImage.gameObject.SetActive(false);
        }
    }

    // Update je volán jednou za snímek
    void Update()
    {
        if (!Won)
        {
            Timer += Time.deltaTime * TimeMultiplier;
            
            var hours = Mathf.FloorToInt(Timer / 60);
            var minutes = Mathf.FloorToInt(Timer - hours * 60);
            
            // Zkontroluj, jestli je čas přehrát varovný zvuk
            // Převeď herní čas na reálný čas pro synchronizaci zvuku
            float timeToEndInGameTime = (6 * 60) - Timer; // Zbývající herní čas v sekundách
            float timeToEndInRealTime = timeToEndInGameTime / TimeMultiplier; // Převod na reálný čas
            
            if (!warningPlayed && timeToEndInRealTime <= WarningTime && timeToEndInRealTime > 0)
            {
                PlayWarningSound();
                warningPlayed = true;
            }
            
            // Formát 12 AM - 6 AM
            string period = "AM";
            int displayHours = hours == 0 ? 12 : hours; // 0 hodin = 12 AM
            
            DigitalClock = displayHours.ToString() + " " + period;
            
            ClockText.text = DigitalClock;
            
            if (hours >= 6)
            {
                // Nastav 6 AM před zobrazením WinScreen
                DigitalClock = "6 AM";
                ClockText.text = DigitalClock;
                
                // Přehraj zvuk konce noci PŘED deaktivací ostatních zvuků
                PlayEndGameSound();
                
                // Přehraj video současně s audio
                PlayEndGameVideo();
                
                // Přesuň kameru do strojírny s 1 sekundovým zpožděním
                if (CameraPositionController != null)
                {
                    StartCoroutine(DelayedCameraMove());
                }
                
                // Deaktivuj všechny interaktivní prvky
                DeactivateGameElements();
                
                if (WinScreen != null)
                {
                    WinScreen.SetActive(true);
                }
                Won = true;
                
                // ODSTRANĚNO: Automatický reset - nyní řídí NightShiftManagement
                
                return; // Zastav další zpracování
            }
        }
    }
    
    private void DeactivateGameElements()
    {
        // Schováj všechny canvasy v seznamu
        if (CanvasesToHide != null)
        {
            foreach (Canvas canvas in CanvasesToHide)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                }
            }
        }
        
        // Zastav všechny zvuky
        if (GeneratorAudioSource != null)
        {
            GeneratorAudioSource.Stop();
            GeneratorAudioSource.enabled = false;
        }
        
        if (CameraAudioSource != null)
        {
            CameraAudioSource.Stop();
            CameraAudioSource.enabled = false;
        }
        
        // Zastav další audio zdroje
        if (AdditionalAudioSources != null)
        {
            foreach (AudioSource audioSource in AdditionalAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.enabled = false;
                }
            }
        }
    }
    
    private void PlayEndGameSound()
    {
        // Přehraj zvuk konce noci
        if (EndGameAudioSource != null && EndGameSound != null)
        {
            EndGameAudioSource.PlayOneShot(EndGameSound);
        }
    }
    
    private void PlayEndGameVideo()
    {
        // Zobraz video RawImage
        if (VideoRawImage != null)
        {
            VideoRawImage.gameObject.SetActive(true);
        }
        
        // Přehraj video
        if (EndGameVideoPlayer != null)
        {
            EndGameVideoPlayer.Play();
        }
    }
    
    private void PlayWarningSound()
    {
        // Přehraj varovný zvuk 16s před koncem
        if (WarningAudioSource != null && WarningSound != null)
        {
            WarningAudioSource.PlayOneShot(WarningSound);
        }
    }
    
    // Veřejná metoda pro znovuzobrazení canvasů (volat při splnění podmínky)
    public void ShowGameElements()
    {
        if (CanvasesToHide != null)
        {
            foreach (Canvas canvas in CanvasesToHide)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                }
            }
        }
    }
    
    // Veřejné metody pro ovládání videa
    public void StopEndGameVideo()
    {
        if (EndGameVideoPlayer != null)
        {
            EndGameVideoPlayer.Stop();
        }
        
        if (VideoRawImage != null)
        {
            VideoRawImage.gameObject.SetActive(false);
        }
    }
    
    public bool IsVideoPlaying()
    {
        return EndGameVideoPlayer != null && EndGameVideoPlayer.isPlaying;
    }
    
    // Veřejná metoda pro reset hry
    public void ResetGame()
    {
        // Reset základních hodnot
        Timer = 0f;
        DigitalClock = "";
        Won = false;
        warningPlayed = false;
        
        // Reset hodinek na 12 AM
        ClockText.text = "12 AM";
        
        // Skryj Win Screen
        if (WinScreen != null)
        {
            WinScreen.SetActive(false);
        }
        
        // Zastav a skryj end game video
        StopEndGameVideo();
        
        // Znovu aktivuj všechny canvasy
        RestoreGameElements();
        
        // Zastav end game audio
        if (EndGameAudioSource != null)
        {
            EndGameAudioSource.Stop();
        }
        
        // Zastav warning audio
        if (WarningAudioSource != null)
        {
            WarningAudioSource.Stop();
        }
        
        Debug.Log("Hra byla resetována!");
    }
    
    private void RestoreGameElements()
    {
        // Znovu zobraz všechny canvasy
        if (CanvasesToHide != null)
        {
            foreach (Canvas canvas in CanvasesToHide)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                }
            }
        }
        
        // Znovu aktivuj všechny zvuky
        if (GeneratorAudioSource != null)
        {
            GeneratorAudioSource.enabled = true;
            // Audio se spustí automaticky podle svého nastavení
        }
        
        if (CameraAudioSource != null)
        {
            CameraAudioSource.enabled = true;
            // Audio se spustí automaticky podle svého nastavení
        }
        
        // Znovu aktivuj další audio zdroje
        if (AdditionalAudioSources != null)
        {
            foreach (AudioSource audioSource in AdditionalAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.enabled = true;
                    // Audio se spustí automaticky podle svého nastavení
                }
            }
        }
    }
    
    // Coroutina pro zpožděný přesun kamery
    private System.Collections.IEnumerator DelayedCameraMove()
    {
        yield return new WaitForSeconds(1f); // Počkej 1 sekundu
        
        CameraPositionController.MoveToPosition(1); // Index 1 = strojírna
        Debug.Log("Kamera se přesouvá do strojírny s 1s zpožděním během ending sekvence");
    }
    
    /// <summary>
    /// Veřejná metoda pro získání aktuální hodiny (0-5, kde 0=12AM, 1=1AM, atd.)
    /// </summary>
    public int GetCurrentHour()
    {
        if (!Won)
        {
            var hours = Mathf.FloorToInt(Timer / 60);
            return Mathf.Clamp(hours, 0, 5); // 0-5 hodin (12AM-5AM)
        }
        return 0;
    }
}
