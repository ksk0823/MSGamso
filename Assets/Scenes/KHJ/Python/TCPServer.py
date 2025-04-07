import socket
import threading
import json

# ================================================================
# TCP 서버 클래스
# Unity에서 전송되는 데이터를 수신하고 처리하는 TCP 서버 구현
# ================================================================
class TCPServer:
    
    # ----------------------------------------------------------------
    # TCP 서버 초기화
    # 서버 설정 및 데이터 관리자 연결 설정
    # ----------------------------------------------------------------
    def __init__(self, host='127.0.0.1', port=5555, data_manager=None):
        # 서버 설정
        self.host = host
        self.port = port
        self.max_buffer_size = 4096
        
        # 상태 관리
        self.running = False
        self.server_socket = None
        self.server_thread = None
        
        # 데이터 관리
        self.data_manager = data_manager

        # 클라이언트 관리
        self.clients = []
    
    # ----------------------------------------------------------------
    # 서버 시작
    # 별도 스레드에서 서버 루프를 실행하여 클라이언트 연결 수신 준비
    # ----------------------------------------------------------------
    def start(self):
        if self.running:
            print("서버가 이미 실행 중입니다.")
            return
            
        self.running = True
        
        # 서버 스레드 시작
        self.server_thread = threading.Thread(target=self._server_loop)
        self.server_thread.daemon = True
        self.server_thread.start()
        
        print(f"[시작] 서버가 {self.host}:{self.port}에서 실행 중...")
    
    # ----------------------------------------------------------------
    # 서버 중지
    # 실행 중인 서버를 중지하고 모든 클라이언트 연결 종료
    # ----------------------------------------------------------------
    def stop(self):
        self.running = False
        
        if self.server_socket: 
            self.server_socket.close()
        
        # 모든 클라이언트 연결 종료
        for client in self.clients[:]:
            try: 
                client.close()
            except: 
                pass

        self.clients.clear()
        
        print("[종료] 서버가 중지되었습니다.")
    
    # ----------------------------------------------------------------
    # 서버 메인 루프
    # 클라이언트 연결을 수신하고 각 클라이언트 처리를 위한 스레드 시작
    # ----------------------------------------------------------------
    def _server_loop(self):
        # 소켓 설정
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        try:
            # 서버 바인딩 및 리스닝
            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(5)
            
            # --- 클라이언트 연결 수락 루프 시작 ---
            while self.running:
                try:
                    client_socket, addr = self.server_socket.accept()
                    self.clients.append(client_socket)
                    
                    # 클라이언트 처리 스레드 시작
                    client_thread = threading.Thread(
                        target=self._handle_client, 
                        args=(client_socket, addr)
                    )

                    client_thread.daemon = True
                    client_thread.start()
                    
                    print(f"[연결] {addr}에서 연결됨 (활성 연결: {len(self.clients)})")
                
                except socket.timeout: 
                    continue
                except Exception as e:
                    if self.running:  # 의도적인 종료가 아닌 경우에만 오류 메시지 출력
                        print(f"[서버 오류] {e}")
                    break
            # --- 클라이언트 연결 수락 루프 종료 ---
        
        except Exception as e:
            print(f"[서버 시작 오류] {e}")
        
        finally:
            if self.server_socket:
                self.server_socket.close()
    
    # ----------------------------------------------------------------
    # 클라이언트 연결 처리
    # 개별 클라이언트 연결에서 데이터를 수신하고 메시지 처리
    # ----------------------------------------------------------------
    def _handle_client(self, client_socket, addr):
        buffer = ""
        
        try:
            # --- 데이터 수신 루프 시작 ---
            
            while self.running:
                data = client_socket.recv(self.max_buffer_size)
                if not data:
                    break
                
                # 수신 데이터를 디코딩하여 버퍼에 추가
                buffer += data.decode('utf-8')
                
                # 버퍼에서 완전한 메시지 추출 (\n으로 구분)
                while '\n' in buffer:
                    message, buffer = buffer.split('\n', 1)
                    self._process_message(message)

            # --- 데이터 수신 루프 종료 ---
        
        except Exception as e:
            print(f"[클라이언트 오류] {addr}: {e}")
        
        finally:
            # 연결 정리
            if client_socket in self.clients:
                self.clients.remove(client_socket)

            client_socket.close()
            
            print(f"[연결 종료] {addr} 연결 해제됨 (활성 연결: {len(self.clients)})")
    
    # ----------------------------------------------------------------
    # 수신된 메시지 처리
    # JSON 형식의 메시지를 파싱하고 데이터 관리자에 전달
    # ----------------------------------------------------------------
    def _process_message(self, message):
        try:
            # JSON 데이터 파싱 시도
            if message.startswith('{') and message.endswith('}'):
                data = json.loads(message)
                print(f"[JSON 데이터] {data}")
                
                # 데이터 관리자에 데이터 추가
                if self.data_manager:
                    self.data_manager.add_data(data)
            else:
                print(f"[텍스트 메시지] {message}")
        
        except json.JSONDecodeError:
            print(f"[파싱 오류] JSON 형식이 아닌 데이터: {message}")
        except Exception as e:
            print(f"[처리 오류] {e}")
