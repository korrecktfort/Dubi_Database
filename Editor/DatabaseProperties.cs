using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dubi.Database.Editor
{
    [InitializeOnLoad]
    public class DatabaseProperties
    {
        [InitializeOnLoad]
        public static class Current
        {
            public static int LastInstanceID
            {
                get => EditorPrefs.GetInt("Dubi.Database.LastInstanceID.ds65$mt");
                set => EditorPrefs.SetInt("Dubi.Database.LastInstanceID.ds65$mt", value);
            }

            public static string Path
            {
                get => EditorPrefs.GetString("Dubi.Database.Path.ds65$mt");
                set => EditorPrefs.SetString("Dubi.Database.Path.ds65$mt", value);
            }
        }
    }
}