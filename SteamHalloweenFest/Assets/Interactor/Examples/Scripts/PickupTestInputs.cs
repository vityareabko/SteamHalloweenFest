using UnityEngine;
using System;
using System.Collections.Generic;

namespace razz
{
    public class PickupTestInputs : MonoBehaviour
    {
        public Interactor interactor;
        [Header("Change Hold Type Examples")]
        public List<HoldInteractions> holdInteractions = new List<HoldInteractions>();
        [Header("Change Drop Type Examples")]
        public List<DropInteractions> dropInteractions = new List<DropInteractions>();
        [Header("Advanced Hold and Drop Examples")]
        public List<HoldDropInteractions> holdDropInteractions = new List<HoldDropInteractions>();
        [Header("Shotgun Reload Key Example")]
        public KeyCode reloadKey;
        public InteractorObject reloadWeapon;
        public InteractorObject reloadObject;
        [Header("AK Inventory Key Example")]
        public KeyCode weaponInvKey;
        public InteractorObject inventoryWeapon;
        [Header("Other")]
        public KeyCode throwAll;

        private void Update()
        {
            for (int i = 0; i < holdInteractions.Count; i++)
            {
                if (Input.GetKeyDown(holdInteractions[i].key))
                {
                    interactor.StartStopInteraction(holdInteractions[i].interactorObject, holdInteractions[i].holdType, holdInteractions[i].holdPoint);
                }
            }

            for (int i = 0; i < dropInteractions.Count; i++)
            {
                if (Input.GetKeyDown(dropInteractions[i].key))
                {
                    Interactor.FullBodyBipedEffector usingHand = interactor.GetEffectorInteractingWith(dropInteractions[i].interactorObject);
                    if (usingHand == Interactor.FullBodyBipedEffector.LeftHand)
                        dropInteractions[i].interactorObject.pickableOneSettings.throwDown = true;
                    else dropInteractions[i].interactorObject.pickableOneSettings.throwDown = false;

                    interactor.StartStopInteraction(dropInteractions[i].interactorObject, dropInteractions[i].dropType);
                }
            }

            for (int i = 0; i < holdDropInteractions.Count; i++)
            {
                if (Input.GetKeyDown(holdDropInteractions[i].key))
                {
                    if (holdDropInteractions[i].interactorObject.currentInteractor != interactor) return;

                    Interactor.FullBodyBipedEffector otherEffector = Interactor.FullBodyBipedEffector.LeftHand;
                    if (holdDropInteractions[i].effectorType == Interactor.FullBodyBipedEffector.LeftHand)
                    {
                        otherEffector = Interactor.FullBodyBipedEffector.RightHand;
                    }

                    if (holdDropInteractions[i].interactorObject.used)
                    {
                        interactor.StartStopInteraction(holdDropInteractions[i].interactorObject, holdDropInteractions[i].dropType);
                    }
                    else if (holdDropInteractions[i].interactorObject.GetTargetForEffectorType((int)otherEffector).gameObject.activeInHierarchy)
                    {
                        if (holdDropInteractions[i].disableOtherTarget)
                        {
                            holdDropInteractions[i].interactorObject.DisableOtherTargets(holdDropInteractions[i].effectorType);
                        }

                        holdDropInteractions[i].interactorObject.ResetUseableEffectors();
                        interactor.SetIntObjUseables(holdDropInteractions[i].interactorObject, holdDropInteractions[i].effectorType);

                        interactor.StartStopInteraction(holdDropInteractions[i].interactorObject, holdDropInteractions[i].holdType, holdDropInteractions[i].holdPoint);
                    }
                    else
                    {
                        interactor.StartStopInteraction(holdDropInteractions[i].interactorObject, holdDropInteractions[i].holdType, holdDropInteractions[i].holdPoint);
                    }

                }
            }

            if (Input.GetKeyDown(throwAll))
            {
                InteractorObject currentInteraction = interactor.GetCurrentInteractorObject(Interactor.FullBodyBipedEffector.LeftHand);
                if (currentInteraction && currentInteraction.pickableOneSettings)
                {
                    currentInteraction.pickableOneSettings.throwDown = true;
                    interactor.StartStopInteraction(currentInteraction, PickableOne.DropType.Throw);
                    return;
                }

                currentInteraction = interactor.GetCurrentInteractorObject(Interactor.FullBodyBipedEffector.RightHand);
                if (currentInteraction && currentInteraction.pickableOneSettings)
                {
                    currentInteraction.pickableOneSettings.throwDown = false;
                    interactor.StartStopInteraction(currentInteraction, PickableOne.DropType.Throw);
                }
            }

            if (Input.GetKeyDown(reloadKey))
            {
                if (interactor.IsInteractingWith(reloadWeapon))
                {
                    interactor.StartStopInteraction(reloadObject);
                }
            }

            if (Input.GetKeyDown(weaponInvKey))
            {
                if (interactor.IsInteractingWith(inventoryWeapon))
                {
                    ChangeInventory changeInv = inventoryWeapon.GetComponent<ChangeInventory>();
                    if (changeInv) changeInv.ChangeInventories();
                }
            }
        }

        [Serializable]
        public class HoldInteractions
        {
            public InteractorObject interactorObject;
            public KeyCode key;
            public PickableOne.HoldType holdType;
            public int holdPoint;
        }

        [Serializable]
        public class DropInteractions
        {
            public InteractorObject interactorObject;
            public KeyCode key;
            public PickableOne.DropType dropType;
        }

        [Serializable]
        public class HoldDropInteractions
        {
            public InteractorObject interactorObject;
            public KeyCode key;
            public PickableOne.HoldType holdType;
            public int holdPoint;

            public PickableOne.DropType dropType;
            public bool disableOtherTarget;
            public Interactor.FullBodyBipedEffector effectorType;
        }
    }
}
