using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetLocker : MonoBehaviour
{
    public GameObject target;
    
    private SwingingArmMotion armMotion;
    
    public bool locked;

    private void Awake()
    {
        armMotion = target.GetComponent<SwingingArmMotion>();
    }
    
    public void Lock()
    {
        locked = true;
        armMotion.enabled = false;
        
        target.transform.position = transform.position;
    }

    public void Unlock()
    {
        locked = false;
        armMotion.enabled = true;
    }
    
    private void Update()
    {
        if (locked)
        {
            //target.transform.position = transform.position;
        }
    }
}
