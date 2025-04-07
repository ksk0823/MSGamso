using UnityEngine;

// ------------------------------------------------------------
/// <summary>
/// 로거 인터페이스입니다.
/// </summary>
// ------------------------------------------------------------
public interface ILogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
}

// ------------------------------------------------------------
/// <summary>
/// 유니티 디버그 로거 구현체입니다.
/// </summary>
// ------------------------------------------------------------
public class UnityDebugLogger : ILogger
{
    public void Log(string message)
    {
        Debug.Log(message);
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }
}