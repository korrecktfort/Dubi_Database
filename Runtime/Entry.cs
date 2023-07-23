using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dubi.Database
{  
    public class Entry : ScriptableObject
    {
        [Hide, HideInInspector] public int id = 0;
        [Hide, HideInInspector] public string displayName = "";
        [Hide, HideInInspector] public int sourceID = -1;
        [Hide, HideInInspector] public List<int> cloneIDs = new List<int>();
        [Hide, HideInInspector] public string[] lockedEntries = new string[0];                     
    }
}
