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

    [Header("어깨와 루트 조인트 참조")]
    [SerializeField] private Transform leftShoulder;
    [SerializeField] private Transform rightShoulder;
    [SerializeField] private Transform rootJoint;

    // 이전 프레임의 위치 저장
    private Vector3 LPast;
    private Vector3 RPast;
    private Vector3 PlayerPositionPreviousFrame;
    private Vector3 PlayerPositionThisFrame;
    
    // 안정화를 위한 타이머
    private float stabilizationTimer = 0f;
    [SerializeField] private float stabilizationTime;
    private bool isStabilized = false;

    [Header("이동 관련 변수")]
    [SerializeField] private float Weight = 0f;
    [SerializeField] private float baseSpeed;

    [Header("상태 판별 임계값")]
    [SerializeField] private float walkThreshold;
    [SerializeField] private float runThreshold;
    [SerializeField] private float minHandMovementThreshold;

    [Header("Weight 감소값")]
    [SerializeField] private float normalSubtraction;
    [SerializeField] private float walkSubtraction;
    [SerializeField] private float runSubtraction;

    [Header("비선형 함수 G(x)의 계수")]
    [SerializeField] private float filterStrength;

    private CharacterController characterController;

    [Header("캐릭터")]
    [SerializeField] private GameObject character;
    [SerializeField] private IKFootSolver rightFoot;
    [SerializeField] private IKFootSolver leftFoot;
    private Animator characterAnimator;

    [Header("애니메이션 제어 변수")]
    [SerializeField] private string walkAnimParam = "isWalking";
    [SerializeField] private string runAnimParam = "isRunning";
    [SerializeField] private int lowerBodyLayer = 1; // 하체 애니메이션 레이어 인덱스

    // 현재 이동 상태
    private enum MovementState { Idle, Walking, Running }
    private MovementState currentState = MovementState.Idle;

    public bool IsMoving {get; private set;} = false;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterAnimator = character.GetComponent<Animator>();

        // 애니메이션 레이어 가중치 초기화
        characterAnimator.SetLayerWeight(lowerBodyLayer, 1f);
        
        // 애니메이션 파라미터 초기화
        characterAnimator.SetBool(walkAnimParam, false);
        characterAnimator.SetBool(runAnimParam, false);

        // 초기 위치 설정
        PlayerPositionPreviousFrame = transform.localPosition;
        PlayerPositionThisFrame = transform.localPosition;
        LPast = LeftHand.transform.position;
        RPast = RightHand.transform.position;

        stabilizationTimer = 0f;
        isStabilized = false;
    }

    private void FixedUpdate()
    {
        // 시작 시 안정화 시간 부여
        if (!isStabilized)
        {
            stabilizationTimer += Time.deltaTime;
            if (stabilizationTimer >= stabilizationTime)
            {
                isStabilized = true;
                // 안정화 완료 후 현재 위치를 기준점으로 다시 설정
                LPast = LeftHand.transform.position;
                RPast = RightHand.transform.position;
                PlayerPositionPreviousFrame = transform.localPosition;
                PlayerPositionThisFrame = transform.localPosition;
                Weight = 0f; // 안정화 후에도 Weight를 0으로 설정
            }
            
            // 안정화 중에는 어떤 이동도 허용하지 않음
            Weight = 0f;
            // IK 활성화 (정지 상태)
            SetMoving(false);
            UpdateAnimationState(MovementState.Idle);
            return;
        }
        
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

        // 팔의 방향 벡터 계산 (양손의 평균 방향)
        Vector3 armDirection = ((LCurrent - leftShoulder.position) + (RCurrent - rightShoulder.position)).normalized;
        
        // 원래 팔 방향 저장 (수직 성분 포함)
        Vector3 originalArmDirection = armDirection;
        
        // XZ 평면에 정사영 (Y축 성분을 완전히 제거하고 재정규화)
        armDirection.y = 0;
        
        // 정규화 전에 벡터가 영벡터인지 체크
        if (armDirection.magnitude > 0.01f)
        {
            armDirection.Normalize();
            
            // 카메라 방향의 수평 성분 구하기
            Vector3 cameraForward = ForwardDirection.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            // 카메라와 팔 방향의 내적으로 앞/뒤 판단
            float dotProduct = Vector3.Dot(cameraForward, armDirection);
            
            Vector3 finalDirection;
            
            // 팔 방향이 카메라 앞쪽 방향(90도 이내)인 경우에만 팔 방향 반영
            if (dotProduct > 0)
            {
                // 팔 방향만 사용 (카메라 방향과의 혼합 제거)
                finalDirection = armDirection;
            }
            else
            {
                // 팔 방향이 카메라의 뒤쪽을 향하면 원래 팔 방향의 XZ 성분 방향 반전
                finalDirection = -armDirection;
            }
            
            // 이동 방향 벡터 저장 (항상 수평 유지)
            finalDirection.y = 0;
            finalDirection.Normalize();
            ForwardDirection.transform.forward = finalDirection;
        }
        else
        {
            // 팔 방향이 거의 수직인 경우, 원래 팔 방향에서 Y성분만 약화시키고 사용
            Vector3 modifiedDirection = originalArmDirection;
            // Y성분 약화 (완전히 제거하지 않음)
            modifiedDirection.y *= 0.2f;
            
            // 벡터 정규화
            if (modifiedDirection.magnitude > 0.01f)
            {
                modifiedDirection.Normalize();
                ForwardDirection.transform.forward = modifiedDirection;
            }
        }

        // 4. 비선형 필터링 값 계산 G(sin(θ))
        float LF = NonLinearFilter(Mathf.Sin(Vector3.Angle(RPN, DL) * Mathf.Deg2Rad));
        float RF = NonLinearFilter(Mathf.Sin(Vector3.Angle(RPN, DR) * Mathf.Deg2Rad));

        // 5. 이동량 계산
        float DifL = Vector3.Distance(LPast, LCurrent) * LF;
        float DifR = Vector3.Distance(RPast, RCurrent) * RF;

        // 최소 움직임 임계값 적용 - 임계값 증가
        float minThreshold = minHandMovementThreshold * 2f; // 더 높은 임계값 적용
        DifL = DifL > minThreshold ? DifL : 0;
        DifR = DifR > minThreshold ? DifR : 0;

        // 플레이어 자체 이동량을 보정
        float playerMovement = Vector3.Distance(PlayerPositionPreviousFrame, PlayerPositionThisFrame);
        DifL = Mathf.Max(0, DifL - playerMovement);
        DifR = Mathf.Max(0, DifR - playerMovement);

        float Move = DifL + DifR;
        
        // 움직임이 없으면 Weight 빠르게 감소
        if (Move < minThreshold)
        {
            Weight -= normalSubtraction * 3f * Time.deltaTime; // 더 빠르게 감소
        }
        else
        {
            Weight += Move;
        }

        // 최대 Weight 제한
        Weight = Mathf.Min(Weight, 1f);

        // 6. 상태 판별 및 이동
        if (Weight > runThreshold)
        {
            // 뛰기 상태
            characterController.Move(ForwardDirection.transform.forward * Weight * baseSpeed * Time.deltaTime);
            UpdateAnimationState(MovementState.Running);
            
            // IK 비활성화 (다리는 애니메이션만으로 제어)
            SetMoving(false);
            
            Weight -= runSubtraction * Time.deltaTime;
        }
        else if (Weight >= walkThreshold)
        {
            // 걷기 상태
            characterController.Move(ForwardDirection.transform.forward * Weight * baseSpeed * Time.deltaTime);
            UpdateAnimationState(MovementState.Walking);
            
            // IK 비활성화 (다리는 애니메이션만으로 제어)
            SetMoving(false);
            
            Weight -= walkSubtraction * Time.deltaTime;
        }
        else
        {
            // 기본 상태 (정지)
            UpdateAnimationState(MovementState.Idle);
            
            // IK 활성화 (다리 IK 제어)
            SetMoving(true);
            
            // Weight가 매우 작으면 완전히 0으로 설정
            if (Weight < 0.01f) 
            {
                Weight = 0f;
            }
            else
            {
                Weight -= normalSubtraction * Time.deltaTime;
            }
        }

        // Weight가 음수가 되지 않도록 조정
        Weight = Mathf.Max(0, Weight);

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
    
    // 애니메이션 상태 업데이트 함수
    private void UpdateAnimationState(MovementState newState)
    {
        // 새 상태 활성화
        switch(newState)
        {
            case MovementState.Walking:
                characterAnimator.SetBool(walkAnimParam, true);
                characterAnimator.SetBool(runAnimParam, false);
                break;
            case MovementState.Running:
                characterAnimator.SetBool(walkAnimParam, false);
                characterAnimator.SetBool(runAnimParam, true);
                break;
            case MovementState.Idle:
                characterAnimator.SetBool(walkAnimParam, false);
                characterAnimator.SetBool(runAnimParam, false);
                break;
        }
        
        // 애니메이터 업데이트 강제 적용
        if (currentState != newState)
        {
            // 상태가 변경되면 애니메이터를 강제로 업데이트
            characterAnimator.Update(Time.deltaTime);
        }
        Debug.Log("newState : " + newState + "Weight : " + Weight);
        // 현재 상태 업데이트
        currentState = newState;
    }

    public void SetMoving(bool value)
    {
        if (value != IsMoving)
        {
            rightFoot.ResetPosition();
            leftFoot.ResetPosition();
        }

        IsMoving = value;
        
        rightFoot.enabled = value;
        leftFoot.enabled = value;
    }
}
