using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Dubi.Database;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor.VersionControl;

namespace Dubi.Database.Editor
{
    public static class EntryExtensions
    {
        public static Entry GetEntryFromInstanceID(this int instanceID)
        {
            return EditorUtility.InstanceIDToObject(instanceID) as Entry;
        }

        public static string GetAssetPath(this Entry entry)
        {
            string assetpath = AssetDatabase.GetAssetPath(entry);
            assetpath = assetpath[..(assetpath.LastIndexOf("/") + 1)].Trim();
            return assetpath;
        }

        public static Entry[] GetAllEntries(this string path, Type type)
        {
            if (path == "")
                return null;

            if (type == null)
                return null;

            string[] paths = Directory.GetFiles(path);

            List<Entry> list = new List<Entry>();
            foreach (string subPath in paths)
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(subPath, type);
                if (asset != null)
                    list.Add(asset as Entry);
            }

            Entry[] array = list.ToArray();
            Array.Sort(array, EntryExtensions.CompareByIDAsc);
            return array.Sort();
        }

        public static void PutClonesOneDepthUp(this Entry entry)
        {
            if (!entry.HasClones())
                return;

            Entry newSource = null;
            int index = entry.CollectionIndex();

            if (entry.IsClone())
            {
                newSource = entry.GetSourceEntry();
                index = entry.TopSource().GetVeryLastClone().CollectionIndex();
            }

            entry.cloneIDs.Reverse();

            foreach (int id in entry.cloneIDs)
            {
                Entry clone = GetEntry(id);
                clone.SetSource(newSource);
                clone.RemoveFromCollection().InsertInCollectionAt(index);
                clone.PutClonesOneDepthUp();
            }
        }

        public static Entry[] Sort(this Entry[] entries)
        {
            Entry GetEntryLocal(int id)
            {
                foreach (Entry e in entries)
                    if (e.id == id)
                        return e;

                return null;
            }

            /// Cleanup Source and Clone relations
            foreach (Entry entry in entries)
            {
                if (entry.IsClone())
                {
                    Entry source = GetEntryLocal(entry.sourceID);

                    if (source != null)
                    {
                        if (!source.cloneIDs.Contains(entry.id))
                            source.cloneIDs.Add(entry.id);
                    }
                    else
                        entry.sourceID = -1;
                }

                if (entry.HasClones())
                {
                    for (int i = entry.cloneIDs.Count - 1; i >= 0; --i)
                    {
                        Entry clone = GetEntryLocal(entry.cloneIDs[i]);
                        if (clone == null)
                            entry.cloneIDs.RemoveAt(i);
                        else
                            clone.sourceID = entry.id;
                    }

                    entry.SortClones();
                }
            }

            List<Entry> entriesList = entries.ToList();
            List<Entry> list = new List<Entry>();

            int mainIndex = 0;

            /// For recursive sorting in
            void SortInClones(Entry entry)
            {
                foreach (int id in entry.cloneIDs)
                {
                    Entry clone = GetEntryLocal(id);
                    AddToList(clone);

                    if (clone.HasClones())
                        SortInClones(clone);
                }
            }

            /// counts forward while adding entries
            void AddToList(Entry entry)
            {
                entriesList.Remove(entry);
                list.Insert(mainIndex, entry);
                ++mainIndex;
            }

            do
            {
                Entry entry = entriesList[0];
                entriesList.RemoveAt(0);

                if (entry.IsClone())
                    break;

                AddToList(entry);

                if (entry.HasClones())
                    SortInClones(entry);

            } while (entriesList.Count > 0);

            return list.ToArray();
        }

        public static bool HasValidSelection(this ListView listView)
        {
            return listView.selectedIndex > -1 && listView.selectedIndex < listView.itemsSource.Count;
        }

        public static void AddToCollection(this Entry entry)
        {
            List<Entry> list = DataCollectionWindow.entries.ToList();
            list.Add(entry);
            DataCollectionWindow.entries = list.ToArray();
        }

        public static void AddButtonClickable(this ListView listView, Action OnAdd)
        {
            listView.Q<Button>("unity-list-view__add-button").clickable = new Clickable(OnAdd);
        }

        public static void RemoveButtonClickable(this ListView listView, Action OnRemove)
        {
            listView.Q<Button>("unity-list-view__remove-button").clickable = new Clickable(OnRemove);
        }

