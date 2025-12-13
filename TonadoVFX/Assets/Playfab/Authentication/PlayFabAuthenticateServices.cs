using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MLGameKit.PlayFab.Authenication
{
    public enum AuthTypes
    {
        None,
        Silent,
    }

    public interface IAuthenticatable
    {
        public void Authenticate(AuthTypes type, Action<LoginResult> callbackOnSuccess = null, Action callbackOnFail = null);
    }

    public class PlayFabAuthenticateServices : IAuthenticatable
    {
        private GetPlayerCombinedInfoRequestParams InfoRequestParams;

        void IAuthenticatable.Authenticate(AuthTypes type, Action<LoginResult> callbackOnSuccess, Action callbackOnFail)
        {
            switch (type)
            {
                case AuthTypes.None:
                    Debug.LogWarning("Calling authenticating with type of None");
                    break;
                case AuthTypes.Silent:
                    AnoynymousLogin(callbackOnSuccess, callbackOnFail);
                    break;
            }
        }

        private void AnoynymousLogin(Action<LoginResult> callbackOnSuccess, Action callbackOnFail)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            //Get the device id from native android
            AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
            AndroidJavaClass secure = new AndroidJavaClass("android.provider.Settings$Secure");
            string deviceId = secure.CallStatic<string>("getString", contentResolver, "android_id");

            //Login with the android device ID
            PlayFabClientAPI.LoginWithAndroidDeviceID(new LoginWithAndroidDeviceIDRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                AndroidDevice = SystemInfo.deviceModel,
                OS = SystemInfo.operatingSystem,
                AndroidDeviceId = deviceId,
                CreateAccount = true,
                InfoRequestParameters = InfoRequestParams
            }, (result) =>
            {
                callbackOnSuccess?.Invoke(result);
            }, (error) =>
            {
                callbackOnFail?.Invoke();
                Debug.LogError(error.GenerateErrorReport());
            });

#elif UNITY_IPHONE || UNITY_IOS && !UNITY_EDITOR
            PlayFabClientAPI.LoginWithIOSDeviceID(new LoginWithIOSDeviceIDRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                DeviceModel = SystemInfo.deviceModel,
                OS = SystemInfo.operatingSystem,
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true,
                InfoRequestParameters = InfoRequestParams
            }, (result) =>
            {
                callbackOnSuccess?.Invoke(result);
            }, (error) =>
            {
                callbackOnFail?.Invoke();
                Debug.LogError(error.GenerateErrorReport());
            });
#else
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true,
                InfoRequestParameters = InfoRequestParams
            }, (result) =>
            {
                callbackOnSuccess?.Invoke(result);
            }, (error) =>
            {
                callbackOnFail?.Invoke();
                Debug.LogError(error.GenerateErrorReport());
            });
#endif
        }
    }
}
