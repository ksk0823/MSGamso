using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ================================================================
// ArmSwingProcessor를 사용하고 테스트하기 위한 컴포넌트
// ================================================================
public class ArmSwingTest : MonoBehaviour
{
    [field: SerializeField]
    public ArmSwingProcessor Processor { get; private set; } = new ArmSwingProcessor();

    [field: SerializeField]
    public PythonCommunicator Communicator { get; private set; } = null;

    private void Awake()
    {
        Processor.OnPoseSnapshotEnqueued += OnPoseSnapshotEnqueued;
        Processor.Initialize();
    }

    private void OnDestroy()
    {
        Processor.OnPoseSnapshotEnqueued -= OnPoseSnapshotEnqueued;
        Processor.CleanUp();
    }

    private void Update()
    {
        Processor.Update();
    }

    private void OnPoseSnapshotEnqueued(PoseSnapshot snapshot)
    {
        // Communicator.Send(snapshot);
    }
}
