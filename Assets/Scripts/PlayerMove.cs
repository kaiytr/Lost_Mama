using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;

    [Header("Layer (everything is possessMask)")]
    [SerializeField] private LayerMask possessMask;

    [Header("Move (Arrow keys only)")]
    [SerializeField] private float moveSpeed = 6f;   // 평상시 좌우 목표 속도
    [SerializeField] private float accel = 70f;      // 땅에서 입력 시 목표속도로 끌어가는 가속
    [SerializeField] private float airAccel = 35f;   // 공중에서 조작력(원하면 0~)

    [Header("Stop / Friction")]
    [SerializeField] private float groundDecel = 120f;  // 땅에서 입력 없을 때 멈추는 감속
    [SerializeField] private float externalDecel = 12f; // 바운스/넉백 등으로 과속일 때 천천히 줄이는 감속

    [Header("Ground by contact normal (no layer split needed)")]
    [SerializeField, Range(0f, 1f)] private float groundNormalY = 0.6f;

    [Header("External velocity protection")]
    [SerializeField] private float defaultExternalProtect = 0.12f;

    private float inputX;
    private float externalProtectTimer;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // ✅ 방향키만 (A/D는 아예 안 읽음)
        bool left = Keyboard.current != null && Keyboard.current.leftArrowKey.isPressed;
        bool right = Keyboard.current != null && Keyboard.current.rightArrowKey.isPressed;
        inputX = (right ? 1f : 0f) - (left ? 1f : 0f); // 둘 다 누르면 0
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (externalProtectTimer > 0f)
        {
            externalProtectTimer -= dt;
            // ✅ 보호 시간에는 X를 건드리지 않음 (바운스가 준 X 성분 보존)
            return;
        }

        bool grounded = IsGroundedByNormal();
        Vector2 v = rb.linearVelocity;

        float desiredX = inputX * moveSpeed;

        if (inputX != 0f)
        {
            // ✅ 바운스로 이미 같은 방향 과속이면 억지로 낮추지 않기
            bool sameDirOverspeed =
                Mathf.Sign(v.x) == Mathf.Sign(desiredX) && Mathf.Abs(v.x) > Mathf.Abs(desiredX);

            if (!sameDirOverspeed)
            {
                float a = grounded ? accel : airAccel;
                v.x = Mathf.MoveTowards(v.x, desiredX, a * dt);
            }
        }
        else
        {
            // ✅ 입력 없을 때:
            // - 공중에서는 X를 건드리지 않음(바운스 보존)
            // - 땅에서만 멈추도록 감속
            if (grounded)
            {
                float dec = (Mathf.Abs(v.x) > moveSpeed) ? externalDecel : groundDecel;
                v.x = Mathf.MoveTowards(v.x, 0f, dec * dt);
            }
        }

        rb.linearVelocity = v;
    }

    private bool IsGroundedByNormal()
    {
        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = possessMask,
            useTriggers = false // ✅ 트리거는 바닥 판정에서 제외
        };

        int count = Physics2D.GetContacts(col, filter, contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y >= groundNormalY)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 바운스/넉백/바람 등 외부에서 속도를 준 직후,
    /// 일정 시간 PlayerMove가 X를 덮어쓰지 않게 보호.
    /// </summary>
    public void NotifyExternalVelocity(float seconds = -1f)
    {
        if (seconds < 0f) seconds = defaultExternalProtect;
        if (seconds > externalProtectTimer) externalProtectTimer = seconds;
    }
}