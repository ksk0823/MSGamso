using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ReadPixelFromChecker : MonoBehaviour
{
    public Camera WallCamera;
    public Camera PoseCamera;

    public RenderTexture WallRenderTexture;
    public RenderTexture PoseRenderTexture;

    private Texture2D WallTexture;
    private Texture2D PoseTexture;


    private int[] PixelCountList;

    private void Awake()
    {
        WallTexture = new Texture2D(WallRenderTexture.width, WallRenderTexture.height, TextureFormat.R8, false);
        PoseTexture = new Texture2D(PoseRenderTexture.width, PoseRenderTexture.height, TextureFormat.R8, false);

        PixelCountList = new int[4];
    }

    private void Start()
    {
        StartCoroutine(Check());
    }

    public NativeArray<byte> GetRenderTexturePixels ( RenderTexture renderTexture, Texture2D texture )
    {
        var currentRenderTexture = RenderTexture.active;

        // Set the new render texture.
        RenderTexture.active = renderTexture;
        texture.ReadPixels ( new Rect ( 0, 0, renderTexture.width, renderTexture.height ), 0, 0 );
        texture.Apply();

        // reapply the previous render texture.
        RenderTexture.active = currentRenderTexture;

        // Return then texture2d pixels. This assumes mipmap level 0.
        return texture.GetRawTextureData<byte>();
    }

    [Flags]
    public enum CheckFlag
    {
        None = 0,
        Wall = 1,
        Pose = 2,
        Intersection = Wall | Pose
    }

    public IEnumerator Check()
    {
        while (true)
        {
            WallCamera.Render();
            PoseCamera.Render();

            // Pixel Count List Clear
            for (int i = 0; i < PixelCountList.Length; i++)
            {
                PixelCountList[i] = 0;
            }
        
            var wallPixels = GetRenderTexturePixels(WallRenderTexture, WallTexture);
            var posePixels = GetRenderTexturePixels(PoseRenderTexture, PoseTexture);

            for (int i = 0; i < wallPixels.Length; i++)
            {
                int wallPixel = wallPixels[i] > 0 ? 1 : 0;
                int posePixel = posePixels[i] > 0 ? 1 : 0;

                int value = wallPixel | (posePixel << 1);

                PixelCountList[value]++;
            }
            
            Debug.Log((float)PixelCountList[(int)CheckFlag.Intersection] / (float)PixelCountList[(int)CheckFlag.Pose]);

            yield return new WaitForSeconds(0.333333f);
        }
    }
}
