using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameplayButtons : MonoBehaviour
{
    [SerializeField] private Button btnAddCoins;
    [SerializeField] private Button btnAddItem;
    [SerializeField] private Button btnAddAmmo;
    [SerializeField] private Button btnDeleteItem;
    [SerializeField] private Button btnShoot;

    private void Start()
    {
        if (btnAddCoins != null) btnAddCoins.onClick.AddListener(OnAddCoins);
        if (btnAddItem != null) btnAddItem.onClick.AddListener(OnAddItem);
        if (btnAddAmmo != null) btnAddAmmo.onClick.AddListener(OnAddAmmo);
        if (btnDeleteItem != null) btnDeleteItem.onClick.AddListener(OnDeleteItem);
        if (btnShoot != null) btnShoot.onClick.AddListener(OnShoot);
    }

    private void OnAddCoins()
    {
        int amount = Random.Range(9, 100);
        InventorySystem.Instance.AddCoins(amount);
        Debug.Log($"Добавлено ({amount}) монет");
    }

    private void OnAddItem()
    {
        string[] items = { "Pistol", "AssaultRifle", "Cap", "Helmet", "Jacket", "BodyArmor" };
        string itemId = items[Random.Range(0, items.Length)];
        var inv = InventorySystem.Instance;

        if (!inv.TryGetItem(itemId, out var itemData))
        {
            Debug.LogWarning($"[GameplayButtons] Предмет '{itemId}' не найден в базе. Проверьте allItemData.");
            return;
        }

        int slotBefore = FindEmptySlot();
        if (slotBefore < 0)
        {
            Debug.Log("Инвентарь полон");
            return;
        }

        bool added = inv.AddItem(itemId);
        if (!added)
        {
            Debug.Log($"Не удалось добавить {itemId}");
            return;
        }

        int slotAfter = FindSlotWithItem(itemId, slotBefore);
        Debug.Log($"Добавлено {itemId} в слот: {slotAfter}");
    }

    private void OnAddAmmo()
    {
        string[] ammoTypes = { "PistolAmmo", "AssaultRifleAmmo" };
        string ammoId = ammoTypes[Random.Range(0, ammoTypes.Length)];
        int amount = Random.Range(10, 31);

        var inv = InventorySystem.Instance;
        var slots = inv.Slots;

        if (!inv.TryGetItem(ammoId, out var ammoData)) return;

        bool hasSpace = false;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].unlocked) continue;
            if (slots[i].IsEmpty) { hasSpace = true; continue; }
            if (slots[i].itemId == ammoId && slots[i].count < ammoData.maxStack)
                hasSpace = true;
        }

        if (!hasSpace)
        {
            Debug.Log("Инвентарь полон");
            return;
        }

        inv.AddItem(ammoId, amount);

        List<int> targetSlots = new List<int>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].unlocked && !slots[i].IsEmpty && slots[i].itemId == ammoId)
                targetSlots.Add(i);
        }
        if (targetSlots.Count == 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].unlocked && slots[i].IsEmpty)
                {
                    targetSlots.Add(i);
                    break;
                }
            }
        }

        Debug.Log($"Добавлено ({amount}) {ammoId} в слот: {targetSlots[0]}");
    }

    private void OnDeleteItem()
    {
        var inv = InventorySystem.Instance;
        var slots = inv.Slots;

        List<int> filled = new List<int>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].unlocked && !slots[i].IsEmpty)
                filled.Add(i);
        }

        if (filled.Count == 0)
        {
            Debug.Log("Инвентарь пуст");
            return;
        }

        int idx = filled[Random.Range(0, filled.Count)];
        var slot = slots[idx];
        string id = slot.itemId;
        int count = slot.count;
        inv.RemoveItem(id, count);
        Debug.Log($"Удалено ({count}) {id} из слота: {idx}");
    }

    private void OnShoot()
    {
        var inv = InventorySystem.Instance;
        var slots = inv.Slots;

        List<int> weaponSlots = new List<int>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && inv.TryGetItem(slots[i].itemId, out var d) && d.type == ItemType.Weapon)
                weaponSlots.Add(i);
        }

        if (weaponSlots.Count == 0)
        {
            Debug.Log("Нет оружия");
            return;
        }

        int wIdx = weaponSlots[Random.Range(0, weaponSlots.Count)];
        var weaponData = inv.TryGetItem(slots[wIdx].itemId, out var wData) ? wData : null;
        if (weaponData == null) return;

        string ammoId = weaponData.ammoTypeId;
        if (string.IsNullOrEmpty(ammoId))
        {
            Debug.Log($"Нет патронов для {weaponData.id}");
            return;
        }

        int ammoSlot = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemId == ammoId && slots[i].count > 0)
            {
                ammoSlot = i;
                break;
            }
        }

        if (ammoSlot < 0)
        {
            Debug.Log($"Нет патронов для {weaponData.id}");
            return;
        }

        inv.RemoveItem(ammoId, 1);
        Debug.Log($"Выстрел из {weaponData.id}, патроны: {ammoId}, урон: {weaponData.damage}");
    }

    private int FindEmptySlot()
    {
        var slots = InventorySystem.Instance.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].unlocked && slots[i].IsEmpty) return i;
        }
        return -1;
    }

    private int FindSlotWithItem(string itemId, int startIndex)
    {
        var slots = InventorySystem.Instance.Slots;
        for (int i = startIndex; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemId == itemId)
                return i;
        }
        for (int i = 0; i < startIndex && i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemId == itemId)
                return i;
        }
        return startIndex;
    }
}
