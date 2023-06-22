using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

namespace Dubi.Database
{
    [CustomPropertyDrawer(typeof(Entry), true)]
    public class EntryDrawer : PropertyDrawer
    {        
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {          
            EditorGUI.BeginProperty(position, label, property);
            Entry entry = property.objectReferenceValue as Entry;            
            position.height = EditorGUIUtility.singleLineHeight;           
            float width = position.width;                  
            
            /// Foldout
            EditorGUI.BeginDisabledGroup(entry == null);            
            EditorGUI.BeginChangeCheck();            
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                Event.current.Use();                
            }

            EditorGUI.EndDisabledGroup();

            /// Object Field for the Scriptable Object              
            position.width = width;            
            EditorGUI.BeginChangeCheck();
            EditorGUI.ObjectField(position, property, new GUIContent(property.displayName));
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();

                if(property.objectReferenceValue == null)
                {
                    property.isExpanded = false;
                    property.serializedObject.ApplyModifiedProperties();                   
                }                
            }
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();


            /// Display Properties
            if(entry != null && property.isExpanded)
            {
                ++EditorGUI.indentLevel;

                List<string> propertyNames = new List<string>();

                foreach(FieldInfo fieldInfo in entry.GetType().GetFields(this.flags))
                {
                    if (fieldInfo.GetCustomAttribute<HideAttribute>() != null)
                        continue;

                    if(fieldInfo.GetCustomAttribute<HideInInspector>() != null)
                        continue;

                    if (fieldInfo.IsPrivate && fieldInfo.GetCustomAttribute<SerializeField>() == null)
                        continue;

                    propertyNames.Add(fieldInfo.Name);
                }

                SerializedObject serializedEntry = new SerializedObject(property.objectReferenceValue);               
                float lastPropHeight = position.height;
                position.y += EditorGUIUtility.standardVerticalSpacing;

                foreach(string propertyName in propertyNames.ToArray())
                {
                    position.y += lastPropHeight;
                    SerializedProperty currentProp = serializedEntry.FindProperty(propertyName);

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(position, currentProp, new GUIContent(currentProp.displayName), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        currentProp.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.ApplyModifiedProperties();
                    }

                    lastPropHeight = EditorGUI.GetPropertyHeight(currentProp) + EditorGUIUtility.standardVerticalSpacing;                    
                }
                                
                --EditorGUI.indentLevel;
            }
          
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Entry entry = property.objectReferenceValue as Entry;

            if (entry != null && property.isExpanded)
            {
                List<string> propertyNames = new List<string>();
                foreach (FieldInfo fieldInfo in entry.GetType().GetFields(this.flags))
                {
                    if (fieldInfo.GetCustomAttribute<HideAttribute>() != null)
                        continue;

                    if (fieldInfo.GetCustomAttribute<HideInInspector>() != null)
                        continue;

                    if (fieldInfo.IsPrivate && fieldInfo.GetCustomAttribute<SerializeField>() == null)
                        continue;

                    propertyNames.Add(fieldInfo.Name);
                }

                SerializedObject serializedEntry = new SerializedObject(property.objectReferenceValue);

                foreach (string propertyName in propertyNames.ToArray())
                {
                    height += EditorGUI.GetPropertyHeight(serializedEntry.FindProperty(propertyName)) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }
    }
}