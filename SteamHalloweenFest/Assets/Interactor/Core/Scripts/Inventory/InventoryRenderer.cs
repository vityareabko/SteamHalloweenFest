using UnityEngine;
using UnityEngine.UI;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#inventorycs")]
    public class InventoryRenderer : MonoBehaviour
    {
        [Tooltip("Inventory to show when inventory ui is on.")]
        public Inventory currentInventory;
        [Tooltip("Interactor that will use items from the inventory.")]
        public Interactor playerInteractor;
        [Tooltip("Closes inventory ui when double clicked on any item to pick it up.")]
        public bool hideInventoryOnPick;

        [Header("Generic Settings")]
        public GameObject canvasObject;
        public GameObject slotPrefab;
        public GameObject slotItemDataPrefab;
        public RectTransform inventoryPanel;
        public RectTransform slotPanel;
        public Text titleText;
        public Tooltip tooltip;

        [HideInInspector] public Slot[] slots;
        [HideInInspector] public bool invOpen;

        private int _width, _height;
        private Vector2 _baseSize = new Vector2(96, 107);
        private Vector2 _slotSize = new Vector2(60, 60);
        private Inventory _lastInventory;
        private int _lastSize;

        private SlotItemData[] _slotItemDatas;

        private void Start()
        {
            _lastInventory = null;
            _lastSize = 0;
            if (canvasObject) canvasObject.SetActive(false);
            if (!playerInteractor) Debug.Log("PlayerInteractor is not assigned. If you wish to use objects from the Inventory window, assign it.", this);
        }
        private void Update()
        {
            if (!invOpen) return;

            if (tooltip.isActiveAndEnabled)
            {
                tooltip.transform.position = BasicInput.GetMousePosition();
            }

            if (invOpen)
            {
                if (CheckNewInventory() || currentInventory.reordered)
                {
                    CreateSlots();
                    UpdateSlotItemDatas();
                    currentInventory.invUpdated = false;
                    currentInventory.reordered = false;
                }

                if (currentInventory.invUpdated)
                {
                    UpdateSlotItemDatas();
                    currentInventory.invUpdated = false;
                }
            }
        }

        public void ChangeInventoryToRender(Inventory inventory)
        {
            if (inventory) currentInventory = inventory;
        }

        public void ShowInventory()
        {
            if (!canvasObject || !currentInventory)
            {
                Debug.LogWarning("Canvas or current inventory is null!", this);
                HideInventory();
                return;
            }
            if (!slotPrefab || !slotItemDataPrefab)
            {
                if (slotItemDataPrefab) Debug.LogWarning("slotPrefab is null!", this);
                else Debug.LogWarning("slotItemPrefab is null!", this);
                return;
            }
            if (!inventoryPanel || !slotPanel || !titleText || !tooltip)
            {
                Debug.LogWarning("inventoryPanel, slotPanel, titleText or tooltip is/are null!", this);
                return;
            }
            if (!currentInventory.isActiveAndEnabled)
            {
                Debug.LogWarning("currentInventory is not active in the scene!", currentInventory);
                return;
            }
            if (invOpen) return;

            invOpen = true; //open first

            if (CheckNewInventory())
            {
                CreateSlots();
            }
            UpdateSlotItemDatas();
            currentInventory.invUpdated = false;

            canvasObject.SetActive(true);
            if (tooltip) tooltip.Deactivate();
        }
        public void HideInventory()
        {
            invOpen = false; //close first
            if (tooltip) tooltip.Deactivate();
            if (canvasObject) canvasObject.SetActive(false);
        }

        public void UpdateSlotItemDatas(Inventory inventory)
        {
            if (inventory != currentInventory) return;

            UpdateSlotItemDatas();
        }
        private void UpdateSlotItemDatas()
        {
            for (int i = 0; i < currentInventory.currentItems.Length; i++)
            {
                if (currentInventory.currentItems[i] && currentInventory.currentAmounts[i] > 0)
                {
                    ItemData itemToAdd = ItemDatabase.Instance.GetItemDataByIndex(currentInventory.currentItems[i].dbIndex);

                    if (_slotItemDatas[i])
                    {
                        UpdateSlotItemData(_slotItemDatas[i], itemToAdd, i);
                    }
                    else
                    {
                        _slotItemDatas[i] = AddSlotItemData(itemToAdd, i);

                        if (_slotItemDatas[i] == null)
                        {
                            HideInventory();
                            continue;
                        }
                    }
                }
                else
                {
                    if (_slotItemDatas[i]) Destroy(_slotItemDatas[i].gameObject);
                }
            }
        }
        private void CreateSlots()
        {
            _width = Mathf.Max(currentInventory.inventorySize.x, 1);
            _height = Mathf.Max(currentInventory.inventorySize.y, 1);
            inventoryPanel.sizeDelta = new Vector2(_baseSize.x + (_width - 1) * _slotSize.x, _baseSize.y + (_height - 1) * _slotSize.y);

            ClearPrevious();
            _lastInventory = currentInventory;
            _lastSize = currentInventory.currentItems.Length;
            titleText.text = currentInventory.inventoryName;

            slots = new Slot[currentInventory.currentItems.Length];
            _slotItemDatas = new SlotItemData[currentInventory.currentItems.Length];

            for (int i = 0; i < currentInventory.currentItems.Length; i++)
            {
                GameObject newSlot = Instantiate(slotPrefab);
                slots[i] = newSlot.GetComponent<Slot>();
                slots[i].slotIndex = i;
                slots[i].SetInvRenderer(this, currentInventory);
                newSlot.transform.SetParent(slotPanel);
                newSlot.transform.localScale = Vector3.one;
                slots[i].name = "InvSlot " + i;
            }
        }

        public SlotItemData GetSlotItemData(int index)
        {
            if (index < _slotItemDatas.Length && _slotItemDatas[index])
            {
                return _slotItemDatas[index];
            }
            else return null;
        }
        public void ReplaceSlotItemDatas(int replaced, int dragged)
        {
            if (replaced < _slotItemDatas.Length && dragged < _slotItemDatas.Length)
            {
                SlotItemData temp = _slotItemDatas[replaced];
                _slotItemDatas[replaced] = _slotItemDatas[dragged];
                _slotItemDatas[dragged] = temp;
            }
        }

        private SlotItemData AddSlotItemData(ItemData itemToAdd, int slotIndex)
        {
            GameObject newSlotItemDataObject = Instantiate(slotItemDataPrefab);
            SlotItemData newSlotItemData = newSlotItemDataObject.GetComponent<SlotItemData>();
            if (!newSlotItemData.SetData(this, currentInventory, slotIndex, tooltip))
                return null;

            newSlotItemDataObject.transform.SetParent(slots[slotIndex].transform);
            newSlotItemDataObject.transform.localPosition = Vector3.zero;
            newSlotItemDataObject.transform.localScale = Vector3.one;
            newSlotItemDataObject.name = "Item: " + itemToAdd.Title;
            return newSlotItemData;
        }
        private bool UpdateSlotItemData(SlotItemData slotItemData, ItemData itemToAdd, int slotIndex)
        {
            if (!slotItemData.SetData(this, currentInventory, slotIndex, tooltip))
                return false;

            slotItemData.transform.SetParent(slots[slotIndex].transform);
            slotItemData.transform.localPosition = Vector3.zero;
            slotItemData.transform.localScale = Vector3.one;
            slotItemData.name = "Item: " + itemToAdd.Title;
            return true;
        }

        private bool CheckNewInventory()
        {
            if (currentInventory != _lastInventory || _lastSize != currentInventory.currentItems.Length)
            {
                return true;
            }
            return false;
        }

        private void ClearPrevious()
        {
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    Destroy(slots[i].gameObject);
                }
            }
            if (_slotItemDatas != null)
            {
                for (int i = 0; i < _slotItemDatas.Length; i++)
                {
                    if (_slotItemDatas[i])
                    {
                        Destroy(_slotItemDatas[i].gameObject);
                    }
                }
            }
        }
    }
}
