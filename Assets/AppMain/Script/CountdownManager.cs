using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("操作ロック/解禁先")]
    [SerializeField] private RaceStartGate raceStartGate; // ← ここに車の RaceStartGate を割り当て

    [Header("表示設定")]
    public float interval = 0.2f;
    public float fadeTime = 0.3f;

    private void Awake()
    {
        // 未設定ならUI自動取得
        if (countdownText == null)
            countdownText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (countdownText != null)
        {
            if (canvasGroup == null)
                canvasGroup = countdownText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = countdownText.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // 初期は透明
        }
        else
        {
            Debug.LogError("[CountdownManager] TextMeshProUGUIが見つかりません。");
        }

        // RaceStartGate が未設定ならシーンから探す（念のため）
        if (raceStartGate == null)
            raceStartGate = FindObjectOfType<RaceStartGate>(true);
    }

    private void Start()
    {
        // カウントダウン開始前にロック
        if (raceStartGate != null) raceStartGate.LockControls();
        StartCoroutine(CountdownSequence());
    }

    private IEnumerator CountdownSequence()
    {
        yield return StartCoroutine(ShowText("Ready?"));
        yield return StartCoroutine(ShowText("3"));
        yield return StartCoroutine(ShowText("2"));
        yield return StartCoroutine(ShowText("1"));
        yield return StartCoroutine(ShowText("GO!", 0.8f));

        // GOの瞬間に解禁
        if (raceStartGate != null) raceStartGate.UnlockControls();
    }

    private IEnumerator ShowText(string text, float customInterval = -1f)
    {
        if (countdownText == null || canvasGroup == null) yield break;

        countdownText.text = text;
        float duration = (customInterval > 0f) ? customInterval : interval;

        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeTime));
        yield return new WaitForSeconds(duration);
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeTime));
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float time)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < time)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
