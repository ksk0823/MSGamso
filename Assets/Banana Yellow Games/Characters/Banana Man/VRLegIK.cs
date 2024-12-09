	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	 
	public class VRFootIK : MonoBehaviour
	{
	    private Animator _animator;
	 
	    public Vector3 footOffest;
	    
	    [Range(0,1)]
	    public float rigthFootPosWeight = 1f;
	    [Range(0,1)]
	    public float rigthRotPosWeight = 1f;
	    [Range(0,1)]
	    public float leftFootPosWeight = 1f;
	    [Range(0,1)]
	    public float leftRotPosWeight = 1f;
	    private void Awake()
	    {
	        _animator = GetComponent<Animator>();
	        
	    }
	 
	    private void OnAnimatorIK(int layerIndex)
	    {
	        Vector3 rightFootPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
	        RaycastHit hit;
	 
	        bool hasHit = Physics.Raycast(rightFootPos + Vector3.up, Vector3.down, out hit);
	        if (hasHit)
	        {
	            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rigthFootPosWeight);
	            _animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + footOffest);
	            
	            Quaternion rightRootRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);
	            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rigthRotPosWeight);
	            _animator.SetIKRotation(AvatarIKGoal.RightFoot , rightRootRotation);
	        }
	        else
	        {
	            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
	        }
	        
	        Vector3 leftFootPos = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
	 
	        hasHit = Physics.Raycast(leftFootPos + Vector3.up, Vector3.down, out hit);
	        if (hasHit)
	        {
	            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootPosWeight);
	            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point + footOffest);
	        }
	        else
	        {
	            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
	            
	            Quaternion leftRootRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);
	            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftRotPosWeight);
	            _animator.SetIKRotation(AvatarIKGoal.LeftFoot , leftRootRotation);
	        }
	    }
	}
