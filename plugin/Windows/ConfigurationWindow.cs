using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace ChatFade.Windows;

public sealed class ConfigurationWindow
{
    private static readonly Vector4 Muted = new(0.6f, 0.6f, 0.6f, 1f);

    private readonly Plugin _plugin;
    private Configuration Config => _plugin.Config;

    private bool _isVisible;
    public bool IsVisible { get => _isVisible; set => _isVisible = value; }

    public ConfigurationWindow(Plugin plugin) => _plugin = plugin;

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(420, 380), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Chat Fade###ChatFadeSettings", ref _isVisible))
        {
            ImGui.End();
            return;
        }

        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Fade the chat log when it goes quiet", ref enabled))
        {
            Config.Enabled = enabled;
            _plugin.SaveConfig();
        }

        ImGui.Spacing();

        var idle = Config.IdleSeconds;
        if (ImGui.SliderFloat("Quiet time", ref idle, 3f, 120f, "%.0f s"))
        {
            Config.IdleSeconds = idle;
            _plugin.SaveConfig();
        }
        Hint("How long nothing is said before the log starts going.");

        var fadeOut = Config.FadeOut;
        if (ImGui.Checkbox("Animate fade out", ref fadeOut))
        {
            Config.FadeOut = fadeOut;
            _plugin.SaveConfig();
        }

        var fadeIn = Config.FadeIn;
        if (ImGui.Checkbox("Animate fade in", ref fadeIn))
        {
            Config.FadeIn = fadeIn;
            _plugin.SaveConfig();
        }

        var fade = Config.FadeSeconds;
        if (ImGui.SliderFloat("Fade length", ref fade, 0f, 5f, "%.1f s"))
        {
            Config.FadeSeconds = fade;
            _plugin.SaveConfig();
        }

        var floor = Config.FadeToPercent;
        if (ImGui.SliderInt("Fade to", ref floor, 0, 80, "%d%%"))
        {
            Config.FadeToPercent = floor;
            _plugin.SaveConfig();
        }
        Hint(floor == 0
            ? "Gone entirely, so it stops catching clicks."
            : "A ghost of the log stays behind.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted("While you are");
        Hint("Hiding wins where two of these overlap.");

        if (ImGui.BeginTable("##situations", 2, ImGuiTableFlags.SizingStretchProp))
        {
            foreach (var situation in ChatFade.Situations.All)
                DrawRule(situation);

            ImGui.EndTable();
        }

        ImGui.Spacing();
        var talkOnly = Config.TalkOnly;
        if (ImGui.Checkbox("Only conversation counts", ref talkOnly))
        {
            Config.TalkOnly = talkOnly;
            _plugin.SaveConfig();
        }
        Hint(talkOnly
            ? "Damage, loot and crafting lines do not hold the log open."
            : "Every message counts, including battle spam.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var quiet = _plugin.QuietFor;
        ImGui.TextColored(Muted,
            $"Quiet for {quiet:0}s — showing at {_plugin.Shown * 100:0}%.");
        ImGui.TextColored(Muted, "New conversation and typing fade it back in.");

        ImGui.End();
    }

    private static readonly string[] Rules = { "follow the timer", "keep chat up", "hide chat" };

    private void DrawRule(Situation situation)
    {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(ChatFade.Situations.Label(situation));

        ImGui.TableNextColumn();
        var current = (int)Config.RuleFor(situation);
        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.Combo($"##rule{situation}", ref current, Rules, Rules.Length)) return;

        Config.SetRule(situation, (Rule)current);
        _plugin.SaveConfig();
    }

    private static void Hint(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Muted);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }
}
