using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;

public class PuzzleUI : MonoBehaviour
{
    [Header("General")]
    public Puzzle Puzzle;

    [Header("Image")]
    public SerializedDictionary<Puzzle.Sequence.InputType, Sprite> Images = new();

    [Header("UI")]
    public TextMeshProUGUI TimeUI;
    public TextMeshProUGUI LevelUI;
    public RectTransform ImageUIGroup;

    private Image[] imageUIs;

    private void Awake()
    {
        imageUIs = ImageUIGroup.GetComponentsInChildren<Image>();

        Puzzle.OnSequenceChanged += OnPuzzleSequenceChanged;
        Puzzle.OnEnded           += OnPuzzleEnded;

        Puzzle.Play(30);

    }

    private void Update()
    {
        TimeUI.text     = $"{ Puzzle.LeftTime.ToString(".00") }";
        LevelUI.text    = $"{ Puzzle.CurrentLevel + 1 }";
    }

    private void OnPuzzleSequenceChanged(Puzzle.Sequence sequence)
    {
        void OnSequencePassed(Puzzle.Sequence sequence, int index)
        {
            for (int i = 0; i < imageUIs.Length; i++)
            {
                imageUIs[i].color   = i < sequence.Count && i >= sequence.Index ? Color.white : Color.clear;
            }
        }

        sequence.OnPassed += OnSequencePassed;

        for (int i = 0; i < imageUIs.Length; i++)
        {
            imageUIs[i].gameObject.SetActive(i < sequence.Count);
            imageUIs[i].sprite  = i < sequence.Count ? Images[sequence.Generated[i]] : null;
            imageUIs[i].color   = i < sequence.Count ? Color.white : Color.clear;
        }
    }

    private void OnPuzzleEnded(bool clear)
    {
        if (clear)
        {
            Debug.Log("Clear!");
        }
        else
        {

            Debug.Log("Fail...");
        }
    }
}