        public static void OverrideLockedDataInClones(this Entry entry)
        {
            if (!entry.HasClones())
                return;

            SerializedObject serializedEntry = new SerializedObject(entry);

            foreach (int cloneID in entry.cloneIDs)
            {
                Entry clone = GetEntry(cloneID);
                foreach (string path in clone.lockedEntries)
                {
                    SerializedObject serializedClone = new SerializedObject(clone);
                    serializedClone.CopyFromSerializedProperty(serializedEntry.FindProperty(path));
                    serializedClone.ApplyModifiedProperties();
                }
            }
        }

        public static SerializedObject[] GetDirectSerializedClones(this Entry entry)
        {
            if (!entry.HasClones())
                return null;

            int length = entry.cloneIDs.Count;
            SerializedObject[] array = new SerializedObject[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = new SerializedObject(GetEntry(entry.cloneIDs[i]));
            }
            return array;
        }

        public static void OverrideValueFromSource(this Entry entry, string bindingPath)
        {
            if (!entry.IsClone())
                return;

            SerializedObject serializedEntry = new SerializedObject(entry);
            SerializedObject serializedSource = entry.GetSerializedSource();

            serializedEntry.CopyFromSerializedProperty(serializedSource.FindProperty(bindingPath));
            serializedEntry.ApplyModifiedProperties();
        }

