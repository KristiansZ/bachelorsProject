using UnityEngine;
using KinematicCharacterController.Examples;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    
    [Header("Components")]
    public PlayerStatManager Stats;
    public PlayerDamageHandler DamageHandler;
    public PlayerLeveling Leveling;
    public PlayerController PlayerController;
    public Transform PlayerTransform;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PlayerTransform = transform;
            
            if (!Stats) Stats = GetComponent<PlayerStatManager>();
            if (!DamageHandler) DamageHandler = GetComponent<PlayerDamageHandler>();
            if (!Leveling) Leveling = GetComponent<PlayerLeveling>();
            if (!PlayerController) PlayerController = GetComponent<PlayerController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}