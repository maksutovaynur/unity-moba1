using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


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
    public TextMeshPro statusBar;
    public double inputThrottleInterval = 0.1;
    public float flyPullForce = 5.0f;
    public float flyPowerRecovery = 0.2f;
    public float flyPowerLoss = 0.5f;
    public float jumpPullForce = 5.0f;
    public float movementSpeed = 1.6f;
    public float movementAcceleration = 8.0f;
    
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
    private bool inJump, grounded, groundCollision;
    private const double STILL_GROUNDED_DELTA_TIME = 0.2;
    private const double STILL_JUMP_DELTA_TIME = 0.5;
    private double groundCollisionEnter, groundCollisionExit, lastJumpTime;
    private int groundCollisionNumber, lastJumpGroundCollisionNumber;
    private FixedJoystick joystick;
    
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        joystick = FindObjectOfType<FixedJoystick>();
        playerStatusBar = GameObject.Find("PlayerStatusBar").GetComponent<TMP_Text>();
        rigidBody = GetComponent<Rigidbody2D>();
        
        moveDirectionState = 0;
        flyPower = 1.0f;
        lookDirectionState = 0;
        nickName.SetText(photonView.Owner.NickName);
        statusBar.SetText("");
        animationState = AnimationState.Idle;
        
        grounded = false;
        groundCollision = false;
        groundCollisionNumber = 0;

        if (photonView.IsMine)
        {
            nickName.color = Color.green;
            statusBar.color = Color.green;
            FindObjectOfType<CameraPlayerFollower>().target = this.transform;
        }
        else
        {
            nickName.color = Color.red;
            statusBar.color = Color.red;
            rigidBody.isKinematic = false;
        }
    }

    void Update()
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            GetInputs();
            FixMovement();
            FixAnimationState();
            LogStatusBar();
        }
        spriteRenderer.flipX = (lookDirectionState < 0);
        animator.SetInteger("state", (int) animationState);
    }

    private void FixAnimationState()
    {
        if (grounded)
        {
            if (moveDirectionState != 0) animationState = AnimationState.Walking;
            else animationState = AnimationState.Idle;
        }
        else
        {
            if (inJump)
            {
                animationState = AnimationState.Jumping;
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
        
        var speed = movementSpeed * ((moveSpeedState == 0)? 1.0f : movementAcceleration);
        transform.Translate(moveDirectionState * speed * Time.deltaTime, 0, 0);
    }

    void GetInputs()
    {
        kbMoveDirection = 0;
        kbJump = 0;
        kbMoveSpeed = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) kbMoveDirection -= 1;
        if (Input.GetKey(KeyCode.RightArrow)) kbMoveDirection += 1;

        if (joystick.Horizontal > 0.2f) kbMoveDirection = 1;
        else if (joystick.Horizontal < -0.2f) kbMoveDirection = -1;
        if (Math.Abs(joystick.Horizontal) > 0.7f) kbMoveSpeed = 1;

        if (joystick.Vertical > 0.3f) kbJump = 1;
        
        if (Input.GetKey(KeyCode.Space)) kbJump = 1;
        if (Input.GetKey(KeyCode.LeftShift)) kbMoveSpeed = 1;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (photonView.IsMine) {
            if (other.gameObject.name == "Tilemap-base")
            {
                groundCollision = true;
                groundCollisionNumber += 1;
                groundCollisionEnter = PhotonNetwork.Time;
                Debug.Log("Item grounded " + groundCollisionNumber + " times");
            }
            else
            {
                Debug.Log("Item touched game object '" + other.gameObject.name + "'");
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (photonView.IsMine)
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
        }
        else
        {
            lookDirectionCast = (byte) stream.ReceiveNext();
            unchecked
            {
                lookDirectionState = (sbyte) lookDirectionCast;
            }
            animationState = (AnimationState) stream.ReceiveNext();
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
