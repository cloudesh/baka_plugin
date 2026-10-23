# First in-game check

1. Add the repository URL from the README, install Baka Free Company, and log into a character in Shelter on Raiden.
2. Run `/baka`. Check that the panel reads **Baka Free Company**, with **SHELTER / RAIDEN** above it.
3. Click **Open Free Company**, choose **Members**, and compare online counts, names, jobs, and locations with the native window. If the roster is partial, confirm the notice appears and check additional native pages. No members outside the current zone should be excluded merely for being distant.
4. Check an online, AFK, in-duty, and queued member when available. A crafting job alone must not be labelled as actively crafting.
5. Search by name/job/location, switch filters, and check the no-results state. Test resizing and UI scaling at 100%, 150%, and 200%.
6. Close/reopen with `/baka`, refresh after the cooldown, and verify the UI updates when members log in/out. Data reflects the native roster; opening Members may be necessary to receive fresh server data.
7. View another FC's profile. The Baka panel must clear it instead of showing another company's roster.
8. Log out or switch characters: previous member data must clear. Unload/reload the plugin and check `/xllog` for errors.

The cloud environment cannot run FFXIV. These checks are deliberately not marked as passed by the release workflow.
