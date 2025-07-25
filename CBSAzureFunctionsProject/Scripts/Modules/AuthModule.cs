using PlayFab.ServerModels;
using PlayFab.GroupsModels;
using PlayFab.DataModels;
using PlayFab.Samples;
using PlayFab.AuthenticationModels;
using PlayFab.MultiplayerModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CBS.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using CBS.SharedData.Lootbox;
using Microsoft.Azure.Functions.Worker;
using PlayFab.CloudScriptModels;
using Microsoft.Azure.Functions.Worker.Http;

namespace CBS
{
    public class AuthModule : BaseAzureModule
    {
        private static ILogger<AuthModule>? Logger;

        public AuthModule(ILogger<AuthModule> logger)
        {
            Logger = logger;
        }

        [Function(AzureFunctions.PostAuthMethod)]
        public static async Task<dynamic> GetPostAuthDataTrigger([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var request = context.GetRequest<FunctionPostLoginRequest>();

            var profileID = request.ProfileID;
            var preloadLevel = request.PreloadPlayerLevel;
            var preloadAccount = request.PreloadAccountData;
            var preloadClan = request.PreloadClan;
            var newCreated = request.NewlyCreated;
            var autoGenerateDisplayName = request.AuthGenerateName;
            var namePrefix = request.RandomNamePrefix;
            var newPlayerChecker = request.NewPlayerChecker;
            var loadItems = request.LoadItems;
            
            var generatedName = string.Empty;
            var level = new LevelInfo();
            var avatarID = string.Empty;
            var currencyPacksIDs = new List<string>();
            var calendarItemsIDs = new List<string>();
            var battlePassItemsIDs = new List<string>();
            var recipeContainer = new CBSRecipeContainer();
            var lootboxTable = new CBSLootboxTable();
            var upgradesContainer = new CBSItemUpgradesContainer();
            var chatProfileData = new ProfileChatData();
            
            ClanEntity clanEntity = null;
            var clanID = string.Empty;
            var clanRoleID = string.Empty;

            if (newPlayerChecker == SharedData.NewlyCreatedCheck.PROFILE_DATA_PROPERTY)
            {
                var getCheckinResult = await GetProfileInternalRawData(profileID, ProfileDataKeys.RegistrationCheckin);
                if (getCheckinResult.Error != null)
                {
                    return ErrorHandler.ThrowError(getCheckinResult.Error).AsFunctionResult();
                }
                var checkinRawData = getCheckinResult.Result;
                if (string.IsNullOrEmpty(checkinRawData))
                {
                    newCreated = true;
                    var saveCheckinResult = await SaveProfileInternalDataAsync(profileID, ProfileDataKeys.RegistrationCheckin, true.ToString());
                    if (saveCheckinResult.Error != null)
                    {
                        return ErrorHandler.ThrowError(saveCheckinResult.Error).AsFunctionResult();
                    }
                }
                else
                {
                    newCreated = false;
                }
            }

            if (newCreated)
            {
                await RewardModule.GrantRegistrationRewardAsync(profileID);
            }

            var getClanIDResult = await ProfileModule.GetProfileClanIDAsync(profileID);
            if (getClanIDResult.Error == null)
            {
                clanID = getClanIDResult.Result;
                if (!string.IsNullOrEmpty(clanID))
                {
                    var getRoleResult = await ClanModule.GetMemberRoleIDAsync(profileID);
                    if (getRoleResult.Error != null)
                    {
                        return ErrorHandler.ThrowError(getRoleResult.Error).AsFunctionResult();
                    }
                    clanRoleID = getRoleResult.Result;
                }
            }

            if (newCreated)
            {
                var accountRequest = new GetUserAccountInfoRequest
                {
                    PlayFabId = profileID
                };
                var getAccountInfoResult = await FabServerAPI.GetUserAccountInfoAsync(accountRequest);
                if (getAccountInfoResult.Error == null)
                {
                    var lastDisplayName = getAccountInfoResult.Result.UserInfo.TitleInfo.DisplayName;
                    if (!string.IsNullOrEmpty(lastDisplayName))
                    {
                        var nameResult = await ProfileModule.SetProfileDisplayNameAsync(profileID, lastDisplayName);
                        if (nameResult.Error != null)
                        {
                            return ErrorHandler.ThrowError(nameResult.Error).AsFunctionResult();
                        }
                    }
                    generatedName = lastDisplayName;
                }
                if (string.IsNullOrEmpty(generatedName) && autoGenerateDisplayName)
                {
                    var randomName = NicknameHelper.GenerateRandomName(namePrefix);
                    var nameResult = await ProfileModule.SetProfileDisplayNameAsync(profileID, randomName);
                    if (nameResult.Error != null)
                    {
                        return ErrorHandler.ThrowError(nameResult.Error).AsFunctionResult();
                    }
                    generatedName = nameResult.Result.DisplayName;
                }
            }

            if (preloadLevel)
            {
                var levelResult = await ProfileExpModule.GetProfileExpirienceDetailAsync(profileID);
                if (levelResult.Error == null)
                {
                    level = levelResult.Result;
                }
            }

            if (preloadAccount)
            {
                var avatarResult = await ProfileModule.GetProfileAvatarIDAsync(profileID);
                avatarID = avatarResult.Result;
            }

            if (preloadClan && !string.IsNullOrEmpty(clanID))
            {
                var getClanEntityResult = await ClanModule.GetClanEntityAsync(clanID, CBSClanConstraints.Full());
                if (getClanEntityResult.Error == null)
                {
                    clanEntity = getClanEntityResult.Result.ClanEntity;
                }
            }

            // get currencies pack
            var currencyPacksResult = await CurrencyModule.GetCurrenciesPacksAsync();
            if (currencyPacksResult.Error == null)
            {
                var packs = currencyPacksResult.Result.Items;
                currencyPacksIDs = packs.Select(x=>x.ItemId).ToList();
            }

            // get calendar catalog
            var calendarCatalogResult = await CalendarModule.GetCalendarCatalogItemsAsync();
            if (calendarCatalogResult.Error == null)
            {
                var calendarItems = calendarCatalogResult.Result.Items;
                calendarItemsIDs = calendarItems.Select(x=>x.ItemId).ToList();
            }

            // get battle pass catalog
            var ticketsCatalogResult = await BattlePassModule.GetTicketsCatalogItemsAsync();
            if (ticketsCatalogResult.Error == null)
            {
                var ticketsItems = ticketsCatalogResult.Result.Items;
                battlePassItemsIDs = ticketsItems.Select(x=>x.ItemId).ToList();
            }

            var getChatProfileDataResult = await ChatModule.GetProfileChatDataAsync(profileID);
            if (getChatProfileDataResult.Error == null)
            {
                chatProfileData = getChatProfileDataResult.Result;
            }

            // fetch items
            GetCatalogItemsResult itemsResult = null;
            if (loadItems == SharedData.LoadCatalogItems.SINGLE_CALL)
            {
                var getItemsResult = await ItemsModule.GetCatalogItemsAsync();
                itemsResult = getItemsResult.Result;
            }
            
            // fetch items categories
            var categoriesResult = await ItemsModule.GetItemsCategoriesRawDataAsync();
            // fetch items meta data
            var getMetaResult = await ItemsModule.GetItemsMetaDataAsync();
            if (getMetaResult.Error == null)
            {
                recipeContainer = getMetaResult.Result.Recipes;
                upgradesContainer = getMetaResult.Result.Upgrades;
                lootboxTable = getMetaResult.Result.LootboxTable;
            }

            var result = new FunctionPostLoginResult
            {
                ProfileID = profileID,
                DisplayName = generatedName,
                ClanID = clanID,
                ClanRoleID = clanRoleID,
                ItemsResult = itemsResult?.ToClientInstance(),
                CategoriesResult = categoriesResult.Result.CategoriesData,
                PlayerLevelInfo = level,
                AvatarID = avatarID,
                CurrencyPacksIDs = currencyPacksIDs,
                CalendarCatalogIDs = calendarItemsIDs,
                TicketsCatalogIDs = battlePassItemsIDs,
                Recipes = recipeContainer,
                Upgrades = upgradesContainer,
                ProfileChatData = chatProfileData,
                ClanEntity = clanEntity,
                LootboxTable = lootboxTable,
                OverridedNewPlayerValue = newCreated
            };

            return result.AsFunctionResult();
        }
    }
}