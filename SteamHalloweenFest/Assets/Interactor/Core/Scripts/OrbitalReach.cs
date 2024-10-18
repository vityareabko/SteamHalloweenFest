using UnityEngine;
using System.Collections;

namespace razz
{
    [HelpURL("https://negengames.com/interactor/components.html#orbitalreachcs")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactor))]
    public class OrbitalReach : MonoBehaviour
    {
        [Tooltip("Assign the Interactor manually for the best practice.")]
        public Interactor interactor;
        [Tooltip("Assign the Animator manually for the best practice.")]
        public Animator animator;

        [Header("Calibration")]
        [Tooltip("Minimum height that your player can reach.")]
        [Range(0f, 1f)] public float minHeight = 0.0f;
        [Tooltip("Default height that your player can reach directly without moving its body up or down, usually around the chest area.")]
        [Range(0f, 2f)] public float midHeight = 1.2f;
        [Tooltip("Maximum height that your player can reach: For a 170-175cm character, 1.8 meters is recommended.)")]
        [Range(0f, 4f)] public float maxHeight = 1.8f;
        [Tooltip("Minimum distance to use OrbitalReach from player center. Objects closer than this value cannot be used because they are too close for using IK and orbital animations. The distance slightly changes for objects behind or in front of the player (allows for closer distances than this for frontal objects).")]
        [Range(0f, 1f)] public float minDist = 0.3f;
        [Tooltip("Maximum horizontal distance to use objects.")]
        [Range(0f, 2f)] public float maxDist = 0.8f;
        [Tooltip("Allows a small margin (in cm) to interact with objects slightly out of reach. Applies to both vertical height and horizontal distance.")]
        [Range(0f, 0.5f)] public float maxMargin = 0.05f;

        [Header("Add OrbitalReach Layer")]
        [Tooltip("Set your crouch height threshold in meters. It changes to crouch state to reach objects under this height. For example, if you set it to 0.7, player will lean to reach objects with height of 0.71m and will use crouch to reach objects with height of 0.69m.")]
        [Range(0f, 1f)] public float crouchSetStart = 0.7f;

        public static readonly string OrbitalLayer = "OrbitalReachLayer";
        public static readonly string OAngle = "orbitalAngle";
        public static readonly string OHeight = "orbitalHeight";
        public static readonly string OMirror = "orbitalMirror";
        public static readonly string OSpeed = "orbitalSpeed";
        public static readonly string OCycle = "orbitalCycle";
        public static readonly string DefaultStateName = "Wait for Interaction";
        public static readonly string StandStateName = "StandReach";
        public static readonly string CrouchStateName = "CrouchReach";
        public static readonly int OrbitalAngle = Animator.StringToHash(OAngle);
        public static readonly int OrbitalHeight = Animator.StringToHash(OHeight);
        public static readonly int OrbitalMirror = Animator.StringToHash(OMirror);
        public static readonly int OrbitalSpeed = Animator.StringToHash(OSpeed);
        public static readonly int OrbitalCycle = Animator.StringToHash(OCycle);
        public static readonly int DefaultState = Animator.StringToHash(DefaultStateName);
        public static readonly int StandState = Animator.StringToHash(StandStateName);
        public static readonly int CrouchState = Animator.StringToHash(CrouchStateName);

        [HideInInspector] public bool initiated;
        [HideInInspector] public bool clipPlaying;
        [HideInInspector] public float crouchWeightNormalized;
        private bool _resumed;
        private float _blendtreeDuration;
        private float _lastOrbitalTargetSpeed;
        private float _lastOrbitalBackSpeed;
        private float _lastWeight;
        private InteractorObject _currentIntObj;
        private int _orbitalLayerIndex;
        public int orbitalLayerIndex
        {
            get { return _orbitalLayerIndex; }
            set { _orbitalLayerIndex = value; }
        }

        [Header("Debug")]
        [Tooltip("These options needed only for debugging. This enables debug but also needs OrbitalPositioner first. Helps to see how your character reach with given heights/distances on OrbitalPositioner.")]
        public bool debug;
        [Tooltip("Add OrbitalPositioner to any InteractorObject and it will reposition it to use for debugging.")]
        public OrbitalPositioner debugOrbitalPositioner;
        [Tooltip("Changes hands and mirrors OrbitalReach animations.")]
        public bool debugMirror;
        [Tooltip("This is synced with same option on OrbitalPositioner. Sets the OrbitalReach animation time as normalized.")]
        [Range(0f, 1f)] public float debugDuration;
        [HideInInspector] public Vector3 debugOrbitalValues;

        private void Start()
        {
            Init();
        }
        private void Init()
        {
            if (!interactor)
            {
                interactor = GetComponent<Interactor>();
                Debug.LogWarning("Please assign Interactor to this OrbitalReach for best practice.", this);
            }
            if (!animator)
            {
                animator = GetComponentInChildren<Animator>();
                Debug.LogWarning("Please assign Animator to this OrbitalReach for best practice.", this);
            }
            if (!interactor || !animator)
            {
                initiated = false;
                Debug.LogWarning("Interactor or Animator could not be found. OrbitalReach is disabled.", this);
                return;
            }
            if (!CheckAnimator()) return;
            if (!animator.isHuman)
            {
                Debug.LogWarning("OrbitalReach only works for humanoid characters.", animator);
                return;
            }

            crouchWeightNormalized = -Mathf.InverseLerp(midHeight, minHeight, crouchSetStart);
            initiated = true;
            interactor.orbitalReach = this;
        }
        private bool CheckAnimator()
        {
            orbitalLayerIndex = -1;
            orbitalLayerIndex = animator.GetLayerIndex(OrbitalLayer);
            if (orbitalLayerIndex < 0)
            {
                Debug.LogWarning("OrbitalReach Layer could not be found on Animator Controller for OrbitalReach component! Please add OrbitalReach Layer after stopping the play mode.", this);
                return false;
            }
            return true;
        }

        public bool SetOrbitalReach(InteractorObject interactorObject, InteractorTarget interactorTarget, Vector3 orbitalValues, bool mirror, int arrayPlace, bool returning)
        {
            if (!initiated) return false;
            if (clipPlaying) return false;

            if (interactorObject.animAssistEnabled)
            {
                Debug.LogWarning("AnimAssist is disabled on InteractorObject because Orbital Reach does not support it.", interactorObject);
                interactorObject.animAssistEnabled = false;
            }

            if (interactorObject.pauseOnInteraction)
            {
                _currentIntObj = interactorObject;
                _resumed = false;
            }
            else _resumed = true;

            float lateStart = SetAnimatorParam(orbitalValues, mirror);
            if (interactorObject.targetDuration == 0) interactorObject.targetDuration = 0.001f;
            if (interactorObject.backDuration == 0) interactorObject.backDuration = 0.001f;
            float lateTargetDur = lateStart * interactorObject.targetDuration + interactorObject.orbitalLateStart;
            if (lateTargetDur < 0.1) lateTargetDur = 0.1f;
            float lateBackDur = lateStart * interactorObject.backDuration + interactorObject.orbitalLateEnd;
            if (lateBackDur < 0.1) lateBackDur = 0.1f;

            StartCoroutine(AnimCoroutine(interactorObject, interactorTarget, arrayPlace, lateTargetDur, lateBackDur, returning));

            /*if (animator.updateMode == AnimatorUpdateMode.Normal)
                StartCoroutine(AnimCoroutine(interactorObject, interactorTarget, arrayPlace, lateTargetDur, lateBackDur));
            else StartCoroutine(AnimCoroutineUnscaled(interactorObject, interactorTarget, arrayPlace, lateTargetDur, lateBackDur));*/

            return true;
        }
        public float SetAnimatorParam(Vector3 orbitalValues, bool mirror)
        {
            float lateStartLerped = 0.1f;
            float orbitAngle = orbitalValues.y;
            float orbitalCycle = 0.5f; //Mirrored anims cycle with 0.5 offset
            if (!mirror)
            {
                orbitAngle = -orbitAngle;
                orbitalCycle = 0f;
            }
            animator.SetFloat(OrbitalAngle, orbitAngle);
            animator.SetFloat(OrbitalHeight, orbitalValues.x);
            animator.SetFloat(OrbitalCycle, orbitalCycle);
            animator.SetBool(OrbitalMirror, mirror);
            animator.SetFloat(OrbitalSpeed, 1f);

            //animator.SetLayerWeight(orbitalLayerIndex, orbitalValues.z);
            float orbitalDist = Mathf.InverseLerp(minDist, maxDist, orbitalValues.z);
            float angleNormalized;
            float lerpedWeight = 0f;
            if (orbitAngle > 0.5f)
            {
                angleNormalized = Mathf.InverseLerp(1f, 0.5f, orbitAngle);
                lerpedWeight = Mathf.Lerp(GetRB(orbitalValues.x, orbitalDist, false), GetR(orbitalValues.x, orbitalDist, false), Mathf.Pow(angleNormalized, 3));

                lateStartLerped = Mathf.Lerp(GetRB(orbitalValues.x, orbitalDist, true), GetR(orbitalValues.x, orbitalDist, true), Mathf.Pow(angleNormalized, 3));
            }
            else if (orbitAngle > 0f)
            {
                angleNormalized = Mathf.InverseLerp(0.5f, 0f, orbitAngle);
                lerpedWeight = Mathf.Lerp(GetR(orbitalValues.x, orbitalDist, false), GetF(orbitalValues.x, orbitalDist, false), angleNormalized);

                lateStartLerped = Mathf.Lerp(GetR(orbitalValues.x, orbitalDist, true), GetF(orbitalValues.x, orbitalDist, true), angleNormalized);
            }
            else if (orbitAngle > -0.5f)
            {
                angleNormalized = Mathf.InverseLerp(-0.5f, -0f, orbitAngle);
                lerpedWeight = Mathf.Lerp(GetL(orbitalValues.x, orbitalDist, false), GetF(orbitalValues.x, orbitalDist, false), Mathf.Pow(angleNormalized, 2));

                lateStartLerped = Mathf.Lerp(GetL(orbitalValues.x, orbitalDist, true), GetF(orbitalValues.x, orbitalDist, true), Mathf.Pow(angleNormalized, 2));
            }
            else
            {
                angleNormalized = Mathf.InverseLerp(-1f, -0.5f, orbitAngle);
                lerpedWeight = Mathf.Lerp(GetLB(orbitalValues.x, orbitalDist, false), GetL(orbitalValues.x, orbitalDist, false), Mathf.Pow(angleNormalized, 3));

                lateStartLerped = Mathf.Lerp(GetLB(orbitalValues.x, orbitalDist, true), GetL(orbitalValues.x, orbitalDist, true), Mathf.Pow(angleNormalized, 3));
            }

            if (orbitalValues.x < crouchWeightNormalized) animator.CrossFade(CrouchState, 0f, orbitalLayerIndex, 0f);
            else animator.CrossFade(StandState, 0f, orbitalLayerIndex, 0f);

            animator.SetLayerWeight(orbitalLayerIndex, 0);
            _lastWeight = lerpedWeight;
            return lateStartLerped;
        }
        private IEnumerator AnimCoroutine(InteractorObject intObj, InteractorTarget interactorTarget, int arrayPlace, float lateTarget, float lateBack, bool returning)
        {
            clipPlaying = true;
            yield return new WaitForFixedUpdate();

            float intObjTargetDur = intObj.targetDuration;
            float intObjBackDur = intObj.backDuration;
            if (returning) intObjTargetDur = intObjBackDur;

            _blendtreeDuration = animator.GetCurrentAnimatorStateInfo(orbitalLayerIndex).length;
            _lastOrbitalTargetSpeed = _blendtreeDuration / (intObjTargetDur + lateTarget) * 0.5f;
            _lastOrbitalBackSpeed = _blendtreeDuration / (intObjBackDur + lateBack) * 0.5f;
            animator.SetFloat(OrbitalSpeed, _lastOrbitalTargetSpeed);

            float startTime = Time.time;
            while (Time.time < startTime + lateTarget)
            {
                float t = (Time.time - startTime) / lateTarget;
                float lerpedWeight = Mathf.Lerp(0, _lastWeight, t);
                animator.SetLayerWeight(orbitalLayerIndex, lerpedWeight);
                yield return null;
            }

            interactor.StartStopInteractionOrbital(intObj, interactorTarget, arrayPlace);
            if (returning) intObj.pickableOneSettings.Drop();

            yield return new WaitForSeconds(intObjTargetDur);

            if (returning)
            {
                animator.SetFloat(OrbitalSpeed, -_lastOrbitalBackSpeed);
                startTime = Time.time;
                while (Time.time < startTime + lateBack)
                {
                    float t = (Time.time - startTime) / lateBack;
                    float lerpedWeight = Mathf.Lerp(_lastWeight, 0, t);
                    animator.SetLayerWeight(orbitalLayerIndex, lerpedWeight);
                    yield return null;
                }
                clipPlaying = false;
                yield break;
            }

            if (intObj.pauseOnInteraction)
            {
                animator.SetFloat(OrbitalSpeed, 0f);
                yield return new WaitUntil(() => DidResumed(intObj));
                yield return new WaitForFixedUpdate();
            }
            animator.SetFloat(OrbitalSpeed, _lastOrbitalBackSpeed);
            yield return new WaitForSeconds(intObjBackDur);

            startTime = Time.time;
            while (Time.time < startTime + lateBack)
            {
                float t = (Time.time - startTime) / lateBack;
                float lerpedWeight = Mathf.Lerp(_lastWeight, 0, t);
                animator.SetLayerWeight(orbitalLayerIndex, lerpedWeight);
                yield return null;
            }
            clipPlaying = false;
        }
        private IEnumerator AnimCoroutineUnscaled(InteractorObject intObj, InteractorTarget interactorTarget, int arrayPlace, float lateTarget, float lateBack)
        {
            clipPlaying = true;
            yield return new WaitForFixedUpdate();
            _blendtreeDuration = animator.GetCurrentAnimatorStateInfo(orbitalLayerIndex).length;
            _lastOrbitalTargetSpeed = _blendtreeDuration / (intObj.targetDuration + lateTarget) * 0.5f;
            _lastOrbitalBackSpeed = _blendtreeDuration / (intObj.backDuration + lateBack) * 0.5f;
            animator.SetFloat(OrbitalSpeed, _lastOrbitalTargetSpeed);

            float startTime = Time.unscaledTime;
            while (Time.unscaledTime < startTime + lateTarget)
            {
                float t = (Time.unscaledTime - startTime) / lateTarget;
                float lerpedWeight = Mathf.Lerp(0, _lastWeight, t);
                animator.SetLayerWeight(orbitalLayerIndex, lerpedWeight);
                yield return null;
            }

            interactor.StartStopInteractionOrbital(intObj, interactorTarget, arrayPlace);

            yield return new WaitForSecondsRealtime(intObj.targetDuration);
            if (intObj.pauseOnInteraction)
            {
                animator.SetFloat(OrbitalSpeed, 0f);
                bool exitState = intObj.preventExit;
                intObj.preventExit = true;
                yield return new WaitUntil(() => DidResumed(intObj));
                intObj.preventExit = exitState;
                yield return new WaitForFixedUpdate();
            }
            animator.SetFloat(OrbitalSpeed, _lastOrbitalBackSpeed);
            yield return new WaitForSecondsRealtime(intObj.backDuration);

            startTime = Time.unscaledTime;
            while (Time.unscaledTime < startTime + lateBack)
            {
                float t = (Time.unscaledTime - startTime) / lateBack;
                float lerpedWeight = Mathf.Lerp(_lastWeight, 0, t);
                animator.SetLayerWeight(orbitalLayerIndex, lerpedWeight);
                yield return null;
            }
            clipPlaying = false;
        }
        public bool DidResumed(InteractorObject intObj)
        {
            if (!intObj || !intObj.gameObject.activeInHierarchy)
            {
                _resumed = true;
                return _resumed;
            }

            return _resumed;
        }
        public void ResumeOrbitalAnim(InteractorObject intObj) //TODO resume without check?
        {
            if (intObj == _currentIntObj) _resumed = true;
        }
        public void ReverseOrbitalAnim(InteractorObject intObj)
        {
            if (intObj == _currentIntObj)
            {
                _lastOrbitalTargetSpeed *= -2f;
                animator.SetFloat(OrbitalSpeed, _lastOrbitalTargetSpeed);
                _resumed = true;
                clipPlaying = false;
                StopCoroutine("AnimCoroutine");
                StartCoroutine(ReverseAnim());
            }
        }
        private IEnumerator ReverseAnim()
        {
            yield return new WaitForEndOfFrame();
            float startTime = Time.time;
            while (Time.time < startTime + (_blendtreeDuration * 0.25f))
            {
                float t = (Time.time - startTime) / (_blendtreeDuration * 0.25f);
                float lerpedWeight = Mathf.Lerp(_lastWeight, 0, t);
                animator.SetLayerWeight(orbitalLayerIndex, lerpedWeight);
                yield return null;
            }
            animator.CrossFade(DefaultState, 0f, orbitalLayerIndex, 0f);
        }
        public void SetLayerWeightToZero()
        {
            animator.SetLayerWeight(orbitalLayerIndex, 0);
        }
        public void SetLayerWeightToLast()
        {
            animator.SetLayerWeight(orbitalLayerIndex, _lastWeight);
        }

        private readonly float[] rbWeightsH = new float[25] { 0.60f, 0.61f, 0.63f, 0.64f, 0.65f, 0.67f, 0.68f, 0.69f, 0.71f, 0.72f, 0.73f, 0.75f, 0.76f, 0.77f, 0.79f, 0.81f, 0.82f, 0.84f, 0.85f, 0.87f, 0.89f, 0.91f, 0.93f, 0.95f, 0.97f };
        private readonly float[] rWeightsH = new float[25] { 0.07f, 0.09f, 0.11f, 0.14f, 0.16f, 0.19f, 0.21f, 0.24f, 0.27f, 0.30f, 0.33f, 0.36f, 0.39f, 0.43f, 0.46f, 0.50f, 0.54f, 0.57f, 0.61f, 0.65f, 0.69f, 0.73f, 0.77f, 0.82f, 0.87f };
        private readonly float[] fWeightsH = new float[25] { 0.07f, 0.10f, 0.12f, 0.15f, 0.18f, 0.21f, 0.24f, 0.27f, 0.30f, 0.33f, 0.36f, 0.39f, 0.43f, 0.46f, 0.49f, 0.52f, 0.55f, 0.59f, 0.62f, 0.66f, 0.70f, 0.75f, 0.80f, 0.87f, 0.95f };
        private readonly float[] lWeightsH = new float[25] { 0.50f, 0.52f, 0.53f, 0.55f, 0.56f, 0.58f, 0.60f, 0.61f, 0.63f, 0.64f, 0.66f, 0.68f, 0.69f, 0.71f, 0.73f, 0.75f, 0.77f, 0.79f, 0.81f, 0.83f, 0.85f, 0.88f, 0.90f, 0.93f, 0.96f };
        private readonly float[] lbWeightsH = new float[25] { 0.70f, 0.71f, 0.72f, 0.73f, 0.73f, 0.74f, 0.75f, 0.76f, 0.77f, 0.78f, 0.79f, 0.80f, 0.81f, 0.82f, 0.83f, 0.85f, 0.86f, 0.87f, 0.88f, 0.90f, 0.91f, 0.93f, 0.94f, 0.96f, 0.98f };
        private readonly float[] rbWeightsM = new float[25] { 0.60f, 0.61f, 0.63f, 0.64f, 0.65f, 0.67f, 0.68f, 0.69f, 0.71f, 0.72f, 0.74f, 0.75f, 0.77f, 0.78f, 0.80f, 0.81f, 0.83f, 0.84f, 0.86f, 0.88f, 0.90f, 0.92f, 0.94f, 0.95f, 0.98f };
        private readonly float[] rWeightsM = new float[25] { 0.07f, 0.10f, 0.14f, 0.17f, 0.20f, 0.23f, 0.27f, 0.30f, 0.34f, 0.37f, 0.41f, 0.45f, 0.48f, 0.52f, 0.55f, 0.59f, 0.63f, 0.66f, 0.69f, 0.72f, 0.75f, 0.79f, 0.83f, 0.87f, 0.93f };
        private readonly float[] fWeightsM = new float[25] { 0.07f, 0.10f, 0.14f, 0.17f, 0.20f, 0.23f, 0.26f, 0.29f, 0.32f, 0.35f, 0.39f, 0.42f, 0.46f, 0.49f, 0.53f, 0.56f, 0.60f, 0.63f, 0.67f, 0.71f, 0.75f, 0.80f, 0.84f, 0.89f, 0.94f };
        private readonly float[] lWeightsM = new float[25] { 0.60f, 0.61f, 0.63f, 0.64f, 0.65f, 0.67f, 0.68f, 0.70f, 0.71f, 0.73f, 0.74f, 0.75f, 0.77f, 0.78f, 0.80f, 0.82f, 0.83f, 0.85f, 0.86f, 0.88f, 0.90f, 0.92f, 0.94f, 0.96f, 0.98f };
        private readonly float[] lbWeightsM = new float[25] { 0.80f, 0.81f, 0.81f, 0.82f, 0.83f, 0.83f, 0.84f, 0.85f, 0.86f, 0.86f, 0.87f, 0.88f, 0.89f, 0.89f, 0.90f, 0.91f, 0.92f, 0.93f, 0.93f, 0.94f, 0.95f, 0.96f, 0.97f, 0.98f, 0.99f };
        private readonly float[] rbWeightsL = new float[25] { 0.50f, 0.54f, 0.56f, 0.58f, 0.60f, 0.62f, 0.64f, 0.66f, 0.68f, 0.70f, 0.72f, 0.73f, 0.75f, 0.77f, 0.79f, 0.81f, 0.83f, 0.84f, 0.86f, 0.88f, 0.90f, 0.92f, 0.94f, 0.96f, 0.98f };
        private readonly float[] rWeightsL = new float[25] { 0.20f, 0.24f, 0.28f, 0.32f, 0.35f, 0.39f, 0.43f, 0.46f, 0.50f, 0.54f, 0.58f, 0.60f, 0.63f, 0.65f, 0.68f, 0.70f, 0.73f, 0.76f, 0.79f, 0.82f, 0.84f, 0.87f, 0.90f, 0.93f, 0.96f };
        private readonly float[] fWeightsL = new float[25] { 0.20f, 0.23f, 0.26f, 0.30f, 0.33f, 0.36f, 0.40f, 0.43f, 0.46f, 0.50f, 0.53f, 0.56f, 0.60f, 0.63f, 0.66f, 0.69f, 0.72f, 0.74f, 0.77f, 0.80f, 0.83f, 0.86f, 0.90f, 0.93f, 0.96f };
        private readonly float[] lWeightsL = new float[25] { 0.70f, 0.71f, 0.72f, 0.74f, 0.75f, 0.76f, 0.77f, 0.78f, 0.80f, 0.81f, 0.82f, 0.83f, 0.84f, 0.86f, 0.87f, 0.88f, 0.89f, 0.90f, 0.92f, 0.93f, 0.94f, 0.95f, 0.96f, 0.97f, 0.99f };
        private readonly float[] lbWeightsL = new float[25] { 0.85f, 0.86f, 0.86f, 0.87f, 0.87f, 0.88f, 0.88f, 0.89f, 0.90f, 0.90f, 0.91f, 0.91f, 0.92f, 0.92f, 0.93f, 0.94f, 0.94f, 0.95f, 0.95f, 0.96f, 0.97f, 0.97f, 0.98f, 0.99f, 0.99f };
        private readonly float[] rbWeightsC = new float[25] { 0.70f, 0.71f, 0.72f, 0.73f, 0.75f, 0.76f, 0.77f, 0.78f, 0.79f, 0.80f, 0.81f, 0.82f, 0.84f, 0.85f, 0.86f, 0.87f, 0.88f, 0.90f, 0.91f, 0.92f, 0.93f, 0.95f, 0.96f, 0.97f, 0.99f };
        private readonly float[] rWeightsC = new float[25] { 0.70f, 0.71f, 0.73f, 0.74f, 0.76f, 0.77f, 0.79f, 0.80f, 0.81f, 0.82f, 0.83f, 0.84f, 0.85f, 0.86f, 0.87f, 0.88f, 0.89f, 0.91f, 0.92f, 0.93f, 0.94f, 0.95f, 0.96f, 0.97f, 0.99f };
        private readonly float[] fWeightsC = new float[25] { 0.70f, 0.71f, 0.73f, 0.74f, 0.75f, 0.76f, 0.77f, 0.78f, 0.79f, 0.81f, 0.82f, 0.83f, 0.84f, 0.85f, 0.86f, 0.88f, 0.89f, 0.90f, 0.91f, 0.92f, 0.94f, 0.95f, 0.96f, 0.97f, 0.99f };
        private readonly float[] lWeightsC = new float[25] { 0.70f, 0.71f, 0.72f, 0.73f, 0.74f, 0.75f, 0.76f, 0.78f, 0.79f, 0.80f, 0.81f, 0.82f, 0.83f, 0.85f, 0.86f, 0.87f, 0.88f, 0.89f, 0.91f, 0.92f, 0.93f, 0.94f, 0.96f, 0.97f, 0.99f };
        private readonly float[] lbWeightsC = new float[25] { 0.70f, 0.71f, 0.72f, 0.74f, 0.75f, 0.76f, 0.77f, 0.78f, 0.80f, 0.81f, 0.82f, 0.83f, 0.85f, 0.86f, 0.87f, 0.88f, 0.90f, 0.91f, 0.92f, 0.93f, 0.94f, 0.95f, 0.96f, 0.97f, 0.99f };


        private float GetRB(float heightNormalized, float distance, bool lateStartValue)
        {
            if(heightNormalized >= 0)
            {
                if (lateStartValue) return 0.5f;

                float lerpHeight = Mathf.InverseLerp(1f, 0f, heightNormalized);
                return Mathf.Lerp(rbWeightsH[ClosestIndex(distance, rbWeightsH)], rbWeightsM[ClosestIndex(distance, rbWeightsM)], lerpHeight);
            }
            else if (heightNormalized >= crouchWeightNormalized)
            {
                if (lateStartValue) return 0.55f;

                float lerpHeight = Mathf.InverseLerp(0f, crouchWeightNormalized, heightNormalized);
                return Mathf.Lerp(rbWeightsM[ClosestIndex(distance, rbWeightsM)], rbWeightsL[ClosestIndex(distance, rbWeightsL)], lerpHeight);
            }
            else
            {
                if (lateStartValue) return 0.7f;

                float lerpHeight = Mathf.InverseLerp(crouchWeightNormalized, -1f, heightNormalized);
                return Mathf.Lerp(rbWeightsC[0], rbWeightsC[ClosestIndex(distance, rbWeightsC)], lerpHeight);
            }
        }
        private float GetR(float heightNormalized, float distance, bool lateStartValue)
        {
            if (heightNormalized >= 0)
            {
                if (lateStartValue) return 0.1f;

                float lerpHeight = Mathf.InverseLerp(1f, 0f, heightNormalized);
                return Mathf.Lerp(rWeightsH[ClosestIndex(distance, rWeightsH)], rWeightsM[ClosestIndex(distance, rWeightsM)], lerpHeight);
            }
            else if (heightNormalized >= crouchWeightNormalized)
            {
                if (lateStartValue) return 0.15f;

                float lerpHeight = Mathf.InverseLerp(0f, crouchWeightNormalized, heightNormalized);
                return Mathf.Lerp(rWeightsM[ClosestIndex(distance, rWeightsM)], rWeightsL[ClosestIndex(distance, rWeightsL)], lerpHeight);
            }
            else
            {
                if (lateStartValue) return 0.6f;

                float lerpHeight = Mathf.InverseLerp(crouchWeightNormalized, -1f, heightNormalized);
                return Mathf.Lerp(rWeightsC[0], rWeightsC[ClosestIndex(distance, rWeightsC)], lerpHeight);
            }
        }
        private float GetF(float heightNormalized, float distance, bool lateStartValue)
        {
            if (heightNormalized >= 0)
            {
                if (lateStartValue) return 0.1f;

                float lerpHeight = Mathf.InverseLerp(1f, 0f, heightNormalized);
                return Mathf.Lerp(fWeightsH[ClosestIndex(distance, fWeightsH)], fWeightsM[ClosestIndex(distance, fWeightsM)], lerpHeight);
            }
            else if (heightNormalized >= crouchWeightNormalized)
            {
                if (lateStartValue) return 0.15f;

                float lerpHeight = Mathf.InverseLerp(0f, crouchWeightNormalized, heightNormalized);
                return Mathf.Lerp(fWeightsM[ClosestIndex(distance, fWeightsM)], fWeightsL[ClosestIndex(distance, fWeightsL)], lerpHeight);
            }
            else
            {
                if (lateStartValue) return 0.6f;

                float lerpHeight = Mathf.InverseLerp(crouchWeightNormalized, -1f, heightNormalized);
                return Mathf.Lerp(fWeightsC[0], fWeightsC[ClosestIndex(distance, fWeightsC)], lerpHeight);
            }
        }
        private float GetL(float heightNormalized, float distance, bool lateStartValue)
        {
            if (heightNormalized >= 0)
            {
                if (lateStartValue) return 0.6f;

                float lerpHeight = Mathf.InverseLerp(1f, 0f, heightNormalized);
                return Mathf.Lerp(lWeightsH[ClosestIndex(distance, lWeightsH)], lWeightsM[ClosestIndex(distance, lWeightsM)], lerpHeight);
            }
            else if (heightNormalized >= crouchWeightNormalized)
            {
                if (lateStartValue) return 0.75f;

                float lerpHeight = Mathf.InverseLerp(0f, crouchWeightNormalized, heightNormalized);
                return Mathf.Lerp(lWeightsM[ClosestIndex(distance, lWeightsM)], lWeightsL[ClosestIndex(distance, lWeightsL)], lerpHeight);
            }
            else
            {
                if (lateStartValue) return 0.6f;

                float lerpHeight = Mathf.InverseLerp(crouchWeightNormalized, -1f, heightNormalized);
                return Mathf.Lerp(lWeightsC[0], lWeightsC[ClosestIndex(distance, lWeightsC)], lerpHeight);
            }
        }
        private float GetLB(float heightNormalized, float distance, bool lateStartValue)
        {
            if (heightNormalized >= 0)
            {
                if (lateStartValue) return 1.5f;

                float lerpHeight = Mathf.InverseLerp(1f, 0f, heightNormalized);
                return Mathf.Lerp(lbWeightsH[ClosestIndex(distance, lbWeightsH)], lbWeightsM[ClosestIndex(distance, lbWeightsM)], lerpHeight);
            }
            else if (heightNormalized >= crouchWeightNormalized)
            {
                if (lateStartValue) return 1.8f;

                float lerpHeight = Mathf.InverseLerp(0f, crouchWeightNormalized, heightNormalized);
                return Mathf.Lerp(lbWeightsM[ClosestIndex(distance, lbWeightsM)], lbWeightsL[ClosestIndex(distance, lbWeightsL)], lerpHeight);
            }
            else
            {
                if (lateStartValue) return 1.2f;

                float lerpHeight = Mathf.InverseLerp(crouchWeightNormalized, -1f, heightNormalized);
                return Mathf.Lerp(lbWeightsC[0], lbWeightsC[ClosestIndex(distance, lbWeightsC)], lerpHeight);
            }
        }

        private int ClosestIndex(float distance, float[] array)
        {
            int closest = 0;
            float minDiff = Mathf.Infinity;
            for (int i = 0; i < array.Length; i++)
            {
                float diff = Mathf.Abs(array[i] - distance);
                if (diff < minDiff)
                {
                    closest = i;
                    minDiff = diff;
                }
            }
            return closest;
        }
    }
}
