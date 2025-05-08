using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Code
{
    public class DiceManager : MonoBehaviour
    {
        public static DiceManager Instance { get; private set; }
        public Button rollButton;
        public Text resultText;
        public GameObject resultPanel;
        public DiceCheckZoneScript checkZone;
        [SerializeField] private GameObject dicePrefab1; // Assigned in the Unity Inspector or load dynamically
        [SerializeField] private GameObject dicePrefab2; // Assigned in the Unity Inspector or load dynamically

        public bool rolling;
        private PlayerManager playerManager;
        
        public DiceScript dice1; // First dice reference
        public DiceScript dice2; // Second dice reference


        public int serverDice1, serverDice2;
        private DiceScript _diceScript;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            rolling = false;
            resultPanel.SetActive(false);
            //rollButton.onClick.AddListener(RollDice); // Attach button click event
            rollButton.onClick.AddListener(() => StartCoroutine(URLParameters.Instance.FetchDiceFromServer()));

            playerManager = FindFirstObjectByType<PlayerManager>(); // Reference PlayerManager
        }

        public void DestroyUnusedDice()
        {
            foreach (DiceScript dice in checkZone.diceList)
            {
                dice.gameObject.SetActive(false);
                if (dice != null) // Check if the dice object is not null
                {
                    Destroy(dice.gameObject); // Destroy the dice GameObject
                }
            }

            checkZone.diceList.Clear(); // ✅ Avoids null references
        }
        
        public void SpawnDice()
        {
            checkZone.diceList.Clear();

            // Different spawn positions
            Vector3 dice1Position = new Vector3(0, 2, -5); 
            Vector3 dice2Position = new Vector3(0, 2, 5);

            // Instantiate dice
            GameObject dice1Obj = Instantiate(dicePrefab1, dice1Position, Quaternion.identity);
            GameObject dice2Obj = Instantiate(dicePrefab2, dice2Position, Quaternion.identity);

            // Assign DiceScript components
            dice1 = dice1Obj.GetComponent<DiceScript>();
            dice2 = dice2Obj.GetComponent<DiceScript>();

            // Add to check zone
            checkZone.diceList.Add(dice1);
            checkZone.diceList.Add(dice2);

            Debug.Log($"Dice spawned at: {dice1Position} and {dice2Position}");
        }
        
        public void UpdateDiceAnimations()
        {
            if (dice1 != null && dice2 != null) // Ensure dice exist
            {
                dice1.RollToSpecificNumber(serverDice1);
                dice2.RollToSpecificNumber(serverDice2);
            }
        }
        
        public bool RolledDoubles()
        {
            if (checkZone.diceList.Count < 2) return false;
            //int firstRoll = checkZone.diceList[0].currentNumber;  //previous iteration before server integration
            //int secondRoll = checkZone.diceList[1].currentNumber;
            int firstRoll = serverDice1;
            int secondRoll = serverDice2;

            if ((int)firstRoll == (int)secondRoll)
                Debug.Log($"Dice Values rolled Doubles: Dice1 {firstRoll}, Dice2 {secondRoll}");
        
            return firstRoll == secondRoll; // Check for doubles
        }

        public void RollDice()
        {
            Player currentPlayer = PlayerManager.Instance.players[PlayerManager.Instance.currentPlayerIndex];

            if (currentPlayer.diceTriesLeft <= 0)
            {
                Debug.Log(currentPlayer.playerName + " has no tries left!");
                return;
            }

            if (rolling) return;

            rolling = true;
            rollButton.gameObject.SetActive(false); // Disable button during dice roll
            
            RollAllDice();
            StartCoroutine(WaitForDiceToSettle());
            
            currentPlayer.diceTriesLeft--;
            playerManager.UpdatePlayerUI();
            // If rolled doubles == true then the same player needs to roll again and move again.
        }

        public void RollAllDice()
        {
            for (int i = 0; i < checkZone.diceList.Count; i++)
            {
                int targetNumber = (i == 0) ? serverDice1 : serverDice2;
                checkZone.diceList[i].RollToSpecificNumber(targetNumber);
            }
        }

        private IEnumerator WaitForDiceToSettle()
        {
            yield return new WaitUntil(AllDiceStationary);
            checkZone.CheckAndUpdateSum(); // Update dice sum after they stop
        
            yield return new WaitForSeconds(2.5f); // Delay before hiding result
        
            DisplayResult();
            playerManager.StartPlayerMovement(serverDice1+serverDice2); // Notify PlayerManager to move the player
            rolling = false;
        }

        public int GetDiceSum()
        {
            return checkZone.totalSum;
        }

        public bool AllDiceStationary()
        {
            foreach (DiceScript dice in checkZone.diceList)
            {
                if (!dice.IsStationary()) return false;
            }
            return true;
        }

        private void DisplayResult()
        {
            resultPanel.SetActive(true);
            //resultText.text = $"Dice Total: {checkZone.totalSum}";
            resultText.text = $"Dice Total: {serverDice1 + serverDice2}";
            Invoke(nameof(HideResult), 2f); // Hide result after 2 seconds
        }

        private void HideResult()
        {
            resultPanel.SetActive(false);
        }

    }
}
