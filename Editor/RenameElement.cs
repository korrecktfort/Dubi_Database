using Dubi.Database;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dubi.Database.Editor
{
    class RenameElement : VisualElement
    {
        Button confirm = new Button() { text = "Rename" };
        Button cancel = new Button() { text = "Cancel" };
        TextField textField = new TextField() { focusable = true };
        VisualElement buttonSpacer = new VisualElement();

        Action OnClose;

        public RenameElement()
        {
            base.AddToClassList("listViewElement__renameElement");
            this.textField.AddToClassList("listViewElement__renameElement__renameField");
            this.confirm.AddToClassList("listViewElement__renameElement__renameButton");
            this.cancel.AddToClassList("listViewElement__renameElement__renameButton");
            this.buttonSpacer.AddToClassList("listViewElement__renameElement__buttonSpacer");

            Add(this.textField);
            Add(this.confirm);
            Add(this.buttonSpacer);
            Add(this.cancel);

            base.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        public void Bind(Entry entry, Action OnClose)
        {
            this.OnClose = OnClose;
            SerializedObject serializedEntry = new SerializedObject(entry);
            SerializedProperty displayName = serializedEntry.FindProperty("displayName");

            this.textField.value = displayName.stringValue;
            this.textField.SelectRange(0, textField.value.Length);

            void Confirm()
            {
                string value = this.textField.value;

                displayName.stringValue = value;
                serializedEntry.ApplyModifiedProperties();

                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(entry), value);

                this.OnClose?.Invoke();
            }

            this.textField.RegisterCallback<MouseDownEvent>((e) =>
            {
                if (e.button == 0)
                {
                    this.textField.SelectAll();

                    if (e.target == this.textField)
                        e.StopImmediatePropagation();
                }
            });

            this.textField.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.keyCode == KeyCode.Return)
                {
                    Confirm();
                }
            });

            this.confirm.clickable = new Clickable(() =>
            {
                Confirm();
            });

            this.cancel.clickable = new Clickable(() =>
            {
                this.OnClose?.Invoke();
            });
        }

        public void OverrideOnClose()
        {
            this.OnClose?.Invoke();
        }
    }
}