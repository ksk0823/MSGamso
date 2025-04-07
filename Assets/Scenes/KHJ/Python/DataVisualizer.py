import json
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk
import numpy as np
from collections import deque
import matplotlib.font_manager as fm
import platform
import threading
import tkinter as tk
from tkinter import ttk
import time
from DataManager import PoseSnapshot, TrackedPose, TrackedTransform

# 운영체제별 한글 폰트 설정
system = platform.system()
if system == 'Windows':
    plt.rcParams['font.family'] = 'Malgun Gothic'
elif system == 'Darwin':  # macOS
    plt.rcParams['font.family'] = 'AppleGothic'
else:  # Linux 등
    plt.rcParams['font.family'] = 'NanumGothic'

# 마이너스 기호 깨짐 방지
plt.rcParams['axes.unicode_minus'] = False

# 그래프 스타일 설정
plt.style.use('ggplot')


# ================================================================
# 2D 그래프를 관리하는 클래스
# 단일 채널(X, Y, Z)의 시간에 따른 변화를 시각화
# ================================================================
class PositionGraph2D:
    def __init__(self, ax, name, color, title):
        self.ax = ax          # matplotlib 축 객체
        self.name = name      # 그래프 이름 (예: "HMD X")
        self.color = color    # 그래프 색상
        self.title = title    # 그래프 제목
        
        # 축 기본 설정
        self.ax.set_xlabel('시간 (초)')
        self.ax.set_ylabel('위치 값')
        self.ax.set_title(title, fontsize=10)
        self.ax.grid(True, alpha=0.3)
        self.line, = self.ax.plot([], [], color=self.color, label=self.name, linewidth=1.5)
        self.ax.legend(loc='upper right', fontsize=8)
    
    def update(self, times, values):
        if not times or not values:
            return
        
        # 기존 라인 데이터 업데이트
        self.line.set_data(times, values)
        
        # y 범위 동적 조정
        if values:
            values_array = np.array(values)
            y_min, y_max = values_array.min(), values_array.max()
            margin = max(0.1, (y_max - y_min) * 0.1)  # 최소 마진 설정
            if margin == 0:  # 값이 모두 같을 경우
                margin = 0.5
            self.ax.set_ylim(y_min - margin, y_max + margin)
            
            # x 범위 설정
            if times:
                self.ax.set_xlim(min(times), max(times))
        
        # 최신 값 표시
        for txt in self.ax.texts:
            txt.remove()
            
        if values:
            latest_value = values[-1]
            self.ax.text(0.05, 0.95, f"{latest_value:.2f}", 
                        transform=self.ax.transAxes, fontsize=8,
                        verticalalignment='top', bbox={'facecolor': 'white', 'alpha': 0.7, 'pad': 3})
        
        return self.line,


# ================================================================
# 단일 장치(HMD, 컨트롤러 등)의 위치 데이터를 관리하고 시각화하는 클래스
# ================================================================
class DeviceGraphs:
    def __init__(self, name, max_data_points=100):
        self.name = name    # 장치 이름 (예: "HMD", "LController")
        
        # 위치 데이터 저장
        self.x_data = deque(maxlen=max_data_points)
        self.y_data = deque(maxlen=max_data_points)
        self.z_data = deque(maxlen=max_data_points)
        
        # 그래프 객체 (나중에 설정)
        self.x_graph = None
        self.y_graph = None
        self.z_graph = None
    
    def setup_graphs(self, fig, row_index, col_index):
        # 디스플레이 이름 설정
        if self.name == "HMD":
            display_name = "HMD"
        elif self.name == "LController":
            display_name = "왼쪽 컨트롤러"
        elif self.name == "RController":
            display_name = "오른쪽 컨트롤러"
        else:
            display_name = self.name
        
        # 2D 그래프 생성
        x_ax = fig.add_subplot(3, 3, row_index * 3 + col_index + 1)
        y_ax = fig.add_subplot(3, 3, row_index * 3 + col_index + 2)
        z_ax = fig.add_subplot(3, 3, row_index * 3 + col_index + 3)
        
        # 그래프 객체 생성
        self.x_graph = PositionGraph2D(x_ax, f"{display_name} X", 'red', f"{display_name} X")
        self.y_graph = PositionGraph2D(y_ax, f"{display_name} Y", 'green', f"{display_name} Y")
        self.z_graph = PositionGraph2D(z_ax, f"{display_name} Z", 'blue', f"{display_name} Z")
    
    def add_data(self, position):
        self.x_data.append(position.x)
        self.y_data.append(position.y)
        self.z_data.append(position.z)
    
    def update_graphs(self, times):
        # 2D 그래프 업데이트
        self.x_graph.update(times, self.x_data)
        self.y_graph.update(times, self.y_data)
        self.z_graph.update(times, self.z_data)


