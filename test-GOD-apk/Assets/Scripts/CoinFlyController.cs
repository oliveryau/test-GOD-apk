using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinFlyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas flyCanvas;
    [SerializeField] private RectTransform targetUiIcon;
    [SerializeField] private RectTransform coinPrefab;

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 20;

    [Header("Animation")]
    [SerializeField] private float flightDuration = 0.55f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Vector2 randomStartOffset = new Vector2(35f, 35f);
    [SerializeField] private float arcHeight = 60f;

    private readonly Queue<RectTransform> pool = new Queue<RectTransform>();
    private RectTransform canvasRect;
    private Camera uiCamera;

    private void Awake()
    {
        canvasRect = flyCanvas.GetComponent<RectTransform>();
        uiCamera = flyCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : flyCanvas.worldCamera;

        for (int i = 0; i < initialPoolSize; i++)
        {
            RectTransform coin = CreateCoin();
            ReturnCoin(coin);
        }
    }

    public void SpawnCoinsFromWorld(Vector3 characterWorldPos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RectTransform coin = GetCoin();

            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(worldCamera, characterWorldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCamera, out Vector2 startLocal);

            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, targetUiIcon.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, uiCamera, out Vector2 targetLocal);

            startLocal += new Vector2(
                Random.Range(-randomStartOffset.x, randomStartOffset.x),
                Random.Range(-randomStartOffset.y, randomStartOffset.y));

            coin.anchoredPosition = startLocal;
            coin.localScale = Vector3.one;

            StartCoroutine(AnimateCoin(coin, startLocal, targetLocal));
        }
    }

    private IEnumerator AnimateCoin(RectTransform coin, Vector2 start, Vector2 end)
    {
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / flightDuration);
            float eased = ease.Evaluate(normalized);

            Vector2 position = Vector2.LerpUnclamped(start, end, eased);
            float arc = 4f * normalized * (1f - normalized) * arcHeight;
            position.y += arc;

            coin.anchoredPosition = position;

            float scale = 1f + 0.15f * Mathf.Sin(normalized * Mathf.PI);
            coin.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        coin.anchoredPosition = end;
        ReturnCoin(coin);
    }

    private RectTransform GetCoin()
    {
        if (pool.Count > 0)
        {
            RectTransform coin = pool.Dequeue();
            coin.gameObject.SetActive(true);
            return coin;
        }

        RectTransform newCoin = CreateCoin();
        newCoin.gameObject.SetActive(true);
        return newCoin;
    }

    private void ReturnCoin(RectTransform coin)
    {
        coin.gameObject.SetActive(false);
        coin.SetParent(canvasRect, false);
        pool.Enqueue(coin);
    }

    private RectTransform CreateCoin()
    {
        RectTransform coin = Instantiate(coinPrefab, canvasRect);
        coin.gameObject.SetActive(false);
        return coin;
    }
}
