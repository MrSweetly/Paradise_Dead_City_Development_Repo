using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    public Player player;

    public GameObject cam;

    public void IsLocalPlayer()
    {
        player.enabled = true;
        cam.SetActive(true);
    }
}
