using UnityEngine;
using UnityEngine.EventSystems;

namespace razz
{
    public class Slot : MonoBehaviour, IDropHandler
	{
		public int slotIndex;

		private InventoryRenderer _invRend;
		private Inventory _inventory;

		public void SetInvRenderer(InventoryRenderer invRend, Inventory inventory)
        {
			_invRend = invRend;
			_inventory = inventory;
		}

		public void OnDrop(PointerEventData eventData)
		{
			if (!_invRend || !_inventory) return;

			SlotItemData draggedItem = eventData.pointerDrag.GetComponent<SlotItemData>();
			if (_inventory.currentItems[slotIndex] == null)
			{
				_invRend.ReplaceSlotItemDatas(slotIndex, draggedItem.slotIndex);

				_inventory.currentItems[slotIndex] = draggedItem.item;
				_inventory.currentAmounts[slotIndex] = _inventory.currentAmounts[draggedItem.slotIndex];

				_inventory.currentItems[draggedItem.slotIndex] = null;
				_inventory.currentAmounts[draggedItem.slotIndex] = 0;
				draggedItem.slotIndex = slotIndex;
			}
			else if (draggedItem.slotIndex != slotIndex)
			{
				SlotItemData replacedItemData = _invRend.GetSlotItemData(slotIndex);
				_invRend.ReplaceSlotItemDatas(replacedItemData.slotIndex, draggedItem.slotIndex);

				int replacedItemAmount = _inventory.currentAmounts[slotIndex];
				Item replacedItem = _inventory.currentItems[slotIndex];

				_inventory.currentItems[slotIndex] = draggedItem.item;
				_inventory.currentAmounts[slotIndex] = _inventory.currentAmounts[draggedItem.slotIndex];
				replacedItemData.slotIndex = draggedItem.slotIndex;

				_inventory.currentItems[draggedItem.slotIndex] = replacedItem;
				_inventory.currentAmounts[draggedItem.slotIndex] = replacedItemAmount;
				draggedItem.slotIndex = slotIndex;

				replacedItemData.transform.SetParent(_invRend.slots[replacedItemData.slotIndex].transform);
				replacedItemData.transform.position = _invRend.slots[replacedItemData.slotIndex].transform.position;

				draggedItem.transform.SetParent(transform);
				draggedItem.transform.position = transform.position;
			}
			//_invRend.UpdateSlotItemDatas(_inventory);
		}
	}
}
