using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace VRChatLegends.AviSeek
{
    // the aviseek profile menu, in the wall's own look. vr players open it from a button that
    // shows over the left wrist when the right hand hovers over that arm; desktop players press the
    // open key. a player signs in with a one time code from aviseek.vrchatlegends.com/account
    // and the menu shows their credits, tier, rank and favorites folders, any of which can be
    // put on the wall. it also holds the world's music switch.
    //
    // the code is checked by aviseek and the answer names the vrchat player it was made for.
    // the menu refuses an answer for anyone else, so a code read off a stream is no use to the
    // person who read it. nothing here is synced: whatever a modified client does to its own
    // copy, nobody else sees it
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Menu")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekMenu : UdonSharpBehaviour
    {
        private const string ProfileKey = "aviseek.profile";
        private const string MusicKey = "aviseek.music";
        private const string MusicVolumeKey = "aviseek.musicVolume";

        public AviSeekSearch search;

        [Header("Opening")]
        [Tooltip("The menu itself. It opens in front of the player and stays put while it is open.")]
        public RectTransform menu;
        [Tooltip("Desktop players press this to open and close the menu.")]
        public KeyCode openKey = KeyCode.P;
        [Tooltip("How far in front of the player's eyes the menu opens, in metres, in vr and on desktop.")]
        public float vrDistance = 0.42f;
        public float desktopDistance = 0.62f;
        [Tooltip("The menu closes once the player walks this far from where it opened, in metres.")]
        public float closeDistance = 3f;

        [Header("Wrist button")]
        [Tooltip("Floats over the left wrist while the right hand hovers over the left forearm. The right fingertip taps it.")]
        public Transform wristButton;
        public Image wristRing;
        public float wristTapDistance = 0.035f;
        [Tooltip("The button shows while the right fingertip is this close to the left forearm, in metres.")]
        public float hoverDistance = 0.12f;

        [Header("Sign in")]
        [Tooltip("The address the code is typed after. VRChat's keyboard shows it, so it is kept short.")]
        public VRCUrl signInPrefixUrl;
        public VRCUrlInputField signInField;
        public TextMeshProUGUI signInDisplay;
        public TextMeshProUGUI signInHint;
        public TextMeshProUGUI statusLabel;
        public GameObject signedInRoot;
        public GameObject signedOutRoot;

        [Header("Profile")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI playerLabel;
        public TextMeshProUGUI initialLabel;
        public Image avatarCircle;
        public TextMeshProUGUI tierLabel;
        public Image tierPill;
        public TextMeshProUGUI rankLabel;
        public TextMeshProUGUI creditsValue;
        public TextMeshProUGUI favoritesValue;
        public TextMeshProUGUI foldersValue;
        public TextMeshProUGUI tierValue;
        public TextMeshProUGUI signedInAtLabel;

        [Header("Folders")]
        public GameObject[] folderRows;
        public TextMeshProUGUI[] folderNames;
        public TextMeshProUGUI[] folderCounts;
        public TextMeshProUGUI folderPageLabel;

        [Header("Music")]
        public AudioSource music;
        [Range(0f, 1f)] public float musicVolume = 0.25f;
        public TextMeshProUGUI musicLabel;
        public TextMeshProUGUI musicIcon;
        public TextMeshProUGUI volumeLabel;

        [Header("Sounds")]
        public AudioSource voice;
        public AudioClip openClip;
        public AudioClip closeClip;

        [Header("Colours")]
        public Color accent = new Color(0.34f, 0.86f, 0.95f, 1f);
        public Color idle = new Color(1f, 1f, 1f, 0.45f);

        private bool _open;
        private float _scale = 0.0006f;
        private Vector3 _openedAt;
        private float _openTime = -9f;
        private float _wristCooldown;
        private float _wristFade;
        private bool _vr;
        private float _nextCheck;

        private bool _loading;
        private string _typed = "";
        private bool _wasFocused;

        private bool _signedIn;
        private string _name = "";
        private string _tier = "";
        private string _rank = "";
        private int _credits;
        private int _total;
        private string _signedInAt = "";
        private string[] _folderName = new string[0];
        private int[] _folderCount = new int[0];
        private int _folderPage;
        // the full favorites list only lives for this visit, the saved profile keeps the summary
        private DataList _favorites;

        private bool _restored;
        private bool _musicOn = true;

        private void Start()
        {
            if (menu != null) menu.gameObject.SetActive(false);
            if (wristButton != null) wristButton.gameObject.SetActive(false);
            ResetField();
            ShowProfile();
            ApplyMusic();
        }

        private void Update()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + 2f;
                _vr = player.IsUserInVR();
            }
            if (!_vr && Input.GetKeyDown(openKey)) _Toggle();
            if (_vr) Wrist(player);
            else if (wristButton != null && wristButton.gameObject.activeSelf) wristButton.gameObject.SetActive(false);

            if (!_open) return;
            if (Vector3.Distance(player.GetPosition(), _openedAt) > closeDistance)
            {
                _Close();
                return;
            }
            // a little rise and grow as it opens
            float k = Mathf.Clamp01((Time.time - _openTime) / 0.28f);
            float ease = 1f - (1f - k) * (1f - k) * (1f - k);
            menu.localScale = Vector3.one * (_scale * Mathf.Lerp(0.86f, 1f, ease));

            if (signInField != null)
            {
                bool focused = signInField.isFocused;
                if (focused || _wasFocused) RefreshTyped();
                _wasFocused = focused;
            }
        }

        /* ───────────── opening ───────────── */

        public void _Toggle()
        {
            if (_open) _Close();
            else _Open();
        }

        public void _Open()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null || menu == null) return;
            if (_openTime < 0f) _scale = menu.localScale.x;
            VRCPlayerApi.TrackingData head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Vector3 forward = head.rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = player.GetRotation() * Vector3.forward;
            forward.Normalize();
            float distance = _vr ? vrDistance : desktopDistance;
            menu.position = head.position + forward * distance + Vector3.down * (_vr ? 0.1f : 0.04f);
            // tipped back a little, like a tablet held in front of you
            menu.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(_vr ? 12f : 4f, 0f, 0f);
            menu.gameObject.SetActive(true);
            _open = true;
            _openedAt = player.GetPosition();
            _openTime = Time.time;
            ResetField();
            ShowProfile();
            Play(openClip);
        }

        public void _Close()
        {
            if (!_open) return;
            _open = false;
            if (menu != null) menu.gameObject.SetActive(false);
            Play(closeClip);
        }

        // the button only shows while the right hand hovers over the left forearm, floats just
        // above the wrist facing the eyes, and opens or closes the menu when the fingertip taps it
        private void Wrist(VRCPlayerApi player)
        {
            if (wristButton == null) return;
            Vector3 head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Vector3 wrist = player.GetBonePosition(HumanBodyBones.LeftHand);
            Vector3 elbow = player.GetBonePosition(HumanBodyBones.LeftLowerArm);
            if (wrist == Vector3.zero) wrist = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            if (elbow == Vector3.zero) elbow = wrist + (head - wrist).normalized * 0.25f + Vector3.down * 0.1f;
            else wrist = Vector3.Lerp(elbow, wrist, 0.85f);
            Vector3 tip = Fingertip(player);

            // how far the fingertip is from the forearm, elbow to wrist
            Vector3 arm = wrist - elbow;
            float along = arm.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(tip - elbow, arm) / arm.sqrMagnitude) : 1f;
            float fromArm = Vector3.Distance(tip, elbow + arm * along);
            bool hovering = fromArm < hoverDistance;
            _wristFade = Mathf.MoveTowards(_wristFade, hovering ? 1f : 0f, Time.deltaTime * (hovering ? 8f : 4f));
            if (_wristFade <= 0f)
            {
                if (wristButton.gameObject.activeSelf) wristButton.gameObject.SetActive(false);
                return;
            }
            if (!wristButton.gameObject.activeSelf) wristButton.gameObject.SetActive(true);
            Vector3 toHead = (head - wrist).normalized;
            Vector3 spot = wrist + toHead * 0.04f + Vector3.up * 0.04f;
            wristButton.position = spot;
            wristButton.rotation = Quaternion.LookRotation(spot - head);
            float ease = 1f - (1f - _wristFade) * (1f - _wristFade);
            wristButton.localScale = Vector3.one * (0.0004f * Mathf.Lerp(0.4f, 1f, ease));

            float gap = Vector3.Distance(tip, spot);
            if (wristRing != null)
            {
                Color ring = gap < wristTapDistance * 2f ? Color.white : accent;
                ring.a = ease;
                wristRing.color = ring;
            }
            if (_wristFade >= 1f && gap < wristTapDistance && Time.time > _wristCooldown)
            {
                _wristCooldown = Time.time + 0.8f;
                player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.06f, 0.7f, 200f);
                _Toggle();
            }
        }

        private Vector3 Fingertip(VRCPlayerApi player)
        {
            Vector3 end = player.GetBonePosition(HumanBodyBones.RightIndexDistal);
            if (end != Vector3.zero)
            {
                Vector3 joint = player.GetBonePosition(HumanBodyBones.RightIndexIntermediate);
                return joint == Vector3.zero ? end : end + (end - joint) * 0.8f;
            }
            VRCPlayerApi.TrackingData hand = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            return hand.position + hand.rotation * (Vector3.forward * 0.1f);
        }

        /* ───────────── signing in ───────────── */

        public void _SignIn()
        {
            if (signInField == null || _loading) return;
            VRCUrl url = signInField.GetUrl();
            string text = url == null ? "" : url.Get();
            string prefix = signInPrefixUrl == null ? "" : signInPrefixUrl.Get();
            if (text == null || prefix.Length == 0 || !text.StartsWith(prefix))
            {
                SetStatus("Click the code box and type your code after the address.");
                ResetField();
                return;
            }
            string code = text.Substring(prefix.Length).Replace("-", "").Replace(" ", "");
            if (code.Length != 8)
            {
                SetStatus("A world code has 8 letters and numbers, like ABCD-EFGH.");
                return;
            }
            _loading = true;
            SetStatus("Signing in...");
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public void _SignOut()
        {
            _signedIn = false;
            _name = "";
            _tier = "";
            _rank = "";
            _credits = 0;
            _total = 0;
            _signedInAt = "";
            _folderName = new string[0];
            _folderCount = new int[0];
            _favorites = null;
            if (_restored) PlayerData.SetString(ProfileKey, "");
            SetStatus("Signed out.");
            ShowProfile();
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _loading = false;
            ResetField();
            ApplyAnswer(result.Result);
        }

        private void ApplyAnswer(string json)
        {
            DataToken root;
            if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary)
            {
                SetStatus("<color=#ff7a86>AviSeek sent something unreadable.</color>");
                return;
            }
            DataDictionary data = root.DataDictionary;
            DataToken token;
            if (!data.TryGetValue("ok", TokenType.Boolean, out token) || !token.Boolean)
            {
                string reason = ReadString(data, "reason");
                SetStatus("<color=#ff7a86>" + Escape(reason.Length > 0 ? reason : "That code did not work.") + "</color>");
                return;
            }
            if (!data.TryGetValue("account", TokenType.DataDictionary, out token))
            {
                SetStatus("<color=#ff7a86>AviSeek sent no account.</color>");
                return;
            }
            DataDictionary account = token.DataDictionary;
            // the code was made for one vrchat player. anyone else holding it gets nothing
            string meant = ReadString(account, "vrchatName").Trim();
            string me = Networking.LocalPlayer == null ? "" : Networking.LocalPlayer.displayName.Trim();
            if (meant.Length == 0 || meant.ToLower() != me.ToLower())
            {
                SetStatus("<color=#ff7a86>That code was made for " + Escape(meant) + ", not you. Get your own at aviseek.vrchatlegends.com/account</color>");
                return;
            }
            _name = Clip(ReadString(account, "name"), 40);
            _tier = Clip(ReadString(account, "tier"), 24);
            _rank = Clip(ReadString(account, "rank"), 24);
            _credits = data.TryGetValue("credits", TokenType.DataDictionary, out token) ? (int)ReadNumber(token.DataDictionary, "balance") : 0;
            _total = (int)ReadNumber(data, "totalFavorites");
            _signedInAt = ReadString(data, "signedInAt");
            _favorites = data.TryGetValue("favorites", TokenType.DataList, out token) ? token.DataList : new DataList();
            if (_total < _favorites.Count) _total = _favorites.Count;
            ReadFolders(data.TryGetValue("folders", TokenType.DataList, out token) ? token.DataList : new DataList());
            // the favorites' pictures, all in one image
            if (search != null && data.TryGetValue("pics", out token) && token.IsNumber) search.LoadSiteSheet((int)token.Number, _favorites, ReadNumber(data, "picsPending") > 0);
            _signedIn = true;
            _folderPage = 0;
            SaveProfile();
            SetStatus("Signed in as " + Escape(_name) + ".");
            ShowProfile();
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _loading = false;
            SetStatus("<color=#ff7a86>Could not reach AviSeek (" + result.ErrorCode + "). Try again in a moment.</color>");
        }

        private void ReadFolders(DataList list)
        {
            int count = Mathf.Min(list.Count, 60);
            _folderName = new string[count];
            _folderCount = new int[count];
            int kept = 0;
            for (int i = 0; i < count; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                _folderName[kept] = Clip(ReadString(list[i].DataDictionary, "name"), 32);
                _folderCount[kept] = (int)ReadNumber(list[i].DataDictionary, "count");
                kept++;
            }
            if (kept == count) return;
            string[] names = new string[kept];
            int[] counts = new int[kept];
            for (int i = 0; i < kept; i++)
            {
                names[i] = _folderName[i];
                counts[i] = _folderCount[i];
            }
            _folderName = names;
            _folderCount = counts;
        }

        private void RefreshTyped()
        {
            VRCUrl url = signInField.GetUrl();
            string text = url == null ? "" : url.Get();
            if (text == null) text = "";
            string prefix = signInPrefixUrl == null ? "" : signInPrefixUrl.Get();
            string typed = prefix.Length > 0 && text.StartsWith(prefix) ? text.Substring(prefix.Length) : "";
            // a code is never shown in full, in case someone is streaming
            string masked = "";
            for (int i = 0; i < typed.Length && i < 12; i++) masked += i < 2 ? typed.Substring(i, 1).ToUpper() : "•";
            if (masked == _typed) return;
            _typed = masked;
            if (signInDisplay != null) signInDisplay.text = _typed;
            if (signInHint != null) signInHint.gameObject.SetActive(_typed.Length == 0);
        }

        private void ResetField()
        {
            if (signInField != null && signInPrefixUrl != null) signInField.SetUrl(signInPrefixUrl);
            _typed = "";
            if (signInDisplay != null) signInDisplay.text = "";
            if (signInHint != null) signInHint.gameObject.SetActive(true);
        }

        /* ───────────── saved profile ───────────── */

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            _restored = true;
            bool on;
            if (PlayerData.TryGetBool(player, MusicKey, out on)) _musicOn = on;
            float volume;
            if (PlayerData.TryGetFloat(player, MusicVolumeKey, out volume)) musicVolume = Mathf.Clamp01(volume);
            ApplyMusic();
            string saved;
            if (!_signedIn && PlayerData.TryGetString(player, ProfileKey, out saved) && saved.Length > 0) LoadProfile(saved);
            ShowProfile();
        }

        // only the summary is kept between visits. favorites come back with the next sign in
        private void SaveProfile()
        {
            if (!_restored) return;
            DataDictionary data = new DataDictionary();
            data.SetValue("name", _name);
            data.SetValue("tier", _tier);
            data.SetValue("rank", _rank);
            data.SetValue("credits", _credits);
            data.SetValue("total", _total);
            data.SetValue("at", _signedInAt);
            DataList folders = new DataList();
            for (int i = 0; i < _folderName.Length; i++)
            {
                DataDictionary folder = new DataDictionary();
                folder.SetValue("name", _folderName[i]);
                folder.SetValue("count", _folderCount[i]);
                folders.Add(folder);
            }
            data.SetValue("folders", folders);
            DataToken json;
            if (VRCJson.TrySerializeToJson(data, JsonExportType.Minify, out json)) PlayerData.SetString(ProfileKey, json.String);
        }

        private void LoadProfile(string text)
        {
            DataToken root;
            if (!VRCJson.TryDeserializeFromJson(text, out root) || root.TokenType != TokenType.DataDictionary) return;
            DataDictionary data = root.DataDictionary;
            _name = Clip(ReadString(data, "name"), 40);
            if (_name.Length == 0) return;
            _tier = Clip(ReadString(data, "tier"), 24);
            _rank = Clip(ReadString(data, "rank"), 24);
            _credits = (int)ReadNumber(data, "credits");
            _total = (int)ReadNumber(data, "total");
            _signedInAt = ReadString(data, "at");
            DataToken token;
            ReadFolders(data.TryGetValue("folders", TokenType.DataList, out token) ? token.DataList : new DataList());
            _favorites = null;
            _signedIn = true;
        }

        /* ───────────── showing it ───────────── */

        private void ShowProfile()
        {
            if (signedInRoot != null) signedInRoot.SetActive(_signedIn);
            if (signedOutRoot != null) signedOutRoot.SetActive(!_signedIn);
            string player = Networking.LocalPlayer == null ? "" : Networking.LocalPlayer.displayName;
            if (playerLabel != null) playerLabel.text = Escape(player);
            if (initialLabel != null)
            {
                string source = _signedIn && _name.Length > 0 ? _name : player;
                initialLabel.text = Initial(source);
            }
            Color tierColor = TierColor(_tier);
            if (avatarCircle != null) avatarCircle.color = _signedIn ? new Color(tierColor.r * 0.45f, tierColor.g * 0.45f, tierColor.b * 0.45f, 1f) : new Color(1f, 1f, 1f, 0.08f);
            if (!_signedIn)
            {
                if (nameLabel != null) nameLabel.text = "Not signed in";
                if (tierPill != null) tierPill.gameObject.SetActive(false);
                if (rankLabel != null) rankLabel.text = "";
                return;
            }
            if (nameLabel != null) nameLabel.text = Escape(_name);
            if (tierPill != null)
            {
                tierPill.gameObject.SetActive(_tier.Length > 0);
                tierPill.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.22f);
            }
            if (tierLabel != null)
            {
                tierLabel.text = Escape(_tier);
                tierLabel.color = tierColor;
            }
            if (rankLabel != null) rankLabel.text = _rank.Length > 0 ? Escape(_rank) : "";
            if (creditsValue != null) creditsValue.text = _credits.ToString("N0");
            if (favoritesValue != null) favoritesValue.text = _total.ToString("N0");
            if (foldersValue != null) foldersValue.text = _folderName.Length.ToString();
            if (tierValue != null)
            {
                tierValue.text = _tier.Length > 0 ? Escape(_tier) : "Free";
                tierValue.color = _tier.Length > 0 ? tierColor : Color.white;
            }
            if (signedInAtLabel != null)
            {
                string day = _signedInAt.Length >= 10 ? _signedInAt.Substring(0, 10) : "";
                signedInAtLabel.text = (_favorites == null ? "Saved from " : "Signed in ") + day + (_favorites == null ? ". Sign in again for your favorites." : "");
            }
            ShowFolders();
        }

        private void ShowFolders()
        {
            if (folderRows == null) return;
            int size = folderRows.Length;
            int pages = Mathf.Max(1, (_folderName.Length + size - 1) / size);
            if (_folderPage >= pages) _folderPage = pages - 1;
            int start = _folderPage * size;
            for (int i = 0; i < size; i++)
            {
                int index = start + i;
                bool has = index < _folderName.Length;
                if (folderRows[i] != null) folderRows[i].SetActive(has);
                if (!has) continue;
                if (folderNames != null && i < folderNames.Length && folderNames[i] != null) folderNames[i].text = Escape(_folderName[index]);
                if (folderCounts != null && i < folderCounts.Length && folderCounts[i] != null) folderCounts[i].text = _folderCount[index].ToString();
            }
            if (folderPageLabel != null) folderPageLabel.text = _folderName.Length == 0 ? "No folders yet" : (_folderPage + 1) + " / " + pages;
        }

        public void _FolderNext()
        {
            int size = folderRows == null || folderRows.Length == 0 ? 1 : folderRows.Length;
            if ((_folderPage + 1) * size < _folderName.Length) _folderPage++;
            ShowFolders();
        }

        public void _FolderPrev()
        {
            if (_folderPage > 0) _folderPage--;
            ShowFolders();
        }

        public void _Folder0() { ShowFolder(0); }
        public void _Folder1() { ShowFolder(1); }
        public void _Folder2() { ShowFolder(2); }
        public void _Folder3() { ShowFolder(3); }
        public void _Folder4() { ShowFolder(4); }
        public void _Folder5() { ShowFolder(5); }

        public void _AllFavorites()
        {
            if (!HaveFavorites()) return;
            search.ShowSiteList(_favorites, "site favorites");
            SetStatus("Your site favorites are on the wall.");
        }

        private void ShowFolder(int slot)
        {
            int size = folderRows == null || folderRows.Length == 0 ? 1 : folderRows.Length;
            int index = _folderPage * size + slot;
            if (index >= _folderName.Length || !HaveFavorites()) return;
            DataList picked = new DataList();
            for (int i = 0; i < _favorites.Count; i++)
            {
                if (_favorites[i].TokenType != TokenType.DataDictionary) continue;
                if ((int)ReadNumber(_favorites[i].DataDictionary, "folder") == index) picked.Add(_favorites[i]);
            }
            search.ShowSiteList(picked, "folder \"" + Escape(_folderName[index]) + "\"");
            SetStatus(Escape(_folderName[index]) + " is on the wall.");
        }

        private bool HaveFavorites()
        {
            if (search == null) return false;
            if (_favorites != null) return true;
            SetStatus(_signedIn ? "Sign in again to bring your favorites over." : "Sign in first to see your favorites.");
            return false;
        }

        /* ───────────── music ───────────── */

        public void _ToggleMusic()
        {
            _musicOn = !_musicOn;
            if (_restored) PlayerData.SetBool(MusicKey, _musicOn);
            ApplyMusic();
        }

        public void _MusicUp() { SetVolume(musicVolume + 0.1f); }
        public void _MusicDown() { SetVolume(musicVolume - 0.1f); }

        private void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(Mathf.Round(volume * 10f) / 10f);
            if (musicVolume > 0f && !_musicOn) _musicOn = true;
            if (_restored)
            {
                PlayerData.SetFloat(MusicVolumeKey, musicVolume);
                PlayerData.SetBool(MusicKey, _musicOn);
            }
            ApplyMusic();
        }

        private void ApplyMusic()
        {
            if (music != null)
            {
                music.volume = musicVolume;
                if (_musicOn && musicVolume > 0f)
                {
                    if (!music.isPlaying) music.Play();
                }
                else if (music.isPlaying) music.Pause();
            }
            bool playing = _musicOn && musicVolume > 0f;
            if (musicLabel != null) musicLabel.text = playing ? "Music on" : "Music off";
            if (musicIcon != null) musicIcon.color = playing ? accent : idle;
            if (volumeLabel != null) volumeLabel.text = Mathf.RoundToInt(musicVolume * 100f) + "%";
        }

        /* ───────────── helpers ───────────── */

        // the first letter or digit, so a name like "[DJ] Moth" shows D, not [
        private string Initial(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsLetterOrDigit(name[i])) return name.Substring(i, 1).ToUpper();
            }
            return "?";
        }

        private Color TierColor(string tier)
        {
            string t = tier.ToLower();
            if (t.IndexOf("platinum") >= 0) return new Color(0.62f, 0.93f, 1f, 1f);
            if (t.IndexOf("gold") >= 0) return new Color(1f, 0.8f, 0.3f, 1f);
            if (t.IndexOf("silver") >= 0) return new Color(0.82f, 0.85f, 0.92f, 1f);
            if (t.IndexOf("bronze") >= 0) return new Color(0.88f, 0.58f, 0.36f, 1f);
            return accent;
        }

        private void Play(AudioClip clip)
        {
            if (voice == null || clip == null) return;
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player != null) voice.transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            voice.PlayOneShot(clip, 0.4f);
        }

        private string ReadString(DataDictionary data, string key)
        {
            DataToken token;
            if (data.TryGetValue(key, TokenType.String, out token)) return token.String;
            return "";
        }

        private double ReadNumber(DataDictionary data, string key)
        {
            DataToken token;
            if (data.TryGetValue(key, out token) && token.IsNumber) return token.Number;
            return 0;
        }

        private string Clip(string text, int length)
        {
            if (text == null) return "";
            text = text.Trim();
            return text.Length > length ? text.Substring(0, length - 1) + "…" : text;
        }

        // names are player written, keep their tags from styling the menu
        private string Escape(string text)
        {
            return text.Replace("<", "<​");
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }
    }
}
