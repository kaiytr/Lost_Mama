using UnityEngine;

public class CPU : MonoBehaviour
{
    // Unity Editor 렉걸림 방지를 위한 코드
    void Awake() { Application.targetFrameRate = 144; } // 또는 64
}
