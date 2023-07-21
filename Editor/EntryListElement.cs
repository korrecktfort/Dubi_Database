using UnityEditor;
using UnityEngine;
using Dubi.Database;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace Dubi.Database.Editor
{
    public class EntryListElement : VisualElement
    {
        float relatedGap = 5.0f;

        TextElement id = new TextElement() { bindingPath = "id" };
        TextElement displayName = new TextElement() { bindingPath = "displayName" };
        RenameElement renameElement = new RenameElement();
        Button duplicate = new Button() { text = "+", tooltip = "Duplicate Entry" };
        Button copyAsRelated = new Button() { text = "r", tooltip = "Copy as Related" };
        Button delete = new Button() { text = "-", tooltip = "Delete Entry" };
        Button makeUnique = new Button() { text = "u", tooltip = "Make Entry Unique" };
        VisualElement textRow = new VisualElement();
        VisualElement buttonRow = new VisualElement();
        VisualElement container = new VisualElement();

        public Action updateListView = null;
        public Action<int> selectIndex = null;

        Entry entry = null;

        public EntryListElement()
        {
            this.container.AddToClassList("listViewElement__contentContainer");

            this.textRow.AddToClassList("listViewElement__contentContainer__textRow");
            this.buttonRow.AddToClassList("listViewElement__contentContainer__buttonRow");

            this.id.AddToClassList("listViewElement__contentContainer__textRow__id");
            this.displayName.AddToClassList("listViewElement__contentContainer__textRow__text");

            this.duplicate.AddToClassList("listViewElement__contentContainer__buttonRow__button");
            this.copyAsRelated.AddToClassList("listViewElement__contentContainer__buttonRow__button");
            this.delete.AddToClassList("listViewElement__contentContainer__buttonRow__button");
            this.makeUnique.AddToClassList("listViewElement__contentContainer__buttonRow__button");

            this.textRow.Add(this.id);
            this.textRow.Add(this.displayName);
            this.textRow.Add(this.renameElement);

            this.buttonRow.Add(this.duplicate);
            this.buttonRow.Add(this.copyAsRelated);
            this.buttonRow.Add(this.delete);
            this.buttonRow.Add(this.makeUnique);

            this.container.Add(this.textRow);
            this.container.Add(this.buttonRow);

            Add(this.container);
        }

        private void KeyDown(KeyDownEvent evt)
        {
            if(evt.keyCode == KeyCode.F2)
            {
                InitRenaming(evt);                
            }
        }

        public void Bind(Entry entry)
        {
            this.entry = entry;
            int depth = entry.GetRelatedDepth();
            SetButtonVisibleState(entry, depth);
            this.textRow.style.marginLeft = new StyleLength(this.relatedGap * depth);

            SerializedObject serializedObject = new SerializedObject(entry);
            serializedObject.FindProperty("displayName").stringValue = entry.name;
            serializedObject.ApplyModifiedProperties();

            this.id.Bind(serializedObject);
            this.displayName.Bind(serializedObject);
            this.displayName.focusable = true;

            DepthStyling();

            this.duplicate.clickable = new Clickable(() =>
            {
                Entry copy = entry.GetUniqueCopy();

                /// Dubplicate Stack as well
                if (Event.current.modifiers == EventModifiers.Alt)
                {

                    return;
                }

                copy.ClearClones();

                if (copy.IsClone())
                    copy.InsertMeAtCloneIndex(entry.GetMyCloneIndex() + 1);

                if (entry.HasClones())
                    copy.InsertInCollectionAt(entry.GetLastClone().CollectionIndex() + 1);
                else
                    copy.InsertInCollectionAt(entry.CollectionIndex() + 1);

                this.updateListView.Invoke();
                this.selectIndex.Invoke(copy.CollectionIndex());
            });

            this.copyAsRelated.clickable = new Clickable(() =>
            {
                int index = entry.GetVeryLastClone().CollectionIndex() + 1;
                Entry copy = entry.GetUniqueCopy();
                copy.ClearRelations();
                copy.SetSource(entry);
                copy.InsertInCollectionAt(index);

                this.updateListView.Invoke();
                this.selectIndex.Invoke(copy.CollectionIndex());
            });

            this.delete.clickable = new Clickable(() =>
            {
                int index = entry.CollectionIndex();

                if (Event.current.modifiers == EventModifiers.Alt)
                {
                    return;
                }

                entry.PutClonesOneDepthUp();

                entry.ClearRelations();
                entry.DeleteAsset();
                entry.RemoveFromCollection();
                
                this.updateListView.Invoke();
                this.selectIndex.Invoke(Mathf.Max(0, index - 1));
            });

            this.makeUnique.clickable = new Clickable(() =>
            {
                entry.RemoveFromCollection().InsertInCollectionAt(entry.TopSource().GetVeryLastClone().CollectionIndex() + 1);
                entry.ClearSource();
                entry.SortClones();
                entry.CollectionInsertClonesAfterMe();

                this.updateListView.Invoke();
                this.selectIndex.Invoke(entry.CollectionIndex());
            });

            this.displayName.RegisterCallback<MouseDownEvent>((e) =>
            {
                if (e.button == 0)
                {
                    InitRenaming(e);
                    e.StopImmediatePropagation();
                    Event.current.Use();
                }
            });
        }

        void InitRenaming(EventBase e)
        {
            void OnClose()
            {
                this.renameElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                this.displayName.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                this.buttonRow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            }

            this.renameElement.Bind(entry, OnClose);

            this.renameElement.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            this.displayName.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            this.buttonRow.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            e.StopImmediatePropagation();
            Event.current.Use();
        }

        void SetButtonVisibleState(Entry entry, int depth)
        {
            if (entry.sourceID < 0)
            {
                this.makeUnique.SetEnabled(false);
                this.copyAsRelated.SetEnabled(true);
            }
            else
            {
                if (depth <= DataCollectionWindow.maxDepth)
                {
                    this.makeUnique.SetEnabled(true);
                    this.copyAsRelated.SetEnabled(true);
                }
                else
                {
                    this.makeUnique.SetEnabled(true);
                    this.copyAsRelated.SetEnabled(false);
                }
            }
        }

        public void DepthStyling()
        {
            RemoveFromClassList("listViewElement__contentContainer--unrelated");
            RemoveFromClassList("listViewElement__contentContainer--sourceEntry");
            RemoveFromClassList("listViewElement__contentContainer--related");
            RemoveFromClassList("listViewElement__contentContainer--relatedLast");

            this.container.RemoveFromClassList("listViewElement__contentContainer--sub-related");
            this.container.RemoveFromClassList("listViewElement__contentContainer--sourceEntry");
            this.container.RemoveFromClassList("listViewElement__contentContainer--related");
            this.container.RemoveFromClassList("listViewElement__contentContainer--relatedLast");

            Entry source = this.entry.GetSourceEntry();
            Entry[] topSources = EntryExtensions.TopSources(this.entry);

            /// If Sub Related
            if (this.entry.IsSubRelated() || source.IsClone())
            {
                this.container.AddToClassList("listViewElement__contentContainer--sub-related");

                if (this.entry.HasClones())
                    this.container.AddToClassList("listViewElement__contentContainer--sourceEntry");

                if (this.entry.IsClone())
                {
                    this.container.AddToClassList("listViewElement__contentContainer--related");
                    AddToClassList("listViewElement__contentContainer--related");
                }

                if (this.entry.IsLastCloneOf(source) && !this.entry.HasClones())
                    this.container.AddToClassList("listViewElement__contentContainer--relatedLast");

                /// Last element in sub-realated and main-related
                if (this.entry.IsLastCloneOf(source) && source.IsLastCloneOfAny(topSources))
                    AddToClassList("listViewElement__contentContainer--relatedLast");
            }
            else
            {
                if (this.entry.HasClones())
                    AddToClassList("listViewElement__contentContainer--sourceEntry");

                /// Related Entry
                if (this.entry.IsClone())
                    AddToClassList("listViewElement__contentContainer--related");

                /// Last Related Entry
                if (this.entry.IsLastCloneOf(source))
                    AddToClassList("listViewElement__contentContainer--relatedLast");

                if (!entry.IsClone() && !entry.HasClones())
                    AddToClassList("listViewElement__contentContainer--unrelated");
            }
        }
    }
}