﻿using UnityEngine;
using System.Collections.Generic;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#interactorcs")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public class Interactor : MonoBehaviour
    {
        public const float Version = 0.98f;
        #region Interactor Variables
        public class IntObjComponents
        {
            public InteractorObject interactorObject;
            public float distanceSqr;
        }

        public class EffectorStatus
        {
            public bool connected;
            public InteractorObject connectedTo;
            public InteractorTarget connectedTarget;
        }

        public EffectorStatus[] effectors;
        public bool anyConnected;
        public List<EffectorLink> effectorLinks = new List<EffectorLink>();

        public enum FullBodyBipedEffector
        {
            Body,
            LeftShoulder,
            RightShoulder,
            LeftThigh,
            RightThigh,
            LeftHand,
            RightHand,
            LeftFoot,
            RightFoot
        }

        //This is the list of all interaction objects in sphere area. Its public because UI needs these too.
        [HideInInspector] public List<IntObjComponents> intObjComponents = new List<IntObjComponents>();
        [HideInInspector] public SphereCollider sphereCol;
        [HideInInspector] public Vector3 sphereColWithRotScale;
        [HideInInspector] public GameObject selfInteractionObject;
        [HideInInspector] public bool selfInteractionEnabled = false;
        [HideInInspector] public int selectedByUI = 0;
        [HideInInspector] public bool checkOncePerObject;
        [HideInInspector] public string layerName = "Player";
        [HideInInspector] public Rigidbody playerRigidbody;
        [HideInInspector] public Collider playerCollider;
        [HideInInspector] public Transform playerTransform;
        //The container class that holds interaction states for this player
        [HideInInspector] public InteractionStates interactionStates;

        private InteractorTarget _selfActiveTarget;
        private InteractorIK _interactorIK;
        public InteractorIK interactorIK
        {
            get { return _interactorIK; }
            set { if (!_interactorIK) _interactorIK = value; }
        }

        private InteractorAi _interactorAi;
        public InteractorAi interactorAi
        {
            get { return _interactorAi; }
            set { if (!_interactorAi) _interactorAi = value; }
        }
        private AnimAssist _animAssist;
        public AnimAssist animAssist
        {
            get { return _animAssist; }
            set { if (!_animAssist) _animAssist = value; }
        }
        private OrbitalReach _orbitalReach;
        public OrbitalReach orbitalReach
        {
            get { return _orbitalReach; }
            set { if (!_orbitalReach) _orbitalReach = value; }
        }
        private InteractorPoints _interactorPoints;
        public InteractorPoints interactorPoints
        {
            get 
            {
                if (!_interactorPoints) _interactorPoints = GetComponentInChildren<InteractorPoints>();
                if (!_interactorPoints)
                {
                    GameObject intPointObj = Instantiate(new GameObject("InteractorPoints"), transform.position, transform.rotation, transform);
                    _interactorPoints = intPointObj.AddComponent<InteractorPoints>();
                }
                return _interactorPoints;
            }
        }

        private Vector3 _playerCenter;
        private bool _disconnectOnce;
        private bool _connectOnce;
        private Transform _leftElbow;
        private Transform _rightElbow;
        private Vector3 _leftHandAxis;
        public Vector3 leftHandAxis { get { return _leftHandAxis; } }
        private Vector3 _rightHandAxis;
        public Vector3 rightHandAxis { get { return _rightHandAxis; } }
        public float armLength;

        //These are for raycast calculations
        private RaycastHit _lookHit;
        private Ray _mousePosRay;
        private Camera _mainCam;
        private GameObject _activeDistanceIntObj;
        private int _layerMask;

        //For ProtoTruck example or Parts to animate
        [HideInInspector] public VehicleBasicInput vehicleInput;
        [HideInInspector] public VehiclePartControls vehiclePartCont;
        [HideInInspector] public TurretAim[] childTurrets;
        [HideInInspector] public bool vehiclePartsActive;

        //LookAtTarget variables, rest is on LookAtTarget.cs
        public bool lookAtTargetEnabled = true;
        public float waitForNewTarget = 0;
        public float lookEndTimer = 0;
        public bool lookInitiateFailed;
        public Transform alternateHead;
        private LookAtTarget _lookAtTarget;

        //Exposed properties
        [SerializeField] public float raycastDistance = 20f;

#if UNITY_EDITOR
        private static Interactor _instance;
        [SerializeField]
        public static Interactor Instance
        {
            get
            {
                if (_instance == null)
                    Instance = FindObjectOfType<Interactor>();
                return _instance;
            }
            set { _instance = value; }
        }

        [SerializeField] public bool debug = false;
        [HideInInspector] public int selectedTab;
        [HideInInspector] public string savePath;
        [HideInInspector] public float maxRadius;
        [HideInInspector] [SerializeField] public bool logoChange;
        [HideInInspector] [SerializeField] public float opacity = 1f;

        private ExtraDebugs[] _extraDebugs; //Only 1 needed, but arrays are needed for...
        private ExtraDebugs.DebugData[] _debugDatas; //...multiple ExtraDebugs to check...
        private int[] _targetIDs; //...more effector simultaneously on screen

        //Different debug line colors for each effector. 
        //Max 8 colors right now, can be increased if needed.
        public static Color ColorForArrayPlace(int arrayPlace, bool active)
        {
            Color debugColor;

            switch (arrayPlace)
            {
                case 0:
                    debugColor = Color.blue;
                    break;
                case 1:
                    debugColor = Color.red;
                    break;
                case 2:
                    debugColor = Color.magenta;
                    break;
                case 3:
                    debugColor = Color.green;
                    break;
                case 4:
                    debugColor = Color.yellow;
                    break;
                case 5:
                    debugColor = Color.cyan;
                    break;
                case 6:
                    debugColor = Color.black;
                    break;
                case 7:
                    debugColor = Color.gray;
                    break;
                default:
                    debugColor = Color.white;
                    break;
            }
            
            if (!active)
            {
                debugColor.a = 0.15f;
            }

            return debugColor;
        }
#endif
        #endregion

        private void Awake()
        {
            playerTransform = this.transform;
            sphereCol = GetComponent<SphereCollider>();
            sphereCol.isTrigger = true;

            if (!interactionStates)
                interactionStates = gameObject.AddComponent<InteractionStates>();

            effectors = new EffectorStatus[effectorLinks.Count];
            for (int i = 0; i < effectors.Length; i++)
            {
                effectors[i] = new EffectorStatus();
            }

            //Layermask for raycasts to not hit player colliders or interactor sphere trigger.
            _layerMask = ~LayerMask.GetMask(layerName);
            if (_layerMask == -1)
                Debug.LogWarning("\"" + layerName + "\" layer doesn't exist.  You can create a new Player layer and assign your player to it (with children) or if you already have player layer with different name, you can set it on Interactor component Layer/Raycast settings.", this);
            else if (this.gameObject.layer != LayerMask.NameToLayer(layerName))
                Debug.LogWarning("Player gameobject didn't assigned to \"" + layerName + "\" layer. You can experience raycast issues.", this);

            ExtraDebugInit();
        }
        private void Start()
        {
            //For transferring the player velocity to dropped object (one hand pick ups)
            playerRigidbody = GetComponent<Rigidbody>();
            //Get player collider, first one which isn't a trigger. If you have multiple colliders you need to modify playerCollider as array.
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger)
                {
                    playerCollider = colliders[i];
                    break;
                }
            }

            if (!(_mainCam = Camera.main))
            {
                Debug.LogWarning("Interactor could not find main camera. Distance interactions will be disabled.", this);
            }

            if (!(interactorIK = GetComponent<InteractorIK>()) && !_interactorAi)
            {
                Debug.LogWarning("There is no InteractorIK on " + this.gameObject.name, this);
            }
            if (interactorIK && interactorIK.Animator && interactorIK.Animator.isHuman)
            {
                _leftElbow = interactorIK.Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                Transform leftHand = interactorIK.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform leftUpperArm = interactorIK.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                armLength = Vector3.Distance(leftHand.position, _leftElbow.position) + Vector3.Distance(_leftElbow.position, leftUpperArm.position);
                //_leftHandAxis = HandAxisForward(_leftElbow, leftHand);
                _leftHandAxis = Quaternion.Inverse(leftHand.rotation) * (leftHand.position - _leftElbow.position).normalized;

                _rightElbow = interactorIK.Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                Transform rightHand = interactorIK.Animator.GetBoneTransform(HumanBodyBones.RightHand);
                _rightHandAxis = Quaternion.Inverse(rightHand.rotation) * (rightHand.position - _rightElbow.position).normalized;
            }

            if (vehicleInput = FindObjectOfType<VehicleBasicInput>())
            {
                vehiclePartCont = vehicleInput.vehPartControl;
                if (vehiclePartCont != null)
                {
                    vehiclePartsActive = true;
                }
                childTurrets = FindObjectsOfType<TurretAim>();
            }

            if (lookAtTargetEnabled)
            {
                _lookAtTarget = new LookAtTarget();
                if (alternateHead) lookAtTargetEnabled = _lookAtTarget.Init(this, interactorIK, alternateHead);
                else
                {
                    if (interactorIK)
                    {
                        if (interactorIK.Animator && interactorIK.Animator.isHuman)
                        {
                            Transform headTransform = interactorIK.Animator.GetBoneTransform(HumanBodyBones.Head);
                            if (headTransform) lookAtTargetEnabled = _lookAtTarget.Init(this, interactorIK, headTransform);
                            else
                            {
                                Debug.LogWarning("Interactor LookAtTarget option enabled but avatar has no head bone. You can assign alternative head bone at Interactor/LookAtTarget section. Or disable LookAtTarget option.", this);
                                lookAtTargetEnabled = false;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Interactor LookAtTarget option enabled but Animator component for InteractorIK could not find it on this gameobject and its children. You can assign alternative head bone at Interactor/LookAtTarget section. Or disable LookAtTarget option.", this);
                            lookAtTargetEnabled = false;
                        }
                    }
                    else lookAtTargetEnabled = false;
                }

                if (!lookAtTargetEnabled)
                {
                    _lookAtTarget = null;
                    lookInitiateFailed = true;
                }
            }
            
            if (selfInteractionObject != null && selfInteractionObject.activeInHierarchy)
            {
                IntObjEnter(selfInteractionObject);
                selfInteractionEnabled = true;
                selectedByUI = 1;
            }
            else
                selfInteractionEnabled = false;

            sphereColWithRotScale = (sphereCol.center.x * playerTransform.right) + (sphereCol.center.y * playerTransform.up) + (sphereCol.center.z * playerTransform.forward);

            for (int i = 0; i < effectorLinks.Count; i++)
            {
                effectorLinks[i].Initiate(interactorIK, playerTransform, _layerMask, sphereColWithRotScale, i, this);
            }
        }
        private void FixedUpdate()
        {
            _connectOnce = false;
            _disconnectOnce = false;
            if (raycastDistance > 0) DistanceObjRay();
            if (intObjComponents.Count <= 0) return;

            _playerCenter = playerTransform.position + sphereCol.center;

            for (int i = 0; i < intObjComponents.Count; i++)
            {
                //Object destroyed or exited from list without physics check (colliders)
                if (intObjComponents[i].interactorObject == null || !intObjComponents[i].interactorObject.gameObject.activeInHierarchy)
                {
                    for (int a = 0; a < effectorLinks.Count; a++)
                    {
                        if (effectorLinks[a].enabled && effectors[a].connected)
                            effectorLinks[a].ObjectExit(intObjComponents[i].interactorObject);
                    }

                    if (intObjComponents[i].interactorObject != null && intObjComponents[i].interactorObject.gameObject == _activeDistanceIntObj)
                        _activeDistanceIntObj = null;

                    RemoveInteraction(intObjComponents[i]);
                    continue;
                }

                //If interaction is self or distance, do not check its position.
                //They will always stay on top of list. Self will stay 0 and wont display on list
                //Distance will stay 1 on list and will be first selection on list.
                if (selfInteractionEnabled && i == 0)
                    continue;

                if (intObjComponents[i].interactorObject.interactionType == InteractionTypes.DistanceCrosshair)
                    continue;

                //Get all square distance updates in interaction obj list
                PositionUpdate(i);
            }

            if (intObjComponents.Count > 1)
            {
                //Sort list depending on their distances
                intObjComponents.Sort(DistanceSort);
            }

            //Effectors needs properly positioned and rotated sphere trigger position, if not centered.
            sphereColWithRotScale = (sphereCol.center.x * playerTransform.right) + (sphereCol.center.y * playerTransform.up) + (sphereCol.center.z * playerTransform.forward);

            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                effectorLinks[i].Update(intObjComponents, sphereColWithRotScale);
            }

            LookUpdate();
        }

        #region Raycast & Distance Updates
        private void DistanceObjRay()
        {
            if (!_mainCam) return;

            _mousePosRay = _mainCam.ScreenPointToRay(BasicInput.GetMousePosition());

            if (Physics.Raycast(_mousePosRay, out _lookHit, raycastDistance, _layerMask))
            {
                if (_lookHit.collider.gameObject != _activeDistanceIntObj)
                {
                    if (_activeDistanceIntObj != null) IntObjExit(_activeDistanceIntObj);

                    IntObjEnter(_lookHit.collider.gameObject);
                }
            }
            else if (_activeDistanceIntObj != null)
            {
                IntObjExit(_activeDistanceIntObj);
            }
        }
        //Since we only use distance to sort objects and DefaultAnimated interaction, instead of using expensive distance call, we're using sqrMag.
        private void PositionUpdate(int i)
        {
            intObjComponents[i].distanceSqr = (_playerCenter - intObjComponents[i].interactorObject.transform.position).sqrMagnitude;
        }
        //If compare1 is bigger return 1, else return -1
        public int DistanceSort(IntObjComponents compare1, IntObjComponents compare2)
        {
            if (compare1.interactorObject.priority != compare2.interactorObject.priority)
                return compare2.interactorObject.priority.CompareTo(compare1.interactorObject.priority);

            return compare1.distanceSqr.CompareTo(compare2.distanceSqr);
        }
        #endregion

        #region Add Interaction
        //Adds new interaction objects to list. This is for Self or distance interactions.
        private void IntObjEnter(GameObject Object)
        {
            InteractorObject intObj;
            if (!(intObj = Object.GetComponent<InteractorObject>())) return;

            if (intObj.interactionType != InteractionTypes.DistanceCrosshair && intObj.interactionType != InteractionTypes.SelfItch)
            {
                //Enables UI object selection with cursor
                /*int cursorSelection = CheckInteractionIndex(intObj);
                if (cursorSelection > 0)
                    selectedByUI = cursorSelection;*/
                return;
            }

            if (intObjComponents.Count > 0)
            {
                for (int i = 0; i < intObjComponents.Count; i++)
                {
                    if (intObjComponents[i].interactorObject == intObj) return;
                }
            }

            IntObjComponents temp = new IntObjComponents();
            //This is for keeping self interaction at first position
            //distance interaction at second position on interaction list, 
            //which is sorted by distance except these two (Or priority, these two types have max priority by default).
            if (intObj.interactionType == InteractionTypes.SelfItch)
            {
                temp.distanceSqr = -1;
            }
            else
            {
                temp.distanceSqr = 0;
            }
            temp.interactorObject = intObj;

            if (intObj.interactionType == InteractionTypes.DistanceCrosshair) _activeDistanceIntObj = Object;

            intObjComponents.Add(temp);
            AddNewDebugData(intObj);

            if (selectedByUI < intObjComponents.Count && intObj.interactionType != InteractionTypes.SelfItch && intObjComponents[selectedByUI].interactorObject == intObj)
            {
                NewLookOrder(intObj, Look.OnSelection);
            }
            else
            {
                NewLookOrder(intObj, Look.Before);
            }
        }

        //Adds new interaction objects to list. This is for regular interaction objects with collider.
        private void OnTriggerEnter(Collider collidedObj)
        {
            InteractorObject intObj;
            if (!(intObj = collidedObj.GetComponent<InteractorObject>())) return;

            if (intObj.interactionType == InteractionTypes.DistanceCrosshair) return;

            if (intObjComponents.Count > 0)
            {
                for (int i = 0; i < intObjComponents.Count; i++)
                {
                    if (intObjComponents[i].interactorObject == intObj) return;
                }
            }

            IntObjComponents temp = new IntObjComponents();
            temp.distanceSqr = (sphereCol.center - intObj.transform.position).sqrMagnitude;
            temp.interactorObject = intObj;

            intObjComponents.Add(temp);
            AddNewDebugData(intObj);

            if (selectedByUI < intObjComponents.Count && intObjComponents[selectedByUI].interactorObject == intObj)
            {
                NewLookOrder(intObj, Look.OnSelection);
            }
            else
            {
                NewLookOrder(intObj, Look.Before);
            }
        }
        #endregion

        #region Remove Interaction
        //Removes interaction objects from list. This is for distance interactions.
        private void IntObjExit(GameObject Object)
        {
            InteractorObject intObj;
            if (!(intObj = Object.GetComponent<InteractorObject>())) return;

            if (intObj.interactionType != InteractionTypes.DistanceCrosshair) return;

            if (intObjComponents.Count > 0)
            {
                for (int i = 0; i < intObjComponents.Count; i++)
                {
                    if (intObjComponents[i].interactorObject == intObj)
                    {
                        for (int a = 0; a < effectorLinks.Count; a++)
                        {
                            if (effectorLinks[a].enabled && effectors[a].connected)
                                effectorLinks[a].ObjectExit(intObjComponents[i].interactorObject);
                        }
                        RemoveInteraction(intObjComponents[i]);
                    }
                }
            }
        }

        //Removes interaction objects from list. This is for regular interaction objects with collider.
        private void OnTriggerExit(Collider collidedObj)
        {
            InteractorObject intObj;
            if (!(intObj = collidedObj.GetComponent<InteractorObject>())) return;

            if (intObj.interactionType == InteractionTypes.DistanceCrosshair) return;
            if (intObj.preventExit || intObj.aiTarget != null) return;

            if (intObjComponents.Count > 0)
            {
                for (int i = 0; i < intObjComponents.Count; i++)
                {
                    if (intObjComponents[i].interactorObject == intObj)
                    {
                        for (int a = 0; a < effectorLinks.Count; a++)
                        {
                            if (effectorLinks[a].enabled && effectors[a].connected)
                                effectorLinks[a].ObjectExit(intObjComponents[i].interactorObject);
                        }
                        RemoveInteraction(intObjComponents[i]);
                    }
                }
            }
        }

        private void RemoveInteraction(IntObjComponents removed)
        {
            if (removed.interactorObject)
            {
                removed.interactorObject.used = false;
                removed.interactorObject.RemoveEffectorFromUseables(-1);
                if (removed.interactorObject.gameObject == _activeDistanceIntObj)
                    _activeDistanceIntObj = null;
            }
            if (vehiclePartsActive)
            {
                vehiclePartCont.Animate(removed.interactorObject.vehiclePartId, false, this);
            }
            RemoveLookTargets(removed.interactorObject);
            RemoveDebugData(removed.interactorObject);
            intObjComponents.Remove(removed);
        }
        #endregion

        #region Connect & Disconnect Effectors
        //Starts or stops interaction with sent InteractorObject
        public void StartStopInteraction(InteractorObject interactorObject)
        {
            for (int i = 0; i < intObjComponents.Count; i++)
            {
                if (intObjComponents[i].interactorObject == interactorObject)
                {
                    if (StartInteractorAI(interactorObject)) return;
                    if (interactorObject.animAssistEnabled && _animAssist && _animAssist.CheckAnimAssist(interactorObject.clipName) && !interactorObject.orbitalReach)
                    {//AnimAssist and clip is good. Interaction will get started with anim event.
                        if (_animAssist.clipPlaying) return;
                        if (interactorObject.secondUse || !interactorObject.used)
                        {
                            _animAssist.SetAnimAssist(interactorObject);
                            return;
                        }
                    }

                    for (int j = 0; j < effectorLinks.Count; j++)
                    {
                        if (!effectorLinks[j].enabled) continue;

                        //If already did a ConnectAll or DisconnectedAll in this frame, stop loop. 
                        //To prevent multiple effectors to toggle themself.
                        if (_connectOnce || _disconnectOnce) return;

                        effectorLinks[j].StartStopInteractionThis(intObjComponents, i, false);
                    }
                }
            }
        }

        //Gets called by InteractorAi
        public void StartStopInteractionAi(InteractorObject interactorObject, bool pathfind)
        {
            for (int i = 0; i < intObjComponents.Count; i++)
            {
                if (intObjComponents[i].interactorObject == interactorObject)
                {
                    if (StartInteractorAI(interactorObject) || pathfind) return;
                    if (interactorObject.animAssistEnabled && _animAssist && _animAssist.CheckAnimAssist(interactorObject.clipName) && !interactorObject.orbitalReach)
                    {//AnimAssist and clip is good. Interaction will get started with anim event.
                        if (_animAssist.clipPlaying) return;
                        if (interactorObject.secondUse || !interactorObject.used)
                        {
                            _animAssist.SetAnimAssist(interactorObject);
                            return;
                        }
                    }

                    for (int j = 0; j < effectorLinks.Count; j++)
                    {
                        if (!effectorLinks[j].enabled) continue;

                        //If already did a ConnectAll or DisconnectedAll in this frame, stop loop. 
                        //To prevent multiple effectors to toggle themself.
                        if (_connectOnce || _disconnectOnce) return;

                        effectorLinks[j].StartStopInteractionThis(intObjComponents, i, false);
                    }
                }
            }
        }

        public void StartStopInteractions(bool click)
        {
            if (!click && selectedByUI < intObjComponents.Count)
            {
                InteractorObject interactorObject = intObjComponents[selectedByUI].interactorObject;
                if (StartInteractorAI(interactorObject)) return;
                if (interactorObject.animAssistEnabled && _animAssist && _animAssist.CheckAnimAssist(interactorObject.clipName) && !interactorObject.orbitalReach)
                {//AnimAssist and clip is good. Interaction will get started with anim event.
                    if (_animAssist.clipPlaying) return; //intObj.used can be true for a moment so spamming buttons can go into interactor without returning at below
                    if (interactorObject.secondUse || !interactorObject.used)
                    {
                        _animAssist.SetAnimAssist(interactorObject);
                        return;
                    }
                }
            }

            checkOncePerObject = false;
            //Run for every effector
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                //If already did a ConnectAll or DisconnectedAll in this frame, stop loop. 
                //To prevent multiple effectors to toggle themself.
                if (_connectOnce || _disconnectOnce) break;

                effectorLinks[i].StartStopInteractionThis(intObjComponents, selectedByUI, click);
            }
            ResetSelection();
        }

        //Gets called by AnimAssist clip
        public void StartStopInteractionAnim(InteractorObject interactorObject)
        {
            for (int i = 0; i < intObjComponents.Count; i++)
            {
                if (intObjComponents[i].interactorObject == interactorObject)
                {
                    for (int j = 0; j < effectorLinks.Count; j++)
                    {
                        if (!effectorLinks[j].enabled) continue;
                        if (_connectOnce || _disconnectOnce) return;

                        effectorLinks[j].StartStopInteractionThis(intObjComponents, i, false);
                    }
                }
            }
        }

        public void StartStopInteractionOrbital(InteractorObject interactorObject, InteractorTarget interactorTarget, int effectorArray)
        {
            if (interactorObject.pickableOneSettings && interactorObject.pickableOneSettings.orbitalDropping)
            {//To stop pickableOne orbital
                //bool success = interactorObject.pickableOneSettings.Drop();
                orbitalReach.ResumeOrbitalAnim(interactorObject);
                //if (!success) return;

                interactorObject.SendUnityEndEvent();
                interactorObject.used = false;
                NewLookOrder(interactorObject, Look.After);
                return;
            }

            if (interactorObject.multipleSettings)
            {
                orbitalReach.SetLayerWeightToZero();
                interactorObject.multipleSettings.MultipleIn(this);
                ConnectAll(interactorObject.childTargets, interactorObject);
                return;
            }
            if (interactorObject.interactionType == InteractionTypes.PickableTwo)
            {
                int leftHand = GetEffectorIndex(5);
                int rightHand = GetEffectorIndex(6);
                Connect(leftHand, interactorObject, interactorObject.GetTargetForEffectorType(5));
                effectorLinks[leftHand].ConnectThis(interactorObject);
                Connect(rightHand, interactorObject, interactorObject.GetTargetForEffectorType(6));
                effectorLinks[rightHand].ConnectThis(interactorObject);
                orbitalReach.ResumeOrbitalAnim(interactorObject);
                return;
            }

            //Connect(effectorArray, interactorObject, interactorTarget);
            effectorLinks[effectorArray].ConnectThis(interactorObject);
        }

        //Pick up Methods
        public void StartStopInteraction(InteractorObject interactorObject, PickableOne.HoldType holdType)
        {
            if (!interactorObject || !interactorObject.pickableOneSettings) return;

            if (interactorObject.currentInteractor != this || !interactorObject.pickableOneSettings.oneHandPicked)
            {
                interactorObject.pickableOneSettings.holdingType = holdType;
                StartStopInteraction(interactorObject);
                return;
            }

            interactorObject.pickableOneSettings.dropType = PickableOne.DropType.DontDrop;
            if (interactorObject.pickableOneSettings.holdingType == holdType)
                interactorObject.pickableOneSettings.ChangeHold();
            else interactorObject.pickableOneSettings.ChangeHold(holdType);
        }
        public void StartStopInteraction(InteractorObject interactorObject, PickableOne.HoldType holdType, int holdPoint)
        {
            if (!interactorObject || !interactorObject.pickableOneSettings) return;
            if (interactorObject.currentInteractor != this || !interactorObject.pickableOneSettings.oneHandPicked)
            {
                interactorObject.pickableOneSettings.holdingType = holdType;
                interactorObject.pickableOneSettings.holdPoint = holdPoint;
                StartStopInteraction(interactorObject);
                return;
            }

            interactorObject.pickableOneSettings.dropType = PickableOne.DropType.DontDrop;
            if (interactorObject.pickableOneSettings.holdingType == holdType && interactorObject.pickableOneSettings.holdPoint == holdPoint)
                interactorObject.pickableOneSettings.ChangeHold();
            else interactorObject.pickableOneSettings.ChangeHold(holdType, holdPoint);
        }
        public void StartStopInteraction(InteractorObject interactorObject, int holdPoint1, int holdPoint2)
        {
            if (!interactorObject || !interactorObject.pickableOneSettings) return;
            if (interactorObject.currentInteractor != this || !interactorObject.pickableOneSettings.oneHandPicked)
            {
                interactorObject.pickableOneSettings.holdingType = PickableOne.HoldType.HoldTransform;
                interactorObject.pickableOneSettings.holdPoint = holdPoint1;
                StartStopInteraction(interactorObject);
                return;
            }

            interactorObject.pickableOneSettings.dropType = PickableOne.DropType.DontDrop;
            interactorObject.pickableOneSettings.holdingType = PickableOne.HoldType.HoldTransform;
            interactorObject.pickableOneSettings.ChangeHold(holdPoint1, holdPoint2);
        }
        public void StartStopInteraction(InteractorObject interactorObject, PickableOne.DropType dropType)
        {
            if (!interactorObject || !interactorObject.pickableOneSettings) return;

            interactorObject.pickableOneSettings.dropType = dropType;
            StartStopInteraction(interactorObject);
        }

        public bool EffectorCheckOrbitalPosition(Transform position, int effectorType)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (effectorType == (int)effectorLinks[i].effectorType)
                {
                    return effectorLinks[i].EffectorCheckOrbitalPosition(position);
                }
            }
            return false;
        }

        public bool IsInteractable(InteractorObject interactorObject)
        {
            if (interactorObject.multipleConnections)
                return IsInteractableForAllTargets(interactorObject);
            else return IsInteractableForSingleTarget(interactorObject);
        }
        private bool IsInteractableForAllTargets(InteractorObject interactorObject)
        {
            bool useable = true;
            InteractorTarget target;
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                target = interactorObject.GetTargetForEffectorType((int)effectorLinks[i].effectorType);
                if (!effectorLinks[i].EffectorCheckOrbitalPosition(target.transform))
                {
                    useable = false;
                    break;
                }
            }
            return useable;
        }
        private bool IsInteractableForSingleTarget(InteractorObject interactorObject)
        {
            bool useable = false;
            InteractorTarget target;
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                target = interactorObject.GetTargetForEffectorType((int)effectorLinks[i].effectorType);
                if (effectorLinks[i].EffectorCheckOrbitalPosition(target.transform))
                {
                    useable = true;
                    break;
                }
            }
            return useable;
        }
        public bool IsInteractingWith(InteractorObject interactorObject)
        {
            if (GetEffectorInteractingWith(interactorObject) <= 0) return false;
            else return true;
        }
        public FullBodyBipedEffector GetEffectorInteractingWith(InteractorObject interactorObject)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;
                if (!effectors[i].connected) continue;

                if (effectors[i].connectedTo == interactorObject)
                    return effectorLinks[i].effectorType;
            }
            return (FullBodyBipedEffector)(-1);
        }
        public bool IsEffectorInteracting(FullBodyBipedEffector effectorType)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                if (effectorLinks[i].effectorType == effectorType)
                    return effectors[i].connected;
            }
            return false;
        }
        public InteractorObject GetCurrentInteractorObject(FullBodyBipedEffector effectorType)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                if (effectorLinks[i].effectorType == effectorType)
                    return effectors[i].connectedTo;
            }
            return null;
        }
        public InteractorTarget GetCurrentInteractorTarget(FullBodyBipedEffector effectorType)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (!effectorLinks[i].enabled) continue;

                if (effectorLinks[i].effectorType == effectorType)
                    return effectors[i].connectedTarget;
            }
            return null;
        }

        private bool StartInteractorAI(InteractorObject intObj)
        {
            if (_interactorAi && intObj.aiTarget && !intObj.used && CheckMobility())
            {
                if (!intObj.Reached)
                {
                    if (intObj.GetOtherUseableEffector(10) == -1 || intObj.aiOnly)
                    {
                        _interactorAi.StartPathfinding(intObj);
                        return true;
                    }
                }
                else SetIntObjUseables(intObj);
            }
            return false;
        }
        private bool CheckMobility()
        {//Checks if body or feets are busy for moving
            for (int i = 0; i < effectors.Length; i++)
            {
                if (effectors[i].connected)
                {
                    if (effectorLinks[i].effectorType == FullBodyBipedEffector.Body || effectorLinks[i].effectorType == FullBodyBipedEffector.LeftFoot || effectorLinks[i].effectorType == FullBodyBipedEffector.RightFoot)
                        return false;
                }
            }
            return true;
        }

        //To skip target rules and obstacle checks for InteractorObject
        private void SetIntObjUseables(InteractorObject intObj)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                int type = (int)effectorLinks[i].effectorType;
                if (intObj.HasEffectorTypeInTargets(type))
                    intObj.AddEffectorToUseables(type);
            }
        }

        //Resets UI selection after StartStopInteractions() done
        private void ResetSelection()
        {
            if (selfInteractionEnabled)
            {
                selectedByUI = 1;
                return;
            }
            selectedByUI = 0;
        }

        //Holds connection info for an effector with given array place
        public void Connect(int arrayPlace, InteractorObject connectedTo, InteractorTarget connectedTarget)
        {
            effectors[arrayPlace].connected = true;
            effectors[arrayPlace].connectedTo = connectedTo;
            effectors[arrayPlace].connectedTarget = connectedTarget;
            anyConnected = true;
        }

        //Clears connection info for an effector with given array place
        public void Disconnect(int arrayPlace)
        {
            effectors[arrayPlace].connected = false;
            effectors[arrayPlace].connectedTo = null;
            effectors[arrayPlace].connectedTarget = null;
            anyConnected = false;

            for (int i = 0; i < effectors.Length; i++)
            {
                if (effectors[i].connected)
                    anyConnected = true;
            }
        }

        //Connection info gets updated for all effectors. Runs only once in same frame.
        public void ConnectAll(InteractorTarget[] allTargets, InteractorObject connectedTo)
        {
            if (_connectOnce) return;

            for (int i = 0; i < effectors.Length; i++)
            {
                for (int a = 0; a < allTargets.Length; a++)
                {
                    if ((int)effectorLinks[i].effectorType == (int)allTargets[a].effectorType)
                    {
                        Connect(i, connectedTo, allTargets[a]);
                        effectorLinks[i].ConnectThis(connectedTo);
                    }
                }
            }
            _connectOnce = true;
        }

        //Clears connection info for all effectors at the same time
        public void DisconnectAll()
        {
            if (_disconnectOnce) return;
            if (!anyConnected)
            {
                _disconnectOnce = true;
                return;
            }

            for (int i = 0; i < effectors.Length; i++)
            {
                if (effectors[i].connected)
                {
                    effectorLinks[i].DisconnectThis();
                    Disconnect(i);
                }
            }
            anyConnected = false;
            _disconnectOnce = true;
        }
        public void DisconnectAllForThisOnce(InteractorObject disconnectedObject)
        {
            if (_disconnectOnce) return;

            for (int i = 0; i < effectors.Length; i++)
            {
                if (effectors[i].connectedTo == disconnectedObject)
                {
                    effectorLinks[i].DisconnectThis();
                    Disconnect(i);
                }
            }
            _disconnectOnce = true;
        }
        public void DisconnectAllForThis(InteractorObject disconnectedObject)
        {
            for (int i = 0; i < effectors.Length; i++)
            {
                if (effectors[i].connectedTo == disconnectedObject)
                {
                    effectorLinks[i].DisconnectThis();
                    Disconnect(i);
                }
            }
        }
        public int GetEffectorIndex(int effectorType)
        {
            if (effectorType < 0) return -1;

            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if ((int)effectorLinks[i].effectorType == effectorType)
                {
                    return i;
                }
            }
            return -1;
        }
        public Transform GetEffectorElbow(int effectorType)
        {
            if (effectorLinks[GetEffectorIndex(effectorType)].effectorType == FullBodyBipedEffector.LeftHand) return _leftElbow;
            else if (effectorLinks[GetEffectorIndex(effectorType)].effectorType == FullBodyBipedEffector.RightHand) return _rightElbow;
            return null;
        }
        #endregion

        #region Look At Target
        public void NewLookOrder(InteractorObject interactorObject, Look newLookState)
        {
            if (lookInitiateFailed) return;
            if (_lookAtTarget == null)
            {
                lookInitiateFailed = true;
                return;
            }
            if (!CheckInteraction(interactorObject)) return;

            _lookAtTarget.NewLookOrder(interactorObject, newLookState);
        }

        public void RemoveLookTargets(InteractorObject removeObject)
        {
            if (lookInitiateFailed) return;
            if (_lookAtTarget == null)
            {
                lookInitiateFailed = true;
                return;
            }

            _lookAtTarget.RemoveLookTargets(removeObject);
        }

        private void LookUpdate()
        {
            if (lookInitiateFailed || _lookAtTarget == null) return;

            if (waitForNewTarget > 0)
            {
                waitForNewTarget -= Time.fixedDeltaTime;
                if (waitForNewTarget <= 0)
                {
                    _lookAtTarget.TryNextLook();
                }
            }

            if (lookEndTimer > 0)
            {
                lookEndTimer -= Time.fixedDeltaTime;
                if (lookEndTimer <= 0)
                {
                    _lookAtTarget.RemoveCurrentLookTarget();
                }
            }

            _lookAtTarget.DrawDebugLines();
        }

        public bool CheckInteraction(InteractorObject interactorObject)
        {
            if (!interactorObject) return true;

            for (int i = 0; i < intObjComponents.Count; i++)
            {
                if (intObjComponents[i].interactorObject == interactorObject)
                    if (interactorObject.gameObject.activeInHierarchy && interactorObject.enabled)
                    return true;
            }
            return false;
        }

        public int CheckInteractionIndex(InteractorObject interactorObject)
        {
            if (!interactorObject) return -1;

            for (int i = 0; i < intObjComponents.Count; i++)
            {
                if (intObjComponents[i].interactorObject == interactorObject)
                    return i;
            }
            return -1;
        }

        public bool CheckSelection(InteractorObject interactorObject)
        {
            if (selectedByUI >= intObjComponents.Count) return false;
            if (intObjComponents[selectedByUI].interactorObject == interactorObject)
                    return true;
            
            return false;
        }
        #endregion

        #region AccessMethods
        public InteractorTarget ReturnSelfActiveTarget()
        {
            return _selfActiveTarget;
        }

        public LayerMask GetPlayerLayerMask()
        {
            return _layerMask;
        }
        //Adds an InteractorObject to Interactor's active area interactions list
        public void AddInteractionManual(InteractorObject interactorObject)
        {
            if (intObjComponents.Count > 0)
            {
                for (int i = 0; i < intObjComponents.Count; i++)
                {
                    if (intObjComponents[i].interactorObject == interactorObject) return;
                }
            }

            IntObjComponents temp = new IntObjComponents();
            temp.interactorObject = interactorObject;
            intObjComponents.Add(temp);
            AddNewDebugData(interactorObject);
            for (int i = 0; i < effectorLinks.Count; i++)
            {//Update effectors and related intObjs to set themselves so added interaction can be used this frame
                if (!effectorLinks[i].enabled) continue;

                effectorLinks[i].Update(intObjComponents, sphereColWithRotScale);
            }

            if (selectedByUI < intObjComponents.Count && interactorObject.interactionType != InteractionTypes.SelfItch && intObjComponents[selectedByUI].interactorObject == interactorObject)
            {
                NewLookOrder(interactorObject, Look.OnSelection);
            }
            else
            {
                NewLookOrder(interactorObject, Look.Before);
            }
        }
        //Removes an InteractorObject from Interactor's active area interactions list
        public void RemoveInteractionManual(InteractorObject interactorObject)
        {
            if (intObjComponents.Count > 0)
            {
                for (int i = 0; i < intObjComponents.Count; i++)
                {
                    if (intObjComponents[i].interactorObject == interactorObject)
                    {
                        for (int a = 0; a < effectorLinks.Count; a++)
                        {
                            if (effectorLinks[a].enabled && effectors[a].connected)
                                effectorLinks[a].ObjectExit(intObjComponents[i].interactorObject);
                        }
                        RemoveInteraction(intObjComponents[i]);
                    }
                }
            }
        }

        public bool CanPick(InteractorObject interactorObject)
        {
            for (int i = 0; i < interactorObject.childTargets.Length; i++)
            {
                InteractorTarget target = interactorObject.childTargets[i];
                int effectorIndex = GetEffectorIndex((int)target.effectorType);

                if (!effectorLinks[effectorIndex].enabled) continue;
                if (effectors[effectorIndex].connected) continue;

                bool passed = !interactorObject.used;
                if (passed && interactorObject.orbitalReach) passed = effectorLinks[effectorIndex].EffectorCheckOrbital(target, interactorObject);
                if (passed && !interactorObject.orbitalReach) passed = effectorLinks[effectorIndex].EffectorCheckXZ(target);
                if (passed) passed = !effectorLinks[effectorIndex].HasObstacleForTarget(interactorObject, effectorLinks[effectorIndex].effectorWorldSpace, target.transform);

                if (passed)
                {
                    interactorObject.AddEffectorToUseables((int)effectorLinks[effectorIndex].effectorType);
                }
                return passed;
            }
            return false;
        }
        public bool ForcePick(InteractorObject interactorObject, bool orbitalIncluded)
        {
            for (int i = 0; i < interactorObject.childTargets.Length; i++)
            {
                InteractorTarget target = interactorObject.childTargets[i];
                int effectorIndex = GetEffectorIndex((int)target.effectorType);
                if (!effectorLinks[effectorIndex].enabled) continue;
                if (effectors[effectorIndex].connected) continue;
                if (interactorObject.used) continue;

                interactorObject.used = true;
                interactorObject.Prepare(false);

                AddInteractionManual(interactorObject);
                interactorObject.preventExit = true;

                if (interactorObject.orbitalReach && orbitalIncluded) effectorLinks[effectorIndex].PrepareOrbitalInteraction(interactorObject, target);
                else effectorLinks[effectorIndex].StartInteraction(target.effectorType, target, interactorObject);

                interactorObject.gameObject.SetActive(true);
                return true;
            }
            return false;
        }

        public void SetIntObjUseables(InteractorObject intObj, FullBodyBipedEffector effectorType)
        {
            for (int i = 0; i < effectorLinks.Count; i++)
            {
                if (effectorLinks[i].effectorType == effectorType && intObj.HasEffectorTypeInTargets((int)effectorType))
                {
                    intObj.AddEffectorToUseables((int)effectorType);
                }
            }
        }
        #endregion

        #region ExtraDebugs
        private void ExtraDebugInit()
        {
#if UNITY_EDITOR
            _extraDebugs = GetComponents<ExtraDebugs>();
            int length = _extraDebugs.Length;
            if (length > 0)
            {
                _debugDatas = new ExtraDebugs.DebugData[length];
                for (int i = 0; i < length; i++)
                {
                    _debugDatas[i] = new ExtraDebugs.DebugData(null, null, (InteractionTypes)(-1));
                }
                _targetIDs = new int[length];
            }
#endif
        }
        private void AddNewDebugData(InteractorObject intObj)
        {
#if UNITY_EDITOR
            if (!debug) return;
            if (_extraDebugs.Length == 0) return;
            if (intObj.interactionType == InteractionTypes.DistanceCrosshair || intObj.interactionType == InteractionTypes.DefaultAnimated || intObj.interactionType == InteractionTypes.SelfItch) return;

            for (int i = 0; i < _extraDebugs.Length; i++)
            {
                if (!_extraDebugs[i] || !_extraDebugs[i].enabled) continue;

                InteractorTarget[] target = intObj.GetTargetsForEffectorType(_extraDebugs[i].GetSelectedEffectorType());
                for (int j = 0; j < target.Length; j++)
                {
                    _extraDebugs[i].AddNewDebugData(target[j].gameObject.GetInstanceID(), target[j], target[j].intObj, intObj.interactionType);
                }
            }
#endif
        }
        private void RemoveDebugData(InteractorObject intObj)
        {
#if UNITY_EDITOR
            if (!debug) return;
            if (_extraDebugs.Length == 0) return;
            if (!intObj) return;
            if (intObj.interactionType == InteractionTypes.DistanceCrosshair || intObj.interactionType == InteractionTypes.DefaultAnimated || intObj.interactionType == InteractionTypes.SelfItch) return;

            for (int i = 0; i < _extraDebugs.Length; i++)
            {
                if (!_extraDebugs[i]) continue;

                InteractorTarget[] target = intObj.GetTargetsForEffectorType(_extraDebugs[i].GetSelectedEffectorType());
                for (int j = 0; j < target.Length; j++)
                {
                    if (!target[j]) continue;

                    _extraDebugs[i].RemoveDebugData(target[j].gameObject.GetInstanceID());
                }
            }
#endif
        }
        private void GatherDebugDataStart(int effector, InteractorTarget target, InteractorObject intObj, InteractionTypes intType)
        {
#if UNITY_EDITOR
            if (!debug) return;
            if (_extraDebugs.Length == 0) return;

            for (int i = 0; i < _extraDebugs.Length; i++)
            {
                if (!_extraDebugs[i] || !_extraDebugs[i].enabled) continue;
                if (_extraDebugs[i].useInteractorTab) //Set selected tab
                    _extraDebugs[i].SetSelectedTab(selectedTab);

                if (_extraDebugs[i].useInteractorTab && selectedTab != effector) continue;
                if (!_extraDebugs[i].useInteractorTab && _extraDebugs[i].GetSelectedTab() != effector) continue;

                int currentID = target.gameObject.GetInstanceID();
                if (currentID != _targetIDs[i])
                {//This gather is called first, so we're resetting to switch to new DebugData
                    _debugDatas[i] = new ExtraDebugs.DebugData(target, intObj, intType);
                }

                _targetIDs[i] = currentID;
                _debugDatas[i].targetName = target.name;
                _debugDatas[i].intObjName = intObj.name;
                _extraDebugs[i].UpdateDebugData(_targetIDs[i], _debugDatas[i], false);
            }
#endif
        }
        private void GatherDebugDataEnd(int effector, int targetID, bool waiting, bool reposition, float distance, float minDist, float maxDist, bool angleCheck, bool obstacleEnabled, bool obstacle, string obstacleName, bool used, bool addToUseables, int useables, float progress, bool pause, int lastStatus)
        {
#if UNITY_EDITOR
            if (!debug) return;
            if (_extraDebugs.Length == 0) return;

            for (int i = 0; i < _extraDebugs.Length; i++)
            {
                if (!_extraDebugs[i] || !_extraDebugs[i].enabled) continue;
                if (_extraDebugs[i].useInteractorTab && selectedTab != effector) continue;
                if (!_extraDebugs[i].useInteractorTab && _extraDebugs[i].GetSelectedTab() != effector) continue;

                _targetIDs[i] = targetID;
                _debugDatas[i].waiting = waiting;
                _debugDatas[i].reposition = reposition;
                _debugDatas[i].distance = distance;
                _debugDatas[i].minDist = minDist;
                _debugDatas[i].maxDist = maxDist;
                _debugDatas[i].angleCheck = angleCheck;
                _debugDatas[i].obstacleEnabled = obstacleEnabled;
                _debugDatas[i].obstacle = obstacle;
                _debugDatas[i].obstacleName = obstacleName;
                _debugDatas[i].used = used;
                _debugDatas[i].addToUseables = addToUseables;
                _debugDatas[i].useables = useables;
                _debugDatas[i].progress = progress;
                _debugDatas[i].pause = pause;
                _debugDatas[i].lastStatus = lastStatus;
                _extraDebugs[i].UpdateDebugData(_targetIDs[i], _debugDatas[i], true);
            }
#endif
        }
        #endregion

        [System.Serializable]
        public class EffectorLink
        {
            #region Effector Variables
            private Interactor _interactor;
            private InteractorObject _interactorObject;
            private InteractorIK _interactorIK;
            private Transform _playerTransform;
            private bool _initiated;
            private int _this;
            //If any interaction gets in position with successful check then selfPossible gets false
            //to make itself not possible. That way we disable conflicts.
            private bool _selfPossible;
            //To check sending events once per interaction for needed interactions
            private bool _eventSent;
            private Vector3 _sphereColWithRotScale;
            private int _layerMask;
            private InteractorTarget[] _allTargets;
            private InteractorTarget _closestTarget;

            //Temporary holders for offset calculations
            private Vector3 _offsetTempVector;
            private float _tempMaxRadius, _tempMinRadius;
            private float _targetAngleY, _targetAngleY2, _targetAngleZ, _targetAngleZ2, _targetDistance;
            private float _maxAngleZ, _minAngleZ, _maxAngleY, _minAngleY;
            private bool _reverseZ, _reverseY;
            private RaycastHit hit;

            [HideInInspector] public bool targetActive;
            [HideInInspector] public Vector3 targetPosition;
            [HideInInspector] public Vector3 effectorWorldSpace;

            //Debug properties
            private bool _updateStarted;
            private int _lastTargetID;
            private bool _waiting;
            private bool _lastReposition;
            private bool _lastAngleCheck;
            private bool _lastObstacleEnabled;
            private bool _lastObstacle;
            private string _lastObstacleName;
            private bool _used;
            private bool _lastAddToUseables;
            private int _lastUseables;
            private float _lastProgress;
            private bool _lastPause;
            private int _lastStatus;
            private int _lastStatusID;

            //InterruptTransfer properties
            private bool _transferring;
            private bool _cachedTransfer;
            private bool _newEvent;
            private float _transferProgress;
            private float _oldProgress;
            private Vector3 _toTargetCachedLocalPos;
            private Quaternion _toTargetCachedLocalRot;
            private Vector3 _fromTargetCachedLocalPos;
            private Quaternion _fromTargetCachedLocalRot;
            private Transform[] _toTargetChildrenBones;
            private InteractorObject _transferFromIntObj;
            private InteractorTarget _transferFromTarget;
            private InteractorObject _transferToIntObj;
            private InteractorTarget _transferToTarget;

            #region Exposed Effector Specs
            [SerializeField] public bool enabled = true;
            [SerializeField] public string effectorName;
            [SerializeField] public FullBodyBipedEffector effectorType;
            [SerializeField] public Vector3 posOffset = Vector3.zero;
            [SerializeField][Range(-180f, 180f)] public float angleOffset;
            [SerializeField][Range(0f, 360f)] public float angleXZ = 45f;
            [SerializeField][Range(-180f, 180f)] public float angleOffsetYZ;
            [SerializeField][Range(0f, 360f)] public float angleYZ = 45f;
            [SerializeField] public float maxRadius = 0.1f;
            [SerializeField] public float minRadius = 0.05f;
            #endregion
            #endregion

            #region Initiation

            public void Initiate(InteractorIK interactorIK, Transform playerTransform, LayerMask layermask, Vector3 sphereColWithRotScale, int effectorArrayPlace, Interactor interactor)
            {
                _interactor = interactor;
                _interactorIK = interactorIK;
                _playerTransform = playerTransform;
                _layerMask = layermask;
                _sphereColWithRotScale = sphereColWithRotScale;
                _this = effectorArrayPlace;
                targetActive = false;

                _initiated = true;
            }
            #endregion

            #region Effector Checks

            //Selects a method depends on if its Z only or both axis
            public bool EffectorCheckOrbital(InteractorTarget target, InteractorObject intObj)
            {
                if (_interactor.orbitalReach && intObj.orbitalReach) return OrbitalCheck(target.transform);
                else
                {
                    if (!_interactor.orbitalReach && intObj.orbitalReach)
                    {
                        intObj.orbitalReach = false;
                        Debug.LogWarning("There is no OrbitalReach component on Interactor gameObject. Add it or disable the OrbitalReach option on InteractorObject settings.", intObj);
                    }
                    return EffectorCheck(target);
                }
            }

            private bool EffectorCheck(InteractorTarget target)
            {
                return EffectorCheck(target, target.transform);
            }
            private bool EffectorCheck(Transform outsideTransform)
            {
                return EffectorCheck(_interactor.effectors[_this].connectedTarget, outsideTransform);
            }

            //Checks the target position if it's ok to interact with given effector rules
            private bool EffectorCheck(InteractorTarget target, Transform targetTransform)
            {
                _tempMaxRadius = maxRadius;
                _tempMinRadius = minRadius;
                if (target.overrideEffector)
                {
                    _tempMaxRadius = Mathf.Clamp(target.oMaxRange, 0, _interactor.sphereCol.radius);
                    _tempMinRadius = Mathf.Clamp(target.oMinRange, 0, _tempMaxRadius);
                }

                _targetDistance = Vector3.Distance(effectorWorldSpace, targetTransform.position);
                if (_targetDistance > _tempMaxRadius) return false;
                if (_targetDistance < _tempMinRadius) return false;

                _offsetTempVector = targetTransform.position - effectorWorldSpace;
                _offsetTempVector = Vector3.ProjectOnPlane(_offsetTempVector, _playerTransform.up);
                _targetAngleZ = Vector3.SignedAngle(_offsetTempVector, _playerTransform.right, _playerTransform.up);
                _targetAngleZ2 = -(180 + _targetAngleZ);
                _targetAngleZ = 180f - _targetAngleZ;

                _reverseZ = false;

                if (target.overrideEffector)
                {
                    _maxAngleZ = target.oHorizontalOffset + target.oHorizontalAngle;
                    _minAngleZ = target.oHorizontalOffset;
                    _maxAngleY = target.oVerticalOffset + target.oVerticalAngle;
                    _minAngleY = target.oVerticalOffset;
                }
                else
                {
                    _maxAngleZ = angleOffset + angleXZ;
                    _minAngleZ = angleOffset;
                    _maxAngleY = angleOffsetYZ + angleYZ;
                    _minAngleY = angleOffsetYZ;
                }

                if (_targetAngleZ >= _maxAngleZ)
                {
                    _reverseZ = true;
                }

                _offsetTempVector = effectorWorldSpace - targetTransform.position;
                _offsetTempVector = Vector3.ProjectOnPlane(_offsetTempVector, _playerTransform.right);
                _targetAngleY = Vector3.SignedAngle(_offsetTempVector, _playerTransform.up, _playerTransform.right);
                _targetAngleY2 = -(180 + _targetAngleY);
                _targetAngleY = 180f - _targetAngleY;

                _reverseY = false;

                if (_targetAngleY >= _maxAngleY)
                {
                    _reverseY = true;
                }

                _lastAngleCheck = true;
                if (!_reverseZ)
                {
                    if (!_reverseY)
                    {
                        if (_targetAngleY >= _minAngleY && _targetAngleY <= _maxAngleY)
                        {
                            if (_targetAngleZ >= _minAngleZ && _targetAngleZ <= _maxAngleZ)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (_targetAngleY2 >= _minAngleY && _targetAngleY2 <= _maxAngleY)
                        {
                            if (_targetAngleZ >= _minAngleZ && _targetAngleZ <= _maxAngleZ)
                            {
                                return true;
                            }
                        }
                    }

                }
                else
                {
                    if (!_reverseY)
                    {
                        if (_targetAngleY >= _minAngleY && _targetAngleY <= _maxAngleY)
                        {
                            if (_targetAngleZ2 >= _minAngleZ && _targetAngleZ2 <= _maxAngleZ)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (_targetAngleY2 >= _minAngleY && _targetAngleY2 <= _maxAngleY)
                        {
                            if (_targetAngleZ2 >= _minAngleZ && _targetAngleZ2 <= _maxAngleZ)
                            {
                                return true;
                            }
                        }
                    }
                }
                _lastAngleCheck = false;
                return false;
            }

            public bool EffectorCheckXZ(InteractorTarget target)
            {
                Transform targetTransform = target.transform;
                _tempMaxRadius = maxRadius;
                _tempMinRadius = minRadius;
                if (target.overrideEffector)
                {
                    _tempMaxRadius = Mathf.Clamp(target.oMaxRange, 0, _interactor.sphereCol.radius);
                    _tempMinRadius = Mathf.Clamp(target.oMinRange, 0, _tempMaxRadius);
                }

                Vector3 _effectorWorldSpaceNoY = effectorWorldSpace;
                _effectorWorldSpaceNoY.y = targetTransform.position.y;
                _targetDistance = Vector3.Distance(_effectorWorldSpaceNoY, targetTransform.position);
                if (_targetDistance > _tempMaxRadius) return false;
                if (_targetDistance < _tempMinRadius) return false;

                _offsetTempVector = targetTransform.position - effectorWorldSpace;
                _offsetTempVector = Vector3.ProjectOnPlane(_offsetTempVector, _playerTransform.up);
                _targetAngleZ = Vector3.SignedAngle(_offsetTempVector, _playerTransform.right, _playerTransform.up);
                _targetAngleZ2 = -(180 + _targetAngleZ);
                _targetAngleZ = 180f - _targetAngleZ;

                _reverseZ = false;

                if (target.overrideEffector)
                {
                    _maxAngleZ = target.oHorizontalOffset + target.oHorizontalAngle;
                    _minAngleZ = target.oHorizontalOffset;
                }
                else
                {
                    _maxAngleZ = angleOffset + angleXZ;
                    _minAngleZ = angleOffset;
                }

                if (_targetAngleZ >= _maxAngleZ)
                {
                    _reverseZ = true;
                }

                _lastAngleCheck = true;
                if (!_reverseZ)
                {
                    if (_targetAngleZ >= _minAngleZ && _targetAngleZ <= _maxAngleZ)
                    {
                        return true;
                    }
                }
                else
                {
                    if (_targetAngleZ2 >= _minAngleZ && _targetAngleZ2 <= _maxAngleZ)
                    {
                        return true;
                    }
                }
                _lastAngleCheck = false;
                return false;
            }

            private bool OrbitalCheck(Transform targetTransform)
            {
                _tempMaxRadius = _interactor.orbitalReach.maxDist + _interactor.orbitalReach.maxMargin;
                Vector2 playerPosXZ = new Vector2(_playerTransform.position.x, _playerTransform.position.z);
                Vector2 targetPosXZ = new Vector2(targetTransform.position.x, targetTransform.position.z);
                _targetDistance = Vector2.Distance(playerPosXZ, targetPosXZ);
                if (_targetDistance > _tempMaxRadius) return false;

                _tempMinRadius = _interactor.orbitalReach.minDist - _interactor.orbitalReach.maxMargin;
                float angleNormalized = CalcOrbitalAngleNormalized(targetTransform);
                if (angleNormalized < -0.4f || angleNormalized > 0.4f)
                {
                    if (_targetDistance < _tempMinRadius) return false;
                }
                else if (_targetDistance < _tempMinRadius * 0.5f) return false;

                if (targetTransform.position.y > _playerTransform.position.y + _interactor.orbitalReach.maxHeight + _interactor.orbitalReach.maxMargin || targetTransform.position.y < _playerTransform.position.y + _interactor.orbitalReach.minHeight - _interactor.orbitalReach.maxMargin) return false;

                return true;
            }

            private Vector3 CalcOrbital(Transform target)
            {
                float minHeight = _interactor.orbitalReach.minHeight;
                float midHeight = _interactor.orbitalReach.midHeight;
                float maxHeight = _interactor.orbitalReach.maxHeight;
                float orbitalHeight = Mathf.Clamp(target.position.y - _playerTransform.position.y, minHeight, maxHeight);
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

                Vector3 playerPositionXZ = new Vector3(_playerTransform.position.x, _playerTransform.position.y + target.position.y, _playerTransform.position.z);
                _targetDistance = Vector3.Distance(playerPositionXZ, target.position);
                return new Vector3(orbitalHeightNormalized, CalcOrbitalAngleNormalized(target), _targetDistance);
            }
            private float CalcOrbitalAngleNormalized(Transform target)
            {
                //Angle and distance calculations are based on player position without Y to target. So height won't matter because all anims are adjusting body to height.
                Vector3 playerPositionXZ = new Vector3(_playerTransform.position.x, _playerTransform.position.y + target.position.y, _playerTransform.position.z);
                Vector3 orbitalVector = target.position - playerPositionXZ;
                orbitalVector = Vector3.ProjectOnPlane(orbitalVector, _playerTransform.up);
                float orbitalAngle = Vector3.SignedAngle(orbitalVector, _playerTransform.right, _playerTransform.up) - 90f;
                if (orbitalAngle > 90f || orbitalAngle <= -180f)
                    orbitalAngle = orbitalAngle + 360f;
                float orbitalAngleNormalized = Mathf.InverseLerp(-180f, 180f, orbitalAngle);
                return Mathf.Lerp(-1f, 1f, orbitalAngleNormalized); //-1 is hand side, 1 is other hand side.
            }

            public bool EffectorCheckOrbitalPosition(Transform target)
            {
                InteractorObject checkingIntObj = _interactor.effectors[_this].connectedTo;

                if (_interactor.orbitalReach && checkingIntObj.orbitalReach) return OrbitalCheck(target);
                else
                {
                    if (!_interactor.orbitalReach && checkingIntObj.orbitalReach)
                    {
                        checkingIntObj.orbitalReach = false;
                        Debug.LogWarning("There is no OrbitalReach component on Interactor gameObject. Add it or disable the OrbitalReach option on InteractorObject settings.", checkingIntObj);
                    }
                    return EffectorCheck(target);
                }
            }

            private bool EffectorCheckPosition(Vector3 position)
            {
                _tempMaxRadius = maxRadius;
                _tempMinRadius = minRadius;

                Vector3 _effectorWorldSpaceNoY = effectorWorldSpace;
                _effectorWorldSpaceNoY.y = position.y;
                _targetDistance = Vector3.Distance(_effectorWorldSpaceNoY, position);
                if (_targetDistance > _tempMaxRadius) return false;
                if (_targetDistance < _tempMinRadius) return false;

                _offsetTempVector = position - effectorWorldSpace;
                _offsetTempVector = Vector3.ProjectOnPlane(_offsetTempVector, _playerTransform.up);
                _targetAngleZ = Vector3.SignedAngle(_offsetTempVector, _playerTransform.right, _playerTransform.up);
                _targetAngleZ2 = -(180 + _targetAngleZ);
                _targetAngleZ = 180f - _targetAngleZ;

                _reverseZ = false;

                _maxAngleZ = angleOffset + angleXZ;
                _minAngleZ = angleOffset;
                
                if (_targetAngleZ >= _maxAngleZ)
                {
                    _reverseZ = true;
                }

                if (!_reverseZ)
                {
                    if (_targetAngleZ >= _minAngleZ && _targetAngleZ <= _maxAngleZ)
                    {
                        return true;
                    }
                }
                else
                {
                    if (_targetAngleZ2 >= _minAngleZ && _targetAngleZ2 <= _maxAngleZ)
                    {
                        return true;
                    }
                }
                return false;
            }

            //Checks if is there any obstacles between source and target. If obstacle is an object other than interacted object, return true.
            private bool HasObstacle(InteractorObject interactingObject, Vector3 raySource, float rayDistanceSqr)
            {
                if (!interactingObject.obstacleRaycast) return false;

                float rayDistance = Mathf.Sqrt(rayDistanceSqr);
                if (Physics.Raycast(raySource, interactingObject.transform.position - raySource, out hit, rayDistance, _layerMask))
                {
                    for (int i = 0; i < interactingObject.excludeColliders.Length; i++)
                    {
                        if (interactingObject.excludeColliders[i])
                        {
                            if (hit.collider == interactingObject.excludeColliders[i])
                                return false;
                        }
                    }
                    return true;
                }
                return false;
            }

            public bool HasObstacleForTarget(InteractorObject interactingObject, Vector3 raySource, Transform target)
            {
                if (!interactingObject.obstacleRaycast) return false;

                float rayDistance = Vector3.Distance(target.position, effectorWorldSpace);
                if (Physics.Raycast(raySource, target.position - raySource, out hit, rayDistance, _layerMask))
                {
                    for (int i = 0; i < interactingObject.excludeColliders.Length; i++)
                    {
                        if (interactingObject.excludeColliders[i])
                        {
                            if (hit.collider == interactingObject.excludeColliders[i])
                                return false;
                        }
                    }
                    _lastObstacle = true;
                    _lastObstacleName = hit.collider.name;
                    return true;
                }
                return false;
            }

            //Gets the closest target index for same object to this effector
            private int ClosestTargetSameEffector(InteractorTarget[] allTargets)
            {
                float shortestSqrDist = 25f;
                int targetPointer = -1;
                float distSqr;

                for (int i = 0; i < allTargets.Length; i++)
                {
                    if ((int)allTargets[i].effectorType != (int)effectorType) continue;

                    distSqr = (effectorWorldSpace - allTargets[i].transform.position).sqrMagnitude;

                    if (distSqr < shortestSqrDist)
                    {
                        shortestSqrDist = distSqr;
                        targetPointer = i;
                    }
                }
                return targetPointer;
            }
            private bool CloserEffector(int otherEffector, InteractorTarget target)
            {
                int otherEffectorIndex = _interactor.GetEffectorIndex(otherEffector);
                if (_interactor.effectors[otherEffectorIndex].connected) return false;

                float thisDistSqr = (effectorWorldSpace - target.transform.position).sqrMagnitude;
                Transform otherTarget = target.intObj.GetTargetForEffectorType(otherEffector).transform;
                Vector3 otherEffectorWorldSpace = _interactor.effectorLinks[otherEffectorIndex].effectorWorldSpace;
                float otherDistSqr = (otherEffectorWorldSpace - otherTarget.transform.position).sqrMagnitude;
                return otherDistSqr < thisDistSqr;
            }
            private bool IsEffectorConnected(int otherEffector)
            {//Uses InteractorObject's useable target index, not effector array index.
                int otherEffectorIndex = _interactor.GetEffectorIndex(otherEffector);
                return _interactor.effectors[otherEffectorIndex].connected;
            }
            #endregion

            #region Interruption
            public void InterruptTransfer()
            {
                if (!_cachedTransfer)
                {
                    RotatePivot(_transferToIntObj);
                    _toTargetCachedLocalPos = _transferToTarget.transform.localPosition;
                    _toTargetCachedLocalRot = _transferToTarget.transform.localRotation;
                    if (_transferToTarget.matchChildBones)
                    { //TODO
                        if (_transferToTarget.MatchSource.targetBones.Length != 0)
                        {
                            _toTargetChildrenBones = _transferToTarget.MatchSource.targetBones;
                        }
                    }
                    _oldProgress = _interactorIK.GetProgress(_interactor.effectors[_this].connectedTarget.effectorType);

                    _transferToTarget.transform.position = _transferFromTarget.transform.position;
                    _transferToTarget.transform.rotation = _transferFromTarget.transform.rotation;
                    _fromTargetCachedLocalPos = _transferToTarget.transform.localPosition;
                    _fromTargetCachedLocalRot = _transferToTarget.transform.localRotation;

                    //Previous interaction was ending
                    if (_oldProgress > 1f)
                    {
                        StopInteraction(effectorType);
                        StartInteraction(effectorType, _transferToTarget, _transferToIntObj);
                        _oldProgress = 2f - _oldProgress;
                        _interactorIK.ChangeIKPartWeight(effectorType, _oldProgress);
                        _eventSent = false;

                        _lastStatus = 3;
                        _lastStatusID = _transferToTarget.gameObject.GetInstanceID();
                    }
                    //Previous interaction was starting
                    else
                    {
                        _interactorIK.ChangeIKPartTarget(effectorType, _transferToTarget, _transferToIntObj);
                        _interactor.Connect(_this, _transferToIntObj, _transferToTarget);
                        _newEvent = true;

                        _lastStatus = 3;
                        _lastStatusID = _transferToTarget.gameObject.GetInstanceID();
                    }

                    _transferFromIntObj.used = false;
                    _transferFromIntObj.RemoveEffectorFromUseables((int)effectorType);
                    _transferToIntObj.RemoveEffectorFromUseables((int)effectorType);

                    _cachedTransfer = true;
                }

                if (_transferProgress > 0.98f)
                {
                    ResetTransfer();
                    return;
                }

                if (_oldProgress <= 1f)
                {
                    if (_newEvent && _transferProgress > 0.8f)
                    {
                        _eventSent = false;
                        _newEvent = false;
                    }
                    _transferProgress = Mathf.Lerp(_transferProgress, 1, Time.deltaTime * 4f);
                    _transferToTarget.transform.localRotation = Quaternion.Lerp(_fromTargetCachedLocalRot, _toTargetCachedLocalRot, _transferProgress);
                    _transferToTarget.transform.localPosition = Vector3.Lerp(_fromTargetCachedLocalPos, _toTargetCachedLocalPos, _transferProgress);
                }
                return;
            }

            private void ResetTransfer()
            {
                _oldProgress = 0;
                _transferProgress = 0;
                _newEvent = false;
                _transferFromIntObj = null;
                _transferFromTarget = null;
                _toTargetChildrenBones = null;
                _cachedTransfer = false;
                _transferring = false;
            }
            #endregion

            //End interactions or send disable calls to object, depending on interaction type.
            public void ObjectExit(InteractorObject exitInteractorObject)
            {
                if (exitInteractorObject == null)
                {
                    if (_interactor.effectors[_this].connectedTarget == null)
                    {
                        StopInteraction(effectorType);
                        _interactor.effectors[_this].connected = false;
                    }
                }

                _eventSent = false;
                if (exitInteractorObject.interactionType == InteractionTypes.DistanceCrosshair || exitInteractorObject.interactionType == InteractionTypes.MultipleCockpit || exitInteractorObject.interactionType == InteractionTypes.SelfItch || exitInteractorObject.interactionType == InteractionTypes.ClimbableLadder || exitInteractorObject.interactionType == InteractionTypes.ManualButton || exitInteractorObject.interactionType == InteractionTypes.DefaultAnimated)
                {
                    _interactor.interactionStates.playerUsable = false;
                    exitInteractorObject.RemoveEffectorFromUseables(-1);
                    exitInteractorObject.used = false;

                    if (exitInteractorObject.interactionType == InteractionTypes.ClimbableLadder && _interactor.vehiclePartsActive)
                    {
                        _interactor.vehiclePartCont.Animate(exitInteractorObject.vehiclePartId, false, _interactor);
                    }
                    return;
                }

                if (exitInteractorObject == _interactor.effectors[_this].connectedTo && _interactor.effectors[_this].connected)
                {
                    StopInteraction(effectorType);
                    DisconnectThis();
                }

                if (exitInteractorObject.interactionType == InteractionTypes.ManualHit)
                {
                    exitInteractorObject.hitObjUseable = false;
                }

                _interactor.interactionStates.playerUsing = false;
                return;
            }

            private void GatherDebugDataStart(InteractorTarget target, InteractorObject intObj, InteractionTypes intType)
            {
#if UNITY_EDITOR
                if (!_interactor.debug) return;

                //Reset GatherDebugDataEnd data
                _lastTargetID = target.gameObject.GetInstanceID();
                _waiting = false;
                _lastReposition = false;
                _targetDistance = 0;
                _tempMinRadius = 0;
                _tempMaxRadius = 0;
                _lastAngleCheck = false;
                _lastObstacle = false;
                _lastObstacleName = "";
                _used = false;
                _lastAddToUseables = false;
                _lastUseables = 0;
                _lastProgress = 0;
                _lastPause = false;

                _lastObstacleEnabled = intObj.obstacleRaycast;
                _interactor.GatherDebugDataStart(_this, target, intObj, intType);
                _updateStarted = true;
#endif
            }

            public void StartInteraction(FullBodyBipedEffector effector, InteractorTarget interactorTarget, InteractorObject interactorObject)
            {
                if (_interactorIK) _interactorIK.StartInteraction(effector, interactorTarget, interactorObject);
                if (interactorObject.interactionType != InteractionTypes.ClimbableLadder)
                    _interactor.NewLookOrder(interactorObject, Look.OnPause);
                if (interactorObject.interactionType != InteractionTypes.SelfItch)
                    _interactor.Connect(_this, interactorObject, interactorTarget);
                _lastStatus = 3; //for debug purposes
                _lastStatusID = interactorTarget.gameObject.GetInstanceID();
            }
            private void ResumeInteraction(FullBodyBipedEffector effector)
            {
                _lastStatus = 4;
                if (_interactor.effectors[_this].connectedTarget)
                    _lastStatusID = _interactor.effectors[_this].connectedTarget.gameObject.GetInstanceID();
                
                _interactorIK.ResumeInteraction(effector);
                if (_interactor.effectors[_this].connectedTo)
                {
                    _interactor.NewLookOrder(_interactor.effectors[_this].connectedTo, Look.After);

                    if (_interactor.effectors[_this].connectedTo.orbitalReach && _interactor.orbitalReach)
                        _interactor.orbitalReach.ResumeOrbitalAnim(_interactor.effectors[_this].connectedTo);
                }
            }
            private void ReverseInteraction(FullBodyBipedEffector effector)
            {
                _lastStatus = 5;
                if (_interactor.effectors[_this].connectedTarget)
                    _lastStatusID = _interactor.effectors[_this].connectedTarget.gameObject.GetInstanceID();

                _interactorIK.ReverseInteraction(effector);
                if (_interactor.effectors[_this].connectedTo)
                {
                    _interactor.NewLookOrder(_interactor.effectors[_this].connectedTo, Look.Never);

                    /*if (_interactor.effectors[_this].connectedTo.orbitalReach && _interactor.orbitalReach)
                        _interactor.orbitalReach.ReverseOrbitalAnim(_interactor.effectors[_this].connectedTo);*/
                }
            }
            private void StopInteraction(FullBodyBipedEffector effector)
            {
                if (_interactor.effectors[_this].connectedTarget)
                    _lastStatusID = _interactor.effectors[_this].connectedTarget.gameObject.GetInstanceID();

                _lastStatus = 6;
                _interactorIK.StopInteraction(effector);
            }
            private void StopAllInteractions()
            {
                for (int i = 0; i < _interactor.effectors.Length; i++)
                {
                    _lastStatus = 6;
                    if (_interactor.effectors[i].connectedTarget)
                        _lastStatusID = _interactor.effectors[i].connectedTarget.gameObject.GetInstanceID();
                }
                _interactorIK.StopAll();
            }

            public bool PrepareOrbitalInteraction(InteractorObject intObj, InteractorTarget currentTarget)
            {
                if (!_interactor.orbitalReach)
                {
                    Debug.LogWarning("There is no OrbitalReach component on Interactor gameObject.", _interactor);
                    return false;
                }

                bool mirror = effectorType == FullBodyBipedEffector.LeftHand;
                if (!mirror && effectorType != FullBodyBipedEffector.RightHand)
                {
                    Debug.LogWarning("OrbitalReach is only for Left/Right Hand targets.", currentTarget);
                    ResetInteractorObject(intObj);
                    return false;
                }

                bool success = _interactor.orbitalReach.SetOrbitalReach(intObj, currentTarget, CalcOrbital(currentTarget.transform), mirror, _this, false);
                if (!success)
                {
                    ResetInteractorObject(intObj);
                    return false;
                }

                _interactor.Connect(_this, intObj, currentTarget);
                return true;
            }
            private void ResetInteractorObject(InteractorObject intObj)
            {
                intObj.used = false;
                intObj.RemoveEffectorFromUseables((int)effectorType);
                intObj.Prepare(true);
            }
            private bool PrepareEndingOrbitalInteraction(InteractorObject intObj, InteractorTarget currentTarget)
            {
                if (!_interactor.orbitalReach)
                {
                    Debug.LogWarning("There is no OrbitalReach component on Interactor gameObject.", _interactor);
                    return false;
                }

                bool mirror = effectorType == FullBodyBipedEffector.LeftHand;
                if (!mirror && effectorType != FullBodyBipedEffector.RightHand)
                {
                    Debug.LogWarning("OrbitalReach is only for Left/Right Hand targets.", currentTarget);
                    return false;
                }

                if (!intObj.dropLocation) return false;

                bool success = _interactor.orbitalReach.SetOrbitalReach(intObj, currentTarget, CalcOrbital(intObj.dropLocation), mirror, _this, true);
                if (!success) return false;

                return true;
            }

            //This is the main loop for all effectors to check all interaction objects in sphere trigger. 
            //If an interaction possible with given effector specs(depends on interaction type), 
            //it calls methods on object, changes some bools on PlayerState or etc to tell interaction is possible.
            //Also this loops ends interactions when object leaves the sphere area or ends automatically.
            public void Update(List<IntObjComponents> interactionObjsInRange, Vector3 sphereColWithRotScale)
            {
                //This is for debug, used by InteractorEditor
                targetActive = false;

                if (!_initiated) return;
                if (!enabled) return;

                if (_interactor.effectors[_this].connected)
                {
                    if (!_interactor.effectors[_this].connectedTo || !_interactor.effectors[_this].connectedTarget || !_interactor.effectors[_this].connectedTarget.enabled || !_interactor.effectors[_this].connectedTarget.gameObject.activeInHierarchy || !_interactor.effectors[_this].connectedTo.enabled || !_interactor.effectors[_this].connectedTo.gameObject.activeInHierarchy)
                    {
                        if (_interactor.effectors[_this].connectedTo)
                            _interactor.effectors[_this].connectedTo.used = false;
                        _eventSent = false;
                        _interactor.Disconnect(_this);
                    }
                }

                _sphereColWithRotScale = sphereColWithRotScale;
                //This is effectors world space position.
                effectorWorldSpace = _playerTransform.position + _sphereColWithRotScale + ((_playerTransform.right * posOffset.x) + (_playerTransform.forward * posOffset.z) + (_playerTransform.up * posOffset.y));

                if (_transferring)
                {
                    InterruptTransfer();
                }

                //Main loop for every interaction objects in sphere area
                for (int objPlaceInList = 0; objPlaceInList < interactionObjsInRange.Count; objPlaceInList++)
                {
                    _interactorObject = interactionObjsInRange[objPlaceInList].interactorObject;
                    if (_interactorObject == null || !_interactorObject.enabled || !_interactorObject.gameObject.activeInHierarchy)
                        continue;

                    if (_interactorObject.currentInteractor != _interactor)
                        _interactorObject.AssignInteractor(_interactor);

                    _allTargets = _interactorObject.childTargets;
                    _interactor.checkOncePerObject = false;

                    //Draws debug lines for all targets of all interaction objects in sphere area
                    DrawDebugLines(_allTargets);

                    switch (_interactorObject.interactionType)
                    {
                        case InteractionTypes.DefaultAnimated:
                            {//Default (int)10-20
                                //No need to be checked by more than one effector
                                if (_interactor.checkOncePerObject) continue;
                                _interactor.checkOncePerObject = true;

                                //Instead of calculating expensive root operation we can use square ones.
                                if (interactionObjsInRange[objPlaceInList].distanceSqr <= _interactorObject.defaultSettings.defaultAnimatedDistance * _interactorObject.defaultSettings.defaultAnimatedDistance)
                                {
                                    //It will check if InteractorObject is blocked. Useful for doors below or above the level.
                                    if (HasObstacle(_interactorObject, _interactor._playerCenter, interactionObjsInRange[objPlaceInList].distanceSqr)) continue;

                                    if (!_interactorObject.used)
                                    {
                                        _interactorObject.AddEffectorToUseables(0);
                                        //This sends Vehicle(VehiclePartControls) its cached id and sets its 
                                        //animation on, if it has parameters in Vehicle Animator with same name.
                                        if (_interactor.vehiclePartsActive)
                                        {
                                            _interactor.vehiclePartCont.Animate(_interactorObject.vehiclePartId, true, _interactor);
                                        }
                                        //Start its events if there are any
                                        if (!_eventSent)
                                        {
                                            _interactorObject.SendUnityEvent();
                                            _eventSent = true;
                                        }
                                        //Start looking at this object when used if LookAtTarget enabled.
                                        _interactor.NewLookOrder(_interactorObject, Look.OnPause);
                                        _interactorObject.used = true;
                                    }
                                }
                                else if (_interactorObject.used)
                                {
                                    _interactorObject.used = false;
                                    _eventSent = false;
                                    _interactor.NewLookOrder(_interactorObject, Look.Never);
                                }
                                break;
                            }
                        case InteractionTypes.ManualButton:
                            {//Manual (int)20-30
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.ManualButton);
                                    if (_interactorObject != _interactor.effectors[_this].connectedTo)
                                    {
                                        bool passed = !_interactorObject.used; //if target already used by another effector
                                        if (passed) passed = EffectorCheckOrbital(_allTargets[i], _interactorObject); //Target is in good position for this effector?
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform); //Obstacle check enabled and there is no obstacles?

                                        if (passed)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            _selfPossible = false;
                                        }
                                        else
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (_interactorObject.UseableEffectorCount() > 0) continue;
                                            //It is still useable since other effectors have useability
                                            _interactor.interactionStates.playerUsable = false;
                                        }
                                    }
                                    else
                                    {
                                        //If interaction have pause enabled, set object on so we can interact to resume again.
                                        if (_interactorIK.IsPaused(effectorType))
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                        //If IK animation is on half way which means effector bone 
                                        //is in target position, fire its events if there are any.
                                        if (_interactorIK.GetProgress(effectorType) > 0.98f && !_eventSent)
                                        {//0 to 1f target path, 1f is target, 1f to 2f back path
                                            _interactorObject.SendUnityEvent();
                                            _eventSent = true;
                                            _interactor.NewLookOrder(_interactorObject, Look.After);
                                        }
                                        
                                        //Interaction anim is almost done, 
                                        //which means effector bone is back in deault position, end interaction.
                                        if (_interactorIK.GetProgress(effectorType) > 1.9f || (_interactorIK.GetProgress(effectorType) == 0 && _eventSent))
                                        {
                                            _interactor.interactionStates.playerUsable = true;
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            _interactorObject.used = false;
                                            _eventSent = false;
                                            _interactor.NewLookOrder(_interactorObject, Look.Never);
                                            _interactor.Disconnect(_this);
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.ManualSwitch:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.ManualSwitch);
                                    if (_interactorObject != _interactor.effectors[_this].connectedTo)
                                    {
                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheckOrbital(_allTargets[i], _interactorObject);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            _selfPossible = false;
                                        }
                                        else
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (_interactorObject.UseableEffectorCount() > 0) continue;

                                            _interactor.interactionStates.playerUsable = false;
                                        }
                                    }
                                    else
                                    {
                                        //If target is not in position anymore end interaction.
                                        if (!EffectorCheckOrbital(_allTargets[i], _interactorObject))
                                        {
                                            DisconnectThis(); 
                                            _interactor.RemoveLookTargets(_interactorObject);
                                        }
                                        //Interaction is paused so effector is on object now
                                        if (_interactorIK.IsPaused(effectorType))
                                        {
                                            if (!_eventSent)
                                            {
                                                _interactorObject.SendUnityEvent();
                                                _eventSent = true;
                                            }
                                            //InteractorObject has Automover?
                                            if (_interactorObject.hasAutomover)
                                            {
                                                //If Automover didnt start yet, start
                                                if (!_interactorObject.autoMovers[i].started)
                                                {
                                                    _interactorObject.autoMovers[i].StartMovement();
                                                }
                                                //Automover started and came half in movement(animation in this case)
                                                //If so, run click
                                                else if (_interactorObject.autoMovers[i].half)
                                                {
                                                    //Check if InteractorObject has cached InteractiveSwitches
                                                    if (_interactorObject.hasInteractiveSwitch)
                                                    {
                                                        _interactorObject.interactiveSwitches[i].Click();
                                                        _interactorObject.autoMovers[i].half = false;
                                                    }
                                                    else
                                                    {
                                                        Debug.LogWarning(_interactorObject.name + " has no InteractiveSwitch script on targets.", _interactorObject);
                                                    }
                                                }
                                                //If Automover ended, end interaction
                                                else if (_interactorObject.autoMovers[i].ended)
                                                {
                                                    ResumeInteraction(effectorType);
                                                    _interactorObject.autoMovers[i].ended = false;
                                                    _interactorObject.SendUnityEndEvent();
                                                }
                                            }
                                            else
                                            {
                                                if (_interactorObject.hasInteractiveSwitch)
                                                {
                                                    _interactorObject.interactiveSwitches[i].Click();
                                                }
                                                else
                                                {
                                                    Debug.LogWarning(_interactorObject.name + " has no InteractiveSwitch script on targets.", _interactorObject);
                                                }
                                            }
                                        }
                                        //If all done, deactivate and reset
                                        else if (_interactorIK.GetProgress(effectorType) > 1.9f || (_interactorIK.GetProgress(effectorType) == 0 && _eventSent))
                                        {
                                            _eventSent = false;
                                            _interactor.interactionStates.playerUsable = true;
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            _interactorObject.used = false;
                                            if (_interactorObject.hasAutomover)
                                            {
                                                _interactorObject.autoMovers[i].ResetBools();
                                            }
                                            _interactor.NewLookOrder(_interactorObject, Look.Never);
                                            _interactor.Disconnect(_this);
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.ManualRotator:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.ManualRotator);
                                    if (_interactorObject != _interactor.effectors[_this].connectedTo)
                                    {
                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheckOrbital(_allTargets[i], _interactorObject);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            _selfPossible = false;
                                        }
                                        else
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (_interactorObject.UseableEffectorCount() > 0) continue;

                                            _interactor.interactionStates.playerUsable = false;
                                        }
                                    }
                                    else
                                    {
                                        if (!EffectorCheckOrbital(_allTargets[i], _interactorObject))
                                        {
                                            DisconnectThis();
                                            continue;
                                        }

                                        if (_interactorIK.IsPaused(effectorType))
                                        {
                                            if (!_eventSent)
                                            {
                                                _interactorObject.SendUnityEvent();
                                                _eventSent = true;
                                            }

                                            if (_interactorObject.hasAutomover)
                                            {
                                                if (!_interactorObject.autoMovers[i].started)
                                                {
                                                    _interactorObject.autoMovers[i].started = true;
                                                    if (_interactorObject.hasInteractiveRotator)
                                                    {
                                                        _interactorObject.interactiveRotators[i].active = true;
                                                    }
                                                    else
                                                    {
                                                        Debug.LogWarning(_interactorObject.name + " has no InteractiveRotator script on targets.", _interactorObject);
                                                    }
                                                }

                                                if (!_interactorObject.used)
                                                {
                                                    _interactorObject.interactiveRotators[i].active = false;
                                                    ResumeInteraction(effectorType);
                                                }
                                            }
                                            else
                                            {
                                                if (_interactorObject.hasInteractiveRotator)
                                                {
                                                    _interactorObject.interactiveRotators[i].active = true;
                                                }
                                                else
                                                {
                                                    Debug.LogWarning(_interactorObject.name + " has no InteractiveRotator script on targets.", _interactorObject);
                                                }

                                                if (!_interactorObject.used)
                                                {
                                                    _interactorObject.interactiveRotators[i].active = false;
                                                    ResumeInteraction(effectorType);
                                                }
                                            }
                                        }
                                        else if (_interactorIK.GetProgress(effectorType) > 1.9f || (_interactorIK.GetProgress(effectorType) == 0 && _eventSent))
                                        {
                                            _eventSent = false;
                                            _interactor.interactionStates.playerUsable = true;
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (_interactorObject.hasAutomover)
                                            {
                                                _interactorObject.autoMovers[i].ResetBools();
                                            }
                                            _interactor.NewLookOrder(_interactorObject, Look.Never);
                                            //Send interacted object to camera for unlocking Y rotation
                                            //Because ManualRotator no longer needs that.
                                            FreeLookCam.LockCamY(_interactorObject.gameObject);
                                            _interactor.Disconnect(_this);
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.ManualHit:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    //Hit is LeftHand only, yet.
                                    if (effectorType == FullBodyBipedEffector.LeftHand && (int)_allTargets[i].effectorType == (int)FullBodyBipedEffector.LeftHand && !_interactorObject.used)
                                    {
                                        //If hit object is in sphere area, set it useable which turns it to player each frame
                                        //This will move target accordingly and it can check its position
                                        _interactorObject.hitObjUseable = true;
                                        _interactorObject.Hit(_allTargets[i].transform, effectorWorldSpace, _interactor);
                                    }

                                    if (_interactor.effectors[_this].connectedTo != _interactorObject)
                                    {
                                        if (EffectorCheck(_allTargets[i]))
                                        {
                                            if (HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform)) continue;

                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            //Stop rotating object, target is in position
                                            _interactorObject.hitObjUseable = false;
                                            if (_interactorObject.used) continue;

                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            _selfPossible = false;
                                        }
                                        else
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (_interactorObject.UseableEffectorCount() > 0) continue;

                                            _interactor.interactionStates.playerUsable = false;
                                        }
                                    }
                                    else
                                    {
                                        _interactorObject.hitObjUseable = false;
                                        //Hit now since input changed usedBy
                                        //It will repos target
                                        _interactorObject.Hit(_allTargets[i].transform, effectorWorldSpace, _interactor);

                                        if (_interactorIK.IsPaused(effectorType))
                                        {
                                            //Effector is on repositioned target, now send back so it will look hit anim
                                            _interactorObject.HitPosDefault(_allTargets[i].transform, effectorWorldSpace);
                                            //If hit done, end interaction
                                            if (_interactorObject.hitDone)
                                            {
                                                //Since its hit moment, send events now
                                                _interactorObject.SendUnityEvent();
                                                ResumeInteraction(effectorType);
                                            }
                                        }
                                        //If interaction almost ended, reset all.
                                        if (_interactorIK.GetProgress(effectorType) > 1.9f)
                                        {
                                            _interactor.interactionStates.playerUsable = true;
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            _interactorObject.used = false;
                                            _interactor.NewLookOrder(_interactorObject, Look.Never);
                                            _interactor.Disconnect(_this);
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.ManualForce:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    //This is a basic interaction example, it will push the objects, not cached (rigidbodies).
                                    //No connection here, but if effector connected it won't effect cubes.
                                    if (effectorType == FullBodyBipedEffector.LeftHand || effectorType == FullBodyBipedEffector.RightHand)
                                    {
                                        if (!_interactor.effectors[_this].connected)
                                        {
                                            if (EffectorCheck(_allTargets[i]))
                                            {
                                                if (HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform)) continue;

                                                _interactorObject.AddEffectorToUseables((int)effectorType);
                                                Rigidbody anyRigid = _interactorObject.GetComponent<Rigidbody>();
                                                if (!anyRigid) anyRigid = _allTargets[i].GetComponent<Rigidbody>();

                                                if (anyRigid)
                                                {
                                                    Vector3 force = (_allTargets[i].transform.position - _interactorIK.GetBone(effectorType).position);
                                                    float magnitude = maxRadius / force.magnitude;
                                                    anyRigid.AddForce(force * magnitude, ForceMode.Impulse);

                                                    Debug.DrawLine(anyRigid.transform.position, effectorWorldSpace, Color.red, 3f);
                                                }
                                            }
                                        }
                                    }
                                    //This is for ProtoTruck example and it's using extra effector types which has no other use case yet.
                                    //LeftThigh and RightThigh are used for Turrets.
                                    else if (effectorType == FullBodyBipedEffector.LeftThigh || effectorType == FullBodyBipedEffector.RightThigh)
                                    {
                                        if (!_interactor.effectors[_this].connected)
                                        {
                                            if (EffectorCheck(_allTargets[i]))
                                            {
                                                if (_interactor.childTurrets.Length != 0)
                                                {
                                                    for (int k = 0; k < _interactor.childTurrets.Length; k++)
                                                    {
                                                        if ((int)_interactor.childTurrets[k].effector == (int)effectorType)
                                                        {
                                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                                            _interactor.childTurrets[k].Attack(_allTargets[i].transform);
                                                            _interactor.Connect(_this, _interactorObject, _allTargets[i]);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (_interactorObject == _interactor.effectors[_this].connectedTo)
                                        {
                                            if (!EffectorCheck(_allTargets[i]))
                                            {
                                                _interactor.NewLookOrder(_interactorObject, Look.Never);
                                                _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                                _interactor.Disconnect(_this);
                                            }
                                            else if (_allTargets[i] == _interactor.effectors[_this].connectedTarget)
                                            {
                                                DrawDebugLines(_allTargets[i], objPlaceInList);
                                            }
                                        }
                                    }
                                    //This is for ProtoTruck back door example. If any object is in position, it will block the door so it wont work until InteractorObjects are out of position.
                                    else if (effectorType == FullBodyBipedEffector.RightShoulder)
                                    {
                                        if (!_interactor.effectors[_this].connected)
                                        {
                                            if (EffectorCheck(_allTargets[i]))
                                            {
                                                _interactor.vehicleInput.Blocked(true);
                                                Debug.DrawLine(_allTargets[i].transform.position, effectorWorldSpace, Color.red, 3f);
                                                _interactorObject.AddEffectorToUseables((int)effectorType);
                                                _interactor.Connect(_this, _interactorObject, _allTargets[i]);
                                            }
                                        }
                                        else if (_interactorObject == _interactor.effectors[_this].connectedTo)
                                        {
                                            if (!EffectorCheck(_allTargets[i]) && _allTargets[i] == _interactor.effectors[_this].connectedTarget)
                                            {
                                                _interactor.vehicleInput.Blocked(false);
                                                _interactor.NewLookOrder(_interactorObject, Look.After);
                                                _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                                _interactor.Disconnect(_this);
                                            }
                                            else if (_allTargets[i] == _interactor.effectors[_this].connectedTarget)
                                            {
                                                DrawDebugLines(_allTargets[i], objPlaceInList);
                                            }
                                        }
                                    }
                                    //This is for ProtoTruck windshield example. If any InteractorObject is in position, shield anim will run.
                                    else if (effectorType == FullBodyBipedEffector.LeftShoulder)
                                    {
                                        if (!_interactor.effectors[_this].connected)
                                        {
                                            if (EffectorCheck(_allTargets[i]))
                                            {
                                                DrawDebugLines(_allTargets[i], objPlaceInList);
                                                _interactor.vehicleInput.SetWindshield(true);
                                                Debug.DrawLine(_allTargets[i].transform.position, effectorWorldSpace, Color.red, 3f);
                                                _interactorObject.AddEffectorToUseables((int)effectorType);
                                                _interactor.Connect(_this, _interactorObject, _allTargets[i]);
                                            }
                                        }
                                        else if (_interactorObject == _interactor.effectors[_this].connectedTo)
                                        {
                                            if (!EffectorCheck(_allTargets[i]) && _allTargets[i] == _interactor.effectors[_this].connectedTarget)
                                            {
                                                _interactor.vehicleInput.SetWindshield(false);
                                                _interactor.NewLookOrder(_interactorObject, Look.After);
                                                _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                                _interactor.Disconnect(_this);
                                            }
                                            else if (_allTargets[i] == _interactor.effectors[_this].connectedTarget)
                                            {
                                                DrawDebugLines(_allTargets[i], objPlaceInList);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.TouchVertical:
                            {//Touch (int)30-40
                                //Touch interaction is automatic, no need for StartStopInteractionThis()
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    bool leftHand;
                                    if (effectorType == FullBodyBipedEffector.LeftHand)
                                        leftHand = true;
                                    else if (effectorType == FullBodyBipedEffector.RightHand)
                                        leftHand = false;
                                    else continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.TouchVertical);
                                    if (!_interactor.effectors[_this].connected)
                                    {
                                        _lastReposition = _interactorObject.RaycastVertical(_playerTransform, out hit, leftHand);
                                        if (!_lastReposition) continue;
                                        _interactorObject.touchSettings.ReposForVertical(effectorWorldSpace, hit, _allTargets[i].transform, leftHand);

                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheck(_allTargets[i]);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            //To prevent interaction starting stutter(start stop, start stop)
                                            //wait a cooldown to start interaction
                                            if (_interactorObject.touchSettings.touchVCooldown > 0)
                                                _interactorObject.touchSettings.touchVCooldown -= Time.fixedDeltaTime;
                                            else
                                            {
                                                //Reset cooldown timers to their default time
                                                _interactorObject.touchSettings.ResetTouchCooldowns();
                                                StartInteraction(_allTargets[i].effectorType, _allTargets[i], _interactorObject);
                                                _interactorObject.used = true;
                                            }
                                        }
                                    }
                                    else if (_interactorObject == _interactor.effectors[_this].connectedTo)
                                    {
                                        //Continue to raycast so target will move on surface on every frame
                                        _lastReposition = _interactorObject.RaycastVertical(_playerTransform, out hit, leftHand);
                                        if (_lastReposition)
                                        {
                                            _interactorObject.touchSettings.ReposForVertical(effectorWorldSpace, hit, _allTargets[i].transform, leftHand);

                                            if (!EffectorCheck(_allTargets[i]))
                                            {
                                                _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                                if (!_interactorIK.IsPaused(_allTargets[i].effectorType))
                                                {
                                                    ReverseInteraction(effectorType);
                                                }
                                                else
                                                {
                                                    ResumeInteraction(effectorType);
                                                }
                                                _eventSent = false;
                                                _interactor.Disconnect(_this);
                                                _interactorObject.used = false;
                                                break;
                                            }
                                            else
                                            {
                                                if (_interactorIK.IsPaused(_allTargets[i].effectorType))
                                                {
                                                    if (!_eventSent)
                                                    {
                                                        _interactorObject.SendUnityEvent();
                                                        _eventSent = true;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (!_interactorIK.IsPaused(_allTargets[i].effectorType))
                                            {
                                                ReverseInteraction(effectorType);
                                            }
                                            else
                                            {
                                                ResumeInteraction(effectorType);
                                            }
                                            _eventSent = false;
                                            _interactor.Disconnect(_this);
                                            _interactorObject.used = false;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.TouchHorizontalUp:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;
                                    //If target is below the player, pass.
                                    if (_allTargets[i].transform.position.y < _playerTransform.position.y) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.TouchHorizontalUp);
                                    if (!_interactor.effectors[_this].connected)
                                    {
                                        _lastReposition = _interactorObject.RaycastHorizontalUp(_playerTransform, out hit);
                                        if (!_lastReposition) continue;
                                        _interactorObject.touchSettings.ReposForHorizontalUp(_playerTransform, hit, _allTargets[i].transform);

                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheck(_allTargets[i]);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            if (_interactorObject.touchSettings.touchHCooldown > 0)
                                                _interactorObject.touchSettings.touchHCooldown -= Time.fixedDeltaTime;
                                            else
                                            {
                                                _interactorObject.touchSettings.ResetTouchCooldowns();
                                                StartInteraction(_allTargets[i].effectorType, _allTargets[i], _interactorObject);
                                                _interactorObject.used = true;
                                            }
                                        }
                                    }
                                    else if (_interactorObject == _interactor.effectors[_this].connectedTo)
                                    {
                                        _lastReposition = _interactorObject.RaycastHorizontalUp(_playerTransform, out hit);
                                        if (_lastReposition)
                                        {
                                            _interactorObject.touchSettings.ReposForHorizontalUp(_playerTransform, hit, _allTargets[i].transform);

                                            if (!EffectorCheck(_allTargets[i]))
                                            {
                                                _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                                if (!_interactorIK.IsPaused(_allTargets[i].effectorType))
                                                {
                                                    ReverseInteraction(effectorType);
                                                }
                                                else
                                                {
                                                    ResumeInteraction(effectorType);
                                                }
                                                _eventSent = false;
                                                _interactor.Disconnect(_this);
                                                _interactorObject.used = false;
                                                break;
                                            }
                                            else
                                            {
                                                if (_interactorIK.IsPaused(_allTargets[i].effectorType))
                                                {
                                                    if (!_eventSent)
                                                    {
                                                        _interactorObject.SendUnityEvent();
                                                        _eventSent = true;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            if (!_interactorIK.IsPaused(_allTargets[i].effectorType))
                                            {
                                                ReverseInteraction(effectorType);
                                            }
                                            else
                                            {
                                                ResumeInteraction(effectorType);
                                            }
                                            _eventSent = false;
                                            _interactor.Disconnect(_this);
                                            _interactorObject.used = false;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.TouchHorizontalDown:
                            {
                                //TODO
                                break;
                            }
                        case InteractionTypes.TouchStill:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.TouchStill);
                                    if (!_interactor.effectors[_this].connected)
                                    {
                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheck(_allTargets[i]);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            RotatePivot(_interactorObject);
                                            StartInteraction(_allTargets[i].effectorType, _allTargets[i], _interactorObject);
                                            _interactorObject.used = true;
                                        }
                                    }
                                    else if (_allTargets[i] == _interactor.effectors[_this].connectedTarget && _interactorObject == _interactor.effectors[_this].connectedTo)
                                    {
                                        if (!EffectorCheck(_allTargets[i]))
                                        {
                                            _interactorObject.used = false;
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            //Resume if interaction reached to target, reverse if not.
                                            if (_interactorIK.IsPaused(_allTargets[i].effectorType))
                                            {
                                                ResumeInteraction(effectorType);
                                            }
                                            else
                                            {
                                                ReverseInteraction(effectorType);
                                                _interactor.RemoveLookTargets(_interactorObject);
                                            }
                                            _eventSent = false;
                                            _interactor.Disconnect(_this);
                                            break;
                                        }
                                        else
                                        {
                                            if (_interactorIK.IsPaused(_allTargets[i].effectorType))
                                            {
                                                if (!_eventSent)
                                                {
                                                    _interactorObject.SendUnityEvent();
                                                    _eventSent = true;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.DistanceCrosshair:
                            {//Distance (int)40-50
                                if (_interactor.checkOncePerObject) continue;
                                _interactor.checkOncePerObject = true;

                                _interactorObject.AddEffectorToUseables((int)effectorType);
                                _selfPossible = false;
                                _interactor.interactionStates.playerUsable = true;
                                break;
                            }
                        case InteractionTypes.ClimbableLadder:
                            {//Climbable (int)50-60
                                //Get this effectors closest target
                                int closest = ClosestTargetSameEffector(_allTargets);
                                if (closest == -1) continue;

                                _closestTarget = _allTargets[closest];
                                if (!_closestTarget.gameObject.activeInHierarchy || !_closestTarget.enabled) continue;

                                GatherDebugDataStart(_closestTarget, _interactorObject, InteractionTypes.ClimbableLadder);
                                //All except LeftHand. LeftHand is starter here and its below this if().
                                if (effectorType != FullBodyBipedEffector.LeftHand)
                                {
                                    if (!_interactor.effectors[_this].connected)
                                    {
                                        //usedBy on InteractorObject gets true when player uses input to start interaction by Left Hand. This is for other hand and both feet.
                                        if (_interactorObject.used && !_interactor.interactionStates.playerGrounded)
                                        {
                                            float progress = _interactorIK.GetProgress(_closestTarget.effectorType);
                                            //If this effector didnt start any interaction or almost finished(which happens when its previous target is out of position)
                                            //If closestTarget is in position, start with that target.
                                            bool passed = (progress > 1.9f) || (progress == 0);
                                            if (passed) passed = EffectorCheck(_closestTarget);
                                            if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _closestTarget.transform);

                                            if (passed)
                                            {
                                                _interactorObject.AddEffectorToUseables((int)effectorType);
                                                StartInteraction(_closestTarget.effectorType, _closestTarget, _interactorObject);
                                            }
                                        }
                                    }
                                    else if (_interactor.effectors[_this].connectedTo == _interactorObject)
                                    {
                                        //usedBy set false by LeftHand when interaction ended by user prematurely.
                                        if (!_interactorObject.used || _interactor.interactionStates.playerGrounded)
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            ReverseInteraction(effectorType);
                                            _interactor.Disconnect(_this);
                                            continue;
                                        }
                                        //If closest target isnt current target, resume interaction so next loops catch when anim is over 0.9 and switches to it.
                                        if (_closestTarget != _interactor.effectors[_this].connectedTarget)
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            ResumeInteraction(effectorType);
                                            _interactor.Disconnect(_this);
                                            continue;
                                        }
                                        //When current target is out of position, continue anim (end interaction).
                                        if (!EffectorCheck(_interactor.effectors[_this].connectedTarget) || HasObstacleForTarget(_interactorObject, effectorWorldSpace, _closestTarget.transform))
                                        {
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            ResumeInteraction(effectorType);
                                            _interactor.Disconnect(_this);
                                        }
                                    }
                                    continue;
                                }

                                //Codes below are LeftHand Only
                                if (!_interactor.effectors[_this].connected)
                                {
                                    float progress = _interactorIK.GetProgress(_closestTarget.effectorType);
                                    bool passed = (progress > 1.9f) || (progress == 0);
                                    if (passed) passed = EffectorCheck(_closestTarget);
                                    if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _closestTarget.transform);

                                    if (passed)
                                    {
                                        DrawDebugLines(_closestTarget, objPlaceInList);
                                        _interactor.interactionStates.playerClimable = true;
                                        _interactorObject.AddEffectorToUseables((int)effectorType);
                                        if (_interactor.vehiclePartsActive)
                                        {//Fire ladder animation via VehiclePartController
                                            _interactor.vehiclePartCont.Animate(_interactorObject.vehiclePartId, true, _interactor);
                                        }
                                        StartInteraction(_closestTarget.effectorType, _closestTarget, _interactorObject);
                                        _interactor.NewLookOrder(_interactorObject, Look.OnPause);
                                    }
                                }
                                else if (_interactor.effectors[_this].connectedTo == _interactorObject)
                                {
                                    if (_closestTarget != _interactor.effectors[_this].connectedTarget)
                                    {
                                        _interactor.interactionStates.playerClimable = false;
                                        _interactorObject.RemoveEffectorFromUseables((int)effectorType);

                                        if (_interactorObject.used)
                                        {
                                            ResumeInteraction(effectorType);
                                        }
                                        else
                                        {
                                            ReverseInteraction(effectorType);
                                        }
                                        _interactor.Disconnect(_this);
                                        continue;
                                    }

                                    if (!EffectorCheck(_interactor.effectors[_this].connectedTarget) || HasObstacleForTarget(_interactorObject, effectorWorldSpace, _closestTarget.transform))
                                    {
                                        _interactor.interactionStates.playerClimable = false;
                                        _interactorObject.RemoveEffectorFromUseables((int)effectorType);

                                        if (_interactorIK.IsPaused(_interactor.effectors[_this].connectedTarget.effectorType))
                                        {
                                            ResumeInteraction(effectorType);
                                        }
                                        else
                                        {
                                            ReverseInteraction(effectorType);
                                        }
                                        _interactor.Disconnect(_this);
                                    }
                                }

                                if (!_interactor.interactionStates.playerClimbing && _interactor.interactionStates.playerGrounded)
                                {
                                    ResumeInteraction(FullBodyBipedEffector.LeftFoot);
                                    ResumeInteraction(FullBodyBipedEffector.RightFoot);
                                }
                                break;
                            }
                        case InteractionTypes.MultipleCockpit:
                            {//Multiple (int)60-70
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.MultipleCockpit);
                                    if ((int)_allTargets[i].effectorType != (int)FullBodyBipedEffector.Body)
                                    {
                                        _waiting = true; //for debug
                                        continue; //Only Body will pass as checker/starter
                                    }

                                    bool passed = !_interactorObject.used;
                                    if (_interactor.effectors[_this].connectedTo == _interactorObject)
                                        passed = true; //Because on newer Unity versions OnTriggerExits set used false.
                                    else
                                    {
                                        if (passed) passed = EffectorCheckOrbital(_allTargets[i], _interactorObject);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);
                                    }

                                    if (passed)
                                    {
                                        DrawDebugLines(_allTargets[i], objPlaceInList);
                                        //Set InteractorObject on
                                        _interactorObject.AddEffectorToUseables((int)effectorType);
                                        _selfPossible = false;
                                        //Set the vehicle GameObject to use
                                        _interactor.interactionStates.enteredVehicle = _interactorObject.transform.root.gameObject;
                                        //Set the PlayerState that player can change, so now BasicInput waits for input. 
                                        //And when pressed, it will send PlayerController where it is actually changes.
                                        _interactor.interactionStates.playerChangable = true;
                                    }
                                    else
                                    {
                                        //Deactivate object glow because Body effector is out of position.
                                        _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                        _interactor.interactionStates.playerChangable = false;
                                        _interactor.NewLookOrder(_interactorObject, Look.Never);
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.SelfItch:
                            {//Self (int)70-80
                                //All targets for this effector, will randomly (with given odds) test their luck and if player is idle only one of them will run
                                if (_allTargets.Length <= 0) continue;

                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    if (!_interactor.effectors[_this].connected && _selfPossible && !_interactorObject.selfSettings.selfActive)
                                    {
                                        if (_interactorObject.selfSettings.CheckOdds(i))
                                        {
                                            if (_interactor.interactionStates.playerIdle && !_interactorIK.IsInInteraction(_allTargets[i].effectorType))
                                            {
                                                _interactorObject.AddEffectorToUseables((int)effectorType);
                                                _eventSent = false;
                                                _interactorObject.selfSettings.pathMovers[i].StartMove(_interactorObject.targetDuration);
                                                StartInteraction(_allTargets[i].effectorType, _allTargets[i], _interactorObject);
                                                _interactor._selfActiveTarget = _allTargets[i];
                                                _interactorObject.selfSettings.selfActive = true;
                                            }
                                        }
                                    }
                                    else if (_interactorObject.selfSettings.selfActive && _allTargets[i] == _interactor._selfActiveTarget)
                                    {
                                        if (!_interactor.interactionStates.playerIdle || !_interactorObject.selfSettings.pathMovers[i].playing)
                                        {
                                            _interactorObject.selfSettings.selfActive = false;
                                            if (_interactorIK.IsPaused(_allTargets[i].effectorType))
                                            {
                                                if (!_eventSent)
                                                {
                                                    _interactorObject.SendUnityEvent();
                                                    _eventSent = true;
                                                }
                                                ResumeInteraction(effectorType);
                                            }
                                            else
                                            {
                                                ReverseInteraction(effectorType);
                                            }
                                            _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                        }
                                    }
                                }

                                //Since self interaction is always on first place at interactionObjsInRange list, it will be
                                //disabled if previous frame set it false (which means other interactions are possible).
                                //We set it true only here, so if this frame nothing sets it false, its possible to self interact next frame.
                                if (_interactor.selfInteractionEnabled) _selfPossible = true;

                                break;
                            }
                        case InteractionTypes.PickableOne:
                            {//Pickables (int)80-90
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.PickableOne);

                                    if (_interactorObject != _interactor.effectors[_this].connectedTo)
                                    {
                                        bool passed = !_interactorObject.used;

                                        bool skipChecks = false;
                                        if (passed && _interactorObject.itemForInventory)
                                        {//Item is in current inv
                                            if (_interactorObject.itemForInventory.currentInventory == _interactor.interactorPoints.inventory) skipChecks = true;
                                        }
                                        if (passed && !skipChecks)
                                        {//Item is on body
                                            int bodyPoint = _interactorObject.pickableOneSettings.bodyPoint;
                                            if (bodyPoint < _interactor.interactorPoints.onBodyPoints.Count && _interactor.interactorPoints.onBodyPoints[bodyPoint].currentObject == _interactorObject.transform) skipChecks = true;
                                        }


                                        if (passed && _interactorObject.orbitalReach) passed = EffectorCheckOrbital(_allTargets[i], _interactorObject);
                                        if (passed && !_interactorObject.orbitalReach) passed = EffectorCheckXZ(_allTargets[i]);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed || skipChecks)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            //Tells InteractorObject that this effector can pick now.
                                            _interactorObject.AddEffectorToUseables((int)effectorType);
                                            _selfPossible = false;
                                        }
                                        else _interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                    }
                                    else
                                    {
                                        if (_interactorIK.IsPaused(effectorType))
                                        {
                                            if (_interactorObject.pickableOneSettings.progressDone && _interactorObject.pickableOneSettings.dropping)
                                            {
                                                _interactorObject.pickableOneSettings.Reset();
                                                _interactorIK.ResumeInteraction(effectorType);
                                                _interactor.Disconnect(_this);
                                                ResetInteractorObject(_interactorObject);
                                                _interactor.NewLookOrder(_interactorObject, Look.After);
                                                continue;
                                            }

                                            if (!_interactorObject.pickableOneSettings.oneHandPicked && !_interactorObject.pickableOneSettings.dropping)
                                            {
                                                if (!_interactorObject.pickableOneSettings.Pick(_interactorIK, (int)effectorType))
                                                {
                                                    _interactorIK.ResumeInteraction(effectorType);
                                                    _interactor.Disconnect(_this);
                                                    ResetInteractorObject(_interactorObject);
                                                    if (_interactor.orbitalReach && _interactorObject.orbitalReach) _interactor.orbitalReach.ResumeOrbitalAnim(_interactorObject);
                                                    continue;
                                                }
                                                _interactor.NewLookOrder(_interactorObject, Look.OnPause);
                                                if (_interactor.orbitalReach && _interactorObject.orbitalReach) _interactor.orbitalReach.ResumeOrbitalAnim(_interactorObject);
                                                _interactorObject.SendUnityEvent();
                                                _eventSent = true;
                                                continue;
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.PickableTwo:
                            {
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.PickableTwo);
                                    if (_interactor.effectors[_this].connectedTo != _interactorObject)
                                    {
                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheckXZ(_allTargets[i]);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            //Checks if any other effector is in position because this pick needs more than one effector
                                            _interactorObject.AddEffectorToUseables((int)effectorType, 2);
                                            _selfPossible = false;
                                        }
                                        else _interactorObject.RemoveEffectorFromUseables((int)effectorType, 2);
                                    }
                                    else if (_interactor.effectors[_this].connected)
                                    {
                                        if (_interactorIK.IsPaused(_interactor.effectors[_this].connectedTarget.effectorType) && !_interactor.effectors[_this].connectedTo.pickableTwoSettings.twoHandPicked && _interactor.effectors[_this].connectedTo.pickableTwoSettings.pickable)
                                        {
                                            _interactor.effectors[_this].connectedTo.pickableTwoSettings.TwoHandPick(_playerTransform, _interactor.effectors[_this].connectedTarget.transform);
                                            _interactorObject.SendUnityEvent();
                                            _interactor.NewLookOrder(_interactorObject, Look.After);
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.Push:
                            {//Push & Pull (int)90-100
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    GatherDebugDataStart(_allTargets[i], _interactorObject, InteractionTypes.Push);
                                    if (_interactor.effectors[_this].connectedTo != _interactorObject)
                                    {
                                        bool passed = !_interactorObject.used;
                                        if (passed) passed = EffectorCheck(_allTargets[i]);
                                        if (passed) passed = !HasObstacleForTarget(_interactorObject, effectorWorldSpace, _allTargets[i].transform);

                                        if (passed)
                                        {
                                            DrawDebugLines(_allTargets[i], objPlaceInList);
                                            _interactorObject.AddEffectorToUseables((int)effectorType, 2);
                                            _selfPossible = false;
                                        }
                                        else _interactorObject.RemoveEffectorFromUseables((int)effectorType, 2);
                                    }
                                    else if (_interactor.effectors[_this].connected)
                                    {
                                        if (_interactorIK.IsPaused(_interactor.effectors[_this].connectedTarget.effectorType) && !_interactor.interactionStates.playerPushing)
                                        {
                                            _interactor.effectors[_this].connectedTo.pushSettings.PushStart(_playerTransform);
                                            _interactor.interactionStates.playerPushing = true;
                                            _interactorObject.SendUnityEvent();
                                            _interactor.NewLookOrder(_interactorObject, Look.After);
                                        }
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.CoverStand:
                            {//Cover Stand (int)100-110
                                //TODO
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    if (EffectorCheck(_allTargets[i]))
                                    {
                                        DrawDebugLines(_allTargets[i], objPlaceInList);
                                    }
                                }
                                break;
                            }
                        case InteractionTypes.CoverCrouch:
                            {
                                //TODO
                                for (int i = 0; i < _allTargets.Length; i++)
                                {
                                    if ((int)_allTargets[i].effectorType != (int)effectorType) continue;
                                    if (!_allTargets[i].gameObject.activeInHierarchy || !_allTargets[i].enabled) continue;

                                    if (EffectorCheck(_allTargets[i]))
                                    {
                                        DrawDebugLines(_allTargets[i], objPlaceInList);
                                    }
                                }
                                break;
                            }
                        default:
                            break;
                    }

                    #region ExtraDebugs DebugData update
                    if (!_updateStarted) continue;
                    _updateStarted = false;
                    if (_lastTargetID == 0 && !_interactor.effectors[_this].connected) continue;

                    if (_interactorObject)
                    {
                        _used = _interactorObject.used;
                        _lastAddToUseables = _interactorObject.CanEffectorUse((int)effectorType);
                        _lastUseables = _interactorObject.UseableEffectorCount();
                        if (_interactorObject.interactionType == InteractionTypes.PickableTwo || _interactorObject.interactionType == InteractionTypes.Push)
                        {
                            if (_lastUseables < 2) _waiting = true;
                        }
                    }

                    int others = _lastStatus;
                    Transform targetTransform = _interactorIK.GetTargetTransform(effectorType);
                    if (targetTransform && targetTransform.gameObject.GetInstanceID() == _lastTargetID)
                    {
                        if (_waiting)
                        {
                            _waiting = false;
                            _lastStatusID = _lastTargetID;
                        }
                        _lastProgress = _interactorIK.GetProgress(effectorType);
                        _lastPause = _interactorIK.IsPaused(effectorType);
                    }
                    else if(_lastStatusID != _lastTargetID) others = 0;

                    if (!targetTransform)
                    {
                        _lastStatusID = 0;
                        _lastStatus = 0;
                        others = 0;
                    }

                    _interactor.GatherDebugDataEnd(_this, _lastTargetID, _waiting, _lastReposition, _targetDistance, _tempMinRadius, _tempMaxRadius, _lastAngleCheck, _lastObstacleEnabled, _lastObstacle, _lastObstacleName, _used, _lastAddToUseables, _lastUseables, _lastProgress, _lastPause, others);
                    #endregion
                }
            }

            //Starts or finishes avaiable manual interactions for this effector. Called by outside class (BasicInput class)
            public void StartStopInteractionThis(List<IntObjComponents> intOjbComponents, int selectedByUI, bool click)
            {
                InteractorObject startStopObject = null;
                //Leave null when ALL selected
                if (selectedByUI < intOjbComponents.Count)
                    startStopObject = intOjbComponents[selectedByUI].interactorObject;

                //If this method called by mouse click. To seperate two different inputs (click and F)
                if (click)
                {
                    //Click is used just for DistanceCrosshair
                    if (startStopObject && startStopObject.interactionType == InteractionTypes.DistanceCrosshair)
                    {
                        //We dont want to enter here for all effectors, just need once.
                        if (_interactor.checkOncePerObject) return;
                        _interactor.checkOncePerObject = true;

                        //Send VehiclePartController to animate its part via its animation state
                        if (_interactor.vehiclePartsActive)
                        {
                            _interactor.vehiclePartCont.Animate(startStopObject.vehiclePartId, true, _interactor);
                        }
                        startStopObject.SendUnityEvent();
                        startStopObject.RemoveEffectorFromUseables((int)effectorType);
                        _interactor.NewLookOrder(startStopObject, Look.OnPause);
                    }
                    return;
                }

                //Run F button selected interaction by UI selection. UI selection comes from BasicUI class.
                if (selectedByUI < intOjbComponents.Count)
                {
                    //This is for connected interactions, so they will disconnect and end.
                    if (_interactor.effectors[_this].connected)
                    {
                        if (!startStopObject.HasEffectorTypeInTargets((int)effectorType)) return;

                        #region Interruption Transfer
                        InteractionTypes selectedIntType = startStopObject.interactionType;
                        InteractionTypes currentType = _interactor.effectors[_this].connectedTo.interactionType;

                        //This block deals with interruptions
                        if (startStopObject != _interactor.effectors[_this].connectedTo && !SkipInterruption(currentType))
                        {
                            if (selectedIntType == InteractionTypes.DistanceCrosshair) return;

                            if (!startStopObject.CanEffectorUse((int)effectorType)) return;
                            if (startStopObject.used) return;
                            if (startStopObject.pickableOneSettings && startStopObject.pickableOneSettings.oneHandPicked) return;

                            //Check other effector is that one is available for connection, if so connect that with interruption instead of disconnecting this effector.
                            //Only for single connection (one hand etc) and has more than one target interactions
                            if (!startStopObject.multipleConnections)
                            {
                                if (startStopObject.pickableTwoSettings && startStopObject.pickableTwoSettings.pickable) return;

                                if (startStopObject.UseableEffectorCount() > 1)
                                {
                                    int otherUseable = _interactorObject.GetOtherUseableEffector((int)effectorType);
                                    int otherEffectorIndex = _interactor.GetEffectorIndex(otherUseable);
                                    if (otherEffectorIndex >= 0 && !_interactor.effectors[otherEffectorIndex].connected)
                                    {
                                        startStopObject.RemoveEffectorFromUseables((int)effectorType);
                                        return;
                                    }
                                    else if(otherEffectorIndex >= 0)
                                        startStopObject.RemoveEffectorFromUseables(otherEffectorIndex);
                                }
                            }

                            if (_interactor.effectors[_this].connectedTo.interruptible && _interactor.effectors[_this].connectedTo.priority <= startStopObject.priority)
                            {
                                if (_interactor.effectors[_this].connectedTo.interactionType != InteractionTypes.MultipleCockpit)
                                {
                                    //Start transferring interruption, manuelbutton type only.
                                    if (_interactor.effectors[_this].connectedTo.interactionType == InteractionTypes.ManualButton && startStopObject.interactionType == InteractionTypes.ManualButton)
                                    {
                                        if (_transferring) return;

                                        _transferFromIntObj = _interactor.effectors[_this].connectedTo;
                                        _transferFromTarget = _interactor.effectors[_this].connectedTarget;
                                        _transferToIntObj = startStopObject;
                                        _transferToTarget = startStopObject.GetTargetForEffectorType((int)effectorType);

                                        _transferring = true;
                                        return;
                                    }

                                    _interactor.effectors[_this].connectedTo.SendUnityEndEvent();
                                    _interactor.DisconnectAllForThis(_interactor.effectors[_this].connectedTo);
                                    if (startStopObject.multipleConnections)
                                    {
                                        int otherUseable = _interactorObject.GetOtherUseableEffector((int)effectorType);
                                        int otherEffectorIndex = _interactor.GetEffectorIndex(otherUseable);
                                        if (otherEffectorIndex >= 0 && _interactor.effectors[otherEffectorIndex].connected)
                                        {
                                            _interactor.DisconnectAllForThis(_interactor.effectors[otherEffectorIndex].connectedTo);
                                        }
                                    }
                                    StartStopInteractionThis(intOjbComponents, selectedByUI, click);
                                }
                                else
                                {
                                    if (_transferring) return;

                                    _transferFromIntObj = _interactor.effectors[_this].connectedTo;
                                    _transferFromTarget = _interactor.effectors[_this].connectedTarget;
                                    _transferToIntObj = startStopObject;
                                    _transferToTarget = startStopObject.GetTargetForEffectorType((int)effectorType);

                                    _transferring = true;
                                    return;
                                }
                            }
                            return;
                        }
                        #endregion

                        //focus object is active interaction type
                        switch (_interactor.effectors[_this].connectedTo.interactionType)
                        {
                            case InteractionTypes.ManualButton:
                                //If PauseOnObject enabled
                                    if (_interactorIK.IsPaused(effectorType))
                                        ResumeInteraction(effectorType);
                                break;

                            case InteractionTypes.ManualSwitch:
                                //ManualSwitch interaction ends automatically in effector Update
                                break;

                            case InteractionTypes.ManualRotator:
                                //Turn off InteractorObject usedBy, interaction ended.
                                _interactor.effectors[_this].connectedTo.used = false;
                                startStopObject.SendUnityEndEvent();
                                break;

                            case InteractionTypes.ManualHit:
                                startStopObject.SendUnityEndEvent();
                                break;

                            case InteractionTypes.DistanceCrosshair:
                                //DistanceCrosshair interaction ends automatically
                                break;

                            case InteractionTypes.ClimbableLadder:
                                {
                                    //Climb interaction starts or ends with only LeftHand
                                    if (effectorType != FullBodyBipedEffector.LeftHand) return;
                                    //This is toggleable, since LeftHand connection is more complex 
                                    //(Connection can be already started in most cases because we're already touching ladder when we close)
                                    //To prevent bugs same codes goes to other side of StartStop below
                                    //If climbing didnt started yet, start with usedBy so other effectors gonna start too.
                                    if (!_interactor.effectors[_this].connectedTo.used)
                                    {
                                        _interactor.effectors[_this].connectedTo.used = true;
                                        _interactor.effectors[_this].connectedTo.RemoveEffectorFromUseables((int)effectorType);
                                        _interactor.effectors[_this].connectedTo.climbableSettings.ReposClimbingPlayerBottom(_interactor.effectors[_this].connectedTo, effectorWorldSpace);
                                        _interactor.effectors[_this].connectedTo.climbableSettings.ReposClimbingPlayerTop(_interactor.effectors[_this].connectedTo, effectorWorldSpace);
                                    }
                                    //If already climbing, end with usedBy same way
                                    else
                                    {
                                        _interactor.effectors[_this].connectedTo.used = false;
                                        StopAllInteractions();
                                        startStopObject.SendUnityEndEvent();
                                        _interactor.Disconnect(_this);
                                    }
                                    break;
                                }
                            case InteractionTypes.MultipleCockpit:
                                {
                                    if (_interactor.effectors[_this].connectedTo != startStopObject) return;

                                    startStopObject.multipleSettings.MultipleOut(_interactor);
                                    _interactor.DisconnectAllForThisOnce(_interactor.effectors[_this].connectedTo);
                                    if (startStopObject.orbitalReach)
                                    {
                                        _interactor.orbitalReach.SetLayerWeightToLast();
                                        _interactor.orbitalReach.ResumeOrbitalAnim(startStopObject);
                                    }
                                    startStopObject.used = false;
                                    //Toggling windshield anim if it is ProtoTruck
                                    if (_interactor.vehiclePartsActive && _interactor.interactionStates.enteredVehicle == _interactor.vehicleInput.gameObject)
                                    {
                                        _interactor.vehiclePartCont.ToggleWindshield(false, _interactor);
                                    }
                                    startStopObject.SendUnityEndEvent();
                                    break;
                                }
                            case InteractionTypes.PickableOne:
                                {
                                    if (_interactor.effectors[_this].connectedTo != startStopObject)
                                    {//New object other than the one this effector using currently
                                        if (startStopObject.used) break; //Other effector is using it
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) break;
                                        int otherEffector = startStopObject.GetOtherUseableEffector((int)effectorType);
                                        if (otherEffector >= 0 && !IsEffectorConnected(otherEffector)) break;//Leave it to other effector
                                    }
                                    if (!startStopObject.pickableOneSettings.oneHandPicked) break;

                                    if (startStopObject.orbitalReach)
                                    {
                                        if (startStopObject.pickableOneSettings.dropType == PickableOne.DropType.DropLocation && _interactor.EffectorCheckOrbitalPosition(startStopObject.dropLocation, (int)effectorType))
                                        {
                                            startStopObject.pickableOneSettings.orbitalDropping = true;
                                            PrepareEndingOrbitalInteraction(startStopObject, _interactor.effectors[_this].connectedTarget);
                                            break;
                                        }
                                    }
                                    
                                    if (!startStopObject.pickableOneSettings.Drop()) break;
                                    
                                    startStopObject.SendUnityEndEvent();
                                    startStopObject.used = false;
                                    _interactor.NewLookOrder(startStopObject, Look.After);
                                    break;
                                }
                            case InteractionTypes.PickableTwo:
                                {
                                    if (_interactor.effectors[_this].connectedTo.pickableTwoSettings.twoHandPicked)
                                        _interactor.effectors[_this].connectedTo.pickableTwoSettings.TwoHandDrop();
                                    startStopObject.SendUnityEndEvent();
                                    startStopObject.used = false;
                                    if (startStopObject.orbitalReach)
                                        _interactor.orbitalReach.ResumeOrbitalAnim(startStopObject);
                                    StopInteraction(effectorType);
                                    _interactor.Disconnect(_this);
                                    break;
                                }
                            case InteractionTypes.Push:
                                {
                                    _interactor.interactionStates.playerPushing = false;
                                    _interactor.effectors[_this].connectedTo.RemoveEffectorFromUseables((int)effectorType);
                                    _interactor.effectors[_this].connectedTo.pushSettings.PushEnd();
                                    ResumeInteraction(effectorType);
                                    startStopObject.SendUnityEndEvent();
                                    startStopObject.used = false;
                                    _interactor.Disconnect(_this);
                                    break;
                                }
                            default:
                                //Not implemented yet or unnecessary interaction calls drop here.
                                break;
                        }
                    }
                    //This part is mostly for interaction start by input. Since effector is not connected but is in position for a connection.
                    else
                    {
                        for (int j = 0; j < startStopObject.childTargets.Length; j++)
                        {
                            InteractorTarget currentTarget = startStopObject.childTargets[j];
                            if ((int)currentTarget.effectorType != (int)effectorType)
                                continue;

                            switch (startStopObject.interactionType)
                            {
                                case InteractionTypes.ManualButton:
                                    {
                                        //If already used or this effector cant use, pass. Else start interaction.
                                        if (startStopObject.used) continue;
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;
                                        if (!currentTarget.gameObject.activeInHierarchy || !currentTarget.enabled) continue;

                                        //If used button is part of Vehicle
                                        if (_interactor.vehiclePartsActive)
                                        {
                                            _interactor.vehiclePartCont.Animate(startStopObject.vehiclePartId, true, _interactor);
                                        }

                                        int otherEffector = startStopObject.GetOtherUseableEffector((int)effectorType);
                                        if (otherEffector >= 0)
                                        {//Object have more than one InteractorTarget and currently multiple effectors can use it. Use the closest one.
                                            if (CloserEffector(otherEffector, currentTarget)) continue;
                                        }

                                        //Interactor used this object, turn off outline mat if exist.
                                        //Because it is early for RemoveEffectorFromUseables yet, it will handled on Update when interaction is over.
                                        startStopObject.Prepare(false);
                                        startStopObject.used = true;
                                        RotatePivot(startStopObject);

                                        if (startStopObject.orbitalReach) PrepareOrbitalInteraction(startStopObject, currentTarget);
                                        else StartInteraction(currentTarget.effectorType, currentTarget, startStopObject);
                                        break;
                                    }
                                case InteractionTypes.ManualSwitch:
                                    {
                                        if (startStopObject.used) continue;
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;

                                        startStopObject.Prepare(false);
                                        startStopObject.used = true;
                                        RotatePivot(startStopObject);

                                        if (startStopObject.orbitalReach) PrepareOrbitalInteraction(startStopObject, currentTarget);
                                        else StartInteraction(currentTarget.effectorType, currentTarget, startStopObject);
                                        break;
                                    }
                                case InteractionTypes.ManualRotator:
                                    {
                                        if (startStopObject.used) continue;
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;

                                        startStopObject.Prepare(false);
                                        startStopObject.used = true;
                                        RotatePivot(startStopObject);

                                        if (startStopObject.orbitalReach)
                                        {
                                            if (!PrepareOrbitalInteraction(startStopObject, currentTarget)) break;
                                        }
                                        else StartInteraction(currentTarget.effectorType, currentTarget, startStopObject);
                                        //Send interacted object to camera for locking Y rotation
                                        //Because ManualRotator uses Y axis for interact.
                                        FreeLookCam.LockCamY(startStopObject.gameObject);
                                        break;
                                    }
                                case InteractionTypes.ManualHit:
                                    {
                                        if (startStopObject.used) continue;
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;

                                        startStopObject.Prepare(false);
                                        startStopObject.used = true;
                                        RotatePivot(startStopObject);

                                        StartInteraction(currentTarget.effectorType, currentTarget, startStopObject);
                                        break;
                                    }
                                case InteractionTypes.ClimbableLadder:
                                    {
                                        //Same with connected side of StartStopInteractionThis because its toggleable.
                                        if (effectorType != FullBodyBipedEffector.LeftHand) continue;
                                        if (!_interactor.effectors[_this].connectedTo) continue;

                                        if (!_interactor.effectors[_this].connectedTo.used)
                                        {
                                            _interactor.effectors[_this].connectedTo.used = true;
                                            _interactor.effectors[_this].connectedTo.Prepare(false);
                                            _interactor.effectors[_this].connectedTo.climbableSettings.ReposClimbingPlayerBottom(_interactor.effectors[_this].connectedTo, effectorWorldSpace);
                                            _interactor.effectors[_this].connectedTo.climbableSettings.ReposClimbingPlayerTop(_interactor.effectors[_this].connectedTo, effectorWorldSpace);
                                        }
                                        else
                                        {
                                            _interactor.effectors[_this].connectedTo.used = false;
                                            StopAllInteractions();
                                            _interactor.NewLookOrder(_interactor.effectors[_this].connectedTo, Look.Never);
                                            _interactor.Disconnect(_this);
                                        }
                                        break;
                                    }
                                case InteractionTypes.MultipleCockpit:
                                    {
                                        if (_interactor.checkOncePerObject) return;
                                        _interactor.checkOncePerObject = true;

                                        //Check the Player State which is true when effector is in position
                                        if (!_interactor.interactionStates.playerChangable) continue;

                                        //If any effectors connected, disconnect because we're gonna connect it.
                                        if (_interactor.anyConnected) _interactor.DisconnectAll();

                                        if (startStopObject.orbitalReach)
                                        {
                                            if (!PrepareOrbitalInteraction(startStopObject, currentTarget)) break;
                                        }
                                        else
                                        {
                                            startStopObject.multipleSettings.MultipleIn(_interactor);
                                            RotatePivot(startStopObject);

                                            _interactor.ConnectAll(startStopObject.childTargets, startStopObject);
                                        }

                                        startStopObject.used = true;
                                        startStopObject.SendUnityEvent();
                                        //Toggling windshield anim if it is ProtoTruck
                                        if (_interactor.vehiclePartsActive && _interactor.interactionStates.enteredVehicle == _interactor.vehicleInput.gameObject)
                                        {
                                            _interactor.vehiclePartCont.ToggleWindshield(true, _interactor);
                                        }
                                        _interactor.NewLookOrder(startStopObject, Look.OnPause);
                                        break;
                                    }
                                case InteractionTypes.PickableOne:
                                    {
                                        //If object already picked (maybe by other effectors), or this effector cant use, pass. Else connect. Picking will be done in effector Update.
                                        if (startStopObject.used) continue;
                                        if (!currentTarget.gameObject.activeInHierarchy) continue;
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;
                                        int otherEffector = startStopObject.GetOtherUseableEffector((int)effectorType);
                                        if (otherEffector >= 0 && CloserEffector(otherEffector, currentTarget)) continue;

                                        startStopObject.used = true;
                                        startStopObject.Prepare(false);
                                        RotatePivot(startStopObject);

                                        if (startStopObject.orbitalReach) PrepareOrbitalInteraction(startStopObject, currentTarget);
                                        else StartInteraction(effectorType, currentTarget, startStopObject);
                                        break;
                                    }
                                case InteractionTypes.PickableTwo:
                                    {
                                        //If this effector cant use or object has less than 2 useables, pass. Else connect.
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;
                                        if (startStopObject.UseableEffectorCount() < 2) continue;

                                        startStopObject.Prepare(false);
                                        startStopObject.pickableTwoSettings.pickable = true;
                                        RotatePivot(startStopObject);

                                        if (startStopObject.orbitalReach) PrepareOrbitalInteraction(startStopObject, currentTarget);
                                        else StartInteraction(currentTarget.effectorType, currentTarget, startStopObject);
                                        startStopObject.used = true;
                                        break;
                                    }
                                case InteractionTypes.Push:
                                    {
                                        if (!startStopObject.CanEffectorUse((int)effectorType)) continue;
                                        if (startStopObject.UseableEffectorCount() < 2) continue;

                                        startStopObject.Prepare(false);
                                        RotatePivot(startStopObject);

                                        StartInteraction(currentTarget.effectorType, currentTarget, startStopObject);
                                        startStopObject.used = true;
                                        break;
                                    }
                                default:
                                    //Not yet implemented or unnecessary interaction calls drop here.
                                    break;
                            }
                        }
                    }
                }
                //This is similar and half implemented yet(Only works with ManualButtons). Runs when user select All Objects on UI.
                //Starts interactions of all possible effectors with all possible InteractorObjects(one per effector, obviously).
                else if (selectedByUI == intOjbComponents.Count)
                {
                    if (_interactor.effectors[_this].connected)
                    {
                        switch (_interactor.effectors[_this].connectedTo.interactionType)
                        {
                            case InteractionTypes.ManualButton:
                                //ManualButton interaction ends automatically in effector Update
                                break;

                            default:
                                //Not yet implemented or unnecessary interaction calls drop here.
                                break;
                        }
                    }
                    else if (intOjbComponents.Count > 0)
                    {
                        for (int i = 0; i < intOjbComponents.Count; i++)
                        {
                            for (int j = 0; j < intOjbComponents[i].interactorObject.childTargets.Length; j++)
                            {
                                InteractorTarget currentTarget = intOjbComponents[i].interactorObject.childTargets[j];
                                if ((int)currentTarget.effectorType != (int)effectorType)
                                    continue;

                                if (_interactor.effectors[_this].connected) continue;

                                switch (intOjbComponents[i].interactorObject.interactionType)
                                {
                                    case InteractionTypes.ManualButton:
                                        {
                                            if (intOjbComponents[i].interactorObject.used) continue;
                                            if (!intOjbComponents[i].interactorObject.CanEffectorUse((int)effectorType)) continue;
                                            if (intOjbComponents[i].interactorObject.orbitalReach) continue;

                                            if (_interactor.vehiclePartsActive)
                                            {
                                                _interactor.vehiclePartCont.Animate(intOjbComponents[i].interactorObject.vehiclePartId, true, _interactor);
                                            }
                                            intOjbComponents[i].interactorObject.RemoveEffectorFromUseables((int)effectorType);
                                            intOjbComponents[i].interactorObject.used = true;
                                            RotatePivot(intOjbComponents[i].interactorObject);
                                            StartInteraction(currentTarget.effectorType, currentTarget, intOjbComponents[i].interactorObject);
                                            break;
                                        }
                                    default:
                                        //Not yet implemented or unnecessary interaction calls drop here.
                                        break;
                                }
                            }
                        }
                    }
                    else
                        return;
                }
                else
                {
                    Debug.Log("UI selection error");
                }
            }

            private void RotatePivot(InteractorObject intObj)
            {
                Transform elbow = _interactor.GetEffectorElbow((int)effectorType);
                if (elbow) intObj.RotatePivotToElbow(_interactor.GetEffectorElbow((int)effectorType));
                else intObj.RotateToV3(effectorWorldSpace);
            }

            private bool SkipInterruption(InteractionTypes type)
            {
                return type == InteractionTypes.PickableOne ||
                       type == InteractionTypes.PickableTwo;
            }

            public void ConnectThis(InteractorObject connectedTo)
            {
                _lastStatus = 3;
                connectedTo.RemoveEffectorFromUseables((int)effectorType);
                _interactorIK.StartInteraction(effectorType, _interactor.effectors[_this].connectedTarget, connectedTo);
            }

            //Ends interaction for this effector, called by Interactor with its Disconnect method
            public void DisconnectThis()
            {
                _interactor.effectors[_this].connectedTo.RemoveEffectorFromUseables((int)effectorType);
                _interactor.effectors[_this].connectedTo.used = false;
                if (_interactor.effectors[_this].connectedTo.pushSettings)
                {
                    _interactor.effectors[_this].connectedTo.pushSettings.PushEnd();
                    _interactor.interactionStates.playerPushing = false;
                }

                if (_interactor.effectors[_this].connectedTo.hasInteractiveRotator)
                {
                    _interactor.effectors[_this].connectedTo.GetComponent<InteractiveRotator>().active = false;
                    FreeLookCam.LockCamY(_interactor.effectors[_this].connectedTo.gameObject);
                }
                if (_interactorObject.hasAutomover)
                {
                    AutoMover auto = _interactor.effectors[_this].connectedTarget.GetComponent<AutoMover>();
                    if (auto != null)
                    {
                        auto.ResetBools();
                    }
                }
                _interactor.effectors[_this].connected = false;
                _eventSent = false;
                StopInteraction(effectorType);
            }

            #region Debug Lines
            //Draws fainted lines for every interaction object's every same effector type target in sphere
            private void DrawDebugLines(InteractorTarget[] allTargets)
            {
#if UNITY_EDITOR
                if (_interactor.debug)
                {
                    for (int i = 0; i < allTargets.Length; i++)
                    {
                        if (allTargets[i].effectorType != effectorType) continue;
                        if (!allTargets[i]) return;

                        Debug.DrawLine(effectorWorldSpace, allTargets[i].transform.position, ColorForArrayPlace(_this, false));
                    }
                }
#endif
            }
            //Draws possible interaction targets
            //Set by effector Update() when target passes the EffectorCheck()
            private void DrawDebugLines(InteractorTarget allTargets, int intObjPlaceInList)
            {
#if UNITY_EDITOR
                if (_interactor.debug)
                {
                    if (!allTargets) return;

                    //If InteractorObjects list place equals to UI selection, set a position for 
                    //InteractorEditor to draw bezier.
                    if (intObjPlaceInList == _interactor.selectedByUI)
                    {
                        targetPosition = allTargets.transform.position;
                        targetActive = true;
                    }

                    Debug.DrawLine(effectorWorldSpace, allTargets.transform.position, ColorForArrayPlace(_this, true));
                }
#endif
            }
            #endregion
        }
    }
}
