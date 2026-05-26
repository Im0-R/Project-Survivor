using TMPro;
using UnityEngine;

public class MapEventTimerUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI difficultyText;

    private void Update()
    {
        MapEventState state = MapEventState.Instance;

        if (state == null || !state.eventRunning)
        {
            if (root != null)
                root.SetActive(false);

            return;
        }

        if (root != null)
            root.SetActive(true);

        int seconds = Mathf.CeilToInt(state.remainingTime);
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;

        if (timerText != null)
            timerText.text = $"{minutes:00}:{remainingSeconds:00}";

        if (difficultyText != null)
            difficultyText.text = $"Difficulty {state.difficulty}";
    }
}