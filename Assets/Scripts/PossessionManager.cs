using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PossessionManager : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask possessMask;

    private PossessableObject current;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);

            // ✅ 전부 맞춰놓고 조건에 맞는 첫 대상 선택
            var hits = Physics2D.GetRayIntersectionAll(ray, 200f, possessMask);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    var col = hits[i].collider;
                    if (col == null) continue;

                    var p = ResolvePossessTarget(col);
                    if (p == null) continue;

                    if (p != current)
                    {
                        if (current != null) current.SetPossessed(false);
                        current = p;
                        current.SetPossessed(true);
                    }
                    break;
                }
            }
        }

        float x = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;

        bool charging = kb.spaceKey.isPressed;

        if (current != null)
        {
            current.Handle(x);
            current.HandleCharging(charging);
        }
    }

    private PossessableObject ResolvePossessTarget(Collider2D clickedCol)
    {
        if (clickedCol == null) return null;

        // 1) 같은 오브젝트에 PossessableObject가 있으면 그걸 우선
        var direct = clickedCol.GetComponent<PossessableObject>();
        if (direct != null)
            return direct.IsColliderSelectable(clickedCol) ? direct : null;

        // 2) 부모에서 PossessableObject 찾기
        var parent = clickedCol.GetComponentInParent<PossessableObject>();
        if (parent == null) return null;

        return parent.IsColliderSelectable(clickedCol) ? parent : null;
    }
}