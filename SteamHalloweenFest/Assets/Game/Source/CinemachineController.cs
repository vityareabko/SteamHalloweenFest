using Cinemachine;
using UnityEngine;
using Zenject;

namespace Game.Source
{
    public class CinemachineController : MonoBehaviour
    {
        public CinemachineVirtualCamera _cinemachine;

        [Inject] private void Construct(IPLayer player) => _cinemachine.Follow = player.CameraTarget;
    }
}