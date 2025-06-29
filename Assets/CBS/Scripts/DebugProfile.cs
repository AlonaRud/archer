using UnityEngine;
using CBS;

public class DebugProfile : MonoBehaviour
{
    void Start()
    {
        var cbsProfile = CBSModule.Get<CBSProfileModule>();
        Debug.Log($"CBSProfile: {(cbsProfile == null ? "null" : "exists")}");
        if (cbsProfile != null)
            Debug.Log($"CBSProfile.Avatar: {(cbsProfile.Avatar == null ? "null" : cbsProfile.Avatar.AvatarID)}");
    }
}
