using System;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

enum AnimationState : byte
{
    Idle = 0,
    Walking = 1,
    Jumping = 2,
    Hurt = 3,
    Striking = 4,
    FallDown = 5,
    Stop = 101,
}

public class PlayerControls : MonoBehaviour, IPunObservable
{
    public TextMeshPro nickName;
    public float jumpPullForce = 5.0f;
    public float movementSpeed = 1.6f;
    public float movementAcceleration = 8.0f;
    public GameObject fireballPrefab;
    public double fireballInterval = 0.5f;
    public int maxHealth = 100;

    private sbyte lookDirectionState, moveDirectionState, moveSpeedState;
    private sbyte kbJump, kbMoveDirection, kbMoveSpeed;

    private AnimationState animationState;
    private TMP_Text playerStatusBar;
    private Animator animator;
    private Rigidbody2D rigidBody;
    private int lastCheckInput;
    private float flyPower;
    private PhotonView photonView;
    private SpriteRenderer spriteRenderer;
    private bool inJump, grounded, groundCollision, isFiringBalls;
    private const double STILL_GROUNDED_DELTA_TIME = 0.2;
    private const double STILL_JUMP_DELTA_TIME = 0.5;
    private double groundCollisionEnter, groundCollisionExit, lastJumpTime, lastFireballTime;
    private int groundCollisionNumber, lastJumpGroundCollisionNumber;
    private FixedJoystick joystick;
    private Vector2 fireballPoint;
    private bool platformIsPC;
    private bool platformIsMobile;
    private EventSystem eventSystem;
    private Damageable damage;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        var collider = GetComponent<Collider2D>();
        eventSystem = EventSystem.current;
        damage = GetComponent<Damageable>();

        joystick = FindObjectOfType<FixedJoystick>();
        playerStatusBar = GameObject.Find("PlayerStatusBar").GetComponent<TMP_Text>();
        rigidBody = GetComponent<Rigidbody2D>();

        moveDirectionState = 0;
        flyPower = 1.0f;
        lookDirectionState = 0;
        nickName.SetText(photonView.Owner.NickName);
        animationState = AnimationState.Idle;

        grounded = false;
        groundCollision = false;
        isFiringBalls = false;
        groundCollisionNumber = 0;

        if (HasControl())
        {
            nickName.color = Color.green;
            FindObjectOfType<CameraPlayerFollower>().target = this.transform;
            
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    platformIsPC = true;
                    break;
                case RuntimePlatform.WindowsEditor:
                    platformIsPC = true;
                    break;
                case RuntimePlatform.Android:
                    platformIsMobile = true;
                    break;
            }

