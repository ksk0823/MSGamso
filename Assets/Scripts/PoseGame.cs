using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class PoseGame : MiniGame
{
    public Camera poseCamera;
    public Camera wallCamera;
    
    [Header("카메라 머티리얼 설정")]
    public Material poseCameraMaterial;
    public Material wallCameraMaterial;

    public int CurrentLevel = 0;
    public int Success = 0;
    public GameObject[] Walls;
    public Transform SpawnPosition;

    private MoveWall moveWall;

    private void Start()
    {
        if (poseCamera != null && poseCameraMaterial != null)
        {
            poseCamera.SetReplacementShader(poseCameraMaterial.shader, "RenderType");
        }

        if (wallCamera != null && wallCameraMaterial != null)
        {
            wallCamera.SetReplacementShader(wallCameraMaterial.shader, "RenderType");
        }
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
