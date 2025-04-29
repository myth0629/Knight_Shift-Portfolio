using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private int sceneNumber; // 전환할 씬 이름
    SceneManager sceneManager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //SceneManager.LoadScene("NextSceneName"); // 다음 씬으로 전환
            Debug.Log("Player entered the portal");
            //SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
            SceneManager.LoadSceneAsync(sceneNumber);
        }
    }
}
