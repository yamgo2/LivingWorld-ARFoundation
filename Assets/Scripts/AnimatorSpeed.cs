using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorSpeed : MonoBehaviour
{
    public float speed;

    void Start()
    {
        // Control speed of Animator
        gameObject.GetComponent<Animator>().speed = speed;
        gameObject.GetComponent<Animator>().keepAnimatorControllerStateOnDisable = true;
    }
}