using System;
using UnityEngine;

public interface IPLayer
{
    Transform Transform { get; }
}

public class Player : MonoBehaviour, IPLayer
{
    public Transform Transform => transform;
    public Camera _camera;
    private PlayerInteraction _playerInteraction;

    private void Awake()
    {
        _playerInteraction = new PlayerInteraction(_camera);
    }

    private void Update()
    {
        _playerInteraction.Tick();
    }
}

public class PlayerInteraction
{
    private float _interactionDistance = 3f;  
    private LayerMask _interactableLayer;     
    private Color _outlineColor = Color.white;  
    private float _outlineWidth = 8f;           

    private Camera _camera;
    private GameObject _highlightedObject;  
    private Outline _currentOutline;

    public PlayerInteraction(Camera camera)
    {
        _camera = camera;
        _interactableLayer = LayerMask.GetMask(Layers.Interactable);
    }

    public void Tick()
    {
        HandleHighlighting();
    }

    void HandleHighlighting()
    {
        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _interactionDistance, _interactableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (_highlightedObject != hitObject)
            {
                RemoveOutline(); 
                
                _highlightedObject = hitObject;
                AddOutline(_highlightedObject);
            }
        }
        else
        {
            RemoveOutline();
        }
    }

    void AddOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }
        
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = _outlineColor;
        outline.OutlineWidth = _outlineWidth;
        outline.enabled = true;
        
        _currentOutline = outline;
    }

    void RemoveOutline()
    {
        if (_currentOutline != null)
        {
            _currentOutline.enabled = false; 
            _currentOutline = null;
            _highlightedObject = null;
        }
    }
}
