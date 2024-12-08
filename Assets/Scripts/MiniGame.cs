using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 모든 미니게임의 기본 동작을 정의합니다.
/// </summary>
public abstract class MiniGame : MonoBehaviour
{
    /// <summary>
    /// 게임의 상태가 변경될 때 알립니다.
    /// </summary>
    public delegate void OnStateChangedEvent(State state);
    /// <summary>
    /// 게임이 클리어되었을 때 알립니다.
    /// </summary>
    public delegate void OnClearedEvent();

    public event OnStateChangedEvent OnStateChanged;
    public event OnClearedEvent OnCleared;

    [SerializeField]
    private UnityEvent onCleared;

    /// <summary>
    /// 게임의 진행 상태를 나타냅니다.
    /// </summary>
    public enum State
    {
        Idle,    // 대기 중
        Running, // 실행 중
        Done     // 완료됨
    }

    public State CurrentState { get; private set; } = State.Idle;

    private void SetState(State state)
    {
        CurrentState = state;
         
        OnStateChanged?.Invoke(state);
    }

    /// <summary>
    /// 게임을 초기 상태로 되돌립니다.
    /// </summary>
    public virtual void Reset()
    {
        SetState(State.Idle);
    }

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    public virtual void Play()
    {
        SetState(State.Running);
    }

    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    public virtual void Stop()
    {
        SetState(State.Done);
    }

    /// <summary>
    /// 게임이 클리어되었음을 설정합니다.
    /// </summary>
    protected void SetCleared()
    {
        Stop();

        OnCleared?.Invoke();
        onCleared?.Invoke();
    }
}
