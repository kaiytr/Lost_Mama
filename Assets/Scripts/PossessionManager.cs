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

        // 클릭으로 대상 선택
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 200f, possessMask);

            if (hit.collider != null)
            {
                var p = hit.collider.GetComponentInParent<PossessableObject>();
                if (p != null && p != current)
                {
                    if (current != null) current.SetPossessed(false);
                    current = p;
                    current.SetPossessed(true);
                }
            }
        }

        // A/D 입력
        float x = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;

        // Space 차지 입력
        bool charging = kb.spaceKey.isPressed;

        if (current != null)
        {
            current.Handle(x);
            current.HandleCharging(charging);
        }
    }
}
