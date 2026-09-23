using UnityEngine;
using Fusion;

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
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (GetInput(out NetworkInputData input))
        {
            if (input.shift)
            {
                Debug.Log("W");
                netMecAnim.Animator.SetTrigger("Shift");
            }
        }
    }

    public struct NetworkInputData : INetworkInput
    {
        public bool shift;
    }
}