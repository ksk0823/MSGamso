using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButterflyGenerator : MonoBehaviour
{
    public GameObject butterflyPrefab;
    public int numberOfButterflies = 10;
    public Vector3 spawnAreaCenter = Vector3.zero;
    public Vector3 spawnAreaSize = new Vector3(20, 3, 20);

    void Start()
    {
        GenerateButterflies();
    }

    void GenerateButterflies()
    {
        if (butterflyPrefab == null)
        {
            return;
        }

        for (int i = 0; i < numberOfButterflies; i++)
        {
            Vector3 randomPosition = GetRandomPositionWithinArea();
            GameObject butterfly = Instantiate(butterflyPrefab, randomPosition, Quaternion.identity);
            
            // 개별 나비에 로컬 이동 영역 설정
            Butterfly butterflyScript = butterfly.GetComponent<Butterfly>();
            if (butterflyScript != null)
            {
                butterflyScript.areaCenter = transform.position; // Generator의 월드 좌표를 중심으로
                butterflyScript.areaSize = spawnAreaSize;
            }
            
            butterfly.transform.parent = transform;
            butterfly.tag = "Butterfly"; // 태그 추가
            Debug.Log($"Butterfly {i} 생성 완료 at {randomPosition}");
        }
    }

    Vector3 GetRandomPositionWithinArea()
    {
        float randomX = Random.Range(spawnAreaCenter.x - spawnAreaSize.x / 2, spawnAreaCenter.x + spawnAreaSize.x / 2);
        float randomY = Random.Range(spawnAreaCenter.y - spawnAreaSize.y / 2, spawnAreaCenter.y + spawnAreaSize.y / 2);
        float randomZ = Random.Range(spawnAreaCenter.z - spawnAreaSize.z / 2, spawnAreaCenter.z + spawnAreaSize.z / 2);

        return new Vector3(randomX, randomY, randomZ);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
    }
}
