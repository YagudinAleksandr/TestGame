using UnityEngine;

/// <summary>
/// ScriptableObject — данные предмета. Создаётся через Assets > Create > Inventory > ItemData.
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Основные параметры")]

    /// <summary>
    /// Уникальный идентификатор предмета (например "Pistol", "PistolAmmo").
    /// </summary>
    public string id;

    /// <summary>
    /// Тип предмета (голова, торс, патроны, оружие).
    /// </summary>
    public ItemType type;

    /// <summary>
    /// Иконка предмета, отображаемая в слоте инвентаря.
    /// </summary>
    public Sprite icon;

    /// <summary>
    /// Вес одного предмета в кг.
    /// </summary>
    public float weight;

    /// <summary>
    /// Максимальное количество предметов в одном стаке.
    /// </summary>
    public int maxStack = 1;

    [Header("Защита (Head, Torso)")]

    /// <summary>
    /// Значение защиты. Используется для типов Head и Torso.
    /// </summary>
    public int protection;

    [Header("Оружие (Weapon)")]

    /// <summary>
    /// Урон оружия. Используется для типа Weapon.
    /// </summary>
    public int damage;

    /// <summary>
    /// ID типа патронов, необходимых для стрельбы (например "PistolAmmo").
    /// </summary>
    public string ammoTypeId;
}
