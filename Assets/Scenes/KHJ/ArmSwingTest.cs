using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void OnPoseSnapshotEnqueued(PoseSnapshot poseSnapshot)
    {
        Communicator.Send(poseSnapshot);
    }
}
