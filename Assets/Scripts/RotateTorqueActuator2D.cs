using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateTorqueActuator2D : MonoBehaviour, IActuator
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Speed (press longer -> faster)")]
    [SerializeField] private float baseMaxAngVel = 80f;     // 시작 속도 (원한대로 80)
    [SerializeField] private float maxMaxAngVel = 420f;     // 오래 누르면 도달할 최대속도
    [SerializeField] private float secondsToMax = 5f;       // ✅ 이 시간이 길수록 가속이 '천천히' 체감됨

    [Header("Torque (also ramps up so it actually accelerates)")]
    [SerializeField] private float baseTorque = 300f;       // 시작 토크
    [SerializeField] private float maxTorque = 2000f;       // 오래 누르면 토크(하중 버티려면 크게)

    [Header("Idle Brake (smooth decel)")]
    [SerializeField] private float brakeStrength = 1.2f;    // 낮을수록 천천히 감속
    [SerializeField] private float stopThreshold = 0.0f;    // 0 추천(뚝 멈춤 제거)

    [Header("Input")]
    [SerializeField] private float inputDeadzone = 0.05f;

    [Header("Ramp behavior when released")]
    [SerializeField] private float rampDownPerSec = 0.6f;   // 손 떼면 누적가속이 얼마나 빨리 내려갈지
    [SerializeField] private bool resetRampInstantlyOnRelease = false;

    private float inputX;

    // 누르고 있는 시간(초)
    private float rampTime;

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

        if (driving)
        {
            // ✅ 누르고 있는 시간 누적
            rampTime += dt;
        }
        else
        {
            // ✅ 손 떼면 누적 가속 내려가기
            if (resetRampInstantlyOnRelease)
                rampTime = 0f;
            else
                rampTime = Mathf.Max(0f, rampTime - rampDownPerSec * dt);
        }

        // 0~1 가속 게이지
        float ramp01 = (secondsToMax <= 0.001f) ? 1f : Mathf.Clamp01(rampTime / secondsToMax);

        // ramp에 따라 최고속도/토크 함께 증가 (체감 + 실제 가속 보장)
        float currentMaxAngVel = Mathf.Lerp(baseMaxAngVel, maxMaxAngVel, ramp01);
        float currentTorque = Mathf.Lerp(baseTorque, maxTorque, ramp01);

        if (driving)
        {
            rb.AddTorque(-inputX * currentTorque, ForceMode2D.Force);
            rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -currentMaxAngVel, currentMaxAngVel);
        }
        else
        {
            // ✅ 부드러운 감속
            float av = rb.angularVelocity;
            float t = 1f - Mathf.Exp(-brakeStrength * dt);
            av = Mathf.Lerp(av, 0f, t);

            if (stopThreshold > 0f && Mathf.Abs(av) < stopThreshold) av = 0f;
            rb.angularVelocity = av;
        }
    }
}
