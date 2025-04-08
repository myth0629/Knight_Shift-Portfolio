using System.Collections;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public int attackCount = 0;
    public int maxAttackCount = 4;
    public float attackCooldown = 0.5f;
    public float exitTime = 0.3f;
    public bool max_combo = false;

    Animator animator;
    StarterAssetsInputs input;
    ThirdPersonController thirdPersonController;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();
        
        if (input == null)
        {
            input = GetComponentInParent<StarterAssetsInputs>();
            
            if (input == null)
            {
                input = GetComponentInChildren<StarterAssetsInputs>();
                
                if (input == null)
                {
                    Debug.LogError("StarterAssetsInputs를 찾을 수 없습니다!");
                }
            }
        }
    }

    
    void Update()
    {
        if(input != null && input.attack && attackCount < maxAttackCount && !max_combo)
        {
            attackCount++;
            if(attackCount == maxAttackCount)
            {
                max_combo = true;
            }

            if(attackCount == 1 && !max_combo)
            {
                StartCoroutine(attackTimer());
            }
        }
    }

    private IEnumerator attackTimer()
    {
        int loopCount = 0;

        while(attackCount > 0)
        {
            if(loopCount == maxAttackCount)
            {
                Debug.Log("오버플로우");
                loopCount = 0;
                attackCount = 0;
                animator.SetInteger("attackCount", loopCount);
                break;
            }

            animator.SetTrigger("Attack");
            animator.SetInteger("attackCount", loopCount);

            yield return new WaitForSeconds(attackCooldown);
            attackCount--;
        }
        animator.SetTrigger("attackEnd");
    }
}
