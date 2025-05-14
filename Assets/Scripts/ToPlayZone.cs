using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToPlayZone : MonoBehaviour
{
    // Start is called before the first frame update
    public string sceneName = "MazeScene";

    public void GoToPlayZone()
    {
        StartCoroutine(DelayedSceneLoad());
    }

    IEnumerator DelayedSceneLoad()
    {
        yield return new WaitForSeconds(3f); // 3초 대기
        SceneManager.LoadScene(sceneName);
    }
}
