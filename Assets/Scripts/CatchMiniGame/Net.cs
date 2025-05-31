using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Net : MonoBehaviour
{
    private AudioSource audio;
    public AudioClip catchSound;
    public GameObject particle;

    private void Awake()
    {
        audio = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        // 나비 태그 확인하고 잡기
        if (other.CompareTag("Butterfly"))
        {
            Butterfly butterfly = other.GetComponent<Butterfly>();
            if (butterfly != null)
            {
                audio.PlayOneShot(catchSound);
                GameObject particles = Instantiate(particle, other.transform.position, Quaternion.identity);
                butterfly.Caught();
            }
        }
    }
}
