using UnityEngine;
// EnemyHealth.cs (체력 관리)
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100;
    public bool IsDead { get; private set; }
    
    private float currentHealth;
    private Animator anim;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        if(IsDead) return;

        currentHealth -= damage;
        anim.SetTrigger("Hit");  // 피격 애니메이션 실행

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        IsDead = true;
        anim.SetTrigger("Die");
        GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        GetComponent<Collider>().enabled = false;
    }
}

