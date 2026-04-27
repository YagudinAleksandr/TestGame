using UnityEngine;
using TMPro;

/// <summary>
/// Всплывающее окно с информацией о предмете. Синглтон.
/// Навешивается на объект ItemTooltip на Canvas. Показывается при клике на предмет в инвентаре.
/// </summary>
public class ItemTooltip : MonoBehaviour
{
    /// <summary>
    /// Единственный экземпляр tooltip (синглтон).
    /// </summary>
    public static ItemTooltip Instance { get; private set; }

    [Header("Ссылки на дочерние объекты")]
    [Tooltip("GameObject Panel — контейнер tooltip, скрывается целиком")]
    [SerializeField] private GameObject panel;

    [Tooltip("TextMeshPro — название предмета")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("TextMeshPro — тип предмета (заголовок)")]
    [SerializeField] private TMP_Text typeText;

    [Tooltip("TextMeshPro — полное описание с параметрами")]
    [SerializeField] private TMP_Text descText;

    /// <summary>
    /// Инициализация синглтона. Скрывает tooltip при старте.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    /// <summary>
    /// Скрывает tooltip при клике вне его области.
    /// </summary>
    private void Update()
    {
        if (panel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                panel.GetComponent<RectTransform>(), Input.mousePosition, null))
                Hide();
        }
    }

    /// <summary>
    /// Показывает tooltip с информацией о предмете. Размещает панель возле курсора.
    /// </summary>
    /// <param name="data">Данные предмета для отображения.</param>
    /// <param name="count">Количество предметов в слоте.</param>
    public void Show(ItemData data, int count)
    {
        if (data == null || panel == null) return;

        string info = $"<b>{data.id}</b>\n";
        info += $"Тип: {GetTypeLabel(data.type)}\n";
        info += $"Вес: {data.weight} x {count} = {data.weight * count:F2}\n";

        if (data.type == ItemType.Head || data.type == ItemType.Torso)
            info += $"Защита: {data.protection}\n";

        if (data.type == ItemType.Weapon)
            info += $"Урон: {data.damage}\nАмуниция: {data.ammoTypeId}\n";

        if (data.maxStack > 1)
            info += $"Стак: {count}/{data.maxStack}\n";

        if (nameText != null) nameText.text = data.id;
        if (descText != null) descText.text = info;

        panel.SetActive(true);

        var rt = panel.GetComponent<RectTransform>();
        if (rt != null)
            rt.position = ClampToScreen(Input.mousePosition, rt);
    }

    /// <summary>
    /// Скрывает tooltip.
    /// </summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// Ограничивает позицию tooltip, чтобы он не выходил за пределы экрана.
    /// </summary>
    /// <param name="pos">Желаемая позиция (координаты курсора).</param>
    /// <param name="rt">RectTransform панели tooltip.</param>
    /// <returns>Скорректированная позиция в пределах экрана.</returns>
    Vector3 ClampToScreen(Vector3 pos, RectTransform rt)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return pos;

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        Vector2 size = new Vector2(rt.rect.width, rt.rect.height);

        float x = Mathf.Clamp(pos.x, size.x * 0.5f, Screen.width - size.x * 0.5f);
        float y = Mathf.Clamp(pos.y, size.y * 0.5f, Screen.height - size.y * 0.5f);

        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// Возвращает текстовое название типа предмета на русском языке.
    /// </summary>
    /// <param name="type">Тип предмета из enum ItemType.</param>
    /// <returns>Название типа на русском.</returns>
    string GetTypeLabel(ItemType type)
    {
        switch (type)
        {
            case ItemType.Head: return "Защита головы";
            case ItemType.Torso: return "Защита торса";
            case ItemType.Ammo: return "Патроны";
            case ItemType.Weapon: return "Оружие";
            default: return type.ToString();
        }
    }
}
