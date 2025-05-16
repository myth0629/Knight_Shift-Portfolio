using UnityEngine;

public class GameManager : MonoBehaviour
{
    public void SetGamePauseState(bool isPaused)
    {
        Debug.Log("일시정지");
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
