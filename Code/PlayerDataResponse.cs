using System.Collections.Generic;
using Code;

[System.Serializable]
public class PlayerDataResponse
{
    public List<PlayerData> players;
}

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int balance;
    public int tries = 3;
}
