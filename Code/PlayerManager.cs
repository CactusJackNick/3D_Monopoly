using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Code
{
    public class PlayerManager : MonoBehaviour
    {    
        public static PlayerManager Instance { get; private set; }
        [Header("Player's UI elements")]
        public Text playerTurnText;
        public Text playerMoneyText;
        public Text rentPopupText;
        public Text playerTriesText;
    
        [Header("Player Related Information")]
        public List<Player> players; //List of players
        public List<Transform> playersTransforms; // List of all player transforms  ALSO //List of player physical models
        public List<Transform> waypointParents; // Parent objects containing waypoints for each player
    
        public List<List<Transform>> _playerWaypoints; // Extracted waypoint lists for each player
        public Dictionary<Transform, int> _playerPositions; // Tracks each player's position in their waypoint list
        public int currentPlayerIndex; // The player whose turn it is
    
        [Header("UI Buttons")]
        public Button rollButton; // Reference to the roll button
        public Button terminateButton; // Reference to the terminate button
        public Button payJailFeeButton; // Reference to the pay fee to free the player from Jail
        [Header("Managers References")]
        public CameraManager cameraManager; // Reference to the CameraManager

        [Header("References for the Animator")]
        public List<Animator> playersAnimators; // List of player objects
    
        private DiceManager _diceManager; 
        private readonly bool _isMoving = false; // Prevents multiple moves at once
    
        private PlayerState _state;
        private Player _player;
        private PlayerMovement playerMovement; // Reference to PlayerMovement script
    
        private readonly Dictionary<int, int> _playerDiceRolls = new Dictionary<int, int>(); // Store each player's dice roll
    
    
        /* THIS SCRIPT IS RESPONSIBLE FOR MANAGING PLAYER TURNS,
    JAIL ACTIONS AND PROPERTY OWNERSHIP */
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    
        void Start()
        {
            UpdatePlayerUI();
            _diceManager = FindFirstObjectByType<DiceManager>(); // Automatically find the DiceManager in the scene
            playerMovement = GetComponent<PlayerMovement>(); // Get the PlayerMovement component
            StartTurn();

            // Extract waypoints from parent objects and initialize positions
            _playerWaypoints = new List<List<Transform>>();
            foreach (Transform parent in waypointParents)
            {
                List<Transform> waypoints = new List<Transform>();
                foreach (Transform child in parent)
                {
                    waypoints.Add(child);
                }

                _playerWaypoints.Add(waypoints);
            }

            // Initialize player positions
            _playerPositions = new Dictionary<Transform, int>();
            foreach (var player in playersTransforms)
            {
                _playerPositions[player] = 0; // Start each player at their first waypoint
            }
        
            for (int i = 0; i < players.Count; i++)
            {
                // Assign the animator for each player body
                Animator animator = playersTransforms[i].GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    playersAnimators.Add(animator);
                }
                else
                {
                    Debug.LogError($"Animator missing on player body {playersTransforms[i].name}");
                }
            }

            StartCoroutine(URLParameters.Instance.FetchUserData());
        }

        public void StartPlayerMovement(int diceSum)
        {
            if (_isMoving) return; // Prevent multiple movements

            Player currentPlayerTurn = players[currentPlayerIndex];
            Transform currentPlayerTransform = playersTransforms[currentPlayerIndex];

            // Store this player's dice roll
            _playerDiceRolls[currentPlayerIndex] = diceSum;

            Transform playerCameraTarget = currentPlayerTransform.Find("PlayerTargetCamera");
            if (cameraManager != null) cameraManager.SwitchToPlayer(playerCameraTarget);

            // If the player is in jail, handle jail logic
            if (currentPlayerTurn.state == PlayerState.InJail)
            {
                HandleJailTurns(currentPlayerTurn);
                if (currentPlayerTurn.IsInJail) return; // If still in jail, skip movement
            }

            // Normal movement case
            rollButton.gameObject.SetActive(false);
            List<Transform> waypoints = _playerWaypoints[currentPlayerIndex];
            int currentPos = _playerPositions[currentPlayerTransform];

            StartCoroutine(playerMovement.MovePlayer(
                currentPlayerTransform,
                waypoints,
                currentPos,
                diceSum,
                () => OnMovementComplete(currentPlayerTransform, (currentPos + diceSum) % waypoints.Count)
            ));

            terminateButton.gameObject.SetActive(false);
        }

        private void OnMovementComplete(Transform player, int finalPosition)
        {
            _playerPositions[player] = finalPosition; // Update the player's position
            terminateButton.gameObject.SetActive(true);
            _diceManager.DestroyUnusedDice(); // After movement remove clones of dice
            
            // 🔹 If the player lands on "Go To Jail" (assuming tile index is 30)
            if (finalPosition == 30)
            {
                Debug.Log($"{player.name} landed on 'Go To Jail'. Moving to jail...");
                StartCoroutine(MovePlayerToJail(player));
            }
        }
        
        public IEnumerator MovePlayerToJail(Transform playerTransform)
        {
            Player player = playerTransform.GetComponent<Player>();
            List<Transform> waypoints = _playerWaypoints[currentPlayerIndex];

            int jailTileIndex = 10; // Jail tile index
            int currentPos = _playerPositions[playerTransform];

            // Move player to the Jail waypoint using the same "Move To Tile" logic
            int tilesToMove = (jailTileIndex - currentPos + waypoints.Count) % waypoints.Count;
            
            yield return new WaitForSeconds(1f);
            StartCoroutine(ShowRentPopup($"Moving {player.playerName} to jail..."));
            Debug.Log("Moving to jail...");
            
            yield return StartCoroutine(playerMovement.MovePlayer(
                playerTransform,
                waypoints,
                currentPos,
                tilesToMove,
                () => 
                {
                    _playerPositions[playerTransform] = jailTileIndex;
                    player.GoToJail(); // Apply jail effect
                }
            ));
        }



        // THIS SECTION HANDLES JAIL STATES AND ACTIONS
        // THAT THE PLAYERS CAN DO WHEN JAILED AS WELL AS UI BUTTONS
        private void CheckPlayerStates()
        {
            foreach (Player player in players)
            {
                if (player.state == PlayerState.InJail)
                {
                    player.EnterJail();
                }
                else
                {
                    player.state = PlayerState.Playing;
                }
            }
        }

        private void HandleJailTurns(Player player)
        {
            int diceSum = _playerDiceRolls[currentPlayerIndex]; // Get stored roll
            _diceManager.DestroyUnusedDice(); // Remove dice visuals

            if (player.getOutOfJailFreeCards > 0)
            {
                player.getOutOfJailFreeCards--;
                player.LeaveJail();
                StartPlayerMovement(diceSum);
                return;
            }
        
            // Show the pay button **only if the player has enough money**
            if (player.money >= player.jailFee)
            {
                payJailFeeButton.gameObject.SetActive(true);
                payJailFeeButton.onClick.RemoveAllListeners();
                payJailFeeButton.onClick.AddListener(() => PayJailFee(player));
            }
    
            // Check if they rolled doubles
            if (_diceManager.RolledDoubles())
            {
                player.LeaveJail();
                Debug.Log($"Player {currentPlayerIndex + 1} rolled doubles and left jail.");
                StartPlayerMovement(diceSum); // Move the player
            }
            else if (player.jailTurns <= 1) // If it's their last turn, they leave jail automatically
            {
                player.LeaveJail();
                Debug.Log($"Player {currentPlayerIndex + 1} served their time and left jail.");
                StartPlayerMovement(diceSum);
            }
            else
            {
                player.jailTurns--; // Reduce jail turns
                Debug.Log($"Player {currentPlayerIndex + 1} stays in jail for {player.jailTurns} more turns.");
                EndTurn(); // End turn WITHOUT movement
            }
        }
        private IEnumerator WaitForDiceToRoll()
        {
            yield return new WaitForSeconds(0.5f);
            _diceManager.RollDice();
        }
    
        // When the player chooses to pay, they leave jail and roll
        private void PayJailFee(Player player)
        {
            player.money -= player.jailFee;
            player.LeaveJail();
            payJailFeeButton.gameObject.SetActive(false); // Hide pay button
            Debug.Log($"Player {currentPlayerIndex + 1} paid the jail fee and left jail.");
            StartCoroutine(WaitForDiceToRoll()); // Proceed to rolling normally
            UpdatePlayerUI();
        }
    
        public void TerminateTurn()
        {
            EndTurn(); // Call EndTurn to proceed to the next player's turn
            terminateButton.gameObject.SetActive(false);

            var currentPlayer = players[currentPlayerIndex];
            if (currentPlayer.state == PlayerState.InJail && currentPlayer.money >= _player.jailFee)
            {
                payJailFeeButton.gameObject.SetActive(true);
                terminateButton.gameObject.SetActive(false);
            }
            else 
                payJailFeeButton.gameObject.SetActive(false);
        }

        private void EndTurn()
        {
            Debug.Log($"Player {currentPlayerIndex + 1} has finished their turn.");
            cameraManager.SwitchToDiceCamera(); 
        
            rollButton.gameObject.SetActive(false); // Disable roll button
            terminateButton.gameObject.SetActive(false); // Disable terminate button
        
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count; // Move to the next player's turn
            StartTurn();
        }

        private void StartTurn()
        {
            UpdatePlayerUI();
            _player = players[currentPlayerIndex];
            CheckPlayerStates();

            _diceManager.SpawnDice(); // Always spawn dice for new turn
            rollButton.gameObject.SetActive(true); // Enable roll button
            terminateButton.gameObject.SetActive(true); // Enable terminate button

            // Reset dice roll for this player
            _playerDiceRolls[currentPlayerIndex] = 0;

            // If in jail, they still roll, but movement depends on the jail logic
            if (_player.state == PlayerState.InJail)
            {
                Debug.Log($"Player {currentPlayerIndex + 1} is in jail. They must roll to attempt an escape.");
            }
            if(!_player.IsInJail)
                payJailFeeButton.gameObject.SetActive(false);
        }

        public void UpdatePlayerUI()
        {
            if (players.Count > 0)
            {
                Player currentPlayer = players[currentPlayerIndex];
            
                playerTurnText.text = currentPlayer.playerName;
                playerMoneyText.text = "$" + currentPlayer.money.ToString();
                playerTriesText.text = "Tries Left: " + currentPlayer.diceTriesLeft.ToString();
            }
        }

        public IEnumerator ShowRentPopup(string message) // New Coroutine to Show Rent Payment Info
        {
            rentPopupText.text = message;
            rentPopupText.gameObject.SetActive(true);

            yield return new WaitForSeconds(3f); // Show for 3 seconds

            rentPopupText.gameObject.SetActive(false);
        }
        
        public void UpdateAllPlayersData(List<PlayerData> playerDataList)
        {
            foreach (var serverPlayer in playerDataList)
            {
                var localPlayer = players.Find(p => p.playerName == serverPlayer.playerName);
                if (localPlayer != null)
                {
                    localPlayer.money = serverPlayer.balance;
                    localPlayer.diceTriesLeft = serverPlayer.tries;

                    Debug.Log($"Updated {localPlayer.playerName}: Balance = {localPlayer.money}, Tries = {localPlayer.diceTriesLeft}");
                }
                else
                {
                    Debug.LogError($"Player {serverPlayer.playerName} not found locally!");
                }
            }

            UpdatePlayerUI();
        }
    }
}

