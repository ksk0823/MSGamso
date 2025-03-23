using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwingingArmMotion : MonoBehaviour
{
    public GameObject LeftHand;
    public GameObject RightHand;
    public GameObject CenterEyeCamera;
    public GameObject ForwardDirection;

    //public Text rotationText;

    private Vector3 PositionPreviousFrameLeftHand;
    private Vector3 PositionPreviousFrameRightHand;
    private Vector3 PlayerPositionPreviousFrame;
    private Vector3 PlayerPositionThisFrame;
    private Vector3 PositionThisFrameLeftHand;
    private Vector3 PositionThisFrameRightHand;

    private Vector3 RotationPreviousFrameLeftHand;
    private Vector3 RotationPreviousFrameRightHand;
    private Vector3 RotationThisFrameLeftHand;
    private Vector3 RotationThisFrameRightHand;

    public float speed = 70;
    [SerializeField] float HandSpeed;

    private CharacterController characterController;

    public GameObject character;
    public GameObject rightLeg;
    public GameObject leftLeg;
    private Animator characterAnimator;

    // 어깨와 루트 조인트 참조 추가
    public Transform leftShoulder;
    public Transform rightShoulder;
    public Transform rootJoint;

    // 속도 임계값 추가
    public float walkThreshold = 0.05f;
    public float runThreshold = 0.08f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        characterAnimator = character.GetComponent<Animator>();

        PlayerPositionPreviousFrame = transform.localPosition;
        
        PositionPreviousFrameLeftHand = LeftHand.transform.localPosition;
        PositionPreviousFrameRightHand = RightHand.transform.localPosition;

        RotationPreviousFrameLeftHand = LeftHand.transform.localEulerAngles;
        RotationPreviousFrameRightHand = RightHand.transform.localEulerAngles;
    }

    private void Update()
    {
        float yRotation = CenterEyeCamera.transform.eulerAngles.y;
        ForwardDirection.transform.eulerAngles = new Vector3(0, yRotation, 0);        

        // 현재 왼손-오른손 컨트롤러의 위치
        PositionThisFrameLeftHand = LeftHand.transform.localPosition;
        PositionThisFrameRightHand = RightHand.transform.localPosition;

        PlayerPositionThisFrame = transform.localPosition;

        RotationThisFrameLeftHand = LeftHand.transform.localEulerAngles;
        RotationThisFrameRightHand = RightHand.transform.localEulerAngles;

        // 이전 왼손-오른손 컨트롤러의 위치와의 차이값 가져오기
        var playerDistanceMoved = Vector3.Distance(PlayerPositionThisFrame, PlayerPositionPreviousFrame);
        var leftHandDistanceMoved = Vector3.Distance(PositionPreviousFrameLeftHand, PositionThisFrameLeftHand);
        var rightHandDistanceMoved = Vector3.Distance(PositionPreviousFrameRightHand, PositionThisFrameRightHand);

        var leftHandRotationMoved = Vector3.Angle(RotationPreviousFrameLeftHand, RotationThisFrameLeftHand);
        var rightHandRotationMoved = Vector3.Angle(RotationPreviousFrameRightHand, RotationThisFrameRightHand);

        // 어깨와 루트로 평면 생성
        Vector3 shoulderPlaneNormal = Vector3.Cross(
            (rightShoulder.position - rootJoint.position),
            (leftShoulder.position - rootJoint.position)
        ).normalized;

        // 각 팔의 방향 벡터
        Vector3 leftArmDirection = (LeftHand.transform.position - leftShoulder.position).normalized;
        Vector3 rightArmDirection = (RightHand.transform.position - rightShoulder.position).normalized;

        // 평면과 팔이 이루는 각도 계산
        float leftArmAngle = Vector3.Angle(shoulderPlaneNormal, leftArmDirection);
        float rightArmAngle = Vector3.Angle(shoulderPlaneNormal, rightArmDirection);

        // 각도와 거리를 모두 고려한 속도 계산
        float angleInfluence = (leftArmAngle + rightArmAngle) / 360f; // 0~1 사이 값으로 정규화
        HandSpeed = (leftHandDistanceMoved + rightHandDistanceMoved) * angleInfluence;

        //rotationText.text = HandSpeed.ToString();

        //if (Time.timeSinceLevelLoad > 1f)

        if (HandSpeed > 0.1f)
            HandSpeed = 0.1f;

        if (HandSpeed > runThreshold)
        {
            rightLeg.SetActive(false);
            leftLeg.SetActive(false);
            characterController.Move(ForwardDirection.transform.forward.normalized * HandSpeed * speed);
            characterAnimator.SetTrigger("Running");
        }
        else if (HandSpeed > walkThreshold)
        {
            rightLeg.SetActive(false);
            leftLeg.SetActive(false);
            characterController.Move(ForwardDirection.transform.forward.normalized * HandSpeed * speed);
            characterAnimator.SetTrigger("Walking");
        }
        else
        {
            rightLeg.SetActive(true);
            leftLeg.SetActive(true);
            characterAnimator.ResetTrigger("Walking");
            characterAnimator.ResetTrigger("Running");
        }
        // transform.position += ForwardDirection.transform.forward * HandSpeed * speed * Time.deltaTime;
        PositionPreviousFrameLeftHand = PositionThisFrameLeftHand;
        PositionPreviousFrameRightHand = PositionThisFrameRightHand;

        RotationPreviousFrameLeftHand = RotationThisFrameLeftHand;
        RotationPreviousFrameRightHand = RotationThisFrameRightHand;

        PlayerPositionPreviousFrame = PlayerPositionThisFrame;
    }
}
