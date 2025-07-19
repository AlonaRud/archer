using CBS.Models;
using CBS;
using CBS.Playfab;
using CBS.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab.CloudScriptModels;

namespace CBS
{
    public class CBSAchievementsModule : CBSModule, IAchievements
    {
        public event Action<CBSTask> OnCompleteAchievement;
        public event Action<CBSTask> OnCompleteAchievementTier;
        public event Action<GrantRewardResult> OnProfileRewarded;

        private IProfile Profile { get; set; }
        private IFabAchievements FabAchievements { get; set; }

        protected override void Init()
        {
            Profile = Get<CBSProfileModule>();
            FabAchievements = FabExecuter.Get<FabAchievements>();
        }

        public void GetAchievementsTable(Action<CBSGetAchievementsTableResult> result)
        {
            InternalGetAchievements(TasksState.ALL, result);
        }

        public void GetActiveAchievementsTable(Action<CBSGetAchievementsTableResult> result)
        {
            InternalGetAchievements(TasksState.ACTIVE, result);
        }

        public void GetCompletedAchievementsTable(Action<CBSGetAchievementsTableResult> result)
        {
            InternalGetAchievements(TasksState.COMPLETED, result);
        }

        public void AddAchievementPoint(string achievementID, Action<CBSModifyAchievementPointResult> result)
        {
            InternalModifyPoints(achievementID, 1, ModifyMethod.ADD, result);
        }

        public void AddAchievementPoint(string achievementID, int points, Action<CBSModifyAchievementPointResult> result)
        {
            InternalModifyPoints(achievementID, points, ModifyMethod.ADD, result);
        }

        public void UpdateAchievementPoint(string achievementID, int points, Action<CBSModifyAchievementPointResult> result)
        {
            InternalModifyPoints(achievementID, points, ModifyMethod.UPDATE, result);
        }

        public void PickupAchievementReward(string achievementID, Action<CBSPickupAchievementRewardResult> result)
        {
            var profileID = Profile.ProfileID;

            FabAchievements.PickupReward(profileID, achievementID, onPick =>
            {
                var cbsError = onPick.GetCBSError();
                if (cbsError != null)
                {
                    result?.Invoke(new CBSPickupAchievementRewardResult
                    {
                        IsSuccess = false,
                        Error = cbsError
                    });
                    return;
                }

                var functionResult = onPick.GetResult<FunctionModifyTaskResult<CBSTask>>();
                var achievement = functionResult.Task;
                var reward = functionResult.RewardResult;

                if (functionResult != null && reward != null)
                {
                    var currencies = reward.GrantedCurrencies;
                    if (currencies != null)
                    {
                        var codes = currencies.Select(x => x.Key).ToArray();
                        Get<CBSCurrencyModule>().ChangeRequest(codes);
                    }

                    var grantedInstances = reward.GrantedInstances;
                    if (grantedInstances != null && grantedInstances.Count > 0)
                    {
                        var inventoryItems = grantedInstances.Select(x => x.ToCBSInventoryItem()).ToList();
                        Get<CBSInventoryModule>().AddRequest(inventoryItems);
                    }

                    // Apply bonus (commented until managers are implemented)
                    if (achievement.TierList != null)
                    {
                        var currentTier = achievement.TierList.FirstOrDefault(t => t.Index == achievement.TierIndex);
                        if (currentTier != null && currentTier.Reward != null)
                        {
                            var bonus = currentTier.Reward;
                            if (!string.IsNullOrEmpty(bonus.Type))
                            {
                                // if (bonus.Type == "TroopsStrength" && !string.IsNullOrEmpty(bonus.TargetUnitType))
                                //     Get<TroopsManager>().AddStrengthBonus(bonus.Value, bonus.TargetUnitType);
                                // else if (bonus.Type == "HeroHealth")
                                //     Get<HeroManager>().AddHealthBonus(bonus.Value);
                                // else if (bonus.Type == "EconomyIncome")
                                //     Get<EconomyManager>().AddIncomeBonus(bonus.Value);
                            }
                        }
                    }

                    OnProfileRewarded?.Invoke(reward);
                }

                result?.Invoke(new CBSPickupAchievementRewardResult
                {
                    IsSuccess = true,
                    Achievement = achievement,
                    ReceivedReward = reward
                });
            }, onFailed =>
            {
                result?.Invoke(new CBSPickupAchievementRewardResult
                {
                    IsSuccess = false,
                    Error = CBSError.FromTemplate(onFailed)
                });
            });
        }

