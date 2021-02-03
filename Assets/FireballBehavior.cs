using Photon.Pun;
using UnityEngine;

public class FireballBehavior : MonoBehaviour, IPunObservable
{
    public PhotonView photonView;
    public GameObject parent;

    public int damage = 10;
    public float velocity = 4.0f;
    private bool exploded;
    private Vector3 velocityVector;
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        velocityVector = Vector3.right * velocity;
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("Exploded", exploded);
        transform.Translate(velocityVector * Time.deltaTime);
    }
    
    private bool HasControl()
    {
        return photonView.IsMine || !PhotonNetwork.IsConnected;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasControl()) return;
        if (parent == other.gameObject) return;
        var damageable = other.gameObject.GetComponent<Damageable>();
        if (damageable != null)
        {
            damageable.MakeDamage(damage);
        }
        exploded = true;
        animator.SetBool("Exploded", exploded);
        velocityVector = Vector3.zero;
        Debug.Log($"Fireball: damageable {damageable != null}");
    }

    private void KillFireball()
    {
        if (!HasControl()) return;
        PhotonNetwork.Destroy(gameObject);
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    { if (stream.IsWriting)
        {
            stream.SendNext(exploded);
        }
        else
        {
            exploded = (bool) stream.ReceiveNext();
        }
    }
    
}
