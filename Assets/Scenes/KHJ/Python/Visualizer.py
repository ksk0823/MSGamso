import platform
import threading
import numpy as np
import tkinter as tk
from tkinter import ttk
from collections import deque
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk
from matplotlib.animation import FuncAnimation

SYSTEM_FONTS = {
    'Windows': 'Malgun Gothic',
    'Darwin': 'AppleGothic',
    'Linux': 'NanumGothic'
}

system = platform.system()

plt.rcParams['font.family'] = SYSTEM_FONTS.get(system, 'NanumGothic')
plt.rcParams['axes.unicode_minus'] = False

plt.style.use('ggplot')

# ================================================================
# 2D 그래프를 관리하는 클래스
# 단일 채널(X, Y, Z)의 시간에 따른 변화를 시각화
# ================================================================
class AxisGraph2D:
    def __init__(self, ax: plt.Axes, name: str, color: str, title: str, graph_type='position', max_data_points=100):
        self.ax = ax          # matplotlib 축 객체
        self.name = name      # 그래프 이름 (예: "HMD X")
        self.color = color    # 그래프 색상
        self.title = title    # 그래프 제목

        self.graph_type = graph_type
        self.max_data_points = max_data_points
        
        # 데이터 저장
        self.times : deque[float] = deque(maxlen=max_data_points)
        self.values : deque[float] = deque(maxlen=max_data_points)
        
        # 축 기본 설정
        self.ax.set_xlabel('시간 (초)')
        
        # 그래프 유형에 따른 레이블 설정
        if graph_type == 'velocity':
            self.ax.set_ylabel('속도 (m/s)')
            self.ax.set_title(f"{title} 속도", fontsize=10)
        else:
            self.ax.set_ylabel('위치 (m)')
            self.ax.set_title(f"{title} 위치", fontsize=10)
            
        self.ax.grid(True, alpha=0.3)
        self.line, = self.ax.plot([], [], color=self.color, label=self.name, linewidth=1.5)
        self.ax.legend(loc='upper right', fontsize=8)
        
        # 최신 값 표시를 위한 텍스트 객체
        self.value_text = self.ax.text(0.05, 0.95, "", 
                    transform=self.ax.transAxes, fontsize=8,
                    verticalalignment='top', 
                    bbox={'facecolor': 'white', 'alpha': 0.7, 'pad': 3})
    
    # ----------------------------------------------------------------
    # 데이터 및 시간 추가
    # ----------------------------------------------------------------
    def add_data(self, time, value):
        self.times.append(time)
        self.values.append(value)
    
    # ----------------------------------------------------------------
    # 그래프 업데이트 - FuncAnimation 대응을 위해 아티스트 반환
    # ----------------------------------------------------------------
    def update(self):
        if not self.values or not self.times or len(self.values) < 2: 
            return [self.line, self.value_text]
        
        # 배열로 변환
        times_array = np.array(self.times)
        values_array = np.array(self.values)
            
        # 기존 라인 데이터 업데이트
        self.line.set_data(times_array, values_array)
            
        # 축 범위 조정
        self._adjust_axis_limits(times_array, values_array)
            
        # 최신 값 표시
        self._display_latest_value()
        
        return [self.line, self.value_text]
    
    # ----------------------------------------------------------------
    # 축 범위 조정
    # ----------------------------------------------------------------
    def _adjust_axis_limits(self, times, values):
        if not values.size: 
            return
        
        # y 범위 동적 조정
        y_min, y_max = values.min(), values.max()
        
        margin = max(0.1, (y_max - y_min) * 0.1)  # 최소 마진 설정
        
        if margin == 0:  # 값이 모두 같을 경우
            margin = 0.5
            
        self.ax.set_ylim(y_min - margin, y_max + margin)
        
        # x 범위 설정
        if times.size:
            self.ax.set_xlim(times.min(), times.max())
    
    # ----------------------------------------------------------------
    # 최신 값 표시
    # ----------------------------------------------------------------
    def _display_latest_value(self):
        if not self.values: return
        
        # 최신 값 표시
        latest_value = self.values[-1]
        unit = "m/s" if self.graph_type == 'velocity' else "m"
        
        self.value_text.set_text(f"{latest_value:.2f} {unit}")


