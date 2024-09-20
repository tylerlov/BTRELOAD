using System.Collections;
using System.Collections.Generic;
using SonicBloom.Koreo;
using UnityEngine;
using UnityEngine.VFX;
using PrimeTween;

public class WarpSpeed : MonoBehaviour
{
    [EventID]
    public string eventID;
    public VisualEffect WarpVFX;
    private bool _warpActive;

    // Start is called before the first frame update
    void Start()
    {
        Koreographer.Instance.RegisterForEvents(eventID, OnMusicalAction);
        WarpVFX.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _warpActive = true;
            StartCoroutine(ActivateParticles());
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _warpActive = false;
        }
    }

    IEnumerator ActivateParticles()
    {
        if (_warpActive)
        {
            WarpVFX.Play();
            Tween.Custom(0f, 1f, 0.1f, v => WarpVFX.SetFloat("WarpAmount", v), Ease.InOutQuad);
            yield return new WaitForSeconds(0.2f);
            WarpVFX.SetFloat("WarpAmount", 0);
        }
        else
        {
            WarpVFX.Stop();
        }
    }

    void OnMusicalAction(KoreographyEvent evt)
    {
        if (Time.timeScale != 0f)
        {
            _warpActive = true;
            StartCoroutine(ActivateParticles());
        }
        else
        {
            _warpActive = false;
        }
    }
}
