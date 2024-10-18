using UnityEditor;
using UnityEngine;

namespace razz
{
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button(new GUIContent("Update Database with Scene Items", "Finds all Item components in the scene and adds their indexes to database if that index is not exist.")))
            {
                if (ItemDatabase.Instance.AddSceneItemsToDatabase())
                {
                    Debug.Log("Database updated with Item Indexes.");
                    EditorUtility.SetDirty(ItemDatabase.Instance);
                }
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Clear All Database"))
            {
                ItemDatabase.Instance.ClearDatabase();
            }
        }
    }
}
