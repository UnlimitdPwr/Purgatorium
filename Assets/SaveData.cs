using System;
using System.Collections.Generic;
using UnityEngine;

// Everything written to disk when the player rests at a bonfire. Plain data
// (JsonUtility). Items are stored by ItemDatabase ID; "" = empty slot.
// Bump `version` if fields change in a way old saves can't load.
[Serializable]
public class SaveData
{
    public int version = 1;

    [Serializable]
    public class SavedStack
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    public class SavedWear
    {
        public string shieldId;
        public float wear;
    }

    // World
    public bool hasCheckpoint;
    public Vector3 checkpointPosition;
    public List<Vector3> bonfirePositions = new List<Vector3>();

    // Progression
    public int essence;
    public int healthLevel = 1;
    public int staminaLevel = 1;
    public int strengthLevel = 1;
    public int dexterityLevel = 1;
    public int soulDurabilityLevel = 1;

    // Items
    public List<SavedStack> inventory = new List<SavedStack>();
    public List<string> shieldSlots = new List<string>();
    public List<string> quickSlots = new List<string>();
    public List<string> talismanSlots = new List<string>();
    public int activeShieldIndex = -1;
    public List<SavedWear> shieldWear = new List<SavedWear>();
}
