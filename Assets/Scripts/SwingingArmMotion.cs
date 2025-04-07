using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwingingArmMotion : MonoBehaviour
{
    [SerializeField] private GameObject LeftHand;
    [SerializeField] private GameObject RightHand;
    [SerializeField] private GameObject CenterEyeCamera;
    [SerializeField] private GameObject ForwardDirection;

    // 어깨와 루트 조인트 참조
    [SerializeField] private Transform leftShoulder;
    [SerializeField] private Transform rightShoulder;
    [SerializeField] private Transform rootJoint;

    // 이전 프레임의 위치 저장
    private Vector3 LPast;
    private Vector3 RPast;
    private Vector3 PlayerPositionPreviousFrame;
    private Vector3 PlayerPositionThisFrame;

    // 이동 관련 변수
    [SerializeField] private float Weight = 0f;
    [SerializeField] private float baseSpeed = 70f;

    // 상태 판별 임계값
    [SerializeField] private float walkThreshold = 0.1f;
    [SerializeField] private float runThreshold = 0.3f;

    // Weight 감소값
    [SerializeField] private float normalSubtraction = 0.02f;
    [SerializeField] private float walkSubtraction = 0.05f;
    [SerializeField] private float runSubtraction = 0.1f;

    // 비선형 함수 G(x)의 계수
    [SerializeField] private float filterStrength = 5f;

    private CharacterController characterController;

    [SerializeField] private GameObject character;
    [SerializeField] private GameObject rightLeg;
    [SerializeField] private GameObject leftLeg;
    private Animator characterAnimator;

    // 디버깅용
    // [SerializeField] private Text debugText;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterAnimator = character.GetComponent<Animator>();

        PlayerPositionPreviousFrame = transform.localPosition;
        
        // 초기 위치 설정
        LPast = LeftHand.transform.position;
        RPast = RightHand.transform.position;
    }

    private void Update()
    {
        // 카메라 방향에 따라 이동 방향 조정
        float yRotation = CenterEyeCamera.transform.eulerAngles.y;
        ForwardDirection.transform.eulerAngles = new Vector3(0, yRotation, 0);        

        // 현재 위치 가져오기
        Vector3 LCurrent = LeftHand.transform.position;
        Vector3 RCurrent = RightHand.transform.position;
        PlayerPositionThisFrame = transform.localPosition;

        // 1. 어깨와 루트로 평면 생성 (RP)
        Vector3 RPN = Vector3.Cross(
            (rightShoulder.position - rootJoint.position),
            (leftShoulder.position - rootJoint.position)
        ).normalized;

        // 2. 손-어깨 직선 구하기 (DL, DR)
        Vector3 DL = (LCurrent - leftShoulder.position).normalized;
        Vector3 DR = (RCurrent - rightShoulder.position).normalized;

        // 3. 각도 계산 (Lθ, Rθ)
        float Ltheta = Vector3.Angle(RPN, DL) * Mathf.Deg2Rad;
        float Rtheta = Vector3.Angle(RPN, DR) * Mathf.Deg2Rad;

        // 4. 비선형 필터링 값 계산 G(sin(θ))
        float LF = NonLinearFilter(Mathf.Sin(Ltheta));
        float RF = NonLinearFilter(Mathf.Sin(Rtheta));

        // 5. 이동량 계산
        float DifL = Vector3.Distance(LPast, LCurrent) * LF;
        float DifR = Vector3.Distance(RPast, RCurrent) * RF;

        // 플레이어 자체 이동량을 보정
        float playerMovement = Vector3.Distance(PlayerPositionPreviousFrame, PlayerPositionThisFrame);
        DifL = Mathf.Max(0, DifL - playerMovement);
        DifR = Mathf.Max(0, DifR - playerMovement);

        float Move = DifL + DifR;
        Weight += Move;

        // 최대 Weight 제한
        Weight = Mathf.Min(Weight, 0.5f);

        // 6. 상태 판별 및 이동
        if (Weight > runThreshold)
        {
            // 뛰기 상태
            rightLeg.SetActive(false);
            leftLeg.SetActive(false);
            characterController.Move(ForwardDirection.transform.forward.normalized * Weight * baseSpeed * Time.deltaTime);
            characterAnimator.SetTrigger("Running");
            characterAnimator.ResetTrigger("Walking");
            Weight -= runSubtraction * Time.deltaTime;
        }
        else if (Weight >= walkThreshold)
        {
            // 걷기 상태
            rightLeg.SetActive(false);
            leftLeg.SetActive(false);
            characterController.Move(ForwardDirection.transform.forward.normalized * Weight * baseSpeed * Time.deltaTime);
            characterAnimator.SetTrigger("Walking");
            characterAnimator.ResetTrigger("Running");
            Weight -= walkSubtraction * Time.deltaTime;
        }
        else
        {
            // 기본 상태
            rightLeg.SetActive(true);
            leftLeg.SetActive(true);
            characterAnimator.ResetTrigger("Walking");
            characterAnimator.ResetTrigger("Running");
            Weight -= normalSubtraction * Time.deltaTime;
        }

        // Weight가 음수가 되지 않도록 조정
        Weight = Mathf.Max(0, Weight);

        // 디버깅용
        // if (debugText != null)
        //     debugText.text = $"Weight: {Weight:F3}, LF: {LF:F2}, RF: {RF:F2}";

        // 다음 프레임을 위해 현재 위치 저장
        LPast = LCurrent;
        RPast = RCurrent;
        PlayerPositionPreviousFrame = PlayerPositionThisFrame;
    }

    // 비선형 필터링 함수 G(x) = e^(-filterStrength * x^2)
    private float NonLinearFilter(float x)
    {
        return Mathf.Exp(-filterStrength * x * x);
    }
}
