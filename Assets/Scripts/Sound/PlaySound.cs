using UnityEngine;

public class PlaySoundOnEnable : MonoBehaviour
{
    private AudioSource audioSource;

    // 스크립트가 로드될 때(가장 먼저) 한 번 호출됩니다.
    void Awake()
    {
        // 오브젝트에 붙어있는 AudioSource 컴포넌트를 가져옵니다.
        audioSource = GetComponent<AudioSource>();

        // AudioSource가 없으면 경고를 출력하고 스크립트 작동을 중지합니다.
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource 컴포넌트가 이 오브젝트에 없습니다!");
            enabled = false; // 스크립트 비활성화
        }
    }

    // 오브젝트가 활성화될 때마다 호출됩니다.
    void OnEnable()
    {
        // AudioSource가 준비되었고, 현재 재생 중이 아니라면 소리를 재생합니다.
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // 오브젝트가 비활성화될 때마다 호출됩니다. (선택 사항)
    void OnDisable()
    {
        // 필요하다면 소리를 멈출 수 있습니다.
        // if (audioSource != null)
        // {
        //     audioSource.Stop();
        // }
    }
}