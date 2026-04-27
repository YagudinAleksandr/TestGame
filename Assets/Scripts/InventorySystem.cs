using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Данные одного слота инвентаря. Хранит идентификатор предмета, количество и статус разблокировки.
/// </summary>
[System.Serializable]
public class SlotData
{
    /// <summary>
    /// Идентификатор предмета в слоте. Пустая строка — слот свободен.
    /// </summary>
    public string itemId;

    /// <summary>
    /// Количество предметов в слоте.
    /// </summary>
    public int count;

    /// <summary>
    /// Разблокирован ли слот для использования.
    /// </summary>
    public bool unlocked;

    /// <summary>
    /// Возвращает true, если слот пуст (нет предмета или количество равно 0).
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;

    /// <summary>
    /// Инициализирует слот с заданным статусом разблокировки.
    /// </summary>
    /// <param name="unlocked">Разблокирован ли слот.</param>
    public SlotData(bool unlocked)
    {
        this.itemId = "";
        this.count = 0;
        this.unlocked = unlocked;
    }

    /// <summary>
    /// Очищает слот: удаляет предмет и обнуляет количество.
    /// </summary>
    public void Clear()
    {
        itemId = "";
        count = 0;
    }
}

/// <summary>
/// Основная система инвентаря. Синглтон. Управляет слотами, монетами, сохранением/загрузкой.
/// Навешивается на любой GameObject (например, GameManager).
/// </summary>
public class InventorySystem : MonoBehaviour
{
    /// <summary>
    /// Единственный экземпляр системы инвентаря (синглтон).
    /// </summary>
    public static InventorySystem Instance { get; private set; }

    /// <summary>
    /// Общее количество слотов инвентаря.
    /// </summary>
    public const int TotalSlots = 50;

    /// <summary>
    /// Количество слотов, разблокированных по умолчанию.
    /// </summary>
    public const int DefaultUnlocked = 15;

    /// <summary>
    /// Количество слотов в одном ряду сетки.
    /// </summary>
    public const int SlotsPerRow = 5;

    /// <summary>
    /// Событие, вызываемое при любом изменении инвентаря (добавление, удаление, перемещение, монеты).
    /// </summary>
    public event Action OnChanged;

    [Header("Настройка предметов")]
    [Tooltip("Перетащите сюда все ItemData ассеты из папки InventoryAssets/Items/")]
    [SerializeField] private ItemData[] allItemData;

    [Header("Настройка разблокировки")]
    [Tooltip("Стоимость разблокировки каждого слота начиная с 15-го. Если пусто — генерируется автоматически: 10, 15, 20, 25...")]
    [SerializeField] private int[] slotUnlockCosts;

    private Dictionary<string, ItemData> itemDatabase;
    private List<SlotData> slots = new List<SlotData>();
    private int coins;

    /// <summary>
    /// Текущий баланс монет.
    /// </summary>
    public int Coins => coins;

    /// <summary>
    /// Общий вес всех предметов в инвентаре.
    /// </summary>
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

    /// <summary>
    /// Доступ только для чтения ко всем слотам инвентаря.
    /// </summary>
    public IReadOnlyList<SlotData> Slots => slots;

    /// <summary>
    /// Инициализация синглтона, базы предметов и загрузка сохранённых данных.
    /// </summary>
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

    /// <summary>
    /// Пытается найти предмет в базе по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор предмета.</param>
    /// <param name="data">Найденные данные предмета (или null).</param>
    /// <returns>True, если предмет найден.</returns>
    public bool TryGetItem(string id, out ItemData data)
    {
        return itemDatabase.TryGetValue(id, out data);
    }

