using Dalamud.Game.Text;

namespace ChatFade;

/// <summary>Which chat messages mean the log is being used.</summary>
internal static class Activity
{
    /// <summary>
    /// True for anything a person typed or emoted. Damage, loot, crafting and
    /// system lines are deliberately out: they arrive during normal play and
    /// would hold the log open forever.
    /// </summary>
    internal static bool IsTalk(XivChatType type) => (type & (XivChatType)0x7F) switch
    {
        XivChatType.Say or XivChatType.Shout or XivChatType.Yell => true,
        XivChatType.TellIncoming or XivChatType.TellOutgoing => true,
        XivChatType.Party or XivChatType.CrossParty or XivChatType.Alliance => true,
        XivChatType.FreeCompany or XivChatType.FreeCompanyAnnouncement => true,
        XivChatType.NoviceNetwork or XivChatType.PvPTeam => true,
        XivChatType.CustomEmote or XivChatType.StandardEmote => true,
        XivChatType.Echo => true,
        >= XivChatType.Ls1 and <= XivChatType.Ls8 => true,
        XivChatType.CrossLinkShell1 => true,
        >= XivChatType.CrossLinkShell2 and <= XivChatType.CrossLinkShell8 => true,
        >= XivChatType.GmTell and <= XivChatType.GmNoviceNetwork => true,
        _ => false,
    };
}
