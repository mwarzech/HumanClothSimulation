using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaleAnimationController : MonoBehaviour
{

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            if (anim.GetBool("dancing"))
            {
                anim.SetBool("dancing", false);
            }
            else
            {
                anim.SetBool("dancing", true);
            }
        }
    }
}
