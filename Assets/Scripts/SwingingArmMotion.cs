using UnityEngine;

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

    // 안정화를 위한 타이머
    private float stabilizationTimer = 0f;
    [SerializeField] private float stabilizationTime;
    private bool isStabilized = false;

    [Header("이동 관련 변수")]
    [SerializeField] private float MaxWeight;
    [SerializeField] private float Weight = 0f;
    [SerializeField] private float baseSpeed;

    [Header("방향 스무딩")]
    [SerializeField] private float directionSmoothSpeed = 5f;
    private Vector3 smoothedDirection;

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

    // 움직임 정지 감지 변수
    private float accumulatedMovement = 0f;
    private float movementCheckTime = 0f;
    private const float MOVEMENT_CHECK_DURATION = 0.3f;
    private const float MIN_MOVEMENT_THRESHOLD = 0.03f;

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
    
#region 이동 관련 상태 변수
    
    // 이전 프레임의 위치 저장
    private Vector3 LPast;
    private Vector3 RPast;
    private Vector3 PlayerPositionPreviousFrame;
    private Vector3 PlayerPositionThisFrame;

    // 현재 위치 가져오기
    private (Vector3 L, Vector3 R) Current => 
    (
        LeftHand.transform.position, RightHand.transform.position
    );
    
    private (Vector3 L, Vector3 R) Direction => 
    (
        (Current.L - leftShoulder.position).normalized, (Current.R - rightShoulder.position).normalized
    );

