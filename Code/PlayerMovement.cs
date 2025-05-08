using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public bool _isMoving = false;
    private PlayerManager _playerManager;
    
    //public Transform playerBody; // Assign the physical player model here
    //public Animator playerAnimator; // Assign the Animator controlling the player's animations
    
    private Animator _animator;
    
    private void Awake()
    {
        // Get the Animator component
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator component not found on the player!");
        }
    }

    public IEnumerator MovePlayer(Transform player, List<Transform> waypoints, int currentPosition, int diceSum, System.Action onMovementComplete)
    {
        _isMoving = true;
        
        int playerIndex = PlayerManager.Instance.playersTransforms.IndexOf(player); // Get the player's index
        Animator playerAnimator = PlayerManager.Instance.playersAnimators[playerIndex]; // Get their animator
        
        // Trigger the movement animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isMoving", true);
        }

        int totalWaypoints = waypoints.Count;

        int steps = diceSum > 0 ? 1 : -1; // Determine movement direction based on diceSum

        // Loop through all steps
        for (int i = 0; i != diceSum; i += steps)
        {
            // Calculate the next waypoint index
            int nextWaypointIndex = (currentPosition + i + steps + totalWaypoints) % totalWaypoints;
            Transform targetWaypoint = waypoints[nextWaypointIndex];

            // Move to the next waypoint
            yield return StartCoroutine(MoveToTarget(player, targetWaypoint));
        }

        // Update the player's final position after movement
        _isMoving = false;
        onMovementComplete?.Invoke();
        
        // Stop the movement animation
        if (_isMoving == false)
        {
            playerAnimator.SetBool("isMoving", false);
        }
    }

    private IEnumerator MoveToTarget(Transform player, Transform target)
    {
        float speed = 10f;
        float rotationSpeed = 10f;

        while (Vector3.Distance(player.position, target.position) > 0.1f)
        {
            // Move the player towards the waypoint
            player.position = Vector3.MoveTowards(player.position, target.position, speed * Time.deltaTime);

            // Rotate smoothly towards the waypoint
            Vector3 direction = (target.position - player.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            player.rotation = Quaternion.Slerp(player.rotation, lookRotation, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        // Snap to exact position
        player.position = target.position;
    }
    
}
