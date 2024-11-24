using System;
using System.Collections;
using System.Collections.Generic;
using inonego;
using UnityEngine;

public partial class Puzzle : MonoBehaviour
{
    public enum State
    {
        Play, Stop
    }

    public delegate void OnStateChangedEvent(State state);
    public delegate void OnSequenceChangedEvent(Sequence sequence);
    public delegate void OnEndedEvent(bool clear);
    
    public event OnStateChangedEvent OnStateChanged;
    public event OnSequenceChangedEvent OnSequenceChanged;
    public event OnEndedEvent OnEnded;

    public List<int> SequenceLengthOnEachLevel = new(); 

    public int TotalLevel => SequenceLengthOnEachLevel.Count;
    public int CurrentLevel         { get; private set; } = 0;

    public Sequence CurrentSequence { get; private set; } = null;

    public State CurrentState       { get; private set; } = State.Stop;

    private Timer playTimer = new Timer();

    public float ElapsedTime    => playTimer.ElapsedTime;
    public float LeftTime       => playTimer.LeftTime;

    private void Update()
    {
        playTimer.Update();

        if (playTimer.WasEndedThisFrame)
        {
            OnEnded?.Invoke(clear: false);

            Stop();
        }
    }

    public void Play(float time)
    {
        SetState(State.Play);

        playTimer.Start(time);

        // 레벨 0부터 시작함
        if (0 < TotalLevel) SetLevel(0);
    }

    public void Stop()
    {
        SetState(State.Stop);

        playTimer.Stop();

        CurrentLevel = 0;
        CurrentSequence = null;
    }

    private void SetState(State state)
    {
        CurrentState = state;

        OnStateChanged?.Invoke(state);
    }

    private void SetLevel(int level)
    {
        CurrentLevel = level;
        CurrentSequence = new Sequence(SequenceLengthOnEachLevel[CurrentLevel]);

        CurrentSequence.OnCleared    += OnSequenceCleared;
        CurrentSequence.OnFailed     += OnSequenceFailed;

        OnSequenceChanged?.Invoke(CurrentSequence);
    }

    private void OnSequenceCleared()
    {
        CurrentLevel += 1;

        if (CurrentLevel == TotalLevel)
        {
            OnEnded?.Invoke(clear: true);

            Stop();
        }
        else
        {
            SetLevel(CurrentLevel);
        }
    }

    private void OnSequenceFailed()
    {
        // 실패 시에 해당 레벨 재시작
        SetLevel(CurrentLevel);
    }

    public void ProcessInput(Sequence.InputType inputType)
    {
        CurrentSequence?.ProcessInput(inputType);
    }

    public void ProcessInput(int inputType)
    {
        CurrentSequence?.ProcessInput((Sequence.InputType)inputType);
    }
}