        public void ResetAchievement(string achievementID, Action<CBSResetAchievementResult> result)
        {
            var profileID = Profile.ProfileID;

            FabAchievements.ResetAchievement(profileID, achievementID, onReset =>
            {
                var cbsError = onReset.GetCBSError();
                if (cbsError != null)
                {
                    result?.Invoke(new CBSResetAchievementResult
                    {
                        IsSuccess = false,
                        Error = cbsError
                    });
                    return;
                }

                var functionResult = onReset.GetResult<FunctionModifyTaskResult<CBSTask>>();
                var achievement = functionResult.Task;

                result?.Invoke(new CBSResetAchievementResult
                {
                    IsSuccess = true,
                    Achievement = achievement
                });
            }, onFailed =>
            {
                result?.Invoke(new CBSResetAchievementResult
                {
                    IsSuccess = false,
                    Error = CBSError.FromTemplate(onFailed)
                });
            });
        }

        public void GetAchievementsBadge(Action<CBSBadgeResult> result)
        {
            var profileID = Profile.ProfileID;

            FabAchievements.GetAchievementsBadge(profileID, onReset =>
            {
                var cbsError = onReset.GetCBSError();
                if (cbsError != null)
                {
                    result?.Invoke(new CBSBadgeResult
                    {
                        IsSuccess = false,
                        Error = cbsError
                    });
                    return;
                }

                var functionResult = onReset.GetResult<FunctionBadgeResult>();
                var badgeCount = functionResult.Count;

                result?.Invoke(new CBSBadgeResult
                {
                    IsSuccess = true,
                    Count = badgeCount
                });
            }, onFailed =>
            {
                result?.Invoke(new CBSBadgeResult
                {
                    IsSuccess = false,
                    Error = CBSError.FromTemplate(onFailed)
                });
            });
        }

   public void StartTechStudy(string achievementID, int tierIndex, Action<CBSStartTechStudyResult> result)
{
    var profileID = Profile.ProfileID;

    FabAchievements.CheckActiveStudy(profileID, onGetActive =>
    {
        var cbsError = onGetActive.GetCBSError();
        if (cbsError != null)
        {
            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = cbsError });
            return;
        }

        var studyResult = onGetActive.GetResult<FunctionActiveStudyResult>();
        if (studyResult.IsActive)
        {
            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = new CBSError { Message = "Another task is active" } });
            return;
        }

        FabAchievements.GetProfileAchievements(profileID, TasksState.ALL, onGet =>
        {
            var cbsError = onGet.GetCBSError();
            if (cbsError != null)
            {
                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = cbsError });
                return;
            }

