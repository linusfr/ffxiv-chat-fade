using System;
using ChatFade.Windows;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ChatFade;

public sealed class Plugin : IDalamudPlugin
{
    // ── Injected services ─────────────────────────────────────────────────────
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog              Log             { get; private set; } = null!;
    [PluginService] internal static ICommandManager         CommandManager  { get; private set; } = null!;
    [PluginService] internal static IFramework              Framework       { get; private set; } = null!;
    [PluginService] internal static IChatGui                ChatGui         { get; private set; } = null!;
    [PluginService] internal static ICondition              Condition       { get; private set; } = null!;
    [PluginService] internal static IGameGui                GameGui         { get; private set; } = null!;
    [PluginService] internal static IKeyState               KeyState        { get; private set; } = null!;

    private const string Cmd = "/chatfade";

    internal Configuration Config { get; }

    private readonly ChatLog             _chat;
    private readonly ConfigurationWindow _window;

    private DateTime _lastActivity = DateTime.UtcNow;

    public Plugin()
    {
        Config  = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        _chat   = new ChatLog(GameGui, Log);
        _window = new ConfigurationWindow(this);

        CommandManager.AddHandler(Cmd, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Chat Fade settings. \"/chatfade toggle\" turns the fading off and on.",
        });

        ChatGui.ChatMessage                    += OnChatMessage;
        Framework.Update                       += OnUpdate;
        PluginInterface.UiBuilder.Draw         += _window.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfig;
        PluginInterface.UiBuilder.OpenMainUi   += OnOpenConfig;

        Log.Info("ChatFade: Plugin loaded.");
    }

    /// <summary>Anything worth seeing resets the clock.</summary>
    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (!Config.TalkOnly || Activity.IsTalk((XivChatType)message.LogKind))
            Wake();
    }

    /// <summary>Brings the log back to full and starts the quiet time over.</summary>
    internal void Wake() => _lastActivity = DateTime.UtcNow;

    /// <summary>Seconds the log has been quiet, for the settings window.</summary>
    internal double QuietFor => (DateTime.UtcNow - _lastActivity).TotalSeconds;

    private long _loggedAt;
    private long _updatedAt = Environment.TickCount64;
    private float _shown = 1f;
    private float _frameSeconds;
    private bool _enterWasDown;

    /// <summary>The visibility currently applied, including a fade back in.</summary>
    internal float Shown => _shown;

    private void OnUpdate(IFramework framework)
    {
        var now = Environment.TickCount64;
        _frameSeconds = Math.Clamp((now - _updatedAt) / 1000f, 0f, 0.1f);
        _updatedAt = now;

        var enterDown = KeyState[VirtualKey.RETURN];
        var enterPressed = enterDown && !_enterWasDown;
        _enterWasDown = enterDown;

        if (now - _loggedAt >= 1000)
        {
            _loggedAt = Environment.TickCount64;
            Log.Info($"ChatFade: enabled={Config.Enabled} hud={GameGui.GameUiHidden} quiet={QuietFor:0.0}s "
                     + $"typing={_chat.IsTyping()} verdict={Situations.Verdict(Config, Condition)} "
                     + $"target={Visibility(QuietFor):0.00}");
        }

        if (!Config.Enabled)
        {
            _shown = 1f;
            _chat.Restore();
            return;
        }

        // The whole HUD being off is someone else's decision, and a screenshot
        // or a cutscene is the worst moment to argue about alpha.
        if (GameGui.GameUiHidden)
            return;

        // A fully hidden chat addon cannot report its input becoming active,
        // so catch the key that opens it as well as the resulting input state.
        if (enterPressed)
        {
            Wake();
            Show(1f);
            return;
        }

        // Typing outranks everything: a text field is open, so the log is in
        // use whatever else is going on.
        if (_chat.IsTyping())
        {
            Wake();
            Show(1f);
            return;
        }

        switch (Situations.Verdict(Config, Condition))
        {
            case Rule.Hide:
                Show(0f, immediate: true);
                return;
            case Rule.KeepUp:
                Wake();
                Show(1f);
                return;
        }

        Show(Visibility(QuietFor));
    }

    /// <summary>Fades upward to a target; the timer already handles fading downward.</summary>
    private void Show(float target, bool immediate = false)
    {
        target = Math.Clamp(target, 0f, 1f);

        if (immediate || target <= _shown || !Config.FadeIn || Config.FadeSeconds <= 0f)
            _shown = target;
        else
            _shown = Math.Min(target, _shown + (_frameSeconds / Config.FadeSeconds));

        _chat.Want(_shown);
    }

    /// <summary>Where the fade has got to after <paramref name="quiet"/> seconds.</summary>
    internal float Visibility(double quiet)
    {
        var floor = Math.Clamp(Config.FadeToPercent, 0, 100) / 100f;
        var past  = quiet - Config.IdleSeconds;

        if (past <= 0)
            return 1f;
        if (!Config.FadeOut || Config.FadeSeconds <= 0f)
            return floor;

        var progress = (float)Math.Clamp(past / Config.FadeSeconds, 0d, 1d);
        return 1f - (progress * (1f - floor));
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "debug":
                ChatGui.Print($"[Chat Fade] quiet {QuietFor:0}s, typing={_chat.IsTyping()}, "
                              + $"verdict={Situations.Verdict(Config, Condition)}, {_chat.Describe()}");
                break;
            case "toggle":
                Config.Enabled = !Config.Enabled;
                SaveConfig();
                ChatGui.Print($"[Chat Fade] fading {(Config.Enabled ? "on" : "off")}.");
                break;
            default:
                _window.IsVisible = !_window.IsVisible;
                break;
        }
    }

    private void OnOpenConfig() => _window.IsVisible = true;

    internal void SaveConfig() => PluginInterface.SavePluginConfig(Config);

    public void Dispose()
    {
        ChatGui.ChatMessage                    -= OnChatMessage;
        Framework.Update                       -= OnUpdate;
        PluginInterface.UiBuilder.Draw         -= _window.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfig;
        PluginInterface.UiBuilder.OpenMainUi   -= OnOpenConfig;

        _chat.Dispose();
        CommandManager.RemoveHandler(Cmd);
    }
}
