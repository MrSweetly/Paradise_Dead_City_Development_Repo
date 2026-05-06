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

    // Stats reference (assigned at spawn)
    public Model_Stats_SO Stats;

    // Current state
    public int Current_Health;
    public bool Has_Moved_This_Turn;
    public bool Has_Attacked_This_Turn;

    // Smooth model transitioning
    private Vector3 Desired_Position;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, Desired_Position, Time.deltaTime * 10f);
    }

    public virtual void Set_Position(Vector3 Position, bool Force =  false)
    {
        Desired_Position = Position;
        if (Force)
            transform.position = Desired_Position;
    }

    // Check if this model can move to a target tile
    public bool Can_Move_To(int Target_X, int Target_Y)
    {
        if (Has_Moved_This_Turn)
            return false;

        if (Stats == null)
            return true; // No stats = free movement (backward compatibility)

        return Stats.Is_Witin_Movement_Range(Current_X, Current_Y, Target_X, Target_Y);
    }
}
