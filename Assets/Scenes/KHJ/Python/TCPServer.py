import json
import socket
import threading
from typing import Callable, Optional

# ================================================================
# TCP 서버 클래스
# Unity에서 전송되는 데이터를 수신하고 이벤트로 전달
# ================================================================
class TCPServer:
    
    # ----------------------------------------------------------------
    # TCP 서버 초기화
    # ----------------------------------------------------------------
    def __init__(self, host='127.0.0.1', port=5555):
        # 서버 설정
        self.host = host
        self.port = port
        self.max_buffer_size = 4096
        
        # 상태 관리
        self.running = False

        self.server_socket = None
        self.server_thread = None
        
        # 클라이언트 관리
        self.clients = []
        
        # 이벤트 핸들러
        self.on_message: Optional[Callable[[dict], None]] = None
        self.on_connect: Optional[Callable[[tuple], None]] = None
        self.on_disconnect: Optional[Callable[[tuple], None]] = None
    
    # ----------------------------------------------------------------
    # 이벤트 핸들러 설정
    # ----------------------------------------------------------------
    def set_message_handler(self, handler: Callable[[dict], None]):
        self.on_message = handler
    
    def set_connect_handler(self, handler: Callable[[tuple], None]):
        self.on_connect = handler
    
    def set_disconnect_handler(self, handler: Callable[[tuple], None]):
        self.on_disconnect = handler
    
    # ----------------------------------------------------------------
    # 서버 시작
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
    # ----------------------------------------------------------------
    def stop(self):
        self.running = False
        
        if self.server_socket:
            self.server_socket.close()
        
        # 모든 클라이언트 연결 종료
        for client in self.clients:
            try: 
                client.close()
            except:
                pass
        
        self.clients.clear()
        
        print("[종료] 서버가 중지되었습니다.")
    
    # ----------------------------------------------------------------
    # 서버 메인 루프
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
                    
                    # 연결 이벤트 발생
                    if self.on_connect:
                        self.on_connect(addr)
                    
                    # 클라이언트 처리 스레드 시작
                    client_thread = threading.Thread(
                        target = self._handle_client,
                        args = (client_socket, addr)
                    )

                    client_thread.daemon = True
                    client_thread.start()
                    
                    print(f"[연결] {addr}에서 연결됨 (활성 연결: {len(self.clients)})")
                
                except socket.timeout:
                    continue
                except Exception as e:
                    if self.running:
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
    # ----------------------------------------------------------------
    def _handle_client(self, client_socket, addr):
        buffer = ""
        
        try:
            # --- 데이터 수신 루프 시작 ---
            
            while self.running:
                data = client_socket.recv(self.max_buffer_size)
                
                if not data: break
                
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
            
            # 연결 해제 이벤트 발생
            if self.on_disconnect:
                self.on_disconnect(addr)
            
            print(f"[연결 종료] {addr} 연결 해제됨 (활성 연결: {len(self.clients)})")
    
    # ----------------------------------------------------------------
    # 수신된 메시지 처리
    # ----------------------------------------------------------------
    def _process_message(self, message):
        try:
            data = json.loads(message)
            
            # 메시지 이벤트 발생
            if self.on_message:
                self.on_message(data)
        
        except json.JSONDecodeError:
            print(f"[파싱 오류] JSON 형식이 아닌 데이터: {message}")
        except Exception as e:
            print(f"[처리 오류] {e}")
