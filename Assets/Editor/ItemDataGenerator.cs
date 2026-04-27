#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ItemDataGenerator
{
    [MenuItem("Tools/Generate Item Data")]
    public static void Generate()
    {
        string folder = "Assets/InventoryAssets/Items/Data";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/InventoryAssets/Items", "Data");

        CreateItem("Cap", ItemType.Head, "Cap", 0.2f, 1, 1, 0, "");
        CreateItem("Helmet", ItemType.Head, "Helmet", 1f, 1, 8, 0, "");
        CreateItem("Jacket", ItemType.Torso, "Jacket", 0.8f, 1, 3, 0, "");
        CreateItem("BodyArmor", ItemType.Torso, "BodyArmor", 10f, 1, 10, 0, "");
        CreateItem("PistolAmmo", ItemType.Ammo, "PistolAmmo", 0.01f, 50, 0, 0, "");
        CreateItem("AssaultRifleAmmo", ItemType.Ammo, "AssaultRifleAmmo", 0.015f, 40, 0, 0, "");
        CreateItem("Pistol", ItemType.Weapon, "Pistol", 1f, 1, 0, 10, "PistolAmmo");
        CreateItem("AssaultRifle", ItemType.Weapon, "AssaultRifle", 5f, 1, 0, 20, "AssaultRifleAmmo");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ItemData assets generated.");
    }

    static void CreateItem(string id, ItemType type, string iconFilename, float weight, int maxStack, int protection, int damage, string ammoType)
    {
        string path = $"Assets/InventoryAssets/Items/Data/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null) return;

        var asset = ScriptableObject.CreateInstance<ItemData>();
        asset.id = id;
        asset.type = type;
        asset.weight = weight;
        asset.maxStack = maxStack;
        asset.protection = protection;
        asset.damage = damage;
        asset.ammoTypeId = ammoType;

        string iconPath = $"Assets/InventoryAssets/Items/{iconFilename}.png";
        asset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

        AssetDatabase.CreateAsset(asset, path);
    }
}
#endif
