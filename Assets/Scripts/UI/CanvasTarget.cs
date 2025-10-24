using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTarget : MonoBehaviour
{
    [SerializeField] private Slider lifeBar;
    [SerializeField] private TextMeshProUGUI nameTMP;

    public void Update()
    {
        // Raycast from  the camera to the hovered NetworkEntity
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            NetworkEntity entHit = hitInfo.collider.GetComponentInParent<NetworkEntity>();
            if (entHit != null)
            {
                if (!lifeBar.gameObject.activeSelf || !lifeBar.gameObject.activeSelf)
                {
                    ShowUI();
                    lifeBar.value = (entHit.currentHealth / entHit.maxHealth) * 100f;
                    nameTMP.text = entHit.entityName.ToString();
                }
            }
            else
            {
                HideUI();
            }
        }
        else
        {
            HideUI();
        }
    }

    private void ShowUI()
    {
        lifeBar.gameObject.SetActive(true);
        nameTMP.gameObject.SetActive(true);
    }
    private void HideUI()
    {
        lifeBar.gameObject.SetActive(false);
        nameTMP.gameObject.SetActive(false);
    }
}