# ================================================================
# Tkinter 기반 UI 클래스
# VR 위치 데이터 시각화를 위한 사용자 인터페이스
# ================================================================
class TkinterApp:
    def __init__(self, visualizer):
        self.visualizer = visualizer
        
        # Tkinter 창 생성
        self.root = tk.Tk()
        self.root.title("VR 장치 위치 데이터 시각화")
        self.root.geometry("1200x800")
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
        
        # UI 구성
        self.setup_ui()
        
        # 업데이트 타이머
        self.update_delay = 50  # 50ms 간격으로 업데이트
        self.running = False
        
    def setup_ui(self):
        # 상단 메뉴 프레임
        menu_frame = ttk.Frame(self.root, padding=10)
        menu_frame.pack(fill=tk.X)
        
        # 시작/정지 버튼
        self.start_button = ttk.Button(menu_frame, text="시각화 시작", command=self.toggle_visualization)
        self.start_button.pack(side=tk.LEFT, padx=5)
        
        # 정보 표시 레이블
        self.info_label = ttk.Label(menu_frame, text="FPS: 0")
        self.info_label.pack(side=tk.RIGHT, padx=5)
        
        # 그래프 영역 생성
        self.graph_frame = ttk.Frame(self.root)
        self.graph_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
        
        # 그래프 생성
        self.fig = plt.Figure(figsize=(12, 8), dpi=100)
        self.canvas = FigureCanvasTkAgg(self.fig, master=self.graph_frame)
        self.canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)
        
        # 네비게이션 툴바 추가
        toolbar_frame = ttk.Frame(self.graph_frame)
        toolbar_frame.pack(fill=tk.X)
        toolbar = NavigationToolbar2Tk(self.canvas, toolbar_frame)
        toolbar.update()
        
        # 3x3 그리드 설정
        self.visualizer.setup_tkinter_graphs(self.fig)
        
        # 레이아웃 조정
        self.fig.tight_layout(rect=[0, 0, 1, 0.95])
        self.fig.suptitle("VR 장치 위치 데이터 모니터링", fontsize=12)
        
    def toggle_visualization(self):
        if self.running:
            self.stop_visualization()
            self.start_button.config(text="시각화 시작")
        else:
            self.start_visualization()
            self.start_button.config(text="시각화 정지")
    
    def start_visualization(self):
        self.running = True
        self.visualizer.running = True
        self.update_graphs()
    
    def stop_visualization(self):
        self.running = False
        self.visualizer.running = False
    
    def update_graphs(self):
        if not self.running:
            return
        
        # 데이터 업데이트
        self.visualizer.update_data()
        
        # 그래프 업데이트
        self.visualizer.update_tkinter_graphs()
        
        # FPS 갱신
        self.info_label.config(text=f"FPS: {self.visualizer.current_fps}")
        
        # 캔버스 다시 그리기
        self.canvas.draw_idle()
        
        # 다음 업데이트 예약
        self.root.after(self.update_delay, self.update_graphs)
    
    def on_closing(self):
        self.stop_visualization()
        self.root.destroy()
    
    def run(self):
        self.root.mainloop()


