using CBS;
using CBS.Models;
using PlayFab;
using PlayFab.CloudScriptModels;
using System;

namespace CBS.Playfab
{
    public class FabAchievements : FabExecuter, IFabAchievements
    {
        public void GetProfileAchievements(string profileID, TasksState state, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "GetProfileAchievements",
                FunctionParameter = new FunctionGetAchievementsRequest
                {
                    ProfileID = profileID,
                    State = state
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onGet, onFailed);
        }

        public void ModifyAchievementPoint(string profileID, string achievementID, int points, ModifyMethod method, Action<ExecuteFunctionResult> onAdd, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "AddAchievementPoints",
                FunctionParameter = new FunctionModifyAchievementPointsRequest
                {
                    ProfileID = profileID,
                    Points = points,
                    Method = method,
                    AchievementID = achievementID
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onAdd, onFailed);
        }

        public void PickupReward(string profileID, string achievementID, Action<ExecuteFunctionResult> onPick, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "PickupAchievementReward",
                FunctionParameter = new FunctionIDRequest
                {
                    ProfileID = profileID,
                    ID = achievementID
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onPick, onFailed);
        }

        public void ResetAchievement(string profileID, string achievementID, Action<ExecuteFunctionResult> onReset, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "ResetAchievement",
                FunctionParameter = new FunctionIDRequest
                {
                    ProfileID = profileID,
                    ID = achievementID
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onReset, onFailed);
        }

        public void GetAchievementsBadge(string profileID, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "GetAchievementsBadge",
                FunctionParameter = new FunctionBaseRequest
                {
                    ProfileID = profileID
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onGet, onFailed);
        }

        public void StartTechStudy(string profileID, string achievementID, int tierIndex, DateTime endTime, Action<ExecuteFunctionResult> onStudy, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "StartTechStudy",
                FunctionParameter = new FunctionTechStudyRequest
                {
                    ProfileID = profileID,
                    AchievementID = achievementID,
                    TierIndex = tierIndex,
                    EndTime = endTime
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onStudy, onFailed);
        }

        public void CheckActiveStudy(string profileID, Action<ExecuteFunctionResult> onGet, Action<PlayFabError> onFailed)
        {
            var request = new ExecuteFunctionRequest
            {
                FunctionName = "CheckActiveStudy",
                FunctionParameter = new FunctionBaseRequest
                {
                    ProfileID = profileID
                }
            };
            PlayFabCloudScriptAPI.ExecuteFunction(request, onGet, onFailed);
        }
    }

    [Serializable]
    public class FunctionTechStudyRequest
    {
        public string ProfileID;
        public string AchievementID;
        public int TierIndex;
        public DateTime EndTime;
    }

    [Serializable]
    public class FunctionActiveStudyResult
    {
        public bool IsActive;
    }
}
