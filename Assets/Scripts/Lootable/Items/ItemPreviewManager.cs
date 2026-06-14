using UnityEngine;

public class ItemPreviewManager : MonoBehaviour
{
    public static ItemPreviewManager Instance;

    [SerializeField] private GameObject itemPreviewPrefab;
    [SerializeField] private GameObject parentCanvas;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void InitPreview(InventoryItemData data)
    {
        ClosePreview();

        GameObject preview = Instantiate(itemPreviewPrefab, parentCanvas.transform);
        ItemPreview itemPreview = preview.GetComponent<ItemPreview>();

        if (itemPreview == null)
        {
            Debug.LogError("[ItemPreviewManager] Preview prefab has no ItemPreview component.");
            return;
        }

        itemPreview.Init(data);
    }

    public void ClosePreview()
    {
        foreach (Transform child in parentCanvas.transform)
        {
            if (child.GetComponent<ItemPreview>() != null)
                Destroy(child.gameObject);
        }
    }
}