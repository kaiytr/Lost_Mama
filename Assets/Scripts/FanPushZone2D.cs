using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FanPushZone2D : MonoBehaviour
{
    [Header("Who can be pushed")]
    [SerializeField] private LayerMask targetMask;

    [Header("Push direction (if null, uses transform.up)")]
    [SerializeField] private Transform pushDirection;

    [Header("Push feel (mass-independent)")]
    [SerializeField] private float acceleration = 8f; // m/s^2 : 매초 이만큼 속도가 증가(천천히 밀림)
    [SerializeField] private float maxSpeedAlongDir = 6f; // dir 방향 최대 속도(캡)

    [Header("Optional")]
    [SerializeField] private bool cancelOppositeVelocity = true; // dir 반대 성분(역풍) 제거
    [SerializeField] private float radiusForFalloff = 0f; // 0이면 감쇠 없음. >0이면 중심에서 멀수록 약해짐

    // ✅ 콜라이더가 여러 개여도 안전하게: rb별로 "존 내부 콜라이더 개수"를 카운트
    private readonly Dictionary<Rigidbody2D, int> insideCount = new();

    private Vector2 Dir => (pushDirection ? (Vector2)pushDirection.up : (Vector2)transform.up).normalized;

    private void Reset()
    {
        // 기본적으로 이 스크립트는 Trigger Zone 용도
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private bool IsTarget(Collider2D col)
        => ((1 << col.gameObject.layer) & targetMask) != 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTarget(other)) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        insideCount.TryGetValue(rb, out int c);
        insideCount[rb] = c + 1;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        if (!insideCount.TryGetValue(rb, out int c)) return;

        c -= 1;
        if (c <= 0) insideCount.Remove(rb);
        else insideCount[rb] = c;
    }

    private void FixedUpdate()
    {
        if (insideCount.Count == 0) return;

        Vector2 dir = Dir;
        float dt = Time.fixedDeltaTime;

        // foreach 도중 Remove가 일어날 수 있으니 null 정리용 리스트
        List<Rigidbody2D> toRemove = null;

        foreach (var kv in insideCount)
        {
            var rb = kv.Key;
            if (rb == null)
            {
                (toRemove ??= new List<Rigidbody2D>()).Add(rb);
                continue;
            }

            // 거리 감쇠(선택)
            float falloff = 1f;
            if (radiusForFalloff > 0f)
            {
                float dist = Vector2.Distance(rb.worldCenterOfMass, (Vector2)transform.position);
                falloff = Mathf.Clamp01(1f - (dist / radiusForFalloff));
                if (falloff <= 0f) continue;
            }

            Vector2 v = rb.linearVelocity;

            float along = Vector2.Dot(v, dir);

            // 역풍 성분 제거(선택)
            if (cancelOppositeVelocity && along < 0f)
            {
                v -= dir * along; // along이 음수라서 빼면 반대 성분이 사라짐
                along = 0f;
            }

            // "천천히 밀기": dir 방향 속도를 acceleration * dt 만큼 증가시키되, 최대치로 캡
            float newAlong = Mathf.Min(along + acceleration * falloff * dt, maxSpeedAlongDir);

            // dir축 성분만 원하는 값으로 맞추기(옆속도는 유지)
            v += dir * (newAlong - along);

            rb.linearVelocity = v;
            rb.WakeUp();
        }

        if (toRemove != null)
        {
            foreach (var rb in toRemove) insideCount.Remove(rb);
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 dir = (pushDirection ? (Vector2)pushDirection.up : (Vector2)transform.up).normalized;
        Vector3 p = transform.position;
        UnityEngine.Gizmos.DrawLine(p, p + (Vector3)dir * 2f);
    }
#endif
}