using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public sealed class GameController : MonoBehaviour
{
    [SerializeField]
    private CameraHoverMotion cameraHover;

    [SerializeField]
    private int maxHp = 3;

    [SerializeField]
    private int hpPenaltyWrong = 1;

    [SerializeField]
    private float timeLimit = 60f;

    [SerializeField]
    private int requiredClearCount = 20;

    [SerializeField]
    private int successSceneNumber = 2;

    [SerializeField]
    private int failSceneNumber = 3;

    [SerializeField]
    private GameObject[] hpObjects;

    [SerializeField]
    private Text minuteText;

    [SerializeField]
    private Text secondText;

    [SerializeField]
    private Text centisecondText;

    [SerializeField]
    private Text remainingBoxText;

    private int currentHp;
    private int clearedCount;
    private float remainingTime;
    private bool gameEnded;
    private Tween remainingBoxTween;

    private void Start()
    {
        currentHp = maxHp;
        clearedCount = 0;
        remainingTime = timeLimit;
        gameEnded = false;
        UpdateHpUI();
        UpdateTimerUI();
        UpdateRemainingBoxUI();
        UpdateUI();
    }

    private void Update()
    {
        if (gameEnded)
            return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeUp();
        }

        UpdateTimerUI();
    }

    public void ResolveRouting(bool isCorrect)
    {
        if (gameEnded)
            return;

        if (isCorrect)
        {
            clearedCount++;

            cameraHover.PlaySuccessKick();

            UpdateRemainingBoxUI();

            if (clearedCount >= requiredClearCount)
                OnGameClear();
        }
        else
        {
            ApplyHpPenalty(hpPenaltyWrong);

            cameraHover.PlayFailShake();
        }

        UpdateUI();
    }

    private void ApplyHpPenalty(int amount)
    {
        if (gameEnded)
            return;

        int oldHp = currentHp;
        currentHp -= amount;
        if (currentHp < 0)
            currentHp = 0;

        if (currentHp < oldHp)
        {
            for (int i = currentHp; i < oldHp; i++)
                PlayHpLossAnimation(i);
        }

        if (currentHp <= 0)
            OnGameOver();
    }

    private void OnTimeUp()
    {
        if (gameEnded)
            return;

        if (clearedCount >= requiredClearCount)
            OnGameClear();
        else
            OnGameOver();
    }

    private void OnGameClear()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        SceneManager.LoadScene(successSceneNumber);
    }

    private void OnGameOver()
    {
        if (gameEnded)
            return;

        gameEnded = true;
        SceneManager.LoadScene(failSceneNumber);
    }

    private void UpdateUI()
    {
        Debug.Log(
            "HP: " + currentHp +
            " Cleared: " + clearedCount + "/" + requiredClearCount +
            " Time: " + remainingTime.ToString("F1")
        );
    }

    private void UpdateHpUI()
    {
        for (int i = 0; i < hpObjects.Length; i++)
        {
            GameObject obj = hpObjects[i];
            if (obj == null)
                continue;

            bool active = i < currentHp;
            obj.SetActive(active);

            if (active)
            {
                Transform t = obj.transform;
                Vector3 scale = t.localScale;
                t.localScale = scale;

                Image img = obj.GetComponent<Image>();
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }
        }
    }

    private void PlayHpLossAnimation(int index)
    {
        if (index < 0 || index >= hpObjects.Length)
            return;

        GameObject obj = hpObjects[index];
        if (!obj.activeSelf)
            return;

        Transform t = obj.transform;
        Vector3 baseScale = t.localScale;

        Image img = obj.GetComponent<Image>();
        Color baseColor = Color.white;
        baseColor = img.color;

        float duration = 0.25f;
        float scaleFactor = 1.3f;

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOScale(baseScale * scaleFactor, duration));
        seq.Join(img.DOFade(0f, duration));
        seq.OnComplete(() =>
        {
            t.localScale = baseScale;
            img.color = baseColor;
            obj.SetActive(false);
        });
    }

    private void UpdateTimerUI()
    {
        int totalMilliseconds = Mathf.Max(0, (int)(remainingTime * 1000f));
        int minutes = totalMilliseconds / 60000;
        int seconds = (totalMilliseconds / 1000) % 60;
        int centiseconds = (totalMilliseconds / 10) % 100;


        minuteText.text = minutes.ToString("00");

        secondText.text = seconds.ToString("00");

        centisecondText.text = centiseconds.ToString("00");
    }

    private void UpdateRemainingBoxUI()
    {

        int remaining = Mathf.Max(0, requiredClearCount - clearedCount);
        remainingBoxText.text = "할당량까지: " + remaining + "개 남음";
        PlayRemainingBoxTween();
    }

    private void PlayRemainingBoxTween()
    {

        Transform t = remainingBoxText.transform;

        if (remainingBoxTween != null && remainingBoxTween.IsActive())
            remainingBoxTween.Kill();

        Vector3 baseScale = t.localScale;
        Color baseColor = remainingBoxText.color;

        float upDuration = 0.12f;
        float downDuration = 0.12f;
        float scaleFactor = 1.06f;
        float fadeAlpha = 0.7f;

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOScale(baseScale * scaleFactor, upDuration));
        seq.Join(remainingBoxText.DOFade(fadeAlpha, upDuration));
        seq.Append(t.DOScale(baseScale, downDuration));
        seq.Join(remainingBoxText.DOFade(baseColor.a, downDuration));

        remainingBoxTween = seq;
    }
}