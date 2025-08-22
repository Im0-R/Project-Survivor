using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class SpellsManager : MonoBehaviour
{
    public class SpellProjStats
    {
        public Image UIImage;
        public string spellName;
        public float damage;
        public float speed;
        public float cooldown;
        public float range;
        public int manaCost;
        public SpellProjStats(Image Image,string name, float dmg, float spd, float cd, float rng, int mana)
        {
            UIImage = Image;
            spellName = name;
            damage = dmg;
            speed = spd;
            cooldown = cd;
            range = rng;
            manaCost = mana;
        }
    }
    public static SpellsManager Instance { get; private set; }

    [Header("Projectiles")]
    public GameObject[] projsSpellsGO;
    public GameObject fireballPrefab;
    public GameObject iceboltPrefab;

    [Header("Autres sorts")]
    public GameObject healPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optionnel si tu veux qu'il survive aux scènes
    }

    public GameObject GetPrefab(string spellName)
    {
        return spellName switch
        {
            "Fireball" => fireballPrefab,
            "Icebolt" => iceboltPrefab,
            "Heal" => healPrefab,
            _ => null
        };
    }

    //public Spell GetSpell()
    //{
    //    return new Spell();
    //}
}
