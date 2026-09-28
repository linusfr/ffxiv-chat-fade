using System;
using System.Collections.Generic;

using Dalamud.Configuration;

namespace ChatFade;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;

    public bool Enabled { get; set; } = true;

    /// Quiet time before the log starts fading.
    public float IdleSeconds { get; set; } = 20f;

    /// How long the fade itself takes. Zero jumps straight to the end.
    public float FadeSeconds { get; set; } = 1.5f;

    public bool FadeOut { get; set; } = true;

    public bool FadeIn { get; set; } = true;

    /// Where the fade stops, in percent. Zero hides the log outright, which
    /// also stops it swallowing clicks; anything higher leaves a ghost.
    public int FadeToPercent { get; set; }

    /// Battle spam, loot rolls and crafting messages arrive constantly, so by
    /// default only something a person said counts as the chat being in use.
    public bool TalkOnly { get; set; } = true;

    /// What each situation does to the fade. Combat holds the log open because
    /// that is where being told to move matters; a cutscene puts it away.
    public Dictionary<Situation, Rule> Rules { get; set; } = new()
    {
        [Situation.Combat] = Rule.KeepUp,
        [Situation.Cutscene] = Rule.Hide,
    };

    public Rule RuleFor(Situation situation) =>
        Rules is not null && Rules.TryGetValue(situation, out var rule) ? rule : Rule.Timer;

    public void SetRule(Situation situation, Rule rule)
    {
        Rules ??= new Dictionary<Situation, Rule>();
        Rules[situation] = rule;
    }
}
