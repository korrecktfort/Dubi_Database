using Dubi.Database;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

namespace Dubi.Database.Editor
{
    public class LockablePropertyField : VisualElement
    {
        VisualElement lockElement = new VisualElement();
        PropertyField propertyField = new PropertyField();
        bool lockState = false;

        public LockablePropertyField()
        {
            base.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            this.lockElement.AddToClassList("lock");
            this.propertyField.style.flexGrow = new StyleFloat(1.0f);

            Add(this.lockElement);
            Add(this.propertyField);
        }

        public LockablePropertyField(Entry entry, string bindingPath) : this()
        {
            Bind(entry, bindingPath);
        }

        public void Bind(Entry entry, string bindingPath)
        {
            this.lockState = entry.IsLocked(bindingPath);

            this.propertyField.bindingPath = bindingPath;
            this.propertyField.Bind(new SerializedObject(entry));

            void SetLockElement(bool locked, bool changeEntry = true)
            {
                this.lockElement.RemoveFromClassList("lock-in");
                this.lockElement.RemoveFromClassList("lock-out");

                if (locked)
                {
                    this.lockElement.AddToClassList("lock-in");
                    this.propertyField.SetEnabled(false);

                    if (changeEntry)
                    {
                        entry.AddLockedPropertyPath(bindingPath);
                        entry.OverrideValueFromSource(bindingPath);
                    }
                }
                else
                {
                    this.lockElement.AddToClassList("lock-out");
                    this.propertyField.SetEnabled(true);

                    if (changeEntry)
                        entry.RemoveLockedPropertyPath(bindingPath);
                }
            }

            if (entry.IsClone())
            {
                SetLockElement(this.lockState, false);
            }
            else
            {
                SetLockElement(false, false);
                this.lockElement.SetEnabled(false);
                this.lockElement.focusable = false;
                this.lockElement.pickingMode = PickingMode.Ignore;
            }

            this.lockElement.RegisterCallback<MouseDownEvent>((e) =>
            {
                this.lockState = !this.lockState;
                SetLockElement(this.lockState);
            });            
        }
    }
}