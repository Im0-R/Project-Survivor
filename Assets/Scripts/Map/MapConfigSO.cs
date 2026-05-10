using UnityEngine;

[CreateAssetMenu(menuName = "Maps/Map Config")]
public class MapConfigSO : ScriptableObject
{
    public string mapId;

    public BiomeConfigSO biome;

    public int monsterLevel;

    public GameObject[] possibleMonsterPrefabs;
}