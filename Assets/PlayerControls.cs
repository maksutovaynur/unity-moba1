using Photon.Pun;
using UnityEngine;

public class PlayerControls : MonoBehaviour, IPunObservable
{
    private sbyte lookDirection;
    private sbyte moveDirection;
    private PhotonView photonView;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveDirection = 0;
        lookDirection = 0;
    }
    
    void Update()
    {
        if (photonView.IsMine || ! PhotonNetwork.IsConnected )
        {
            ProcessInput();
        }

        if (lookDirection == 0) lookDirection = 1;
        spriteRenderer.flipX = (lookDirection < 0);
    }

    void ProcessInput()
    {
        moveDirection = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) moveDirection = -1;
        if (Input.GetKey(KeyCode.RightArrow)) moveDirection = 1;

        transform.Translate(Time.deltaTime * moveDirection, 0, 0);
        if (moveDirection != 0) lookDirection = moveDirection;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(moveDirection);
            stream.SendNext(lookDirection);
        }
        else
        {
            moveDirection = (sbyte) stream.ReceiveNext();
            lookDirection = (sbyte) stream.ReceiveNext();
        }
    }
}
