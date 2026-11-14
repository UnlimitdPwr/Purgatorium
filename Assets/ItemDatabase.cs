using System;
using System.Collections.Generic;
using UnityEngine;

// Every ItemData in the project with a stable ID (its asset GUID), so save
// files can store items as text and find them again. Lives at
// Assets/Resources/ItemDatabase.asset and is rebuilt AUTOMATICALLY by the
// editor whenever item assets are added, moved or deleted
// (also: Tools > Save > Rebuild Item Database).
public class ItemDatabase : ScriptableObject
{
    public const string ResourcePath = "ItemDatabase";

    [Serializable]
    public class Entry
    {
        public string id;
        public ItemData item;
    }

    public List<Entry> entries = new List<Entry>();

    private static ItemDatabase instance;

    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ItemDatabase>(ResourcePath);
            return instance;
        }
    }

    public string GetId(ItemData item)
    {
        if (item == null)
            return "";

        foreach (Entry entry in entries)
        {
            if (entry.item == item)
                return entry.id;
        }

        Debug.LogWarning("ItemDatabase: '" + item.name + "' isn't in the database, so it can't be saved. " +
                         "Run Tools > Save > Rebuild Item Database.");
        return "";
    }

    public ItemData GetItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        foreach (Entry entry in entries)
        {
            if (entry.id == id)
                return entry.item;
        }

        Debug.LogWarning("ItemDatabase: no item with id " + id + " (deleted since the save was made?).");
        return null;
    }
}
