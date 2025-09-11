using UnityEngine;

namespace EnemyAI
{
    // 공유 데이터(상태 간 전달) 저장
    [System.Serializable]
    public class EnemyBlackboard
    {
        public Transform Player;
        public Vector3 SpawnPosition;
        public Vector3 LastKnownPlayerPosition;
        public float TimeSinceLostPlayer;
        public bool CanSeePlayer;
        public bool InAttackRange;
        public float AlertTimer;
        public bool IsAttacking; // 현재 공격 애니메이션 재생 중 여부
    }
}
