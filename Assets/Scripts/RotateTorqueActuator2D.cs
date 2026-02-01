using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateTorqueActuator2D : MonoBehaviour, IActuator
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Speed (press longer -> faster)")]
    [SerializeField] private float baseMaxAngVel = 22f;
    [SerializeField] private float maxMaxAngVel = 140f;
    [SerializeField] private float secondsToMax = 6f;

    [Header("Torque (also ramps up so it actually accelerates)")]
    [SerializeField] private float baseTorque = 55f;
    [SerializeField] private float maxTorque = 700f;

    [Header("Idle Brake (smooth decel)")]
    [SerializeField] private float brakeStrength = 8.0f;
    [SerializeField] private float stopThreshold = 0.15f;

    [Header("Input")]
    [SerializeField] private float inputDeadzone = 0.05f;

    [Header("Extra damping (optional)")]
    [SerializeField] private float angularDragWhenIdle = 3.0f;
    [SerializeField] private float angularDragWhenDriving = 0.2f;

    [Header("Direction change")]
    [SerializeField] private bool resetRampOnDirectionChange = true;

    private float inputX;
    private float rampTime;
    private int lastDir; // -1, 0, +1

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void SetInput(float x) => inputX = x;

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        bool driving = Mathf.Abs(inputX) > inputDeadzone;
        int dir = driving ? (inputX > 0f ? 1 : -1) : 0;

        // ✅ 요구사항 1: 입력이 없으면 가속 누적은 무조건 0 (잔존 불가)
        if (!driving)
        {
            rampTime = 0f;
        }
        else
        {
            // ✅ 요구사항 2: 방향 바꾸면 누적 초기화 (A로 쌓은 가속이 D로 넘어가지 않음)
            if (resetRampOnDirectionChange && lastDir != 0 && dir != lastDir)
                rampTime = 0f;

            rampTime += dt;
        }

        // 관성 제어(원하면 값 조절)
        rb.angularDamping = driving ? angularDragWhenDriving : angularDragWhenIdle;

        float ramp01 = (secondsToMax <= 0.001f) ? 1f : Mathf.Clamp01(rampTime / secondsToMax);

        // 초반 미세조작 곡선
        float rampFine = ramp01 * ramp01;

        float currentMaxAngVel = Mathf.Lerp(baseMaxAngVel, maxMaxAngVel, rampFine);
        float currentTorque = Mathf.Lerp(baseTorque, maxTorque, rampFine);

        if (driving)
        {
            rb.AddTorque(-inputX * currentTorque, ForceMode2D.Force);

            // ✅ 뚝뚝 끊김 줄이기: 매틱 강제 Clamp 대신 "초과할 때만" 제한
            if (Mathf.Abs(rb.angularVelocity) > currentMaxAngVel)
                rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * currentMaxAngVel;
        }
        else
        {
            // 감속(입력 없으면 rampTime은 이미 0이지만, 각속도는 물리 관성이라 남을 수 있음)
            float av = rb.angularVelocity;
            float t = 1f - Mathf.Exp(-brakeStrength * dt);
            av = Mathf.Lerp(av, 0f, t);

            if (Mathf.Abs(av) < stopThreshold) av = 0f;
            rb.angularVelocity = av;
        }

        lastDir = dir;
    }
}
