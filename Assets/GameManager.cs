using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player " + newPlayer.NickName + " entered Room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player " + otherPlayer.NickName + " left Room");
    }
    
    public void StopGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void Start()
    {
        var startPosition = new Vector3(Random.Range(-10, 10), Random.Range(-5, -2));
        PhotonNetwork.Instantiate(playerPrefab.name, startPosition, Quaternion.identity);
    }
}
