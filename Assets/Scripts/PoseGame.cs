using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class PoseGame : MiniGame
{
    public int CurrentLevel = 0;
    public int Success = 0;
    public GameObject[] Walls;
    public Transform SpawnPosition;

    private MoveWall moveWall;

    public override void Play()
    {
        base.Play();
        MakePose();
    }

    public override void Stop()
    {
        base.Stop();
    }

    public void MakePose()
    {
        moveWall = Instantiate(Walls[CurrentLevel], SpawnPosition.transform.position, Quaternion.Euler(-90, 90, 0)).GetComponent<MoveWall>();
        moveWall.OnWallDestroyed += HandleWallDestroyed;
    }

    private void HandleWallDestroyed()
    {
        if (CurrentLevel < Walls.Length - 1)
        {
            CurrentLevel++;

            MakePose();
        }
        else
        {
            SetCleared();
        }
    }

}
