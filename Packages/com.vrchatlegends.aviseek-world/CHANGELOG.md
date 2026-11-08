# Changelog

## 1.7.0

- **Use wears the avatar you picked**: VRChat's pedestal loads an avatar a moment after it is switched, and pressing Use before that put on the one it held before. The wall now waits until the new avatar has had time to arrive (about a second and a half after picking it), shows "Switching avatar..." meanwhile, and only ever wears the last one picked.
- **Three rows of results**: the results panel shows 18 avatars a page, with Back and Next buttons and the page count under the cards. Taller panels hang from the same top line as the rest.
- **Smooth edges everywhere**: cards, panels, the profile menu and icon boxes cut their pictures with a RectMask2D and the new AviSeek/UI Rounded shader, which rounds the corners with a soft one pixel edge. The stencil masks that left jagged rims are gone. The Loading Card, UI Float and Round Picture shaders round to the same corners.
- **Bigger mirror**: the mirror panel is 1100 x 1360 with a mirror more than twice the area, and bigger buttons.
- **One download line for the whole wall**: the profile menu's requests go through the wall's own loader, so the wall and the menu together keep VRChat's one string download every five seconds and nothing lands out of order. The player's search goes first, then the profile, then the background lists. Searches answered in the last 90 seconds come back from memory without a download, and the same request is never queued twice.
- **Join animation**: on joining, the AviSeek logo fills the view, then the view dives into the magnifying glass. The backdrop inside the lens fades first so the world shows through it, the cat fades out, and the panels fly in as the ring rushes past. Drawn by the new AviSeek/Intro Overlay shader on a quad held in front of the player's eyes. Turn it off with Show Logo on the AviSeek Intro.
- **Sign in codes can not be stolen by a world**: a world that sends the typed code to its own server only gets a pending link. A code used from a different internet connection than the browser that made it waits until the owner presses Approve on the website, and the world says so and signs in by itself once they do. Needs the matching AviSeek server and website update.
- **Walls placed before an update space themselves out again**: a wall in a scene keeps its old panel spots, and a panel that grew would overlap its neighbour. Opening the scene now lays the panels out again when they need it.

## 1.6.2

- **Sharper account picture**: the profile menu's picture comes inside the profile itself at 128 x 128, instead of as one small tile of a whole picture sheet. It no longer downloads a full sheet image on every favorite or folder change.
- **Smooth round pictures**: the account picture and the supporter pictures are cut round by the new AviSeek/Round Picture shader, with a soft edge instead of the jagged rim a UI mask left.
- **Fewer downloads**: the account picture only comes with signing in and syncing. While signed in, the profile syncs by itself every 10 minutes and never more often, in the same single download as the favorites and folders. Needs the matching AviSeek server update.

## 1.6.1

- **Supporter pictures**: the promoted list, which carries every supporter's picture, could be skipped for the whole visit when it was asked for while a search was still downloading. It now always goes once the download before it is done. Supporters who sign in with VRChat Legends now show their account picture too (needs the matching AviSeek server update).
- **Tier tag**: the tier pill under the player's name grows to fit long tiers like Platinum Legend+ instead of spilling out of it.
- **No presses while moving or sizing**: the menu's buttons ignore fingers while a hand holds the menu's bar or a corner, and for a moment after letting go.
- **Wrist button on the back of the wrist**: it sits on the back of the left wrist like a watch face, and only shows while the right hand is close to that side of the forearm.
- **Wrist button works with the VRChat laser**: it is a real button now, and stays a moment after the hand moves off so the laser can reach it.

## 1.6.0

- **Favorites live on the AviSeek account**: hearts need a signed in player and save straight to aviseek.vrchatlegends.com, so the wall, every AviSeek world and the website share one list. The website's favorite limit holds in the world too (5 free, more for supporters and bought slots). The old favorites kept in PlayerData are no longer used.
- **Linked worlds**: signing in links this VRChat client to the account for a day of use (30 days at most). The world can favorite, unfavorite and sort avatars into folders, nothing else. The website's account page lists every linked world with an Unlink button. Needs the matching AviSeek server and website update.
- **Sync**: a Sync button in the profile menu and on the Folders panel brings the account's favorites and folders down again. A returning player who is still linked syncs by themselves on join.
- **Folders panel**: a new panel on the wall. Show a folder on the wall, put the selected avatar into a folder with +, take it out again, make a new folder, and delete one with two taps. Its avatars stay in the favorites.
- **Profile menu redesign**: bigger, with the account picture in a ring and glow in the tier's colour, a favorites bar against the limit, the five newest favorites with pictures, folders, and the music volume as a bar. Size buttons in the header.
- **Move, size and throw (VR)**: grab the bar under the menu to move it, a corner to make it bigger or smaller, and throw it away to shatter it into glowing glass pieces with a glass sound (Kenney Impact Sounds, CC0). The shatter is the new AviSeek/Shatter shader.
- **Smaller touch pointer on the menu**: the touch cursor and its trail shrink with the panel they are on.
- **Gold name on the supporters board**: a signed in supporter's own line shows in gold with "(you)".
- **Profile button**: on the Search panel, with a P key hint for desktop players.
- **Mirror panel**: a mirror beside the wall with Off, Players and Full buttons. Each player picks for themselves, the choice is remembered, and the mirror switches itself off while the player is far away.

## 1.5.1

