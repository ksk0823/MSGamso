import threading
from collections import deque
from dataclasses import dataclass
from typing import Dict, Any

from Transform import Vector3, Quaternion

# ================================================================
# 위치와 회전을 표현하는 구조체입니다.
# ================================================================
@dataclass
class TrackedTransform:
    Position: Vector3 = None
    Rotation: Quaternion = None
    
    def __post_init__(self):
        if self.Position is None:
            self.Position = Vector3()
            
        if self.Rotation is None:
            self.Rotation = Quaternion.identity()
    
    @classmethod
    def identity(cls):
        return cls(Vector3(0, 0, 0), Quaternion.identity())
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]):
        if data is None:
            return cls.identity()
            
        position = Vector3.from_dict(data.get('Position', {}))
        rotation = Quaternion.from_dict(data.get('Rotation', {}))
        
        return cls(position, rotation)


# ================================================================
# 추적된 VR 기기의 포즈를 표현하는 구조체입니다.
# ================================================================
@dataclass
class TrackedPose:
    HMD: TrackedTransform = None
    LController: TrackedTransform = None
    RController: TrackedTransform = None
    
    def __post_init__(self):
        if self.HMD is None:
            self.HMD = TrackedTransform.identity()
            
        if self.LController is None:
            self.LController = TrackedTransform.identity()
            
        if self.RController is None:
            self.RController = TrackedTransform.identity()
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]):
        if data is None:
            return cls()
            
        hmd = TrackedTransform.from_dict(data.get('HMD', {}))
        lcon = TrackedTransform.from_dict(data.get('LController', {}))
        rcon = TrackedTransform.from_dict(data.get('RController', {}))
        
        return cls(hmd, lcon, rcon)


# ================================================================
# 현재 포즈 정보를 저장하는 구조체입니다.
# ================================================================
@dataclass
class PoseSnapshot:
    Pose: TrackedPose = None
    TimeStamp: float = 0.0
    
    def __post_init__(self):
        if self.Pose is None:
            self.Pose = TrackedPose()
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]):
        if data is None:
            return cls()
            
        # 타임스탬프 추출
        timestamp = 0.0
        for key in ('TimeStamp', 'timestamp', 'time'):
            if key in data:
                timestamp = float(data[key])
                break
        
        # 포즈 데이터 추출
        pose = None
        if 'Pose' in data:
            pose = TrackedPose.from_dict(data['Pose'])
        else:
            pose = TrackedPose()
        
        return cls(pose, timestamp)

# ================================================================
# 처리된 데이터를 저장하는 구조체입니다.
# ================================================================
@dataclass
class ProcessedData:
    HMDVelocity: Vector3 = None
    LVelocity: Vector3 = None
    RVelocity: Vector3 = None
    DeltaTime: float = 0.0
    TimeStamp: float = 0.0

# ================================================================
# 데이터 관리 및 저장을 담당하는 큐 클래스
# ================================================================
class ProcessedDataQueue:
    
    # ----------------------------------------------------------------
    # 데이터 관리자 초기화
    # 데이터 저장소와 스레드 안전을 위한 락 설정
    # ----------------------------------------------------------------
    def __init__(self):
        # 데이터 저장소 초기화
        self.data : deque[ProcessedData] = deque()
        
        # 마지막 스냅샷 저장
        self.last_snapshot : PoseSnapshot = None
        
        # 스레드 안전 관리
        self.lock = threading.Lock()
    
    # ----------------------------------------------------------------
    # 데이터 추가
    # 데이터를 저장소에 추가
    # ----------------------------------------------------------------
    def enqueue(self, data):
        with self.lock:
            self.data.append(data)
    
    # ----------------------------------------------------------------
    # 데이터 제거
    # 저장소의 마지막 데이터를 제거
    # ----------------------------------------------------------------
    def dequeue(self):
        with self.lock:
            return self.data.popleft()
    
    # ----------------------------------------------------------------
    # 가장 최신 데이터 가져오기
    # 데이터를 제거하지 않고 가장 최근에 추가된 데이터 반환
    # ----------------------------------------------------------------
    def latest(self):
        with self.lock:
            if len(self.data) == 0:
                return None
            return self.data[-1]
        
    # ----------------------------------------------------------------
    # 데이터 존재 여부 확인
    # 저장소에 데이터가 비어있는지 확인
    # ----------------------------------------------------------------
    def empty(self):
        with self.lock:
            return len(self.data) == 0
    
    # ----------------------------------------------------------------
    # 모든 데이터 제거
    # 저장소의 모든 데이터를 제거
    # ----------------------------------------------------------------
    def clear(self):
        with self.lock:
            self.data.clear()
            
            self.last_snapshot = None

    # ----------------------------------------------------------------
    # 데이터 처리
    # ----------------------------------------------------------------
    def process(self, json_data: dict):
        with self.lock:
            try:
                # PoseSnapshot으로 변환
                snapshot = PoseSnapshot.from_dict(json_data)
                
                # 속도 계산을 위한 변수 초기화
                hmd_velocity = Vector3(x=0, y=0, z=0)
                lcon_velocity = Vector3(x=0, y=0, z=0)
                rcon_velocity = Vector3(x=0, y=0, z=0)
                delta_time = 0.0
                
                # 이전 스냅샷이 있는 경우 속도 계산
                if self.last_snapshot is not None:
                    delta_time = snapshot.TimeStamp - self.last_snapshot.TimeStamp
                    
                    (pose, last_pose) = (snapshot.Pose, self.last_snapshot.Pose)

                    if delta_time > 0:
                        # 속도 계산 (현재 위치 - 이전 위치) / 시간
                        hmd_velocity = (pose.HMD.Position - last_pose.HMD.Position) / delta_time
                        lcon_velocity = (pose.LController.Position - last_pose.LController.Position) / delta_time
                        rcon_velocity = (pose.RController.Position - last_pose.RController.Position) / delta_time
                
                # ProcessedData 객체 생성 및 저장
                processed_data = ProcessedData(
                    HMDVelocity=hmd_velocity,
                    LVelocity=lcon_velocity,
                    RVelocity=rcon_velocity,
                    DeltaTime=delta_time,
                    TimeStamp=snapshot.TimeStamp
                )
                
                # 데이터 저장
                self.data.append(processed_data)
                
                # 현재 스냅샷을 마지막 스냅샷으로 저장
                self.last_snapshot = snapshot
            except Exception as e:
                print(f"스냅샷 변환 오류: {e}")