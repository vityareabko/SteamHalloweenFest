using UnityEngine;

namespace razz
{
    [CreateAssetMenu(fileName = "PickableOneSettings", menuName = "Interactor/PickableOneSettings")]
    public class PickableOne : InteractionTypeSettings
    {
        [Header("One Hand")]
        [Tooltip("Sets the holding type of this pick-up interaction. It can be changed at runtime before using the object. However, it cannot be changed while holding it in the inspector or accessing this variable. To change it while holding the object, call interactor.StartStopInteraction(intObj, newHoldType). \n\nBone Transform will hold the object at the current player animation hand position. \n\nHold Transform will hold the object at one of hold transforms on InteractorPoints. \n\nDont Hold will initiate dropping without holding when picked up.")]
        public HoldType holdingType;
        [Tooltip("Determines which hold point transform will be used from HoldPoints list on InteractorPoints.")]
        [Conditional(Condition.Show, nameof(ShowHoldTransformSettings))]
        public int holdPoint = 0;

        [Tooltip("Sets the dropping type of this pick - up interaction.It can be changed at runtime before dropping the object, even while holding.To initiate the drop and change simultaneously, call interactor.StartStopInteraction(intObj, dropType). \n\nDontDrop will make the object change its hold type between Hold Transform and Bone Transform when used. \n\nDropLocation will check the Pick Up/DropLocation transform on InteractorObject settings, and if it is valid, it will drop it there. \n\nGround will drop the object into air with given vectors. \n\nInventory will send the object to the inventoryPoint transform on current inventory or the onBody point on InteractorPoints/current inventory and disable it. \n\nThrow will send it to ThrowUp or ThrowDown points on InteractorPoints and wait for a second input to throw.")]
        public DropType dropType;
        [Tooltip("Parents to Drop Location transform when dropped.")]
        [Conditional(Condition.Show, nameof(ShowDropLocationsSettings))]
        public bool parentToLocation = true;
        [Tooltip("Enables the disabled collider and the rigidbody when dropped.")]
        [Conditional(Condition.Show, nameof(ShowDropLocationsSettings))]
        public bool enableComponents = true;
        [Tooltip("When Drop Location transform is not valid point (null or can't reach at the moment), it will drop to Ground instead.")]
        [Conditional(Condition.Show, nameof(ShowDropLocationsSettings))]
        public bool failToGround = true;
        [Tooltip("Drops into the air/ground with the given vector (X axis is positive for used hand side). The vector will determine the direction, and the hand will move to this offset from the position when called. Provide smaller values, as the hand will interpolate (lerp) this vector as well. Object velocity and direction will be affected by player's rigidbody too.")]
        [Conditional(Condition.Show, nameof(ShowGroundSettings))]
        public Vector3 groundVector = new Vector3(0.05f, 0.1f, 0.1f);
        [Tooltip("Drops the object into air/ground with this velocity multiplier.")]
        [Conditional(Condition.Show, nameof(ShowGroundSettings))]
        public float groundForce = 3f;
        [Tooltip("Drops the object into air/ground with this rotation vector.")]
        [Conditional(Condition.Show, nameof(ShowGroundSettings))]
        public Vector3 groundTorque = new Vector3(0f, 0f, 0f);
        [Conditional(Condition.Show, nameof(ShowInventorySettings))]
        [Tooltip("Drops the object selected onBody point tranform on InteractorPoints or Inventory. Set enable if you wish to see the object and move with selected transform.")]
        public bool onBody;
        [Conditional(Condition.Show, nameof(ShowBodyPointSettings))]
        [Tooltip("Determines which onBody point transform will be used from onBodyPoints list on InteractorPoints component or Inventory component.")]
        public int bodyPoint = 0;
        [Conditional(Condition.Show, nameof(ShowBodyPointSettings))]
        [Tooltip("Uses the invOnBodyPoints on current inventory instead of onBodyPoints on InteractorPoints. Use it to drop the object onto another object (requires inventory on the object).")]
        public bool invOnBodyPointInstead;
        [Conditional(Condition.Show, nameof(ShowBodyPointSettings))]
        [Tooltip("Normally, onBody option doesn't require Item component or Inventory. But if you wish to send item into inventory as well, select this and add Item component for this InteractorObject.")]
        public bool sendToInvToo;
        [Tooltip("The gameObject that have Item component and the one that have InventoryObject will be seperated when dropped onBody. This way when picked back up, The one with Item component will continue to stay onBody point until this pick up item will be dropped to ground or threw.")]
        [Conditional(Condition.Show, nameof(ShowBodyPointSettings))]
        public bool seperateItemObject;
        [Tooltip("Will use Throw Down Point instead of Throw Up Point on InteractorPoints to start throwing when enabled.")]
        [Conditional(Condition.Show, nameof(ShowThrowSettings))]
        public bool throwDown;
        [Tooltip("Throw with this given vector (X axis is positive for used hand side). The vector will determine the direction, and the hand will move to this offset from the position when called. Use smaller values, as the hand will interpolate (lerp) with this vector as well. Object velocity and direction will not be affected by the player's rigidbody.")]
        [Conditional(Condition.Show, nameof(ShowThrowSettings))]
        public Vector3 throwVector = new Vector3(0f, 0.2f, 0.4f);
        [Tooltip("Throws the object with this velocity multiplier.")]
        [Conditional(Condition.Show, nameof(ShowThrowSettings))]
        public float throwForce = 25f;
        [Tooltip("Throws the object with this rotation vector.")]
        [Conditional(Condition.Show, nameof(ShowThrowSettings))]
        public Vector3 throwTorque = new Vector3(0f, 0f, 0f);