#endregion

    // 현재 이동 상태
    private enum MovementState { Idle, Walking, Running }
    private MovementState currentState = MovementState.Idle;

    public bool IsMoving {get; private set;} = false;

    private void Awake()
    {
        GetComponents();
    }

    private void GetComponents()
    {
        characterController = GetComponent<CharacterController>();
        characterAnimator = character.GetComponent<Animator>();
    }

    private void Start()
    {
        // 애니메이션 레이어 가중치 초기화
        characterAnimator.SetLayerWeight(lowerBodyLayer, 1f);
        
        // 애니메이션 파라미터 초기화
        characterAnimator.SetBool(walkAnimParam, false);
        characterAnimator.SetBool(runAnimParam, false);

        // 초기 위치 설정
        ResetPosition();

        stabilizationTimer = 0f;

        isStabilized = false;
        
        // 초기 방향 설정
        if (CenterEyeCamera != null)
        {
            Vector3 initialDirection = CenterEyeCamera.transform.forward;
            initialDirection.y = 0;
            initialDirection.Normalize();
            smoothedDirection = initialDirection;
        }
    }

    private void FixedUpdate()
    {
        // 시작 시 안정화 처리
        if (!HandleStabilization())
        {
            return;
        }
        
        // 카메라 방향에 따라 이동 방향 조정
        UpdateForwardDirection();

        PlayerPositionThisFrame = transform.localPosition;

        // 움직임 계산 및 Weight 업데이트
        CalculateMovementAndUpdateWeight();

        // 이동 및 애니메이션 상태 설정
        ApplyMovementBasedOnWeight();

        // 다음 프레임을 위해 현재 위치 저장
        LPast = LeftHand.transform.position;
        RPast = RightHand.transform.position;

        PlayerPositionPreviousFrame = PlayerPositionThisFrame;
    }

    //============================================================
    // <summary>
    // 위치 초기화
    // </summary>
    //============================================================
    private void ResetPosition()
    {
        PlayerPositionPreviousFrame = PlayerPositionThisFrame = transform.localPosition;

        LPast = LeftHand.transform.position;
        RPast = RightHand.transform.position;
    }

    //============================================================
    // <summary>
    // 안정화 처리를 담당하는 메서드
    // </summary>
    //============================================================
    private bool HandleStabilization()
    {
        if (!isStabilized)
        {
            stabilizationTimer += Time.deltaTime;

            if (stabilizationTimer >= stabilizationTime)
            {
                isStabilized = true;

                ResetPosition();
                
                // 안정화 후에도 Weight를 0으로 설정
                Weight = 0f;
            }
            
            // 안정화 중에는 어떤 이동도 허용하지 않음
            Weight = 0f;

            // IK 활성화 (정지 상태)
            SetMoving(false);

            UpdateAnimationState(MovementState.Idle);

            return false;
        }

        return true;
    }

    //============================================================
    // <summary>
    // 카메라 방향을 기반으로 이동 방향 업데이트 (부드러운 전환 적용)
    // </summary>
    //============================================================
    private void UpdateForwardDirection()
    {
        if (CenterEyeCamera != null)
        {
            // 카메라의 현재 전방 방향 (수평만 사용)
            Vector3 targetDirection = CenterEyeCamera.transform.forward;
            targetDirection.y = 0;
            
            // 방향 벡터가 너무 작지 않은지 확인
            if (targetDirection.magnitude > 0.01f)
            {
                targetDirection.Normalize();
                
                // 스무딩 적용 (Lerp)
                smoothedDirection = Vector3.Lerp(smoothedDirection, targetDirection, Time.deltaTime * directionSmoothSpeed);
                smoothedDirection.Normalize();
                
                // ForwardDirection 게임오브젝트 방향 설정
                ForwardDirection.transform.forward = smoothedDirection;
            }
        }
    }

    //============================================================
    // <summary>
    // 움직임 계산 및 Weight 업데이트
    // </summary>
    //============================================================
    private void CalculateMovementAndUpdateWeight()
    {
        // 1. 어깨와 루트로 평면 생성 (RP)
        var (RPN, up) = CalculateReferentialPlaneNormal();

        Vector3 l = Vector3.ProjectOnPlane(Direction.L, up).normalized;
        Vector3 r = Vector3.ProjectOnPlane(Direction.R, up).normalized;
        
        //벡터분해
        // 4. 비선형 필터링 값 계산 G(sin(θ))
        float LF = NonLinearFilter(Mathf.Sin(Vector3.Angle(RPN, l) * Mathf.Deg2Rad));
        float RF = NonLinearFilter(Mathf.Sin(Vector3.Angle(RPN, r) * Mathf.Deg2Rad));

        
        // 5. 이동량 계산
        float DifL = Vector3.Distance(LPast, Current.L) * LF;
        float DifR = Vector3.Distance(RPast, Current.R) * RF;

        UpdateWeight(DifL, DifR);
    }

    //============================================================
    // <summary>
    // 어깨와 루트로 기준면의 법선 벡터 계산
    // </summary>
    //============================================================
    private (Vector3 rpn, Vector3 up) CalculateReferentialPlaneNormal()
    {
        Vector3 rightShoulderToRoot = rightShoulder.position - rootJoint.position;
        Vector3 leftShoulderToRoot = leftShoulder.position - rootJoint.position;

        return (Vector3.Cross(rightShoulderToRoot, leftShoulderToRoot).normalized, (rightShoulderToRoot + leftShoulderToRoot).normalized);
    }

    //============================================================
    // <summary>
    // Weight 값 업데이트
    // </summary>
    //============================================================
    private void UpdateWeight(float DifL, float DifR)
    {
        // 최소 움직임 임계값 적용 - 임계값 증가
        DifL = DifL > minHandMovementThreshold ? DifL : 0;
        DifR = DifR > minHandMovementThreshold ? DifR : 0;

        float Move = DifL + DifR;
        Move *= 2f;
        Weight += Move;

        // 움직임 정지 감지 로직
        accumulatedMovement += Move;
        movementCheckTime += Time.deltaTime;
        
        if (movementCheckTime >= MOVEMENT_CHECK_DURATION)
        {
            // 0.25초 동안 누적 움직임이 임계값 이하면 정지로 판단
            if (accumulatedMovement <= MIN_MOVEMENT_THRESHOLD)
            {
                Debug.Log("정지 판단");
                Weight = 0f;
            }
            
            // 측정 변수 초기화
            accumulatedMovement = 0f;
            movementCheckTime = 0f;
        }

        // 최대 Weight 제한
        Weight = Mathf.Min(Weight, MaxWeight);
    }

    //============================================================
    // <summary>
    // Weight에 따라 이동 및 애니메이션 상태 적용
    // </summary>
    //============================================================
    private void ApplyMovementBasedOnWeight()
    {
        // 상태 판별 및 이동
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
    }

    // 비선형 필터링 함수 G(x) = e^(-filterStrength * x^2)
    private float NonLinearFilter(float x)
    {
        return Mathf.Exp(-filterStrength * x * x);
    }
    
    //============================================================
    // <summary>
    // 애니메이션 상태 업데이트 함수
    // </summary>
    //============================================================
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

        Debug.Log("New State : " + newState + "Weight : " + Weight);

        // 현재 상태 업데이트
        currentState = newState;
    }

    //============================================================
    // <summary>
    // IK 솔버 활성화/비활성화 설정
    // </summary>
    //============================================================
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
