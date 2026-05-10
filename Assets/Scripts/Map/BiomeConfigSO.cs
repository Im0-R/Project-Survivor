using UnityEngine;

[CreateAssetMenu(menuName = "Maps/Biome Config")]
public class BiomeConfigSO : ScriptableObject
{
    public string biomeId;

    [Header("Environment")]
    public GameObject environmentPrefab;

    [Header("Lighting")]
    public Color ambientColor = Color.white;
}