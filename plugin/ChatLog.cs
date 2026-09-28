using System;
using System.Text;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ChatFade;

/// <summary>
/// The chat log addons, dimmed together. The main log and its detached panels
/// are separate windows to the game, so a fade has to reach all of them.
/// </summary>
internal sealed class ChatLog : IDisposable
{
    private static readonly string[] Addons =
    {
        "ChatLog",
        "ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3",
    };

    private readonly IGameGui _gui;
    private readonly IPluginLog _log;

    /// Only ever un-hide what this plugin hid: the game hides chat for its own
    /// reasons — cutscenes, /chatdisplay off — and those must stick.
    private bool _hidden;

    private byte _wanted = byte.MaxValue;

    /// Throttles the frame-by-frame logging to something readable.
    private long _loggedAt;

    internal ChatLog(IGameGui gui, IPluginLog log)
    {
        _gui = gui;
        _log = log;
    }

    /// <summary>True while the cursor is in a text field anywhere in the UI.</summary>
    internal unsafe bool IsTyping()
    {
        var rapture = RaptureAtkModule.Instance();
        if (rapture is not null && rapture->IsTextInputActive())
            return true;

        var stage = AtkStage.Instance();
        var chat = (AddonChatLog*)_gui.GetAddonByName("ChatLog").Address;
        if (chat is not null)
        {
            // Opening chat makes the game show the addon again. Notice that
            // transition before Want() re-hides it on this framework tick.
            if (_hidden && chat->AtkUnitBase.IsVisible)
                return true;

            if (chat->TextInput is not null
                && chat->TextInput->AtkComponentInputBase.IsActive)
                return true;

            var focus = stage is not null ? stage->GetFocus() : null;
            var inputNode = chat->TextInput is not null
                ? chat->TextInput->AtkComponentInputBase.AtkComponentBase.GetAtkResNode()
                : null;
            while (focus is not null)
            {
                if (focus == inputNode)
                    return true;
                focus = focus->ParentNode;
            }
        }

        return stage is not null && stage->AtkInputManager is not null
                                 && stage->AtkInputManager->IsTextInputActive;
    }

    /// <summary>
    /// Applies a visibility between 0 (gone) and 1 (untouched). Called every
    /// frame: the chat log rewrites its own transparency whenever it feels like
    /// it, so the fade has to be re-stated rather than set once.
    /// </summary>
    internal unsafe void Want(float visibility)
    {
        _wanted = (byte)Math.Clamp((int)MathF.Round(Math.Clamp(visibility, 0f, 1f) * 255f), 0, 255);
        var hide = _wanted == 0;
        var talk = Environment.TickCount64 - _loggedAt >= 1000;

        foreach (var name in Addons)
        {
            var addon = (AtkUnitBase*)_gui.GetAddonByName(name).Address;
            if (addon is null)
                continue;

            var root = addon->RootNode;
            var foundUnit = addon->Alpha;
            var foundRoot = root is not null ? root->Color.A : (byte)0;

            if (hide)
            {
                addon->IsVisible = false;
            }
            else
            {
                if (_hidden)
                    addon->IsVisible = true;

                addon->SetAlpha(_wanted);

                // The unit's own alpha is not always what reaches the screen,
                // so the root node — which every other node draws through —
                // gets it too.
                if (root is not null)
                    root->Color.A = _wanted;
            }

            if (!talk)
                continue;

            _log.Info($"ChatFade: {name} want={_wanted} found unit={foundUnit} root={foundRoot} "
                      + $"now unit={addon->Alpha} root={(root is not null ? root->Color.A : 0)} "
                      + $"visible={addon->IsVisible}");
        }

        if (talk)
            _loggedAt = Environment.TickCount64;

        _hidden = hide;
    }

    /// <summary>Puts everything back the way the game had it.</summary>
    internal void Restore() => Want(1f);

    /// <summary>What the addons actually report, for "/chatfade debug".</summary>
    internal unsafe string Describe()
    {
        var report = new StringBuilder($"want {_wanted}");

        foreach (var name in Addons)
        {
            var addon = (AtkUnitBase*)_gui.GetAddonByName(name).Address;
            if (addon is null)
                continue;

            var root = addon->RootNode;
            report.Append($"; {name}: visible={addon->IsVisible} alpha={addon->Alpha}");
            if (root is not null)
                report.Append($" root={root->Color.A}");
        }

        return report.ToString();
    }

    public void Dispose() => Restore();
}
