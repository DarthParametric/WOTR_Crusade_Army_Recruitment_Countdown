using CrusadeArmyRecruitmentCountdown.Events;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.Kingdom;
using Kingmaker.PubSubSystem;
using Kingmaker.Settings;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using System.Text;
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

		var Start = BlueprintRoot.Instance.Calendar.GetStartDate();
		var TimePF = Game.Instance.TimeController.GameTime;
		var TimePFFull = BlueprintRoot.Instance.Calendar.GetDateText(TimePF, GameDateFormat.Extended, false);
		var TimeReal = Start.AddYears(2700) + TimePF;
		var LastGrowthPF = KingdomState.Instance.RecruitsManager.LastGrowthTime;
		var LastGrowthPFFull = BlueprintRoot.Instance.Calendar.GetDateText(LastGrowthPF, GameDateFormat.Extended, false);
		var LastGrowthReal = Start.AddYears(2700) + LastGrowthPF;
		var NextGrowthPF = LastGrowthPF + TimeSpan.FromDays(7);
		var NextGrowthPFFull = BlueprintRoot.Instance.Calendar.GetDateText(NextGrowthPF, GameDateFormat.Extended, false);
		var NextGrowthReal = Start.AddYears(2700) + NextGrowthPF;
		var DayCount = (int)(TimePF - LastGrowthPF).TotalDays;
        var DaysRemain = Math.Abs(DayCount - 7);
		UnityEngine.Color MsgColour;
		string sMsg;
		CombatLogMessage message;

		if (DaysRemain > 0)
		{
			MsgColour = new(0f, 0.157f, 0.494f);
			sMsg = $@"{DaysRemain.Pluralize("day")} remaining until Crusade army recruitment renews.";
			string sPopUp = string.Empty;

			// First day of the week differences require an offset to report the correct day name.
			StringBuilder sPopTmp = GameLogUtility.StringBuilder;
			sPopTmp.Append($"Current Date: {TimePFFull} ({TimeReal - TimeSpan.FromDays(1):dddd}, {TimeReal:d MMMM, yyyy})");
			sPopTmp.AppendLine();
			sPopTmp.AppendLine();
			sPopTmp.Append($"Last Recruitment Renewal: {LastGrowthPFFull} ({LastGrowthReal - TimeSpan.FromDays(1):dddd}, {LastGrowthReal:d MMMM, yyyy})");
			sPopTmp.AppendLine();
			sPopTmp.AppendLine();
			sPopTmp.Append($"Next Recruitment Renewal: {NextGrowthPFFull} ({NextGrowthReal - TimeSpan.FromDays(1):dddd}, {NextGrowthReal:d MMMM, yyyy})");
			sPopTmp.AppendLine();
			sPopTmp.AppendLine();
			sPopTmp.Append($@"Remaining Time: {DaysRemain.Pluralize("Day")}");

			sPopUp = sPopTmp.ToString();
			sPopTmp.Clear();

			TooltipTemplateCombatLogMessage CountdownTooltip = new("Crusade Army Recruitment Cooldown", sPopUp);
			message = new(sMsg, MsgColour, PrefixIcon.RightArrow, CountdownTooltip, true);
		}
		else
		{
			MsgColour = new(0f, 0.50f, 0f);
			sMsg = "Crusade army recruitment is now available!";
			message = new(sMsg, MsgColour, PrefixIcon.RightArrow, null, false);
		}

		var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Common].First(x => x is MessageLogThread);

        messageLog.AddMessage(message);
    }
}
