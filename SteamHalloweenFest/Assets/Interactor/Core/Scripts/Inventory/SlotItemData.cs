using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace razz
{
    public class SlotItemData : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
		public CanvasGroup canvasGroupComponent;
		public Text amountTextComponent;
		public Image imageComponent;

		[HideInInspector] public Item item;
		[HideInInspector] public int slotIndex;

		private InventoryRenderer _invRend;
		private Inventory _inventory;
		private Tooltip _tooltip;
		private Vector2 _dragOffset;
		private bool _closedBeforeEndDrag;

		private float _lastTapTime;
		private const float _doubleTapTimeThreshold = 0.5f;

		public bool SetData(InventoryRenderer invRend, Inventory inventory, int slotIndex, Tooltip tooltip)
        {
			_inventory = inventory;
			item = _inventory.currentItems[slotIndex];
			if (item.dbIndex < 0 || item.dbIndex >= ItemDatabase.Instance.database.Count)
            {
				Debug.LogWarning("This item has wrong database index.", item);
				return false;
			}

			_invRend = invRend;
			imageComponent.sprite = ItemDatabase.Instance.GetItemDataByIndex(item.dbIndex).Sprite;
			this.slotIndex = slotIndex;
			_tooltip = tooltip;
			SetAmountText();
			return true;
		}
		public void SetAmountText()
        {
            if (_inventory.currentAmounts[slotIndex] > 1) amountTextComponent.text = _inventory.currentAmounts[slotIndex].ToString();
            else amountTextComponent.text = "";
		}

		private bool IsPointerOverUIObject()
		{
			PointerEventData eventData = new PointerEventData(EventSystem.current);

			if (Input.touchCount > 0)
				eventData.position = Input.GetTouch(0).position;
			else eventData.position = Input.mousePosition;

			GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
			List<RaycastResult> results = new List<RaycastResult>();
			raycaster.Raycast(eventData, results);

			foreach (RaycastResult result in results)
			{
				if (result.gameObject == gameObject) return true;
			}
			return false;
		}
		private bool IsDoubleTapOnUIObject()
		{
			if (IsPointerOverUIObject())
			{
				float timeSinceLastTap = Time.time - _lastTapTime;
				bool isDoubleTap = timeSinceLastTap < _doubleTapTimeThreshold;
				_lastTapTime = Time.time;
				return isDoubleTap;
			}
			return false;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!canvasGroupComponent)
            {
				Debug.Log("No CanvasGroup!", this); 
				return;
			}
			if (item == null) return;

			transform.SetParent(transform.parent.parent);
			transform.position = eventData.position - _dragOffset;
			canvasGroupComponent.blocksRaycasts = false;
			_closedBeforeEndDrag = true;
		}
		public void OnDrag(PointerEventData eventData)
		{
			if (item == null) return;

			transform.position = eventData.position - _dragOffset;
		}
		public void OnEndDrag(PointerEventData eventData)
		{
			if (!canvasGroupComponent)
			{
				Debug.Log("No CanvasGroup!", this);
				return;
			}

			transform.SetParent(_invRend.slots[slotIndex].transform);
			transform.position = _invRend.slots[slotIndex].transform.position;
			canvasGroupComponent.blocksRaycasts = true;
			_closedBeforeEndDrag = false;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (IsDoubleTapOnUIObject())
			{
				if (!item.PickItem(_invRend.playerInteractor)) return;
				
				_tooltip.Deactivate();
				if (_invRend.hideInventoryOnPick) _invRend.HideInventory();
				return;
			}

			_dragOffset = eventData.position - new Vector2(transform.position.x, transform.position.y);
		}
		public void OnPointerEnter(PointerEventData eventData)
		{
			_tooltip.Activate(item);
		}
		public void OnPointerExit(PointerEventData eventData)
		{
			_tooltip.Deactivate();
		}

		private void OnDisable()
		{//To prevent bugs when inv closed while dragging
			if (_invRend && gameObject && (!_invRend.invOpen || _closedBeforeEndDrag)) Destroy(gameObject);
		}
	}
}
