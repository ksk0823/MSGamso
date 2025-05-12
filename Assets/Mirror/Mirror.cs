using UnityEngine;

public class Mirror : MonoBehaviour
{    
    [SerializeField] private Camera playerCam;
    [SerializeField] private Camera mirrorCam;
    [SerializeField] private MeshRenderer quad;

    [SerializeField] private Transform[] corners;
    
    // 거울 평면에 대한 정보
    private Vector3 mirrorPoint => quad.transform.position;
    private Vector3 mirrorNormal => quad.transform.forward;

    // ====================================================================
    // <summary>
    // 초기화 함수: 렌더 텍스처 생성 및 설정
    // </summary>
    // ====================================================================
    private void Awake()
    {
        // 렌더 텍스처 설정
        SetWindowSize();
    }
    
    // ====================================================================
    // <summary>
    // 거울 렌더 텍스처 해상도 설정
    // </summary>
    // ====================================================================
    private void SetWindowSize()
    {        
        float width = quad.transform.lossyScale.x;
        float height = quad.transform.lossyScale.y;
        
        // 픽셀 밀도 계산 (1 유닛당 픽셀 수)
        float pixelsPerUnit = 128;
        
        // 해상도 계산
        int textureWidth = Mathf.RoundToInt(width * pixelsPerUnit);
        int textureHeight = Mathf.RoundToInt(height * pixelsPerUnit);
        
        Debug.Log($"거울 렌더 텍스처 해상도 - {textureWidth} x {textureHeight}");
        
        // 렌더 텍스처 생성
        var renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 24);
        
        var material = quad.material;

        material.mainTexture = mirrorCam.targetTexture = renderTexture;

        material.mainTextureScale = new Vector2(-1, 1);
        material.mainTextureOffset = new Vector2(1, 0);
    }

    // ====================================================================
    // <summary>
    // 매 프레임마다 거울 반사 계산 및 카메라 업데이트
    // </summary>
    // ====================================================================
    private void Update()
    {
        if (playerCam == null || mirrorCam == null) return;
        
        // 카메라 위치 반사
        var mirroredPosition = ReflectPoint(playerCam.transform.position);
        
        // 미러 카메라 위치와 방향 설정 (LookRotation으로 정확한 회전 계산)
        mirrorCam.transform.position = mirroredPosition;
        mirrorCam.transform.rotation = Quaternion.LookRotation(-quad.transform.forward, quad.transform.up);
        
        Matrix4x4 quadFrustumMatrix = CalculateQuadFrustum();
        mirrorCam.projectionMatrix = quadFrustumMatrix;
    }
    
    private Vector3 ReflectPoint(Vector3 point)
    {
        float distance = Vector3.Dot(point - mirrorPoint, mirrorNormal);
        
        return point - 2f * distance * mirrorNormal;
    }

    private Vector3 ReflectForward(Vector3 forward, Vector3 normal)
    {
        return Vector3.Reflect(forward, normal);
    }
    
    // ====================================================================
    // <summary>
    // 쿼드 절두체 계산
    // </summary>
    // ====================================================================
    private Matrix4x4 CalculateQuadFrustum()
    {        
        // ------------------------------------------------------------
        // 월드 -> 카메라 변환
        // ------------------------------------------------------------
        Matrix4x4 worldToCamera = mirrorCam.worldToCameraMatrix;
        
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        for (int i = 0; i < 4; i++)
        {
            Vector3 cornerInCameraSpace = worldToCamera.MultiplyPoint(corners[i].position);
            
            // 카메라 공간에서의 경계 찾기
            minX = Mathf.Min(minX, cornerInCameraSpace.x);
            maxX = Mathf.Max(maxX, cornerInCameraSpace.x);
            minY = Mathf.Min(minY, cornerInCameraSpace.y);
            maxY = Mathf.Max(maxY, cornerInCameraSpace.y);
        }
        
        Vector3 quadCenterInCameraSpace = worldToCamera.MultiplyPoint(quad.transform.position);
        float nearZ = -quadCenterInCameraSpace.z;
        
        nearZ = Mathf.Max(0.001f, nearZ * 0.999f);
        float farZ = nearZ + 100f;
        
        Matrix4x4 frustumMatrix = Matrix4x4.Frustum(minX, maxX, minY, maxY, nearZ, farZ);

        return frustumMatrix;
    }    
}