    /// <summary>
    /// Добавляет монеты на баланс игрока.
    /// </summary>
    /// <param name="amount">Количество монет для добавления (должно быть > 0).</param>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Пытается списать монеты с баланса игрока.
    /// </summary>
    /// <param name="amount">Количество монет для списания.</param>
    /// <returns>True, если монеты успешно списаны. False — недостаточно монет.</returns>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount) return false;
        coins -= amount;
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Добавляет предмет в инвентарь. Сначала заполняет существующие стаки, затем пустые слоты.
    /// </summary>
    /// <param name="itemId">Идентификатор предмета.</param>
    /// <param name="amount">Количество для добавления (по умолчанию 1).</param>
    /// <returns>True, если хотя бы часть предметов добавлена.</returns>
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

    /// <summary>
    /// Удаляет предмет из инвентаря. Удаляет с конца (последних слотов).
    /// </summary>
    /// <param name="itemId">Идентификатор предмета.</param>
    /// <param name="amount">Количество для удаления.</param>
    /// <returns>True, если предметы успешно удалены.</returns>
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

    /// <summary>
    /// Возвращает стоимость разблокировки указанного слота.
    /// </summary>
    /// <param name="slotIndex">Индекс слота (от 0 до TotalSlots-1).</param>
    /// <returns>Стоимость в монетах, или -1 если слот не подлежит разблокировке.</returns>
    public int GetSlotUnlockCost(int slotIndex)
    {
        if (slotIndex < DefaultUnlocked || slotIndex >= TotalSlots) return -1;
        int costIndex = slotIndex - DefaultUnlocked;
        if (costIndex < slotUnlockCosts.Length) return slotUnlockCosts[costIndex];
        return -1;
    }

    /// <summary>
    /// Пытается разблокировать слот за монеты. Слоты разблокируются строго по порядку.
    /// </summary>
    /// <param name="slotIndex">Индекс слота для разблокировки.</param>
    /// <returns>True, если слот успешно разблокирован.</returns>
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

    /// <summary>
    /// Перемещает предмет между слотами. Поддерживает: перемещение в пустой, стакание, обмен.
    /// </summary>
    /// <param name="fromIndex">Индекс исходного слота.</param>
    /// <param name="toIndex">Индекс целевого слота.</param>
    /// <returns>True, если перемещение выполнено.</returns>
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

    /// <summary>
    /// Путь к файлу сохранения (Application.persistentDataPath/inventory_save.json).
    /// </summary>
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "inventory_save.json");

    /// <summary>
    /// Сохраняет текущее состояние инвентаря (монеты, слоты, разблокировка) в JSON-файл.
    /// </summary>
    public void Save()
    {
        try
        {
            var data = new InventorySaveData
            {
                coins = this.coins,
                slots = new List<SlotData>(this.slots)
            };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySystem] Ошибка сохранения: {e.Message}");
        }
    }

    /// <summary>
    /// Загружает сохранённое состояние инвентаря из JSON-файла. Если файл не найден — создаёт пустой инвентарь.
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                var data = JsonUtility.FromJson<InventorySaveData>(json);
                if (data != null && data.slots != null && data.slots.Count == TotalSlots)
                {
                    coins = data.coins;
                    slots = data.slots;
                    OnChanged?.Invoke();
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySystem] Ошибка загрузки: {e.Message}");
        }

        coins = 0;
        slots = new List<SlotData>();
        for (int i = 0; i < TotalSlots; i++)
            slots.Add(new SlotData(i < DefaultUnlocked));
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Сохраняет инвентарь при сворачивании приложения.
    /// </summary>
    /// <param name="pause">True, если приложение приостановлено.</param>
    private void OnApplicationPause(bool pause)
    {
        if (pause) Save();
    }

    /// <summary>
    /// Сохраняет инвентарь при выходе из приложения.
    /// </summary>
    private void OnApplicationQuit()
    {
        Save();
    }
}

/// <summary>
/// Структура для сериализации/десериализации состояния инвентаря в JSON.
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    /// <summary>
    /// Баланс монет.
    /// </summary>
    public int coins;

    /// <summary>
    /// Список всех слотов инвентаря.
    /// </summary>
    public List<SlotData> slots;
}
