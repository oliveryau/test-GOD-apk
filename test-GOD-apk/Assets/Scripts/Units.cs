using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Units : MonoBehaviour
{
    [SerializeField] private int _tier = 1;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _tierIcon;

    private GameManager _gameManager;
    private Camera _mainCamera;
    private bool _isDragging;
    private float _dragHeight;
    private bool _canInteract = true;
    private int _activeTouchFingerId = -1;

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

    private void Update()
    {
        HandleTouchInput();
        FaceIconTowardCamera();
    }

    private void OnMouseDown()
    {
        if (IsUsingTouchInput() || _gameManager == null || !_canInteract)
        {
            return;
        }

        BeginDrag();
    }

    private void OnMouseDrag()
    {
        if (IsUsingTouchInput() || !_isDragging)
        {
            return;
        }

        if (TryGetDragPoint(Input.mousePosition, out Vector3 dragWorldPosition))
        {
            transform.position = dragWorldPosition;
        }
    }

    private void OnMouseUp()
    {
        if (IsUsingTouchInput() || !_isDragging || _gameManager == null)
        {
            return;
        }

        EndDrag();
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
            _activeTouchFingerId = -1;
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

    private void HandleTouchInput()
    {
        if (!IsUsingTouchInput() || _mainCamera == null || _gameManager == null || !_canInteract)
        {
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (_activeTouchFingerId == -1)
            {
                if (touch.phase == TouchPhase.Began && IsTouchOnThisUnit(touch.position))
                {
                    _activeTouchFingerId = touch.fingerId;
                    BeginDrag();
                }

                continue;
            }

            if (touch.fingerId != _activeTouchFingerId)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (_isDragging && TryGetDragPoint(touch.position, out Vector3 dragWorldPosition))
                {
                    transform.position = dragWorldPosition;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (_isDragging)
                {
                    EndDrag();
                }

                _activeTouchFingerId = -1;
            }
        }
    }

    private bool IsTouchOnThisUnit(Vector2 screenPosition)
    {
        if (_mainCamera == null)
        {
            return false;
        }

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return false;
        }

        return hit.transform == transform || hit.transform.IsChildOf(transform);
    }

    private bool TryGetDragPoint(Vector2 screenPosition, out Vector3 dragWorldPosition)
    {
        dragWorldPosition = default;
        if (_mainCamera == null)
        {
            return false;
        }

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));
        if (!dragPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        dragWorldPosition = ray.GetPoint(enter);
        return true;
    }

    private void BeginDrag()
    {
        _isDragging = true;
        _dragHeight = transform.position.y;
        _gameManager.NotifyDragStarted(this);
    }

    private void EndDrag()
    {
        _isDragging = false;
        _gameManager.NotifyDragEnded(this);
    }

    private static bool IsUsingTouchInput()
    {
        return Application.isMobilePlatform && Input.touchSupported;
    }

    private void FaceIconTowardCamera()
    {
        if (_tierIcon == null || _mainCamera == null)
        {
            return;
        }

        Vector3 directionToCamera = _mainCamera.transform.position - _tierIcon.transform.position;
        _tierIcon.transform.rotation = Quaternion.LookRotation(directionToCamera);
    }

    public void SetTierIconActive(bool isActive)
    {
        if (_tierIcon != null)
        {
            _tierIcon.SetActive(isActive);
        }
    }

    //public void TriggerCoinAnimation(string triggerName)
    //{
    //    if (_coinMergeAnim == null || string.IsNullOrEmpty(triggerName))
    //    {
    //        return;
    //    }

    //    _coinMergeAnim.SetTrigger(triggerName);
    //}
}
