using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private Player _playerPrefab;
    
    public override void InstallBindings()
    {
        var player = Instantiate(_playerPrefab, _playerSpawnPoint);
        Container.BindInterfacesAndSelfTo<Player>().FromInstance(player).AsSingle();
    }
}
