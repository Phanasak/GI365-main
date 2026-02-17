using TMPro;
using UnityEngine;

public class A_SimplePlayer : MonoBehaviour // library สำหรับตอน gameplay
{
    private Rigidbody2D rigid; // สำหรับการเคลื่อนที่
    private Animator anim; // สำหรับ animation

    [Header("Ground And Wall Check")]
    [SerializeField] private float groundDistCheck = 1f; // ระยะ sensor ที่วิ่งไปชนพื้น
    [SerializeField] private float wallDistCheck = 1f; // ระยะ sensor ที่วิ่งไปชนผนัง
    [SerializeField] private LayerMask groundLayer; // หาเฉพาะ layer ของพื้น
    public bool isGrounded = false; // ตรวจชนพื้น
    public bool isWalled = false;  // ตรวจชนกำแพง

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    private float X_input, Y_input;
    private int facing = 1;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(15f, 10f);
    private bool isJumping = false;
    private bool isWallJumping = false;
    private bool isWallSliding = false;
    private bool canDoubleJump = false;

    [SerializeField] private float coyoteTimeLimit = .5f;
    [SerializeField] private float bufferTimeLimit = .5f;
    private float coyoteTime = -10000f;
    private float bufferTime = -10000f;

    private void Awake() // ทำงานก่อนเข้ามาใน game
    {
        rigid = GetComponent<Rigidbody2D>(); // มันอยู่ที่ gameobject นี้
        anim = GetComponentInChildren<Animator>(); // ใช้ InChildren เพราะ Animator อยู่ที่ลูก
    }
    private void Update() // ทำงานทุก frame
    {
        JumpState(); // ตรวจสถานะว่า อยู่บนพื้น กำลังกระโดด กำลังลงพื้น หรือ wallSlide
        Jump(); // สั่งกระโดดในแบบต่างๆ
        WallSlide(); // สั่ง wallSlide
        InputVal(); // ตรวจ input จากผู้เล่น
        Move(); // สั้งเคลื่อนไหวทั้งบนพื้นและอากาศ
        Flip(); // สั่งหันหน้าไปทางทิศการเคลื่อนที่อัดโนมัต
        GroundAndWallCheck(); // ตรวจจับพื้นและผนัง
        Animation(); // สั่ง animation
    }
    private void JumpState()
    {
        if (!isGrounded && !isJumping) // takeoff / fall
        {
            isJumping = true;

            if (rigid.linearVelocityY <= 0f) // fall
            {
                coyoteTime = Time.time; // เริ่มนับ coyote
            }
        }

        if (isGrounded && isJumping) // landing
        {
            isJumping = false;
            isWallJumping = false;
            isWallSliding = false;
            canDoubleJump = false;
        }

        if (isWalled) // wallSliding
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;
            if (isGrounded)
            {
                isWallSliding = false;
            }
            else
            {
                isWallSliding = true;
            }
        }
        else
        {
            isWallSliding = false;
        }
    }
    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isWalled)
            {
                if (isGrounded)
                {
                    canDoubleJump = true;
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***normalJump
                }
                else
                {
                    if (rigid.linearVelocityY > 0f && canDoubleJump)
                    {
                        canDoubleJump = false;
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***doubleJump
                    }

                    if (rigid.linearVelocityY < 0f)
                    {
                        if (Time.time < coyoteTime + coyoteTimeLimit)
                        {
                            coyoteTime = 0f;
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***coyoteJump
                        }
                        else
                        {
                            bufferTime = Time.time; // เริ่ม bufferTime
                        }
                    }
                }
            }
            else
            {
                isWallJumping = true;
                rigid.linearVelocity = new Vector2(wallJumpForce.x * facing, wallJumpForce.y);
            }
        }
        else
        {
            if (isGrounded && Time.time < bufferTime + bufferTimeLimit)
            {
                bufferTime = 0f;
                canDoubleJump = true;
                rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***bufferJump
            }
        }
    }
    private void WallSlide()
    {
        if (!isWallSliding || isGrounded || isWallJumping || rigid.linearVelocityY > 0f)
            return;

        float Y_slide = Y_input < 0f ? 1f : .5f;
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * Y_slide);
    }
    private void InputVal()
    {
        X_input = Input.GetAxisRaw("Horizontal");
        Y_input = Input.GetAxisRaw("Vertical");
    }
    private void Move()
    {
        if (isWallJumping)
            return;

        if (isGrounded)
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY);
        }
        else
        {
            float X_airMove = X_input != 0f ? X_input * moveSpeed : rigid.linearVelocityX;
            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }
    private void Flip()
    {
        if (rigid.linearVelocityX > 0.1f)
        {
            facing = -1;
            transform.rotation = Quaternion.identity;
        }
        else if (rigid.linearVelocityX < -0.1f)
        {
            facing = 1;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
    private void GroundAndWallCheck()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundDistCheck, groundLayer); // sensor พื้น
        isWalled = Physics2D.Raycast(transform.position, transform.right, wallDistCheck, groundLayer); // sensor ผนัง
    }
    private void OnDrawGizmos() // กราฟฟิกแสดงผลของ sensor ตรวจจับพื้นและผนัง
    {
        Gizmos.color = Color.blue; // เส้นสีน้ำเงิน
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistCheck); // เส้น sensor ตรวจพื้น
        Gizmos.color = Color.red; // เส้นแดง
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallDistCheck); // เส้น sensor ตรวจผนัง
    }
    private void Animation()
    {

    }
}