using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTarget : MonoBehaviour
{
    [SerializeField] private Slider lifeBar;
    [SerializeField] private TextMeshProUGUI nameTMP;

    [Header("Debug")]
    [SerializeField] private bool enableClientLogs = true;

    private NetworkEntity currentTarget;

    private void Update()
    {
        if (Camera.main == null)
        {
            HideUI();
            LogClient("Camera.main is null.");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            if (currentTarget != null)
                LogClient("Raycast hit nothing, hiding target UI.");

            currentTarget = null;
            HideUI();
            return;
        }

        NetworkEntity entHit = FindNetworkEntity(hitInfo.collider);

        if (entHit == null)
        {
            if (currentTarget != null)
                LogClient($"Hit {hitInfo.collider.name}, but no NetworkEntity found.");

            currentTarget = null;
            HideUI();
            return;
        }

        if (currentTarget != entHit)
        {
            currentTarget = entHit;
            LogClient($"New target detected: {GetEntityName(entHit)} | netId={entHit.netId} | collider={hitInfo.collider.name}");
        }

        UpdateTargetUI(entHit);
    }

    private void UpdateTargetUI(NetworkEntity entHit)
    {
        if (entHit == null || entHit.StatComp == null)
        {
            HideUI();
            return;
        }

        ShowUI();

        float currentHealth = entHit.StatComp.Get(StatId.CurrentHealth);
        float maxHealth = entHit.StatComp.Get(StatId.MaxHealth);

        if (maxHealth <= 0f)
        {
            lifeBar.value = 0f;
            LogClient($"Target {GetEntityName(entHit)} has MaxHealth <= 0.");
            return;
        }

        lifeBar.value = currentHealth / maxHealth;

        if (nameTMP != null)
            nameTMP.text = entHit.StatComp.Name;
    }

    private NetworkEntity FindNetworkEntity(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        NetworkEntity entity = hitCollider.GetComponent<NetworkEntity>();
        if (entity != null)
            return entity;

        entity = hitCollider.GetComponentInParent<NetworkEntity>();
        if (entity != null)
            return entity;

        entity = hitCollider.GetComponentInChildren<NetworkEntity>();
        if (entity != null)
            return entity;

        Transform root = hitCollider.transform.root;
        if (root != null)
        {
            entity = root.GetComponentInChildren<NetworkEntity>();
            if (entity != null)
                return entity;
        }

        return null;
    }

    private string GetEntityName(NetworkEntity entity)
    {
        if (entity == null)
            return "null";

        if (entity.StatComp != null && !string.IsNullOrWhiteSpace(entity.StatComp.Name))
            return entity.StatComp.Name;

        return entity.name;
    }

    private void ShowUI()
    {
        if (lifeBar != null && !lifeBar.gameObject.activeSelf)
            lifeBar.gameObject.SetActive(true);

        if (nameTMP != null && !nameTMP.gameObject.activeSelf)
            nameTMP.gameObject.SetActive(true);
    }

    private void HideUI()
    {
        if (lifeBar != null && lifeBar.gameObject.activeSelf)
            lifeBar.gameObject.SetActive(false);

        if (nameTMP != null && nameTMP.gameObject.activeSelf)
            nameTMP.gameObject.SetActive(false);
    }

    private void LogClient(string message)
    {
        if (!enableClientLogs)
            return;

        Debug.Log($"[Client][CanvasTarget] {message}");
    }
}