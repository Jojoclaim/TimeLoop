using UnityEngine;

public class PlayerAttackDebugGUI : MonoBehaviour
{
    private PlayerAttack playerAttack;

    private void Awake()
    {
        if (playerAttack == null)
        {
            Debug.LogWarning("PlayerAttack not found! Debug GUI disabled.");
            enabled = false;
        }
    }

    private void OnValidate()
    {
        playerAttack = playerAttack != null ? playerAttack : GetComponent<PlayerAttack>();
    }

    private void OnGUI()
    {
        if (playerAttack == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label($"Charging: {playerAttack.IsCharging}");
        GUILayout.Label($"Charge Progress: {playerAttack.ChargeProgress:P0}");
        GUILayout.Label($"Current Damage: {playerAttack.CurrentDamage:F1}");
        GUILayout.EndArea();
    }
}