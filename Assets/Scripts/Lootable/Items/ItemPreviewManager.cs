using UnityEngine;

public class ItemPreviewManager : MonoBehaviour
{
    public static ItemPreviewManager Instance;

    [SerializeField] private GameObject itemPreviewPrefab;
    [SerializeField] private GameObject parentCanvas;

    [Header("Position")]
    [SerializeField] private float spacing = 20f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void InitPreview(InventoryItemData data, RectTransform sourceRect)
    {
        ClosePreview();

        if (itemPreviewPrefab == null || parentCanvas == null)
        {
            Debug.LogError("[ItemPreviewManager] Missing prefab or parentCanvas.");
            return;
        }

        GameObject preview = Instantiate(itemPreviewPrefab, parentCanvas.transform);

        ItemPreview itemPreview = preview.GetComponent<ItemPreview>();

        if (itemPreview == null)
        {
            Debug.LogError("[ItemPreviewManager] Preview prefab has no ItemPreview component.");
            Destroy(preview);
            return;
        }

        itemPreview.Init(data);

        RectTransform previewRect = preview.GetComponent<RectTransform>();

        if (previewRect != null && sourceRect != null)
            PositionPreview(previewRect, sourceRect);
    }

    public void ClosePreview()
    {
        if (parentCanvas == null)
            return;

        foreach (Transform child in parentCanvas.transform)
        {
            if (child.GetComponent<ItemPreview>() != null)
                Destroy(child.gameObject);
        }
    }

    private void PositionPreview(RectTransform previewRect, RectTransform sourceRect)
    {
        Canvas.ForceUpdateCanvases();

        Vector3[] itemCorners = new Vector3[4];
        sourceRect.GetWorldCorners(itemCorners);

        Vector3[] previewCorners = new Vector3[4];
        previewRect.GetWorldCorners(previewCorners);

        float previewWidth = previewCorners[2].x - previewCorners[0].x;

        Vector3 rightCenter = (itemCorners[2] + itemCorners[3]) * 0.5f;
        Vector3 leftCenter = (itemCorners[0] + itemCorners[1]) * 0.5f;

        Vector3 wantedPosition = rightCenter + new Vector3(spacing, 0f, 0f);

        if (wantedPosition.x + previewWidth > Screen.width)
            wantedPosition = leftCenter - new Vector3(previewWidth + spacing, 0f, 0f);

        previewRect.position = wantedPosition;
    }
}