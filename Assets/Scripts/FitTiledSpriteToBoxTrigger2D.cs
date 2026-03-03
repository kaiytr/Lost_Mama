using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FitTiledSpriteToBoxTrigger2D : MonoBehaviour
{
    [SerializeField] private BoxCollider2D targetCollider;
    [SerializeField] private SpriteRenderer sr;

    private void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!targetCollider) targetCollider = GetComponentInParent<BoxCollider2D>();
    }

    private void OnValidate() => Apply();
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) Apply();
#endif
    }

    private void Apply()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!targetCollider || !sr) return;

        // 타일 반복
        sr.drawMode = SpriteDrawMode.Tiled;

        // 콜라이더 로컬 사이즈/오프셋에 맞추기
        sr.size = targetCollider.size;
        transform.localPosition = (Vector3)targetCollider.offset;

        // 스케일 꼬임 방지(가능하면 부모/자식 스케일을 1로 유지하는 게 베스트)
        if (transform.localScale != Vector3.one)
            transform.localScale = Vector3.one;
    }
}