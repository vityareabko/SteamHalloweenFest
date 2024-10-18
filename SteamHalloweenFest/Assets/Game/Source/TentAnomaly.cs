using UnityEngine;
using Zenject;

public class TentAnomaly : MonoBehaviour
{
    private const float MaxFronAnomalyDeparture = 80;
    private const float MinDistanceToMove = 2.5f;
    private const float MaxTentInsideScaleZ = 50;
    private const float BackTentDistanceThreshold = 5f;
    private const float BackTentFixedDistance = 5f;
    
    [SerializeField] private Transform _tentFront;
    [SerializeField] private Transform _tentBack;
    [SerializeField] private Transform _tentInside;
    
    [SerializeField] private float _multiplayer = 1.2f;
    
    private IPLayer _player;
    private Vector3 _startPlayerPos;
    
    private float _lastTentPosition;
    private float _lastTentBackZ;
    private float _initialTentDistanceFrontAndBack; 
    private float _smoothTime = 0.1f;  
    private float _velocityZ = 0.0f;
    
    private bool _anomalyHasFinished;

    [Inject] private void Construct(IPLayer player)
    {
        _player = player;
    }

    private void Awake()
    {
        _startPlayerPos = _player.Transform.position;
        _lastTentPosition = _tentFront.position.z;
        _lastTentBackZ = _tentBack.position.z;
        
        _initialTentDistanceFrontAndBack = _tentFront.position.z - _tentBack.position.z;

    }

   private void Update()
    {
        if (_player == null)
        {
            Debug.LogWarning("Player is not assigned!");
            return;
        }
    
        if (_anomalyHasFinished)
            return;
        
        float distanceTravelled = Vector3.Distance(_startPlayerPos, _player.Transform.position);

        // if (distanceTravelled > BackTentDistanceThreshold)
        // {
        //     float targetBackTentZ = _player.Transform.position.z - BackTentFixedDistance;
        //     
        //     if (targetBackTentZ > _lastTentBackZ && (_tentFront.position.z - targetBackTentZ >= _initialTentDistanceFrontAndBack))
        //     {
        //         _lastTentBackZ = Mathf.SmoothDamp(_lastTentBackZ, targetBackTentZ, ref _velocityZ, 0.1f);
        //
        //         Vector3 newTentBackPosition = _tentBack.position;
        //         newTentBackPosition.z = _lastTentBackZ;
        //         _tentBack.position = newTentBackPosition;
        //     }
        // }
    }

   private void FixedUpdate()
   {
       if (_anomalyHasFinished)
           return;
       
       float distanceTravelled = Vector3.Distance(_startPlayerPos, _player.Transform.position);
       
       if (distanceTravelled > MinDistanceToMove)
       {
           float tentFrontTargetDistance = Mathf.Min(distanceTravelled * _multiplayer, MaxFronAnomalyDeparture);

           if (tentFrontTargetDistance > _lastTentPosition - _tentInside.position.z)
           {
               float frontVelocityZ = 0.0f;  
               Vector3 newTentFrontPosition = _tentFront.position;
               newTentFrontPosition.z = Mathf.SmoothDamp(_tentFront.position.z, _tentInside.position.z + tentFrontTargetDistance, ref frontVelocityZ, _smoothTime);

               _tentFront.position = newTentFrontPosition;
               _lastTentPosition = newTentFrontPosition.z;

               float scaleProgress = distanceTravelled * _multiplayer / MaxFronAnomalyDeparture;
               float targetScaleZ = Mathf.Lerp(_tentInside.localScale.z, MaxTentInsideScaleZ * Mathf.Clamp(scaleProgress, 0f, 1f), 0.1f);

               Vector3 newTentInsideScale = _tentInside.localScale;
               newTentInsideScale.z = targetScaleZ;
               _tentInside.localScale = newTentInsideScale;
               _lastTentPosition = newTentFrontPosition.z;
           }
           else
           {
               _anomalyHasFinished = true;
           }
       }
   }
}
