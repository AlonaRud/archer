using UnityEngine;
using CBS;

public class DebugLoginScene : MonoBehaviour
{
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
                Debug.Log("Login successful: AutoLogin completed");
            else
                Debug.LogError($"Login failed: {result.Error?.Message}");
        });
    }
}
