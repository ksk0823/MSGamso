from Data import ProcessedDataQueue
from TCPServer import TCPServer
from Visualizer import DataVisualizer


class UnityGrapherApp:
    # ----------------------------------------------------------------
    # 애플리케이션 초기화
    # ----------------------------------------------------------------
    def __init__(self, host='127.0.0.1', port=5555):
        
        (self.server, self.data_queue) = TCPServer(host, port), ProcessedDataQueue()
        
        # 서버 이벤트 핸들러 설정
        self.server.set_message_handler(self.data_queue.process)
        
        self.visualizer = DataVisualizer(self.data_queue)
    
    # ----------------------------------------------------------------
    # 애플리케이션 실행
    # ----------------------------------------------------------------
    def run(self):
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
    
    # ----------------------------------------------------------------
    # 애플리케이션 종료
    # ----------------------------------------------------------------
    def stop(self):
        # 서버 중지
        self.server.stop()
        print("프로그램이 종료되었습니다.")
