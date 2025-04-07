import json
import threading
from collections import deque
from dataclasses import dataclass
from typing import Dict, Any

# ================================================================
# 위치를 표현하는 구조체입니다. (Vector3에 해당)
# ================================================================
@dataclass
class Vector3:
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]):
        if data is None:
            return cls()
            
        return cls(
            x=float(data.get('x', 0.0)),
            y=float(data.get('y', 0.0)),
            z=float(data.get('z', 0.0))
        )


# ================================================================
# 회전을 표현하는 구조체입니다. (Quaternion에 해당)
# ================================================================
@dataclass
class Quaternion:
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    w: float = 1.0
    
    @classmethod
    def identity(cls):
        return cls(0.0, 0.0, 0.0, 1.0)
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]):
        if data is None:
            return cls.identity()
            
        return cls(
            x=float(data.get('x', 0.0)),
            y=float(data.get('y', 0.0)),
            z=float(data.get('z', 0.0)),
            w=float(data.get('w', 1.0))
        )


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
# 데이터 관리 및 저장을 담당하는 클래스
# TCP 서버로부터 받은 VR 기기 데이터를 관리하고 저장
# ================================================================
class DataManager:
    
    # ----------------------------------------------------------------
    # 데이터 관리자 초기화
    # 데이터 저장소와 스레드 안전을 위한 락 설정
    # ----------------------------------------------------------------
    def __init__(self, max_data_points=100):
        # 데이터 저장소 초기화
        self.data = deque(maxlen=max_data_points)
        
        # 스레드 안전 관리
        self.lock = threading.Lock()
    
    # ----------------------------------------------------------------
    # 데이터 추가
    # 수신된 데이터를 스레드 안전하게 저장소에 추가
    # ----------------------------------------------------------------
    def add_data(self, data):
        with self.lock:
            self.data.append(data)
    
    # ----------------------------------------------------------------
    # 데이터 접근
    # 저장된 데이터를 리스트 형태로 반환
    # ----------------------------------------------------------------
    def get_data(self):
        with self.lock:
            return list(self.data)
    
    # ----------------------------------------------------------------
    # 데이터 존재 여부 확인
    # 저장소에 데이터가 있는지 확인
    # ----------------------------------------------------------------
    def has_data(self):
        with self.lock:
            return len(self.data) > 0
    
    # ----------------------------------------------------------------
    # 모든 데이터 삭제
    # 저장소의 모든 데이터를 안전하게 삭제
    # ----------------------------------------------------------------
    def clear_data(self):
        with self.lock:
            self.data.clear()
