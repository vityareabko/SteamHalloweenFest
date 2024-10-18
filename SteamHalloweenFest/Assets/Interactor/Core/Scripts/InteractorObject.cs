using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace razz
{
    #region Interaction & Look Types
    //You can add new tags here with category name and interaction name, then add it in InteractionTypes too
    public static class Tags
    {
        [TagField(categoryName = "")] public const int Unselected = 0;
        [TagField(categoryName = "Default")] public const int Animated = 10;
        [TagField(categoryName = "Manual")] public const int Button = 20;
        [TagField(categoryName = "Manual")] public const int Switch = 21;
        [TagField(categoryName = "Manual")] public const int Rotator = 22;
        [TagField(categoryName = "Manual")] public const int Hit = 23;
        [TagField(categoryName = "Manual")] public const int Force = 24;
        [TagField(categoryName = "Touch")] public const int Vertical = 30;
        [TagField(categoryName = "Touch")] public const int HorizontalUp = 31;
        [TagField(categoryName = "Touch")] public const int HorizontalDown = 32;
        [TagField(categoryName = "Touch")] public const int Still = 33;
        [TagField(categoryName = "Distance")] public const int CrossHair = 40;
        [TagField(categoryName = "Climbable")] public const int Ladder = 50;
        [TagField(categoryName = "Multiple")] public const int Cockpit = 60;
        [TagField(categoryName = "Self")] public const int Itch = 70;
        [TagField(categoryName = "Pickable")] public const int OneHand = 80;
        [TagField(categoryName = "Pickable")] public const int TwoHands = 85;
        [TagField(categoryName = "Push - Pull")] public const int Push = 90;
        [TagField(categoryName = "Cover")] public const int Stand = 100;
        [TagField(categoryName = "Cover")] public const int Crouch = 101;
    }

    public enum InteractionTypes
    {
        DefaultAnimated = Tags.Animated,
        ManualButton = Tags.Button,
        ManualSwitch = Tags.Switch,
        ManualRotator = Tags.Rotator,
        ManualHit = Tags.Hit,
        ManualForce = Tags.Force,
        TouchVertical = Tags.Vertical,
        TouchHorizontalUp = Tags.HorizontalUp,
        TouchHorizontalDown = Tags.HorizontalDown,
        TouchStill = Tags.Still,
        DistanceCrosshair = Tags.CrossHair,
        ClimbableLadder = Tags.Ladder,
        MultipleCockpit = Tags.Cockpit,
        SelfItch = Tags.Itch,
        PickableOne = Tags.OneHand,
        PickableTwo = Tags.TwoHands,
        Push = Tags.Push,
        CoverStand = Tags.Stand,
        CoverCrouch = Tags.Crouch
    };

    public enum Look
    {
        Never = 0,
        OnSelection = (1 << 0),
        Before = (1 << 1),
        OnPause = (1 << 2),
        After = (1 << 3),
        Always = ~0
    }
    #endregion

    [HelpURL("https://negengames.com/interactor/components.html#interactorobjectcs")]
    [DisallowMultipleComponent]
    public class InteractorObject : MonoBehaviour
    {
        #region InteractorObject Variables
        //TagFilter is custom attribute, it will give drop down menu with categories filled with Tags class
        //And return assigned int. You can remove it and use regular enum  property on inspector.
        [Header("Type Specific Settings")]
        [TagFilter(typeof(Tags))] public int interaction;
        [Space(20f)]
        [ConditionalSO(Condition.Show, nameof(DefaultEnabled))] 
        public Default defaultSettings;
        [ConditionalSO(Condition.Show, nameof(ManualEnabled))]
        public Manual manualSettings;
        [ConditionalSO(Condition.Show, nameof(TouchEnabled))]
        public Touch touchSettings;
        [ConditionalSO(Condition.Show, nameof(DistanceEnabled))]
        public Distance distanceSettings;
        [ConditionalSO(Condition.Show, nameof(ClimbableEnabled))]
        public Climbable climbableSettings;
        [ConditionalSO(Condition.Show, nameof(MultipleEnabled))]
        public Multiple multipleSettings;
        [ConditionalSO(Condition.Show, nameof(SelfEnabled))]
        public Self selfSettings;
        [ConditionalSO(Condition.Show, nameof(PickableOneEnabled))]
        public PickableOne pickableOneSettings;
        [ConditionalSO(Condition.Show, nameof(PickableTwoEnabled))]
        public PickableTwo pickableTwoSettings;
        [ConditionalSO(Condition.Show, nameof(PushEnabled))]
        public Push pushSettings;
        [ConditionalSO(Condition.Show, nameof(CoverEnabled))]
        public Cover coverSettings;

        [Foldout("Interaction Settings", true)]
        [Tooltip("Enables Orbital Reach animations for this interaction, allowing the player to reach objects when they are nearby. Bypasses the effector rules because the player can reach anywhere within the sphere. Requires the Orbital Reach component on the Interactor gameobject.")]
        public bool orbitalReach = false;
        [Tooltip("If using OrbitalReach, adds this time (in seconds) to slow down or speed up the beginning of the orbital animation.")]
        public float orbitalLateStart = 0f;
        [Tooltip("If using OrbitalReach, adds this time (in seconds) to slow down or speed up the ending of the orbital animation.")]
        public float orbitalLateEnd = 0f;
        [Tooltip("Prevents this object from leaving Interactor's list with OnTriggerExit. If enabled, this InteractorObject will only exit with Interactor.RemoveInteractionManual() call.")]
        public bool preventExit;
        [Tooltip("Interactable objects list gets ordered with this priority. Higher priority interaction will be upper on the selection list and also will take look target priority")]
        [Range(0, 200)]
        public int priority = 100;
        [Tooltip("Enable if this object needs more than one effector to interact.")]
        public bool multipleConnections = false;
        [Tooltip("If this interaction needs to pause on half.")] 
        public bool pauseOnInteraction;
        [Tooltip("If this interaction interruptible.")] 
        public bool interruptible;
        [Tooltip("If there is pivot object between target and parent, assign here to rotate itself to effector while interacting.")]
        public Transform pivot;
        [Tooltip("Rotate pivot on X axis")]
        public bool pivotX = true;
        [Tooltip("Rotate pivot on Y axis")]
        public bool pivotY = true;
        [Tooltip("Rotate pivot on Z axis")]
        public bool pivotZ = true;
        [Tooltip("Resets pivot rotation to back when interaction is over.")]
        public bool resetPivot = false;
        [HideInInspector] public Transform rotatePivotTo;
        [Tooltip("Extra raycast to check if object has any obstacles between target and effector.")]
        public bool obstacleRaycast;
        [Tooltip("Exclude colliders to be checked for obstacleRaycast. You can exclude this object's and child colliders for checking obstacles for example.")]
        public Collider[] excludeColliders;
        [Tooltip("If all targets arent child of this InteractorObject, select a parent who has all targets.")]
        public GameObject otherTargetsRoot;
        [Tooltip("If other InteractorObject targets exist in this children, exlclude them from this interactions' targets.")]
        public InteractorTarget[] excludeTargets;

        [Foldout("Speed Settings", true)]
        [Tooltip("The time needs to pass to reach the target which is half of interaction.")]
        public float targetDuration = 0.8f;
        [Tooltip("The time needs to pass to go back to default position from target which is other half of interaction.")]
        public float backDuration = 0.7f;
        [Tooltip("Interaction animation easing. Select Custom for Animation Curve editing.")]
        public EaseType easeType = EaseType.QuadInOut;
        [Tooltip("Animation curve for custom speed needs at least 3 keyframes at (0,0), (1,1) and (0,2). You can add more keyframes between those. So they'll be between 0 and 2 (horizontal values). 0 to 1 is for targetPath, 1 to 2 is for backPath.")]
        [Conditional(Condition.Show, nameof(ShowAnimationCurve))]
        public AnimationCurve speedCurve;
        [TextArea]
        public string ps = "Before editing the animation curve, select an InteractorTarget (one of this InteractorObject's), then select back this object (also make sure Speed debug is enabled (Speed button on InteractorTarget)). This way it can visualize the speed values in SceneView and also it will check your animation curve to prevent editing mistakes.";

        [Foldout("Animation Assist Settings", true)]
        [Tooltip("Enable if you wish to use animation clips with this interaction. Interactor player needs to have AnimAssist component to use this.")]
        public bool animAssistEnabled;
        [Conditional(Condition.Show, "animAssistEnabled")] 
        public string clipName = "CaSe-SEnsitiVe";
        [Tooltip("Lower this weight if you wish to blend the clip with your default Animator Controller layer animation.")]
        [Range(0f, 1f)] public float clipWeight = 1f;
        [Tooltip("Adjust clip speed from here, not on state settings on Animator Controller. Start Time and Clip Offset will be adjusted accordingly.")]
        [Range(0.05f, 10f)] public float clipSpeed = 1f;
        [Tooltip("If you wish to skip first part of your clip, adjust this normalized value. It is between 0 and 1 so it will work as percentage. Start Time calculation will start after this cut. And if you wish to cut last part of the clip, use InteractorLayer state settings at Animator Controller by selecting exit transitions and adjusting their Exit Times.")]
        [Range(0f, 1f)] public float clipOffset = 0f;
        [Tooltip("Adjust this value to determine the starting point of the clip where the interaction will begin. You can select the clip and make your decision while previewing it.")]
        [Range(0f, 1f)] public float startTime = 0.5f;
        [Tooltip("Enable this option if you wish to use the same animation clip for leaving the InteractorObject. This is applicable to InteractorObjects that have two uses, such as using the same object to drop.")]
        public bool secondUse = false;
        [Tooltip("If you use Second Use, you can set different starting time for the same clip. If you wish to use same, set same value as Start Time.")]
        [Range(0f, 1f)] public float secondStartTime = 0.5f;

        [Foldout("AI Settings", true)]
        [Tooltip("Set any transform to enable AI movement for this interaction. Player will move to its position and rotation then will start interaction by itself. But this InteractorObject needs to be in a PathGrid.")]
        public Transform aiTarget;
        [Tooltip("InteractorObjects are exclusively added to Interactors by the PathGrid. Players will initiate this interaction only when they have reached their designated aiTarget. If disabled, interactions can be used as normal too when player is in a good position.")]
        public bool aiOnly;
        [Tooltip("Interactor initiates the interaction before reaching the aiTarget, resulting in a more natural movement. (in meters)")]
        public float startEarly = 0;
        private bool _reached;

        public bool Reached
        {
            get { return _reached; }
            set { _reached = value; }
        }

        [Foldout("Look Settings", true)]
        [Tooltip("Enable when you want to look at this. You can select multiple states.")]
        [EnumFlags] public Look lookAtThis = Look.Never;
        [Tooltip("If enabled, it will look at active InteractorTarget of this InteractorObject instead of this transform.")]
        public bool lookAtChildren = false;
        [Tooltip("If assigned, it will look at this transform.")]
        public Transform alternateLookTarget;
        [Tooltip("Seconds to pass before starting to look")]
        public float waitTimeToLook = 0f;
        [Tooltip("Seconds to end total ongoing look. 0 for disable.")]
        public float lookTimeout = 0f;
        [Tooltip("You can lower weight for close targets.")]
        [Range(0.05f, 1f)]public float lookWeight = 1f;
        [Tooltip("The time needs to pass to rotate the head to the target.")]
        public float rotationDurationTarget = 1f;
        [Tooltip("The time needs to pass to rotate the head to its default rotation.")]
        public float rotationDurationBack = 1f;

        [Foldout("Other", true)]
        [Tooltip("If highlighted material is another object, assign here. It will enable outlines when object is interactable. If it is this object's own material leave empty, it will get material by itself.")]
        public MeshRenderer outlineMat;
        [Tooltip("Enables this object when interactable, disables when used or not interactable. Useful for input text over objects or infos around them.")]
        public GameObject toggleObject;

        [Foldout("Events", true)]
        [Space(10)]
        public UnityEvent unityEvent;
        public UnityEvent unityEndEvent;

        [Foldout("Pick Up", true)]
        [Tooltip("If the selected Drop Type is Drop Location for PickableOne, and if this hand target transform is a valid location for the Interactor, the object will be dropped here.")]
        public Transform dropLocation;
        [Tooltip("If the selected Drop Type is Inventory (onBody doesn't require Item component) and this item will be send into inventory, assign Item component here.")]
        public Item itemForInventory;
        [Tooltip("When an item sent to an inventory, the renderer/renderers of this object will set disabled until hand reaches the inventory point to pick back up. Assign the visible renderers here. If set empty, they'll be gathered on start for pick up type interactions.")]
        public Renderer[] enabledRenderers;

        [HideInInspector] public InteractionTypes interactionType;
        [HideInInspector] public InteractorTarget[] childTargets;
        [HideInInspector] public bool ready;
        [HideInInspector] public bool used;
        [HideInInspector] public Interactor currentInteractor;

        private bool[] _useableEffectors;
        private InteractionTypeSettings _activeSettings;

        //Rigidbody, raycast or pivot operations
        [HideInInspector] public Collider col;
        [HideInInspector] public Rigidbody rigid;
        [HideInInspector] public bool hasRigid;
        [HideInInspector] public Vector3 rotateTo;
        private Quaternion _pivotLocalEulerCached;
        private Vector3 _tempRotation;
        //ManualHit
        private HitHandler _hitHandler;
        [HideInInspector] public bool hitObjUseable;
        [HideInInspector] public bool hitDone;
        //Outline Material
        private int _propertyIdFirstOutline;
        private int _propertyIdSecondOutline;
        private Color _firstOutline;
        private Color _secondOutline;
        private bool _hasOutlineMat;
        [HideInInspector] public Material thisMat;
        //AutoMovers
        [HideInInspector] public AutoMover[] autoMovers;
        [HideInInspector] public bool hasAutomover;
        //Switches & Rotators
        [HideInInspector] public InteractiveSwitch[] interactiveSwitches;
        [HideInInspector] public bool hasInteractiveSwitch;
        [HideInInspector] public InteractiveRotator[] interactiveRotators;
        [HideInInspector] public bool hasInteractiveRotator;
        //Vehicle Parts
        //Gets true automatically by VehiclePartControls if it has animation on Vehicle Animator
        [HideInInspector] public bool isVehiclePartwithAnimation;
        //Gets its hash id automatically by VehiclePartControls if it has animation on Vehicle Animator
        [HideInInspector] public int vehiclePartId;
        #endregion

        public void SendUnityEvent()
        {
            if (unityEvent != null) unityEvent.Invoke();
        }

        public void SendUnityEndEvent()
        {
            if (unityEndEvent != null) unityEndEvent.Invoke();
        }

        private void Awake()
        {
            if (easeType == EaseType.CustomCurve)
            {
                if (speedCurve == null || speedCurve.keys.Length < 3)
                {
                    Debug.LogWarning("SpeedCurve is not correct. \"" + this.name + "\" easing type set as QuadIn. Please set Custom Curve of InteractorObject.", this);
                    easeType = EaseType.QuadIn;
                }
            }
            //Unselected InteractionType
            if (interaction == 0)
            {
                Debug.LogWarning(this.name + " has InteractorObject with unselected Interaction Type! GameObject disabled.", this);
                gameObject.SetActive(false);
                return;
            }
            else
            {
                bool pauseWarning = false;
                //Set all settings files null except the one for selected type
                if (!DefaultEnabled()) defaultSettings = null;
                else if (defaultSettings != null) _activeSettings = defaultSettings;
                if (!ManualEnabled()) manualSettings = null;
                else if (manualSettings != null) _activeSettings = manualSettings;
                if (!TouchEnabled()) touchSettings = null;
                else if (touchSettings != null)
                {
                    touchSettings = Instantiate(touchSettings);
                    _activeSettings = touchSettings;
                }
                if (!DistanceEnabled()) distanceSettings = null;
                else if (distanceSettings != null) _activeSettings = distanceSettings;
                if (!ClimbableEnabled()) climbableSettings = null;
                else if (climbableSettings != null)
                {
                    _activeSettings = climbableSettings;
                    if (!pauseOnInteraction) pauseWarning = true;
                    multipleConnections = true;
                }
                if (!MultipleEnabled()) multipleSettings = null;
                else if (multipleSettings != null)
                {
                    multipleSettings = Instantiate(multipleSettings);
                    _activeSettings = multipleSettings;
                    if (!pauseOnInteraction) pauseWarning = true;
                    multipleConnections = true;
                }
                if (!SelfEnabled()) selfSettings = null;
                else if (selfSettings != null)
                {
                    _activeSettings = selfSettings;
                    if (!pauseOnInteraction) pauseWarning = true;
                }
                if (!PickableOneEnabled()) pickableOneSettings = null;
                else if (pickableOneSettings != null)
                {
                    pickableOneSettings = Instantiate(pickableOneSettings);
                    _activeSettings = pickableOneSettings;
                    if (!pauseOnInteraction) pauseWarning = true;
                }
                if (!PickableTwoEnabled()) pickableTwoSettings = null;
                else if (pickableTwoSettings != null)
                {
                    pickableTwoSettings = Instantiate(pickableTwoSettings);
                    _activeSettings = pickableTwoSettings;
                    if (!pauseOnInteraction) pauseWarning = true;
                    multipleConnections = true;
                }
                if (!PushEnabled()) pushSettings = null;
                else if (pushSettings != null)
                {
                    pushSettings = Instantiate(pushSettings);
                    _activeSettings = pushSettings;
                    if (!pauseOnInteraction) pauseWarning = true;
                    multipleConnections = true;
                }
                if (!CoverEnabled()) coverSettings = null;
                else if (coverSettings != null) _activeSettings = coverSettings;

                if (!_activeSettings)
                {
                    Debug.LogWarning(this.name + "<b><color=red> has no settings file!</color></b>", this);
                    gameObject.SetActive(false);
                    return;
                }

                if (pauseWarning)
                {
                    Debug.Log("The interaction type for this InteractorObject typically needs the PauseOnInteraction option selected in the settings.If you intentionally opted not to do so, please disregard this message.", this);
                }
            }

            interactionType = (InteractionTypes)interaction;
            _useableEffectors = new bool[9];

            //Outline
            if (outlineMat)
            {
                thisMat = outlineMat.material;
                if (thisMat.HasProperty("_FirstOutlineColor"))
                {
                    //Instead of strings, we cache ids, much faster.
                    _propertyIdFirstOutline = Shader.PropertyToID("_FirstOutlineColor");
                    _firstOutline = thisMat.GetColor(_propertyIdFirstOutline);

                    _propertyIdSecondOutline = Shader.PropertyToID("_SecondOutlineColor");
                    _secondOutline = thisMat.GetColor(_propertyIdSecondOutline);

                    _hasOutlineMat = true;

                    _firstOutline.a = 0;
                    _secondOutline.a = 0;
                    thisMat.SetColor(_propertyIdFirstOutline, _firstOutline);
                    thisMat.SetColor(_propertyIdSecondOutline, _secondOutline);
                }
            }
            else if (GetComponentInParent<MeshRenderer>())
            {
                thisMat = GetComponentInParent<MeshRenderer>().material;
                if (thisMat.HasProperty("_FirstOutlineColor"))
                {
                    //Instead of strings, we cache ids, much faster.
                    _propertyIdFirstOutline = Shader.PropertyToID("_FirstOutlineColor");
                    _firstOutline = thisMat.GetColor(_propertyIdFirstOutline);

                    _propertyIdSecondOutline = Shader.PropertyToID("_SecondOutlineColor");
                    _secondOutline = thisMat.GetColor(_propertyIdSecondOutline);

                    _hasOutlineMat = true;

                    _firstOutline.a = 0;
                    _secondOutline.a = 0;
                    thisMat.SetColor(_propertyIdFirstOutline, _firstOutline);
                    thisMat.SetColor(_propertyIdSecondOutline, _secondOutline);
                }
            }

            col = GetComponent<Collider>();
            if (rigid = GetComponent<Rigidbody>())
                hasRigid = true;

            //Get all targets on children
            if (otherTargetsRoot != null)
            {
                childTargets = otherTargetsRoot.GetComponentsInChildren<InteractorTarget>();
                if (excludeTargets.Length > 0)
                    childTargets = ExcludedTargets(childTargets);
                for (int i = 0; i < childTargets.Length; i++)
                {
                    childTargets[i].intObj = this;
                }

                autoMovers = otherTargetsRoot.GetComponentsInChildren<AutoMover>();
                if (autoMovers != null && autoMovers.Length > 0)
                {
                    hasAutomover = true;
                }
            }
            else
            {
                childTargets = GetComponentsInChildren<InteractorTarget>();
                if (excludeTargets.Length > 0)
                    childTargets = ExcludedTargets(childTargets);
                for (int i = 0; i < childTargets.Length; i++)
                {
                    childTargets[i].intObj = this;
                }

                autoMovers = GetComponentsInChildren<AutoMover>();
                if (autoMovers != null && autoMovers.Length > 0)
                {
                    hasAutomover = true;
                }
            }

            interactiveSwitches = GetComponentsInChildren<InteractiveSwitch>();
            if (interactiveSwitches != null && interactiveSwitches.Length > 0)
            {
                hasInteractiveSwitch = true;
            }

            interactiveRotators = GetComponentsInChildren<InteractiveRotator>();
            if (interactiveRotators != null && interactiveRotators.Length > 0)
            {
                hasInteractiveRotator = true;
            }

            //ManualHit
            _hitHandler = GetComponentInChildren<HitHandler>();

            if (itemForInventory) itemForInventory.interactorObject = this;

            //If there is a info for this object, assign it as GameObject, deactivating it here for to be activated with interactions.
            Info(false);
        }

        public InteractorTarget[] ExcludedTargets(InteractorTarget[] interactorTargets)
        {
            List<InteractorTarget> newTargets = new List<InteractorTarget>(interactorTargets);
            for (int i = 0; i < excludeTargets.Length; i++)
            {
                int index = newTargets.IndexOf(excludeTargets[i]);
                if (index >= 0)
                {
                    newTargets.RemoveAt(index);
                }
            }
            return newTargets.ToArray();
        }

        private void Start()
        {
            //Needs to be on start for InstantiateRandomAreaPool because it instantiates its children on awake. For ManualForce Truck Example.
            if (interactionType == InteractionTypes.ManualForce)
            {
                //If there is a InstantiateRandomAreaPool component, add its prefabs to childTargets array, since they arent parented.
                InstantiateRandomAreaPool _pool;
                if ((_pool = GetComponent<InstantiateRandomAreaPool>()))
                {
                    //If there are already child targets as children, add spawned prefabs with copying arrays.
                    if (childTargets.Length != 0)
                    {
                        InteractorTarget[] childTargetsCopy = new InteractorTarget[childTargets.Length + _pool.maxPrefabCount];

                        for (int i = 0; i < childTargets.Length; i++)
                        {
                            childTargetsCopy[i] = childTargets[i];
                        }

                        for (int j = 0; j < _pool.maxPrefabCount; j++)
                        {
                            childTargetsCopy[j + childTargets.Length] = _pool._prefabList[j].GetComponent<InteractorTarget>();
                        }

                        childTargets = childTargetsCopy;
                    }
                    else
                    {
                        InteractorTarget[] childTargetsCopy = new InteractorTarget[_pool.maxPrefabCount];

                        for (int j = 0; j < _pool.maxPrefabCount; j++)
                        {
                            childTargetsCopy[j] = _pool._prefabList[j].GetComponent<InteractorTarget>();
                        }

                        childTargets = childTargetsCopy;
                    }
                }
            }
            if (pivot) _pivotLocalEulerCached = pivot.localRotation;

            _activeSettings.Init(this);
        }

        public void AddAnotherInteractorObjectManual(InteractorObject interactorObject)
        {
            if (!currentInteractor)
            {
                Debug.Log("This InteractorObject can not add " + interactorObject + " to Interactor because it is not currently in use by any Interactor.", this);
                return;
            }

            interactorObject.preventExit = true;
            currentInteractor.AddInteractionManual(interactorObject);
        }
        public void RemoveAnotherInteractorObjectManual(InteractorObject interactorObject)
        {
            if (!currentInteractor)
            {
                Debug.Log("This InteractorObject can not remove " + interactorObject + " from Interactor because it is not currently in use by any Interactor.", this);
                return;
            }

            interactorObject.preventExit = false;
            currentInteractor.RemoveInteractionManual(interactorObject);
        }

        //Assigns the Interactor which tries to use this so InteractorObject can know which Interactor is going to use now
        public void AssignInteractor(Interactor interactor)
        {
            currentInteractor = interactor;
        }
        //Used is true when used by effector, called by Interactor directly for turning off the outline material. 
        //Otherwise, it is only called by AddEffectorToUseables and RemoveEffectorFromUseables
        public void Prepare(bool On)
        {
            if (!ready && On)
            {
                Useable();
                ready = true;
            }
            else if(ready && On) return;

            if (ready && !On)
            {
                NotUseable();
                ready = false;
            }
        }
        //Outline and info texts
        public void Useable()
        {
            if (_hasOutlineMat)
            {
                _firstOutline.a = 0.6f;
                _secondOutline.a = 0.4f;
                thisMat.SetColor(_propertyIdFirstOutline, _firstOutline);
                thisMat.SetColor(_propertyIdSecondOutline, _secondOutline);
            }
            Info(true);
        }
        //Outline and info texts
        public void NotUseable()
        {
            if (_hasOutlineMat)
            {
                _firstOutline.a = 0;
                _secondOutline.a = 0;
                thisMat.SetColor(_propertyIdFirstOutline, _firstOutline);
                thisMat.SetColor(_propertyIdSecondOutline, _secondOutline);
            }
            Info(false);
            Reached = false;
        }
        public void Used(bool used)
        {
            this.used = used;
        }
        //Check if effector switched its toggle to useable for this object
        public bool CanEffectorUse(int effector)
        {
            return _useableEffectors[effector];
        }
        //Returns first useable effector other than input
        public int GetOtherUseableEffector(int effector)
        {
            for (int i = 0; i < _useableEffectors.Length; i++)
            {
                if (i == effector) continue;

                if (_useableEffectors[i])
                {
                    return i;
                }
            }
            return -1;
        }
        //Activates object prepares InteractorObject
        //Adds or removes effectors depending on their useability for this object
        public void AddEffectorToUseables(int effector)
        {
            if (effector < 0) return;

            _useableEffectors[effector] = true;
            Prepare(true);
        }
        //Adds effector, enables object if count amount of effectors in use.
        public void AddEffectorToUseables(int effector, int count)
        {
            if (effector < 0) return;

            _useableEffectors[effector] = true;
            if (UseableEffectorCount() >= count)
                Prepare(true);
        }
        public void RemoveEffectorFromUseables(int effector)
        {
            if (effector < 0)
            {
                ResetUseableEffectors();
                Prepare(false);
                return;
            }

            _useableEffectors[effector] = false;
            if (UseableEffectorCount() == 0)
            {
                Prepare(false);
                if (pivot && resetPivot) pivot.localRotation = _pivotLocalEulerCached;
            }
        }
        public void RemoveEffectorFromUseables(int effector, int count)
        {
            if (effector < 0)
            {
                ResetUseableEffectors();
                Prepare(false);
                return;
            }

            _useableEffectors[effector] = false;
            if (UseableEffectorCount() < count)
                Prepare(false);
        }
        //Returns how many effectors is in use position for this object
        public int UseableEffectorCount()
        {
            int count = 0;
            for (int i = 0; i < _useableEffectors.Length; i++)
            {
                if (_useableEffectors[i])
                {
                    count++;
                }
            }
            return count;
        }
        public void ResetUseableEffectors()
        {
            for (int i = 0; i < _useableEffectors.Length; i++)
            {
                _useableEffectors[i] = false;
            }
        }
        //Returns all target transforms in list for given effector type
        public List<Transform> GetTargetTransformsForEffectorType(int effectorType)
        {
            List<Transform> targetTransforms = new List<Transform>();
            for (int i = 0; i < childTargets.Length; i++)
            {
                if ((int)childTargets[i].effectorType == effectorType)
                {
                    targetTransforms.Add(childTargets[i].transform);
                }
            }
            return targetTransforms;
        }
        //Returns first InteractorTarget for given type
        public InteractorTarget GetTargetForEffectorType(int effectorType)
        {
            for (int i = 0; i < childTargets.Length; i++)
            {
                if ((int)childTargets[i].effectorType == effectorType)
                {
                    return childTargets[i];
                }
            }
            return null;
        }
        //Returns all targets for given effector type
        public InteractorTarget[] GetTargetsForEffectorType(int effectorType)
        {
            List<InteractorTarget> targets = new List<InteractorTarget>();
            for (int i = 0; i < childTargets.Length; i++)
            {
                if ((int)childTargets[i].effectorType == effectorType)
                {
                    targets.Add(childTargets[i]);
                }
            }
            return targets.ToArray();
        }
        //Checks if this InteractorObject has this effector type in children
        public bool HasEffectorTypeInTargets(int effectorType)
        {
            for (int i = 0; i < childTargets.Length; i++)
            {
                if ((int)childTargets[i].effectorType == effectorType)
                {
                    return true;
                }
            }
            return false;
        }
        public void RemoveTargetFromChildTargets(InteractorTarget removeTarget)
        {
            List<InteractorTarget> newChildTargets = new List<InteractorTarget>();
            for (int i = 0; i < childTargets.Length; i++)
            {
                if (childTargets[i] == removeTarget) continue;

                newChildTargets.Add(childTargets[i]);
            }
            childTargets = newChildTargets.ToArray();
        }
        public void DisableOtherTargets(Interactor.FullBodyBipedEffector effectorType)
        {
            for (int i = 0; i < childTargets.Length; i++)
            {
                if (childTargets[i].effectorType == effectorType)
                    childTargets[i].gameObject.SetActive(true);
                else childTargets[i].gameObject.SetActive(false);
            }
        }
        public void EnableOtherTargets(Interactor.FullBodyBipedEffector effectorType)
        {
            for (int i = 0; i < childTargets.Length; i++)
            {
                if (childTargets[i].effectorType == effectorType)
                    childTargets[i].gameObject.SetActive(false);
                else childTargets[i].gameObject.SetActive(true);
            }
        }

        public void RotatePivotToElbow(Transform rotatePivotTo)
        {
            if (pivot != null)
            {
                this.rotatePivotTo = rotatePivotTo;
                RotateToTransform(this.rotatePivotTo);
            }
        }
        private void RotateToTransform(Transform rotateToTransform)
        {
            RotateToV3(rotateToTransform.position);
        }
        public void RotateToV3(Vector3 rotateToPosition)
        {
            if (!pivot) return;

            _tempRotation = (pivot.position - rotateToPosition).normalized;
            Quaternion direction = Quaternion.LookRotation(_tempRotation, pivot.up);
            Quaternion originalPivot = pivot.rotation;
            pivot.rotation = direction;
            Vector3 newLocalEuler = pivot.localEulerAngles;
            pivot.rotation = originalPivot;

            if (!pivotX) newLocalEuler.x = _pivotLocalEulerCached.x;
            if (!pivotY) newLocalEuler.y = _pivotLocalEulerCached.y;
            if (!pivotZ) newLocalEuler.z = _pivotLocalEulerCached.z;

            pivot.localEulerAngles = newLocalEuler;
        } 
        public void KeepRotatePivot()
        {
            if (pivot && rotatePivotTo) RotateToTransform(rotatePivotTo);
        }

        public void AddManualChildInteraction(InteractorObject interactorObject)
        {//Adds other (probably a child in prefab) interactions to current interactor of this intObj
            if (!currentInteractor) return;

            currentInteractor.AddInteractionManual(interactorObject);
        }

        //Only used by Hit interaction, explained in Interactor.EffectorLink Update
        public void Hit(Transform interactionTarget, Vector3 effectorPos, Interactor interactor)
        {
            if (hitObjUseable)
            {
                _hitHandler.HitHandlerRotate(interactor);
            }

            if (used && !_hitHandler.moveOnce)
            {
                _hitHandler.moveOnce = true;
                hitDone = false;
                _hitHandler.HitPosMove(interactionTarget, effectorPos);
            }
        }
        //Only used by Hit interaction
        public void HitPosDefault(Transform interactionTarget, Vector3 effectorPos)
        {
            if (!_hitHandler.hitDone)
            {
                _hitHandler.HitPosDefault(interactionTarget, effectorPos);
            }
            else
            {
                hitDone = true;
            }
        }

        //Touch Method Entries for Passing this collider.
        public bool RaycastVertical(Transform playerTransform, out RaycastHit hit, bool left)
        {
            if (_activeSettings != touchSettings) 
            {
                Debug.LogWarning("This interaction is not set as Touch.", this);
                hit = new RaycastHit();
                return false;
            }
            return touchSettings.RaycastVertical(playerTransform, out hit, col, left);
        }
        public bool RaycastHorizontalUp(Transform playerTransform, out RaycastHit hit)
        {
            if (_activeSettings != touchSettings)
            {
                Debug.LogWarning("This interaction is not set as Touch.", this);
                hit = new RaycastHit();
                return false;
            }
            return touchSettings.RaycastHorizontalUp(playerTransform, out hit, col);
        }

        //You can delete this with its three references, just for info texts.
        private void Info(bool on)
        {
            if (toggleObject != null)
            {
                if (on)
                    toggleObject.SetActive(true);
                else
                    toggleObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {//Only PickableOne and PickableTwo
            if (!currentInteractor) return;
            if (_activeSettings != pickableOneSettings && _activeSettings != pickableTwoSettings) return;

            _activeSettings.UpdateSettings();
        }

        #region Gizmos and Inspector
        private void OnDrawGizmosSelected()
        {
            if (!currentInteractor || _activeSettings != pickableOneSettings) return;

            if (pickableOneSettings && pickableOneSettings.debug)
                pickableOneSettings.DrawBezierPathGizmos();
        }
        public bool ShowAnimationCurve()
        {
            if (easeType == EaseType.CustomCurve) return true;
            else return false;
        }
        public bool DefaultEnabled()
        {
            if (interaction >= 10 && interaction < 20) return true;
            else return false;
        }
        public bool ManualEnabled()
        {
            if (interaction >= 20 && interaction < 30) return true;
            else return false;
        }
        public bool TouchEnabled()
        {
            if (interaction >= 30 && interaction < 40) return true;
            else return false;
        }
        public bool DistanceEnabled()
        {
            if (interaction >= 40 && interaction < 50) return true;
            else return false;
        }
        public bool ClimbableEnabled()
        {
            if (interaction >= 50 && interaction < 60) return true;
            else return false;
        }
        public bool MultipleEnabled()
        {
            if (interaction >= 60 && interaction < 70) return true;
            else return false;
        }
        public bool SelfEnabled()
        {
            if (interaction >= 70 && interaction < 80) return true;
            else return false;
        }
        public bool PickableOneEnabled()
        {
            if (interaction >= 80 && interaction < 85) return true;
            else return false;
        }
        public bool PickableTwoEnabled()
        {
            if (interaction >= 85 && interaction < 90) return true;
            else return false;
        }
        public bool PushEnabled()
        {
            if (interaction >= 90 && interaction < 100) return true;
            else return false;
        }
        public bool CoverEnabled()
        {
            if (interaction >= 100 && interaction < 110) return true;
            else return false;
        }
        #endregion
    }
}
