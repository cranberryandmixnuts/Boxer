using UnityEngine;
using UnityEngine.SceneManagement;

public class ToTitle : BaseBehaviour
{
    public void GoToTitle() => SceneManager.LoadScene(0);
}