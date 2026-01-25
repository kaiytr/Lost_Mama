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
        // 입력 장치가 없으면(빌드/환경) 방어
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        // 1) 클릭으로 대상 선택
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 200f, possessMask);

            if (hit.collider != null)
            {
                var p = hit.collider.GetComponentInParent<PossessableObject>();
                if (p != null) current = p;
            }
        }

        // 2) A/D 입력 축 만들기 (-1 ~ +1)
        float x = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;

        // 3) 현재 빙의 대상에만 입력 전달
        if (current != null)
            current.Handle(x);
    }
}
