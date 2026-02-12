using System.Collections.Generic;
using UnityEngine;

public class AutoBouncePad2D : MonoBehaviour
{
    [Header("Who can trigger")]
    [SerializeField] private LayerMask targetMask;

    [Header("Launch Direction")]
    [SerializeField] private Transform launchDirection; // 비우면 transform.up 사용

    [Header("Launch (mass=0.0001 friendly)")]
    [SerializeField] private float launchSpeed = 12f; // ✅ 질량과 무관하게 일정하게 튀는 속도(m/s)

    [Header("Safety / Feel")]
    [SerializeField] private float perTargetCooldown = 0.15f;  // 같은 대상 연타 방지
    [SerializeField] private float minApproachSpeed = 0.1f;    // 내려오면서 밟을 때만 발동(0이면 항상 발동)
    [SerializeField] private bool removeAxisVelocityBeforeLaunch = true; // 발사 방향 성분 속도 제거(옆속도는 유지)

    private readonly Dictionary<Rigidbody2D, float > lastLaunchTime = new();

    private Vector2 Dir => (launchDirection ? (Vector2)launchDirection.up : (Vector2)transform.up).normalized;

    // Trigger 방식(감지용 콜라이더 IsTrigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTarget(other)) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        TryLaunch(rb);
    }

    // Collision 방식(바닥 콜라이더로만 감지할 때)
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsTarget(col.collider)) return;
        var rb = col.rigidbody;
        if (rb == null) return;

        TryLaunch(rb);
    }

    private bool IsTarget(Collider2D col)
    {
        return ((1 << col.gameObject.layer) & targetMask) != 0;
    }

    private void TryLaunch(Rigidbody2D rb)
    {
        float now = Time.time;

        // ✅ 같은 대상 연타 방지 (Trigger+Collision이 둘 다 불려도 여기서 막힘)
        if (lastLaunchTime.TryGetValue(rb, out float last) && now - last < perTargetCooldown)
            return;

        Vector2 dir = Dir;

        // ✅ "밟는" 상황만 발동: dir이 위면(-dir) 아래로 내려오고 있어야 함
        if (minApproachSpeed > 0f)
        {
            float approach = Vector2.Dot(rb.linearVelocity, -dir);
            if (approach < minApproachSpeed) return;
        }

        // ✅ 발사 방향 성분 속도만 제거(위로 튀기 전에 기존 위/아래 속도 제거)
        if (removeAxisVelocityBeforeLaunch)
        {
            float along = Vector2.Dot(rb.linearVelocity, dir);
            rb.linearVelocity -= dir * along;
            // Debug.Log("[AutoBouncePad2D] Removed launch-axis velocity: " + along);
        }

        // ✅ mass=0.0001이어도 안정적인 방식: "속도"를 직접 부여
        rb.linearVelocity += dir * launchSpeed;

        lastLaunchTime[rb] = now;
    }
}