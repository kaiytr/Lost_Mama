using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateTorqueActuator2D : MonoBehaviour, IActuator
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float torque = 800f;
    [SerializeField] private float maxAngVel = 360f;

    private float inputX;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    public void SetInput(float x) => inputX = x;

    private void FixedUpdate()
    {
        rb.AddTorque(-inputX * torque * Time.fixedDeltaTime, ForceMode2D.Force);
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxAngVel, maxAngVel);
    }
}
