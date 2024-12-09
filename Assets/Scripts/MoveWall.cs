using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWall : MonoBehaviour
{
    public Action OnWallDestroyed;
    public float speed = 3f;
    public float penalty = 5f;

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "BlockGlass")
        { 
            OnWallDestroyed?.Invoke();

            Destroy(gameObject);

        }
        else if(other.gameObject.tag == "Player")
        {
            GameManager.Instance.GameTime += penalty;
        }
    }
}