        [Tooltip("Determines the easing of hand movement between hold positions or going into any drop point.")]
        public EaseType easeType = EaseType.QuadInOut;
        [Tooltip("Determines the speed of hand movement between hold positions or going into any drop point. Will multiply the speed settings on InteractorObject.")]
        [Range(0, 8f)] public float pickupSpeedMult = 1f;
        [Tooltip("When the object is out of reach to hand at picking up moment, it will be teleported to hand if the distance is bigger than this value. Otherwise, it will be interpolated (lerped) from that distance.")]
        [Range(0, 1f)] public float teleportLimit = 0.2f;
        [Tooltip("Experimental option! This functionality reverses the rotation direction when transitioning between hold points or moving to drop/throw positions. In standard Unity behavior, Quaternion slerp calculates the shortest path, but this option utilizes the longest path instead and changes the direction of the rotation.")]
        public bool reverseRotation = false;
        [Tooltip("Generates procedural bezier paths for hand movements (when changing hold points or transitioning to drop/throw positions) instead of directly lerping. It will create a curved path and avoid the body to achieve a natural movement. Calculation is based on shoulder frontal and lateral lines. If two points intersect with those lines, bezier curves will be generated (y and z axes for frontal line, x axis for lateral line). See the documentation for pictures with details.")]
        public bool proceduralBeziers = true;
        [Tooltip("Adjust the heights of the bezier curves for each axis (player direction).")]
        [Conditional(Condition.Show, nameof(proceduralBeziers))]
        public Vector3 bezierMultipliers = new Vector3(1f, 1f, 1f);
        [Tooltip("Adjust the lateral shoulder line length (the line between two shoulders responsible for generating curves along the X-axis of the player). The minimum point is the distance between the shoulder and the head (must be negative), while the maximum point is the distance at which you want your hand to avoid the player's shoulder area. You can enable debug to observe the lines and intersections while changing positions. Values will be added to the shoulder's X position to determine lines. You can enable debug to see the lines while changing hold positions.")]
        [Conditional(Condition.Show, nameof(proceduralBeziers))]
        public Vector2 shoulderXMinMax = new Vector2(-0.1f, 0.25f);
        [Tooltip("Adjust the frontal shoulder line height (the blue line starts from the player's forward and ends behind the player). The minimum point defines the area you want to use behind the shoulder, so the hand will move above the shoulder to reach this area. The maximum point is where your shoulders' top point is, so Bezier curves can move above this point to reach back. Values will be added to the shoulder's Y position to determine lines. You can enable debug to see the lines while changing hold positions.")]
        [Conditional(Condition.Show, nameof(proceduralBeziers))]
        public Vector2 shoulderYMinMax = new Vector2(-0.1f, 0.12f);
        [Tooltip("Adjust the frontal shoulder line front length (front tip of the blue line). It should be where your player's chest starts on Z axis of the player (not the height). You can enable debugging to see the lines while changing hold positions.")]
        [Conditional(Condition.Show, nameof(proceduralBeziers))]
        public float shoulderZMax = 0.15f;
        [Tooltip("Shows bezier paths and shoulder lines when object selected and used. Also debug logs for destinations.")]
        public bool debug;

        public enum HoldType
        {
            BoneTransform,
            HoldTransform,
            DontHold
        }
        public enum DropType
        {
            DontDrop,
            DropLocation,
            Ground,
            Inventory,
            Throw
        }

        public InteractorPoints currentIntPoints
        {
            get
            {
                if (!_currentIntPoints)  _currentIntPoints = _intObj.currentInteractor.interactorPoints;
                return _currentIntPoints;
            }
        }
        private int _effectorType;

        private Transform _intObjTransform;
        private Transform _currentPointTransform;
        private Transform _pickupStart;
        private Transform _shoulderTransform;
        private Transform _otherShoulderTransform;
        private Transform _playerTransform;
        private Transform _oldPivotTransform;

        public Transform pickupStart
        {
            get
            {
                if (!_pickupStart)
                {
                    GameObject pickupObj = new GameObject("PickupStart_" + (Interactor.FullBodyBipedEffector)_effectorType);
                    _pickupStart = pickupObj.transform;
                    _pickupStart.position = _intObjTransform.position;
                    _pickupStart.rotation = _intObjTransform.rotation;
                    _pickupStart.parent = currentIntPoints.transform;
                }
                return _pickupStart;
            }
        }
        private Transform _pickupDest; //Takes stuff into account like target/intObj pos and rot differences
        public Transform pickupDest
        {
            get
            {
                if (!_pickupDest)
                {
                    GameObject pickupObj = new GameObject("PickupDest_" + (Interactor.FullBodyBipedEffector)_effectorType);
                    _pickupDest = pickupObj.transform;
                    _pickupDest.position = Vector3.zero;
                    _pickupDest.rotation = Quaternion.identity;
                    _pickupDest.parent = currentIntPoints.transform;
                }
                return _pickupDest;
            }
        }

        private bool _oneHandPicked;
        public bool oneHandPicked { get { return _oneHandPicked; } set { _oneHandPicked = value; } }
        private bool _dropping;
        public bool dropping { get { return _dropping; } set { _dropping = value; } }
        private bool _progressDone;
        public bool progressDone { get { return _progressDone; } set { _progressDone = value; } }
        private bool _throwReady;
        public bool throwReady { get { return _throwReady; } set { _throwReady = value; } }
        private bool _orbitalDropping;
        public bool orbitalDropping { get { return _orbitalDropping; } set { _orbitalDropping = value; } }

        private InteractorIK _currentIntIK;
        private InteractorPoints _currentIntPoints;
        private Vector3 _intObjToChildPosDiff;
        private Quaternion _intObjToChildRotDiff;
        private float _durationTarget, _durationBack;
        private float _elapsedTime;
        private float _progress;
        private bool _bezierActive;
        private bool _sideBezier, _frontBezier;
        private float _aboveDist;
        private Vector3 _startTangent;
        private Vector3 _destTangent;
        private Vector3 _shoulderSLineStart;
        private Vector3 _shoulderSLineEnd;
        private Vector3 _shoulderFLineStart;
        private Vector3 _shoulderFLineEnd;
        private Vector3 _shoulderSideDir;
        private Vector3 _shoulderFrontDir;
        [HideInInspector] public float handDir = 1f;
        private bool _inInv = false;
        private bool _failedToGround;
        private EaseType _originalEaseType;
        private bool _bodyPointEnd;
        private float _progressSpeed = 1f;
        private bool _cachedPrevent;
        private bool _shorterPathRotation = true;
        private Vector3 _defaultHandForward;
        private Transform _intObjParent;

        private Vector3 _calcPosition;
        private Vector3 _playerPosDiff;
        private Quaternion _calcRotation;
        private Quaternion _playerRotDiff;

        public override void Init(InteractorObject interactorObject)
        {
            base.Init(interactorObject);

            _intObjTransform = _intObj.transform;
            _cachedPrevent = _intObj.preventExit;

            if (!onBody)
            {
                if (_intObj.enabledRenderers == null || _intObj.enabledRenderers.Length == 0)
                {
                    _intObj.enabledRenderers = _intObj.GetComponentsInChildren<Renderer>(false);
                }
            }
        }

        public override void UpdateSettings()
        {
            if (oneHandPicked)
            {
                UpdateDest();
                if (!progressDone) UpdateProgress();
                _intObjTransform.position = GetPosition(_progress);
                _intObjTransform.rotation = GetRotation(_progress);
            }
        }

        #region Public Methods
        public bool Pick(InteractorIK interactorIK, int effectorType)
        {
            if (oneHandPicked) return false;

            _currentIntIK = interactorIK;
            _effectorType = effectorType;
            _playerTransform = _intObj.currentInteractor.playerTransform;
            if (!CacheBones()) return false;

            PrepareObjectForGrab();

            ChangeDest(false);
            oneHandPicked = true;
            return true;
        }
        public bool Drop()
        {
            if (dropping) return false;

            bool executeDrop = dropType != DropType.DontDrop;
            if (executeDrop && dropType == DropType.Throw && !throwReady)
            {
                ChangeDest(true);
                return false;
            }

            if (!executeDrop) ChangeHold();
            else return ChangeDest(executeDrop);
            return executeDrop;
        }

