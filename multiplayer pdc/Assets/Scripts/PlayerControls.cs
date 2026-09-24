using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class PlayerControls : NetworkBehaviour
{
    public Camera playerCamera;
    public GameObject localShape;

    [Networked]
    public int playerIndex { get; set; }

    private NetworkMecanimAnimator netMecAnim;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = true;

                var audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                    audioListener.enabled = true;
            }
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;

                var audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                    audioListener.enabled = false;
            }
        }

        if (localShape == null)
        {
            if (Runner.LocalPlayer.RawEncoded - 1 == 1)
                localShape = GameObject.Find("Cube");
            else if (Runner.LocalPlayer.RawEncoded - 1 == 2)
                localShape = GameObject.Find("Sphere");
        }
        if (localShape != null)
        {
            netMecAnim = localShape.GetComponent<NetworkMecanimAnimator>();
        }
        Debug.Log($"Input Authority: {Object.InputAuthority}");
        Debug.Log($"Has Input Authority: {HasInputAuthority}");
        Debug.Log($"State Authority: {Object.StateAuthority}");
        Debug.Log($"Has State Authority: {HasStateAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (GetInput(out GameplayInput input))
        {
            if (input.shift)
            {
                Debug.Log("W");
                netMecAnim.Animator.SetTrigger("Shift");
            }
        }
    }
}