# ================================================================
# 데이터 시각화 클래스
# VR 장치의 위치 데이터를 실시간으로 시각화
# ================================================================
class DataVisualizer:
    
    # ----------------------------------------------------------------
    # 시각화 관리자 초기화
    # ----------------------------------------------------------------
    def __init__(self, data_manager):
        # 데이터 관리자 설정
        self.data_manager = data_manager
        
        # 데이터 저장소
        self.max_data_points = 100
        self.timestamps = deque(maxlen=self.max_data_points)
        
        # 장치별 그래프 객체 생성
        self.hmd = DeviceGraphs("HMD", self.max_data_points)
        self.lcon = DeviceGraphs("LController", self.max_data_points)
        self.rcon = DeviceGraphs("RController", self.max_data_points)
        
        # 파싱된 포즈 데이터 저장
        self.pose_snapshots = deque(maxlen=self.max_data_points)
        
        # 상태 관리
        self.running = False
        self.lock = threading.Lock()
        
        # 단위 시간당 갱신 수 측정
        self.fps_counter = 0
        self.last_fps_time = 0
        self.current_fps = 0
    
    # ----------------------------------------------------------------
    # Tkinter 그래프 설정
    # ----------------------------------------------------------------
    def setup_tkinter_graphs(self, fig):
        # 각 장치별 그래프 설정
        self.hmd.setup_graphs(fig, 0, 0)     # 1행
        self.lcon.setup_graphs(fig, 1, 0)    # 2행
        self.rcon.setup_graphs(fig, 2, 0)    # 3행
    
    # ----------------------------------------------------------------
    # Tkinter 앱 실행
    # ----------------------------------------------------------------
    def run_tkinter_app(self):
        app = TkinterApp(self)
        app.run()
    
    # ----------------------------------------------------------------
    # 데이터 업데이트
    # ----------------------------------------------------------------
    def update_data(self):
        # 실행 중이 아니면 업데이트 중단
        with self.lock:
            if not self.running:
                return False
        
        # 데이터 관리자에서 데이터 직접 접근
        try:
            with self.data_manager.lock:
                # 데이터가 없으면 업데이트 중단
                if not self.data_manager.data:
                    return False
                    
                # 최신 데이터만 처리
                latest_data = self.data_manager.data[-1]
                self._process_data(latest_data)
                
            # FPS 계산
            current_time = time.time()
            self.fps_counter += 1
            if current_time - self.last_fps_time >= 1.0:  # 1초마다 FPS 갱신
                self.current_fps = self.fps_counter
                self.fps_counter = 0
                self.last_fps_time = current_time
                
            return True
        except Exception as e:
            print(f"데이터 업데이트 오류: {e}")
            return False
    
    # ----------------------------------------------------------------
    # Tkinter 그래프 업데이트
    # ----------------------------------------------------------------
    def update_tkinter_graphs(self):
        # 상대 시간 계산
        if not self.timestamps:
            relative_times = []
        else:
            relative_times = [t - self.timestamps[0] for t in self.timestamps]
        
        if not relative_times:
            return
        
        # 각 장치별 그래프 업데이트
        self.hmd.update_graphs(relative_times)
        self.lcon.update_graphs(relative_times)
        self.rcon.update_graphs(relative_times)
    
    # ----------------------------------------------------------------
    # JSON 데이터를 PoseSnapshot으로 파싱하고 위치 정보 저장
    # ----------------------------------------------------------------
    def _process_data(self, json_data):
        try:
            # PoseSnapshot으로 파싱
            pose_snapshot = PoseSnapshot.from_dict(json_data)
            self.pose_snapshots.append(pose_snapshot)
            
            # 타임스탬프 저장
            self.timestamps.append(pose_snapshot.TimeStamp)
            
            # 각 장치별 위치 데이터 저장
            self.hmd.add_data(pose_snapshot.Pose.HMD.Position)
            self.lcon.add_data(pose_snapshot.Pose.LController.Position)
            self.rcon.add_data(pose_snapshot.Pose.RController.Position)
            
            return pose_snapshot
        except Exception as e:
            print(f"데이터 처리 오류: {e}")
            return None
