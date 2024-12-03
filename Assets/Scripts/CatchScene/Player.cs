using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    
    public int score = 0;

    void OnEnable()
    {
        Butterfly.OnCaught += IncreaseScore;
    }

    void OnDisable()
    {
        Butterfly.OnCaught -= IncreaseScore;
    }
    

    void IncreaseScore()
    {
        score++;
        Debug.Log("Score: " + score);
    }
    
    
}
