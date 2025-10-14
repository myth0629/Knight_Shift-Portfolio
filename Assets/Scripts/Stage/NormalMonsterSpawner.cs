using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NormalMonsterSpawner : MonoBehaviour
{
    [Header("몬스터 설정")]
    [Tooltip("이 스포너에서 생성할 몬스터 프리팹 목록")]
    public GameObject[] normalMonsterPrefabs;

    [Tooltip("기본 몬스터 수 (보스 킬 수 0일 때)")]
    public int baseMonsterCount = 3;

    [Tooltip("보스를 잡을 때마다 증가할 몬스터 수")]
    public int monsterCountIncreasePerBossKill = 1;

    [Header("스폰 위치 설정")]
    [Tooltip("몬스터가 생성될 위치(Transform) 목록")]
    public Transform[] spawnPoints;

    [Tooltip("몬스터가 겹치지 않도록 퍼뜨릴 반경")]
    public float spawnSpreadRadius = 3.0f;

    [Tooltip("땅을 찾은 지점에서 NavMesh를 검색할 최대 반경 (보통 작은 값)")]
    public float navMeshFindRadius = 5.5f;

    void Start()
    {
        if (normalMonsterPrefabs == null || normalMonsterPrefabs.Length == 0)
        {
            Debug.LogError("[NormalMonsterSpawner] 생성할 몬스터 프리팹(normalMonsterPrefabs)이 설정되지 않았습니다.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[NormalMonsterSpawner] 스폰 위치(spawnPoints)가 설정되지 않았습니다.");
            return;
        }

        if (StageManager.Instance == null)
        {
            Debug.LogError("[NormalMonsterSpawner] StageManager.Instance를 찾을 수 없습니다.");
            return;
        }

        // StageManager로부터 이번 플레이의 보스 킬 수를 가져옵니다.
        int bossKills = StageManager.Instance.BossKillsThisRun;
        
        // 보스 킬 수에 따라 생성할 몬스터 목록을 계산합니다.
        List<GameObject> monstersToSpawn = GetMonsterList(bossKills);

        if (monstersToSpawn.Count == 0)
        {
            Debug.Log("[NormalMonsterSpawner] 생성할 몬스터가 없습니다.");
            return;
        }

        Debug.Log($"[NormalMonsterSpawner] 총 {monstersToSpawn.Count}마리의 몬스터 생성을 시작합니다. (현재 보스 킬: {bossKills})");

        for (int i = 0; i < monstersToSpawn.Count; i++)
        {
            Transform initialSpawnPoint = spawnPoints[i % spawnPoints.Length];
            GameObject monsterPrefab = monstersToSpawn[i];

            Vector3 spawnCenter = initialSpawnPoint.position;
            Vector2 randomCircle = Random.insideUnitCircle * spawnSpreadRadius;
            Vector3 randomPointOnPlane = spawnCenter + new Vector3(randomCircle.x, 0, randomCircle.y);

            Vector3 raycastStart = randomPointOnPlane + Vector3.up * 20f;
            if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit rayHit, 40f))
            {
                if (NavMesh.SamplePosition(rayHit.point, out NavMeshHit navHit, navMeshFindRadius, NavMesh.AllAreas))
                {
                    Instantiate(monsterPrefab, navHit.position, initialSpawnPoint.rotation);
                }
                else
                {
                    Debug.LogWarning($" - 스폰 실패: 땅({rayHit.point})은 찾았지만, 주변 {navMeshFindRadius}m 내에 NavMesh가 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning($" - 스폰 실패: {randomPointOnPlane} 아래에 땅을 찾지 못했습니다.");
            }
        }
    }

    private List<GameObject> GetMonsterList(int bossKills)
    {
        List<GameObject> spawnList = new List<GameObject>();
        int monstersToSpawn = baseMonsterCount + bossKills * monsterCountIncreasePerBossKill;

        if (monstersToSpawn >= normalMonsterPrefabs.Length && normalMonsterPrefabs.Length > 0)
        {
            foreach (var prefab in normalMonsterPrefabs)
            {
                spawnList.Add(prefab);
            }
        }

        int remainingMonsters = monstersToSpawn - spawnList.Count;
        for (int i = 0; i < remainingMonsters; i++)
        {
            spawnList.Add(normalMonsterPrefabs[Random.Range(0, normalMonsterPrefabs.Length)]);
        }
    
        for (int i = 0; i < spawnList.Count; i++)
        {
            int randomIndex = Random.Range(i, spawnList.Count);
            GameObject temp = spawnList[i];
            spawnList[i] = spawnList[randomIndex];
            spawnList[randomIndex] = temp;
        }

        return spawnList;
    }
}