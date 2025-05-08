using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Code
{
    public class URLParameters : MonoBehaviour
    {
        string token = null;
        string playerId = null;
        private DiceManager diceManager;
        public static URLParameters Instance;
        
        void Awake()
        {
            //Instance = this;
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public static Dictionary<string, string> GetURLParameters()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("Application.absoluteURL is empty. This only works in WebGL builds.");
                return result;
            }

            Uri uri = new Uri(url);
            string query = uri.Query; // Everything after '?'

            if (query.StartsWith("?"))
                query = query.Substring(1);

            string[] parameters = query.Split('&');
            foreach (string param in parameters)
            {
                string[] kvp = param.Split('=');
                if (kvp.Length == 2)
                {
                    string key = Uri.UnescapeDataString(kvp[0]);
                    string value = Uri.UnescapeDataString(kvp[1]);
                    result[key] = value;
                }
            }

            return result;
        }

        void Start()
        {
            var parameters = GetURLParameters();

            if (parameters.ContainsKey("token"))
            {
                token = parameters["token"];
                Debug.Log("Token: " + token);
            }

            if (parameters.ContainsKey("player"))
            {
                playerId = parameters["player"];
                Debug.Log("Player ID: " + playerId);
            }

            StartCoroutine(SendTokenToBackend());
            StartCoroutine(FetchUserData());
            StartCoroutine(FetchDiceFromServer());
        }

        IEnumerator SendTokenToBackend()
        {
            string backendUrl = "http://localhost:3000/players";
            UnityWebRequest request = new UnityWebRequest(backendUrl, "GET");

            request.downloadHandler = new DownloadHandlerBuffer();

            // ✅ Add headers
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", "Bearer " + token);

            if (!string.IsNullOrEmpty(playerId))
                request.SetRequestHeader("PlayerID", playerId);

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Backend error: " + request.error);
            }
            else
            {
                Debug.Log("Backend response: " + request.downloadHandler.text);
            }
        
        }

        public IEnumerator FetchUserData()
        {
            //string url = "https://dummyjson.com/users/1";
            string url = "http://localhost:3000/players";
            UnityWebRequest request = UnityWebRequest.Get(url);
    
            // ✅ Add headers
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", "Bearer " + token);
    
            if (!string.IsNullOrEmpty(playerId))
                request.SetRequestHeader("PlayerID", playerId);
    
            yield return request.SendWebRequest();
    
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = "Error fetching user data: " + request.error;
                Debug.LogError(errorMessage);
                Application.ExternalEval($"console.error('{errorMessage}');");
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Player data: " + jsonResponse);

                PlayerDataResponse playerDataResponse = JsonUtility.FromJson<PlayerDataResponse>(jsonResponse);
                PlayerManager.Instance.UpdateAllPlayersData(playerDataResponse.players);
                PlayerManager.Instance.UpdatePlayerUI();
            
                // Send it to browser console as well
                Application.ExternalEval($"console.log('User data: {EscapeForJavaScript(jsonResponse)}');");
            }

        }
            
        public IEnumerator FetchDiceFromServer() // 🔹 Fetch Dice Rolls from Server    
        {
            string url = "http://localhost:3000/roll-dice";
            UnityWebRequest request = UnityWebRequest.Get(url);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching dice rolls: " + request.error);
            }
            else
            {
                
                string json = request.downloadHandler.text;
                ServerData data = JsonUtility.FromJson<ServerData>(json);

                DiceManager.Instance.serverDice1 = data.dice1;
                DiceManager.Instance.serverDice2 = data.dice2;

                // 🚀 Immediately update the animations so the next roll is correct
                DiceManager.Instance.UpdateDiceAnimations();
                
                //DiceManager.Instance.RollDice();
            }
        }

        // 🔹 Helper class for server response
        [System.Serializable]
        public class ServerData
        {
            public int dice1;
            public int dice2;
        }
    
        string EscapeForJavaScript(string json)
        {
            return json.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
