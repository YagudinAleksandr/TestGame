using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform gridContent;
    public GameObject slotPrefab;

    [Header("Настройки")]
    public int totalSlots = 50;
    public int unlockedSlots = 15;

    private void Start()
    {
        BuildInventory();
    }

    void BuildInventory()
    {
        foreach(Transform child in gridContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < totalSlots; i++) 
        {
            // Создаем слот
            GameObject slot = Instantiate(slotPrefab, gridContent);

            // Ищем объект "Lock" внутри слота
            Transform lockObj = slot.transform.Find("Lock");

            if (i < unlockedSlots)
            {
                // Открытый слот
                if (lockObj != null) lockObj.gameObject.SetActive(false);
            }
            else
            {
                // Закрытый слот
                if (lockObj != null) lockObj.gameObject.SetActive(true);
            }
        }
    }
}