            var functionResult = onGet.GetResult<FunctionTasksResult<CBSTask>>();
            var achievements = functionResult.Tasks ?? new List<CBSTask>();
            var achievement = achievements.FirstOrDefault(x => x.ID == achievementID);
            if (achievement == null || tierIndex >= achievement.TierList.Count)
            {
                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = new CBSError { Message = "Achievement or tier not found" } });
                return;
            }

            var tier = achievement.TierList[tierIndex];

            // Проверка зависимостей
            bool dependenciesMet = true;
            var failedDependencies = new List<Dependency>();
            if (tier.Dependencies != null)
            {
                foreach (var dep in tier.Dependencies)
                {
                    var depAchievement = achievements.FirstOrDefault(x => x.ID == dep.TechID);
                    if (depAchievement == null || depAchievement.TierIndex < dep.Level)
                    {
                        dependenciesMet = false;
                        failedDependencies.Add(dep);
                    }
                }
            }

            if (!dependenciesMet)
            {
                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, FailedDependencies = failedDependencies });
                return;
            }

            var inventoryModule = Get<CBSInventoryModule>();
            var currencyModule = Get<CBSCurrencyModule>();

            if (achievement.Type == TaskType.BUILDING)
            {
                // Проверка ресурсов (предметов)
                if (tier.ResourceCosts != null && tier.ResourceCosts.Count > 0)
                {
                    var inventoryResult = inventoryModule.GetInventoryFromCache();
                    if (!inventoryResult.IsSuccess)
                    {
                        result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = inventoryResult.Error });
                        return;
                    }

                    foreach (var resource in tier.ResourceCosts)
                    {
                        var items = inventoryResult.AllItems.Where(x => x.ItemID == resource.ItemId).ToList();
                        int totalQuantity = items.Sum(x => x.Count ?? 0);
                        if (totalQuantity < resource.Amount)
                        {
                            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = new CBSError { Message = $"Недостаточно предмета {resource.ItemId}" } });
                            return;
                        }
                    }

                    // Списание предметов
                    foreach (var resource in tier.ResourceCosts)
                    {
                        var items = inventoryResult.AllItems.Where(x => x.ItemID == resource.ItemId).ToList();
                        int remainingAmount = resource.Amount;
                        foreach (var item in items)
                        {
                            if (remainingAmount <= 0) break;
                            int consumeAmount = Math.Min(remainingAmount, item.Count ?? 0);
                            inventoryModule.ConsumeItem(item.InstanceID, consumeAmount, consumeResult =>
                            {
                                if (!consumeResult.IsSuccess)
                                {
                                    result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = consumeResult.Error });
                                    return;
                                }
                            });
                            remainingAmount -= consumeAmount;
                        }
                    }
                }

                // Запуск таймера
                var timeInMinutes = tier.StudyHours * 60 + tier.StudyMinutes;
                var endTime = DateTime.UtcNow.AddMinutes(timeInMinutes);
                FabAchievements.StartTechStudy(profileID, achievementID, tierIndex, endTime, onStudy =>
                {
                    var studyError = onStudy.GetCBSError();
                    if (studyError != null)
                    {
                        result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = studyError });
                        return;
                    }

                    achievement.IsActive = true;
                    InternalModifyPoints(achievementID, 0, ModifyMethod.UPDATE, modifyResult =>
                    {
                        if (modifyResult.IsSuccess)
                        {
                            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = true, Achievement = modifyResult.Achievement });
                        }
                        else
                        {
                            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = modifyResult.Error });
                        }
                    });
                }, onFailed =>
                {
                    result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = CBSError.FromTemplate(onFailed) });
                });
            }
            else
            {
                // Существующая логика для технологий
                if (tier.Cost > 0 && !string.IsNullOrEmpty(tier.CurrencyCode))
                {
                    currencyModule.GetProfileCurrencies(profileID, currencyResult =>
                    {
                        if (!currencyResult.IsSuccess)
                        {
                            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = currencyResult.Error });
                            return;
                        }

                        var currency = currencyResult.Currencies.GetValueOrDefault(tier.CurrencyCode);
                        if (currency == null || currency.Value < tier.Cost)
                        {
                            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = new CBSError { Message = "Недостаточно валюты" } });
                            return;
                        }

                        currencyModule.SubtractCurrencyFromProfile(profileID, tier.CurrencyCode, tier.Cost, subtractResult =>
                        {
                            if (!subtractResult.IsSuccess)
                            {
                                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = subtractResult.Error });
                                return;
                            }

                            var timeInMinutes = tier.StudyHours * 60 + tier.StudyMinutes;
                            var endTime = DateTime.UtcNow.AddMinutes(timeInMinutes);
                            FabAchievements.StartTechStudy(profileID, achievementID, tierIndex, endTime, onStudy =>
                            {
                                var studyError = onStudy.GetCBSError();
                                if (studyError != null)
                                {
                                    result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = studyError });
                                    return;
                                }

                                achievement.IsActive = true;
                                InternalModifyPoints(achievementID, 0, ModifyMethod.UPDATE, modifyResult =>
                                {
                                    if (modifyResult.IsSuccess)
                                    {
                                        result?.Invoke(new CBSStartTechStudyResult { IsSuccess = true, Achievement = modifyResult.Achievement });
                                    }
                                    else
                                    {
                                        result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = modifyResult.Error });
                                    }
                                });
                            }, onFailed =>
                            {
                                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = CBSError.FromTemplate(onFailed) });
                            });
                        });
                    });
                }
                else
                {
                    var timeInMinutes = tier.StudyHours * 60 + tier.StudyMinutes;
                    var endTime = DateTime.UtcNow.AddMinutes(timeInMinutes);
                    FabAchievements.StartTechStudy(profileID, achievementID, tierIndex, endTime, onStudy =>
                    {
                        var studyError = onStudy.GetCBSError();
                        if (studyError != null)
                        {
                            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = studyError });
                            return;
                        }

                        achievement.IsActive = true;
                        InternalModifyPoints(achievementID, 0, ModifyMethod.UPDATE, modifyResult =>
                        {
                            if (modifyResult.IsSuccess)
                            {
                                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = true, Achievement = modifyResult.Achievement });
                            }
                            else
                            {
                                result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = modifyResult.Error });
                            }
                        });
                    }, onFailed =>
                    {
                        result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = CBSError.FromTemplate(onFailed) });
                    });
                }
            }
        }, onFailed =>
        {
            result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = CBSError.FromTemplate(onFailed) });
        });
    }, onFailed =>
    {
        result?.Invoke(new CBSStartTechStudyResult { IsSuccess = false, Error = CBSError.FromTemplate(onFailed) });
    });
}
        private void InternalModifyPoints(string achievementID, int points, ModifyMethod modify, Action<CBSModifyAchievementPointResult> result)
        {
            var profileID = Profile.ProfileID;

            FabAchievements.ModifyAchievementPoint(profileID, achievementID, points, modify, onAdd =>
            {
                var cbsError = onAdd.GetCBSError();
                if (cbsError != null)
                {
                    result?.Invoke(new CBSModifyAchievementPointResult
                    {
                        IsSuccess = false,
                        Error = cbsError
                    });
                    return;
                }

                var functionResult = onAdd.GetResult<FunctionModifyTaskResult<CBSTask>>();
                var achievement = functionResult.Task;
                var reward = functionResult.RewardResult;
                var complete = functionResult.CompleteTask;
                var completeTier = functionResult.CompleteTier;

                if (functionResult != null && reward != null)
                {
                    var currencies = reward.GrantedCurrencies;
                    if (currencies != null)
                    {
                        var codes = currencies.Select(x => x.Key).ToArray();
                        Get<CBSCurrencyModule>().ChangeRequest(codes);
                    }
                    OnProfileRewarded?.Invoke(reward);

                    var grantedInstances = reward.GrantedInstances;
                    if (grantedInstances != null && grantedInstances.Count > 0)
                    {
                        var inventoryItems = grantedInstances.Select(x => x.ToCBSInventoryItem()).ToList();
                        Get<CBSInventoryModule>().AddRequest(inventoryItems);
                    }
                }

                if (complete)
                {
                    OnCompleteAchievement?.Invoke(achievement);
                }
                if (completeTier)
                {
                    OnCompleteAchievementTier?.Invoke(achievement);
                }

                result?.Invoke(new CBSModifyAchievementPointResult
                {
                    IsSuccess = true,
                    Achievement = achievement,
                    ReceivedReward = reward,
                    CompleteAchievement = complete,
                    CompleteTier = completeTier
                });
            }, onFailed =>
            {
                result?.Invoke(new CBSModifyAchievementPointResult
                {
                    IsSuccess = false,
                    Error = CBSError.FromTemplate(onFailed)
                });
            });
        }

        private void InternalGetAchievements(TasksState queryType, Action<CBSGetAchievementsTableResult> result)
        {
            var profileID = Profile.ProfileID;

            FabAchievements.GetProfileAchievements(profileID, queryType, onGet =>
            {
                var cbsError = onGet.GetCBSError();
                if (cbsError != null)
                {
                    result?.Invoke(new CBSGetAchievementsTableResult
                    {
                        IsSuccess = false,
                        Error = cbsError
                    });
                    return;
                }

                var functionResult = onGet.GetResult<FunctionTasksResult<CBSTask>>();
                var achievementsList = functionResult.Tasks ?? new List<CBSTask>();

                result?.Invoke(new CBSGetAchievementsTableResult
                {
                    IsSuccess = true,
                    AchievementsData = new AchievementsData
                    {
                        Tasks = achievementsList
                    }
                });
            }, onFailed =>
            {
                result?.Invoke(new CBSGetAchievementsTableResult
                {
                    IsSuccess = false,
                    Error = CBSError.FromTemplate(onFailed)
                });
            });
        }
    }

    public class CBSStartTechStudyResult : CBSBaseResult
    {
        public CBSTask Achievement;
        public List<Dependency> FailedDependencies;
    }

    [Serializable]
    public class FunctionActiveStudyResult
    {
        public bool IsActive;
    }
}