        public static void RemoveLockedPropertyPath(this Entry entry, string propertyPath)
        {
            if (!entry.IsLocked(propertyPath))
                return;

            SerializedObject serializedEntry = new SerializedObject(entry);
            SerializedProperty array = serializedEntry.FindProperty("lockedEntries");
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).stringValue == propertyPath)
                {
                    array.DeleteArrayElementAtIndex(i);
                    array.serializedObject.ApplyModifiedProperties();
                    break;
                }
            }
        }

        public static void AddLockedPropertyPath(this Entry entry, string propertyPath)
        {
            if (entry.IsLocked(propertyPath))
                return;

            SerializedObject serializedEntry = new SerializedObject(entry);
            SerializedProperty array = serializedEntry.FindProperty("lockedEntries");

            array.arraySize++;
            array.serializedObject.ApplyModifiedProperties();

            array.GetArrayElementAtIndex(array.arraySize - 1).stringValue = propertyPath;
            array.serializedObject.ApplyModifiedProperties();
        }

        public static bool IsLocked(this Entry entry, string bindingPath)
        {
            if (entry.IsClone())
                foreach (string path in entry.lockedEntries)
                    if (path == bindingPath)
                        return true;

            return false;
        }

        public static List<string> GetHidePaths(this Type type)
        {
            List<string> list = new List<string>();

            list.Add("m_Script");
            list.Add("m_ObjectHideFlags");

            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                if (fieldInfo.GetCustomAttribute<HideAttribute>() != null)
                    list.Add(fieldInfo.Name);

            return list;
        }

        public static List<string> GetLockablePaths(this Type type)
        {
            List<string> list = new List<string>();

            do
            {
                foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                {
                    if (fieldInfo.GetCustomAttribute<HideAttribute>() != null)
                        continue;

                    if(!list.Contains(fieldInfo.Name))
                        list.Add(fieldInfo.Name);
                }

                type = type.BaseType;

            } while (type != typeof(Entry));

            return list;
        }

        public static void CollectionInsertClonesAfterMe(this Entry entry)
        {
            if (!entry.HasClones())
                return;

            int index = entry.CollectionIndex();
            List<Entry> list = DataCollectionWindow.entries.ToList();

            for (int i = 0; i < entry.cloneIDs.Count; i++)
            {
                Entry clone = GetEntry(entry.cloneIDs[i]);
                list.Remove(clone);
                list.Insert(index + i + 1, clone);
            }

            DataCollectionWindow.entries = list.ToArray();
        }

        public static void SortClones(this Entry entry)
        {
            if (!entry.HasClones())
                return;

            int[] sortedCloneIDs = entry.cloneIDs.ToArray();
            Array.Sort(sortedCloneIDs, CompareByAsc);
            SerializedObject serializedEntry = new SerializedObject(entry);
            SerializedProperty cloneIDs = serializedEntry.FindProperty("cloneIDs");
            for (int i = 0; i < sortedCloneIDs.Length; i++)
            {
                cloneIDs.GetArrayElementAtIndex(i).intValue = sortedCloneIDs[i];
            }
            serializedEntry.ApplyModifiedProperties();
        }

        public static int CompareByIDAsc(Entry left, Entry right)
        {
            if (left.id > right.id) return 1;
            if (left.id < right.id) return -1;
            return 0;
        }

        public static int CompareByAsc(int left, int right)
        {
            if (left > right) return 1;
            if (left < right) return -1;
            return 0;
        }

        public static void ClearRelations(this Entry entry)
        {
            if (entry.IsClone())
                entry.ClearSource();

            if (entry.HasClones())                            
                entry.ClearClones();
            
        }

        public static void DeleteAsset(this Entry entry)
        {
            string path = AssetDatabase.GetAssetPath(entry);

            if (AssetDatabase.LoadAssetAtPath(path, entry.GetType()).GetInstanceID() == DatabaseProperties.Current.LastInstanceID)
                DatabaseProperties.Current.LastInstanceID = 0;

            AssetDatabase.DeleteAsset(path);
        }

        public static void SetSource(this Entry entry, Entry source)
        {
            SerializedObject serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("sourceID").intValue = source != null ? source.id : -1;
            serializedEntry.ApplyModifiedProperties();

            if (source != null)
                entry.InsertMeAtCloneIndex(source.cloneIDs.Count);
        }

        public static Entry GetLastClone(this Entry entry)
        {
            if (!entry.HasClones())
                return entry;

            return GetEntry(entry.cloneIDs[entry.cloneIDs.Count - 1]);
        }

        public static Entry GetVeryLastClone(this Entry entry)
        {
            if (!entry.HasClones()) return entry;

            Entry lastClone = entry.GetLastClone();

            if (!lastClone.HasClones())
                return lastClone;

            do
            {
                lastClone = lastClone.GetLastClone();
            } while (lastClone.HasClones());

            return lastClone;
        }

        public static Entry RemoveFromCollection(this Entry entry)
        {
            List<Entry> list = DataCollectionWindow.entries.ToList();
            list.Remove(entry);
            DataCollectionWindow.entries = list.ToArray();
            return entry;
        }

        public static Entry InsertInCollectionAt(this Entry entry, int index)
        {
            List<Entry> list = DataCollectionWindow.entries.ToList();
            list.Insert(index, entry);
            DataCollectionWindow.entries = list.ToArray();
            return entry;
        }

        /// <summary>
        /// Cancels if entry is not a clone
        /// </summary>
        public static void InsertMeAtCloneIndex(this Entry entry, int cloneIndex)
        {
            if (!entry.IsClone())
                return;

            SerializedObject serializedSource = entry.GetSerializedSource();
            SerializedProperty cloneIDs = serializedSource.FindProperty("cloneIDs");
            cloneIDs.InsertArrayElementAtIndex(cloneIndex);
            serializedSource.ApplyModifiedProperties();
            cloneIDs.GetArrayElementAtIndex(cloneIndex).intValue = entry.id;
            serializedSource.ApplyModifiedProperties();
        }

        public static SerializedObject GetSerializedSource(this Entry entry)
        {
            if (entry.IsClone())
                return new SerializedObject(entry.GetSourceEntry());

            return null;
        }

        public static int GetMyCloneIndex(this Entry entry)
        {
            return entry.GetSourceEntry().GetCloneIndex(entry);
        }

        public static int GetCloneIndex(this Entry source, Entry clone)
        {
            return source.cloneIDs.IndexOf(clone.id);
        }

        public static int CollectionIndex(this Entry entry)
        {
            return DataCollectionWindow.entries.ToList().IndexOf(entry);
        }

        public static SerializedObject GetSerializedCopyOf(this Entry entry)
        {
            Entry copy = entry.GetUniqueCopy();

            SerializedObject serializedCopy = new SerializedObject(copy);
            entry.CopyLockableDataTo(serializedCopy);
            return serializedCopy;
        }

        public static Entry GetUniqueCopy(this Entry entry)
        {
            Entry copy = ScriptableObject.Instantiate<Entry>(entry);
            copy.AddUniqueSuffix();
            copy.SetUniqueID();
            copy.CreateAsset();
            return copy;
        }

        public static void CopyLockableDataTo(this Entry entry, SerializedObject target)
        {
            SerializedObject source = new SerializedObject(entry);

            foreach (string path in entry.lockedEntries)
                target.CopyFromSerializedProperty(source.FindProperty(path));

            target.ApplyModifiedProperties();
        }

        public static void CreateAsset(this Entry entry)
        {
            AssetDatabase.CreateAsset(entry, DatabaseProperties.Current.Path + entry.displayName + ".asset");
            // AssetDatabase.Refresh();
        }

        public static void AddUniqueSuffix(this Entry entry)
        {
            string name = entry.displayName;

            if (name == "")
                name = entry.GetType().ToString();

            if (name.Contains("~"))
                name = name[..name.LastIndexOf("~")];

            name += "~" + Guid.NewGuid().ToString()[..5];

            SerializedObject serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("displayName").stringValue = name;
            serializedEntry.ApplyModifiedProperties();
        }

        public static void ClearSource(this Entry entry)
        {
            SerializedObject serializedSource = entry.GetSerializedSource();
            SerializedProperty cloneIDs = serializedSource.FindProperty("cloneIDs");
            for (int i = 0; i < cloneIDs.arraySize; i++)
            {
                if (cloneIDs.GetArrayElementAtIndex(i).intValue == entry.id)
                {
                    cloneIDs.DeleteArrayElementAtIndex(i);
                    serializedSource.ApplyModifiedProperties();
                    break;
                }
            }

            SerializedObject serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("sourceID").intValue = -1;
            serializedEntry.ApplyModifiedProperties();
        }

        public static void ClearClones(this Entry entry)
        {
            //foreach (int id in entry.cloneIDs)
            //{
            //    SerializedObject serializedClone = new SerializedObject(GetEntry(id));
            //    serializedClone.FindProperty("sourceID").intValue = -1;
            //    serializedClone.ApplyModifiedProperties();
            //}

            SerializedObject serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("cloneIDs").arraySize = 0;
            serializedEntry.ApplyModifiedProperties();
        }

        public static void SetBaseName(this Entry entry)
        {
            SerializedObject serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("displayName").stringValue += entry.GetType().ToString();
            serializedEntry.ApplyModifiedProperties();
        }

        public static void SetUniqueID(this Entry entry)
        {
            int highestID = -1;

            foreach (Entry e in DataCollectionWindow.entries)
                highestID = Mathf.Max(highestID, e.id);

            SerializedObject serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("id").intValue = ++highestID;
            serializedEntry.ApplyModifiedProperties();
        }

        public static Entry GetEntry(int id)
        {
            foreach (Entry e in DataCollectionWindow.entries)
            {
                if (e.id == id)
                    return e;
            }

            return null;
        }

        public static Entry GetSourceEntry(this Entry entry)
        {
            if (entry.sourceID < 0)
                return null;

            return GetEntry(entry.sourceID);
        }

        public static int GetRelatedDepth(this Entry entry)
        {
            if (entry.sourceID < 0)
                return 0;

            int depth = 0;

            do
            {
                depth++;
                entry = GetSourceEntry(entry);
            } while (entry != null);

            return depth;
        }

        public static bool IsClone(this Entry entry)
        {
            if (entry == null)
                return false;

            return entry.sourceID > -1;
        }

        public static bool HasClones(this Entry entry)
        {
            if (entry == null)
                return false;

            return entry.cloneIDs.Count > 0;
        }

        public static Entry TopSource(this Entry entry)
        {
            if (entry == null)
                return null;

            if (!entry.IsClone())
                return entry;

            do
            {
                entry = entry.GetSourceEntry();
            } while (entry.IsClone());

            return entry;
        }

        public static Entry[] TopSources(Entry exclude = null)
        {
            List<Entry> list = new List<Entry>();

            foreach (Entry e in DataCollectionWindow.entries)
            {
                if (!e.IsClone() && e.HasClones())
                    list.Add(e);
            }

            if (exclude != null && list.Contains(exclude))
                list.Remove(exclude);

            if (list.Count > 0)
                return list.ToArray();

            return null;
        }

        public static bool IsSubRelated(this Entry entry)
        {
            if (entry == null)
                return false;

            return entry.IsClone() && entry.HasClones();
        }

        public static bool IsLastCloneOf(this Entry entry, Entry source)
        {
            if (entry == null || source == null || !source.HasClones())
                return false;

            return source.cloneIDs[source.cloneIDs.Count - 1] == entry.id;
        }

        public static bool IsLastCloneOfAny(this Entry entry, Entry[] sources)
        {
            if (entry == null || sources == null || sources.Length == 0)
                return false;

            foreach (Entry source in sources)
                if (entry.IsLastCloneOf(source)) return true;

            return false;
        }
    }
}