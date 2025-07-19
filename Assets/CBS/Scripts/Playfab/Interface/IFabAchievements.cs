using CBS;
using CBS.Models;
using PlayFab;
using PlayFab.CloudScriptModels;
using System;

namespace CBS.Playfab
{
    public interface IFabAchievements
    {
        void GetProfileAchievements(string profileID, TasksState state, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed);

        void ModifyAchievementPoint(string profileID, string achievementID, int points, ModifyMethod method, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed);

        void PickupReward(string profileID, string achievementID, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed);

        void ResetAchievement(string profileID, string achievementID, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed);

        void GetAchievementsBadge(string profileID, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed);

        // NEW: Start technology study
        void StartTechStudy(string profileID, string achievementID, int tierIndex, DateTime endTime, Action<ExecuteFunctionResult> onStudy, Action<PlayFabError> onFailed);

        // NEW: Check if any technology is being studied
        void CheckActiveStudy(string profileID, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed);
    }
}