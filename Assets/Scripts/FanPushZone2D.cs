using System.Collections.Generic;
using UnityEngine;

public class FanPushZone2D : MonoBehaviour
{
    [Header("Zone (Wind Trigger Collider)")]
    [SerializeField] private Collider2D zoneCollider;   // ✅ 바람 범위 트리거 콜라이더
    [SerializeField] private Transform windOrigin;      // 비우면 transform.position

    [Header("Push target (who gets pushed)")]
    [SerializeField] private LayerMask targetMask;      // ✅ Player만

    [Header("Push direction")]
    [SerializeField] private Transform pushDirection;   // 비우면 transform.up

    [Header("Push feel")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float maxSpeedAlongDir = 6f;
    [SerializeField] private bool cancelOppositeVelocity = true;

    [Header("Occlusion (block wind)")]
    [SerializeField] private bool blockByObjects = true;

    [Tooltip("Layers that CAN block wind. Usually Everything.")]
    [SerializeField] private LayerMask blockerMask = ~0; // ✅ Everything 추천

    [Tooltip("Layers that should NOT block wind. (e.g. Player)")]
    [SerializeField] private LayerMask blockIgnoreMask;  // ✅ Player 넣기

    [Tooltip("If ON, only hits whose hit point is inside zone will block.")]
    [SerializeField] private bool onlyBlockInsideZone = true;

    [Tooltip("Ignore trigger colliders as blockers (recommended).")]
    [SerializeField] private bool ignoreTriggerBlockers = true;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;

    private readonly HashSet<Rigidbody2D> inside = new();

    // 스캔 버퍼(NonAlloc)
    private readonly Collider2D[] overlapBuffer = new Collider2D[64];

    // ✅ readonly 제거 (여기만 바뀜)
    private ContactFilter2D targetFilter;

    private static readonly RaycastHit2D[] rayHits = new RaycastHit2D[32];

    private Vector2 Dir => (pushDirection ? (Vector2)pushDirection.up : (Vector2)transform.up).normalized;
    private Vector2 Origin => windOrigin ? (Vector2)windOrigin.position : (Vector2)transform.position;

    private void Awake()
    {
        if (!zoneCollider)
        {
            // 트리거 우선 탐색
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols)
            {
                if (c && c.isTrigger) { zoneCollider = c; break; }
            }
            if (!zoneCollider && cols.Length > 0) zoneCollider = cols[0];
        }

        // ✅ 필터 초기화(여기서 설정)
        targetFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = targetMask,
            useTriggers = true
        };
    }

    private void OnEnable()
    {
        inside.Clear();
    }

    private void OnDisable()
    {
        inside.Clear();
    }

    private void FixedUpdate()
    {
        if (!zoneCollider) return;

        // ✅ 매 FixedUpdate마다 현재 존 안 타겟 재구성(존 밖이면 즉시 빠짐)
        RebuildInsideSet();

        if (inside.Count == 0) return;

        Vector2 dir = Dir;
        Vector2 origin = Origin;
        float dt = Time.fixedDeltaTime;

        foreach (var rb in inside)
        {
            if (!rb) continue;

            if (blockByObjects && IsBlocked(origin, rb))
                continue;

            Vector2 v = rb.linearVelocity;
            float along = Vector2.Dot(v, dir);

            if (cancelOppositeVelocity && along < 0f)
            {
                v -= dir * along;
                along = 0f;
            }

            float newAlong = Mathf.Min(along + acceleration * dt, maxSpeedAlongDir);
            v += dir * (newAlong - along);

            rb.linearVelocity = v;
            rb.WakeUp();

            if (debugDraw)
                Debug.DrawLine(origin, rb.worldCenterOfMass, Color.white, 0.02f);
        }
    }

    private void RebuildInsideSet()
    {
        inside.Clear();

        // targetMask가 런타임에 바뀔 수 있으면 아래 한 줄로 갱신해도 됨
        // targetFilter.layerMask = targetMask;

        int n = zoneCollider.Overlap(targetFilter, overlapBuffer);
        for (int i = 0; i < n; i++)
        {
            var col = overlapBuffer[i];
            if (!col) continue;

            var rb = col.attachedRigidbody;
            if (!rb) continue;

            // 콜라이더/리지드바디 레이어 꼬임 방지
            int colLayer = col.gameObject.layer;
            int rbLayer = rb.gameObject.layer;
            if (!IsInMask(colLayer, targetMask) && !IsInMask(rbLayer, targetMask))
                continue;

            inside.Add(rb);
        }
    }

    private bool IsBlocked(Vector2 origin, Rigidbody2D targetRb)
    {
        Vector2 targetPoint = targetRb.worldCenterOfMass;
        Vector2 to = targetPoint - origin;
        float dist = to.magnitude;
        if (dist < 0.001f) return false;

        Vector2 d = to / dist;

        int count = Physics2D.RaycastNonAlloc(origin, d, rayHits, dist, blockerMask.value);
        for (int i = 0; i < count; i++)
        {
            var h = rayHits[i];
            var col = h.collider;
            if (!col) continue;

            if (ignoreTriggerBlockers && col.isTrigger) continue;                 // 트리거 제외
            if (IsInMask(col.gameObject.layer, blockIgnoreMask)) continue;       // Player 등 제외
            if (col.transform.IsChildOf(transform)) continue;                    // 팬 자기 자신 제외
            if (h.rigidbody == targetRb) continue;                               // 목표 자신 제외

            if (onlyBlockInsideZone && !zoneCollider.OverlapPoint(h.point))      // 바람 존 안에서만 차단
                continue;

            if (debugDraw)
                Debug.DrawLine(origin, h.point, Color.red, 0.05f);

            return true;
        }

        return false;
    }

    private static bool IsInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}