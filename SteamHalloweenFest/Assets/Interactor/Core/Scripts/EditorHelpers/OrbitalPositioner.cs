using UnityEngine;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#orbitalpositionercs")]
    public class OrbitalPositioner : MonoBehaviour
    {
        [Tooltip("Orbital center object, usualy the player gameobject or its parent whose center is on the ground.")]
        public Transform center;
        [Tooltip("The angle ranges from 0 to 360, where 0 to 179 represent the right side, and 180 to 359 represent the left side.")]
        [Range(0, 359)] public float angle = 0;
        [Tooltip("Vertical position of the object.")]
        [Range(0, 2f)] public float height = 1f;
        [Tooltip("The horizontal distance of the object increases along with the orbit radius.")]
        [Range(0, 2f)] public float distance = 1f;
        [Tooltip("Only affects the gizmos that show up in Sceneview. Helps to see angle slices.")]
        public int gizmoAngles = 30;

        private Vector3 centerPosition;
        private Vector3 centerHeight;
        private Quaternion centerRotation;
        private Color white = new Color(1f, 1f, 1f, 0.4f);
        private Color whiteAlpha = new Color(1f, 1f, 1f, 0.05f);

        [Tooltip("This value is synced with the OrbitalReach component on the player, setting the normalized duration for orbital animations for debugging purposes. This allows you to observe where your player reaches, such as at half (0.5), for example.")]
        [Range(0f, 1f)] public float debugDuration;
        [Tooltip("Adjusts the weight of the OrbitalReach layers. It is normally adjusted automatically. A value of 0 will have no effect on your debugging, while a value of 1 will have full effect.")]
        [Range(0f, 1f)] public float debugWeight;
        [Tooltip("Normalized values for height, angle, and layer weight are utilized by OrbitalReach blend trees. Height and angle range between 1 and -1.")]
        [ReadOnly] public Vector3 debugOrbitalValues;
        [HideInInspector] public OrbitalReach debugOrbitalReach;
        [HideInInspector] public Transform debugPlayerTransform;
        [HideInInspector] public Transform debugTarget;

        public void SetNewOrbitalPosition()
        {
            GetCenter();

            Vector3 offset = centerRotation * Quaternion.Euler(0, angle, 0) * new Vector3(0, height, distance);
            transform.position = centerPosition + offset;

            centerHeight = new Vector3(centerPosition.x, transform.position.y, centerPosition.z);
            Quaternion initialRotation;
            if (transform.position != centerHeight)
                initialRotation = Quaternion.LookRotation(transform.position - centerHeight, Vector3.up);
            else initialRotation = Quaternion.identity;
            Quaternion rotationOffset = Quaternion.Inverse(centerRotation) * initialRotation;
            transform.rotation = centerRotation * rotationOffset;
        }

        private void GetCenter()
        {
            if (center)
            {
                centerPosition = center.position;
                centerRotation = center.rotation;
            }
            else
            {
                centerPosition = Vector3.zero;
                centerRotation = Quaternion.identity;
            }
        }

        public Vector3 DebugCalcOrbital()
        {
            float minHeight = debugOrbitalReach.minHeight;
            float midHeight = debugOrbitalReach.midHeight;
            float maxHeight = debugOrbitalReach.maxHeight;
            float orbitalHeight = Mathf.Clamp(debugTarget.position.y - debugPlayerTransform.position.y, minHeight, maxHeight);
            float orbitalHeightNormalized;
            if (orbitalHeight <= midHeight)
            { //Mid height is where object is level with hand/chest area. So we get weights relative to this point to possible max and min Y points.
                float normalizedValue = Mathf.InverseLerp(minHeight, midHeight, orbitalHeight);
                orbitalHeightNormalized = Mathf.Lerp(-1f, 0f, normalizedValue);
            }
            else
            {
                float normalizedValue = Mathf.InverseLerp(midHeight, maxHeight, orbitalHeight);
                orbitalHeightNormalized = Mathf.Lerp(0f, 1f, normalizedValue);
            }

            //Angle and distance calculations are based on player position without Y to target. So height won't matter because all anims are adjusting body to height.
            Vector3 playerPositionXZ = new Vector3(debugPlayerTransform.position.x, debugPlayerTransform.position.y + debugTarget.position.y, debugPlayerTransform.position.z);
            Vector3 orbitalVector = debugTarget.position - playerPositionXZ;
            orbitalVector = Vector3.ProjectOnPlane(orbitalVector, debugPlayerTransform.up);
            float orbitalAngle = Vector3.SignedAngle(orbitalVector, debugPlayerTransform.right, debugPlayerTransform.up) - 90f;
            if (orbitalAngle > 90f || orbitalAngle <= -180f)
                orbitalAngle = orbitalAngle + 360f;
            float orbitalAngleNormalized = Mathf.InverseLerp(-180f, 180f, orbitalAngle);
            orbitalAngleNormalized = Mathf.Lerp(-1f, 1f, orbitalAngleNormalized); //-1 is hand side, 1 is other hand side.

            debugOrbitalValues = new Vector3(orbitalHeightNormalized, orbitalAngleNormalized, debugWeight);
            return debugOrbitalValues;
        }

        private void OnDrawGizmos()
        {
            float forward = 0;
            if (center) forward = center.transform.eulerAngles.y;
            if (center) centerPosition = center.transform.position;

            Gizmos.color = whiteAlpha;
            Vector3 lastPosition = Vector3.zero;
            for (int i = 0; i <= 360; i++)
            {
                float radianAngle = Mathf.Deg2Rad * i;
                Vector3 position = centerPosition + new Vector3(Mathf.Cos(radianAngle) * distance, height, Mathf.Sin(radianAngle) * distance);

                if (i > 0) Gizmos.DrawLine(lastPosition, position);
                lastPosition = position;
            }

            Gizmos.color = whiteAlpha;
            Vector3 zeroPos = centerPosition + new Vector3(0, height, distance);
            int lines = 360 / gizmoAngles;
            for (int i = 0; i < lines; i++)
            {
                float radianAngle = Mathf.Deg2Rad * ((gizmoAngles * (i + 1)) + forward);
                Vector3 startPos = centerPosition + new Vector3(Mathf.Sin(radianAngle) * distance, height, Mathf.Cos(radianAngle) * distance);
                Vector3 centerHeight = new Vector3(centerPosition.x, height, centerPosition.z);
                if (i == lines - 1)
                {
                    Gizmos.color = white;
                    zeroPos = startPos;
                }
                Gizmos.DrawLine(startPos, centerHeight);
            }

            Gizmos.DrawWireSphere(zeroPos, 0.03f);
        }
    }
}
