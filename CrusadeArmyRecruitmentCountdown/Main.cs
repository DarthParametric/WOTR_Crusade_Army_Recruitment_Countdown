using CrusadeArmyRecruitmentCountdown.Events;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Kingdom;
using Kingmaker.PubSubSystem;
using Kingmaker.Settings;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using UnityModManagerNet;

namespace CrusadeArmyRecruitmentCountdown;

public static class Main
{
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger log;
    private static OnDayChanged m_day_changed_handler;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        log = modEntry.Logger;
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        m_day_changed_handler = new OnDayChanged();
        EventBus.Subscribe(m_day_changed_handler);
        return true;
    }

    public static void LogDebug(string message)
    {
#if DEBUG
        log.Log($"DEBUG: {message}");
#endif
    }

    // Stolen from https://stackoverflow.com/a/40940304
    /// <summary>
    /// Pluralize: takes a word, inserts a number in front, and makes the word plural if the number is not exactly 1.
    /// </summary>
    /// <example>"{n.Pluralize("maid")} a-milking</example>
    /// <param name="word">The word to make plural</param>
    /// <param name="number">The number of objects</param>
    /// <param name="pluralSuffix">An optional suffix; "s" is the default.</param>
    /// <param name="singularSuffix">An optional suffix if the count is 1; "" is the default.</param>
    /// <returns>Formatted string: "number word[suffix]", pluralSuffix (default "s") only added if the number is not 1, otherwise singularSuffix (default "") added</returns>
    internal static string Pluralize(this int number, string word, string pluralSuffix = "s", string singularSuffix = "")
    {
        return $@"{number} {word}{(number != 1 ? pluralSuffix : singularSuffix)}";
    }

    public static void AddRecruitmentCounter()
    {
        if (!KingdomState.Founded || (bool)(SettingsEntity<bool>)SettingsRoot.Difficulty.AutoCrusade)
        {
            LogDebug("AddRecruitmentCounter: Invalid crusade state, skipping counter.");
            return;
        }

        var Time = Game.Instance.TimeController.GameTime;
        var LastGrouth = KingdomState.Instance.RecruitsManager.LastGrowthTime;
        var DayCount = (int)(Time - LastGrouth).TotalDays;
        var DaysRemain = Math.Abs(DayCount - 7);
        string sMsg;
        UnityEngine.Color MsgColour;

        LogDebug($"AddRecruitmentCounter: \nCurrent Time = {BlueprintRoot.Instance.Calendar.GetDateText(Time, GameDateFormat.Full, true)} ({Time}) \nLast Recruitment Growth Time = {BlueprintRoot.Instance.Calendar.GetDateText(LastGrouth, GameDateFormat.Full, true)} ({LastGrouth}) \nCooldown Day Count = {DayCount} \nCooldown Days Remaining = {DaysRemain}");

        if (DaysRemain > 0)
        {
            MsgColour = new(0f, 0.157f, 0.494f);
            sMsg = $@"{DaysRemain.Pluralize("day")} remaining until Crusade army recruitment renews.";
        }
        else
        {
            MsgColour = new(0f, 0.50f, 0f);
            sMsg = $@"Crusade army recruitment is now available!";
        }

        CombatLogMessage message = new(sMsg, MsgColour, PrefixIcon.RightArrow, null, false);

        var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Common].First(x => x is MessageLogThread);

        messageLog.AddMessage(message);
    }
}
