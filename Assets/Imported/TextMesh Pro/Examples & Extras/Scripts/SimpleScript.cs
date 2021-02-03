using UnityEngine;
using Photon.Pun;


namespace TMPro.Examples
{
    
    public class LobbyManger : MonoBehaviourPunCallbacks
    {
        public TextMeshPro logText;
        // Start is called before the first frame update
        void Start()
        {
            PhotonNetwork.NickName = "Player " + Random.Range(1000, 9999);
            Log("Generated username: " + PhotonNetwork.NickName);
            PhotonNetwork.GameVersion = "1.0.0";
            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnConnectedToMaster()
        {
            Log("Connected to Master");
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void Log(string message)
        {
            Debug.Log(message);
            logText.text += "\n" + message;
        }
    }
}