        public void ChangeHold()
        {
            if (holdingType == HoldType.BoneTransform) holdingType = HoldType.HoldTransform;
            else if (holdingType == HoldType.HoldTransform) holdingType = HoldType.BoneTransform;
            ChangeDest(false);
        }
        public void ChangeHold(int point1, int point2)
        {
            holdingType = HoldType.HoldTransform;
            holdPoint = (holdPoint == point1) ? point2 : point1;
            ChangeDest(false);
        }
        public void ChangeHold(HoldType holdType)
        {
            holdingType = holdType;
            ChangeDest(false);
        }
        public void ChangeHold(HoldType holdType, int holdPoint)
        {
            holdingType = holdType;
            this.holdPoint = holdPoint;
            ChangeDest(false);
        }

        public void Reset()
        {
            if (!_intObj) return;

            PrepareObjectForRelease();
            oneHandPicked = false;
            dropping = false;
            _failedToGround = false;
            throwReady = false;
            orbitalDropping = false;
            currentIntPoints.TurnOffPath();
            easeType = _originalEaseType;
            _progressSpeed = 1f;
            if (_pickupStart) Destroy(_pickupStart.gameObject);
            if (_pickupDest) Destroy(_pickupDest.gameObject);
            _intObj.ResetUseableEffectors();
        }

        public bool CheckTransformForEffector(Interactor interactor, Transform transform, Interactor.FullBodyBipedEffector effectorType)
        {//Checks a transform position for effector if rules are ok to pick or drop to location
            if ((int)effectorType != 5 && (int)effectorType != 6)
            {
                Debug.LogWarning("Wrong effector type. Only hands.");
                return false;
            }
            return interactor.EffectorCheckOrbitalPosition(transform, (int)effectorType);
        }
        public bool IsHolding()
        {

            return false;
        }
        public bool IsMoving()
        {
            return !progressDone;
        }
        public bool IsDropping()
        {
            return dropping;
        }
        public bool IsInInv()
        {
            return _inInv;
        }
        #endregion

