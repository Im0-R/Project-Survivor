using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void LoadMenuScene()
    {
        if (SceneManager.GetActiveScene().name != "Menu")
            SceneManager.LoadScene("Menu");
    }
    public static void LoadTownScene()
    {
        if (SceneManager.GetActiveScene().name != "Town")
            SceneManager.LoadScene("Town");
    }
}
