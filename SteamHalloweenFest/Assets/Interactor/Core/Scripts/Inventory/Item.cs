using UnityEngine;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#inventorycs")]
    public class Item : MonoBehaviour
    {
        [Tooltip("The database index that this item will use is from the ItemDatabase component.")]
        public int dbIndex = -1;
        [Tooltip("InteractorObject, which this item belongs.")]
        public InteractorObject interactorObject;
        [Tooltip("Shows the current inventory if this item is dropped into one.")]
        [ReadOnly] public Inventory currentInventory;

        public void IntoInventory(Inventory gotoInventory)
        {
            this.currentInventory = gotoInventory;
        }

        public void OutOfInv()
        {
            currentInventory = null;
        }

        public bool PickItem(Interactor usingInteractor)
        {
            if (!interactorObject)
            {
                Debug.LogWarning("InteractorObject is not assigned on Item.", this);
                return false;
            }
            if (!interactorObject.pickableOneSettings)
            {
                Debug.LogWarning("Interaction type is not Pickable One Hand.", interactorObject);
                return false;
            }
            if (!usingInteractor)
            {
                Debug.LogWarning("Interactor is attempting to pick up this object, but it is null. If the player is using it from an inventory, assign PlayerInteractor to InventoryRenderer.");
                return false;
            }
            if (!currentInventory)
            {
                Debug.LogWarning("InteractorObject is currently not in an inventory.", this);
                return false;
            }

            if (usingInteractor.interactorPoints.inventory == currentInventory)
            {//Owner is using
                if (usingInteractor.ForcePick(interactorObject, false))
                {
                    currentInventory.RemoveItemFromInventory(this);
                    return true;
                }
                else
                {
                    Debug.Log("Unable to pick up this object. Effectors are not available.", this);
                    return false;
                }
            }
            
            if (!usingInteractor.CanPick(interactorObject))
            {
                Debug.Log("Unable to pick up this object; either it is out of range/angles, the InteractorObject has already been used, or the effector is not available.", usingInteractor);
                return false;
            }

            //Other interactor is using other than owner of inv
            usingInteractor.ForcePick(interactorObject, true);
            currentInventory.RemoveItemFromInventory(this);
            return true;
        }
    }
}
