using UnityEngine;

namespace inonego
{

public class Timer
{

#region Enumerations

    public enum State
    {
        Started, Paused, Stopped
    }

#endregion

#region EventArgs

    public struct StateChangedEventArgs
    {
        public State Previous;
        public State Current;
    }

#endregion

#region Events

    public delegate void OnEndedEvent();
    public delegate void OnStateChangedEvent(StateChangedEventArgs e);

    /// <summary>
    /// 타이머가 종료되었을때 호출되는 이벤트입니다.
    /// </summary>
    public event OnEndedEvent OnEnded;
    /// <summary>
    /// 상태가 변화되었을떼 호출되는 이벤트입니다.
    /// </summary>
    public event OnStateChangedEvent OnStateChanged;

#endregion

    [field: SerializeField] public State Current { get; private set; } = State.Stopped;

    public bool IsWorking => Current == State.Started;

    private float time      = 0f;
    private float current   = 0f;

    public bool WasEndedThisFrame { get; private set; } = false;

    public float LeftTime       => current; 
    public float ElapsedTime    => time - current;
    public float LeftTime01     => LeftTime / time;
    public float ElapsedTime01  => ElapsedTime / time;

    private void SetState(State state)
    {
        State previous = Current, current = state;

        Current = current;

        WasEndedThisFrame = false;

        OnStateChanged?.Invoke(new StateChangedEventArgs { Previous = previous, Current = current });
    }
    
    /// <summary>
    /// 타이머를 업데이트합니다.
    /// </summary>
    public void Update()
    {
        WasEndedThisFrame = false;

        if (IsWorking)
        {
            current -= Time.deltaTime;

            if (current <= 0f)
            {
                Stop();
                
                WasEndedThisFrame = true;
                
                OnEnded?.Invoke();
            }
        }
    }

    /// <summary>
    /// 타이머의 작동을 시작시킵니다.
    /// </summary>
    /// <param name="time">시간 값</param>
    public void Start(float time)
    {
        SetState(State.Started);

        this.time = current = time;
    }

    /// <summary>
    /// 타이머의 작동을 중지시킵니다.
    /// </summary>
    public void Stop()
    {
        if (Current != State.Stopped)
        {
            SetState(State.Stopped);

            this.time = current = 0f;
        }
    }

    /// <summary>
    /// 타이머의 작동을 일시정지시킵니다.
    /// </summary>
    public void Pause()
    {
        if (Current == State.Started)
        {
            SetState(State.Paused);
        }
    }

    /// <summary>
    /// 타이머의 작동을 재개시킵니다.
    /// </summary>
    public void Resume()
    {
        if (Current == State.Paused)
        {
            SetState(State.Started);
        }
    }
}

}