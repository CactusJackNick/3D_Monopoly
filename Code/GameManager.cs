using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public DiceManager diceManager;
    public PlayerManager playerManager;
    public CameraManager cameraManager;
    private DiceCheckZoneScript checkZone;
    private DiceScript diceScript;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameFlow();
        playerManager.terminateButton.gameObject.SetActive(false);
    }
    
    // Update is called once per frame
    void Update()
    {
            
    }
    
    public void GameFlow()
    {
        // ADJUSTING THE CAMERA TO THE DICE VIEW
        cameraManager.SwitchToDiceCamera();
        
    }
}
