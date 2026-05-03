using UnityEngine;

public enum Model_Type
{
    Chaff = 0,
    Specialist_A= 1,
    Specialist_B = 2,
    Axillary = 3,
    DeathHead = 4
}

public class Model_Standard_Behavior : MonoBehaviour
{
    public int Team;
    public int Current_X;
    public int Current_Y;
    public Model_Type Type;

    // Smooth model transitioning
    private Vector3 Desired_Position;
    private Vector3 Desired_Scale;
}
