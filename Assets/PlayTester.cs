using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayTester : MonoBehaviour
{
    public MiniGame MiniGame;

    private void Start()
    {
        MiniGame.Play();
    }
}
