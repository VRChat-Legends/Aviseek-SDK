# AviSeek World SDK

A drop-in avatar search wall for VRChat worlds, powered by [AviSeek](https://aviseek.vrchatlegends.com). Players search more than 200,000 public avatars, filter by platform and performance rank, keep favorites, browse promoted avatars and groups, and switch avatars with one click. VR players can use it as a touch screen.

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

The wall is about 7 metres wide and holds up to six panels, all flat on the wall. Each panel is a solid slab with a glowing rim, and the cards and buttons float off it with soft shadows. Switch a panel off and the others close up. There is no back wall: hang the panels on any wall in your world.

| Panel | What it does |
| --- | --- |
| Supporters | The AviSeek supporters, each tier on its own pages in its own colour, with a link players can copy to become one. Pages fade over by themselves. |
| Socials | Your community links. Clicking a link opens the VRChat keyboard so players can copy it (they can not change it), and VRChat groups get an Open button that opens the group in game. |
| Search | Avatar or creator search, platform tabs, sort, performance rank, creator filter, favorites, paging and the selected avatar's details. |
| Results | Twelve avatar cards with VRChat's own picture, platforms, ratings, a heart and a Use button. |
| Promoted | Avatars and groups promoted on AviSeek. Avatars can be viewed or worn, and groups open in game. Long lists page by themselves. |
| Store | Off by default. Your own VRChat Creator Economy products, see below. |

## Make it yours

Select the wall to see its inspector, or use the Branding tab of the SDK Panel.

- **Title and highlighted word**: the heading on the Search panel, for example `AVATAR` and `SEARCH`.
- **Logo**: any sprite. Leave it empty to hide the logo.
- **Accent colour**: the buttons, glows and rims. Custom colours write their materials to `Assets/AviSeek Branding`, so the package stays untouched.
- **Panels**: switch the Supporters, Socials, Promoted and Store panels on or off. A hidden Promoted panel skips its download.
- **Touch screen**: on by default. VR players press the panels with a fingertip.

Select **Socials Panel** to edit the links. Each link has a label, address, icon or logo picture, icon colour and an optional VRChat group id (`grp_...`). **Restore the VRChat Legends links** puts the default list back.

## Creator store (VRChat Creator Economy)

Switch on **Creator store** in the wall's Panels, then select **Store Panel**:

- **Products**: your UdonProduct assets, in order. Each shows its picture, name, description and an Owned badge for players who have it.
- **Listing ids**: the store listing for each product, from the VRChat Creator Dashboard. **Get** opens it. With no listing id, Get opens your group store, or the world store.
- **Group id**: adds a **Group store** button. **Show World Store** adds a **World store** button.
- **Perk objects**: objects switched on for the local player when they own a product, such as a badge, a key or a prop. Pick the product for each one by its number, from 0.
- **Announce purchases**: shows a thank you line on the panel when anyone in the instance buys something.

Purchases always happen in VRChat's own store window.

## What players get

- **Search by avatar or creator**: the Avatar and Creator buttons pick what the search box looks through. Players click the box, type and press Enter. VRChat only lets worlds load web addresses that a player typed, so the box holds the AviSeek address and the player's search is added to the end. The wall shows only what they typed, but VRChat's own keyboard shows the whole address while typing.
- **Touch screen**: VR players press any button or box with the tip of their index finger. A ring follows the finger and closes in as it nears the panel, the button under it lights up with an outline, a short trail follows the finger as it slides along, and a press ripples with a click and a small haptic tap. The sounds are Kenney's UI Audio (CC0).
- **Popular list**: the home button reloads it. Paging past the end of a set of 120 loads the next set, for up to 10 sets (100 pages).
- **Filters**: All, PC, Quest or iOS, sort by popularity, name or creator, and performance rank. The rank filter uses the rating for the platform the player is on.
- **Filter by creator**: narrows whatever is on the wall, search results or favorites, to creators whose name contains the typed text. Clicking the creator line on the selected avatar fills it in.
- **Favorites**: the heart on any card or on the selected avatar. Up to 100 are saved with VRChat persistence (PlayerData) and load again on the player's next visit. The Favorites button shows the count and switches the wall to the saved list.
- **Details**: clicking a picture shows the avatar's ratings for each platform and its avatar ID. Clicking the ID opens the VRChat keyboard so players can copy it. Any typing is undone at once.
- **Ratings at a glance**: each card shows the Windows, Android and Apple logos coloured by that platform's performance rank. A faded logo means there is no version for that platform.
- **Use**: switches the player into the avatar through a hidden VRC Avatar Pedestal.
- **Promoted**: refreshed every 10 minutes. Searches always download first.

## Notes

- Players need **Allow Untrusted URLs** turned on in VRChat (Settings > Comfort & Safety) unless the AviSeek domain is on VRChat's trusted list. The wall says so when a download is blocked.
- Downloads are spaced at least 5.5 seconds apart, and the wall waits 30 seconds after an HTTP 429, as the API docs ask.
- Pictures are VRChat's own. Every card has a small hidden avatar pedestal, and VRChat draws the avatar's picture on it straight to each player, so pictures cost no download from AviSeek and favorites show theirs too. The first time the wall loads, it measures how big VRChat draws that picture on a pedestal under the floor, then fits every card's picture to its spot. If it can not measure it, the pictures stay off and the cards show a silhouette.
- Searches download text only. Promoted group icons come inside the promoted list, in the same single download.
- A missing performance rating shows as Unknown and is never guessed.
- A separate hidden avatar pedestal switches avatars. Its placement sits far below the floor.
- Searches, favorites and the promoted list all work in Play mode with ClientSim. Avatar switching and group pages only work in VRChat.

## For maintainers

The prefab, icon fonts, panel meshes and sprites are all generated by `Editor/AviSeekPrefabBuilder.cs`. Change the layout there and run **AviSeek > Maintainers > Rebuild Prefab**, instead of editing the prefab by hand.

Scripts are UdonSharp, in `Runtime/`:

- `AviSeekSearch` runs the wall, the download queue and favorites.
- `AviSeekCard` draws one result.
- `AviSeekPromoted` fills the Promoted panel.
- `AviSeekSocials` fills the Socials panel.
- `AviSeekRow` forwards a list row's buttons to its panel.
- `AviSeekSupporters` fills the Supporters panel.
- `AviSeekStore` runs the Creator store panel.
- `AviSeekTouch` is the touch screen. The builder lists every button and box for it.
- `AviSeekPicture` draws VRChat's picture of an avatar on a card through a small pedestal, and measures that picture once.

## Links

- [AviSeek](https://aviseek.vrchatlegends.com) and its [world API docs](https://aviseek.vrchatlegends.com/world)
- Avatar index powered by [VRC-NEXUS](https://search.vrc-nexus.online)
- [AviSeek Discord](https://discord.gg/UkvKMG8eUY) and [VRChat Legends Discord](https://discord.gg/6xPkZ7Dxp9)
- [Report a bug](https://github.com/VRChat-Legends/Aviseek-SDK/issues)
- Support: support@vrchatlegends.com