# ================================================================
# 단일 장치(HMD, 컨트롤러 등)의 위치 데이터를 관리하고 시각화하는 클래스
# ================================================================
class VectorGraph2D:
    def __init__(self, name, graph_type='velocity', max_data_points=100):
        self.name = name

        self.graph_type = graph_type
        self.max_data_points = max_data_points
        
        # 그래프 객체 (나중에 설정)
        self.x_graph = None
        self.y_graph = None
        self.z_graph = None
    
    def setup_graphs(self, fig, row_index, col_index):
        display_name = self.name
        
        # 2D 그래프 생성
        x_ax = fig.add_subplot(3, 3, row_index * 3 + col_index + 1)
        y_ax = fig.add_subplot(3, 3, row_index * 3 + col_index + 2)
        z_ax = fig.add_subplot(3, 3, row_index * 3 + col_index + 3)
        
        # 그래프 객체 생성
        self.x_graph = AxisGraph2D(x_ax, f"{display_name} X", 'red', f"{display_name} X", self.graph_type, self.max_data_points)
        self.y_graph = AxisGraph2D(y_ax, f"{display_name} Y", 'green', f"{display_name} Y", self.graph_type, self.max_data_points)
        self.z_graph = AxisGraph2D(z_ax, f"{display_name} Z", 'blue', f"{display_name} Z", self.graph_type, self.max_data_points)
    
    # ----------------------------------------------------------------
    # 데이터 추가
    # ----------------------------------------------------------------
    def add_data(self, time, value):
        # 각 축 그래프에 해당 축 데이터 추가
        self.x_graph.add_data(time, value.x)
        self.y_graph.add_data(time, value.y)
        self.z_graph.add_data(time, value.z)
    
    # ----------------------------------------------------------------
    # 그래프 업데이트 - FuncAnimation 대응을 위해 아티스트 반환
    # ----------------------------------------------------------------
    def update_graphs(self):
        artists = []
        
        # 각 축 그래프 업데이트 후 반환된 아티스트를 수집
        artists.extend(self.x_graph.update())
        artists.extend(self.y_graph.update())
        artists.extend(self.z_graph.update())
        
        return artists


# ================================================================
# Tkinter 기반 UI 클래스
# VR 위치 데이터 시각화를 위한 사용자 인터페이스
# ================================================================
class TkinterApp:
    def __init__(self, visualizer):
        self.visualizer = visualizer
        
        # Tkinter 창 생성
        self.root = tk.Tk()
        self.root.geometry("1200x800")
        self.root.title("VR 장치 데이터 시각화")
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
        
        # 그래프 업데이트 설정 - UI 설정 전에 초기화 필요
        self.update_interval = 100  # 100ms 간격으로 업데이트 (1초에 10번)
        self.running = False
        self.animation = None
        
        # UI 구성
        self.setup_ui()
    
    # ----------------------------------------------------------------
    # UI 구성
    # ----------------------------------------------------------------
    def setup_ui(self):
        # 상단 메뉴 프레임
        menu_frame = ttk.Frame(self.root, padding=10)
        menu_frame.pack(fill=tk.X)
        
        # 시작/정지 버튼
        self.start_button = ttk.Button(menu_frame, text="시각화 시작", command=self.toggle_visualization)
        self.start_button.pack(side=tk.LEFT, padx=5)
        
        # 업데이트 주기 설정
        ttk.Label(menu_frame, text="업데이트 주기(ms):").pack(side=tk.LEFT, padx=(20, 5))
        self.update_interval_var = tk.StringVar(value=str(self.update_interval))
        update_entry = ttk.Entry(menu_frame, textvariable=self.update_interval_var, width=5)
        update_entry.pack(side=tk.LEFT, padx=5)
        ttk.Button(menu_frame, text="적용", command=self.apply_settings).pack(side=tk.LEFT, padx=5)
        
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
        
        # 그래프 유형에 따른 제목 설정
        graph_type = self.visualizer.hmd.graph_type if hasattr(self.visualizer.hmd, 'graph_type') else 'position'
        title = "VR 장치 속도 데이터 모니터링" if graph_type == 'velocity' else "VR 장치 위치 데이터 모니터링"
        self.fig.suptitle(title, fontsize=12)
    
    # ----------------------------------------------------------------
    # 설정 적용
    # ----------------------------------------------------------------
    def apply_settings(self):
        try:
            # 업데이트 주기 설정 적용
            interval = int(self.update_interval_var.get())

            if interval >= 10: 
                self.update_interval = interval
                
                # 애니메이션이 실행 중이면 간격 업데이트
                if self.animation is not None and hasattr(self.animation, 'event_source'):
                    self.animation.event_source.interval = self.update_interval
                
            print(f"설정 적용됨: 업데이트 주기 = {self.update_interval}ms")
        except ValueError:
            print("잘못된 설정 값입니다.")
    
    # ----------------------------------------------------------------
    # 시각화 시작/정지 토글
    # ----------------------------------------------------------------
    def toggle_visualization(self):
        if self.running:
            self.stop_visualization()
            self.start_button.config(text="시각화 시작")
        else:
            self.start_visualization()
            self.start_button.config(text="시각화 정지")
    
    # ----------------------------------------------------------------
    # 시각화 시작
    # ----------------------------------------------------------------
    def start_visualization(self):
        self.running = True
        self.visualizer.running = True
        
        # 이전 애니메이션 제거
        if self.animation is not None:
            self.animation.event_source.stop()
            self.animation = None
            
        # 애니메이션 업데이트 함수
        def update_animation(frame):
            if not self.running:
                return []
                
            try:
                # 데이터 업데이트
                self.visualizer.update()
                
                # 그래프 업데이트 및 아티스트 반환
                artists = self.visualizer.update_tkinter_graphs()
                if not artists:
                    artists = []
                    
                return artists
            except Exception as e:
                print(f"애니메이션 업데이트 오류: {e}")
                return []
        
        # 애니메이션 생성
        self.animation = FuncAnimation(
            self.fig, 
            update_animation, 
            interval=self.update_interval,
            blit=True,
            cache_frame_data=False
        )
        
        # 캔버스에 애니메이션 연결
        self.canvas.draw()
    
    # ----------------------------------------------------------------
    # 시각화 정지
    # ----------------------------------------------------------------
    def stop_visualization(self):
        self.running = False
        self.visualizer.running = False
        
        # 애니메이션 정지
        if self.animation is not None:
            self.animation.event_source.stop()
    
    # ----------------------------------------------------------------
    # 창 닫기
    # ----------------------------------------------------------------
    def on_closing(self):
        # 시각화 정지
        self.stop_visualization()

        # 창 닫기
        self.root.destroy()
    
    # ----------------------------------------------------------------
    # 앱 실행
    # ----------------------------------------------------------------
    def run(self):
        self.root.mainloop()


