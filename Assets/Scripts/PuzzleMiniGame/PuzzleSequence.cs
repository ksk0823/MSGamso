using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public partial class PuzzleMiniGame : MiniGame
{
    [Serializable]
    public class Sequence
    {
        public enum InputType
        {
            LHand, RHand, LFoot, RFoot
        }

        public delegate void OnPassedEvent(Sequence sequence, int index);
        public delegate void OnClearedEvent();
        public delegate void OnFailedEvent();

        public event OnPassedEvent OnPassed;
        public event OnClearedEvent OnCleared;
        public event OnFailedEvent OnFailed;

        [SerializeField, HideInInspector]
        private List<InputType> generated = new List<InputType>();
        public IReadOnlyList<InputType> Generated => generated;

        public int Index { get; private set; } = 0;
        public int Count => generated.Count;
        public InputType? CurrentType => Index < generated.Count ? generated[Index] : null;

        public Sequence(int count)
        {
            for (int i = 0; i < count; i++)
            {
                generated.Add((InputType)Random.Range(0, Enum.GetValues(typeof(InputType)).Length));
            }
        }

        internal void ProcessInput(InputType inputType)
        {
            if (CurrentType is not null)
            {   
                bool HasPassed() => CurrentType == inputType;

                // 성공했을 경우
                if (HasPassed())
                {
                    int index = Index;

                    Index += 1;

                    OnPassed?.Invoke(sequence: this, index: index);
                }
                else
                {
                    OnFailed?.Invoke();
                }
            }

            if (Index == Count)
            {
                OnCleared?.Invoke();
            }
        }
    }

}
