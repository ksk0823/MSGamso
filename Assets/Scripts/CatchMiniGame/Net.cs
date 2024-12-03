using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Net : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 나비 태그 확인하고 잡기
        if (other.CompareTag("Butterfly"))
        {
            Butterfly butterfly = other.GetComponent<Butterfly>();
            if (butterfly != null)
            {
                butterfly.Caught();
            }
        }
    }
}
