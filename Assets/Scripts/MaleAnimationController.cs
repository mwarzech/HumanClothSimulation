using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class MaleAnimationController : MonoBehaviour
{
    public string[] animations;

    public TMP_Dropdown dropdown;
    public TMP_Text buttonLabel;

    private Animator anim;

    private const string idleStateName = "Idle";
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
        anim.Play(dropdown.options[dropdown.value].text);
        buttonLabel.text = "Stop Animation";
        isPlaying = true;
    }

    private void StopAnimation()
    {
        anim.Play(idleStateName);
        buttonLabel.text = "Play Animation";
        isPlaying = false;
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        if(animations.Length > 0)
        {
            dropdown.options.AddRange(animations.Select(x => new TMP_Dropdown.OptionData(x)));
            dropdown.captionText.text = animations[0];
        }
    }
}
