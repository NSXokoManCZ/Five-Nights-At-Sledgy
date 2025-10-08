using UnityEngine;

[System.Serializable]
public class ThatcherNightConfig
{
    [Header("Night Configuration")]
    [SerializeField] private int nightNumber = 1; // Číslo noci (1-6)
    
    [Header("Hourly AI Settings")]
    [SerializeField] private int[] hourlyAI = new int[6] { 0, 0, 0, 0, 0, 0 }; // AI pro každou hodinu (12AM, 1AM, 2AM, 3AM, 4AM, 5AM)
    
    [Header("Hourly Movement Intervals")]
    [SerializeField] private float[] hourlyMovementIntervals = new float[6] { 10f, 10f, 10f, 10f, 10f, 10f }; // Interval pro každou hodinu v sekundách
    
    public ThatcherNightConfig(int night)
    {
        nightNumber = night;
        
        // Výchozí nastavení pro každou noc
        switch (night)
        {
            case 1:
                hourlyAI = new int[6] { 0, 0, 1, 1, 2, 2 };
                hourlyMovementIntervals = new float[6] { 15f, 15f, 12f, 12f, 10f, 10f };
                break;
            case 2:
                hourlyAI = new int[6] { 0, 1, 2, 3, 4, 5 };
                hourlyMovementIntervals = new float[6] { 12f, 11f, 10f, 9f, 8f, 7f };
                break;
            case 3:
                hourlyAI = new int[6] { 1, 2, 3, 4, 5, 6 };
                hourlyMovementIntervals = new float[6] { 10f, 9f, 8f, 7f, 6f, 5f };
                break;
            case 4:
                hourlyAI = new int[6] { 2, 3, 4, 5, 6, 8 };
                hourlyMovementIntervals = new float[6] { 9f, 8f, 7f, 6f, 5f, 4f };
                break;
            case 5:
                hourlyAI = new int[6] { 3, 4, 5, 6, 8, 10 };
                hourlyMovementIntervals = new float[6] { 8f, 7f, 6f, 5f, 4f, 3f };
                break;
            case 6:
                hourlyAI = new int[6] { 5, 6, 8, 10, 12, 15 };
                hourlyMovementIntervals = new float[6] { 6f, 5f, 4f, 3f, 2f, 1.5f };
                break;
            default:
                hourlyAI = new int[6] { 0, 0, 0, 0, 0, 0 };
                hourlyMovementIntervals = new float[6] { 10f, 10f, 10f, 10f, 10f, 10f };
                break;
        }
    }
    
    // Getter metody
    public int GetNightNumber() => nightNumber;
    
    public int GetAIForHour(int hour)
    {
        if (hour >= 0 && hour < hourlyAI.Length)
            return hourlyAI[hour];
        return 0;
    }
    
    public float GetMovementIntervalForHour(int hour)
    {
        if (hour >= 0 && hour < hourlyMovementIntervals.Length)
            return hourlyMovementIntervals[hour];
        return 10f;
    }
    
    // Setter metody (pro runtime úpravy v inspektoru)
    public void SetAIForHour(int hour, int aiLevel)
    {
        if (hour >= 0 && hour < hourlyAI.Length)
            hourlyAI[hour] = Mathf.Clamp(aiLevel, 0, 20);
    }
    
    public void SetMovementIntervalForHour(int hour, float interval)
    {
        if (hour >= 0 && hour < hourlyMovementIntervals.Length)
            hourlyMovementIntervals[hour] = Mathf.Max(0.1f, interval);
    }
    
    // Validace dat
    public void ValidateData()
    {
        // Ujisti se, že máme správný počet prvků
        if (hourlyAI.Length != 6)
            hourlyAI = new int[6] { 0, 0, 0, 0, 0, 0 };
            
        if (hourlyMovementIntervals.Length != 6)
            hourlyMovementIntervals = new float[6] { 10f, 10f, 10f, 10f, 10f, 10f };
        
        // Validuj AI hodnoty (0-20)
        for (int i = 0; i < hourlyAI.Length; i++)
        {
            hourlyAI[i] = Mathf.Clamp(hourlyAI[i], 0, 20);
        }
        
        // Validuj intervaly (min 0.1s)
        for (int i = 0; i < hourlyMovementIntervals.Length; i++)
        {
            hourlyMovementIntervals[i] = Mathf.Max(0.1f, hourlyMovementIntervals[i]);
        }
    }
}