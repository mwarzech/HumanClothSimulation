using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class MaleAnimationController : MonoBehaviour
{
    public string[] animations;

    public GameObject cloth;
    public TMP_Dropdown dropdown;
    public TMP_Text buttonLabel;

    public Animator anim;

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
        Destroy(cloth);
        cloth = cloth_copy;
        cloth.SetActive(true);
        cloth_copy = Instantiate(cloth);
        cloth_copy.SetActive(false);
        //cloth.SetActive(false);
        //cloth.SetActive(true);
        anim.Play(idleStateName);
        anim.SetBool("dancing", false);
        buttonLabel.text = "Play Animation";
        isPlaying = false;
    }

    private GameObject cloth_copy;

    void Start()
    {
        cloth_copy = Instantiate(cloth);
        cloth_copy.SetActive(false);
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
}
