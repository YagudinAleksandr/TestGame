using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform gridContent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text weightText;

    private void Start()
    {
        BuildSlots();
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged -= Refresh;
            InventorySystem.Instance.OnChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnChanged -= Refresh;
    }

    private void BuildSlots()
    {
        if (gridContent == null || slotPrefab == null) return;

        foreach (Transform child in gridContent)
            Destroy(child.gameObject);

        for (int i = 0; i < InventorySystem.TotalSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, gridContent);
            var slotUI = slotObj.GetComponent<SlotUI>();
            if (slotUI == null) slotUI = slotObj.AddComponent<SlotUI>();
            slotUI.Initialize(i);
        }
    }

    public void Refresh()
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        var slots = inv.Slots;
        int childCount = gridContent != null ? gridContent.childCount : 0;

        for (int i = 0; i < childCount && i < slots.Count; i++)
        {
            var slotUI = gridContent.GetChild(i).GetComponent<SlotUI>();
            if (slotUI != null)
                slotUI.Refresh(slots[i], slots[i].unlocked);
        }

        if (coinsText != null)
            coinsText.text = $"Монеты: {inv.Coins}";

        if (weightText != null)
            weightText.text = $"Вес: {inv.TotalWeight:F1}";
    }
}
