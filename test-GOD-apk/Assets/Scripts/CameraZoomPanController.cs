using UnityEngine;
using UnityEngine.EventSystems;

public class CameraZoomPanController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private float _zoomTransitionSpeed = 8f;

    [Header("Zoom Out State")]
    [SerializeField] private Vector3 _zoomOutPositionOffset = new Vector3(0f, 2f, -4f);
    [SerializeField] private float _zoomOutFov = 48f;

    [Header("Rotate While Zoomed Out")]
    [SerializeField] private float _rotationDegreesPerFullScreenDrag = 180f;
    [SerializeField] private Vector2 _pitchBounds = new Vector2(15f, 75f);
    [SerializeField] private Vector2 _yawOffsetBounds = new Vector2(-60f, 60f);

    private Vector3 _defaultPosition;
    private Quaternion _defaultRotation;
    private float _defaultFov;
    private Vector3 _desiredPosition;
    private Quaternion _desiredRotation;
    private float _desiredFov;
    private bool _isZoomedOut;
    private float _baseYaw;
    private float _currentYawOffset;
    private float _currentPitch;

    private bool _isMousePanning;
    private Vector2 _lastMousePosition;

    private int _activeTouchFingerId = -1;
    private Vector2 _lastTouchPosition;

    public bool IsZoomedOut => _isZoomedOut;

    private void Awake()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        if (_targetCamera == null)
        {
            enabled = false;
            return;
        }

        _defaultPosition = _targetCamera.transform.position;
        _defaultRotation = _targetCamera.transform.rotation;
        _defaultFov = _targetCamera.fieldOfView;
        _desiredPosition = _defaultPosition;
        _desiredRotation = _defaultRotation;
        _desiredFov = _defaultFov;
        Vector3 euler = _defaultRotation.eulerAngles;
        _baseYaw = euler.y;
        _currentYawOffset = 0f;
        _currentPitch = NormalizeAngle(euler.x);
    }

    private void Update()
    {
        if (_targetCamera == null)
        {
            return;
        }

        if (_isZoomedOut)
        {
            HandlePanInput();
        }

        float t = 1f - Mathf.Exp(-_zoomTransitionSpeed * Time.unscaledDeltaTime);
        _targetCamera.transform.position = Vector3.Lerp(_targetCamera.transform.position, _desiredPosition, t);
        _targetCamera.transform.rotation = Quaternion.Slerp(_targetCamera.transform.rotation, _desiredRotation, t);
        _targetCamera.fieldOfView = Mathf.Lerp(_targetCamera.fieldOfView, _desiredFov, t);
    }

    public bool ToggleZoom()
    {
        SetZoomedOut(!_isZoomedOut);
        return _isZoomedOut;
    }

    public void SetZoomedOut(bool zoomOut)
    {
        _isZoomedOut = zoomOut;

        if (_isZoomedOut)
        {
            _desiredPosition = _defaultPosition + _zoomOutPositionOffset;
            _desiredFov = _zoomOutFov;
            _desiredRotation = Quaternion.Euler(_currentPitch, _baseYaw + _currentYawOffset, 0f);
        }
        else
        {
            _desiredPosition = _defaultPosition;
            _desiredRotation = _defaultRotation;
            _desiredFov = _defaultFov;
            _isMousePanning = false;
            _activeTouchFingerId = -1;
            _currentYawOffset = 0f;
            _currentPitch = NormalizeAngle(_defaultRotation.eulerAngles.x);
        }
    }

    private void HandlePanInput()
    {
        HandleMousePan();
        HandleTouchPan();
    }

    private void HandleMousePan()
    {
        if (Application.isMobilePlatform && Input.touchSupported)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            _isMousePanning = true;
            _lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isMousePanning = false;
        }

        if (!_isMousePanning || !Input.GetMouseButton(0))
        {
            return;
        }

        Vector2 current = Input.mousePosition;
        Vector2 delta = current - _lastMousePosition;
        _lastMousePosition = current;
        ApplyRotationDelta(delta);
    }

    private void HandleTouchPan()
    {
        if (!Application.isMobilePlatform || !Input.touchSupported)
        {
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (_activeTouchFingerId == -1)
            {
                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    continue;
                }

                _activeTouchFingerId = touch.fingerId;
                _lastTouchPosition = touch.position;
                continue;
            }

            if (touch.fingerId != _activeTouchFingerId)
            {
                continue;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 delta = touch.position - _lastTouchPosition;
                _lastTouchPosition = touch.position;
                ApplyRotationDelta(delta);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _activeTouchFingerId = -1;
            }
        }
    }

    private void ApplyRotationDelta(Vector2 screenDelta)
    {
        if (screenDelta.sqrMagnitude <= 0.0001f)
        {
            return;
        }
        float xRatio = screenDelta.x / Mathf.Max(1f, Screen.width);
        float yRatio = screenDelta.y / Mathf.Max(1f, Screen.height);

        _currentYawOffset += xRatio * _rotationDegreesPerFullScreenDrag;
        _currentPitch -= yRatio * _rotationDegreesPerFullScreenDrag;

        _currentYawOffset = Mathf.Clamp(_currentYawOffset, _yawOffsetBounds.x, _yawOffsetBounds.y);
        _currentPitch = Mathf.Clamp(_currentPitch, _pitchBounds.x, _pitchBounds.y);

        _desiredRotation = Quaternion.Euler(_currentPitch, _baseYaw + _currentYawOffset, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
