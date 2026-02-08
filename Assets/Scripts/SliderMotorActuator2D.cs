using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SliderJoint2D))]
public class SliderMotorActuator2D : MonoBehaviour, IActuator, IChargeInputReceiver
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

    [Header("Charge / Release (optional)")]
    [SerializeField] private bool enableCharge = true;
    [SerializeField] private float maxChargeSeconds = 2.0f;
    [SerializeField] private float releaseSpeedMultiplier = 1.8f;
    [SerializeField] private float releaseGuardSeconds = 0.12f;

    private float inputX;
    private float rampTime;
    private float lastDir;

    private bool isCharging;
    private bool prevCharging;
    private float chargeTime;
    private float chargeDir;

    private float releaseGuardTimer;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!joint) joint = GetComponent<SliderJoint2D>();

        // 기본적으로 joint motor를 스크립트가 제어한다고 가정
        joint.useMotor = true;
    }

    // PossessionHub(PossessableObject)에서 호출됨
    public void SetInput(float x)
    {
        inputX = Mathf.Clamp(-x, -1f, 1f);
    }

    // 스페이스 홀드/릴리즈 전달받음
    public void SetCharging(bool charging)
    {
        isCharging = charging;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (releaseGuardTimer > 0f) releaseGuardTimer -= dt;

        // 릴리즈 감지(차지 -> 해제 순간)
        if (enableCharge && prevCharging && !isCharging)
        {
            if (chargeTime > 0f && Mathf.Abs(chargeDir) > 0f)
            {
                float t01 = Mathf.Clamp01(chargeTime / maxChargeSeconds);
                float relSpeed = Mathf.Lerp(baseSpeed, maxSpeed, t01) * releaseSpeedMultiplier;

                ApplyMotor(chargeDir * relSpeed);
                releaseGuardTimer = releaseGuardSeconds;

                // 평상시 램프와 동기화(원하면 유지)
                rampTime = chargeTime;
                lastDir = chargeDir;

                chargeTime = 0f;
                chargeDir = 0f;
            }
        }

        prevCharging = isCharging;

        // 차지 중: 입력 방향만 기억하고 시간 누적, 실제 구동은 멈춤(또는 아주 약하게)
        if (enableCharge && isCharging)
        {
            float dir = Mathf.Abs(inputX) > inputDeadzone ? Mathf.Sign(inputX) : 0f;

            if (dir != 0f)
            {
                // 방향 바뀌면 차지 리셋(원하면 옵션화 가능)
                if (chargeDir != 0f && dir != chargeDir)
                    chargeTime = 0f;

                chargeDir = dir;
                chargeTime = Mathf.Min(maxChargeSeconds, chargeTime + dt);
            }

            rampTime = 0f; // 차지 중에는 평상시 가속 누적 끄기
            ApplyMotor(0f); // 차지 중에는 정지(도르레 “당기는 중” 느낌)
            return;
        }

        bool driving = Mathf.Abs(inputX) > inputDeadzone;

        if (driving)
        {
            float dir = Mathf.Sign(inputX);

            // 방향 바뀌면 램프 리셋
            if (lastDir != 0f && dir != lastDir)
                rampTime = 0f;

            rampTime += dt;
            lastDir = dir;

            float r01 = Mathf.Clamp01(rampTime / Mathf.Max(0.0001f, secondsToMax));
            float rFine = r01 * r01;

            float speed = Mathf.Lerp(baseSpeed, maxSpeed, rFine);
            ApplyMotor(dir * speed);

            // 릴리즈 직후 잠깐은 스크립트가 “브레이크/클램프” 건드리지 않게
            // (SliderJoint2D limits가 알아서 막아줌)
        }
        else
        {
            rampTime = 0f;
            lastDir = 0f;

            if (releaseGuardTimer <= 0f)
            {
                // joint 축 방향 속도만 부드럽게 0으로
                float v = GetAxisVelocity();
                float t = 1f - Mathf.Exp(-brakeStrength * dt);
                float v2 = Mathf.Lerp(v, 0f, t);
                if (Mathf.Abs(v2) < stopThreshold) v2 = 0f;

                ApplyMotor(v2);
            }
        }
    }

    private void ApplyMotor(float motorSpeed)
    {
        // SliderJoint2D 모터는 "Motor Speed"로 축 방향 속도를 만든다
        JointMotor2D m = joint.motor;
        m.motorSpeed = motorSpeed;
        joint.motor = m;
    }

    // joint의 이동축 방향 속도를 추정(축 기준은 joint.angle)
    private float GetAxisVelocity()
    {
        // joint.angle 0=오른쪽(월드 x), 90=위쪽(월드 y)
        float ang = joint.angle * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized;
        return Vector2.Dot(rb.linearVelocity, axis);
    }
}