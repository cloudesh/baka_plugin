# Baka Free Company

A modern parchment-style Dalamud plugin for **Shelter on Raiden**. Open `/baka` to see the online roster, each member's job and location, and game-reported activity such as duties, queues, cutscenes, or being AFK.

## Install in Dalamud

1. Open Dalamud Settings (`/xlsettings`) → **Experimental** → **Custom Plugin Repositories**.
2. Add and enable:
   ```text
   https://github.com/cloudesh/baka_plugin/releases/latest/download/repo.json
   ```
3. Save, open `/xlplugins`, find **Baka Free Company**, and install it.
4. Open the game's **Free Company → Members** tab, then run `/baka`.

The repository URL becomes live when the first tagged GitHub release succeeds. Updates use the same URL.

## Roster behavior

The panel reads the game's Free Company roster, not the nearby object list. It accepts only **Shelter / Raiden**, checks that the company and member proxies refer to the same FC, and copies native data on the framework thread. No roster data is uploaded anywhere.

Member requests are limited to once every 30 seconds while the panel is open. The game's Members tab may need to be opened before data is available. Counts and activities reflect the last native roster response; they are not guaranteed real-time. Incomplete rosters are marked, and unavailable locations are shown explicitly. A job does not prove someone is actively crafting or gathering, and the game may not disclose an exact duty name.

`/baka` toggles the window. `/baka refresh` requests an update, respecting the cooldown.

## Build and release

Requires .NET SDK 10 and Dalamud API 15. Download the stable developer distribution from `https://goatcorp.github.io/dalamud-distrib/latest.zip`, extract it, and set `DALAMUD_HOME` to that directory. XIVLauncher users can use their local `addon/Hooks/dev` directory.

```sh
dotnet restore DalamudPluginProjectTemplate.csproj --locked-mode
dotnet build DalamudPluginProjectTemplate.csproj --no-restore -c Release
dotnet restore tests/BakaPlugin.Tests.csproj --locked-mode
dotnet test tests/BakaPlugin.Tests.csproj --no-restore -c Release
python scripts/package.py --tag v0.1.0.0
```

Push a `v<Version>` tag matching the project version to build, test, package, and publish through GitHub Actions. The workflow uploads `BakaPlugin.zip`, `repo.json`, and `SHA256SUMS` to a GitHub release. The package validator rejects mismatched tags, manifests, or DLLs. No manually supplied repository token is needed by the workflow.

Automated tests cover identity, offline filtering, combined statuses, duplicate rows, search, and incomplete data. In-game UI and native-data checks are in [the test checklist](docs/IN_GAME_TEST.md).
