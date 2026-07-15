using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemPreviewManager : MonoBehaviour
{
    public static ItemPreviewManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject itemPreviewPrefab;
    [SerializeField] private GameObject parentCanvas;

    [Header("Position")]
    [SerializeField] private float spacing = 20f;

    private GameObject currentPreview;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
            Destroy(gameObject);
    }

    private void Update()
    {
        if (currentPreview == null || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame ||
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClosePreview();
        }
    }

    private void OnDisable()
    {
        ClosePreview();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitPreview(InventoryItemData data, RectTransform sourceRect)
    {
        ClosePreview();

        if (data == null)
        {
            Debug.LogError("[ItemPreviewManager] Cannot display null item data.");
            return;
        }

        if (itemPreviewPrefab == null || parentCanvas == null)
        {
            Debug.LogError(
                "[ItemPreviewManager] Missing itemPreviewPrefab or parentCanvas.",
                this
            );

            return;
        }

        currentPreview = Instantiate(
            itemPreviewPrefab,
            parentCanvas.transform
        );

        ItemPreview itemPreview = currentPreview.GetComponent<ItemPreview>();

        if (itemPreview == null)
        {
            Debug.LogError(
                "[ItemPreviewManager] Preview prefab has no ItemPreview component.",
                currentPreview
            );

            Destroy(currentPreview);
            currentPreview = null;
            return;
        }

        itemPreview.Init(data);

        RectTransform previewRect =
            currentPreview.GetComponent<RectTransform>();

        if (previewRect == null || sourceRect == null)
            return;

        // Force l'UI à calculer la taille finale de la preview,
        // notamment avec les ContentSizeFitter et LayoutGroup.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(previewRect);
        Canvas.ForceUpdateCanvases();

        PositionPreview(previewRect, sourceRect);
    }

    public void ClosePreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        /*
         * Sécurité pour supprimer une éventuelle ancienne preview
         * qui n'aurait pas été enregistrée dans currentPreview.
         */
        if (parentCanvas == null)
            return;

        for (int i = parentCanvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = parentCanvas.transform.GetChild(i);

            if (child.GetComponent<ItemPreview>() != null)
                Destroy(child.gameObject);
        }
    }

    private void PositionPreview(
        RectTransform previewRect,
        RectTransform sourceRect
    )
    {
        Canvas.ForceUpdateCanvases();

        Vector3[] itemCorners = new Vector3[4];
        sourceRect.GetWorldCorners(itemCorners);

        Vector3[] previewCorners = new Vector3[4];
        previewRect.GetWorldCorners(previewCorners);

        float previewWidth =
            previewCorners[2].x - previewCorners[0].x;

        Vector3 rightCenter =
            (itemCorners[2] + itemCorners[3]) * 0.5f;

        Vector3 leftCenter =
            (itemCorners[0] + itemCorners[1]) * 0.5f;

        Vector3 wantedPosition =
            rightCenter + new Vector3(spacing, 0f, 0f);

        // Si la preview dépasse à droite, on la place à gauche.
        if (wantedPosition.x + previewWidth > Screen.width)
        {
            wantedPosition =
                leftCenter - new Vector3(
                    previewWidth + spacing,
                    0f,
                    0f
                );
        }

        previewRect.position = wantedPosition;
    }
}