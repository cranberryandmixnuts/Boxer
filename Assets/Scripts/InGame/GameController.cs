using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField]
    private CameraHoverMotion cameraHover;

    [SerializeField]
    private int maxHp = 5;

    [SerializeField]
    private float slowDragThreshold = 0.6f;

    [SerializeField]
    private int scorePerBox = 100;

    [SerializeField]
    private int hpPenaltyWrong = 1;

    [SerializeField]
    private int hpPenaltySlow = 1;

    private int currentHp;
    private int score;

    private void Start()
    {
        currentHp = maxHp;
        score = 0;
    }

    public void ResolveRouting(bool isCorrect, BoxController box, float dragTime)
    {
        if (dragTime > slowDragThreshold)
            ApplyHpPenalty(hpPenaltySlow);

        if (isCorrect)
        {
            score += scorePerBox;
            if (cameraHover != null)
                cameraHover.PlaySuccessKick();
        }
        else
        {
            ApplyHpPenalty(hpPenaltyWrong);
            if (cameraHover != null)
                cameraHover.PlayFailShake();
        }

        UpdateUI();
    }

    private void ApplyHpPenalty(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
            OnGameOver();
    }

    private void OnGameOver()
    {
        Debug.Log("Game Over");
    }

    private void UpdateUI()
    {
        Debug.Log("HP: " + currentHp + " Score: " + score);
    }
}
