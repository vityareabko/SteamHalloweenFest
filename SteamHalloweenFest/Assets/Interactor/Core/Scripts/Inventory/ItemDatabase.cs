using System;
using System.Collections.Generic;
using UnityEngine;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#inventorycs")]
    public class ItemDatabase : MonoBehaviour
    {
        private static ItemDatabase instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ItemDatabase>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("ItemDatabase");
                        instance = obj.AddComponent<ItemDatabase>();
                    }
                }
                return instance;
            }
        }

        [Tooltip("Items will be represented with those informations when they're at inventory.")]
        public List<ItemData> database = new List<ItemData>();
        public static Color[] rarityColors = { 
            new Color(0f, 1f, 0f, 1f), 
            new Color(0.42f, 0.62f, 0.92f, 1f), 
            new Color(0.6f, 0f, 1f, 1f), 
            new Color(0.9f, 0.57f, 0.22f, 1f) };

        public ItemData GetItemDataByIndex(int index)
        {
            if (database.Count > index) return database[index];
            else return null;
        }
        public int GetFirstEmptyIndex()
        {
            for (int i = 0; i < database.Count; i++)
            {
                if (database[i] == null || database[i].Id == -1) return i;
            }
            return database.Count;
        }
        public void AddDatabaseIndex(ItemData item)
        {
            if (item.Id < 0) return;

            if (item.Id < database.Count && database[item.Id].Id == -1)
            {
                database[item.Id] = item;
            }
            else
            {
                int fillNull = Mathf.Max(0, item.Id - database.Count);
                for (int i = 0; i < fillNull; i++)
                {
                    AddNullIndex();
                }
                item.Id = database.Count;
                database.Add(item);
            }
        }
        private void AddNullIndex()
        {
            ItemData item = new ItemData();
            database.Add(item);
        }
        public bool AddSceneItemsToDatabase()
        {
            Item[] itemIndexes = Resources.FindObjectsOfTypeAll<Item>();

            bool updated = false;
            foreach (Item itemIndex in itemIndexes)
            {
                Debug.Log(itemIndex.gameObject.name);
                if (itemIndex.dbIndex < 0)
                {
                    Debug.LogWarning("Negative dbIndex", itemIndex);
                }
                else
                {
                    if ((itemIndex.dbIndex < database.Count && database[itemIndex.dbIndex].Id == -1) || itemIndex.dbIndex >= database.Count)
                    {
                        ItemData newItem = new ItemData();
                        newItem.Title = itemIndex.name;
                        newItem.Id = itemIndex.dbIndex;
                        AddDatabaseIndex(newItem);
                        updated = true;
                    }
                }
            }
            return updated;
        }
        public void ClearDatabase()
        {
            database.Clear();
        }
    }

    [Serializable]
    public class ItemData
    {
        [SerializeField] private int id = -1;
        public int Id { get { return id; } set { id = value; } }
        [SerializeField] private string title = "";
        public string Title { get { return title; } set { title = value; } }
        [SerializeField] private int value = 100;
        public int Value { get { return value; } set { this.value = value; } }
        [SerializeField] private ItemRarity rarity;
        public ItemRarity Rarity { get { return rarity; } set { rarity = value; } }
        [TextArea(1, 4)] [SerializeField] private string description = "";
        public string Description { get { return description; } set { description = value; } }
        [SerializeField] private bool stackable;
        public bool Stackable { get { return stackable; } set { stackable = value; } }
        [SerializeField] private Sprite sprite;
        public Sprite Sprite { get { return sprite; } set { sprite = value; } }

        public enum ItemRarity { Common, Rare, Epic, Legendary };

        public ItemData() { }
    }
}
