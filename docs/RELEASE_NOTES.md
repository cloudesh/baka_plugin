First in-game test release for **Shelter on Raiden**.

- Modern parchment UI, ink typography, gold rules, and a compact Baka seal.
- Online roster with job, location, and game-reported activity.
- Name/job/location/activity search and activity filters.
- Shelter/Raiden identity validation, partial-roster notices, and throttled refresh.
- `/baka` opens the window; `/baka refresh` requests an update.

Add this custom repository in Dalamud Settings → Experimental → Custom Plugin Repositories:

`https://github.com/cloudesh/baka_plugin/releases/latest/download/repo.json`

Install **Baka Free Company**, then open the game's **Free Company → Members** tab to populate the roster. The plugin reads the game's FC data, including members outside your current zone. Some data may be unavailable or stale; a partial roster is explicitly marked. Exact duties and activities are only shown when the game provides them.

Requires Dalamud API 15. Build and automated roster tests run in GitHub Actions. Native roster updates and visual behavior need validation in-game; see `docs/IN_GAME_TEST.md`.