            if (!platformIsMobile)
            {
                Destroy(joystick.gameObject);
            }
        }
        else
        {
            nickName.color = Color.red;
            // rigidBody.bodyType = RigidbodyType2D.Static;
            Destroy(rigidBody);
            collider.gameObject.layer = 8;
            // Destroy(collider);
        }
    }

    private bool HasControl()
    {
        return photonView.IsMine || !PhotonNetwork.IsConnected;
    }

    void Update()
    {
        if (HasControl())
        {
            GetInputs();
            FixMovement();
            FireBalls();
            FixAnimationState();
            LogStatusBar();
        }

        spriteRenderer.flipX = (lookDirectionState < 0);
        animator.SetInteger("state", (int) animationState);
    }

    private void FixAnimationState()
    {            
        if (inJump)
        {
            animationState = AnimationState.Jumping;
        }
        else
        {
            if (moveDirectionState != 0) animationState = AnimationState.Walking;
            else if (grounded)
            {
                animationState = AnimationState.Idle;
            }
            else
            {
                animationState = AnimationState.FallDown;
            }
        }
    }

    private void FixMovement()
    {
        var currentTime = PhotonNetwork.Time;

        bool recentColl = IsWithin(currentTime - groundCollisionEnter, 0, STILL_GROUNDED_DELTA_TIME);
        bool recentJump = IsWithin(currentTime - lastJumpTime, 0, STILL_JUMP_DELTA_TIME);
        bool sameCollision = (lastJumpGroundCollisionNumber == groundCollisionNumber);

        inJump = recentJump || (sameCollision && !groundCollision);
        grounded = groundCollision || recentColl;

        if (grounded)
        {
            if (!inJump)
            {
                if (kbJump == 1)
                {
                    rigidBody.AddForce(jumpPullForce * Vector2.up, ForceMode2D.Impulse);
                    lastJumpGroundCollisionNumber = groundCollisionNumber;
                    lastJumpTime = currentTime;
                }
            }

            moveDirectionState = kbMoveDirection;
            moveSpeedState = kbMoveSpeed;
        }

        if (kbMoveDirection != 0) lookDirectionState = kbMoveDirection;

        var speed = movementSpeed * ((moveSpeedState == 0) ? 1.0f : movementAcceleration);
        rigidBody.velocity = new Vector2(moveDirectionState * speed, rigidBody.velocity.y);
    }

    private void FireBalls()
    {
        if (isFiringBalls)
        {
            var cantFire = IsWithin(PhotonNetwork.Time - lastFireballTime, 0.0, fireballInterval);
            if (cantFire) return;
            lastFireballTime = PhotonNetwork.Time;
            var currentPosition = transform.position;
            var startPosition = new Vector3(currentPosition.x, currentPosition.y, currentPosition.z + 1.0f);
            var fireballDirection = new Vector3(
                fireballPoint.x - currentPosition.x, 
                fireballPoint.y - currentPosition.y, 0.0f);
            var rotation = Quaternion.FromToRotation(Vector3.right, fireballDirection);
            var o = PhotonNetwork.Instantiate(fireballPrefab.name, startPosition, rotation);
            o.GetComponent<FireballBehavior>().parent = gameObject;
        }
    }

    void GetInputs()
    {
        kbMoveDirection = 0;
        kbJump = 0;
        kbMoveSpeed = 0;

        var hor = Input.GetAxis("Horizontal");
        if (hor > 0.2f) kbMoveDirection = 1;
        else if (hor < -0.2f) kbMoveDirection = -1;

        if (platformIsMobile)
        {
            if (joystick.Horizontal > 0.2f) kbMoveDirection = 1;
            else if (joystick.Horizontal < -0.2f) kbMoveDirection = -1;
            if (Math.Abs(joystick.Horizontal) > 0.7f) kbMoveSpeed = 1;

            if (joystick.Vertical > 0.3f) kbJump = 1;
        }

        if (Input.GetKey(KeyCode.Space)) kbJump = 1;
        if (Input.GetKey(KeyCode.LeftShift)) kbMoveSpeed = 1;
        
        
        isFiringBalls = false;
        if (platformIsPC && Input.GetMouseButton(0))
        {
                fireballPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                isFiringBalls = true;
        }

        else if (platformIsMobile && Input.touchCount > 0)
        {
            var ts = Input.touches.Where(t => !eventSystem.IsPointerOverGameObject(t.fingerId));
            ts = ts.Where(t => joystick.lastPointerId != t.fingerId);
            ts = ts.Where(t => t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled);
            if (ts.Any())
            {
                var touch = ts.First();
                fireballPoint = Camera.main.ScreenToWorldPoint(touch.position);
                isFiringBalls = true;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!HasControl()) return;
        // if (other.gameObject.name != "Tilemap-base") return;
        groundCollision = true;
        groundCollisionNumber += 1;
        groundCollisionEnter = PhotonNetwork.Time;
        Debug.Log("Item grounded " + groundCollisionNumber + " times");
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (!groundCollision) OnCollisionEnter2D(other);
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (HasControl())
        {
            groundCollision = false;
            groundCollisionExit = PhotonNetwork.Time;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        byte lookDirectionCast;
        if (stream.IsWriting)
        {
            unchecked
            {
                lookDirectionCast = (byte) lookDirectionState;
            }

            stream.SendNext(lookDirectionCast);
            stream.SendNext(animationState);
            stream.SendNext(damage.GetHealth());
        }
        else
        {
            lookDirectionCast = (byte) stream.ReceiveNext();
            unchecked
            {
                lookDirectionState = (sbyte) lookDirectionCast;
            }

            animationState = (AnimationState) stream.ReceiveNext();
            damage.SetHealth((int) stream.ReceiveNext());
        }
    }

    private void LogStatusBar()
    {
        playerStatusBar.SetText($@"
                         Nick: {nickName.text}
              Animation State: {animationState}
              Is in collision: {groundCollision}
             Collision number: {groundCollisionNumber} 
   Last Jump Collision Number: {lastJumpGroundCollisionNumber}
                   Is in jump: {inJump}
                  Is grounded: {grounded}
              Is firing balls: {isFiringBalls}
           AplicationPlatform: {Application.platform}
                          PC?: {platformIsPC}
                      Mobile?: {platformIsMobile}
                  HasControl?: {HasControl()}
                       Health: {damage.GetHealth()}
");
    }

    private string MovemenDirectionToSymbol()
    {
        return (moveDirectionState == 0) ? "_" : (moveDirectionState < 0) ? "<" : ">";
    }

    private bool IsWithin(double x, double left, double right)
    {
        return (left < x) && (x < right);
    }
}