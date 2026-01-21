using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class ItemPreviewManager : MonoBehaviour
{
    // singleton instance
    public static ItemPreviewManager Instance;

    public GameObject itemPreviewPrefab;

    public Canvas parentCanvas;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitPreview(ItemInstance itemInstance )
    {
        GameObject preview = Instantiate(itemPreviewPrefab, parentCanvas.transform);
        ItemPreview itemPreview = preview.GetComponent<ItemPreview>();
        itemPreview.Init(itemInstance);
    }
    public void ClosePreview()
    {
        foreach (Transform child in parentCanvas.transform)
        {
            ItemPreview itemPreview = child.GetComponent<ItemPreview>();
            if (itemPreview != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
