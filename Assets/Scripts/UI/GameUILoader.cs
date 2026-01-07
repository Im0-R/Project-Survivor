using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUILoader : MonoBehaviour
{
    public static GameUILoader Instance { get; private set; }

    [SerializeField] private string uiSceneName = "PlayerUI";

    private bool uiLoaded = false;
    private AsyncOperation loadingOp;
    public string playerName = "Player";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // only load once
    public IEnumerator EnsureUILoadedOnce()
    {
        if (uiLoaded) yield break;

        // Déjà en train de charger ?
        if (loadingOp != null && !loadingOp.isDone)
        {
            yield return loadingOp;
            uiLoaded = true;
            yield break;
        }

        //already loaded?
        if (SceneManager.GetSceneByName(uiSceneName).isLoaded)
        {
            uiLoaded = true;
            yield break;
        }

        loadingOp = SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        yield return loadingOp;

        uiLoaded = true;
    }
}
