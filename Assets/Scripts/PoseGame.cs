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


    private void OnDestroy()
    {
        MoveWall.OnWallDestroyed -= HandleWallDestroyed;
    }
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
        Instantiate(Walls[CurrentLevel], SpawnPosition.transform.position, Quaternion.Euler(-90, 90, 0));
    }

    private void HandleWallDestroyed()
    {
        if (CurrentLevel < 4)
        {
            CurrentLevel++;
            MakePose();
        }
        else
        {
            base.SetCleared();
        }
    }

}
