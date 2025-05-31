using System;
using Unity.Collections;
using UnityEngine;

public class ReadPixelFromChecker : MonoBehaviour
{
    public RenderTexture WallRenderTexture;
    public RenderTexture PoseRenderTexture;

    private Texture2D WallTexture;
    private Texture2D PoseTexture;

    private int TotalPixelCount;

    private int[] PixelCountList;

    private void Awake()
    {
        WallTexture = new Texture2D(WallRenderTexture.width, WallRenderTexture.height, TextureFormat.R8, false);
        PoseTexture = new Texture2D(PoseRenderTexture.width, PoseRenderTexture.height, TextureFormat.R8, false);

        PixelCountList = new int[4];
        
        TotalPixelCount = WallRenderTexture.width * WallRenderTexture.height;
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
        Goal = 1,
        Pose = 2,
        Intersection = Goal | Pose
    }

    public float Check()
    {
        PixelCountList[(int)CheckFlag.Goal] = TotalPixelCount;
        PixelCountList[(int)CheckFlag.Pose] = 0;
        PixelCountList[(int)CheckFlag.Intersection] = 0;
    
        var wallPixels = GetRenderTexturePixels(WallRenderTexture, WallTexture);
        var posePixels = GetRenderTexturePixels(PoseRenderTexture, PoseTexture);

        for (int i = 0; i < TotalPixelCount; i++)
        {
            int wallPixel = wallPixels[i] > 0 ? 1 : 0;
            int posePixel = posePixels[i] > 0 ? 1 : 0;

            PixelCountList[(int)CheckFlag.Goal] -= wallPixel;       
            PixelCountList[(int)CheckFlag.Pose] += posePixel;

            PixelCountList[(int)CheckFlag.Intersection] += (1 - wallPixel) * posePixel;
        }

        var (intersection, pose, goal) = (PixelCountList[(int)CheckFlag.Intersection], PixelCountList[(int)CheckFlag.Pose], PixelCountList[(int)CheckFlag.Goal]);

        float result = (float)(intersection) / (float)(goal + pose - intersection);

        return result;
    }
}
