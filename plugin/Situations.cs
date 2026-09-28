using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace ChatFade;

/// <summary>Things you might be doing that the fade should know about.</summary>
public enum Situation
{
    Combat,
    Duty,
    Cutscene,
    Crafting,
    Gathering,
    Fishing,
}

/// <summary>What a situation does to the fade.</summary>
public enum Rule
{
    /// <summary>Nothing special: the quiet timer decides.</summary>
    Timer,

    /// <summary>Hold the log open however quiet it gets.</summary>
    KeepUp,

    /// <summary>Put the log away at once, however busy chat is.</summary>
    Hide,
}

internal static class Situations
{
    internal static readonly Situation[] All =
    {
        Situation.Combat, Situation.Duty, Situation.Cutscene,
        Situation.Crafting, Situation.Gathering, Situation.Fishing,
    };

    private static readonly Dictionary<Situation, ConditionFlag[]> Flags = new()
    {
        [Situation.Combat] = new[] { ConditionFlag.InCombat },
        [Situation.Duty] = new[] { ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95 },
        [Situation.Cutscene] = new[]
        {
            ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78,
        },
        [Situation.Crafting] = new[] { ConditionFlag.Crafting, ConditionFlag.PreparingToCraft },
        [Situation.Gathering] = new[] { ConditionFlag.Gathering },
        [Situation.Fishing] = new[] { ConditionFlag.Fishing },
    };

    internal static string Label(Situation situation) => situation switch
    {
        Situation.Combat => "In combat",
        Situation.Duty => "In a duty",
        Situation.Cutscene => "In a cutscene",
        Situation.Crafting => "Crafting",
        Situation.Gathering => "Gathering",
        Situation.Fishing => "Fishing",
        _ => situation.ToString(),
    };

    internal static bool Active(Situation situation, ICondition condition)
    {
        foreach (var flag in Flags[situation])
            if (condition[flag])
                return true;

        return false;
    }

    /// <summary>
    /// The rule that wins right now. Hiding beats keeping the log up: it is the
    /// more deliberate of the two, and the pair only ever collide when two
    /// situations overlap, like a cutscene starting mid-fight.
    /// </summary>
    internal static Rule Verdict(Configuration config, ICondition condition)
    {
        var verdict = Rule.Timer;

        foreach (var situation in All)
        {
            var rule = config.RuleFor(situation);
            if (rule == Rule.Timer || !Active(situation, condition))
                continue;

            if (rule == Rule.Hide)
                return Rule.Hide;

            verdict = Rule.KeepUp;
        }

        return verdict;
    }
}
