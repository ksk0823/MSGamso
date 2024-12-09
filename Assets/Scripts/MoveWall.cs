using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWall : MonoBehaviour
{

    public static System.Action OnWallDestroyed;
    public float speed = 3f;
    public float penalty = 5f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
      
        if (collision.gameObject.tag == "BlockGlass")
        { 
            OnWallDestroyed?.Invoke();
            Destroy(gameObject);

        }
        else if(collision.gameObject.tag == "Player")
        {
            GameManager.Instance.GameTime += penalty;
        }
    }
}
