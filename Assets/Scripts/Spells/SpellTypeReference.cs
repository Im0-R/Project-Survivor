using System;
using UnityEngine;

[Serializable]
public class SpellTypeReference
{
    [SerializeField] private string className;

    public Type SpellType
    {
        get
        {
            if (string.IsNullOrEmpty(className)) return null;
            return Type.GetType(className);
        }
    }

    public void SetType(Type type)
    {
        className = type.AssemblyQualifiedName;
    }

    public string ClassName => className;
}
