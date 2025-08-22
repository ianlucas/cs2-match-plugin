# CS2 Match Plugin

> A [CounterStrikeSharp](https://docs.cssharp.dev) plugin for Counter-Strike 2 matches

## Installation

1. Install the latest release of [Metamod and CounterStrikeSharp](https://docs.cssharp.dev/docs/guides/getting-started.html).
2. [Download the latest release](https://github.com/ianlucas/cs2-match-plugin/releases) of CS2 Match Plugin.
3. Extract the ZIP file contents into `addons/counterstrikesharp`.

## Quick Start

### 1. Basics

#### `match_chat_prefix` ConVar

- Prefix for chat messages.
- **Type:** `string`
- **Default:** `[{red}Match{default}]`

#### `match_bots` ConVar

- Bots join the game to fill slots. Not recommended to be enabled.
- **Type:** `bool`
- **Default:** `false`

#### `get5_server_id` ConVar

- A string that identifies your server.
- **Type:** `string`
- **Default:** _empty_

### 2. Server Setup

#### `match_matchmaking` ConVar

- Matchmaking mode. Players not assigned to a team will be kicked from the server (unless they are administrators).
- **Type:** `bool`
- **Default:** `false`

#### `match_matchmaking_ready_timeout` ConVar

- Time to players ready up. Requires `match_matchmaking` to be `true`. Players will be kicked if they don't ready up in time.
- **Type:** `int`
- **Default:** `300`

#### `get5_remote_log_url` ConVar

- The URL to send all events to.
- **Type:** `string`
- **Default:** _empty_

#### `get5_remote_log_header_key` ConVar

- Key of the header sent on remote log request.
- **Type:** `string`
- **Default:** _empty_

#### `get5_remote_log_header_value` ConVar

- Value of the header sent on remote log request.
- **Type:** `string`
- **Default:** _empty_

### 3. Match CSTV Setup

#### `match_tv_record` ConVar

- Are we recording demos?
- **Type:** `bool`
- **Default:** `true`

#### `match_tv_delay` ConVar

- CSTV's broadcast delay (in seconds).
- **Type:** `int`
- **Default:** `105`

### 4. Match Setup

#### `match_players_needed` ConVar

- Number of players needed for a match.
- **Type:** `int`
- **Default:** `10`

#### `match_players_needed_per_team` ConVar

- Number of players needed per team.
- **Type:** `int`
- **Default:** `5`

#### `match_max_rounds` ConVar

- Max number of rounds to play.
- **Type:** `int`
- **Default:** `24`

#### `match_ot_max_rounds` ConVar

- Additional rounds to determine winner.
- **Type:** `int`
- **Default:** `6`

#### `match_friendly_pause` ConVar

- Teams can pause at any time.
- **Type:** `bool`
- **Default:** `false`

#### `match_knife_round_enabled` ConVar

- Are knife rounds enabled?
- **Type:** `bool`
- **Default:** `true`

#### `match_knife_vote_timeout` ConVar

- Time to decide side (in seconds).
- **Type:** `int`
- **Default:** `60`

#### `match_forfeit_timeout` ConVar

- Time to forfeit a team (in seconds).
- **Type:** `int`
- **Default:** `60`

### 5. Optional

#### `match_verbose` ConVar

> [!IMPORTANT]
> Keeping this setting enabled makes debugging issues easier.

- Are we debugging the plugin?
- **Type:** `bool`
- **Default:** `true`

## Commands

### Admin

#### `match_status` Command

- Prints a status report of the plugin in the console. Requires `@css/config` permission.

#### `css_start` or `!start` Command

- Forcefully starts the match during warmup. Requires `@css/config` permission.

#### `css_map <mapname>` or `!map <mapname>` Command

- Changes the current map. Limited mapnames starting with `de_`. Requires `@css/config` permission.

#### `css_restart` or `!restart` Command

- Forcefully restarts a running match to warmup. Requires `@css/config` permission.

#### `match_load <filepath>` or `get5_loadmatch <filepath>` Command

- Loads a [Get5 match configuration file (JSON)](https://splewis.github.io/get5/latest/match_schema) relative to the `csgo/addons/counterstrikesharp/config/plugins/MatchPlugin` or `csgo` directories. Requires `@css/config` permission.

#### `css_restore <round>` or `!restore <round>` Command

- Tries to restore a round in a live match. Requires `@css/config` permission.

### Get5 Match Schema

#### Differences

Not all properties from Get5 Match Schema are being used, check the source code for the `match_load` command.

##### `SteamID` String

We only support 64-bit SteamIDs, e.g. `76561197960287930`.

##### `Get5MatchTeam` Object

- `leaderid` (`string`) property has been added. It's the `SteamID` for the in-game leader of the team. If absent, the plugin will elect the first player as the team in-game leader.

#### Minimal Example

```json
{
  "matchid": "12345",
  "maplist": ["de_train", "de_dust2", "de_inferno"],
  "team1": {
    "name": "Team 1",
    "players": {
      "12345": "Player 1",
      "12345": "Player 2",
      "12345": "Player 3",
      "12345": "Player 4",
      "12345": "Player 5"
    }
  },
  "team2": {
    "name": "Team 2",
    "players": {
      "12345": "Player 6",
      "12345": "Player 7",
      "12345": "Player 8",
      "12345": "Player 9",
      "12345": "Player 10"
    }
  },
  "cvars": {
    "match_matchmaking": "true"
  }
}
```

### Get5 Events

The plugin has compatibility with most Get5 events. Once you setup `get5_remote_log_url` ConVar with a URL, the plugin will send events to it. You can refer to the events at [Get5 Events & Forwards](https://splewis.github.io/get5/latest/events.html).

Not all events are implemented, and some events may have some differences, so please check below.

#### Missing events

- `OnMapPicked`
- `OnMapVetoed`
- `OnDemoUploadEnded`

#### `Get5Player` Object

- `user_id` property may be `null`.
- `ping` (`number | null`) property has been added.

#### `Get5StatsTeam` Object

- `ping` (`number | null`) property has been added.

#### `OnGameStateChanged` Event

- `new_state` and `old_state` properties can't have `pre_veto`, `veto`, `going_live` and `post_game` states. The lifecycle of the plugin follows this order: `none` → `warmup` → `knife` → `waiting_for_knife_decision` → `live` → `none`.

#### `OnMapResult` Event

- `winner` property may be `null`.
- `result` (`number`) property has been added.
  - `0` is `MapResult.None`;
  - `1` is `MapResult.Completed`;
  - `2` is `MapResult.Cancelled`;
  - `3` is `MapResult.Forfeited`.

#### `OnSeriesResult` Event

- `winner` property may be `null`.
- `last_map_number` (`number`) property has been added.
