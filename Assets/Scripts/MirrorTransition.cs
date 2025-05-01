using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorTransition : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> objectsToEnable = new List<GameObject>();
    
    [SerializeField]
    private List<GameObject> objectsToDisable = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // 켜야 할 오브젝트들을 활성화
            foreach (GameObject obj in objectsToEnable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }

            // 꺼야 할 오브젝트들을 비활성화
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }

}
