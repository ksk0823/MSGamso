using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Butterfly : MonoBehaviour
{
    [Header("Flight Settings")]
    public float speed = 2.0f; // 나비의 이동 속도
    public float changeDirectionInterval = 3.0f; // 방향을 변경하는 간격 (초)

    [Header("Boundary Settings")]
    public Vector3 areaCenter = Vector3.zero; // 나비가 날아다니는 영역의 중심
    public Vector3 areaSize = new Vector3(20, 3, 20); // 나비가 날아다니는 영역 크기

    private Vector3 targetPosition;
    
    public delegate void ButterflyCaught();
    public event ButterflyCaught OnCaught;
    
    // Start is called before the first frame update
    void Start()
    {
        SetRandomTargetPosition();
        InvokeRepeating(nameof(SetRandomTargetPosition), changeDirectionInterval, changeDirectionInterval);
    }

    // Update is called once per frame
    void Update()
    {
        MoveTowardsTarget();
    }
    
    void SetRandomTargetPosition()
    {
        float randomX = Random.Range(areaCenter.x - areaSize.x / 2, areaCenter.x + areaSize.x / 2);
        float randomY = Random.Range(areaCenter.y - areaSize.y / 2, areaCenter.y + areaSize.y / 2);
        float randomZ = Random.Range(areaCenter.z - areaSize.z / 2, areaCenter.z + areaSize.z / 2);

        targetPosition = new Vector3(randomX, randomY, randomZ);
    }
    
    void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetRandomTargetPosition();
        }
    }
    
    public void Caught()
    {
        // 이벤트로 잡혔음을 알림
        OnCaught?.Invoke();
        Destroy(gameObject); // 나비 제거
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(areaCenter, areaSize);
    }
}
