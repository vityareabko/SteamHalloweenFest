using UnityEngine;

namespace razz
{
    public class InteractionSpawner : MonoBehaviour
    {
        public GameObject prefab;
        public PickupTestInputs pickupTestInputs;

        [ContextMenu("Spawn")]
        public void Spawn()
        {
            if (!prefab) return;

            GameObject spawnedGO = Instantiate(prefab);
            spawnedGO.name = prefab.name;
            spawnedGO.transform.position = transform.position;
            spawnedGO.transform.rotation = transform.rotation;
            if (pickupTestInputs)
            {
                InteractorObject intObj = spawnedGO.GetComponentInChildren<InteractorObject>();
                if (!intObj) return;

                for (int i = 0; i < pickupTestInputs.holdInteractions.Count; i++)
                {
                    if (pickupTestInputs.holdInteractions[i].interactorObject.name == spawnedGO.name)
                    {
                        pickupTestInputs.holdInteractions[i].interactorObject = intObj;
                    }
                }

                for (int i = 0; i < pickupTestInputs.dropInteractions.Count; i++)
                {
                    if (pickupTestInputs.dropInteractions[i].interactorObject.name == spawnedGO.name)
                    {
                        pickupTestInputs.dropInteractions[i].interactorObject = intObj;
                    }
                }

                for (int i = 0; i < pickupTestInputs.holdDropInteractions.Count; i++)
                {
                    if (pickupTestInputs.holdDropInteractions[i].interactorObject.name == spawnedGO.name)
                    {
                        pickupTestInputs.holdDropInteractions[i].interactorObject = intObj;
                    }
                }
            }
        }
    }
}
