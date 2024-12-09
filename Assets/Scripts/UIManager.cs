using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI TimeText;

    private void Update()
    {
        TimeText.text = $"{(int)GameManager.Instance.GameTime / 60}:{(int)GameManager.Instance.GameTime % 60}";
    }
}
