using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

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
        HMD = new TrackedTransform
        {
            Position = HMD.position,
            Rotation = HMD.rotation
        },

        LController = new TrackedTransform
        {
            Position = LController.position,
            Rotation = LController.rotation
        },

        RController = new TrackedTransform
        {
            Position = RController.position,
            Rotation = RController.rotation
        }
    };

    public PoseSnapshot GetSnapshot()
    {
        return new PoseSnapshot
        {
            Pose = Pose, TimeStamp = Time.time
        };
    }
}

// ------------------------------------------------------------
/// <summary>
/// 
/// </summary>
// ------------------------------------------------------------
[Serializable]
public class ArmSwingProcessor
{
    [SerializeReference]
    private IVRTracker Tracker = new VRTracker();

    public const int InitialProcessCollectionCapacity = 60;
    private readonly Queue<PoseSnapshot> poseSnapshotQueue = new(capacity: InitialProcessCollectionCapacity);
    
    // 이전 프레임의 PoseSnapshot (속도 계산 등에 사용)
    private PoseSnapshot? lastPoseSnapshot = null;

    public event Action<PoseSnapshot> OnPoseSnapshotEnqueued = null;

    [SerializeField] private int minSampleCount = 4;
    [SerializeField] private float minSampleTimeWindow = 0.4f;
    [SerializeField] private float maxSampleTimeWindow = 4.0f;
    
    public enum MovementState
    {
        Idle, Walking, Running
    }
    
    // 현재 움직임 상태
    public MovementState CurrentMovementState { get; private set; } = MovementState.Idle;

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
        lastPoseSnapshot = null;

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
        lastPoseSnapshot = null;

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
        
        // 전처리된 데이터를 큐에 추가
        poseSnapshotQueue.Enqueue(poseSnapshot);
        
        // 이전 스냅샷 업데이트
        lastPoseSnapshot = poseSnapshot;

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

        PoseSnapshot GetOldestData() => poseSnapshotQueue.Peek();

        bool IsOutdatedData(PoseSnapshot poseSnapshot) => poseSnapshot.TimeStamp + maxSampleTimeWindow < Time.time;

        // ------------------------------------------------------------
        // 오래된 데이터를 큐에서 제거
        // ------------------------------------------------------------
        while (!IsEmpty() && IsOutdatedData(GetOldestData()))
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
        // 데이터가 충분하지 않으면 상태 업데이트 안함
        if (poseSnapshotQueue.Count < minSampleCount)
        {
            return;
        }
        
        PoseSnapshot GetOldestSnapshot() => poseSnapshotQueue.Peek();

        // 가장 오래된 스냅샷과 가장 최근 스냅샷 사이의 시간 차이 계산
        var (oldest, latest) = (GetOldestSnapshot().TimeStamp, Time.time);

        float span = latest - oldest;
        
        // 최소 처리 시간보다 적은 데이터가 모였으면 처리하지 않음
        if (span < minSampleTimeWindow)
        {
            return;
        }
        
        // 여기에 실제 움직임 상태 결정 로직 구현
        // 컨트롤러 속도, 양팔의 스윙 패턴 등을 분석하여 상태 결정
        CalculateMovementState();
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
        
        // TODO: 팔 스윙 속도 계산 및 움직임 상태 결정 로직 구현
    }
}
