using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MLGameKit.PlayFab;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MLGameKit.PlayFab.Leaderboard
{

    public enum LeaderboardType
    {
        HighLevel,
        WeeklyTournament
    }

    public interface ILeaderboardServices
    {
        public void RequestLeaderboard(LeaderboardType type, int count, int? version, Action<GetLeaderboardResult> callbackOnSuccess, Action callbackOnFail);
        public void RequestSelfLeaderboard(LeaderboardType type, int? version, Action<GetLeaderboardAroundPlayerResult> callbackOnSuccess, Action callbackOnFail);
        public void SubmitStat(LeaderboardType type, int stat, Action callbackOnSuccess, Action callbackOnFail);
        public UniTask<int> GetTournamentIteration();
        public UniTask<int> GetTournamentPrizePosition();
        public void ClaimTournamentReward(int position, Action callbackOnSuccess, Action callbackOnFail);
    }

    public class PlayFabLeaderboardServices : ILeaderboardServices
    {
        //nmkha: Key to track tournament iteration, must be the same setup as PlayFab
        private const string weeklyTournamentIterationKey = "weekly_tournament_iteration";
        private PlayerProfileViewConstraints profileConstraints;

        /// <summary>
        /// You should configure title setting in PlayFab to allow Profile Constraint for Locations, DisplayName, Avatar URL
        /// </summary>
        public PlayFabLeaderboardServices()
        {
            profileConstraints = new PlayerProfileViewConstraints
            {
                ShowLocations = true,
                ShowDisplayName = true,
                ShowAvatarUrl = true,
            };
        }

        void ILeaderboardServices.RequestLeaderboard(LeaderboardType type, int count, int? version, Action<GetLeaderboardResult> callbackOnSuccess, Action callbackOnFail)
        {
            PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
            {
                StatisticName = type.ToString(),
                StartPosition = 0,
                MaxResultsCount = count,
                Version = version,
                ProfileConstraints = profileConstraints
            }, result =>
            {
                callbackOnSuccess?.Invoke(result);
            }, error =>
            {
                callbackOnFail?.Invoke();
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }

        void ILeaderboardServices.RequestSelfLeaderboard(LeaderboardType type, int? version, Action<GetLeaderboardAroundPlayerResult> callbackOnSuccess, Action callbackOnFail)
        {
            PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = type.ToString(),
                MaxResultsCount = 1,
                Version = version
            }, result =>
            {
                callbackOnSuccess?.Invoke(result);
            }, error =>
            {
                callbackOnFail?.Invoke();
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }

        void ILeaderboardServices.SubmitStat(LeaderboardType type, int stat, Action callbackOnSuccess, Action callbackOnFail)
        {
            PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>{
                    new StatisticUpdate{
                        StatisticName = type.ToString(),
                        Value = stat
                    }
                }
            }, result =>
            {
                OnStatisticUpdated(result, type);
                callbackOnSuccess?.Invoke();
            }, error =>
            {
                callbackOnFail?.Invoke();
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }

        private void OnStatisticUpdated(UpdatePlayerStatisticsResult result, LeaderboardType type)
        {
#if UNITY_EDITOR
            Debug.Log($"Player {PlayFabManager.Instance.playFabId} updated new statistic to {type}.");
#endif
        }

        async UniTask<int> ILeaderboardServices.GetTournamentIteration()
        {
            int iteration = -1;
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
            {
                Keys = new List<string>() { weeklyTournamentIterationKey }
            }, result =>
            {
                bool hasKey = result.Data.TryGetValue(weeklyTournamentIterationKey, out var record);
                if (!hasKey)
                {
                    iteration = 0;
                }
                else
                {
                    iteration = int.Parse(record);
                }
            }, error =>
            {
#if UNITY_EDITOR
                Debug.Log(error.GenerateErrorReport());
#endif
                iteration = 0;
            });
            await UniTask.WaitUntil(() => iteration != -1);
            return iteration;
        }

        /// <summary>
        /// return position value start from 1 to 3
        /// </summary>
        /// <returns></returns>
        async UniTask<int> ILeaderboardServices.GetTournamentPrizePosition()
        {
            int pos = -1;
            PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
            {
                PlayFabId = PlayFabManager.Instance.playFabId,
                Keys = new List<string>() { "tournament_prize" }
            }, result =>
            {
                bool hasKey = result.Data.TryGetValue("tournament_prize", out var record);
                if (hasKey)
                {
                    int value = int.Parse(record.Value);
                    pos = value;
                }
                else pos = 0;
            }, error =>
            {
                pos = 0;
#if UNITY_EDITOR
                Debug.Log(error.GenerateErrorReport());
#endif
            });

            await UniTask.WaitUntil(() => pos != -1);
            return pos;
        }

        void ILeaderboardServices.ClaimTournamentReward(int position, Action callbackOnSuccess, Action callbackOnFail)
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "ClaimWeeklyTournamentPrize"
            }, result =>
            {
                callbackOnSuccess?.Invoke();
            }, error =>
            {
                callbackOnFail?.Invoke();
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }
    }
}
