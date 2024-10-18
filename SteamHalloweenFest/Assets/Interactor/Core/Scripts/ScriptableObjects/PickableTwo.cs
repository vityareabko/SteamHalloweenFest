using UnityEngine;

namespace razz
{
    [CreateAssetMenu(fileName = "PickableTwoSettings", menuName = "Interactor/PickableTwoSettings")]
    public class PickableTwo : InteractionTypeSettings
    {
        [Tooltip("Multiplier for moving pickup object, 4 will be similar to this interaction durations (in and out).")]
        [Range(0, 8f)] public float pickupSpeed = 1f;
        [Tooltip("Two handed object target position relative to player. You can adjust this to change position of two handed objects while holding.")]
        public Vector3 holdPoint = new Vector3(0, 0.93f, 0.3f);

        [HideInInspector] public bool twoHandPicked;
        [HideInInspector] public bool pickable;
        [HideInInspector] public Vector3 pickPos;
        [HideInInspector] public bool dropDone;

        private Transform _playerTransform;
        private Transform _oldParentTransform;
        private Collider _col;
        private float _holdWeight;
        private bool _pickReady;
        private float _durationTarget;
        private float _elapsedTime;

        public override void Init(InteractorObject interactorObject)
        {
            base.Init(interactorObject);

            _col = _intObj.col;

            if (!_col) Debug.Log(_intObj.name + " has no collider!");
        }

        public override void UpdateSettings()
        {
            if (twoHandPicked)
            {//It will move object with on pause and will stop when reached.
                if (_elapsedTime >= _durationTarget) return;

                _elapsedTime += Time.deltaTime;
                _holdWeight = Mathf.Clamp01(Ease.FromType(_intObj.easeType)(_elapsedTime / _durationTarget, _intObj.speedCurve));
                Vector3 playerRelativePos = _playerTransform.position + _playerTransform.right * holdPoint.x + _playerTransform.up * holdPoint.y + _playerTransform.forward * holdPoint.z;
                _intObj.transform.position = Vector3.Lerp(pickPos, playerRelativePos, _holdWeight);
            }
        }

        //One or Two handed picks, also uses late update loop for moving objects
        public void TwoHandPick(Transform toParentTransform, Transform childTarget)
        {
            if (_pickReady) return;

            _playerTransform = _intObj.currentInteractor.playerTransform;
            //Durations comes from InteractorObject interaction speed settings
            _durationTarget = _intObj.targetDuration / pickupSpeed;
            dropDone = false;
            _oldParentTransform = _intObj.transform.parent;

            if (_intObj.hasRigid)
            {
                _intObj.rigid.linearVelocity = Vector3.zero;
                _intObj.rigid.isKinematic = true;
            }
            if (_col) _col.enabled = false;

            //Cache object position for pick up on LateUpdate
            pickPos = _intObj.transform.position;
            twoHandPicked = true;
            
            _intObj.transform.parent = toParentTransform;
            _pickReady = true;
        }
        //dropTransformIndex gives possible to drop transfrom index. -1 for drop original position, -2 for skip dropping back.
        public void TwoHandDrop()
        {
            _intObj.transform.parent = _oldParentTransform;

            if (_intObj.hasRigid)
            {
                _intObj.rigid.isKinematic = false;
                if (_intObj.currentInteractor.playerRigidbody)
                    _intObj.rigid.linearVelocity = _intObj.currentInteractor.playerRigidbody.linearVelocity;
            }
            if (_col) _col.enabled = true;

            dropDone = true;
            Reset();
        }

        //Reset all so it can be pickable again.
        public void Reset()
        {
            _holdWeight = 0f;
            _elapsedTime = 0;
            _pickReady = false;
            twoHandPicked = false;
            pickable = false;
            if (_intObj)
                _intObj.ResetUseableEffectors();
        }
    }
}
