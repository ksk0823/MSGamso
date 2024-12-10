using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetGlass : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject glassback;
    public GameObject glassfront;
    public PoseGame posegame;

    private void Awake()
    {
        posegame = gameObject.GetComponentInParent<PoseGame>();
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if(other.gameObject.tag == "Player")
        {
            for(int i = 0;i< 2; i++)
            {
                glassfront.gameObject.SetActive(true);
                glassback.gameObject.GetComponent<MeshRenderer>().enabled = true;
                glassback.gameObject.GetComponent<Collider>().enabled = true;
            }
            posegame.Play();
        }
    }

}
