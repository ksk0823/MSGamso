using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetLocker : MonoBehaviour
{
    public GameObject target;
    public bool Teleport = true;
    public bool MoveLock = true;
    
    private SwingingArmMotion armMotion;
    
    public bool locked;

    private void Awake()
    {
        armMotion = target.GetComponent<SwingingArmMotion>();
    }
    
    public void Lock()
    {
        locked = true;
        
        if (MoveLock)
        armMotion.enabled = false;
        
        if (Teleport)
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
