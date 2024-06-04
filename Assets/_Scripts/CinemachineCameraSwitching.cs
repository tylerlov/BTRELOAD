using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CinemachineCameraSwitching : MonoBehaviour
{

    [SerializeField] private Volume v1;
    private Animator animator;
    private bool mainCamera;
    public GameObject reticle;
    [SerializeField] private GameObject playerBody;

    [SerializeField] private UnityEvent playerFacingForward;
    //private MotionBlur mb;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        mainCamera = true;
        // v1.profile.TryGet<MotionBlur>(out mb);
    }

    public void SetMainCamera()
    {
        if (mainCamera == false) 
        {
            animator.Play("Main Camera");
            playerBody.SetActive(true);
            reticle.SetActive(true);
            playerBody.transform.localPosition = new Vector3(0, 0, 0);
            mainCamera = true;
            //mb.active = true;
            playerFacingForward.Invoke();
        }
        else
        {
            ConditionalDebug.Log("Set Main Camera when not needed");
        }
    }
    public void SwitchToTransitionCamera()
    {
        //currently using main camera
        if (mainCamera)
        {
            animator.Play("Transition Camera");
            reticle.SetActive(false);
            playerBody.SetActive(false);
            //mb.active = false;

        }
        else
        {
            animator.Play("Main Camera");
            playerFacingForward.Invoke();
            reticle.SetActive(true);
            playerBody.SetActive(true);
            //mb.active = true;
        }
        mainCamera = !mainCamera;
    }

    IEnumerator delayReleasePlayerConstraints()
    {
        yield return new WaitForSeconds(1f);
    }
}
