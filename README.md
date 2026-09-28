<div align="center">
	<img src="images/icon.png" alt="Chat Fade icon" width="128">
	<h1>Chat Fade</h1>
	<p>The chat log goes away when the chat does.</p>
	<p>
		<a href="https://github.com/linusfr/ffxiv-chat-fade/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/linusfr/ffxiv-chat-fade/ci.yml?branch=main&amp;label=ci&amp;cacheSeconds=300" alt="CI status"></a>
		<a href="https://github.com/linusfr/ffxiv-chat-fade/releases/latest"><img src="https://img.shields.io/github/v/release/linusfr/ffxiv-chat-fade?label=release&amp;cacheSeconds=300" alt="Latest release"></a>
		<a href="LICENSE"><img src="https://img.shields.io/github/license/linusfr/ffxiv-chat-fade?color=blue" alt="MIT license"></a>
	</p>
</div>

An empty chat log is still a box on your screen. Chat Fade dims it out after a
quiet stretch and brings it back when someone talks or you start typing.

## Install

In `/xlsettings`, open **Experimental** > **Custom Plugin Repositories**, paste
this URL, click `+`, then save:

```text
https://raw.githubusercontent.com/linusfr/ffxiv-chat-fade/main/pluginmaster.json
```

Then open `/xlplugins`, search for **Chat Fade**, and select **Install**.

`/chatfade` opens the settings, `/chatfade toggle` turns the fading off and on,
`/chatfade debug` prints what the addons actually report.

## Settings

| Setting | Default | |
| --- | --- | --- |
| Quiet time | 20 s | Silence before the fade starts |
| Fade length | 1.5 s | How long either animation takes |
| Animate fade out | on | Fade away instead of disappearing immediately |
| Animate fade in | on | Fade back instead of appearing immediately |
| Fade to | 0% | Gone entirely, or a ghost left behind |
| Only conversation counts | on | Damage, loot and crafting lines do not hold it open |

Each situation — in combat, in a duty, in a cutscene, crafting, gathering,
fishing — follows the timer, keeps chat up, or hides it outright. Combat keeps
it up and cutscenes hide it by default; hiding wins where two overlap. Typing
anywhere in the UI beats all of them, so opening chat to reply never fights the
fade.

## How it works

The alpha is restated on every framework update because the chat log can rewrite
its own transparency. `ChatLog` and its detached panels are handled together,
since the game treats them as separate windows.

Nothing is ever un-hidden that the plugin did not hide itself — when the game
puts chat away for a cutscene or because you turned it off, that stands. At 0%
the addon is hidden outright rather than made transparent, so it stops catching
clicks.

## Development

```sh
just install   # builds and drops it into ~/.xlcore/devPlugins/ChatFade
```

## License

MIT, see [`LICENSE`](LICENSE).
