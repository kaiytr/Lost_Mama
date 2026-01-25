using UnityEngine;

public class PossessableObject : MonoBehaviour
{
    private IActuator[] actuators;

    private void Awake()
    {
        actuators = GetComponentsInChildren<IActuator>(); // 자식까지 포함(팔이 자식일 때 편함)
    }

    public void Handle(float x)
    {
        for (int i = 0; i < actuators.Length; i++)
            actuators[i].SetInput(x);
    }
}
