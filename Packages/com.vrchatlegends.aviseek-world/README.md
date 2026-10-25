# AviSeek World SDK

A drop-in avatar search wall for VRChat worlds, powered by [AviSeek](https://aviseek.vrchatlegends.com). Players search more than 200,000 public avatars, filter by platform and performance rank, save favorites and folders to their AviSeek account, browse promoted avatars and groups, and switch avatars with one click. VR players can use it as a touch screen. Signed in players see their credits, tier and folders, and the world gets a mirror and background music they can turn down or off.

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

When a player joins, the panels flip in one after another and a welcome banner with their name drops in over the wall.

## The wall

The wall is about 9 metres wide and holds up to eight panels, all flat on the wall. Each panel is a solid slab with a glowing rim, and the cards and buttons float off it with soft shadows. Switch a panel off and the others close up. There is no back wall: hang the panels on any wall in your world.

| Panel | What it does |
| --- | --- |
| Supporters | The AviSeek supporters with their Patreon pictures, each tier on its own pages in its own colour, with a link players can copy to become one. Pages fade over by themselves. |
| Socials | Your community links. Clicking a link opens the VRChat keyboard so players can copy it (they can not change it), and VRChat groups get an Open button that opens the group in game. |
| Search | Avatar or creator search, platform tabs, sort, performance rank, creator filter, favorites, paging and the selected avatar's details. |
| Results | Twelve avatar cards with VRChat's own picture, platforms, ratings, a heart and a Use button. |
| Folders | The player's AviSeek folders, the same as on the website. Show one on the wall, put the selected avatar in one, make or delete folders. |
| Mirror | A mirror with Off, Players and Full buttons, chosen by each player for themselves. Off until they pick. |
| Promoted | Avatars and groups promoted on AviSeek. Avatars can be viewed or worn, and groups open in game. Long lists page by themselves. |
| Store | Off by default. Your own VRChat Creator Economy products, see below. |

## Make it yours

Select the wall to see its inspector, or use the Branding tab of the SDK Panel.

- **Title and highlighted word**: the heading on the Search panel, for example `AVATAR` and `SEARCH`.
- **Logo**: any sprite. Leave it empty to hide the logo.
- **Accent colour**: the buttons, glows and rims. Custom colours write their materials to `Assets/AviSeek Branding`, so the package stays untouched.
- **Panels**: switch the Supporters, Socials, Folders, Mirror, Promoted and Store panels on or off. A hidden Promoted panel skips its download.
- **Touch screen**: on by default. VR players press the panels with a fingertip.
- **Join animation**: on by default. Select the wall and switch off **Play On Join** on AviSeek Intro to skip it.
- **Music**: the **Music** object holds the track. Swap its clip for your own, or delete the object for no music. Players set their own volume in the profile menu and it is saved for their next visit.

Select **Socials Panel** to edit the links. Each link has a label, address, icon or logo picture, icon colour and an optional VRChat group id (`grp_...`). **Restore the VRChat Legends links** puts the default list back.

## Creator store (VRChat Creator Economy)

Switch on **Creator store** in the wall's Panels, then select **Store Panel**:

- **Products**: your UdonProduct assets, in order. Each shows its picture, name, description and an Owned badge for players who have it.
- **Listing ids**: the store listing for each product, from the VRChat Creator Dashboard. **Get** opens it. With no listing id, Get opens your group store, or the world store.
- **Group id**: adds a **Group store** button. **Show World Store** adds a **World store** button.
- **Perk objects**: objects switched on for the local player when they own a product, such as a badge, a key or a prop. Pick the product for each one by its number, from 0.
- **Announce purchases**: shows a thank you line on the panel when anyone in the instance buys something.

Purchases always happen in VRChat's own store window.

## The profile menu

VR players bring their right hand close to the back of their left forearm: a small round AviSeek button appears on the back of the wrist, and tapping it with the right index finger, or clicking it with the VRChat laser, opens the menu in front of them. Desktop players press **P**, or the **Profile** button on the Search panel. The menu wears the wall's own look.

- **Sign in with VRChat Legends**: on aviseek.vrchatlegends.com/account, under **VRChat worlds**, the player enters their VRChat name and gets a code such as `ABCD-EFGH`. They tap the code box in the menu and type it. The menu then shows their account picture, AviSeek name, tier, rank, credits, favorites against their limit, their newest favorites and their folders.
- **Favorites and folders**: hearts save to the account, and the Folders panel makes, fills and deletes folders. The limit is the website's own.
- **Sync**: brings the account's favorites and folders down again. A player who is still linked syncs by themselves when they join.
- **Folders**: tapping a folder puts its avatars on the wall, with pictures, like a search. **Show all** puts every favorite on the wall.
- **Move, size and throw**: in VR, grab the bar under the menu to move it and a corner to size it. Its buttons ignore fingers while a hand holds it. Throw it and it shatters. Desktop players use the size buttons in the header.
- **Music**: on or off, and the volume.
- **Remembered**: the name, tier, credits, folder list, menu size and music are saved with PlayerData for the next visit. The favorites come back with the next sync.

VRChat only lets a world load an address a player typed, so the code box holds a short address (`aviapi.vrchatlegends.com/w/l?c=`) and VRChat's keyboard shows it while typing. The menu itself only ever shows the first two letters of the code.

## What players get

- **Search by avatar or creator**: the Avatar and Creator buttons pick what the search box looks through. Players click the box, type and press Enter. VRChat only lets worlds load web addresses that a player typed, so the box holds the AviSeek address and the player's search is added to the end. The wall shows only what they typed. VRChat's own keyboard shows the address while typing, so the wall switches to a much shorter one (`aviapi.vrchatlegends.com/w?q=`) once the server answers on it.
- **Touch screen**: VR players press any button or box with the tip of their index finger. A ring follows the finger and closes in as it nears the panel, the button under it lights up with an outline, a short trail follows the finger as it slides along, and a press ripples with a click and a small haptic tap. The sounds are Kenney's UI Audio (CC0).
- **Popular list**: the home button reloads it. Paging past the end of a set of 120 loads the next set, for up to 10 sets (100 pages).
- **Filters**: All, PC, Quest or iOS, sort by popularity, name or creator, and performance rank. The rank filter uses the rating for the platform the player is on.
- **Filter by creator**: narrows whatever is on the wall, search results or favorites, to creators whose name contains the typed text. Clicking the creator line on the selected avatar fills it in.
- **Favorites**: the heart on any card or on the selected avatar, for signed in players. They save to the AviSeek account, up to the account's limit, and the Favorites button shows the count against the limit and puts them on the wall.
- **Gold name**: a signed in supporter's own line on the supporters board shows in gold.
- **Mirror**: Off, Players (cheap) or Full, remembered for the next visit, and switched off while the player is more than 9 metres away.
- **Details**: clicking a picture shows the avatar's ratings for each platform and its avatar ID. Clicking the ID opens the VRChat keyboard so players can copy it. Any typing is undone at once.
- **Ratings at a glance**: each card shows the Windows, Android and Apple logos coloured by that platform's performance rank. A faded logo means there is no version for that platform.
- **Use**: switches the player into the avatar through a hidden VRC Avatar Pedestal.
- **Promoted**: refreshed every 10 minutes. Searches always download first.
- **Profile**: while signed in, the profile, favorites, folders and account picture come down together in one download every 10 minutes, or when the player presses Sync.

## Safety

- **Codes**: a world code is 8 characters, works once, for ten minutes, and only for the VRChat name it was made for. The server keeps only a SHA-256 hash of each code, strips codes from its logs, and allows 8 tries a minute per address, with the same answer for every wrong, used or expired code.
- **Name check**: the server sends back the VRChat name the code was made for, and the menu refuses it unless that is the local player's own name. A code read off a stream or over a shoulder is useless to anyone else.
- **What a linked world can do**: signing in links this VRChat client's address to the account (the server keeps only a salted SHA-256 of it). From then on that address may favorite, unfavorite and sort avatars into folders, and nothing else: it can not spend credits, change settings or sign in to the website. Every change names a list and a tile the server sent to that same address within the hour, so it can only ever touch avatars the player was shown.
- **Not from a web page**: browsers add Origin, Referer or Sec-Fetch headers and VRChat does not, so the server refuses any account request carrying them. A web page open on the same network can not act for the player.
- **Ending a link**: a link ends after a day without use, after 30 days at most, when the player signs out in the menu, or when they press **Unlink** on aviseek.vrchatlegends.com/account. Account changes are limited to 30 a minute.
- **Shared addresses**: a link belongs to an internet address, so on a shared connection (one home, a school) someone else's VRChat on that address could use it until it ends. The account page shows when each link was last used.
- **Nothing synced**: the wall, the menu and the music are local to each player. A modified client that edits its own downloads or its own name only changes what that one player sees, never what anyone else sees, and never the account on the site.
- **Checked answers**: the wall keeps at most 300 rows from any answer and only real `avtr_` ids, so an edited answer can not hand the avatar pedestal anything else. Names from the server are shown as plain text.
- **What is saved**: PlayerData can be read by other players in the same instance, so the menu saves only the summary (name, tier, rank, credits, folder names and counts), never the favorites list or a code.

## Notes

- Players need **Allow Untrusted URLs** turned on in VRChat (Settings > Comfort & Safety) unless the AviSeek domain is on VRChat's trusted list. The wall says so when a download is blocked.
- Downloads are spaced at least 5.5 seconds apart, and the wall waits 30 seconds after an HTTP 429, as the API docs ask.
- Pictures come as one image per batch. Every batch of 120 the wall downloads names a picture sheet on the server (`aviapi.vrchatlegends.com/w/p/<slot>`), a single JPEG with all 120 pictures in a 10 x 12 grid, and each card shows its own tile. That is one image download per 120 avatars instead of one per avatar. Pictures the server is still fetching come with a later copy of the sheet, tried again after 15, 30, 60 and 120 seconds. Signed in players' site favorites get a sheet of their own. Like the text, the pictures need **Allow Untrusted URLs**.
- Searches download text only. Promoted group icons and supporter pictures come inside the promoted list, in the same single download. Until they arrive, each supporter shows the first letter of their name.
- Picture slots show an animated loading card (the AviSeek Loading Card shader) until VRChat's picture is ready.
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
- `AviSeekIntro` plays the join animation and the welcome banner.
- `AviSeekMenu` is the profile menu, its wrist button, signing in, the account (favorites, folders and sync go through it), moving, sizing and the shatter, and the music.
- `AviSeekFolders` is the Folders panel.
- `AviSeekMirror` is the Mirror panel.

## Links

- [AviSeek](https://aviseek.vrchatlegends.com) and its [world API docs](https://aviseek.vrchatlegends.com/world)
- Avatar index powered by [VRC-NEXUS](https://search.vrc-nexus.online)
- [AviSeek Discord](https://discord.gg/UkvKMG8eUY) and [VRChat Legends Discord](https://discord.gg/6xPkZ7Dxp9)
- [Report a bug](https://github.com/VRChat-Legends/Aviseek-SDK/issues)
- Support: support@vrchatlegends.com
