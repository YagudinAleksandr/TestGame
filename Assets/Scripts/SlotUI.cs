using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Визуальное представление одного слота инвентаря.
/// Навешивается на преfab слота. Обрабатывает клики (разблокировка, tooltip), drag-and-drop.
/// </summary>
public class SlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("Ссылки на дочерние объекты префаба")]
    [Tooltip("Image для отображения иконки предмета")]
    [SerializeField] private Image iconImage;

    [Tooltip("GameObject с иконкой замка (показывается для заблокированных слотов)")]
    [SerializeField] private GameObject lockOverlay;

    [Tooltip("TextMeshPro для отображения количества предметов в стаке")]
    [SerializeField] private TMP_Text countText;

    /// <summary>
    /// Фоновое изображение слота (получается автоматически).
    /// </summary>
    private Image bgImage;

    /// <summary>
    /// Индекс данного слота в инвентаре (от 0 до TotalSlots-1).
    /// </summary>
    private int slotIndex;

    /// <summary>
    /// Ссылка на слот, с которого началось перетаскивание (статическая, общая для всех слотов).
    /// </summary>
    private static SlotUI dragSource;

    /// <summary>
    /// Временный объект-призрак, следующий за курсором при перетаскивании.
    /// </summary>
    private GameObject dragGhost;

    /// <summary>
    /// Индекс данного слота.
    /// </summary>
    public int SlotIndex => slotIndex;

    /// <summary>
    /// Инициализирует слот: задаёт индекс, получает фоновое изображение, настраивает RectTransform иконки.
    /// </summary>
    /// <param name="index">Индекс слота в инвентаре.</param>
    public void Initialize(int index)
    {
        this.slotIndex = index;
        this.bgImage = GetComponent<Image>();

        if (iconImage != null)
        {
            var rt = iconImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, -10);
        }
    }

    /// <summary>
    /// Обновляет визуальное состояние слота: иконка, количество, замок, фон.
    /// </summary>
    /// <param name="slot">Данные слота (предмет, количество).</param>
    /// <param name="unlocked">Разблокирован ли слот.</param>
    public void Refresh(SlotData slot, bool unlocked)
    {
        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);

        if (!unlocked)
        {
            if (iconImage != null) iconImage.enabled = false;
            if (countText != null) countText.text = "";
            return;
        }

        if (slot == null || slot.IsEmpty)
        {
            if (iconImage != null) { iconImage.enabled = false; iconImage.sprite = null; }
            if (countText != null) countText.text = "";
            if (bgImage != null) bgImage.color = new Color(1f, 1f, 1f, 0.3f);
            return;
        }

        if (InventorySystem.Instance != null && InventorySystem.Instance.TryGetItem(slot.itemId, out var data))
        {
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = data.icon;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
            }
            if (countText != null)
            {
                countText.text = slot.count > 1 ? slot.count.ToString() : "";
            }
        }
        else
        {
            if (iconImage != null) { iconImage.enabled = false; iconImage.sprite = null; }
            if (countText != null) countText.text = "";
            Debug.LogWarning($"[SlotUI] Предмет '{slot.itemId}' не найден в базе.");
        }
    }

    /// <summary>
    /// Обработка клика: разблокировка заблокированного слота или показ tooltip с информацией о предмете.
    /// </summary>
    /// <param name="eventData">Данные события указателя.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        var slots = inv.Slots;
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var slot = slots[slotIndex];

        if (!slot.unlocked)
        {
            inv.UnlockSlot(slotIndex);
            return;
        }

        if (!slot.IsEmpty && ItemTooltip.Instance != null)
        {
            if (inv.TryGetItem(slot.itemId, out var data))
                ItemTooltip.Instance.Show(data, slot.count);
        }
    }

    /// <summary>
    /// Начало перетаскивания: создаёт призрак иконки, скрывает оригинальную иконку.
    /// </summary>
    /// <param name="eventData">Данные события указателя.</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        var slots = inv.Slots;
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var slot = slots[slotIndex];
        if (!slot.unlocked || slot.IsEmpty) return;

        dragSource = this;

        if (iconImage != null && iconImage.sprite != null)
        {
            dragGhost = new GameObject("DragGhost");
            dragGhost.transform.SetParent(transform.root, false);
            var img = dragGhost.AddComponent<Image>();
            img.sprite = iconImage.sprite;
            img.raycastTarget = false;
            img.SetNativeSize();
            var rt = dragGhost.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);
            dragGhost.transform.position = iconImage.transform.position;
        }

        if (iconImage != null) iconImage.enabled = false;
    }

    /// <summary>
    /// Перетаскивание: перемещает призрак иконки за курсором.
    /// </summary>
    /// <param name="eventData">Данные события указателя.</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.transform.position = eventData.position;
    }

    /// <summary>
    /// Завершение перетаскивания: уничтожает призрак, восстанавливает иконку если предмет не был перемещён.
    /// </summary>
    /// <param name="eventData">Данные события указателя.</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            Destroy(dragGhost);

        if (dragSource == this)
        {
            var inv = InventorySystem.Instance;
            if (inv != null)
            {
                var slot = inv.Slots[slotIndex];
                if (slot != null && !slot.IsEmpty && iconImage != null)
                    iconImage.enabled = true;
            }
        }

        dragSource = null;
    }

    /// <summary>
    /// Сброс предмета на целевой слот: выполняет перемещение/стакание/обмен через InventorySystem.
    /// </summary>
    /// <param name="eventData">Данные события указателя.</param>
    public void OnDrop(PointerEventData eventData)
    {
        if (dragSource == null || dragSource == this) return;

        var inv = InventorySystem.Instance;
        if (inv == null) return;

        inv.MoveSlot(dragSource.slotIndex, this.slotIndex);
    }
}
