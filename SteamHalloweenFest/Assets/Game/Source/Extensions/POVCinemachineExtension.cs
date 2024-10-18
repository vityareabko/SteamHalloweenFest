using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class POVCinemachineExtension : CinemachineExtension
{
    [SerializeField] private float clampAngleMin = -80f;
    [SerializeField] private float clampAngleMax = 0f;
    [SerializeField] private float verticalSpeed = 1f;
    [SerializeField] private float horizontalSpeed = 1f;

    private PlayerInputActions inputActions;
    private Vector3 startingRotation;

    protected override void Awake() 
    {
        inputActions = new PlayerInputActions();
        
        if (VirtualCamera != null)
        {
            startingRotation = VirtualCamera.transform.rotation.eulerAngles;
        }

        base.Awake();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        if (vcam.Follow && stage == CinemachineCore.Stage.Aim)
        {
            // Читаем ввод из нового Input System
            Vector2 deltaInput = inputActions.Player.Look.ReadValue<Vector2>();

            // Обновляем повороты на основе ввода
            startingRotation.x += -deltaInput.y * verticalSpeed * deltaTime;
            startingRotation.y += deltaInput.x * horizontalSpeed * deltaTime;

            // Ограничиваем вращение по вертикали
            startingRotation.x = Mathf.Clamp(startingRotation.x, clampAngleMin, clampAngleMax);

            // Приводим углы к диапазону 0-360 градусов, чтобы избежать переполнения
            startingRotation.y = NormalizeAngle(startingRotation.y);

            // Применяем новое вращение к камере
            state.RawOrientation = Quaternion.Euler(startingRotation.x, startingRotation.y, 0f);
        }
    }

    // Нормализация углов для предотвращения дерганий
    private float NormalizeAngle(float angle)
    {
        while (angle < 0f) angle += 360f;
        while (angle > 360f) angle -= 360f;
        return angle;
    }
}