        private bool CacheBones()
        {
            if (_effectorType == 5) //Interactor.FullBodyBipedEffector
            {//LeftHand
                _shoulderTransform = _currentIntIK.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                _otherShoulderTransform = _currentIntIK.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                handDir = -1f;
                _defaultHandForward = _intObj.currentInteractor.leftHandAxis;
            }
            else if (_effectorType == 6)
            {//RightHand
                _shoulderTransform = _currentIntIK.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _otherShoulderTransform = _currentIntIK.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                handDir = 1f;
                _defaultHandForward = _intObj.currentInteractor.rightHandAxis;
            }
            else
            {
                Debug.LogWarning("Wrong effector type for Picking.", _intObj);
                return false;
            }
            if (!_shoulderTransform || !_otherShoulderTransform)
            {
                Debug.LogWarning("Shoulder bones could not be found.");
                return false;
            }
            return true;
        }
        private void UpdateDest()
        {
            _intObjToChildRotDiff = Quaternion.Inverse(_intObj.GetTargetForEffectorType(_effectorType).transform.rotation) * _intObjTransform.rotation;
            _intObjToChildPosDiff = _intObjTransform.position - _intObj.GetTargetForEffectorType(_effectorType).transform.position;

            if (holdingType == HoldType.BoneTransform && !dropping)
            {
                pickupDest.rotation = _currentIntIK.GetRotationBeforeIK((Interactor.FullBodyBipedEffector)_effectorType) * _intObjToChildRotDiff;
                pickupDest.position = _currentIntIK.GetPositionBeforeIK((Interactor.FullBodyBipedEffector)_effectorType) + _intObjToChildPosDiff;
            }
            else
            {
                if (!_currentPointTransform)
                {
                    holdingType = HoldType.BoneTransform;
                    Debug.Log("Change hold type with events or calls only.", _intObj);
                    return;
                }

                pickupDest.rotation = _currentPointTransform.rotation * _intObjToChildRotDiff;
                pickupDest.position = _currentPointTransform.position + _intObjToChildPosDiff;
            }
        }
        private bool ChangeDest(bool dropping)
        {
            _originalEaseType = easeType;
            if (!dropping)
            {
                if (holdingType == HoldType.HoldTransform)
                {
                    if (holdPoint < 0 || holdPoint >= currentIntPoints.holdPoints.Count || !currentIntPoints.holdPoints[holdPoint])
                    {
                        Debug.LogWarning("Selected Hold Point is not valid on the InteractorPoints component for this Pick Up gameobject!", _intObj);
                        holdingType = HoldType.BoneTransform;
                    }
                    else _currentPointTransform = currentIntPoints.holdPoints[holdPoint];
                }
                else if (holdingType == HoldType.DontHold) dropping = true;
                else
                {
                    if (debug) Debug.Log("Current Pick Up destination: " + _currentIntIK.GetBone((Interactor.FullBodyBipedEffector)_effectorType), _intObj);
                }
            }

            if (dropType != DropType.Throw)
            {
                throwReady = false;
                currentIntPoints.TurnOffPath();
            }
            if (dropping)
            {
                this.dropping = true;
                switch (dropType)
                {
                    case DropType.DontDrop: this.dropping = false;
                    break;
                    case DropType.DropLocation:
                        if (_intObj.dropLocation && CheckTransformForEffector(_intObj.dropLocation))
                        {
                            easeType = _intObj.easeType;
                            _currentPointTransform = _intObj.dropLocation;
                            if (debug) Debug.Log("Object dropped to valid DropLocation.", _intObj);
                        }
                        else if (failToGround)
                        {
                            CalcDropGroundPoint();
                            _failedToGround = true;
                            if (debug) Debug.Log("Object dropped to Ground(with failToGround option) because Drop Location wasn't valid.", _intObj);
                        }
                        else this.dropping = false;
                        break;
                    case DropType.Ground:
                        CalcDropGroundPoint();
                        break;
                    case DropType.Inventory:
                        {
                            if (onBody)
                            {
                                if (invOnBodyPointInstead)
                                {
                                    if (bodyPoint >= currentIntPoints.inventory.invOnBodyPoints.Count || currentIntPoints.inventory.invOnBodyPoints[bodyPoint] == null || !currentIntPoints.inventory.invOnBodyPoints[bodyPoint].bodyPoint)
                                    {
                                        Debug.LogWarning("Selected Body Point is not valid on the Inventory component for this Pick Up gameobject!", _intObj);
                                        onBody = false;
                                        //this.dropping = false;
                                    }
                                    else
                                    {
                                        if (currentIntPoints.inventory.invOnBodyPoints[bodyPoint].currentObject)
                                        {
                                            Debug.LogWarning("Current body point is already in use by " + currentIntPoints.inventory.invOnBodyPoints[bodyPoint].currentObject.name + " on index: " + bodyPoint, currentIntPoints.inventory);
                                            this.dropping = false;
                                        }
                                        else
                                        {
                                            currentIntPoints.inventory.invOnBodyPoints[bodyPoint].currentObject = _intObjTransform;
                                            _currentPointTransform = currentIntPoints.inventory.invOnBodyPoints[bodyPoint].bodyPoint;

                                            if (currentIntPoints.inventory.invOnBodyPoints[bodyPoint].bodyPointEnd)
                                                _bodyPointEnd = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (bodyPoint >= currentIntPoints.onBodyPoints.Count || currentIntPoints.onBodyPoints[bodyPoint] == null || !currentIntPoints.onBodyPoints[bodyPoint].bodyPoint)
                                    {
                                        Debug.LogWarning("Selected Body Point is not valid on the InteractorPoints component for this Pick Up gameobject!", _intObj);
                                        onBody = false;
                                        //this.dropping = false;
                                    }
                                    else
                                    {
                                        if (currentIntPoints.onBodyPoints[bodyPoint].currentObject)
                                        {
                                            Debug.LogWarning("Current body point is already in use by " + currentIntPoints.onBodyPoints[bodyPoint].currentObject.name + " on index: " + bodyPoint, currentIntPoints);
                                            this.dropping = false;
                                        }
                                        else
                                        {
                                            currentIntPoints.onBodyPoints[bodyPoint].currentObject = _intObjTransform;
                                            _currentPointTransform = currentIntPoints.onBodyPoints[bodyPoint].bodyPoint;

                                            if (currentIntPoints.onBodyPoints[bodyPoint].bodyPointEnd)
                                                _bodyPointEnd = true;
                                        }
                                    }
                                }
                            }

                            if (!onBody)
                            {
                                if (!currentIntPoints.inventory)
                                {
                                    Debug.LogWarning("Inventory is null on the InteractorPoints.", currentIntPoints);
                                    this.dropping = false;
                                }
                                else if (!currentIntPoints.inventory.inventoryPoint)
                                {
                                    Debug.LogWarning("Inventory Point is null on Inventory.", currentIntPoints.inventory);
                                    this.dropping = false;
                                }
                                else if (!_intObj.itemForInventory)
                                {
                                    Debug.LogWarning("Interactor Object has no ItemForInventory (Item.cs) attached on its settings. You need to add Item class and set its dbIndex from ItemDatabase in the scene.", _intObj);
                                    this.dropping = false;
                                }
                                else if (currentIntPoints.inventory.IsInventoryFull() < 0)
                                {
                                    Debug.LogWarning("Inventory is full.", currentIntPoints.inventory);
                                    this.dropping = false;
                                }
                                else
                                {
                                    _currentPointTransform = currentIntPoints.inventory.inventoryPoint;
                                }
                            }
                        }
                        break;
                    case DropType.Throw:
                        {
                            if (throwReady)
                            {
                                CalcThrowPoint();
                                currentIntPoints.TurnOffPath();
                                break;
                            }

                            if (throwDown)
                            {
                                if (currentIntPoints.throwDownPoint)
                                {
                                    _currentPointTransform = currentIntPoints.throwDownPoint;
                                    holdingType = HoldType.HoldTransform;
                                    this.dropping = false;
                                    throwReady = true;
                                    currentIntPoints.ShowPath(_intObjTransform, this, _intObj.rigid);
                                }
                                else
                                {
                                    CalcDropGroundPoint();
                                    _failedToGround = true;
                                    if (debug) Debug.Log("Object dropped to Ground because ThrowDownPoint on InveractorPoints wasn't valid.", _intObj);
                                }
                            }
                            else
                            {
                                if (currentIntPoints.throwUpPoint)
                                {
                                    _currentPointTransform = currentIntPoints.throwUpPoint;
                                    holdingType = HoldType.HoldTransform;
                                    this.dropping = false;
                                    throwReady = true;
                                    currentIntPoints.ShowPath(_intObjTransform, this, _intObj.rigid);
                                }
                                else
                                {
                                    CalcDropGroundPoint();
                                    _failedToGround = true;
                                    if (debug) Debug.Log("Object dropped to Ground because ThrowUpPoint on InveractorPoints wasn't valid.", _intObj);
                                }
                            }
                        }
                        break;
                }
            }

            ResetTimer();
            CheckStartToBoneRange();
            CheckBezier();
            //CheckCorrectRotationDir();
            return this.dropping;
        }
        private void CheckStartToBoneRange()
        {//If targets start pos is out of bone ik range, current bone pos will be already at the range.
            Vector3 diff = _currentIntIK.GetBone((Interactor.FullBodyBipedEffector)_effectorType).position - _intObj.GetTargetForEffectorType(_effectorType).transform.position;

            if (diff.sqrMagnitude > teleportLimit * teleportLimit) //Avoid sqr root operation
            {
                if (debug) Debug.Log("Pick up object teleported. Distance was: " + diff.magnitude);
                pickupStart.position += diff;
            }
        }
        private bool CheckTransformForEffector(Transform transform)
        {
            return _intObj.currentInteractor.EffectorCheckOrbitalPosition(transform, _effectorType);
        }
        private void UpdateProgress()
        {
            _elapsedTime += Time.fixedDeltaTime * _progressSpeed;
            if (easeType == EaseType.CustomCurve) easeType = EaseType.QuadInOut;

            if (!dropping)
            {
                _progress = Mathf.Clamp01(Ease.FromType(easeType)(_elapsedTime / _durationTarget, _intObj.speedCurve));
            }
            else _progress = Mathf.Clamp01(Ease.FromType(easeType)(_elapsedTime / _durationBack));

            if (_progress > 0.99 && _bodyPointEnd)
            {
                progressDone = false;
                _elapsedTime = 0;
                pickupStart.rotation = _intObjTransform.rotation;
                pickupStart.position = _intObjTransform.position;
                if (invOnBodyPointInstead)
                {
                    _currentPointTransform = currentIntPoints.inventory.invOnBodyPoints[bodyPoint].bodyPointEnd;
                    _progressSpeed = currentIntPoints.inventory.invOnBodyPoints[bodyPoint].endSpeedMult;
                }
                else
                {
                    _currentPointTransform = currentIntPoints.onBodyPoints[bodyPoint].bodyPointEnd;
                    _progressSpeed = currentIntPoints.onBodyPoints[bodyPoint].endSpeedMult;
                }
                
                _bodyPointEnd = false;
                _bezierActive = false;
                return;
            }

            if (_progress > 0.99)
            {
                _progress = 1f;
                progressDone = true;
            }
        }
        private void ResetTimer()
        {
            progressDone = false;
            _elapsedTime = 0;
            _progressSpeed = 1f;
            pickupStart.rotation = _intObjTransform.rotation;
            pickupStart.position = _intObjTransform.position;
            _durationTarget = _intObj.targetDuration / pickupSpeedMult;
            _durationBack = _intObj.backDuration / pickupSpeedMult;

            if (debug)
            {
                if (holdingType != HoldType.BoneTransform || dropping)
                    Debug.Log("Current Pick Up destination: " + _currentPointTransform, _currentPointTransform);
            }
        }

        private Vector3 GetPosition(float progress)
        {
            if (dropType == DropType.Ground && dropping || _failedToGround)
                return GetLerpedPosition(progress);

            if (dropType == DropType.DropLocation && dropping && _intObj.orbitalReach)
                return GetLerpedPosition(progress);

            if (proceduralBeziers && _bezierActive && !progressDone)
            {
                return GetBezierPosition(progress);
            }
            else return GetLerpedPosition(progress);
        }
        private Vector3 GetLerpedPosition(float progress)
        {
            return Vector3.Lerp(pickupStart.position, pickupDest.position, progress);
        }
        private Quaternion GetRotation(float progress)
        {
            if (reverseRotation)
            {
                _shorterPathRotation = false;
                return SlerpDirChange(pickupStart.rotation, pickupDest.rotation, progress);
            }
            else return Quaternion.Slerp(pickupStart.rotation, pickupDest.rotation, progress);
        }
        public Quaternion SlerpDirChange(Quaternion start, Quaternion end, float pos)
        {
            var dot = Quaternion.Dot(start, end);
            if ((_shorterPathRotation && dot < 0) || (!_shorterPathRotation && dot > 0))
            {
                start = ScalarMultiply(start, -1f);
                dot *= -1f;
            }

            dot = Mathf.Clamp(dot, -1f, 1f);
            var theta0 = Mathf.Acos(dot);
            var theta = theta0 * pos;

            var s1 = Mathf.Sin(theta) / Mathf.Sin(theta0);
            var s0 = Mathf.Cos(theta) - dot * s1;

            return Quaternion.Normalize(Add(ScalarMultiply(start, s0), ScalarMultiply(end, s1)));
        }
        public Quaternion ScalarMultiply(Quaternion q, float scalar)
        {
            q.x *= scalar;
            q.y *= scalar;
            q.z *= scalar;
            q.w *= scalar;
            return q;
        }
        public Quaternion Add(Quaternion one, Quaternion two)
        {
            one.x += two.x;
            one.y += two.y;
            one.z += two.z;
            one.w += two.w;
            return one;
        }

        private void CheckBezier()
        {
            if (!proceduralBeziers)
            {
                _bezierActive = false;
                return;
            }

            UpdateDest();
            UpdateShoulderLine();
            CheckLineIntersection();
        }
        private Vector3 GetBezierPosition(float _progress)
        {
            if (!_currentIntIK) return Vector3.zero;

            _playerRotDiff = _playerTransform.rotation * Quaternion.Inverse(_calcRotation);

            float u = 1 - _progress;
            float tt = _progress * _progress;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * _progress;

            Vector3 p = uuu * (pickupStart.position);
            p += 3 * uu * _progress * ((_playerRotDiff * _startTangent) + pickupStart.position);
            p += 3 * u * tt * ((_playerRotDiff * _destTangent) + pickupDest.position);
            p += ttt * (pickupDest.position);

            return p;
        }
        private void UpdateShoulderLine()
        {
            _shoulderSLineStart = _shoulderTransform.position;
            _shoulderSLineStart.y += shoulderYMinMax.x;
            _shoulderSLineEnd = _otherShoulderTransform.position;
            _shoulderSLineEnd.y += shoulderYMinMax.x;
            _shoulderSideDir = (_shoulderSLineEnd - _shoulderSLineStart).normalized;
            _shoulderSLineEnd = _shoulderSLineStart + _shoulderSideDir * _intObj.currentInteractor.armLength;
            _shoulderSLineStart = _shoulderSLineEnd - (_shoulderSideDir * (_intObj.currentInteractor.armLength + shoulderXMinMax.y));

            _shoulderFLineStart = _shoulderTransform.position;
            _shoulderFLineStart.y += shoulderYMinMax.x;
            _shoulderFLineEnd = (_shoulderFLineStart + _playerTransform.rotation * new Vector3(0f, 0f, -_intObj.currentInteractor.armLength));
            _shoulderFrontDir = (_shoulderFLineEnd - _shoulderFLineStart).normalized;
            _shoulderFLineStart = _shoulderFLineStart + _playerTransform.rotation * new Vector3(0f, 0f, shoulderZMax);
        }
        private void CheckLineIntersection()
        {//Check the pickupStart-pickupDest line agaisnt the shoulder-to-shoulder line if it intersects
            _sideBezier = false;
            _frontBezier = false;
            Vector3 localPickupStart = _playerTransform.InverseTransformPoint(pickupStart.position);
            Vector3 localPickupDest = _playerTransform.InverseTransformPoint(pickupDest.position);

            Vector3 localShoulderSLineStart = _playerTransform.InverseTransformPoint(_shoulderSLineStart);
            Vector3 localShoulderSLineEnd = _playerTransform.InverseTransformPoint(_shoulderSLineEnd);

            _sideBezier = CalcIntersection(new Vector2(localPickupStart.x, localPickupStart.z), new Vector2(localPickupDest.x, localPickupDest.z), new Vector2(localShoulderSLineStart.x, localShoulderSLineStart.z), new Vector2(localShoulderSLineEnd.x, localShoulderSLineEnd.z));

            Vector3 localShoulderFLineStart = _playerTransform.InverseTransformPoint(_shoulderFLineStart);
            Vector3 localShoulderFLineEnd = _playerTransform.InverseTransformPoint(_shoulderFLineEnd);

            _frontBezier = CalcIntersection(new Vector2(localPickupStart.y, localPickupStart.z), new Vector2(localPickupDest.y, localPickupDest.z), new Vector2(localShoulderFLineStart.y, localShoulderFLineStart.z), new Vector2(localShoulderFLineEnd.y, localShoulderFLineEnd.z));
            if (!_frontBezier)
            {//Check upper frontal line if bottom fails, but only if lower point is lower than bottom frontal line
                float upperFrontalHeight = localShoulderFLineStart.y - shoulderYMinMax.x + shoulderYMinMax.y;
                bool aboveUpperBelowBottom = localPickupStart.y > upperFrontalHeight && localPickupDest.y < localShoulderFLineStart.y;
                if (!aboveUpperBelowBottom) aboveUpperBelowBottom = localPickupDest.y > upperFrontalHeight && localPickupStart.y < localShoulderFLineStart.y;

                if (aboveUpperBelowBottom)
                {
                    _frontBezier = CalcIntersection(new Vector2(localPickupStart.y, localPickupStart.z), new Vector2(localPickupDest.y, localPickupDest.z), new Vector2(upperFrontalHeight, localShoulderFLineStart.z), new Vector2(upperFrontalHeight, localShoulderFLineEnd.z));
                }
            }

            if (debug)
            {
                Color lineColor = Color.red;
                float time = 1f;
                if (_sideBezier)
                {
                    Debug.Log("Pickup start and pickup end points intersected with side shoulder line. Procedural side bezier enabled.", _intObj);
                    time = 4f;
                }

                Vector3 pickupLine1 = _playerTransform.position + _playerTransform.rotation * new Vector3(localPickupStart.x, localShoulderSLineStart.y, localPickupStart.z);
                Vector3 pickupLine2 = _playerTransform.position + _playerTransform.rotation * new Vector3(localPickupDest.x, localShoulderSLineStart.y, localPickupDest.z);
                Debug.DrawLine(pickupLine1, pickupLine2, lineColor, time);
                Debug.DrawLine(_shoulderSLineStart, _shoulderSLineEnd, Color.red, time);
                Vector3 upperLine = new Vector3(_shoulderSLineStart.x, _shoulderTransform.position.y + shoulderYMinMax.y, _shoulderSLineStart.z) + _shoulderSideDir * shoulderXMinMax.y;
                Vector3 upperLineEnd = new Vector3(_shoulderSLineEnd.x, _shoulderTransform.position.y + shoulderYMinMax.y, _shoulderSLineEnd.z) - _shoulderSideDir * (_intObj.currentInteractor.armLength + shoulderXMinMax.x);
                Debug.DrawLine(upperLine, upperLineEnd, Color.red, time);

                lineColor = Color.blue;
                time = 1f;
                if (_frontBezier)
                {
                    Debug.Log("Pickup start and pickup end points intersected with front shoulder line. Procedural front bezier enabled.", _intObj);
                    time = 4f;
                }

                pickupLine1 = _playerTransform.position + _playerTransform.rotation * new Vector3(localShoulderFLineStart.x, localPickupStart.y, localPickupStart.z);
                pickupLine2 = _playerTransform.position + _playerTransform.rotation * new Vector3(localShoulderFLineStart.x, localPickupDest.y, localPickupDest.z);
                Debug.DrawLine(pickupLine1, pickupLine2, lineColor, time);
                Debug.DrawLine(_shoulderFLineStart, _shoulderFLineEnd, Color.blue, time);
                upperLine = new Vector3(_shoulderFLineStart.x, _shoulderTransform.position.y + shoulderYMinMax.y, _shoulderFLineStart.z);
                upperLineEnd = new Vector3(_shoulderFLineEnd.x, _shoulderTransform.position.y + shoulderYMinMax.y, _shoulderFLineEnd.z);
                Debug.DrawLine(upperLine, upperLineEnd, Color.blue, time);
            }

            if (_sideBezier || _frontBezier) _bezierActive = true;
            else _bezierActive = false;

            if (_bezierActive)
            {
                CalcTangents(localPickupStart, localPickupDest, localShoulderSLineStart, localShoulderFLineStart);
            }
        }
        private bool CalcIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            Vector2 a = p2 - p1;
            Vector2 b = p3 - p4;
            Vector2 c = p1 - p3;

            float alphaNumerator = b.y * c.x - b.x * c.y;
            float alphaDenominator = a.y * b.x - a.x * b.y;
            float betaNumerator = a.x * c.y - a.y * c.x;
            float betaDenominator = a.y * b.x - a.x * b.y;

            bool intersected = true;
            if (alphaDenominator == 0 || betaDenominator == 0) intersected = false;
            else
            {
                if (alphaDenominator > 0)
                {
                    if (alphaNumerator < 0 || alphaNumerator > alphaDenominator) intersected = false;
                }
                else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator) intersected = false;

                if (intersected && betaDenominator > 0)
                {
                    if (betaNumerator < 0 || betaNumerator > betaDenominator) intersected = false;
                }
                else if (betaNumerator > 0 || betaNumerator < betaDenominator)
                    intersected = false;
            }
            return intersected;
        }
        private void CalcTangents(Vector3 localPickupStart, Vector3 localPickupDest, Vector3 localShoulderSLineStart, Vector3 localShoulderFLineStart)
        {
            UpdateShoulderLine();
            _startTangent = Vector3.zero;
            _destTangent = Vector3.zero;
            _calcRotation = _playerTransform.rotation;
            _playerRotDiff = Quaternion.identity;

            Vector3 localActualShoulder = _playerTransform.InverseTransformPoint(_shoulderTransform.position);

            float armLength = _intObj.currentInteractor.armLength;
            Vector3 armLengthV3 = bezierMultipliers * armLength;
            float shoulderXMin = localActualShoulder.x + (shoulderXMinMax.x * handDir);
            float shoulderYMax = localActualShoulder.y + shoulderYMinMax.y;

            float upperXMin, bottomXMin, upperXMax, bottomXMax, upperYMax, bottomYMax, upperZ, upperZMax, bottomZMax;
            bool reverse = false;
            bool behindUpDown = false; //skips horizontal shoulderLine intersection, but moves behind up to behind down, so sideway beziers

            if (localPickupStart.y > localPickupDest.y)
            {//upper is pickupStart, bottom is pickupDest
                upperXMin = localPickupStart.x - shoulderXMin;
                bottomXMin = localPickupDest.x - shoulderXMin;
                upperXMax = localPickupStart.x - localShoulderSLineStart.x;
                bottomXMax = localPickupDest.x - localShoulderSLineStart.x;
                upperYMax = localPickupStart.y - shoulderYMax;
                bottomYMax = localPickupDest.y - shoulderYMax;
                upperZ = localPickupStart.z - localActualShoulder.z;
                upperZMax = localPickupStart.z - localShoulderFLineStart.z;
                bottomZMax = localPickupDest.z - localShoulderFLineStart.z;
            }
            else
            {
                reverse = true; //upper is pickupDest, bottom is pickupStart
                upperXMin = localPickupDest.x - shoulderXMin;
                bottomXMin = localPickupStart.x - shoulderXMin;
                upperXMax = localPickupDest.x - localShoulderSLineStart.x;
                bottomXMax = localPickupStart.x - localShoulderSLineStart.x;
                upperYMax = localPickupDest.y - shoulderYMax;
                bottomYMax = localPickupStart.y - shoulderYMax;
                upperZ = localPickupDest.z - localActualShoulder.z;
                upperZMax = localPickupDest.z - localShoulderFLineStart.z;
                bottomZMax = localPickupStart.z - localShoulderFLineStart.z;
            }

            float upperRight = 0f;
            float upperUp = 0f;
            float upperForward = 0f;
            float bottomRight = 0f;
            float bottomUp = 0f;
            float bottomForward = 0f;

            if (_frontBezier)
            {//upperY higher than bottomY
                if (upperYMax > 0f)
                {//upperY higher than shoulderYMax
                    if (upperZ > 0f)
                    {//upperZ higher than shoulderZ
                        if (bottomZMax > 0f)
                        {//bottomZ higher than shoulderZMax u3b4
                            upperUp = 0f;
                            bottomUp = 0f;
                            if (upperZMax > 0f) upperForward = 0f;
                            else upperForward = Mathf.InverseLerp(armLength, 0f, upperZ) * armLengthV3.z;
                            bottomForward = 0f;
                        }
                        else
                        {//bottomZ lower than shoulderZMax u3b1
                            upperUp = 0f;
                            bottomUp = -Mathf.InverseLerp(armLength * 0.5f, 0f, -bottomYMax) * armLengthV3.y;
                            upperForward = Mathf.InverseLerp(armLength, 0f, upperZ) * armLengthV3.z;
                            bottomForward = Mathf.InverseLerp(0f, armLength * 2f, -bottomZMax) * armLengthV3.z;
                        }
                    }
                    else
                    {//upperZ lower than shoulderZ
                        if (bottomZMax > 0f)
                        {//bottomZ higher than shoulderZMax u2b4
                            upperUp = Mathf.InverseLerp(armLength * 0.5f, 0f, upperYMax) * armLengthV3.y;
                            bottomUp = Mathf.InverseLerp(0f, armLength, -bottomYMax) * armLengthV3.y;
                            upperForward = Mathf.InverseLerp(0f, armLength * 2f, -upperZ) * armLengthV3.z;
                            bottomForward = Mathf.InverseLerp(armLength * 0.5f, 0f, bottomZMax) * armLengthV3.z;
                        }
                        else
                        {//bottomZ lower than shoulderZMax u2b1
                            upperUp = Mathf.InverseLerp(armLength * 0.5f, 0f, upperYMax) * armLengthV3.y;
                            bottomUp = 0f;
                            upperForward = Mathf.InverseLerp(0f, armLength, -upperZ) * armLengthV3.z;
                            bottomForward = Mathf.InverseLerp(0f, armLength * 0.25f, -bottomZMax) * armLengthV3.z * 1.5f;
                            behindUpDown = true;
                        }
                    }
                }
                else
                {//upperY lower than shoulderYMax
                    if (upperZ > 0f)
                    {//upperZ higher than shoulderZ
                        if (bottomZMax > 0f)
                        {//bottomZ higher than shoulderZMax u4b4
                            upperUp = 0f;
                            bottomUp = 0f;
                            if (upperZMax > 0f) upperForward = 0f;
                            else upperForward = Mathf.InverseLerp(armLength, 0f, upperZ) * armLengthV3.z;
                            bottomForward = 0f;
                        }
                        else
                        {//bottomZ lower than shoulderZMax u4b1
                            upperUp = 0f;
                            bottomUp = -Mathf.InverseLerp(armLength * 0.5f, 0f, -bottomYMax) * armLengthV3.y;
                            upperForward = Mathf.InverseLerp(armLength, 0f, upperZ) * armLengthV3.z;
                            bottomForward = Mathf.InverseLerp(0f, armLength * 2f, -bottomZMax) * armLengthV3.z;
                        }
                    }
                    else
                    {//upperZ lower than shoulderZ
                        if (bottomZMax > 0f)
                        {//bottomZ higher than shoulderZMax u1b4
                            upperUp = Mathf.InverseLerp(0f, armLength * 0.25f, -upperYMax) * armLengthV3.y * 1.5f;
                            bottomUp = Mathf.InverseLerp(0f, armLength, -bottomYMax) * armLengthV3.y;
                            upperForward = Mathf.InverseLerp(0f, armLength * 2f, -upperZ) * armLengthV3.z * 1.5f;
                            bottomForward = Mathf.InverseLerp(armLength * 0.5f, 0f, bottomZMax) * armLengthV3.z;
                        }
                        else
                        {//bottomZ lower than shoulderZMax u1b1
                            upperUp = Mathf.InverseLerp(0f, armLength * 0.25f, -upperYMax) * armLengthV3.y * 1.8f;
                            bottomUp = 0f;
                            upperForward = Mathf.InverseLerp(0f, armLength, -upperZ) * armLengthV3.z * 1.5f;
                            bottomForward = Mathf.InverseLerp(0f, armLength * 0.25f, -bottomZMax) * armLengthV3.z * 1.5f;
                            behindUpDown = true;
                        }
                    }
                }
            }

            if (_sideBezier || behindUpDown)
            {
                float upperX = upperXMax;
                float bottomX = bottomXMax;
                if (upperYMax > 0f) upperX = upperXMin * 2f;
                if (bottomYMax > 0f) bottomX = bottomXMin * 2f;

                if (handDir > 0f)
                {//right side
                    if (upperX > 0f) upperRight = 0f;
                    else
                    {
                        upperRight = Mathf.InverseLerp(0f, armLength, -upperX) * armLengthV3.x;
                    }

                    if (bottomX > 0f) bottomRight = 0f;
                    else
                    {
                        bottomRight = Mathf.InverseLerp(0f, armLength, -bottomX) * armLengthV3.x;
                    }
                }
                else
                {//left side
                    if (upperX < 0f) upperRight = 0f;
                    else
                    {
                        upperRight = -Mathf.InverseLerp(0f, armLength, upperX) * armLengthV3.x;
                    }

                    if (bottomX < 0f) bottomRight = 0f;
                    else
                    {
                        bottomRight = -Mathf.InverseLerp(0f, armLength, bottomX) * armLengthV3.x;
                    }
                }
            }

            if (!reverse)
            {
                _startTangent += _playerTransform.right * upperRight + _playerTransform.up * upperUp + _playerTransform.forward * upperForward;
                _destTangent += _playerTransform.right * bottomRight + _playerTransform.up * bottomUp + _playerTransform.forward * bottomForward;
            }
            else
            {
                _startTangent += _playerTransform.right * bottomRight + _playerTransform.up * bottomUp + _playerTransform.forward * bottomForward;
                _destTangent += _playerTransform.right * upperRight + _playerTransform.up * upperUp + _playerTransform.forward * upperForward;
            }
        }

