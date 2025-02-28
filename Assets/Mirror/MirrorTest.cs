using System.Collections;
using System.Collections.Generic;
using UnityEngine;
	 
public class MirrorTest : MonoBehaviour
{
	Transform player;
	Transform mirror;

	void Start()
	{
		player = Camera.main.transform;
		mirror = gameObject.transform.parent;
	}
	
	private void Update()
	{
		Vector3 CamereDir = player.forward;
		Vector3 Normal = mirror.forward * -1;
		
		Vector3 reflected = Vector3.Reflect(CamereDir, Normal);
		transform.forward = reflected;
	}
}
