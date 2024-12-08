using System.Collections;
using System.Collections.Generic;
using inonego;
using UnityCommunity.UnitySingleton;
using UnityEngine;

/// <summary>
/// 전체 게임의 상태와 진행을 관리합니다.
/// </summary>
public class GameManager : MonoSingleton<GameManager>
{
    /// <summary>
    /// 실행 가능한 미니게임 목록입니다.
    /// </summary>
    public List<MiniGame> MiniGameList = new();

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

    /// <summary>
    /// 게임의 상태가 변경될 때 알립니다.
    /// </summary>
    public delegate void OnStateChangedEvent(State state);
    /// <summary>
    /// 게임이 클리어되었을 때 알립니다.
    /// </summary>
    public delegate void OnGameClearedEvent();
    /// <summary>
    /// 게임이 실패했을 때 알립니다.
    /// </summary>
    public delegate void OnGameFailedEvent();

    public event OnStateChangedEvent OnStateChanged;
    public event OnGameClearedEvent OnGameCleared;
    public event OnGameFailedEvent OnGameFailed;

    /// <summary>
    /// 게임의 제한 시간입니다.
    /// </summary>
    public float GameTime;

    private Timer gameTimer = new Timer();

    public float ElapsedTime => gameTimer.ElapsedTime;
    public float LeftTime => gameTimer.LeftTime;

    protected override void Awake()
    {
        gameTimer.OnEnded += OnGameTimerEnded;
    }

    private void Update()
    {
        gameTimer.Update();
    }

    private void SetState(State state)
    {
        CurrentState = state;
     
        OnStateChanged?.Invoke(state);
    }

    #region Game Control

    /// <summary>
    /// 게임을 초기 상태로 되돌립니다.
    /// </summary>
    public void Reset()
    {
        SetState(State.Idle);

        gameTimer.Stop();
    }

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    public void Play()
    {
        SetState(State.Running);

        gameTimer.Start(GameTime);
    }

    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    public void Stop()
    {
        SetState(State.Done);

        gameTimer.Stop();
    }

    #endregion

    #region Event Processing
    
    private void OnGameTimerEnded()
    {
        SetGameFailed();
    }

    /// <summary>
    /// 게임을 클리어한 것으로 설정합니다.
    /// </summary>
    public void SetGameCleared()
    {
        Stop();

        OnGameCleared?.Invoke();
    }

    /// <summary>
    /// 게임을 실패한 것으로 설정합니다.
    /// </summary>
    public void SetGameFailed()
    {
        Stop();

        OnGameFailed?.Invoke();
    }

    #endregion
}
