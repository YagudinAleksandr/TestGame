using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string id;
    public ItemType type;
    public Sprite icon;
    public float weight;
    public int maxStack = 1;
    public int protection;
    public int damage;
    public string ammoTypeId;
}
