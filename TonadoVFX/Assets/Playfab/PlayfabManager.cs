using PlayFab.ClientModels;
using PlayFab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor.PackageManager;
using Newtonsoft.Json;

public class PlayfabManager : MonoBehaviour
{
    public TMP_InputField Input;
    public TextMeshProUGUI Title;
    public void Start()
    {
        var request = new LoginWithCustomIDRequest { CustomId = "GettingStartedGuide", CreateAccount = true };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
    }
    void GetTitleData()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnTitleDataRC, OnError);
    }
    void OnTitleDataRC(GetTitleDataResult result)
    {
        if(result.Data != null && result.Data.ContainsKey(PlayfabKeys.Message)) 
        {
            Title.text = result.Data[PlayfabKeys.Message];
        }
        else
        {
            Debug.Log("::: OnTitleDataRC Error");
        }
    }
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Congratulations, you made your first successful API call!");
        GetTitleData();
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }
    void OnDataSend(UpdateUserDataResult result)
    {
        Debug.Log("::: OnDataSend Successfull <3");
    }
    public void Get()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataRecived, OnError);
    }
    void OnDataRecived(GetUserDataResult result)
    {
        if(result.Data != null)
        {
            Input.text = result.Data[PlayfabKeys.ValueUpdate].Value.ToString();
        }
        else
        {
            Debug.LogError("::: Data Error");
        }
    }
    public void Set()
    {
        PlayerData playerData = new PlayerData();
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string> {
                { PlayfabKeys.ValueUpdate, JsonConvert.SerializeObject(playerData)}  
            }
        };
        PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);

        SendLeaderBoard(123);
        GetLeaderboard("UnityScore");
    }

    public void SendLeaderBoard(int score)
    {
        var request = new UpdatePlayerStatisticsRequest()
        {
            Statistics = new List<StatisticUpdate> 
            { 
                new StatisticUpdate() 
                {
                    StatisticName = "UnityScore",
                    Value = score 
                } 
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdatePlayerStatistic, OnError);
    }
    void OnUpdatePlayerStatistic(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("::: OnDataSend OnUpdatePlayerStatistic <3");
    }

    public void GetLeaderboard(string statisticName)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = statisticName,
            StartPosition = 0,
            MaxResultsCount = 10, // Number of entries to retrieve
            ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
        };

        PlayFabClientAPI.GetLeaderboard(request, OnGetLeaderboardSuccess, OnGetLeaderboardFailure);
    }

    private void OnGetLeaderboardSuccess(GetLeaderboardResult result)
    {
        foreach (var entry in result.Leaderboard)
        {
            Debug.Log($"Rank: {entry.Position}, PlayFabId: {entry.PlayFabId}, Score: {entry.StatValue}, DisplayName: {entry.DisplayName}");
        }
    }

    private void OnGetLeaderboardFailure(PlayFabError error)
    {
        Debug.LogError($"Failed to get leaderboard: {error.GenerateErrorReport()}");
    }
}