        private void CalcDropGroundPoint()
        {
            _currentPointTransform = currentIntPoints.dropGroundPoint;

            Vector3 sidedGroundVector = new Vector3(groundVector.x * handDir, groundVector.y, groundVector.z);
            sidedGroundVector = _playerTransform.right * sidedGroundVector.x + _playerTransform.up * sidedGroundVector.y + _playerTransform.forward * sidedGroundVector.z;
            sidedGroundVector = _currentIntIK.GetBone((Interactor.FullBodyBipedEffector)_effectorType).position + sidedGroundVector;

            _currentPointTransform.position = sidedGroundVector;
            _currentPointTransform.rotation = _currentIntIK.GetBone((Interactor.FullBodyBipedEffector)_effectorType).rotation;
        }
        private void CalcThrowPoint()
        {
            easeType = EaseType.QuadIn;
            _currentPointTransform = currentIntPoints.dropGroundPoint;

            Vector3 sidedThrowVector = new Vector3(throwVector.x * handDir, throwVector.y, throwVector.z);
            sidedThrowVector = _playerTransform.right * sidedThrowVector.x + _playerTransform.up * sidedThrowVector.y + _playerTransform.forward * sidedThrowVector.z;
            sidedThrowVector = _currentIntIK.GetBone((Interactor.FullBodyBipedEffector)_effectorType).position + sidedThrowVector;

            _currentPointTransform.position = sidedThrowVector;
            _currentPointTransform.rotation = _currentIntIK.GetBone((Interactor.FullBodyBipedEffector)_effectorType).rotation;
        }

