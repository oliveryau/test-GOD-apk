using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Units : MonoBehaviour
{
    [SerializeField] private int _tier = 1;
    [SerializeField] private Animator _animator;

    private GameManager _gameManager;
    private Camera _mainCamera;
    private bool _isDragging;
    private float _dragHeight;
    private bool _canInteract = true;

    public int Tier => _tier;
    public Transform CurrentSpawnPoint { get; private set; }

    public void Setup(GameManager gameManager, int tier, Transform spawnPoint)
    {
        _gameManager = gameManager;
        _tier = tier;
        CurrentSpawnPoint = spawnPoint;
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        if (_gameManager == null || !_canInteract)
        {
            return;
        }

        _isDragging = true;
        _dragHeight = transform.position.y;
        _gameManager.NotifyDragStarted(this);
    }

    private void OnMouseDrag()
    {
        if (!_isDragging || _mainCamera == null)
        {
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));

        if (dragPlane.Raycast(ray, out float enter))
        {
            transform.position = ray.GetPoint(enter);
        }
    }

    private void OnMouseUp()
    {
        if (!_isDragging || _gameManager == null)
        {
            return;
        }

        _isDragging = false;
        _gameManager.NotifyDragEnded(this);
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        CurrentSpawnPoint = spawnPoint;
    }

    public void SetInteractable(bool canInteract)
    {
        _canInteract = canInteract;
        if (!canInteract)
        {
            _isDragging = false;
        }
    }

    public void TriggerRunAnimation(string triggerName)
    {
        if (_animator == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        _animator.SetTrigger(triggerName);
    }
}
