using UnityEngine;
using UnityEngine.UI;

public class CanvasSwitcher : MonoBehaviour
{
    struct Menu
    {
        public Canvas canvas;
        public GraphicRaycaster graphicRaycaster;

        public void Toggle()
        {
            canvas.enabled = !canvas.enabled;
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = !graphicRaycaster.enabled;
        }
        public void ToggleOn()
        {
            canvas.enabled = true;
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = true;
        }
        public void ToggleOff()
        {
            canvas.enabled = false;
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = false;
        }
    }

    private Menu[] menu = new Menu[0];
    private int currentIndex = 0;

    private void Start()
    {
        Canvas[] canvas = GetComponentsInChildren<Canvas>(true);

        menu = new Menu[canvas.Length];
        for (int i = 0; i < canvas.Length; i++)
        {
            menu[i].canvas = canvas[i];
            menu[i].graphicRaycaster = canvas[i].gameObject.GetComponent<GraphicRaycaster>();
        }
    }

    public void SwitchCanvas(int menuIndex)
    {
        if (menu == null)
        {
            Debug.LogError("Canvas list is null " + gameObject.name);
            return;
        }
        if (0 <= menuIndex && menuIndex < menu.Length)
        {
            menu[currentIndex].ToggleOff();
            menu[menuIndex].ToggleOn();

            currentIndex = menuIndex;
        }
        else
        {
            Debug.LogError("Menu index out of range: " + menuIndex + " in " + gameObject.name);
        }
    }
    public void SwitchCanvas(string menuName)
    {
        for (int i = 0; i < menu.Length; i++)
        {
            if (menu[i].canvas.gameObject.name == menuName)
            {
                Debug.Log("Switching to menu: " + menuName + " in " + gameObject.name);
                SwitchCanvas(i);
                return;
            }
        }
        Debug.LogError("Menu name not found: " + menuName + " in " + gameObject.name);
    }
    public void SwitchToNextCanvas()
    {
        if (currentIndex + 1 >= menu.Length)
        {
            Debug.LogError("No next canvas to switch to in " + gameObject.name);
            return;
        }
        int nextIndex = (currentIndex + 1);
        SwitchCanvas(nextIndex);
    }
    public void SwitchToPreviousCanvas()
    {
        if (currentIndex - 1 < 0)
        {
            Debug.LogError("No previous canvas to switch to in " + gameObject.name);
            return;
        }
        int previousIndex = (currentIndex - 1 + menu.Length);
        SwitchCanvas(previousIndex);
    }
    public void ToggleCanvas()
    {
        menu[currentIndex].Toggle();
    }

    public bool IsCurrentCanvasEnable()
    {
        return menu[currentIndex].canvas.enabled;
    }
}