using UnityEngine;
using System.Collections;
using razz;

public class ChangeInventory : MonoBehaviour
{
    public Interactor interactor;
    public InventoryRenderer invRend;
    public Inventory inv1;
    public Inventory inv2;

    public InteractionHelper helper;
    private bool _opened;

    [ContextMenu("ChangeInventories")]
    public void ChangeInventories()
    {
        if (invRend.currentInventory == inv1)
        {
            if (inv1.currentInteractor == interactor) inv1.DeattachInventory();
            inv2.AttachInventory(interactor);
            invRend.ChangeInventoryToRender(inv2);
            if (helper) StartCoroutine(OpenInventoryCoroutine());
        }
        else
        {
            if (inv2.currentInteractor == interactor) inv2.DeattachInventory();
            inv1.AttachInventory(interactor);
            invRend.ChangeInventoryToRender(inv1);
            if (helper) StartCoroutine(OpenInventoryCoroutine());
        }
    }

    private IEnumerator OpenInventoryCoroutine()
    {
        if (_opened)
        {
            helper.ToggleRotate();
            invRend.HideInventory();
            _opened = false;
        }
        else
        {
            helper.ToggleRotate();
            yield return new WaitForSeconds(helper.rotationDuration);
            invRend.ShowInventory();
            _opened = true;
        }
    }

    public void OpenInvCoroutine()
    {
        if (helper)
        {
            StartCoroutine(OpenInventoryCoroutine());
        }
    }
}
