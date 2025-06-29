using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

public class DebugPlayFabLogin : MonoBehaviour
{
    [SerializeField] private string lobbyScene = "Lobby"; // Укажи имя сцены лобби

    void Start()
    {
        PlayFabSettings.TitleId = "1DB9B8"; // Твой Title ID

        var request = new LoginWithCustomIDRequest
        {
            CustomId = System.Guid.NewGuid().ToString(), // Случайный уникальный ID
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, result =>
        {
            Debug.Log($"PlayFab Login successful: PlayFabId={result.PlayFabId}");
            SceneManager.LoadScene(lobbyScene);
        }, error =>
        {
            Debug.LogError($"PlayFab Login failed: {error.ErrorMessage} (Code: {error.HttpCode})");
        });
    }
}