        private void PrepareObjectForGrab()
        {
            DisableComponents();
            if (IsInInv()) GetFromInv();
        }
        private void PrepareObjectForRelease()
        {
            if (dropping)
            {
                switch (dropType)
                {
                    case DropType.DontDrop:
                        dropping = false;
                        break;
                    case DropType.DropLocation:
                        if (enableComponents) EnableComponents(Vector3.zero, Vector3.zero, false);
                        _intObj.preventExit = _cachedPrevent;
                        if (_intObj.dropLocation && parentToLocation)
                            _intObjTransform.SetParent(_intObj.dropLocation);
                        RemoveFromInv();
                        break;
                    case DropType.Ground:
                        EnableComponents((pickupDest.position - pickupStart.position).normalized * groundForce, groundTorque, true);
                        if (seperateItemObject) _intObj.itemForInventory.gameObject.SetActive(false);
                        RemoveFromInv();
                        break;
                    case DropType.Inventory:
                        SendToInv();
                        break;
                    case DropType.Throw:
                        if (_failedToGround)
                        {
                            EnableComponents((pickupDest.position - pickupStart.position).normalized * groundForce, groundTorque, true);
                        }
                        else EnableComponents((pickupDest.position - pickupStart.position).normalized * throwForce, throwTorque, false);
                        if (seperateItemObject) _intObj.itemForInventory.gameObject.SetActive(false);
                        RemoveFromInv();
                        break;
                }
            }
            else EnableComponents(Vector3.zero, Vector3.zero, false);
        }
        private void DisableComponents()
        {
            if (_intObj.hasRigid)
            {
                _intObj.rigid.linearVelocity = Vector3.zero;
                _intObj.rigid.isKinematic = true;
            }

            if (_intObj.col) _intObj.col.enabled = false;
            _intObjParent = _intObjTransform.parent;
            _intObjTransform.SetParent(null);
            _intObj.preventExit = true;
        }
        private void EnableComponents(Vector3 velocity, Vector3 torque, bool playerVel)
        {
            if (debug) Debug.DrawLine(pickupStart.position, pickupDest.position, Color.blue, 5f);
            if (_intObj.hasRigid)
            {
                _intObj.rigid.isKinematic = false;
                Vector3 playerVelocity = Vector3.zero;
                if (_intObj.currentInteractor.playerRigidbody && playerVel)
                    playerVelocity += _intObj.currentInteractor.playerRigidbody.linearVelocity;

                _intObj.rigid.linearVelocity = playerVelocity + velocity;

                if (torque != Vector3.zero)
                {
                    _intObj.rigid.AddTorque(torque, ForceMode.Impulse);
                }
            }

            if (_intObj.col) _intObj.col.enabled = true;
            _intObjTransform.SetParent(_intObjParent);
            _intObj.preventExit = _cachedPrevent;
        }
        private void SendToInv()
        {
            DisableComponents();
            if (_intObj.pivot) _oldPivotTransform = _intObj.pivot;

            if (!onBody)
            {
                if (holdingType == HoldType.DontHold) holdingType = HoldType.BoneTransform;

                _currentIntPoints.inventory.AddItemToInventory(_intObj.itemForInventory);
                TurnOffRenderers();
                _intObj.gameObject.SetActive(false);
            }
            else
            {
                if (seperateItemObject)
                    _intObj.itemForInventory.transform.parent = _currentPointTransform;
                if (sendToInvToo) _currentIntPoints.inventory.AddItemToInventory(_intObj.itemForInventory);
            }

            _intObj.pivot = null;
            _intObj.used = false;
            _intObj.ResetUseableEffectors();
            _intObjTransform.SetParent(_currentPointTransform);
            _inInv = true;
        }
        private void GetFromInv()
        {
            _intObjTransform.SetParent(null);
            if (_oldPivotTransform) _intObj.pivot = _oldPivotTransform;

            if (!onBody)
            {
                TurnOnRenderers();
            }
            else
            {
                if (invOnBodyPointInstead)
                {
                    if (bodyPoint < currentIntPoints.inventory.invOnBodyPoints.Count && currentIntPoints.inventory.invOnBodyPoints[bodyPoint] != null && currentIntPoints.inventory.invOnBodyPoints[bodyPoint].currentObject == _intObjTransform)
                        currentIntPoints.inventory.invOnBodyPoints[bodyPoint].currentObject = null;
                }
                else
                {
                    if (bodyPoint < currentIntPoints.onBodyPoints.Count && currentIntPoints.onBodyPoints[bodyPoint] != null && currentIntPoints.onBodyPoints[bodyPoint].currentObject == _intObjTransform)
                        currentIntPoints.onBodyPoints[bodyPoint].currentObject = null;
                }
            }
            _inInv = false;
        }
        private void RemoveFromInv()
        {
            if (_intObj.itemForInventory && _intObj.itemForInventory.currentInventory)
            {
                _intObj.itemForInventory.currentInventory.RemoveItemFromInventory(_intObj.itemForInventory);
                _inInv = false;
            }
        }
        private void TurnOnRenderers()
        {
            for (int i = 0; i < _intObj.enabledRenderers.Length; i++)
                _intObj.enabledRenderers[i].enabled = true;
        }
        private void TurnOffRenderers()
        {//To hide obj after interaction start between hand goes to obj
            for (int i = 0; i < _intObj.enabledRenderers.Length; i++)
                _intObj.enabledRenderers[i].enabled = false;
        }