# ================================================================
# 데이터 시각화 클래스
# VR 장치의 속도 데이터를 실시간으로 시각화
# ================================================================
class DataVisualizer:
    
    # ----------------------------------------------------------------
    # 시각화 관리자 초기화
    # ----------------------------------------------------------------
    def __init__(self, data_queue):
        # 데이터 관리자 설정
        self.data_queue = data_queue
        
        # 데이터 저장소
        self.max_data_points = 100
        
        # 장치별 그래프 객체 생성 (속도 데이터 시각화용)
        self.hmd = VectorGraph2D("HMD", 'velocity', self.max_data_points)
        self.lcon = VectorGraph2D("LController", 'velocity', self.max_data_points)
        self.rcon = VectorGraph2D("RController", 'velocity', self.max_data_points)
        
        # 상태 관리
        self.running = False

        self.lock = threading.Lock()
    
    # ----------------------------------------------------------------
    # Tkinter 앱 실행
    # ----------------------------------------------------------------
    def start(self):
        self.app = TkinterApp(self)
        self.app.run()

    # ----------------------------------------------------------------
    # 데이터 업데이트
    # ----------------------------------------------------------------
    def update(self):
        # 실행 중이 아니면 업데이트 중단
        with self.lock:
            if not self.running: 
                return False
            
            updated = False
            
            try:
                # 큐에서 모든 새 데이터 가져와서 처리
                while not self.data_queue.empty():
                    try:
                        data = self.data_queue.dequeue()
                        
                        if data:
                            timestamp = data.TimeStamp
                            
                            self.hmd.add_data(timestamp, data.HMDVelocity)
                            self.lcon.add_data(timestamp, data.LVelocity)
                            self.rcon.add_data(timestamp, data.RVelocity)
                            
                            updated = True
                    except Exception as e:
                        print(f"데이터 항목 처리 오류: {e}")
                        continue
                
                return updated
            
            except Exception as e:
                print(f"데이터 업데이트 오류: {e}")
                return False
    
    # ----------------------------------------------------------------
    # Tkinter 그래프 설정
    # ----------------------------------------------------------------
    def setup_tkinter_graphs(self, fig):
        # 각 장치별 그래프 설정
        self.hmd.setup_graphs(fig, 0, 0)     # 1행
        self.lcon.setup_graphs(fig, 1, 0)    # 2행
        self.rcon.setup_graphs(fig, 2, 0)    # 3행
    
    # ----------------------------------------------------------------
    # Tkinter 그래프 업데이트 - FuncAnimation 대응
    # ----------------------------------------------------------------
    def update_tkinter_graphs(self):
        with self.lock:
            try:
                # 각 장치별 그래프 업데이트하고 반환된 아티스트 수집
                artists = []
                
                # 각 장치별 그래프 업데이트
                artists.extend(self.hmd.update_graphs())
                artists.extend(self.lcon.update_graphs())
                artists.extend(self.rcon.update_graphs())
                
                return artists
            except Exception as e:
                print(f"그래프 업데이트 오류: {e}")
                return []