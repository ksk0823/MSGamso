from DataManager import DataManager
from TCPServer import TCPServer
from DataVisualizer import DataVisualizer


class UnityGrapherApp:
    """Unity 데이터 시각화 애플리케이션"""
    
    def __init__(self, host='127.0.0.1', port=5555):
        """애플리케이션 초기화"""
        # 구성 요소 초기화
        self.data_manager = DataManager()
        self.server = TCPServer(host, port, self.data_manager)
        self.visualizer = DataVisualizer(self.data_manager)
    
    
    def run(self):
        """애플리케이션 실행"""
        try:
            # 서버 및 시각화 시작
            print("Unity 데이터 시각화 서버 시작 중...")
            self.server.start()
            
            print("데이터 시각화 시작 중...")
            self.visualizer.start()
        
        except KeyboardInterrupt:
            print("사용자에 의해 프로그램이 중단되었습니다.")
        
        finally:
            self.stop()
    
    
    def stop(self):
        """애플리케이션 종료"""
        # 서버 중지
        self.server.stop()
        print("프로그램이 종료되었습니다.")
