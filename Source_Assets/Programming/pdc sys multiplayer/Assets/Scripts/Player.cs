using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed;
    public PlayerControls controls;
    public Animator anim;

    public Renderer rend;

    private InputAction colorChange;
    private InputAction move;

    void OnEnable()
    {
        colorChange = controls.Player.ColorChange;
        move = controls.Player.Move;

        colorChange.Enable();
        move.Enable();
    }
    void OnDisable()
    {
        colorChange.Disable();
        move.Disable();
    }

    void Awake()
    {
        controls = new PlayerControls();
        rend = gameObject.GetComponent<Renderer>();
    }

    void Update()
    {
        anim.SetBool("Color Change", colorChange.ReadValue<float>() > 0);

            Vector3 movement = new Vector3(move.ReadValue<Vector2>().x, 0, move.ReadValue<Vector2>().y);
        transform.Translate(movement * Time.deltaTime * moveSpeed);
    }
}
