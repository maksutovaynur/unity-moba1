using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_Text logText;
    public TMP_InputField nicknameInput;
    void Start()
    {
        logText.text = "";
        PhotonNetwork.GameVersion = "1.0.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Log("Connected to Master");
    }

    private string GenerateRandomUsername()
    {
        return "Player " + Random.Range(1000, 9999);
    }
    
    public void StartGame()
    {
        var nickName = nicknameInput.text.IsNullOrEmpty() ? GenerateRandomUsername() : nicknameInput.text;
        Log("Join room as: " + nickName);
        PhotonNetwork.NickName = nickName;
        PlayerPrefs.SetString("NickName", nickName);
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Log("Cann't join existing room: " + message + "; Creating a new one");
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions {MaxPlayers = 10, CleanupCacheOnLeave = true});
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Log("Failed to create room: " + message);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Game");
    }

    private void Log(string message)
    {
        Debug.Log(message);
        logText.text += "\n" + message;
    }
}