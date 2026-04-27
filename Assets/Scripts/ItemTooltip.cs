using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    private void Update()
    {
        if (panel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                panel.GetComponent<RectTransform>(), Input.mousePosition, null))
                Hide();
        }
    }

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

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

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
