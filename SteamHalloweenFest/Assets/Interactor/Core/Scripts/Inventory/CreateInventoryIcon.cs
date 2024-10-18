using UnityEngine;
using System.IO;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#inventorycs")]
    public class CreateInventoryIcon : MonoBehaviour
    {
        public GameObject[] targetObjects;
        [Range(0, 359)] public int shotAngle = 45;
        public Orientation orientation = Orientation.Local;
        [Range(10, 500)] public float zoomPercentage = 100;
        public int resolution = 128;
        public string customPath = "Assets/Icons";

        public enum Orientation
        {
            Local,
            Global
        }

        public static void TakeAllSnapshotsMenuItem()
        {
            CreateInventoryIcon iconCreator = FindObjectOfType<CreateInventoryIcon>();
            if (iconCreator != null)
                iconCreator.TakeAllSnapshots();
            else Debug.LogError("CreateInventoryIcon script not found in the scene.");
        }

        public void TakeAllSnapshots()
        {
            Camera cam = new GameObject("IconCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            int temporaryLayer = 31; //Using a temp layer to isolate objects
            cam.cullingMask = 1 << temporaryLayer;

            foreach (var targetObject in targetObjects)
            {
                if (targetObject == null)
                {
                    Debug.LogWarning("Target object is null. Skipping.");
                    continue;
                }

                int originalLayer = targetObject.layer;
                SetLayerRecursively(targetObject.transform, temporaryLayer);

                Bounds combinedBounds = CalculateCombinedBounds(targetObject.transform);
                float originalDist = Mathf.Max(combinedBounds.extents.z, combinedBounds.extents.x, combinedBounds.extents.y) * 2.2f;
                float dist = originalDist * (zoomPercentage / 100f);
                Vector3 camPos = GetCameraPosition(targetObject, combinedBounds.center, shotAngle, dist);
                cam.transform.position = camPos;
                cam.transform.LookAt(combinedBounds.center);

                Texture2D img = RenderToTexture(cam);
                byte[] bytes = img.EncodeToPNG();

                if (!customPath.Contains("Assets/"))
                {
                    Debug.LogWarning("Path must contain 'Assets/'.");
                    Destroy(cam.gameObject);
                    continue;
                }

                string directoryPath = Application.dataPath + customPath.Replace("Assets", "");
                string filePath = directoryPath + "/" + targetObject.name + ".png";

                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                if (File.Exists(filePath)) File.Delete(filePath);
                File.WriteAllBytes(filePath, bytes);

                // Restore the original layer
                SetLayerRecursively(targetObject.transform, originalLayer);
            }
            DestroyImmediate(cam.gameObject);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private void SetLayerRecursively(Transform parent, int layer)
        {
            parent.gameObject.layer = layer;

            foreach (Transform child in parent)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private Bounds CalculateCombinedBounds(Transform parent)
        {
            Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("No renderers found in the target object and its children.");
                return new Bounds();
            }

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            return combinedBounds;
        }

        private Vector3 GetCameraPosition(GameObject targetObject, Vector3 targetPosition, int angle, float distance)
        {
            float radianAngle = Mathf.Deg2Rad * angle;

            Vector3 forwardDirection = targetObject.transform.forward;
            Vector3 rightDirection = targetObject.transform.right;
            if (orientation == Orientation.Global)
            {
                forwardDirection = Vector3.forward;
                rightDirection = Vector3.right;
            }

            Vector3 rotatedDirection = Mathf.Cos(radianAngle) * forwardDirection + Mathf.Sin(radianAngle) * rightDirection;

            return targetPosition + rotatedDirection * distance;
        }

        Texture2D RenderToTexture(Camera cam)
        {
            RenderTexture rt = new RenderTexture(resolution, resolution, 24);
            rt.Create();
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D result = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
            result.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;
            DestroyImmediate(rt);

            return result;
        }
    }
}
