using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void LoadMenuScene()
    {
        if (SceneManager.GetActiveScene().name != "Menu")
            SceneManager.LoadScene("Menu");
        Debug.Log("Menu scene loaded.");
    }
    public static void LoadTownScene()
    {
        if (SceneManager.GetActiveScene().name != "Town")
            SceneManager.LoadScene("Town");
        Debug.Log("Town scene loaded.");
    }
    public static void UnloadMenuScene()
    {


        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == "Menu")
            {
                SceneManager.UnloadSceneAsync(i);
                Debug.Log("Menu scene unloaded.");
                return;
            }
        }
    }
    public static void UnloadTownScene()
    {
        if (SceneManager.GetActiveScene().name == "Town")
            SceneManager.UnloadSceneAsync("Town");
        Debug.Log("Town scene unloaded.");
    }
}
