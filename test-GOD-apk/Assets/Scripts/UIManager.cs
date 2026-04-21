using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Camera _gameplayCamera;
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFillImage;
    [SerializeField] private GameObject _upgradePromptIndicator;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private GameObject _playerUI;
    [SerializeField] private TextMeshProUGUI _coinValue;
    [SerializeField] private CoinFlyController _coinFlyController;
    [SerializeField] private int _coinsPerMergeFlyCount = 8;
    [SerializeField] private Button _zoomButton;
    [SerializeField] private CameraZoomPanController _cameraZoomPanController;

    [Header("Progress")]
    [SerializeField] private int _mergesRequiredPerUpgrade = 2;
    [SerializeField] private int _maxCastleUpgrades = 3;

    [Header("Castle Visual Upgrade")]
    [SerializeField] private Castle _castle;
    [SerializeField] private List<GameObject> _castleStages = new List<GameObject>();

    private int _coins;
    private int _mergeCredits;
    private int _castleLevelIndex;

    private void Awake()
    {
        if (_gameplayCamera == null)
        {
            _gameplayCamera = Camera.main;
        }
        if (_cameraZoomPanController == null)
        {
            _cameraZoomPanController = FindAnyObjectByType<CameraZoomPanController>();
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
        if (_zoomButton != null)
        {
            _zoomButton.onClick.AddListener(HandleZoomButtonPressed);
        }

        if (_progressRoot != null)
        {
            _progressRoot.SetActive(true);
        }

        RefreshUi();
        UpdateCoinDisplay();
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
        if (_zoomButton != null)
        {
            _zoomButton.onClick.RemoveListener(HandleZoomButtonPressed);
        }

        if (_cameraZoomPanController != null)
        {
            _cameraZoomPanController.SetZoomedOut(false);
        }
    }
    
    private void OnUnitsMerged(int mergedTier, Vector3 mergeWorldPosition)
    {
        _mergeCredits++;
        _coins += 100;
        if (_coinFlyController != null)
        {
            _coinFlyController.SpawnCoinsFromWorld(mergeWorldPosition, Mathf.Max(1, _coinsPerMergeFlyCount));
        }
        UpdateCoinDisplay();
        RefreshUi();
    }

    private void UpdateCoinDisplay()
    {
        _coinValue.text = _coins.ToString();
        if (_coins > 0) _playerUI.GetComponent<Animator>().SetTrigger("add");
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
        ApplyCastleStageVisual();
        RefreshUi();
    }

    public void HandleZoomButtonPressed()
    {
        if (_cameraZoomPanController == null)
        {
            return;
        }

        bool isZoomedOut = _cameraZoomPanController.ToggleZoom();
        if (_gameManager != null)
        {
            _gameManager.SetBoardInteractable(!isZoomedOut);
        }

        RefreshUi();
    }

    private void RefreshUi()
    {
        bool isZoomedOut = _cameraZoomPanController != null && _cameraZoomPanController.IsZoomedOut;
        int required = Mathf.Max(1, _mergesRequiredPerUpgrade);
        int maxUpgrades = Mathf.Max(0, _maxCastleUpgrades);
        bool hasUpgradeCapacity = _castleLevelIndex < maxUpgrades;
        bool canUpgrade = _mergeCredits >= required && hasUpgradeCapacity;
        float normalized = hasUpgradeCapacity
            ? (canUpgrade ? 1f : (float)_mergeCredits / required)
            : 1f;

        if (_progressFillImage != null)
        {
            _progressFillImage.gameObject.SetActive(!isZoomedOut);
            _progressFillImage.fillAmount = Mathf.Clamp01(normalized);
        }

        if (_progressRoot != null)
        {
            _progressRoot.SetActive(!isZoomedOut);
        }

        if (_progressRoot != null && !isZoomedOut)
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
        else if (_upgradePromptIndicator != null)
        {
            _upgradePromptIndicator.SetActive(false);
        }

        if (_upgradeButton != null && isZoomedOut)
        {
            _upgradeButton.gameObject.SetActive(false);
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = !isZoomedOut && canUpgrade;
            if (_progressRoot != null)
            {
                Animator progressAnimator = _progressRoot.GetComponent<Animator>();
                if (progressAnimator != null)
                {
                    progressAnimator.SetBool("glowing", !isZoomedOut && canUpgrade);
                }
            }
        }
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
            if (stage != null && _castle != null)
            {
                stage.SetActive(i <= activeIndex);
            }
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene("GameScene");
    }
}
