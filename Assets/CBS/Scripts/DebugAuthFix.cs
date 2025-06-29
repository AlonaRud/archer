using UnityEngine;
using CBS;

public class DebugAuthFix : MonoBehaviour
{
    [SerializeField] private string lobbyScene; // Укажи имя сцены лобби, например, "Lobby"

    void Start()
    {
        var auth = CBSModule.Get<CBSAuthModule>();
        if (auth == null)
        {
            Debug.LogError("CBSAuthModule is null");
            return;
        }
        auth.AutoLogin(result =>
        {
            if (result.IsSuccess)
            {
                Debug.Log("Login successful via DebugAuthFix");
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbyScene);
            }
            else
                Debug.LogError($"Login failed: {result.Error?.Message}");
        });
    }
}
