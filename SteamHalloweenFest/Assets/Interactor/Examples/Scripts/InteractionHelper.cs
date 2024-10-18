using UnityEngine;
using razz;

public class InteractionHelper : MonoBehaviour
{
    public bool pingPong;
    public EaseType easeType;

    [Header("ToggleRotate")]
    public Transform rotateObj;
    public Vector3 rotateAmount;
    public float rotationDuration = 0.5f;

    [Header("ToggleMove")]
    public Transform moveObj;
    public Vector3 moveAmount;
    public float moveDuration = 0.5f;

    [Header("Call Interaction After Duration")]
    public Interactor interactor;
    public InteractorObject interactorObject;
    public bool callOnFirstUse;
    public bool callOnSecondUse;

    private bool _rotating;
    private bool _rotated;
    private Vector3 _cachedRotation;
    private float _elapsedTime;

    private bool _moving;
    private bool _moved;
    private Vector3 _cachedLocalPos;

    private void Start()
    {
        if (moveObj) _cachedLocalPos = moveObj.localPosition;
        if (rotateObj) _cachedRotation = rotateObj.localEulerAngles;
    }

    private void Update()
    {
        if (_rotating) RotateUntilDone();
        if (_moving) MoveUntilDone();
    }

    [ContextMenu("ToggleRotate")]
    public void ToggleRotate()
    {
        _elapsedTime = 0f;
        _rotating = true;
    }

    private void RotateUntilDone()
    {
        if (!_rotated)
        {
            if (_elapsedTime < rotationDuration)
            {
                float t = Mathf.Clamp01(Ease.FromType(easeType)(_elapsedTime / rotationDuration));
                rotateObj.localEulerAngles = Vector3.Lerp(_cachedRotation, _cachedRotation + rotateAmount, t);
                _elapsedTime += Time.deltaTime;
            }
            else
            {
                _rotated = true;

                if (!pingPong) _rotating = false;
                else _elapsedTime = 0;

                if (interactorObject && callOnFirstUse)
                {
                    if (interactor.IsInteractingWith(interactorObject))
                    {
                        interactor.StartStopInteraction(interactorObject);
                    }
                }
            }
        }
        else
        {
            if (_elapsedTime < rotationDuration)
            {
                float t = Mathf.Clamp01(Ease.FromType(easeType)(_elapsedTime / rotationDuration));
                rotateObj.localEulerAngles = Vector3.Lerp(_cachedRotation + rotateAmount, _cachedRotation, t);
                _elapsedTime += Time.deltaTime;
            }
            else
            {
                _rotated = false;
                _rotating = false;

                if (interactorObject && callOnSecondUse)
                {
                    if (interactor.IsInteractingWith(interactorObject))
                    {
                        interactor.StartStopInteraction(interactorObject);
                    }
                }
            }
        }
    }

    [ContextMenu("ToggleMove")]
    public void ToggleMove()
    {
        _elapsedTime = 0f;
        _moving = true;
    }

    private void MoveUntilDone()
    {
        if (!_moved)
        {
            if (_elapsedTime < moveDuration)
            {
                float t = Mathf.Clamp01(Ease.FromType(easeType)(_elapsedTime / moveDuration));
                moveObj.localPosition = Vector3.Lerp(_cachedLocalPos, _cachedLocalPos + moveAmount, t);
                _elapsedTime += Time.deltaTime;
            }
            else
            {
                _moved = true;
                if (!pingPong) _moving = false;
                else _elapsedTime = 0;

                if (interactorObject && callOnFirstUse)
                {
                    if (interactor.IsInteractingWith(interactorObject))
                    {
                        interactor.StartStopInteraction(interactorObject);
                    }
                }
            }
        }
        else
        {
            if (_elapsedTime < moveDuration)
            {
                float t = Mathf.Clamp01(Ease.FromType(easeType)(_elapsedTime / moveDuration));
                moveObj.localPosition = Vector3.Lerp(_cachedLocalPos + moveAmount, _cachedLocalPos, t);
                _elapsedTime += Time.deltaTime;
            }
            else
            {
                _moved = false;
                _moving = false;

                if (interactorObject && callOnSecondUse)
                {
                    if (interactor.IsInteractingWith(interactorObject))
                    {
                        interactor.StartStopInteraction(interactorObject);
                    }
                }
            }
        }
    }
}
