using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SliderJoint2D))]
public class SliderMotorActuator2D : MonoBehaviour, IActuator, IChargeInputReceiver, IPossessionCallbacks
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SliderJoint2D joint;

    [Header("Speed (press longer -> faster)")]
    [SerializeField] private float baseSpeed = 2.5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float secondsToMax = 2.5f;

    [Header("Idle Brake")]
    [SerializeField] private float brakeStrength = 10f;
    [SerializeField] private float stopThreshold = 0.05f;
    [SerializeField] private float inputDeadzone = 0.05f;

    [Header("Linear damping (feel)")]
    [SerializeField] private float linearDampingWhenDriving = 0.1f;
    [SerializeField] private float linearDampingWhenIdle = 2.0f;

    [Header("Input freshness (possession switching)")]
    [Tooltip("빙의 전환 등으로 입력이 끊긴 뒤, 이 시간 동안 SetInput/SetCharging이 안 오면 '스테일'로 판단합니다.")]
    [SerializeField] private float inputStaleSeconds = 0.06f;

    [Tooltip("스테일일 때 모터를 켠 채 speed=0으로 '붙잡기(브레이크)'합니다. 미끄러짐까지 거의 사라집니다.")]
    [SerializeField] private bool holdWhenStale = true;

    [Tooltip("스테일일 때(혹은 Unpossessed일 때) 적용할 선형 감쇠(드래그).")]
    [SerializeField] private float linearDampingWhenStale = 6.0f;

    [Header("Charge / Release (optional)")]
    [SerializeField] private bool enableCharge = true;
    [SerializeField] private float maxChargeSeconds = 2.0f;
    [SerializeField] private float releaseSpeedMultiplier = 1.8f;
    [SerializeField] private float releaseGuardSeconds = 0.12f;

    // ===== runtime state =====
    private float inputX;
    private float rampTime;
    private float lastDir;

    private bool isCharging;
    private bool prevCharging;
    private float chargeTime;
    private float chargeDir;

    private float releaseGuardTimer;

    // 입력 끊김 감지용(회전 스크립트 방식)
    private float lastInputTime = -999f;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!joint) joint = GetComponent<SliderJoint2D>();

        // 스크립트가 motor를 제어한다고 가정
        joint.useMotor = true;
    }

    // PossessableObject에서 호출됨
    public void SetInput(float x)
    {
        inputX = Mathf.Clamp(-x, -1f, 1f);
        lastInputTime = Time.time;
    }

    // Space 차지 입력
    public void SetCharging(bool charging)
    {
        isCharging = charging;
        lastInputTime = Time.time; // 차지 신호도 입력 갱신으로 취급(회전 스크립트와 동일)
    }

    // ===== IPossessionCallbacks =====
    public void OnPossessed()
    {
        // 다시 빙의되면 정상 동작
        lastInputTime = Time.time;
        joint.useMotor = true;
    }

    public void OnUnpossessed()
    {
        // ✅ 가장 확실한 즉시 차단(0.06초 기다리는 것조차 싫으면 여기서 바로 끊어버리기)
        ForceStaleStop();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (releaseGuardTimer > 0f) releaseGuardTimer -= dt;

        bool inputFresh = (Time.time - lastInputTime) <= inputStaleSeconds;

        // ===== 입력이 끊겼다면(빙의 전환) =====
        if (!inputFresh)
        {
            // 회전 스크립트처럼 "더 이상 힘을 안 주는 상태"로 정리
            rampTime = 0f;
            chargeTime = 0f;
            chargeDir = 0f;
            lastDir = 0f;
            prevCharging = false;
            releaseGuardTimer = 0f;

            rb.linearDamping = linearDampingWhenStale;

            // ✅ 핵심: motorSpeed를 0으로 "반드시" 내려야, useMotor가 켜져있든/다른 곳에서 켜버리든 계속 안 민다.
            ApplyMotor(0f);

            if (holdWhenStale)
            {
                // 모터를 켠 채 speed=0 → 붙잡기/브레이크(미끄러짐까지 거의 제거)
                joint.useMotor = true;
            }
            else
            {
                // 모터 완전 OFF → 관성대로 코스트
                joint.useMotor = false;
            }

            return;
        }

        // ===== 여기부터는 정상(빙의 중 입력 들어오는 중) =====
        joint.useMotor = true;

        // 릴리즈 감지(차지 -> 해제)
        if (enableCharge && prevCharging && !isCharging)
        {
            if (chargeTime > 0f && Mathf.Abs(chargeDir) > 0f)
            {
                float t01 = Mathf.Clamp01(chargeTime / maxChargeSeconds);
                float relSpeed = Mathf.Lerp(baseSpeed, maxSpeed, t01) * releaseSpeedMultiplier;

                ApplyMotor(chargeDir * relSpeed);
                releaseGuardTimer = releaseGuardSeconds;

                // 평상시 램프와 동기화
                rampTime = chargeTime;
                lastDir = chargeDir;

                chargeTime = 0f;
                chargeDir = 0f;
            }
        }

        prevCharging = isCharging;

        // 차지 중: 누적만, 실제 구동은 멈춤
        if (enableCharge && isCharging)
        {
            rb.linearDamping = linearDampingWhenIdle;

            float dir = Mathf.Abs(inputX) > inputDeadzone ? Mathf.Sign(inputX) : 0f;

            if (dir != 0f)
            {
                if (chargeDir != 0f && dir != chargeDir)
                    chargeTime = 0f;

                chargeDir = dir;
                chargeTime = Mathf.Min(maxChargeSeconds, chargeTime + dt);
            }

            rampTime = 0f;
            ApplyMotor(0f);
            return;
        }

        bool driving = Mathf.Abs(inputX) > inputDeadzone;

        rb.linearDamping = driving ? linearDampingWhenDriving : linearDampingWhenIdle;

        if (driving)
        {
            float dir = Mathf.Sign(inputX);

            if (lastDir != 0f && dir != lastDir)
                rampTime = 0f;

            rampTime += dt;
            lastDir = dir;

            float r01 = Mathf.Clamp01(rampTime / Mathf.Max(0.0001f, secondsToMax));
            float rFine = r01 * r01;

            float speed = Mathf.Lerp(baseSpeed, maxSpeed, rFine);
            ApplyMotor(dir * speed);
        }
        else
        {
            rampTime = 0f;
            lastDir = 0f;

            if (releaseGuardTimer <= 0f)
            {
                // joint 축 방향 속도만 부드럽게 0으로(브레이크)
                float v = GetAxisVelocity();
                float t = 1f - Mathf.Exp(-brakeStrength * dt);
                float v2 = Mathf.Lerp(v, 0f, t);
                if (Mathf.Abs(v2) < stopThreshold) v2 = 0f;

                ApplyMotor(v2);
            }
        }
    }

    private void ForceStaleStop()
    {
        // 즉시 stale 상태로 만들어서 FixedUpdate에서 바로 stale 처리되게 함
        lastInputTime = -999f;

        // 즉시 "계속 미는" 현상 차단
        inputX = 0f;
        isCharging = false;
        prevCharging = false;

        rampTime = 0f;
        lastDir = 0f;

        chargeTime = 0f;
        chargeDir = 0f;

        releaseGuardTimer = 0f;

        rb.linearDamping = linearDampingWhenStale;

        // ✅ 즉시 motorSpeed=0 (제일 중요)
        ApplyMotor(0f);

        if (holdWhenStale)
        {
            joint.useMotor = true;   // speed=0으로 붙잡기
        }
        else
        {
            joint.useMotor = false;  // 모터 OFF 코스트
        }
    }

    private void ApplyMotor(float motorSpeed)
    {
        JointMotor2D m = joint.motor;
        m.motorSpeed = motorSpeed;
        joint.motor = m;
    }

    // joint.angle 기반 축 방향 속도
    private float GetAxisVelocity()
    {
        float ang = joint.angle * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized;
        return Vector2.Dot(rb.linearVelocity, axis);
    }
}