using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float moveSpeed = 60f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private AnimationCurve scaleCurve;

    private float timer;

    public void Init(int damage, bool isCrit)
    {
        text.text = damage.ToString();

        if (isCrit)
        {
            text.fontSize = 42;
            text.color = Color.yellow;
        }
        else
        {
            text.fontSize = 30;
            text.color = Color.white;
        }

        timer = lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        timer -= Time.deltaTime;

        float t = 1f - timer / lifetime;
        transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

        if (timer <= 0f)
            Destroy(gameObject);
    }
}