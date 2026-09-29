using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public GameObject player;
    [Space]
    public Transform spawnPos;

    [Space]
    public GameObject roomCam;
    [Space]
    public GameObject buttonMenu;
    public GameObject connectingScreen;


    public void QuickMatch()
    {
        Debug.Log("Connecting...");

        buttonMenu.SetActive(false);
        connectingScreen.SetActive(true);

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Server");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinOrCreateRoom("test", SetRoomOptions(), null);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("We're connected and in a room now");

        GameObject _player = PhotonNetwork.Instantiate(player.name, spawnPos.position, Quaternion.identity);
        _player.GetComponent<PlayerSetup>().IsLocalPlayer();

        roomCam.SetActive(false);
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (returnCode == 32765)
        {
            Debug.Log("Room is full. Creating new room...");

            PhotonNetwork.CreateRoom("test2", SetRoomOptions(), null);
            roomCam.SetActive(false);
        }
    }

    public RoomOptions SetRoomOptions()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        return roomOptions;
    }
}
