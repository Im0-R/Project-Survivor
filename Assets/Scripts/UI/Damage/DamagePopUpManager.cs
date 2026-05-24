using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private DamagePopup damagePopupPrefab;

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    public void ShowDamage(Vector3 worldPosition, int damage,    bool isCrit)
    {
        if (damagePopupPrefab == null || canvas == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPosition);

        DamagePopup popup = Instantiate(damagePopupPrefab, canvas.transform);
        popup.transform.position = screenPos;

        popup.Init(damage, isCrit);
    }
}