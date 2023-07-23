using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Dubi.Database;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Linq;
using UnityEditor.Callbacks;
using System.CodeDom;
using System.IO;

namespace Dubi.Database.Editor
{
    public class DataCollectionWindow : EditorWindow
    {
        static bool enabled = false;
        public static Type currentEntryType = null;        
        public static Entry[] entries = null;
        public static int maxDepth = 2;
        public static int index = -1;

        public static List<string> lockablePaths = new List<string>();

        private void OnEnable()
        {
            enabled = true;
        }

        private void OnDisable()
        {
            enabled = false;
        }

        [DidReloadScripts]
        static void OnScriptsReload()
        {
            if (enabled && !EditorApplication.isPlaying)
            {
                if(DatabaseProperties.Current.LastInstanceID != 0)
                {
                    Entry entry = DatabaseProperties.Current.LastInstanceID.GetEntryFromInstanceID();

                    if (entry == null)
                        return;

                    DatabaseProperties.Current.Path = entry.GetAssetPath();
                    currentEntryType = entry.GetType();

                    entries = DatabaseProperties.Current.Path.GetAllEntries(currentEntryType);
                    lockablePaths = currentEntryType.GetLockablePaths();
                    index = entry.CollectionIndex();

                    GetWindow<DataCollectionWindow>().SetupWindow();
                    return;
                }

                entries = DatabaseProperties.Current.Path.GetAllEntries(currentEntryType);
                
                if (entries == null)
                    return;

                lockablePaths = currentEntryType.GetLockablePaths();
                index = 0;

                GetWindow<DataCollectionWindow>().SetupWindow();
            }
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID)
        {
            currentEntryType = null;
            entries = null;
            index = -1;
            lockablePaths.Clear();

            Entry entry = instanceID.GetEntryFromInstanceID();

            if (entry == null)
                return false;

            DatabaseProperties.Current.LastInstanceID = instanceID;

            DatabaseProperties.Current.Path = entry.GetAssetPath();
            currentEntryType = entry.GetType();

            entries = DatabaseProperties.Current.Path.GetAllEntries(currentEntryType);
            lockablePaths = currentEntryType.GetLockablePaths();
            index = entry.CollectionIndex();

            GetWindow<DataCollectionWindow>().SetupWindow();

            return true;
        }

        public void SetupWindow()
        {
            this.rootVisualElement.Clear();
            Resources.Load<VisualTreeAsset>("DatabaseWindow").CloneTree(base.rootVisualElement);
            base.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("DatabaseUSS"));

            VisualElement leftColumn = base.rootVisualElement.Q<VisualElement>("Entries__Left");

            VisualElement rightColumn = base.rootVisualElement.Q<VisualElement>("Entries__Right");

            ListView listView = new ListView(entries.ToList(), 22);
            listView.showAddRemoveFooter = true;
            listView.AddToClassList("listView");

            void UpdateListView()
            {
                listView.itemsSource = entries.ToList();
                listView.RefreshItems();
            }

            void SelectIndex(int index)
            {
                index = Mathf.Clamp(index, 0, entries.Length + 1);
                listView.selectedIndex = index;
            }

            listView.makeItem = () =>
            {
                return new EntryListElement() { updateListView = UpdateListView, selectIndex = SelectIndex };
            };

            listView.bindItem = (e, i) =>
            {
                if (i >= entries.Length)
                    return;

                EntryListElement element = e as EntryListElement;
                element.Bind(entries[i]);
            };

            listView.selectionChanged += (s) =>
            {
                rightColumn.Unbind();
                rightColumn.Clear();
                                
                listView.Query<RenameElement>().ForEach(e =>
                {
                    e.OverrideOnClose();
                });

                if (listView.HasValidSelection())
                {
                    /// Right Column Setup            
                    Entry entry = entries[listView.selectedIndex];

                    foreach (string path in lockablePaths)
                        rightColumn.Add(new LockablePropertyField(entry, path));

                    if (entry.HasClones())
                    {
                        SerializedObject serializedEntry = new SerializedObject(entry);
                        rightColumn.Bind(serializedEntry);
                        rightColumn.TrackSerializedObjectValue(serializedEntry, (e) =>
                        {
                            entry.OverrideLockedDataInClones();
                        });
                    }

                    index = listView.selectedIndex;
                }
                else
                    index = -1;
            };

            listView.AddButtonClickable(() =>
            {
                Entry entry = ScriptableObject.CreateInstance(currentEntryType.ToString()) as Entry;
                entry.SetBaseName();
                entry.AddUniqueSuffix();
                entry.SetUniqueID();
                entry.CreateAsset();
                entry.AddToCollection();

                UpdateListView();
                listView.selectedIndex = entry.CollectionIndex();
            });

            listView.RemoveButtonClickable(() =>
            {
                if (entries.Length <= 0)
                    return;

                int index = listView.HasValidSelection() ? listView.selectedIndex : listView.itemsSource.Count - 1;
                Entry entry = entries[index];
                entry.PutClonesOneDepthUp();

                entry.ClearRelations();
                entry.DeleteAsset();
                entry.RemoveFromCollection();

                listView.itemsSource = entries.ToList();
                listView.RefreshItems();
                listView.selectedIndex = Mathf.Max(0, index - 1);
            });

            listView.selectedIndex = index;

            leftColumn.Add(listView);
        }
    }
}