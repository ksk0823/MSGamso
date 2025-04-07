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
    
    # ----------------------------------------------------------------
    # 벡터 덧셈 연산 (+)
    # ----------------------------------------------------------------
    def __add__(self, other):
        if isinstance(other, Vector3):
            return Vector3(
                x=self.x + other.x,
                y=self.y + other.y,
                z=self.z + other.z
            )
        return NotImplemented
    
    # ----------------------------------------------------------------
    # 벡터 뺄셈 연산 (-)
    # ----------------------------------------------------------------
    def __sub__(self, other):
        if isinstance(other, Vector3):
            return Vector3(
                x=self.x - other.x,
                y=self.y - other.y,
                z=self.z - other.z
            )
        return NotImplemented
    
    # ----------------------------------------------------------------
    # 벡터 곱셈 연산 (*)
    # 스칼라 값과의 곱셈
    # ----------------------------------------------------------------
    def __mul__(self, other):
        if isinstance(other, (int, float)):
            return Vector3(
                x=self.x * other,
                y=self.y * other,
                z=self.z * other
            )
        return NotImplemented
    
    # ----------------------------------------------------------------
    # 오른쪽 피연산자로서의 곱셈 연산 (*)
    # 스칼라 값과의 곱셈 (왼쪽이 스칼라일 경우)
    # ----------------------------------------------------------------
    def __rmul__(self, other):
        return self.__mul__(other)
    
    # ----------------------------------------------------------------
    # 벡터 나눗셈 연산 (/)
    # 스칼라 값으로 나누기
    # ----------------------------------------------------------------
    def __truediv__(self, other):
        if isinstance(other, (int, float)) and other != 0:
            return Vector3(
                x=self.x / other,
                y=self.y / other,
                z=self.z / other
            )
        return NotImplemented
    
    # ----------------------------------------------------------------
    # 벡터 부호 반전 연산 (-)
    # ----------------------------------------------------------------
    def __neg__(self):
        return Vector3(
            x=-self.x,
            y=-self.y,
            z=-self.z
        )
    
    # ----------------------------------------------------------------
    # 벡터 동등성 비교 (==)
    # ----------------------------------------------------------------
    def __eq__(self, other):
        if not isinstance(other, Vector3):
            return False
        return (self.x == other.x and 
                self.y == other.y and 
                self.z == other.z)
    
    # ----------------------------------------------------------------
    # 벡터 크기(magnitude) 계산
    # ----------------------------------------------------------------
    def magnitude(self):
        return (self.x**2 + self.y**2 + self.z**2)**0.5
    
    # ----------------------------------------------------------------
    # 벡터 정규화 (단위 벡터로 변환)
    # ----------------------------------------------------------------
    def normalize(self):
        mag = self.magnitude()
        if mag > 0:
            return self / mag
        return Vector3()
    
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
