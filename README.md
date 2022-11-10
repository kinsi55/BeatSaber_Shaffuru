# Shaffuru
### シャッフル | Shuffle in Japanese

Endless Mode evolved. You pick a duration to play for and it keeps feeding you songs that you have downloaded in a random order.

### But there is more!

- You can filter the songs that should be in the map pool in various ways (Using a Playlist, Using NPS / NJS Limits and more!)
- You can make songs start and end at a random point instead of playing through its entirety! This was formerly called CHAOSMOD and is what this mod started out as
- You can accept requests from Twitch chat for songs to be played, and if you want to, even allow a specific difficulty (And Start time) to be requested - The request still has to match your configured filters.
- Compatible with JDFixer / NJSFixer preferences. Ideally you should set those up so you dont play with the same JD on slow and fast songs
- People can request maps that you don't have downloaded ***and they will be downloaded and queued while you are playing!***
- Multiplayer support via BeatTogher + MultiplayerCore!

## Install

For now - **Make sure to not use any mods that disable score submission** as that will softlock you once the level ends. I am not sure why yet. One of those mods would be "PlayFirst".

#### You can always find the latest Download in [the releases](https://github.com/kinsi55/BeatSaber_Shaffuru/releases)

### Requirements (All Availalbe in ModAssistant)

- **SongCore**
- **SiraUtil**
- **BeatSaberMarkupLanguage**
- **SongDetailsCache**
- **CatCore** or **ChatCore** (Optional, only needed if you want Chat requests to work)
- **BeatSaberPlaylistsLib** (Optional, only needed for playlist filtering)

## How to Play

Click the Shaffuru Mod Button in the Main Menu, configure the Settings as you please and then click the Play button at the bottom. It will tell you how many maps you have that fit your configured options, give you an option to pick how long you want to play and off you go.

#### Chat Requests

As mentioned before, this can be used completely offline, but if you are streaming and want to take song requests from chat theres a builtin queue.
Songs can be requested with `!sr [bsr id] ([Difficulty]) ([Time])`. Difficulty and Time are both optional and are ignored if not permitted to be picked, so examples on how to queue a Song would be `!sr 25f ExpertPlus 4:20`, `!sr 25f 3:20` or just `!sr 25f`. Depending on the settings, difficulty and start time are automatically / randomly picked if not explicity given.

For these to work, as mentioned before, you need to have CatCore installed and setup. Additionally, songs that are requested must currently already be downloaded.

## Multiplayer

To play Shaffuru in Multiplayer there is a couple of prerequisites for you as well as everyone else playing:

- You need the [BeatTogether](https://github.com/BeatTogether/BeatTogether#requirements) and [MultiplayerCore](https://github.com/Goobwabber/MultiplayerCore#installation) plugins
- You need the seperate Shaffuru.Multiplayer plugin from the [Releases](#Install)
- The person hosting the Multiplayer Lobby [must be a supporter of mine](https://github.com/sponsors/kinsi55)
	- Getting whitelisted of Shaffuru is currently a manual process - Join the Discord linked on the Sponsors page and let me know if you have sponsored me 😀👍
- Chat-Requests made in non-host players of the lobby are only respected if they themselves are also a supporter of mine

Other than that, the Multiplayer experience is supposed to be exactly the same as Solo, song filtering, requests, downloads, you name it, everything is there!

## Limitations

- If a map has any requirement *other than Mapping Extensions* it cannot be played
- Score submission obviously isnt gonna happen
- Some Counters / Mods dont like this like the "Notes left" and "PB" display, they'll just display garbage
