using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    public Color PressedColor;
    public Color ReleasedColor;

    public float LerpSpeed = 0.5f;

    public AudioClip PressSound;
    public AudioClip ReleaseSound;
    public bool IsPressed { get; private set; } = false;

    private MeshRenderer meshRenderer;

    private AudioSource audioSource;
    private Animator animator;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        audioSource = GetComponent<AudioSource>();

        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        meshRenderer.material.color = Color.Lerp(meshRenderer.material.color, IsPressed ? PressedColor : ReleasedColor, LerpSpeed * Time.deltaTime);
    }

    public void SetPressed(bool isPressed)
    {
        IsPressed = isPressed;

        animator.SetBool("IsPressed", isPressed);
        
        if(isPressed)
        {
            if (PressSound != null) audioSource.PlayOneShot(PressSound);
        }
        else
        {
            if (ReleaseSound != null) audioSource.PlayOneShot(ReleaseSound);
        }
    }
}
