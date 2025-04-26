using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;

using UnityEngine;

#region 데이터 표현

// ------------------------------------------------------------
/// <summary>
/// 위치와 회전을 표현하는 구조체입니다.
/// </summary>
// ------------------------------------------------------------
[Serializable]
public struct TrackedTransform
{
    public Vector3 Position;
    public Quaternion Rotation;

    public TrackedTransform(Transform transform)
    {
        Position = transform.position;
        Rotation = transform.rotation;
    }

    public static TrackedTransform Identity => new TrackedTransform
    {
        Position = Vector3.zero,
        Rotation = Quaternion.identity
    };
}

// ------------------------------------------------------------
/// <summary>
/// 추적된 VR 기기의 포즈를 표현하는 구조체입니다.
/// </summary>
// ------------------------------------------------------------
[Serializable]
public struct TrackedPose
{
    public TrackedTransform HMD;
    public TrackedTransform LController;
    public TrackedTransform RController;
}

// ------------------------------------------------------------
/// <summary>
/// 현재 포즈 정보를 저장하는 구조체입니다.
/// </summary>
// ------------------------------------------------------------
[Serializable]
public struct PoseSnapshot
{
    public TrackedPose Pose;
    public float TimeStamp;
}

#endregion

#region VR 트래거

// ------------------------------------------------------------
/// <summary>
/// 
/// </summary>
// ------------------------------------------------------------
public interface IVRTracker
{
    public Vector3 Up { get; }
    public TrackedPose Pose { get; }
    public PoseSnapshot GetSnapshot();
}

// ------------------------------------------------------------
/// <summary>
/// 
/// </summary>
// ------------------------------------------------------------
[Serializable]
public class VRTracker : IVRTracker
{
    [SerializeField] private Transform HMD;
    [SerializeField] private Transform LController;
    [SerializeField] private Transform RController;

    public Vector3 Up => Vector3.up;

    public TrackedPose Pose => new TrackedPose
    {
        HMD = new TrackedTransform(HMD),
        LController = new TrackedTransform(LController),
        RController = new TrackedTransform(RController),
    };

    public PoseSnapshot GetSnapshot()
    {
        return new PoseSnapshot
        {
            Pose = Pose, TimeStamp = Time.time
        };
    }
}

#endregion

public enum MovementState
{
    Idle, Walking, Running
}

// ------------------------------------------------------------
/// <summary>
/// 
/// </summary>
// ------------------------------------------------------------
[Serializable]
public class ArmSwingProcessor
{
    [field: SerializeField]
    public ArmSwingAnalyzer Analyzer { get; private set; } = new ArmSwingAnalyzer();

#region 설정

    // --------------------------------------------------------------------------------
    // 최소 [minSampleCount] 개, [minSampleTimeWindow] 초
    // 해당 개수 및 시간 이상의 데이터가 수집되어야 분석을 진행합니다.
    //
    // 최대 [maxSampleCount] 개, [maxSampleTimeWindow] 초
    // 해당 개수 및 시간까지의 데이터만 수집하고 그 이상의 데이터는 제거합니다.
    // --------------------------------------------------------------------------------
    [Header("샘플링")]
    [SerializeField] private int minSampleCount = 4;
    [SerializeField] private int maxSampleCount = 1200;
    [SerializeField] private float minSampleTimeWindow = 0.4f;
    [SerializeField] private float maxSampleTimeWindow = 4.0f;
    
    [Header("분석")]
    [SerializeField] private float analysisInterval = 0.2f;

    [Header("로깅")]
    [SerializeField] private bool log = true;

#endregion

#region 상태

    [SerializeReference]
    private IVRTracker Tracker = new VRTracker();

    // 포즈 스냅샷 큐
    private readonly Queue<PoseSnapshot> poseSnapshotQueue = new(capacity: 100);

    // 가장 오래된 스냅샷을 반환합니다.
    private PoseSnapshot GetOldestSnapshot() => poseSnapshotQueue.Peek();

