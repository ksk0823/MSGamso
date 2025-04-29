using System;

using UnityEngine;

// ------------------------------------------------------------
/// <summary>
/// Unity와 Python 간 TCP 통신을 담당하는 클래스입니다.
/// </summary>
// ------------------------------------------------------------
public class PythonCommunicator : MonoBehaviour
{
    [SerializeField] private Address serverAddress = new Address("127.0.0.1", 5555);

    private Session session;
    private ILogger logger;

    void Awake()
    {
        // 로거 생성
        logger = new UnityDebugLogger();
    }

    void Start()
    {
        // 세션 생성 및 로거 주입
        session = new Session(logger);

        ConnectToServer();
    }

    void OnDestroy()
    {
        DisconnectFromServer();
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 파이썬 서버에 연결합니다.
    /// </summary>
    // ------------------------------------------------------------
    private void ConnectToServer()
    {
        session.Connect(serverAddress);
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 파이썬 서버와의 연결을 종료합니다.
    /// </summary>
    // ------------------------------------------------------------
    private void DisconnectFromServer()
    {
        session.Disconnect();
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 객체를 JSON으로 직렬화하여 전송 큐에 추가합니다.
    /// </summary>
    // ------------------------------------------------------------
    public void Send<T>(T target)
    {
        session.Send(target);
    }
} 