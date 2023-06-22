using Dubi.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Entry), true)]
public class EntryEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement visualElement = new VisualElement();
        InspectorElement.FillDefaultInspector(visualElement, base.serializedObject, this);
        return visualElement;
    }
}
