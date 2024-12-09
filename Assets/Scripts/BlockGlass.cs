using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockGlass : MonoBehaviour
{
    private AudioSource audioSource;

    private MeshRenderer meshRenderer;
    private new Collider collider;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponent<MeshRenderer>();
        collider = GetComponent<Collider>();
    }

    public void Open()
    {
        audioSource.Play();
        meshRenderer.enabled = false;
        collider.enabled = false;
    }
}
