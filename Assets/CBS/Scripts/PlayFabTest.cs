using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabTest : MonoBehaviour
{
    void Start()
    {
        // Проверяем, задан ли Title ID
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            Debug.LogError("PlayFab не настроен: Title ID пустой! Укажите Title ID в PlayFabSharedSettings.asset");
            return;
        }

        Debug.Log("PlayFab: Title ID = " + PlayFabSettings.staticSettings.TitleId);

        // Настраиваем запрос для авторизации
        var request = new LoginWithCustomIDRequest
        {
            TitleId = PlayFabSettings.staticSettings.TitleId,
            CustomId = SystemInfo.deviceUniqueIdentifier, // Уникальный ID устройства
            CreateAccount = true // Создать аккаунт, если он не существует
        };

        // Выполняем авторизацию
        PlayFabClientAPI.LoginWithCustomID(request,
            OnLoginSuccess, // Callback при успешной авторизации
            OnLoginFailure  // Callback при ошибке
        );
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("PlayFab: Успешная авторизация! PlayFabId: " + result.PlayFabId);
        Debug.Log("PlayFab работает корректно!");
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("PlayFab: Ошибка авторизации: " + error.GenerateErrorReport());
    }
}
