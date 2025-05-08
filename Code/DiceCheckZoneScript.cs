using System;
using System.Collections.Generic;
using Code;
using Unity.VisualScripting;
using UnityEngine;
public class DiceCheckZoneScript : MonoBehaviour
{
    public List<DiceScript> diceList = new List<DiceScript>();
    public int totalSum = 0;
    private DiceManager diceManager;

    private void Awake()
    {
        diceManager = FindFirstObjectByType<DiceManager>(); 
        if (diceManager == null)
        {
            Debug.LogError("DiceManager not found in scene!");
        }
    }

    private void LateUpdate()
    {
        if (diceList == null || diceList.Count == 0) return; // Skip if no dice
        RecalculateSum();
    }

    public void RecalculateSum()
    {
        totalSum = 0;
        foreach (DiceScript dice in diceList)
        {
            if (dice == null) continue;
            if (dice.IsStationary())
            {
                int topFace = GetTopFaceNumber(dice);
                 if (topFace > 0)
                 {
                     dice.SetNumber(topFace);
                 }
                 totalSum += dice.GetNumber();
            }
        }
    }
    
    // This method is now moved to where it's necessary (e.g., after dice are stationary).
    public void CheckAndUpdateSum()
    {
        if (AllDiceStationary())
        {
            RecalculateSum();
        }
    }

    private bool AllDiceStationary()
    {
        foreach (DiceScript dice in diceList)
        {
            if (!dice.IsStationary()) return false;
        }
        return true;
    }

    int GetTopFaceNumber(DiceScript dice)
    {
        if (diceManager.serverDice1 == dice.GetNumber()) return diceManager.serverDice1;
        if (diceManager.serverDice2 == dice.GetNumber()) return diceManager.serverDice2;
        
        Transform topSide = null;
        float maxY = float.MinValue;
    
        foreach (Transform side in dice.transform)
        {
            if (side.position.y > maxY)
            {
                maxY = side.position.y;
                topSide = side;
            }
        }
    
        if (topSide != null)
        {
            switch (topSide.name)
            {
                case "Side1": return 1;
                case "Side2": return 2;
                case "Side3": return 3;
                case "Side4": return 4;
                case "Side5": return 5;
                case "Side6": return 6;
            }
        }
        
        return 1; // No valid side detected
    }
}
