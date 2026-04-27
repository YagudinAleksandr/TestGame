using UnityEngine;
using TMPro;

/// <summary>
/// Управляет визуальным отображением инвентаря: создаёт слоты, обновляет иконки/текст/монеты/вес.
/// Навешивается на объект InventoryPanel или Scroll View на сцене.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Ссылки на объекты сцены")]
    [Tooltip("Transform объекта Content из Scroll View (контейнер для слотов)")]
    [SerializeField] private Transform gridContent;

    [Tooltip("Префаб слота инвентаря (должен содержать компонент SlotUI и дочерние Icon, Count, Lock)")]
    [SerializeField] private GameObject slotPrefab;

    [Tooltip("TextMeshPro — текст для отображения количества монет")]
    [SerializeField] private TMP_Text coinsText;

    [Tooltip("TextMeshPro — текст для отображения общего веса инвентаря")]
    [SerializeField] private TMP_Text weightText;

    /// <summary>
    /// Создаёт слоты, подписывается на изменения инвентаря и выполняет первую отрисовку.
    /// </summary>
    private void Start()
    {
        BuildSlots();
        Subscribe();
        Refresh();
    }

    /// <summary>
    /// Подписывается на событие изменения инвентаря при активации объекта.
    /// </summary>
    private void OnEnable()
    {
        Subscribe();
    }

    /// <summary>
    /// Отписывается от события изменения инвентаря при деактивации объекта.
    /// </summary>
    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>
    /// Подписывает метод Refresh на событие OnChanged InventorySystem.
    /// </summary>
    private void Subscribe()
    {
        Unsubscribe();
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged -= Refresh;
            InventorySystem.Instance.OnChanged += Refresh;
        }
    }

    /// <summary>
    /// Отписывает метод Refresh от события OnChanged.
    /// </summary>
    private void Unsubscribe()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnChanged -= Refresh;
    }

    /// <summary>
    /// Создаёт все слоты инвентаря из префаба и инициализирует их.
    /// </summary>
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

    /// <summary>
    /// Обновляет визуальное состояние всех слотов, текст монет и веса.
    /// Вызывается автоматически при каждом изменении инвентаря.
    /// </summary>
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
