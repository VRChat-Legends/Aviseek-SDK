# Changelog

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
