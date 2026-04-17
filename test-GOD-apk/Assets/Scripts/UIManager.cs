using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Camera _gameplayCamera;
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFillImage;
    [SerializeField] private GameObject _upgradePromptRoot;
    [SerializeField] private GameObject _upgradePromptIndicator;
    [SerializeField] private Button _upgradeButton;

    [Header("Progress")]
    [SerializeField] private int _mergesRequiredPerUpgrade = 2;
    [SerializeField] private int _maxCastleUpgrades = 3;

    [Header("Camera View Anchors")]
    [SerializeField] private Transform _boardViewAnchor;
    [SerializeField] private Transform _castleViewAnchor;
    [SerializeField] private float _cameraTransitionDuration = 0.35f;
    [SerializeField] private Collider _mergeBoardAreaCollider;

    [Header("Castle Visual Upgrade")]
    [SerializeField] private Animator _castleAnimator;
    [SerializeField] private string _castleUpgradeTrigger = "upgrade";
    [SerializeField] private List<GameObject> _castleStages = new List<GameObject>();

    private int _mergeCredits;
    private int _castleLevelIndex;
    private bool _isShowingCastleView;
    private Coroutine _cameraTransitionRoutine;

    private void Awake()
    {
        if (_gameplayCamera == null)
        {
            _gameplayCamera = Camera.main;
        }

        if (_progressRoot == null && _progressFillImage != null)
        {
            _progressRoot = _progressFillImage.transform.parent != null
                ? _progressFillImage.transform.parent.gameObject
                : _progressFillImage.gameObject;
        }
    }

    private void OnEnable()
    {
        if (_gameManager != null)
        {
            _gameManager.UnitsMerged += OnUnitsMerged;
            _gameManager.SetBoardInteractable(true);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(HandleUpgradeButtonPressed);
        }

        if (_progressRoot != null)
        {
            _progressRoot.SetActive(true);
        }

        RefreshUi();
        ApplyCastleStageVisual();
    }

    private void OnDisable()
    {
        if (_gameManager != null)
        {
            _gameManager.UnitsMerged -= OnUnitsMerged;
            _gameManager.SetBoardInteractable(true);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(HandleUpgradeButtonPressed);
        }
    }

    private void Update()
    {
        if (TryGetPointerDownScreenPosition(out Vector2 screenPosition) && !IsPointerOverUi())
        {
            bool clickedBoard = IsScreenPositionOnBoard(screenPosition);

            if (_isShowingCastleView)
            {
                if (clickedBoard)
                {
                    MoveCameraTo(_boardViewAnchor);
                    _isShowingCastleView = false;
                    if (_gameManager != null)
                    {
                        _gameManager.SetBoardInteractable(true);
                    }
                }
            }
            else
            {
                if (!clickedBoard)
                {
                    MoveCameraTo(_castleViewAnchor);
                    _isShowingCastleView = true;
                    if (_gameManager != null)
                    {
                        _gameManager.SetBoardInteractable(false);
                    }
                }
            }
        }
    }

    private void OnUnitsMerged(int mergedTier)
    {
        _mergeCredits++;
        RefreshUi();
    }

    private void HandleUpgradeButtonPressed()
    {
        int maxUpgrades = Mathf.Max(0, _maxCastleUpgrades);
        if (_mergeCredits < _mergesRequiredPerUpgrade || _castleLevelIndex >= maxUpgrades)
        {
            return;
        }

        _mergeCredits -= _mergesRequiredPerUpgrade;
        _castleLevelIndex++;
        ApplyCastleUpgradeAnimation();
        ApplyCastleStageVisual();
        MoveCameraTo(_castleViewAnchor);
        _isShowingCastleView = true;
        RefreshUi();
    }

    private void RefreshUi()
    {
        int required = Mathf.Max(1, _mergesRequiredPerUpgrade);
        int maxUpgrades = Mathf.Max(0, _maxCastleUpgrades);
        bool hasUpgradeCapacity = _castleLevelIndex < maxUpgrades;
        bool canUpgrade = _mergeCredits >= required && hasUpgradeCapacity;
        float normalized = hasUpgradeCapacity
            ? (canUpgrade ? 1f : (float)_mergeCredits / required)
            : 1f;

        if (_progressFillImage != null)
        {
            _progressFillImage.gameObject.SetActive(true);
            _progressFillImage.fillAmount = Mathf.Clamp01(normalized);
        }

        if (_upgradePromptRoot != null)
        {
            _upgradePromptRoot.SetActive(true);
        }

        if (_upgradePromptRoot != null)
        {
            if (_upgradePromptIndicator != null)
            {
                _upgradePromptIndicator.SetActive(canUpgrade);
            }
            else if (_upgradeButton != null)
            {
                _upgradeButton.gameObject.SetActive(canUpgrade);
            }
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = canUpgrade;
        }
    }

    private void MoveCameraTo(Transform targetAnchor)
    {
        if (_gameplayCamera == null || targetAnchor == null)
        {
            return;
        }

        if (_cameraTransitionRoutine != null)
        {
            StopCoroutine(_cameraTransitionRoutine);
        }

        _cameraTransitionRoutine = StartCoroutine(AnimateCameraTransition(targetAnchor));
    }

    private IEnumerator AnimateCameraTransition(Transform targetAnchor)
    {
        Vector3 fromPosition = _gameplayCamera.transform.position;
        Quaternion fromRotation = _gameplayCamera.transform.rotation;
        Vector3 toPosition = targetAnchor.position;
        Quaternion toRotation = targetAnchor.rotation;

        float duration = Mathf.Max(0.01f, _cameraTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _gameplayCamera.transform.position = Vector3.Lerp(fromPosition, toPosition, t);
            _gameplayCamera.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);
            yield return null;
        }

        _gameplayCamera.transform.position = toPosition;
        _gameplayCamera.transform.rotation = toRotation;
        _cameraTransitionRoutine = null;
    }

    private bool TryGetPointerDownScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = default;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }

            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        return false;
    }

    private static bool IsPointerOverUi(int pointerId = -1)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0 ? EventSystem.current.IsPointerOverGameObject(pointerId) : EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsScreenPositionOnBoard(Vector2 screenPosition)
    {
        if (_gameplayCamera == null)
        {
            return false;
        }

        Ray ray = _gameplayCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return false;
        }

        if (_mergeBoardAreaCollider != null)
        {
            Transform boardRoot = _mergeBoardAreaCollider.transform;
            if (hit.transform == boardRoot || hit.transform.IsChildOf(boardRoot))
            {
                return true;
            }
        }

        return hit.transform.GetComponentInParent<Units>() != null;
    }

    private void ApplyCastleUpgradeAnimation()
    {
        if (_castleAnimator == null || string.IsNullOrEmpty(_castleUpgradeTrigger))
        {
            return;
        }

        _castleAnimator.SetTrigger(_castleUpgradeTrigger);
    }

    private void ApplyCastleStageVisual()
    {
        if (_castleStages == null || _castleStages.Count == 0)
        {
            return;
        }

        int activeIndex = Mathf.Clamp(_castleLevelIndex, 0, _castleStages.Count - 1);
        for (int i = 0; i < _castleStages.Count; i++)
        {
            GameObject stage = _castleStages[i];
            if (stage != null)
            {
                stage.SetActive(i == activeIndex);
            }
        }
    }
}