        #region Gizmos and Inspector
        public void DrawBezierPathGizmos()
        {
            if (!oneHandPicked || progressDone) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(pickupStart.position, 0.02f);
            Gizmos.DrawSphere(pickupDest.position, 0.02f);

            if (_bezierActive)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(pickupStart.position, ((_playerRotDiff * _startTangent) + pickupStart.position));
                Gizmos.DrawLine(pickupDest.position, ((_playerRotDiff * _destTangent) + pickupDest.position));
            }

            int resolution = 10;
            Vector3 prevPoint = GetPosition(0);
            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 bezierPosition = GetPosition(t);
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(bezierPosition, 0.01f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(prevPoint, bezierPosition);

                prevPoint = bezierPosition;
            }
        }

        private bool ShowHoldTransformSettings()
        {
            if (holdingType == HoldType.HoldTransform) return true;
            else return false;
        }
        private bool ShowDropLocationsSettings()
        {
            if (dropType == DropType.DropLocation) return true;
            else return false;
        }
        private bool ShowGroundSettings()
        {
            if (dropType == DropType.Ground || (dropType == DropType.DropLocation && failToGround)) return true;
            else return false;
        }
        private bool ShowInventorySettings()
        {
            if (dropType == DropType.Inventory) return true;
            else return false;
        }
        private bool ShowBodyPointSettings()
        {
            if (dropType == DropType.Inventory && onBody) return true;
            else return false;
        }
        private bool ShowThrowSettings()
        {
            if (dropType == DropType.Throw) return true;
            else return false;
        }
        #endregion
    }
}