- **Avatar pictures that load**: the cards no longer rely on hidden avatar pedestals, whose pictures did not show in VRChat. Every batch's pictures now come as one image, a sheet of 120 tiles from the server, so a whole batch costs a single image download. The selected avatar and signed in players' site favorites show pictures too. Needs the matching AviSeek server update.
- **Profile menu**: now wears the wall's own colours, panel body and glowing rim, with the AviSeek logo.
- **Wrist button**: only shows while your right hand hovers over your left forearm, and is the AviSeek logo in the wall's style.

## 1.5.0

- **Profile menu**: a menu in the style of VRChat's launch pad. VR players open it from a button over the left wrist, desktop players press P. It shows the player's AviSeek name, tier, rank, credits, favorites and folders, and holds the music controls.
- **Sign in with VRChat Legends**: players get a one time code at aviseek.vrchatlegends.com/account and type it in the menu. The code works once, for ten minutes, only for the VRChat name it was made for, and the menu refuses it for anyone else.
- **Site folders on the wall**: tapping a folder in the menu puts its avatars on the wall, with pictures.
- **Background music**: a calm lofi loop (CC0) that each player can turn off or down. Their choice is saved for the next visit.
- **Join animation**: the panels flip in one after another with a soft sound, and a welcome banner with the player's name drops in over the wall.
- **Loading cards**: picture slots show an animated AviSeek card while VRChat's picture is on the way.
- **Supporter pictures**: every supporter shows their Patreon picture in a circle in their tier's colour, all inside the promoted list's one download. The thin line beside each name is gone.
- **Shorter search address**: VRChat's keyboard now shows `aviapi.vrchatlegends.com/w?q=` instead of the long address, once the server answers on it. Older servers keep the long one.
- **Safer answers**: the wall only accepts real avatar ids and at most 300 rows from any answer.

## 1.4.0

- **Real pictures, no downloads**: every card now shows VRChat's own picture of the avatar, drawn by a small hidden avatar pedestal on the card. Pictures go straight from VRChat to each player, so the wall no longer downloads pictures from AviSeek or asks again and again while they load. Favorites show their pictures too. The wall measures VRChat's picture once on a pedestal under the floor and fits every card to it, so the giant picture from before can not come back.
- **Cards**: the picture sits on top in VRChat's 4:3 shape, with the name and creator under it.
- **Group icons**: promoted groups show their own icon. All of them come inside the promoted list, in one download.
- **Supporters**: each card takes its tier's colour, Platinum Legend+, Platinum Legend, Gold, Silver and Bronze each get their own pages, pages fade over, and names flip in one after another.
- **Touch screen**: follows the real tip of the index finger, outlines the button under it, draws a short trail as the finger slides along a panel, presses when the fingertip meets the button's own surface, and plays a soft tick, a click and a release sound (Kenney UI Audio, CC0).
- **No back wall**: the stone wall, pillars and trim are gone. Hang the panels on any wall in your world.

## 1.3.0

- **Touch screen**: VR players can press the panels with their fingers. A glowing ring follows the fingertip, closes in as it nears the panel and ripples on every press, with a small haptic tap. The laser pointer still works. Switch it off in the inspector.
- **Bigger wall**: every panel is 25% larger, and the panels space themselves out when one is switched on or off. The backdrop resizes to fit.
- **Search box**: it now shows only what the player typed. The AviSeek address it needs stays hidden.
- **Clickable boxes**: the search box, the creator filter and the avatar ID box open the keyboard again. The clear button next to the creator filter now clears it instead of opening the keyboard.
- **Pictures**: cards pulse while their picture is on the way and fade it in when it arrives. The wall keeps asking for up to about 8 minutes and shows how many are ready.
- **Platform icons**: the rank dots on each card are now the Windows, Android and Apple logos, coloured by that platform's performance rank. Platforms an avatar has no version for are faded.
- **No more big avatar picture**: the hidden pedestal no longer paints VRChat's avatar picture over the wall.
- **New Supporters panel**: the AviSeek supporters by tier, with a copyable link to become one. Supporters moved here from the Search panel.
- **Promoted lists page**: any number of promoted avatars and groups, with arrows and an automatic page turn every 12 seconds.
- **New Creator store panel** (off by default): shows your VRChat Creator Economy products with picture, name, description and an Owned badge, Get buttons that open the listing, Group store and World store buttons, perk objects for owners, and an optional thank you line when someone buys.
- **Look**: text in Inter with a real bold face, solid button colours that no longer turn flat grey in VRChat, clean icon boxes without the stray line, and the real AviSeek and VRChat Legends logos on their links.
- **Credits**: the avatar index is powered by VRC-NEXUS, now credited on the Search panel.

## 1.2.1

- Clicking a social link or the avatar ID now opens the VRChat keyboard, so players can copy it. They can not change the text: any typing is put back at once.

## 1.2.0

- The panels now sit flat on the wall. They keep their depth: solid bodies, glowing rims, and cards and buttons that float off the panel.
- New **Promoted** panel with the avatars and groups promoted on AviSeek. Avatars can be viewed or worn, and groups open in game.
- New **Socials** panel with the VRChat Legends and AviSeek links. Players copy a link, and VRChat groups open in game. Edit the links in the inspector.
- New **SDK Panel** (AviSeek > SDK Panel) with scene checks, branding, an API check, update checks and help links.
- New inspector for the wall: title, logo and accent colour, with the change shown on the wall straight away. Panels can be switched on and off.
- Brand icons come from Simple Icons (CC0).
- Install through the VRChat Creator Companion from the AviSeek listing.

## 1.1.0

- Search by avatar name or by creator.
- Filter the wall by creator name.
- Favorites saved with VRChat persistence, up to 100, with their own list.
- Solid 3D panels with rims, raised cards and soft shadows.

## 1.0.0

- First release: the AviSeek avatar search wall prefab.