    // 포즈 스냅샷 배열
    private PoseSnapshot[] poseSnapshotArray = null;
    
    // 현재 움직임 상태
    public MovementState CurrentMovementState { get; private set; } = MovementState.Idle;
    
    private float lastAnalysisTime = 0f;
    
#endregion

#region 이벤트

    public event Action<PoseSnapshot> OnPoseSnapshotEnqueued = null;

#endregion

#region 초기 설정 및 초기화

    public bool IsReady { get; private set; } = false;

    // ------------------------------------------------------------
    /// <summary>
    /// 초기 설정을 진행합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void Initialize()
    {
        CleanUp();

        poseSnapshotQueue.Clear();
        poseSnapshotArray = null;

        IsReady = true;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 초기화합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void CleanUp()
    {
        if (!IsReady) return;

        poseSnapshotQueue.Clear();
        poseSnapshotArray = null;
        
        IsReady = false;
    }

#endregion

    // ------------------------------------------------------------
    /// <summary>
    /// 업데이트
    /// </summary>
    // ------------------------------------------------------------
    public void Update()
    {
        if (!IsReady) return;

        // ------------------------------------------------------------
        // 현재 포즈 스냅샷 전처리 후 큐에 추가
        // ------------------------------------------------------------
        EnqueuePoseSnapshotData();
        
        // ------------------------------------------------------------
        // 오래된 데이터 제거
        // ------------------------------------------------------------
        RemoveOutdatedData();
        
        // ------------------------------------------------------------
        // 움직임 상태 업데이트
        // ------------------------------------------------------------
        UpdateMovementState();
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 현재 포즈 스냅샷을 큐에 추가합니다.
    /// </summary>
    // ------------------------------------------------------------
    private void EnqueuePoseSnapshotData()
    {
        PoseSnapshot poseSnapshot = Tracker.GetSnapshot();
        
        // 최대 샘플 수를 초과하면 오래된 데이터를 제거
        while (poseSnapshotQueue.Count >= maxSampleCount)
        {
            poseSnapshotQueue.Dequeue();
        }

        // 전처리된 데이터를 큐에 추가
        poseSnapshotQueue.Enqueue(poseSnapshot);

        // 이벤트 발생
        OnPoseSnapshotEnqueued?.Invoke(poseSnapshot);
    }
    
    // ------------------------------------------------------------
    /// <summary>
    /// 지정된 시간(sampleTimeWindow)보다 오래된 스냅샷을 제거합니다.
    /// </summary>
    // ------------------------------------------------------------
    private void RemoveOutdatedData()
    {   
        bool IsEmpty() => poseSnapshotQueue.Count == 0;

        bool IsOutdatedData(PoseSnapshot poseSnapshot)
        {
            return poseSnapshot.TimeStamp + maxSampleTimeWindow < Time.time;
        }

        // ------------------------------------------------------------
        // 오래된 데이터를 큐에서 제거
        // ------------------------------------------------------------
        while (!IsEmpty() && IsOutdatedData(GetOldestSnapshot()))
        {
            poseSnapshotQueue.Dequeue();
        }
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 수집된 데이터를 기반으로 현재 움직임 상태를 업데이트합니다.
    /// </summary>
    // ------------------------------------------------------------
    private void UpdateMovementState()
    {
        var (now, deltaTime) = (Time.time, Time.time - lastAnalysisTime);

        // 마지막 분석 이후 최소 시간이 지나지 않았으면 처리하지 않음
        if (deltaTime < analysisInterval)
        {
            return;
        }

        // 데이터가 충분하지 않으면 상태 업데이트 안함
        if (poseSnapshotQueue.Count < minSampleCount)
        {
            return;
        }
        
        // 가장 오래된 스냅샷과 가장 최근 스냅샷 사이의 시간 차이 계산
        var (oldest, latest) = (GetOldestSnapshot().TimeStamp, now);

        float span = latest - oldest;
        
        // 최소 처리 시간보다 적은 데이터가 모였으면 처리하지 않음
        if (span < minSampleTimeWindow)
        {
            return;
        }

        // 여기에 실제 움직임 상태 결정 로직 구현
        // 컨트롤러 속도, 양팔의 스윙 패턴 등을 분석하여 상태 결정
        CalculateMovementState();

        lastAnalysisTime = now;
    }
    
    // ------------------------------------------------------------
    /// <summary>
    /// 수집된 데이터를 분석하여 움직임 상태를 계산합니다.
    /// </summary>
    // ------------------------------------------------------------
    private void CalculateMovementState()
    {

        // 현재는 기본 상태를 Idle로 설정
        // 실제 구현에서는 팔 스윙 속도와 패턴을 분석하여 움직임 상태 결정
        CurrentMovementState = MovementState.Idle;
        
        // 포즈 스냅샷 큐 복사
        int sampleCount = CopyPoseSnapshotQueue(ref poseSnapshotArray);

        // 암스윙 분석
        var result = Analyzer.Analyze(poseSnapshotArray, sampleCount);

        if (result.IsSuccess)
        {   
        // 분석 시간 표시
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            
            if (log)
            {
                // 상세 결과 로깅
                Debug.Log
(
$@"""[{timestamp}] === 암스윙 분석 결과 ===
샘플 수 : {sampleCount}
L 컨트롤러 - 주기 : {result.SwingPeriodL:F2}초 (주파수: {result.DominantFrequencyL:F2}Hz)
R 컨트롤러 - 주기 : {result.SwingPeriodR:F2}초 (주파수: {result.DominantFrequencyR:F2}Hz)"""
);
            }
        }

        // TODO: 팔 스윙 속도 계산 및 움직임 상태 결정 로직 구현
    }

#region 데이터 저장 및 로드

    // ------------------------------------------------------------
    /// <summary>
    /// 현재 PoseSnapshot 큐의 내용을 가져옵니다.
    /// </summary>
    /// <returns>큐의 샘플 수를 반환합니다.</returns>
    // ------------------------------------------------------------
    public int CopyPoseSnapshotQueue(ref PoseSnapshot[] snapshots)
    {
        if (snapshots == null)
        {
            snapshots = new PoseSnapshot[maxSampleCount];
        }

        // 큐의 내용을 배열에 복사
        poseSnapshotQueue.CopyTo(snapshots, 0);

        return poseSnapshotQueue.Count;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 현재 PoseSnapshot 큐의 내용을 JSON 형식으로 변환합니다.
    /// </summary>
    /// <returns>JSON 문자열</returns>
    // ------------------------------------------------------------
    public string ExportPoseSnapshotQueueToJson(bool prettyPrint = false)
    {
        int sampleCount = CopyPoseSnapshotQueue(ref poseSnapshotArray);

        Debug.Log($"{sampleCount}개의 포즈 스냅샷 데이터를 내보냅니다.");

        return JsonUtility.ToJson(new PoseSnapshotCollection { Snapshots = poseSnapshotArray }, prettyPrint);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 현재 PoseSnapshot 큐의 내용을 지정된 경로에 JSON 파일로 저장합니다.
    /// </summary>
    /// <param name="path">저장할 파일 경로</param>
    /// <param name="prettyPrint">JSON 형식 깔끔하게 저장할지 여부</param>
    /// <returns>성공 여부</returns>
    // ------------------------------------------------------------
    public bool SavePoseSnapshotQueueToJsonFile(string path, bool prettyPrint = true)
    {
        try
        {
            string json = ExportPoseSnapshotQueueToJson(prettyPrint);

            // 파일로 저장
            File.WriteAllText(path, json);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"PoseSnapshot 저장 중 오류 발생: {e.Message}");

            return false;
        }
    }

#endregion

}

// ------------------------------------------------------------
/// <summary>
/// JSON 직렬화를 위한 PoseSnapshot 컬렉션 클래스
/// </summary>
// ------------------------------------------------------------
[Serializable]
public class PoseSnapshotCollection
{
    public PoseSnapshot[] Snapshots;
}
