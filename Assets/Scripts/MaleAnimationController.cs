using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class MaleAnimationController : MonoBehaviour
{
    public string[] animations;

    public ClothNormalCollisions collisionHandler;
    public ClothParticleSim[] cloths;
    public TMP_Dropdown dropdown;
    public TMP_Text buttonLabel;

    public Animator anim;

    public float animationSpeed = 0.01f;

    private const string idleStateName = "IdlePose";
    private bool isPlaying = false;

    public void AnimationButton()
    {
        if (!isPlaying)
        {
            PlayAnimation();
        }
        else
        {
            StopAnimation();
        }
    }

    private void PlayAnimation()
    {
        //anim.Play(dropdown.options[dropdown.value].text);
        anim.SetInteger("danceNum", dropdown.value);
        anim.SetBool("dancing", true);
        buttonLabel.text = "Stop Animation";
        isPlaying = true;
    }

    private void StopAnimation()
    {
        anim.SetBool("dancing", false);
        anim.Play(idleStateName);
        collisionHandler.ResetPrevMeshes();
        buttonLabel.text = "Play Animation";
        isPlaying = false;

        foreach(var cloth in cloths)
        {
            cloth.ResetMesh();
        }
    }

    private GameObject cloth_copy;

    void Start()
    {
        if (null == anim)
        {
            anim = transform.GetComponent<Animator>();
        }
        if(animations.Length > 0)
        {
            dropdown.options.AddRange(animations.Select(x => new TMP_Dropdown.OptionData(x)));
            dropdown.captionText.text = animations[0];
            dropdown.value = 0;
        }
    }

    private void Update()
    {
        anim.speed = animationSpeed / Time.deltaTime;
    }
}
