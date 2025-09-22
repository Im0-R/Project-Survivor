using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    private void Awake()
    {
        // Ensure there is only one instance of TimeManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void PauseGame () 
    {
        Time.timeScale = 0;
    }
    public void ResumeGame () 
    {
        Time.timeScale = 1;
    }
}
