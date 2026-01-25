using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateTorqueActuator2D : MonoBehaviour, IActuator
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float torque = 800f;
    [SerializeField] private float maxAngVel = 360f;

    [Header("Idle Brake")]
    [Tooltip("입력 없을 때 각속도를 0으로 얼마나 빨리 끌어내릴지(클수록 빨리 멈춤)")]
    [SerializeField] private float brakeStrength = 12f; // 8~25 추천

    [Tooltip("이 이하 각속도면 완전히 멈춘 걸로 처리")]
    [SerializeField] private float stopThreshold = 2f;  // deg/sec

    private float inputX;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void SetInput(float x) => inputX = x;

    private void FixedUpdate()
    {
        if (Mathf.Abs(inputX) > 0.001f)
        {
            // 조작 중: 기존 손맛 유지
            rb.AddTorque(-inputX * torque, ForceMode2D.Force); // dt 곱 빼는 게 보통 더 안정적
            rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxAngVel, maxAngVel);
        }
        else
        {
            // ✅ 입력 없을 때: 관성 감속(브레이크)
            float av = rb.angularVelocity;

            // 지수 감쇠(프레임/고정델타에 독립적으로 자연스럽게 줄어듦)
            float t = 1f - Mathf.Exp(-brakeStrength * Time.fixedDeltaTime);
            av = Mathf.Lerp(av, 0f, t);

            if (Mathf.Abs(av) < stopThreshold) av = 0f;
            rb.angularVelocity = av;
        }
    }
}
