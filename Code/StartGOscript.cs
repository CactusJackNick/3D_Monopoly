using Code;
using UnityEngine;

public class StartGOscript : MonoBehaviour
{
    private Player _player;
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider belongs to a GameObject tagged "Player"
        if (other.CompareTag("Player"))
        {
            // Find the Player script on the parent GameObject
            Player player = other.GetComponent<Player>(); // Directly check if the collider object has the Player script
            if (player == null)
                player = other.GetComponentInParent<Player>();

            // If a Player script is found, add money
            if (player != null && !player.preventGoBonus)
            {
                player.money += 200;
                Debug.Log($"Player received $200! Total money: {player.money}");
            }
            player.preventGoBonus = false;
            PlayerManager.Instance.UpdatePlayerUI();
        }
    }
}
