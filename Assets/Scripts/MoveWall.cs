using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWall : MonoBehaviour
{
    public Action OnWallDestroyed;
    public float speed = 3f;
    public float TimePenalty = 5f;

    private ReadPixelFromChecker readPixelFromChecker;

    private void Awake()
    {
        readPixelFromChecker = GetComponent<ReadPixelFromChecker>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BlockGlass"))
        { 
            OnWallDestroyed?.Invoke();

            Destroy(gameObject);

        }
        else if(other.CompareTag("Player"))
        {
            Debug.Log(other.gameObject.name);

            float intersection = readPixelFromChecker.Check();

            Debug.Log($"포즈 비율 (0 - 1) - {intersection}");

            if (intersection < 0.15f)
            {
                GameManager.Instance.GameTime += TimePenalty;

                Debug.Log($"시간 {TimePenalty}초 패널티");
            }
        }
    }
}
