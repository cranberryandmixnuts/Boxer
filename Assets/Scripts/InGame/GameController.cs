using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public sealed class GameController : SingletonBehaviour<GameController, SceneScope>
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
    private int requiredClearCount = 140;

    [SerializeField]
    private int successSceneNumber = 2;

    [SerializeField]
    private int failSceneNumber = 3;

    [SerializeField]
    private GameObject inputObject;

    [SerializeField]
    private GameObject[] hpObjects;

    [SerializeField]
    private GameObject hpRoot;

    [SerializeField]
    private GameObject timerRoot;

    [SerializeField]
    private Text minuteText;

    [SerializeField]
    private Text secondText;

    [SerializeField]
    private Text centisecondText;

    [SerializeField]
    private Text remainingBoxText;

    [SerializeField]
    private GameObject fadeImageObject;

    [SerializeField]
    private float fadeDuration = 0.9f;

    [SerializeField]
    private GameObject tutorialObject;

    private int currentHp;
    private int clearedCount;
    private float remainingTime;
    private bool gameEnded;
    private bool timerStarted;
    private Tween remainingBoxTween;

    private void Start()
    {
        currentHp = maxHp;
        clearedCount = 0;
        remainingTime = timeLimit;
        gameEnded = false;
        timerStarted = false;

        UpdateHpUI();
        UpdateTimerUI();
        UpdateRemainingBoxUI();
        PlayFadeOut();
    }

    private void Update()
    {
        if (!gameEnded && !timerStarted)
            CheckStartInput();

        if (gameEnded || !timerStarted)
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
        if (gameEnded || !timerStarted)
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
    }

    private void CheckStartInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        timerStarted = true;

        if (tutorialObject != null)
            tutorialObject.SetActive(false);
        fadeImageObject.SetActive(false);
    }

    private void PlayFadeOut()
    {
        if (fadeImageObject == null)
            return;

        CanvasGroup canvasGroup = fadeImageObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            return;

        fadeImageObject.SetActive(true);
        canvasGroup.alpha = 1f;

        canvasGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear);
    }

    private void ApplyHpPenalty(int amount)
    {
        if (gameEnded)
            return;

        int oldHp = currentHp;
        currentHp -= amount;
        if (currentHp < 0)
            currentHp = 0;

        if (currentHp <= 0)
        {
            currentHp = 0;
            OnGameOverByHp();
        }
        else
        {
            if (currentHp < oldHp)
            {
                for (int i = currentHp; i < oldHp; i++)
                    PlayHpLossAnimation(i);
            }
        }
    }

    private void OnTimeUp()
    {
        if (gameEnded)
            return;

        if (clearedCount >= requiredClearCount)
            OnGameClear();
        else
            OnGameOverByTime();
    }

    private void OnGameClear()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        if (inputObject != null)
            inputObject.SetActive(false);

        GameObject target = remainingBoxText != null ? remainingBoxText.gameObject : null;
        BlinkAndLoadScene(target, successSceneNumber);
    }

    private void OnGameOverByHp()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        if (inputObject != null)
            inputObject.SetActive(false);

        ShowAllHpForBlink();

        GameObject target = hpRoot != null ? hpRoot : null;
        if (target == null && hpObjects != null && hpObjects.Length > 0 && hpObjects[0] != null)
            target = hpObjects[0].transform.parent.gameObject;

        BlinkAndLoadScene(target, failSceneNumber);
    }

    private void OnGameOverByTime()
    {
        if (gameEnded)
            return;

        gameEnded = true;

        if (inputObject != null)
            inputObject.SetActive(false);

        GameObject target = timerRoot != null ? timerRoot : null;
        if (target == null && minuteText != null)
            target = minuteText.transform.parent.gameObject;

        BlinkAndLoadScene(target, failSceneNumber);
    }

    private void BlinkAndLoadScene(GameObject target, int sceneIndex)
    {

        target.SetActive(true);

        float interval = 0.5f;
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < 3; i++)
        {
            seq.AppendCallback(() => target.SetActive(true));
            seq.AppendInterval(interval);
            seq.AppendCallback(() => target.SetActive(false));
            seq.AppendInterval(interval);
        }

        seq.OnComplete(() => SceneManager.LoadScene(sceneIndex));
    }

    private void ShowAllHpForBlink()
    {
        for (int i = 0; i < hpObjects.Length; i++)
        {
            GameObject obj = hpObjects[i];
            if (obj == null)
                continue;

            obj.SetActive(true);

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }
        }
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
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 1f;
                    img.color = c;
                }
            }
        }
    }

    private void PlayHpLossAnimation(int index)
    {
        if (hpObjects == null)
            return;
        if (index < 0 || index >= hpObjects.Length)
            return;

        GameObject obj = hpObjects[index];
        if (obj == null)
            return;
        if (!obj.activeSelf)
            return;

        Transform t = obj.transform;
        Vector3 baseScale = t.localScale;

        Image img = obj.GetComponent<Image>();
        Color baseColor = Color.white;
        if (img != null)
            baseColor = img.color;

        float duration = 0.25f;
        float scaleFactor = 1.3f;

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOScale(baseScale * scaleFactor, duration));
        if (img != null)
            seq.Join(img.DOFade(0f, duration));
        seq.OnComplete(
            () =>
            {
                t.localScale = baseScale;
                if (img != null)
                    img.color = baseColor;
                obj.SetActive(false);
            }
        );
    }

    private void UpdateTimerUI()
    {
        int totalMilliseconds = Mathf.Max(0, (int)(remainingTime * 1000f));
        int minutes = totalMilliseconds / 60000;
        int seconds = (totalMilliseconds / 1000) % 60;
        int centiseconds = (totalMilliseconds / 10) % 100;

        if (minuteText != null)
            minuteText.text = minutes.ToString("00");

        if (secondText != null)
            secondText.text = seconds.ToString("00");

        if (centisecondText != null)
            centisecondText.text = centiseconds.ToString("00");
    }

    private void UpdateRemainingBoxUI()
    {
        if (remainingBoxText == null)
            return;

        int remaining = Mathf.Max(0, requiredClearCount - clearedCount);
        remainingBoxText.text = "할당량까지: " + remaining + "개 남음";

        PlayRemainingBoxTween();
    }

    private void PlayRemainingBoxTween()
    {
        if (remainingBoxText == null)
            return;

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
