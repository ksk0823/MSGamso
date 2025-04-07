using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;

// ------------------------------------------------------------
/// <summary>
/// 연결 정보를 저장하는 구조체입니다.
/// </summary>
// ------------------------------------------------------------
[Serializable]
public struct Address
{
    public string IP;
    public int Port;

    public Address(string ip, int port)
    {
        (IP, Port) = (ip, port);
    }
}

// ------------------------------------------------------------
/// <summary>
/// 세션 정보를 관리하는 클래스입니다.
/// </summary>
// ------------------------------------------------------------
public class Session
{
    public bool IsRunning           { get; private set; } = false;

    public TcpClient Client         { get; private set; }
    public NetworkStream Stream     { get; private set; }
    
    private Thread sendThread;      // 데이터 전송 스레드

    public Queue<string> SendQueue  { get; private set; } = new Queue<string>();
    
    public bool IsConnected => Client != null && Client.Connected;
    
    public int IntervalMS = 5;

    // 로거
    private ILogger logger;
    
    public Session(ILogger logger = null)
    {
        this.logger = logger ?? new UnityDebugLogger();
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 서버에 연결합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void Connect(Address address)
    {
        try
        {
            // 이미 연결되어 있으면 먼저 연결 해제
            if (IsConnected)
            {
                Disconnect();
            }
            
            Client = new TcpClient();

            Client.Connect(address.IP, address.Port);
            
            if (IsConnected)
            {
                logger.Log($"서버에 연결되었습니다. ({address.IP}:{address.Port})");

                Stream = Client.GetStream();

                // ------------------------------------------------------------
                // 실행 중
                // ------------------------------------------------------------
                IsRunning = true;
                
                // 데이터 전송 스레드 시작
                sendThread = new Thread(SendQueueThread)
                {
                    IsBackground = true
                };
                
                sendThread.Start();
                
                logger.Log("데이터 전송 스레드가 시작되었습니다.");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"서버 연결 오류: {e.Message}");

            Reset();
        }
    }
    
    // ------------------------------------------------------------
    /// <summary>
    /// 서버와의 연결을 종료합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void Disconnect()
    {
        IsRunning = false;
        
        // 스레드가 종료될 때까지 대기
        if (sendThread != null && sendThread.IsAlive)
        {
            try
            {
                sendThread.Join(500); // 최대 0.5초 대기
            }
            catch (Exception e)
            {
                logger.LogError($"스레드 종료 오류: {e.Message}");
            }
            
            sendThread = null;
        }
        
        if (Stream != null)
        {
            Stream.Close();

            Stream = null;
        }
        
        if (Client != null)
        {
            Client.Close();

            Client = null;
        }
        
        logger.Log("서버와 연결이 종료되었습니다.");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 객체를 JSON으로 직렬화하여 전송 큐에 추가합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void Send<T>(T target)
    {
        if (IsConnected)
        {
            try
            {
                // 객체를 JSON으로 직렬화
                string jsonData = JsonUtility.ToJson(target);
                
                lock (SendQueue)
                {
                    SendQueue.Enqueue(jsonData);
                }
                
                logger.Log($"객체를 전송했습니다: {typeof(T).Name}");
            }
            catch (Exception e)
            {
                logger.LogError($"객체 직렬화 오류: {e.Message}");
            }
        }
        else
        {
            logger.LogWarning("서버에 연결되어 있지 않습니다.");
        }
    }
    
    // ------------------------------------------------------------
    /// <summary>
    /// 세션 상태를 초기화합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void Reset()
    {
        (Client, Stream) = (null, null);

        sendThread = null;

        SendQueue.Clear();

        IsRunning = false;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 데이터 전송 스레드 함수입니다.
    /// </summary>
    // ------------------------------------------------------------
    private void SendQueueThread()
    {
        while (IsRunning && IsConnected)
        {
            try
            {
                string dataToSend = null;
                
                // 큐에서 데이터 가져오기
                lock (SendQueue)
                {
                    if (SendQueue.Count > 0)
                    {
                        dataToSend = SendQueue.Dequeue();
                    }
                }
                
                // 데이터가 있으면 즉시 전송
                if (!string.IsNullOrEmpty(dataToSend))
                {
                    // 메시지 끝에 구분자 추가
                    dataToSend += "\n";
                    
                    byte[] data = Encoding.UTF8.GetBytes(dataToSend);

                    Stream.Write(data, 0, data.Length);
                    
                    // 데이터가 있으면 대기 없이 다음 루프로 진행
                    continue;
                }
                
                // 데이터가 없는 경우에만 짧게 대기
                Thread.Sleep(IntervalMS);
            }
            catch (Exception e)
            {
                if (IsRunning) 
                {
                    logger.LogError($"데이터 전송 오류: {e.Message}");
                    
                    // 오류 발생 시 연결 종료 (별도 스레드에서 처리)
                    new Thread(() => Disconnect()).Start();
                    
                    break;
                }
            }
        }
        
        logger.Log("데이터 전송 스레드가 종료되었습니다.");
    }
}
