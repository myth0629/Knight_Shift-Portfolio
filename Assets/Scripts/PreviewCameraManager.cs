using System.Collections;
using UnityEngine;

public class PreviewCameraManager : MonoBehaviour
{
    void Start()
    {
        Invoke("DisableAfterRender", 0.1f); // 렌더링 후 비활성화
    }

    void DisableAfterRender()
    {
        gameObject.SetActive(false); 
    }
}