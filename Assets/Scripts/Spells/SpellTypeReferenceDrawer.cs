using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(SpellTypeReference))]
public class SpellTypeReferenceDrawer : PropertyDrawer
{
    private List<Type> spellTypes;
    private string[] spellTypeNames;

    private void Init()
    {
        if (spellTypes != null) return;

        var spellType = typeof(Spell);
        spellTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(t => spellType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        spellTypeNames = spellTypes.Select(t => t.Name).ToArray();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Init();

        var classNameProp = property.FindPropertyRelative("className");
        var currentType = !string.IsNullOrEmpty(classNameProp.stringValue)
            ? Type.GetType(classNameProp.stringValue)
            : null;

        int currentIndex = currentType != null ? spellTypes.IndexOf(currentType) : -1;
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, spellTypeNames);

        if (newIndex >= 0 && newIndex < spellTypes.Count)
        {
            classNameProp.stringValue = spellTypes[newIndex].AssemblyQualifiedName;
        }
    }
}
