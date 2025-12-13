using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using MLGameKit.PlayFab.Authenication;
using UnityEngine;
using PlayFab;
using System;
using MLGameKit.PlayFab.Leaderboard;

namespace MLGameKit.PlayFab
{
    public class PlayFabManager
    {
        private static PlayFabManager instance = null;
        public static PlayFabManager Instance
        {
            get
            {
                if (instance == null)
                {
                    _ = new PlayFabManager();
                }
                return instance;
            }
        }

        public readonly IAuthenticatable Authenticatable;
        public readonly ILeaderboardServices LeaderboardServices;
        public string playFabId { get; private set; }
        public string sessionTicket { get; private set; }

        // fields to check on user login, cache these to avoid playfab api calling
        public bool hasChangedName { get; private set; } = false;

        /// <summary>
        /// Authenticate using custom login on instance of PlayFab Manager created
        /// </summary>
        public PlayFabManager()
        {
            if (instance == null)
            {
                instance = this;
                Authenticatable = new PlayFabAuthenticateServices();
                LeaderboardServices = new PlayFabLeaderboardServices();
                Authenticate();
            }
            else
            {
                Debug.LogWarning($"An instance of {nameof(PlayFabManager)} is already exists.\nPlease check your script.\n");
                return;
            }
        }

        private void Authenticate()
        {
            Authenticatable.Authenticate(AuthTypes.Silent, callbackOnSuccess: OnPostLoginProcess);
        }

        private void OnPostLoginProcess(LoginResult result)
        {
            if (result == null)
            {
                Debug.LogWarning("Failed to authenticate user");
                return;
            }

            this.playFabId = result.PlayFabId;
            this.sessionTicket = result.SessionTicket;

            if (result.NewlyCreated)
            {
                UpdateDisplayName("Player" + UnityEngine.Random.Range(1, 100000), true);
                this.hasChangedName = false;
            }

            else
            {
                CheckUserHasChangedName();
            }

        }

        #region Display Name
        public void UpdateDisplayName(string name, bool ignoreUpdateUserData = false, Action OnDoneCallback = null, Action OnFailedCallback = null)
        {
            PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = name
            }, result =>
            {
                if (!hasChangedName)
                {
                    if (ignoreUpdateUserData) return;
                    SetHasChangedNameUserData(OnDoneCallback);
                }
                else OnDoneCallback?.Invoke();
#if UNITY_EDITOR
                Debug.Log($"{playFabId} changed name to {result.DisplayName}");
#endif
            }, error =>
            {
                OnFailedCallback?.Invoke();
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }

        private void SetHasChangedNameUserData(Action OnDoneCallBack)
        {
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>{
                    {"HasChangedName","Y" }
                }
            };
            PlayFabClientAPI.UpdateUserData(request, result =>
            {
                hasChangedName = true;
                OnDoneCallBack?.Invoke();
            }, error =>
            {
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }

        private void CheckUserHasChangedName()
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest()
            {
                PlayFabId = this.playFabId,
                Keys = new List<string>() { "HasChangedName" }
            }, result =>
            {
                //nmkha: use Y or N to represent bool state, if we use number, then an additional int parse is needed
                bool hasKey = result.Data.TryGetValue("HasChangedName", out var record);
                this.hasChangedName = hasKey ? string.Equals(record.Value, "Y") : false;
            }, error =>
            {
#if UNITY_EDITOR
                Debug.LogWarning(error.GenerateErrorReport());
#endif
            });
        }
        #endregion
    }
}
