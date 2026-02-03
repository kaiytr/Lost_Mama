using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateTorqueActuator2D : MonoBehaviour, IActuator, IChargeInputReceiver
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

    [Header("Input freshness (possession switching)")]
    [SerializeField] private float inputStaleSeconds = 0.12f;
    [SerializeField] private bool coastWhenInputStale = true;
    [SerializeField] private float angularDragWhenStale = 0.6f;

    [Header("Charge (hold Space)")]
    [Tooltip("Space를 누른 상태에서는 토크를 적용하지 않고, 누적만 쌓습니다.")]
    [SerializeField] private bool enableCharge = true;

    [Tooltip("Space를 뗄 때, 누적된 값으로 각속도를 즉시 올릴지 여부")]
    [SerializeField] private bool releaseSetsAngularVelocity = true;

    [Header("Release Guard (prevents clamp/brake killing the pop)")]
    [Tooltip("릴리즈 직후 이 시간 동안 clamp/brake를 스킵해서 '팍'이 죽지 않게 함")]
    [SerializeField] private float releaseGuardSeconds = 0.08f;

    // ===== DEBUG =====
    [Header("DEBUG (Inspector)")]
    [SerializeField] private bool dbg_enable = true;

    [SerializeField] private bool dbg_inputFresh;
    [SerializeField] private bool dbg_isCharging;
    [SerializeField] private float dbg_inputX;
    [SerializeField] private bool dbg_driving;
    [SerializeField] private int dbg_dir;
    [SerializeField] private float dbg_chargeTime;
    [SerializeField] private int dbg_chargeDir;
    [SerializeField] private bool dbg_releasedThisStep;
    [SerializeField] private float dbg_angularVelocity;
    [SerializeField] private float dbg_rampTime;
    [SerializeField] private float dbg_releaseGuardTimer;

    private float inputX;
    private bool isCharging;

    private float rampTime;     // 평상시 가속 누적
    private int lastDir;        // 평상시 방향 기록

    private float chargeTime;   // 차지 중 누적
    private int chargeDir;      // 차지 중 마지막 방향(-1/+1)
    private bool prevCharging;  // 스페이스 릴리즈 감지용

    private float lastInputTime = -999f;
    private float releaseGuardTimer = 0f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void SetInput(float x)
    {
        inputX = x;
        lastInputTime = Time.time;
    }

    public void SetCharging(bool charging)
    {
        isCharging = charging;
        lastInputTime = Time.time; // 차지 신호도 입력 갱신으로 취급
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        releaseGuardTimer = Mathf.Max(0f, releaseGuardTimer - dt);

        bool inputFresh = (Time.time - lastInputTime) <= inputStaleSeconds;

        // 디버그를 위해 driving/dir는 early return 전에 계산
        bool driving = Mathf.Abs(inputX) > inputDeadzone;
        int dir = driving ? (inputX > 0f ? 1 : -1) : 0;

        bool releasedThisStep = false;

        if (!inputFresh)
        {
            // 전환 등으로 입력이 끊김 -> 토크/브레이크 관여 최소화(관성 유지)
            rampTime = 0f;
            chargeTime = 0f;
            chargeDir = 0;
            lastDir = 0;
            prevCharging = false;
            releaseGuardTimer = 0f;

            rb.angularDamping = coastWhenInputStale ? angularDragWhenStale : angularDragWhenIdle;

            DebugSnap(inputFresh, driving, dir, releasedThisStep);
            return;
        }

        // -------------------------
        // 1) 스페이스 "떼는 순간" 감지(릴리즈 이벤트)
        // -------------------------
        if (enableCharge && prevCharging && !isCharging)
        {
            if (chargeTime > 0f && chargeDir != 0)
            {
                releasedThisStep = true;

                // ✅ 핵심: 릴리즈가 clamp에 죽지 않게 평상시 누적도 동기화
                rampTime = chargeTime;
                lastDir = chargeDir;

                // ✅ 팍 방출
                ApplyChargeRelease(chargeTime, chargeDir);

                // ✅ 릴리즈 직후 몇 프레임은 clamp/brake를 스킵해서 팍이 유지되게
                releaseGuardTimer = releaseGuardSeconds;

                // 누적 초기화
                chargeTime = 0f;
                chargeDir = 0;
            }
        }

        // -------------------------
        // 2) 차지 중이면: "실제 토크 적용 X", 누적만 쌓기
        // -------------------------
        if (enableCharge && isCharging)
        {
            // 평상시 ramp 누적은 차지 중엔 쌓지 않게
            rampTime = 0f;
            lastDir = 0;

            // 차지는 A/D가 눌릴 때만 누적
            if (driving)
            {
                if (resetRampOnDirectionChange && chargeDir != 0 && dir != chargeDir)
                    chargeTime = 0f;

                chargeDir = dir;
                chargeTime += dt;
            }

            // "움직임을 멈추게" 보이도록: damping을 크게 두는 방식(지금 값이면 멈춰 보임)
            rb.angularDamping = angularDragWhenIdle;

            prevCharging = isCharging;

            DebugSnap(inputFresh, driving, dir, releasedThisStep);
            return;
        }

        // 차지 중이 아니면 prevCharging 갱신
        prevCharging = isCharging;

        // -------------------------
        // 3) 평상시 회전 로직(기존)
        // -------------------------
        if (!driving)
        {
            // 릴리즈 직후엔 rampTime을 0으로 날리면 다시 base clamp로 돌아가므로
            // releaseGuard 동안엔 유지해도 되지만, 여기서는 기존 규칙 유지:
            rampTime = 0f;
        }
        else
        {
            if (resetRampOnDirectionChange && lastDir != 0 && dir != lastDir)
                rampTime = 0f;

            rampTime += dt;
        }

        rb.angularDamping = driving ? angularDragWhenDriving : angularDragWhenIdle;

        float ramp01 = (secondsToMax <= 0.001f) ? 1f : Mathf.Clamp01(rampTime / secondsToMax);
        float rampFine = ramp01 * ramp01;

        float currentMaxAngVel = Mathf.Lerp(baseMaxAngVel, maxMaxAngVel, rampFine);
        float currentTorque = Mathf.Lerp(baseTorque, maxTorque, rampFine);

        if (driving)
        {
            rb.AddTorque(-inputX * currentTorque, ForceMode2D.Force);

            // ✅ 릴리즈 직후 보호 시간에는 clamp 스킵
            if (releaseGuardTimer <= 0f)
            {
                if (Mathf.Abs(rb.angularVelocity) > currentMaxAngVel)
                    rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * currentMaxAngVel;
            }
        }
        else
        {
            // ✅ 릴리즈 직후 보호 시간에는 브레이크 스킵 (팍이 죽는 것 방지)
            if (!releasedThisStep && releaseGuardTimer <= 0f)
            {
                float av = rb.angularVelocity;
                float t = 1f - Mathf.Exp(-brakeStrength * dt);
                av = Mathf.Lerp(av, 0f, t);

                if (Mathf.Abs(av) < stopThreshold) av = 0f;
                rb.angularVelocity = av;
            }
        }

        lastDir = dir;

        DebugSnap(inputFresh, driving, dir, releasedThisStep);
    }

    private void DebugSnap(bool inputFresh, bool driving, int dir, bool releasedThisStep)
    {
        if (!dbg_enable) return;

        dbg_inputFresh = inputFresh;
        dbg_isCharging = isCharging;
        dbg_inputX = inputX;
        dbg_driving = driving;
        dbg_dir = dir;
        dbg_chargeTime = chargeTime;
        dbg_chargeDir = chargeDir;
        dbg_releasedThisStep = releasedThisStep;
        dbg_angularVelocity = rb != null ? rb.angularVelocity : 0f;
        dbg_rampTime = rampTime;
        dbg_releaseGuardTimer = releaseGuardTimer;
    }

    private void ApplyChargeRelease(float chargedSeconds, int dir)
    {
        float ramp01 = (secondsToMax <= 0.001f) ? 1f : Mathf.Clamp01(chargedSeconds / secondsToMax);
        float rampFine = ramp01 * ramp01;

        float releaseMaxAngVel = Mathf.Lerp(baseMaxAngVel, maxMaxAngVel, rampFine);

        // 기존 코드가 AddTorque에서 -inputX를 쓰므로, 방향도 동일한 부호로 맞춤
        float desiredAngVel = (-dir) * releaseMaxAngVel;

        if (releaseSetsAngularVelocity)
        {
            // ✅ 조건부가 아니라 "무조건" 세팅: 팍이 확실하게 보임
            rb.angularVelocity = desiredAngVel;
        }
        else
        {
            // 대체 방식(옵션): 목표 각속도로 가는 임펄스 토크
            // (물리 단위 차이/감각 튜닝이 필요할 수 있음)
            float delta = desiredAngVel - rb.angularVelocity;
            rb.AddTorque(rb.inertia * delta, ForceMode2D.Impulse);
        }
    }
}
