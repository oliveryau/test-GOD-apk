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
