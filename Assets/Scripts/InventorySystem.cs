using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SlotData
{
    public string itemId;
    public int count;
    public bool unlocked;

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;

    public SlotData(bool unlocked)
    {
        this.itemId = "";
        this.count = 0;
        this.unlocked = unlocked;
    }

    public void Clear()
    {
        itemId = "";
        count = 0;
    }
}

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    public const int TotalSlots = 50;
    public const int DefaultUnlocked = 15;
    public const int SlotsPerRow = 5;

    public event Action OnChanged;

    [SerializeField] private ItemData[] allItemData;
    [SerializeField] private int[] slotUnlockCosts;

    private Dictionary<string, ItemData> itemDatabase;
    private List<SlotData> slots = new List<SlotData>();
    private int coins;

    public int Coins => coins;

    public float TotalWeight
    {
        get
        {
            float w = 0;
            foreach (var s in slots)
            {
                if (!s.IsEmpty && TryGetItem(s.itemId, out var data))
                    w += data.weight * s.count;
            }
            return w;
        }
    }

    public IReadOnlyList<SlotData> Slots => slots;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        itemDatabase = new Dictionary<string, ItemData>();
        if (allItemData != null)
        {
            foreach (var d in allItemData)
            {
                if (d != null && !string.IsNullOrEmpty(d.id))
                    itemDatabase[d.id] = d;
            }
        }

        if (itemDatabase.Count == 0)
            Debug.LogWarning("[InventorySystem] allItemData пуст! Назначьте ItemData ассеты в инспекторе.");

        if (slotUnlockCosts == null || slotUnlockCosts.Length == 0)
        {
            slotUnlockCosts = new int[TotalSlots - DefaultUnlocked];
            for (int i = 0; i < slotUnlockCosts.Length; i++)
                slotUnlockCosts[i] = 10 + i * 5;
        }

        Load();
    }

    public bool TryGetItem(string id, out ItemData data)
    {
        return itemDatabase.TryGetValue(id, out data);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        OnChanged?.Invoke();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount) return false;
        coins -= amount;
        OnChanged?.Invoke();
        return true;
    }

    public bool AddItem(string itemId, int amount = 1)
    {
        if (!itemDatabase.ContainsKey(itemId) || amount <= 0) return false;

        var data = itemDatabase[itemId];
        int remaining = amount;

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            if (slots[i].unlocked && !slots[i].IsEmpty && slots[i].itemId == itemId && slots[i].count < data.maxStack)
            {
                int canAdd = Mathf.Min(remaining, data.maxStack - slots[i].count);
                slots[i].count += canAdd;
                remaining -= canAdd;
            }
        }

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            if (slots[i].unlocked && slots[i].IsEmpty)
            {
                int canAdd = Mathf.Min(remaining, data.maxStack);
                slots[i].itemId = itemId;
                slots[i].count = canAdd;
                remaining -= canAdd;
            }
        }

        if (remaining < amount)
        {
            OnChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return false;

        int available = 0;
        foreach (var s in slots)
            if (!s.IsEmpty && s.itemId == itemId) available += s.count;

        if (available < amount) return false;

        int remaining = amount;
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (!slots[i].IsEmpty && slots[i].itemId == itemId)
            {
                int toRemove = Mathf.Min(remaining, slots[i].count);
                slots[i].count -= toRemove;
                remaining -= toRemove;
                if (slots[i].count <= 0)
                    slots[i].Clear();
            }
        }

        OnChanged?.Invoke();
        return true;
    }

    public int GetSlotUnlockCost(int slotIndex)
    {
        if (slotIndex < DefaultUnlocked || slotIndex >= TotalSlots) return -1;
        int costIndex = slotIndex - DefaultUnlocked;
        if (costIndex < slotUnlockCosts.Length) return slotUnlockCosts[costIndex];
        return -1;
    }

    public bool UnlockSlot(int slotIndex)
    {
        if (slotIndex < DefaultUnlocked || slotIndex >= TotalSlots) return false;

        for (int i = DefaultUnlocked; i < slotIndex; i++)
        {
            if (!slots[i].unlocked) return false;
        }

        int cost = GetSlotUnlockCost(slotIndex);
        if (cost < 0 || coins < cost) return false;

        coins -= cost;
        slots[slotIndex].unlocked = true;
        OnChanged?.Invoke();
        return true;
    }

    public bool MoveSlot(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return false;
        if (fromIndex < 0 || fromIndex >= slots.Count) return false;
        if (toIndex < 0 || toIndex >= slots.Count) return false;
        if (!slots[fromIndex].unlocked || !slots[toIndex].unlocked) return false;

        var from = slots[fromIndex];
        var to = slots[toIndex];

        if (from.IsEmpty) return false;

        if (to.IsEmpty)
        {
            to.itemId = from.itemId;
            to.count = from.count;
            from.Clear();
        }
        else if (from.itemId == to.itemId && TryGetItem(from.itemId, out var data) && data.maxStack > 1)
        {
            int space = data.maxStack - to.count;
            int toMove = Mathf.Min(from.count, space);

            if (toMove <= 0) return false;

            to.count += toMove;
            from.count -= toMove;

            if (from.count <= 0)
                from.Clear();
        }
        else
        {
            string tmpId = to.itemId;
            int tmpCount = to.count;

            to.itemId = from.itemId;
            to.count = from.count;

            from.itemId = tmpId;
            from.count = tmpCount;
        }

        OnChanged?.Invoke();
        return true;
    }

    public void Save()
    {
        var data = new InventorySaveData
        {
            coins = this.coins,
            slots = new List<SlotData>(this.slots)
        };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("InventorySave", json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString("InventorySave", "");
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<InventorySaveData>(json);
            if (data != null && data.slots != null && data.slots.Count == TotalSlots)
            {
                coins = data.coins;
                slots = data.slots;
                OnChanged?.Invoke();
                return;
            }
        }

        coins = 0;
        slots = new List<SlotData>();
        for (int i = 0; i < TotalSlots; i++)
            slots.Add(new SlotData(i < DefaultUnlocked));
        OnChanged?.Invoke();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}

[System.Serializable]
public class InventorySaveData
{
    public int coins;
    public List<SlotData> slots;
}
