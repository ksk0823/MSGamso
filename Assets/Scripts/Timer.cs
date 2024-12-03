using System;

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

#region Events

    public delegate void EventHandler();
    public delegate void EventHandler<TEventArgs>(Timer sender, TEventArgs e);

    [Flags]
    private enum EventFlag
    {
        StateChanged    = 1 << 0,
        Ended           = 1 << 1,
    }

    private class Event
    {   
        public EventFlag Flag;

        public bool HasFlag(EventFlag eventFlag) => Flag.HasFlag(eventFlag);

        public void Clear() => Flag = 0;
    }

    private Event @event;

    #region EventArgs

        public struct StateChangedEventArgs
        {
            public State Previous;
            public State Current;
        }

    #endregion

    /// <summary>
    /// 상태가 변화되었을떼 호출되는 이벤트입니다.
    /// </summary>
    public event EventHandler<StateChangedEventArgs> OnStateChanged;
    /// <summary>
    /// 타이머가 종료되었을때 호출되는 이벤트입니다.
    /// </summary>
    public event EventHandler OnEnded;

#endregion

    [field: SerializeField] public State Current { get; private set; } = State.Stopped;

    public bool IsWorking => Current == State.Started;

    private float time      = 0f;
    private float current   = 0f;

    public float LeftTime       => current; 
    public float ElapsedTime    => time - current;
    public float LeftTime01     => LeftTime / time;
    public float ElapsedTime01  => ElapsedTime / time;

    private State previous  = State.Stopped;

    private void Clear()
    {
        @event.Clear();

        previous = Current;
    }
    
    /// <summary>
    /// 타이머를 업데이트합니다.
    /// </summary>
    public void Update()
    {
        if (IsWorking)
        {
            current -= Time.deltaTime;

            if (current <= 0f)
            {
                Stop();
                
                @event.Flag |= EventFlag.Ended;
            }
        }

        if (@event.HasFlag(EventFlag.StateChanged))
        {
            OnStateChanged?.Invoke(this, new StateChangedEventArgs { Previous = previous, Current = Current });
        }

        if (@event.HasFlag(EventFlag.Ended))
        {
            OnEnded?.Invoke();
        }

        Clear();
    }

    private void SetState(State state)
    {
        Current = state;

        @event.Flag |= EventFlag.StateChanged;
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