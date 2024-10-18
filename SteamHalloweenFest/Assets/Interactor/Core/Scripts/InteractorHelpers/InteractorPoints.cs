using UnityEngine;
using System.Collections.Generic;
using System;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#interactorpointscs")]
    public class InteractorPoints : MonoBehaviour
    {
        [Tooltip("Current using inventory to send pick up items.")]
        public Inventory inventory;
        [Header("Hold Points")]
        [Tooltip("Transform list for holding points. These points can be placed into bone transforms so they will follow those bones.")]
        public List<Transform> holdPoints = new List<Transform>();
        [Header("Inventory Body Points")]
        [Tooltip("Transform list for onBody objects to be dropped.")]
        public List<BodyPoint> onBodyPoints = new List<BodyPoint>();
        private Transform _dropGroundPoint;
        public Transform dropGroundPoint
        {
            get
            {
                if (!_dropGroundPoint)
                {
                    GameObject dropGroundPoint = new GameObject("DropGroundPoint");
                    _dropGroundPoint = dropGroundPoint.transform;
                    _dropGroundPoint.parent = this.transform;
                    _dropGroundPoint.position = this.transform.position;
                    _dropGroundPoint.rotation = this.transform.rotation;
                }
                return _dropGroundPoint;
            }
        }

        [Header("Throw Points")]
        [Tooltip("Transform for throw up position.")]
        public Transform throwUpPoint;
        [Tooltip("Transform for throw down position.")]
        public Transform throwDownPoint;
        [Tooltip("LineRenderer component to visualize throw projectile.")]
        public LineRenderer lineRenderer;
        public float lineLenght = 1f;

        private bool _showPath;
        private Transform _throwPoint;
        private Rigidbody _throwRb;
        private PickableOne _usedPickableOne;
        private List<Vector3> linePositions;

        private void Update()
        {
            if (lineRenderer && _showPath) ShowPath();
        }

        public void ShowPath(Transform throwPos, PickableOne usedPickableOne, Rigidbody rigidbody)
        {
            if (!lineRenderer) return;

            _showPath = true;
            lineRenderer.enabled = true;
            _throwPoint = throwPos;
            _usedPickableOne = usedPickableOne;
            _throwRb = rigidbody;
        }
        public void TurnOffPath()
        {
            if (!lineRenderer) return;

            _showPath = false;
            lineRenderer.enabled = false;
        }

        private void ShowPath()
        {
            if (!_usedPickableOne || !lineRenderer || !_throwPoint || !_throwRb)
            {
                if (lineRenderer) lineRenderer.enabled = false;
                _showPath = false;
                return;
            }

            linePositions = new List<Vector3>();
            Vector3 sidedThrowVector = new Vector3(_usedPickableOne.throwVector.x * _usedPickableOne.handDir, _usedPickableOne.throwVector.y, _usedPickableOne.throwVector.z);
            sidedThrowVector = transform.right * sidedThrowVector.x + transform.up * sidedThrowVector.y + transform.forward * sidedThrowVector.z;
            
            float timeStep = lineLenght / (lineRenderer.positionCount - 1);
            Vector3 initialVelocity = sidedThrowVector.normalized * _usedPickableOne.throwForce;

            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                float time = i * timeStep;

                //Not quite right like internal Unity drag calc but ok enough.
                initialVelocity *= Mathf.Clamp01(1f - _throwRb.linearDamping * timeStep);
                Vector3 currentPosition = CalculatePosition(_throwPoint.position, initialVelocity, time);
                linePositions.Add(currentPosition);
            }
            lineRenderer.SetPositions(linePositions.ToArray());
        }
        private Vector3 CalculatePosition(Vector3 initialPosition, Vector3 initialVelocity, float time)
        {
            return initialPosition + initialVelocity * time + 0.5f * Physics.gravity * time * time;
        }

        /*public void AddBodyPoint(Transform bodyPoint, Transform bodyPointPivot)
        {
            BodyPoint bp = new BodyPoint();
            bp.bodyPoint = bodyPoint;
            bp.bodyPointPivot = bodyPointPivot;
            SetBodyPointValues(bp);
            bodyPoints.Add(bp);
        }

        public void UpdateBodyPointPhysics(BodyPoint bp)
        {
            if (!bp.bodyPoint || !bp.bodyPointPivot) return;

            bp.parentAngVelocity = (bp.bodyPoint.eulerAngles - bp.prevParentRotation) / Time.deltaTime;

            Quaternion swingRotation = Quaternion.Euler(bp.parentAngVelocity * bp.swingStrength);
            bp.targetRotation = Quaternion.Euler(bp.initialRotationEuler) * swingRotation;

            bp.bodyPointPivot.localRotation = Quaternion.Slerp(bp.bodyPointPivot.localRotation, Quaternion.Euler(bp.initialRotationEuler), Time.deltaTime * bp.damping);

            bp.bodyPointPivot.localRotation = Quaternion.Slerp(bp.bodyPointPivot.localRotation, bp.targetRotation, Time.deltaTime * bp.swingSmoothing);

            bp.prevParentRotation = bp.bodyPoint.eulerAngles;
        }

        private void SetBodyPointValues(BodyPoint bp)
        {
            if (bp.bodyPoint && bp.bodyPointPivot)
            {
                bp.bodyPointPivot.localRotation = Quaternion.identity;
                bp.initialRotationEuler = bp.bodyPointPivot.localEulerAngles;
                bp.prevParentRotation = bp.bodyPoint.eulerAngles;
            }
        }*/
    }

    [Serializable]
    public class BodyPoint
    {
        [Tooltip("If you want placing animation, assign both bodyPoint and bodyPointEnd so object will be animated between those hand positions when dropping it. If you don't want it, only assign bodyPoint and this will be final location. Otherwise bodyPointEnd will be final position.")] public Transform bodyPoint;
        [Tooltip("If you want placing animation, assign both bodyPoint and bodyPointEnd so object will be animated between those hand positions when dropping it. If you don't want it, only assign bodyPoint and this will be final location. Otherwise bodyPointEnd will be final position.")] public Transform bodyPointEnd;
        [Tooltip("Speed multiplier for placing animation when you have both bodyPoint and bodyPointEnd transforms.")] public float endSpeedMult = 1f;
        [Tooltip("Current placed object. If it is not null, you can't place more objects to this same bodyPoint.")] [ReadOnly] public Transform currentObject;

        /*public bool usePhysics;
        public Transform bodyPointPivot;
        public float swingStrength = 0.5f;
        public float swingSmoothing = 10f;
        public float damping = 0.5f;

        [HideInInspector] public Vector3 initialRotationEuler;
        [HideInInspector] public Vector3 prevParentRotation;
        [HideInInspector] public Vector3 parentAngVelocity;
        [HideInInspector] public Quaternion targetRotation;*/
    }
}
