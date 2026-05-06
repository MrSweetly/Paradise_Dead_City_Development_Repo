using UnityEngine;

[CreateAssetMenu(fileName = "New Model Stats", menuName = "Scriptable Objects/Model Stats")]
public class Model_Stats_SO : ScriptableObject
{
    [Header("Model Name")]
    public string Model_Name;
    public Model_Type Type;

    [Header("Model Stats")]
    public int Health = 2;
    public int Attack_Damage = 1;
    public int Attack_Ranged = 1;
    [Range(0f, 1f)] public float Attack_Chance = 0.75f;

    [Header("Model Movement")]
    public int Movement_Range = 2;

    [Header("Model Special Rules")]
    public bool Is_Ranged = false;
    public bool Has_Splash_Damage = false;

    [Header("Model Special Abilities")]
    public bool Has_Ability = false;

    // Is in movement range?
    public bool Is_Witin_Movement_Range(int Current_X, int Current_Y, int Target_X, int Target_Y)
    {
        int Distance = Mathf.Abs(Target_X - Current_X) + Mathf.Abs(Target_Y - Current_Y);
        return Distance <= Movement_Range;
    }

}
