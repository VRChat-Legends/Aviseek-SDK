# AviSeek World SDK

A drop-in avatar search wall for VRChat worlds, powered by [AviSeek](https://aviseek.vrchatlegends.com). Players search more than 200,000 public avatars, filter by platform and performance rank, keep favorites, browse promoted avatars and groups, and switch avatars with one click.

## Install

**VRChat Creator Companion (recommended)**

1. Add the AviSeek listing: open [the listing page](https://vrchat-legends.github.io/Aviseek-SDK/) and press **Add to VCC**, or in the VCC go to Settings > Packages > Add Repository and paste `https://vrchat-legends.github.io/Aviseek-SDK/index.json`.
2. Open your world project with **Manage Project** and add **AviSeek World SDK**.

**By hand**

Download the `.zip` from the [latest release](https://github.com/VRChat-Legends/Aviseek-SDK/releases/latest) and unzip it into your project's `Packages/com.vrchatlegends.aviseek-world` folder.

## Add it to your world

1. Open **AviSeek > SDK Panel**. The Setup tab checks your scene and adds the wall for you.
2. Or drag **`Runtime/Prefabs/AviSeek Avatar Search.prefab`** into your scene, or use **AviSeek > Add Avatar Search Wall** (also under **GameObject > AviSeek**).
3. Turn it to face your spawn. The panels face the prefab's -Z side.

No API key, account link or setup is needed. It uses the public, read-only world API.

## The wall

The wall is about 5 metres wide and holds four panels, all flat on the wall. Each panel is a solid slab with a glowing rim, and the cards and buttons float off it with soft shadows.

| Panel | What it does |
| --- | --- |
| Socials | Your community links. Clicking a link opens the VRChat keyboard so players can copy it (they can not change it), and VRChat groups get an Open button that opens the group in game. |
| Search | Avatar or creator search, platform tabs, sort, performance rank, creator filter, favorites, paging and the selected avatar's details. |
| Results | Twelve avatar cards with pictures, platforms, ratings, a heart and a Use button. |
| Promoted | Avatars and groups promoted on AviSeek. Avatars can be viewed or worn, and groups open in game. |

The **Backdrop** child is a plain wall with glowing trim. Switch it off to hang the panels on your own wall.

## Make it yours

Select the wall to see its inspector, or use the Branding tab of the SDK Panel.

- **Title and highlighted word**: the heading on the Search panel, for example `AVATAR` and `SEARCH`.
- **Logo**: any sprite. Leave it empty to hide the logo.
- **Accent colour**: the buttons, glows, rims and wall trim. Custom colours write their materials to `Assets/AviSeek Branding`, so the package stays untouched.
- **Panels**: switch the Socials panel, the Promoted panel and the backdrop on or off. A hidden Promoted panel skips its download.

Select **Socials Panel** to edit the links. Each link has a label, address, icon, icon colour and an optional VRChat group id (`grp_...`). **Restore the VRChat Legends links** puts the default list back.

## What players get

- **Search by avatar or creator**: the Avatar and Creator buttons pick what the search box looks through. Players type after `q=` (avatar names) or `author=` (creator names) and press Enter. VRChat only lets worlds load web addresses that a player typed, so the box starts as the AviSeek address and the player adds their search to the end.
- **Popular list**: the home button reloads it. Paging past the end of a set of 120 loads the next set, for up to 10 sets (100 pages).
- **Filters**: All, PC, Quest or iOS, sort by popularity, name or creator, and performance rank. The rank filter uses the rating for the platform the player is on.
- **Filter by creator**: narrows whatever is on the wall, search results or favorites, to creators whose name contains the typed text. Clicking the creator line on the selected avatar fills it in.
- **Favorites**: the heart on any card or on the selected avatar. Up to 100 are saved with VRChat persistence (PlayerData) and load again on the player's next visit. The Favorites button shows the count and switches the wall to the saved list.
- **Details**: clicking a picture shows the avatar's ratings for each platform and its avatar ID. Clicking the ID opens the VRChat keyboard so players can copy it. Any typing is undone at once.
- **Use**: switches the player into the avatar through a hidden VRC Avatar Pedestal.
- **Promoted**: refreshed every 10 minutes. Searches always download first.

## Notes

- Players need **Allow Untrusted URLs** turned on in VRChat (Settings > Comfort & Safety) unless the AviSeek domain is on VRChat's trusted list. The wall says so when a download is blocked.
- Downloads are spaced at least 5.5 seconds apart, and the wall waits 30 seconds after an HTTP 429, as the API docs ask.
- Saved favorites keep their name, creator, platforms and ratings, but not their picture (PlayerData is too small). Pictures show once that avatar has loaded in a search, and selecting a favorite shows VRChat's own picture on the pedestal.
- Thumbnails arrive inside the search response as 64x64 images. When the API is still preparing them, the wall asks again a few times. A missing performance rating shows as Unknown and is never guessed.
- The selected avatar's pedestal also draws VRChat's own picture of the avatar over the details thumbnail. If it lands in the wrong spot in your world, move `Avatar Pedestal/Placement`.
- Searches, favorites and the promoted list all work in Play mode with ClientSim. Avatar switching and group pages only work in VRChat.

## For maintainers

The prefab, icon fonts, panel meshes and sprites are all generated by `Editor/AviSeekPrefabBuilder.cs`. Change the layout there and run **AviSeek > Maintainers > Rebuild Prefab**, instead of editing the prefab by hand.

Scripts are UdonSharp, in `Runtime/`:

- `AviSeekSearch` runs the wall, the download queue and favorites.
- `AviSeekCard` draws one result.
- `AviSeekPromoted` fills the Promoted panel.
- `AviSeekSocials` fills the Socials panel.
- `AviSeekRow` forwards a list row's buttons to its panel.

## Links

- [AviSeek](https://aviseek.vrchatlegends.com) and its [world API docs](https://aviseek.vrchatlegends.com/world)
- [AviSeek Discord](https://discord.gg/UkvKMG8eUY) and [VRChat Legends Discord](https://discord.gg/6xPkZ7Dxp9)
- [Report a bug](https://github.com/VRChat-Legends/Aviseek-SDK/issues)
- Support: support@vrchatlegends.com
