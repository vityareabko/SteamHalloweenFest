using UnityEngine;
using UnityEditor;

namespace razz
{
    [CustomEditor(typeof(CreateInventoryIcon))]
    public class CreateInventoryIconEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CreateInventoryIcon iconCreator = (CreateInventoryIcon)target;

            if (GUILayout.Button("Take All Snapshots"))
            {
                iconCreator.TakeAllSnapshots();
            }
        }
    }
}
