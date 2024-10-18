using UnityEngine;
using System;
using System.Collections.Generic;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#inventorycs")]
    public class Inventory : MonoBehaviour
    {
        [Tooltip("Shows current Interactor that uses this inventory to drop.")]
        [ReadOnly] public Interactor currentInteractor;
        public string inventoryName;
        public Vector2Int inventorySize = new Vector2Int(4, 3);

        [Tooltip("Inventory hand point when dropping items.")]
        public Transform inventoryPoint;
        [Tooltip("Alternative inventory onBody positions instead of InteractorPoints list. Can be used to drop objects on this object instead of player body parts.")]
        public List<BodyPoint> invOnBodyPoints = new List<BodyPoint>();

        [Header("Inspector-Only Settings")]
        public Interactor changeOwner;
        public Item addItem;
        public Item removeItem;
        public bool reorderInventory;

        [ReadOnly] public Item[] currentItems;
        [ReadOnly] public int[] currentAmounts;

        [HideInInspector] public bool invUpdated;
        [HideInInspector] public bool reordered;

        private Vector2Int oldInventorySize;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(inventoryName)) inventoryName = gameObject.name;

            CheckInventorySize();
            if (changeOwner)
            {
                if (currentInteractor == changeOwner) DeattachInventory();
                else AttachInventory(changeOwner);
                changeOwner = null;
            }
            if (addItem)
            {
                AddItemToInventory(addItem);
                addItem = null;
            }
            if (removeItem)
            {
                RemoveItemFromInventory(removeItem);
                removeItem = null;
            }
            if (reorderInventory)
            {
                ReorderInventory();
                reorderInventory = false;
            }
        }
        private void OnEnable()
        {
            if (ItemDatabase.Instance) return;

            Debug.LogWarning("There is no ItemDatabase in this scene.", this);
        }

        public bool AttachInventory(Interactor interactor)
        {
            if (!interactor) return false;

            if (interactor.interactorPoints.inventory && interactor.interactorPoints.inventory.currentInteractor)
            { //To set the interactor's previous inventory as ownerless
                //Comment out for owning multiple inventories
                interactor.interactorPoints.inventory.currentInteractor = null;
            }
            
            interactor.interactorPoints.inventory = this;
            currentInteractor = interactor;
            return true;
        }
		public bool DeattachInventory()
        {
            if (!currentInteractor) return false;

            for (int i = 0; i < currentItems.Length; i++)
            {
                if (!currentItems[i]) continue;
                if (!currentItems[i].interactorObject.pickableOneSettings.onBody) continue;
                if (currentItems[i].interactorObject.pickableOneSettings.invOnBodyPointInstead) continue;

                int bodyPoint = currentItems[i].interactorObject.pickableOneSettings.bodyPoint;

                if (bodyPoint >= 0 && bodyPoint < currentInteractor.interactorPoints.onBodyPoints.Count && currentInteractor.interactorPoints.onBodyPoints[bodyPoint].currentObject == currentItems[i].interactorObject.transform)
                {
                    RemoveItemFromInventory(currentItems[i]);
                }
            }

            currentInteractor.interactorPoints.inventory = null;
            currentInteractor = null;
            return true;
        }

        public void AddItemToInventory(Item item)
        {
            if (!item) return;

            ItemData itemData = ItemDatabase.Instance.GetItemDataByIndex(item.dbIndex);
            int index = IsItemInInventory(item);
            if (index >= 0)
            {
                if (itemData.Stackable)
                {
                    AddAmout(index, 1);
                    item.IntoInventory(this);
                    invUpdated = true;
                }
                else return;
            }
            else
            {
                int addIndex = IsInventoryFull();
                if (addIndex >= 0)
                {
                    currentItems[addIndex] = item;
                    currentAmounts[addIndex] = 1;
                    item.IntoInventory(this);
                    invUpdated = true;
                }
                else Debug.Log("There is no empty space on Inventory!", this);
            }
        }
        public void RemoveItemFromInventory(Item item)
        {
            if (!item) return;

            ItemData itemData = ItemDatabase.Instance.GetItemDataByIndex(item.dbIndex);
            int index = IsItemInInventory(item);

            if (index >= 0)
            {
                if (itemData.Stackable)
                {
                    DecreaseAmount(index, 1);
                    if (currentAmounts[index] == 0)
                    {
                        currentItems[index] = null;
                        item.OutOfInv();
                    }
                }
                else
                {
                    currentItems[index] = null;
                    currentAmounts[index] = 0;
                    item.OutOfInv();
                }
                invUpdated = true;
            }
            else Debug.LogWarning("Item not found in inventory: " + item.name, item);
        }

        public int IsItemInInventory(Item item)
        {//Returns item index, -1 for not in inv
            if (!item) return -1;

            ItemData itemData = ItemDatabase.Instance.GetItemDataByIndex(item.dbIndex);
            if (itemData.Stackable)
            {
                for (int i = 0; i < currentItems.Length; i++)
                {
                    if (currentItems[i] && currentItems[i].dbIndex == item.dbIndex) return i;
                }
            }
            else
            {
                for (int i = 0; i < currentItems.Length; i++)
                {
                    if (currentItems[i] == item) return i;
                }
            }
            return -1;
        }
        public int IsInventoryFull()
        {//Returns first null index, -1 for full
            for (int i = 0; i < currentItems.Length; i++)
            {
                if (!currentItems[i]) return i;
            }
            return -1;
        }
        public int GetEmptyInventorySpace()
        {
            int emptyCount = currentItems.Length;
            for (int i = 0; i < currentItems.Length; i++)
            {
                if (currentItems[i]) emptyCount--;
            }
            return emptyCount;
        }
        public void ReorderInventory()
        {
            List<(Item, int, int)> combinedList = new List<(Item, int, int)>();
            for (int i = 0; i < currentItems.Length; i++)
            {
                combinedList.Add((currentItems[i], currentAmounts[i], i));
            }

            // Sort combined based on currentItems(nulls come last) with original non-null order
            combinedList.Sort((x, y) =>
            {
                if (x.Item1 == null && y.Item1 == null) return 0;
                if (x.Item1 == null) return 1;
                if (y.Item1 == null) return -1;

                return x.Item3.CompareTo(y.Item3);
            });

            for (int i = 0; i < combinedList.Count; i++)
            {
                currentItems[i] = combinedList[i].Item1;
                currentAmounts[i] = combinedList[i].Item2;
            }
            reordered = true;
        }
        public void ResizeInventory(Vector2Int newSize)
        {
            inventorySize.x = Mathf.Max(1, newSize.x);
            inventorySize.y = Mathf.Max(1, newSize.y);
            CheckInventorySize();
        }

        public void AddAmout(int index, int add)
        {
            currentAmounts[index] += add;
        }

        public void DecreaseAmount(int index, int sub)
        {
            currentAmounts[index] = Mathf.Max(0, currentAmounts[index] - sub);
        }

		public bool CheckIfItemIsInInventory(ItemData item)
		{
			for (int i = 0; i < currentItems.Length; i++)
			{
                if (!currentItems[i]) continue;

				if (currentItems[i].dbIndex == item.Id) return true;
			}
			return false;
		}

        private void CheckInventorySize()
        {
            if (oldInventorySize == inventorySize) return;

            inventorySize.x = Mathf.Max(1, inventorySize.x);
            inventorySize.y = Mathf.Max(1, inventorySize.y);
            int size = inventorySize.x * inventorySize.y;
            if (currentItems == null || currentAmounts == null) CreateInvArray(size);
            if (size < currentItems.Length || size < currentAmounts.Length)
            {
                int emptyCount = GetEmptyInventorySpace();
                if ((currentItems.Length - emptyCount) > size)
                {
                    inventorySize = oldInventorySize;
                    Debug.LogWarning("There is no enough empty space for new inventory size. Remove items first!", this);
                    return;
                }
                if (IsInventoryFull() >= 0) ReorderInventory();

                Array.Resize(ref currentItems, size);
                Array.Resize(ref currentAmounts, size);
            }
            else if (size > currentItems.Length || size > currentAmounts.Length)
            {
                int currentSize = (currentItems.Length < currentAmounts.Length) ? currentItems.Length : currentAmounts.Length;

                Item[] newItemArray = new Item[size];
                int[] newAmountArray = new int[size];
                for (int i = 0; i < currentSize; i++)
                {
                    newItemArray[i] = currentItems[i];
                    newAmountArray[i] = currentAmounts[i];
                }
                currentItems = newItemArray;
                currentAmounts = newAmountArray;
            }
            oldInventorySize = inventorySize;
        }
        private void CreateInvArray(int size)
        {
            currentItems = new Item[size];
            currentAmounts = new int[size];
            for (int i = 0; i < size; i++)
                currentAmounts[i] = 0;
            oldInventorySize = inventorySize;
        }
	}
}
