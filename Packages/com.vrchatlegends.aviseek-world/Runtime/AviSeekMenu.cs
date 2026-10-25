using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.Persistence;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace VRChatLegends.AviSeek
{
    // the aviseek profile menu, and the player's aviseek account in the world. vr players open
    // it from a button that shows over the left wrist when the right hand hovers over that arm,
    // desktop players press the open key or the profile button on the wall. it can be grabbed by
    // its bar to move it, by a corner to size it, and thrown away, which shatters it.
    //
    // signing in: the player gets a one time code at aviseek.vrchatlegends.com/account and types
    // it here. the answer names the vrchat player it was made for and the menu refuses it for
    // anyone else. the code also links this vrchat client to the account for a day of use, so
    // favorites and folders made in the world are saved to the website, and the website's come
    // back with every answer. every change goes out as a short address baked into the world,
    // naming a batch and a tile the server handed to this same client, so the world never has to
    // build an address or hold a secret.
    //
    // nothing here is synced: whatever a modified client does to its own copy, nobody else sees it
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Menu")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekMenu : UdonSharpBehaviour
    {
        private const string ProfileKey = "aviseek.profile";
        private const string MusicKey = "aviseek.music";
        private const string MusicVolumeKey = "aviseek.musicVolume";
        private const string SizeKey = "aviseek.menuSize";
        private const int SheetCols = 10;
        private const int SheetRows = 12;
        private const int QueueSize = 16;

        public AviSeekSearch search;
        public AviSeekFolders folders;
        public AviSeekSupporters supporters;

        [Header("Opening")]
        [Tooltip("The menu itself. It opens in front of the player and stays put while it is open.")]
        public RectTransform menu;
        [Tooltip("Desktop players press this to open and close the menu.")]
        public KeyCode openKey = KeyCode.P;
        [Tooltip("How far in front of the player's eyes the menu opens, in metres, in vr and on desktop.")]
        public float vrDistance = 0.45f;
        public float desktopDistance = 0.7f;
        [Tooltip("The menu closes once the player walks this far from where it opened, in metres.")]
        public float closeDistance = 3f;

        [Header("Moving and sizing (vr)")]
        [Tooltip("The bar under the menu. Grab it to move the menu.")]
        public RectTransform moveBar;
        [Tooltip("The four corners. Grab one to make the menu bigger or smaller.")]
        public RectTransform[] corners;
        public Image[] handleImages;
        [Tooltip("How close a hand has to be to a handle to grab it, in metres.")]
        public float grabDistance = 0.09f;
        [Tooltip("Let go while moving faster than this, in metres a second, and the menu shatters and closes.")]
        public float throwSpeed = 2.2f;
        public float minSize = 0.55f;
        public float maxSize = 1.8f;

        [Header("Shatter")]
        [Tooltip("Takes one picture of the menu the moment it is thrown, for the shards.")]
        public Camera snapshotCamera;
        public Renderer shards;
        public float shatterSeconds = 1.1f;
        public AudioClip shatterClip;

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

        [Header("Linked world addresses")]
        public VRCUrl refreshUrl;
        public VRCUrl signOutUrl;
        [Tooltip("/w/s/0 upwards: picks the batch the next action is about.")]
        public VRCUrl[] selectSlotUrls;
        [Tooltip("/w/f/TILE/0 and /1, two per tile: unfavorite and favorite.")]
        public VRCUrl[] favoriteUrls;
        [Tooltip("/w/t/TILE: picks an avatar for a folder action.")]
        public VRCUrl[] pickUrls;
        [Tooltip("/w/m/FOLDER, 0 for no folder.")]
        public VRCUrl[] moveUrls;
        [Tooltip("/w/fd/FOLDER, from 1.")]
        public VRCUrl[] deleteFolderUrls;
        [Tooltip("What a new folder's name is typed after.")]
        public VRCUrl createFolderPrefixUrl;

        [Header("Profile")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI playerLabel;
        public TextMeshProUGUI initialLabel;
        public Image avatarCircle;
        public Image avatarRing;
        [Tooltip("The soft light behind the picture, in the tier's colour.")]
        public Image avatarGlow;
        public RawImage avatarPicture;
        public TextMeshProUGUI tierLabel;
        public Image tierPill;
        public TextMeshProUGUI rankLabel;
        public TextMeshProUGUI creditsValue;
        public TextMeshProUGUI favoritesValue;
        public TextMeshProUGUI foldersValue;
        public TextMeshProUGUI tierValue;
        public TextMeshProUGUI signedInAtLabel;
        public RectTransform limitFill;
        public TextMeshProUGUI limitLabel;

        [Header("Recent favorites")]
        public RawImage[] recentPictures;
        public GameObject[] recentSlots;
        public TextMeshProUGUI[] recentNames;
        [Tooltip("Shown while there are no favorites yet.")]
        public GameObject recentEmpty;

        [Header("Folders")]
        public GameObject[] folderRows;
        public TextMeshProUGUI[] folderNames;
        public TextMeshProUGUI[] folderCounts;
        public TextMeshProUGUI folderPageLabel;
        [Tooltip("Shown while there are no folders yet.")]
        public GameObject foldersEmpty;

        [Header("Music")]
        public AudioSource music;
        [Range(0f, 1f)] public float musicVolume = 0.25f;
        public TextMeshProUGUI musicLabel;
        public TextMeshProUGUI musicIcon;
        public TextMeshProUGUI volumeLabel;
        public RectTransform volumeFill;

        [Header("Sounds")]
        public AudioSource voice;
        public AudioClip openClip;
        public AudioClip closeClip;

        [Header("Colours")]
        public Color accent = new Color(0.52f, 0.48f, 0.99f, 1f);
        public Color idle = new Color(1f, 1f, 1f, 0.45f);

        // read by the folders panel and the wall
        [HideInInspector] public bool signedIn;
        [HideInInspector] public int favoriteCount;
        [HideInInspector] public int favoriteLimit;
        [HideInInspector] public int maxFolders = 20;
        [HideInInspector] public string[] folderNameList = new string[0];
        [HideInInspector] public int[] folderCountList = new int[0];
        [HideInInspector] public string lastMessage = "";
        // true while a hand moves or sizes the menu, and a moment after, for the touch screen
        [HideInInspector] public bool handsBusy;

        private bool _open;
        private float _baseScale = 0.00045f;
        private float _size = 1f;
        private Vector3 _openedAt;
        private float _openTime = -9f;
        private float _wristCooldown;
        private float _wristFade;
        private float _wristSeen = -9f;
        private bool _vr;
        private float _nextCheck;

        // grabbing: which hand holds which handle, -1 for none, 0 the bar, 1 a corner
        private int _grabHand = -1;
        private int _grabMode = -1;
        private Vector3 _grabOffset;
        private float _grabStartDistance;
        private float _grabStartSize;
        private Vector3 _lastHand;
        private Vector3 _velocity;
        private bool _shattering;
        private float _releasedAt = -9f;
        private float _shatterStart;

        private string _typed = "";
        private bool _wasFocused;

        // linked world requests go one at a time, in order
        private VRCUrl[] _queue = new VRCUrl[QueueSize];
        private int _queueHead;
        private int _queueCount;
        private bool _busy;
        private bool _signingIn;
        private int _serverSlot = -1;
        private int _serverTile = -1;
        private int _serverTileSlot = -1;

        private string _name = "";
        private string _tier = "";
        private string _rank = "";
        private int _credits;
        private string _signedInAt = "";
        private int _folderPage;
        private string[] _boardNames = new string[0];
        // the full favorites list for this visit, the saved profile keeps only the summary
        private DataList _favorites;
        private DataDictionary _favoriteIds = new DataDictionary();
        [HideInInspector] public int siteSlot = -1;
        private VRCImageDownloader _pictureLoader;
        private Texture2D _picture;
        private string _pictureUrl = "";

        private bool _restored;
        private bool _musicOn = true;

        private void Start()
        {
            if (menu != null)
            {
                _baseScale = menu.localScale.x;
                menu.gameObject.SetActive(false);
            }
            if (wristButton != null) wristButton.gameObject.SetActive(false);
            if (shards != null) shards.gameObject.SetActive(false);
            if (snapshotCamera != null) snapshotCamera.enabled = false;
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

            if (_shattering)
            {
                Shatter();
                return;
            }
            if (!_open) return;
            if (_grabHand < 0 && Vector3.Distance(player.GetPosition(), _openedAt) > closeDistance)
            {
                _Close();
                return;
            }
            // a little rise and grow as it opens
            float k = Mathf.Clamp01((Time.time - _openTime) / 0.28f);
            float ease = 1f - (1f - k) * (1f - k) * (1f - k);
            float grow = Mathf.Lerp(0.86f, 1f, ease);
            if (_vr) Handles(player);
            handsBusy = _grabHand >= 0 || Time.time - _releasedAt < 0.45f;
            menu.localScale = Vector3.one * (_baseScale * _size * grow);

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
            if (player == null || menu == null || _shattering) return;
            VRCPlayerApi.TrackingData head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            Vector3 forward = head.rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = player.GetRotation() * Vector3.forward;
            forward.Normalize();
            float distance = (_vr ? vrDistance : desktopDistance) * Mathf.Lerp(1f, _size, 0.6f);
            menu.position = head.position + forward * distance + Vector3.down * (_vr ? 0.1f : 0.04f);
            // tipped back a little, like a tablet held in front of you
            menu.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(_vr ? 12f : 4f, 0f, 0f);
            menu.gameObject.SetActive(true);
            // the grab handles are for hands, desktop players have the size buttons
            if (handleImages != null)
            {
                for (int i = 0; i < handleImages.Length; i++) if (handleImages[i] != null) handleImages[i].gameObject.SetActive(_vr);
            }
            _open = true;
            _openedAt = player.GetPosition();
            _openTime = Time.time;
            ResetField();
            ShowProfile();
            Play(openClip, 0.4f);
        }

        public void _Close()
        {
            if (!_open) return;
            _open = false;
            _grabHand = -1;
            handsBusy = false;
            if (menu != null) menu.gameObject.SetActive(false);
            Play(closeClip, 0.4f);
        }

        public void _Bigger() { SetSize(_size + 0.15f); }
        public void _Smaller() { SetSize(_size - 0.15f); }

        private void SetSize(float size)
        {
            _size = Mathf.Clamp(size, minSize, maxSize);
            if (_restored) PlayerData.SetFloat(SizeKey, _size);
        }

        /* ───────────── grabbing, sizing and throwing ───────────── */

        public override void InputGrab(bool value, UdonInputEventArgs args)
        {
            if (!_open || !_vr || _shattering) return;
            int hand = args.handType == HandType.LEFT ? 0 : 1;
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;
            if (value)
            {
                if (_grabHand >= 0) return;
                Vector3 at = HandPosition(player, hand);
                int mode = -1;
                float best = grabDistance;
                if (moveBar != null)
                {
                    float d = Vector3.Distance(at, moveBar.position);
                    if (d < best) { best = d; mode = 0; }
                }
                if (corners != null)
                {
                    for (int i = 0; i < corners.Length; i++)
                    {
                        if (corners[i] == null) continue;
                        float d = Vector3.Distance(at, corners[i].position);
                        if (d < best) { best = d; mode = 1; }
                    }
                }
                if (mode < 0) return;
                _grabHand = hand;
                _grabMode = mode;
                _grabOffset = menu.position - at;
                _grabStartDistance = Mathf.Max(0.02f, Vector3.Distance(at, menu.position));
                _grabStartSize = _size;
                _lastHand = at;
                _velocity = Vector3.zero;
                player.PlayHapticEventInHand(hand == 0 ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, 0.04f, 0.5f, 180f);
                return;
            }
            if (hand != _grabHand) return;
            _releasedAt = Time.time;
            bool thrown = _grabMode == 0 && _velocity.magnitude > throwSpeed;
            _grabHand = -1;
            if (_grabMode == 1 && _restored) PlayerData.SetFloat(SizeKey, _size);
            if (thrown) StartShatter(_velocity);
        }

        private Vector3 HandPosition(VRCPlayerApi player, int hand)
        {
            return player.GetTrackingData(hand == 0 ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).position;
        }

        private void Handles(VRCPlayerApi player)
        {
            Vector3 head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            // the handles light up as a hand comes near
            if (handleImages != null)
            {
                Vector3 left = HandPosition(player, 0);
                Vector3 right = HandPosition(player, 1);
                for (int i = 0; i < handleImages.Length; i++)
                {
                    Image image = handleImages[i];
                    if (image == null) continue;
                    Vector3 at = image.rectTransform.position;
                    float near = Mathf.Min(Vector3.Distance(left, at), Vector3.Distance(right, at));
                    float glow = _grabHand >= 0 ? 1f : 1f - Mathf.Clamp01((near - grabDistance * 0.5f) / grabDistance);
                    Color color = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), accent, glow);
                    color.a = Mathf.Lerp(0.35f, 1f, glow);
                    image.color = color;
                }
            }
            if (_grabHand < 0) return;
            Vector3 hand = HandPosition(player, _grabHand);
            Vector3 step = (hand - _lastHand) / Mathf.Max(Time.deltaTime, 0.001f);
            _velocity = Vector3.Lerp(_velocity, step, 0.5f);
            _lastHand = hand;
            if (_grabMode == 0)
            {
                menu.position = hand + _grabOffset;
                Vector3 look = menu.position - head;
                if (look.sqrMagnitude > 0.0001f) menu.rotation = Quaternion.LookRotation(look);
                _openedAt = player.GetPosition();
            }
            else
            {
                float distance = Vector3.Distance(hand, menu.position);
                _size = Mathf.Clamp(_grabStartSize * distance / _grabStartDistance, minSize, maxSize);
            }
        }

        // one picture of the menu, then the shards fly off along the throw and fade
        private void StartShatter(Vector3 velocity)
        {
            if (shards == null || snapshotCamera == null)
            {
                _Close();
                return;
            }
            _velocity = velocity;
            Transform cam = snapshotCamera.transform;
            float height = menu.rect.height * menu.lossyScale.y;
            cam.position = menu.position - menu.forward * 0.3f;
            cam.rotation = menu.rotation;
            snapshotCamera.orthographicSize = height * 0.5f;
            snapshotCamera.nearClipPlane = 0.2f;
            snapshotCamera.farClipPlane = 0.4f;
            snapshotCamera.enabled = true;
            SendCustomEventDelayedFrames(nameof(_BeginShards), 1);
        }

        public void _BeginShards()
        {
            snapshotCamera.enabled = false;
            Transform piece = shards.transform;
            piece.position = menu.position;
            piece.rotation = menu.rotation;
            float parentScale = piece.parent == null ? 1f : piece.parent.lossyScale.x;
            piece.localScale = Vector3.one * (menu.lossyScale.x / parentScale);
            Material material = shards.material;
            material.SetFloat("_Progress", 0f);
            material.SetVector("_Throw", piece.InverseTransformVector(_velocity));
            material.SetVector("_Gravity", piece.InverseTransformVector(Vector3.down * 3.5f));
            shards.gameObject.SetActive(true);
            menu.gameObject.SetActive(false);
            _open = false;
            _grabHand = -1;
            _shattering = true;
            _shatterStart = Time.time;
            if (voice != null && shatterClip != null)
            {
                voice.transform.position = piece.position;
                voice.PlayOneShot(shatterClip, 0.6f);
            }
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player != null)
            {
                player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.12f, 0.8f, 120f);
                player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.12f, 0.8f, 120f);
            }
        }

        private void Shatter()
        {
            float p = Mathf.Clamp01((Time.time - _shatterStart) / shatterSeconds);
            shards.material.SetFloat("_Progress", p);
            if (p < 1f) return;
            shards.gameObject.SetActive(false);
            _shattering = false;
        }

        // the button sits on the back of the left wrist, like a watch face, and only shows while
        // the right hand is close to that side of the forearm. the fingertip taps it, or the vrchat
        // laser clicks it. it stays a moment after the hand moves off, so the laser can reach it
        private void Wrist(VRCPlayerApi player)
        {
            if (wristButton == null) return;
            Vector3 head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Vector3 hand = player.GetBonePosition(HumanBodyBones.LeftHand);
            Vector3 elbow = player.GetBonePosition(HumanBodyBones.LeftLowerArm);
            if (hand == Vector3.zero) hand = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            Vector3 wrist = hand;
            if (elbow == Vector3.zero) elbow = hand + (head - hand).normalized * 0.25f + Vector3.down * 0.1f;
            else wrist = Vector3.Lerp(elbow, hand, 0.85f);
            Vector3 tip = Fingertip(player);

            // the back of the hand, from the knuckles: the palm faces the other way
            Vector3 back = Vector3.zero;
            Vector3 index = player.GetBonePosition(HumanBodyBones.LeftIndexProximal);
            Vector3 little = player.GetBonePosition(HumanBodyBones.LeftLittleProximal);
            if (index != Vector3.zero && little != Vector3.zero && hand != Vector3.zero) back = Vector3.Cross(little - hand, index - hand);
            bool known = back.sqrMagnitude > 0.0000001f;
            back = known ? back.normalized : Vector3.up;

            // how far the fingertip is from the forearm, elbow to wrist, and on which side of it
            Vector3 arm = wrist - elbow;
            float along = arm.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(tip - elbow, arm) / arm.sqrMagnitude) : 1f;
            Vector3 nearest = elbow + arm * along;
            float fromArm = Vector3.Distance(tip, nearest);
            bool onBack = !known || Vector3.Dot(tip - nearest, back) > 0.005f;
            if (fromArm < hoverDistance && onBack) _wristSeen = Time.time;
            bool showing = Time.time - _wristSeen < 1.2f;
            _wristFade = Mathf.MoveTowards(_wristFade, showing ? 1f : 0f, Time.deltaTime * (showing ? 8f : 4f));
            if (_wristFade <= 0f)
            {
                if (wristButton.gameObject.activeSelf) wristButton.gameObject.SetActive(false);
                return;
            }
            if (!wristButton.gameObject.activeSelf) wristButton.gameObject.SetActive(true);
            Vector3 spot = wrist + back * 0.045f;
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
            if (_wristFade >= 1f && gap < wristTapDistance) _WristPressed();
        }

        // the fingertip tap and the laser click both land here, so one press never counts twice
        public void _WristPressed()
        {
            if (Time.time < _wristCooldown) return;
            _wristCooldown = Time.time + 0.8f;
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player != null) player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.06f, 0.7f, 200f);
            _Toggle();
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
            if (signInField == null) return;
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
            _signingIn = true;
            SetStatus("Signing in...");
            Enqueue(url);
        }

        public void _SignOut()
        {
            if (signedIn) Enqueue(signOutUrl);
            ForgetAccount();
            SetStatus("Signed out.");
        }

        private void ForgetAccount()
        {
            signedIn = false;
            _name = "";
            _tier = "";
            _rank = "";
            _credits = 0;
            favoriteCount = 0;
            favoriteLimit = 0;
            _signedInAt = "";
            folderNameList = new string[0];
            folderCountList = new int[0];
            _favorites = null;
            _favoriteIds = new DataDictionary();
            _boardNames = new string[0];
            _serverSlot = -1;
            _serverTile = -1;
            siteSlot = -1;
            SetPicture(-1);
            if (_restored) PlayerData.SetString(ProfileKey, "");
            ShowProfile();
            Changed();
        }

        /* ───────────── syncing and changes, for the wall and the folders panel ───────────── */

        public void _Sync()
        {
            if (!signedIn)
            {
                SetStatus("Sign in first to sync with aviseek.vrchatlegends.com.");
                return;
            }
            SetStatus("Syncing with the website...");
            Enqueue(refreshUrl);
        }

        public bool IsFavorite(string id)
        {
            return id != null && _favoriteIds.ContainsKey(id);
        }

        // slot and tile name the avatar on a list the server handed this client
        public void ToggleFavorite(string id, int slot, int tile)
        {
            if (!signedIn)
            {
                SetStatus("Sign in to save favorites.");
                return;
            }
            bool on = !IsFavorite(id);
            if (on && favoriteLimit > 0 && favoriteCount >= favoriteLimit)
            {
                Tell("Your collection is full (" + favoriteLimit + " favorites). Remove one, or get more slots at aviseek.vrchatlegends.com");
                return;
            }
            if (!Select(slot) || tile < 0 || favoriteUrls == null || tile * 2 + 1 >= favoriteUrls.Length) return;
            // shown at once, the answer puts it right if the website says no
            if (on) _favoriteIds.SetValue(id, -1);
            else _favoriteIds.Remove(id);
            favoriteCount += on ? 1 : -1;
            Changed();
            Tell(on ? "Saving to your favorites..." : "Removing from your favorites...");
            Enqueue(favoriteUrls[tile * 2 + (on ? 1 : 0)]);
        }

        // folder 0 takes the avatar out of its folder, 1 and up are the folders in order
        public void MoveToFolder(string id, int slot, int tile, int folder)
        {
            if (!signedIn) return;
            if (!IsFavorite(id))
            {
                Tell("Favorite the avatar first, then put it in a folder.");
                return;
            }
            if (moveUrls == null || folder < 0 || folder >= moveUrls.Length || pickUrls == null || tile < 0 || tile >= pickUrls.Length) return;
            if (!Select(slot)) return;
            if (_serverTile != tile || _serverTileSlot != slot)
            {
                Enqueue(pickUrls[tile]);
                _serverTile = tile;
                _serverTileSlot = slot;
            }
            Tell(folder == 0 ? "Taking it out of its folder..." : "Moving to " + folderNameList[folder - 1] + "...");
            Enqueue(moveUrls[folder]);
        }

        public void DeleteFolder(int folder)
        {
            if (!signedIn || deleteFolderUrls == null || folder < 1 || folder >= deleteFolderUrls.Length) return;
            Tell("Deleting " + folderNameList[folder - 1] + "...");
            Enqueue(deleteFolderUrls[folder]);
        }

        // the address the player typed in the folders panel, checked to be the create address
        public void CreateFolder(VRCUrl typed)
        {
            if (!signedIn || typed == null || createFolderPrefixUrl == null) return;
            string text = typed.Get();
            string prefix = createFolderPrefixUrl.Get();
            if (text == null || !text.StartsWith(prefix) || text.Length <= prefix.Length)
            {
                Tell("Type the folder's name after the address.");
                return;
            }
            if (folderNameList.Length >= maxFolders)
            {
                Tell("You can have at most " + maxFolders + " folders.");
                return;
            }
            Tell("Making the folder...");
            Enqueue(typed);
        }

        private bool Select(int slot)
        {
            if (selectSlotUrls == null || slot < 0 || slot >= selectSlotUrls.Length || selectSlotUrls[slot] == null)
            {
                Tell("Load the list again first.");
                return false;
            }
            if (_serverSlot == slot) return true;
            Enqueue(selectSlotUrls[slot]);
            _serverSlot = slot;
            _serverTile = -1;
            return true;
        }

        private void Changed()
        {
            if (search != null) search._SiteChanged();
            if (folders != null) folders.Refresh();
            if (supporters != null) supporters.SetMine(_boardNames);
        }

        private void Tell(string text)
        {
            lastMessage = text;
            SetStatus(text);
            if (folders != null) folders.SetStatus(text);
            if (search != null) search.SetSiteStatus(text);
        }

        /* ───────────── requests ───────────── */

        private void Enqueue(VRCUrl url)
        {
            if (url == null) return;
            if (_queueCount >= QueueSize)
            {
                Tell("Slow down a little, still saving the last few changes.");
                return;
            }
            _queue[(_queueHead + _queueCount) % QueueSize] = url;
            _queueCount++;
            Pump();
        }

        private void Pump()
        {
            if (_busy || _queueCount == 0) return;
            VRCUrl url = _queue[_queueHead];
            _queue[_queueHead] = null;
            _queueHead = (_queueHead + 1) % QueueSize;
            _queueCount--;
            _busy = true;
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _busy = false;
            bool signingIn = _signingIn && signInPrefixUrl != null && result.Url != null && result.Url.Get().StartsWith(signInPrefixUrl.Get());
            if (signingIn)
            {
                _signingIn = false;
                ResetField();
            }
            ApplyAnswer(result.Result, signingIn);
            Pump();
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _busy = false;
            _signingIn = false;
            _serverSlot = -1;
            _serverTile = -1;
            Tell("<color=#ff7a86>Could not reach AviSeek (" + result.ErrorCode + "). Press Sync to try again.</color>");
            Pump();
        }

        private void ApplyAnswer(string json, bool signingIn)
        {
            DataToken root;
            if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary)
            {
                Tell("<color=#ff7a86>AviSeek sent something unreadable.</color>");
                return;
            }
            DataDictionary data = root.DataDictionary;
            DataToken token;
            bool ok = data.TryGetValue("ok", TokenType.Boolean, out token) && token.Boolean;
            if (data.TryGetValue("signedOut", TokenType.Boolean, out token) && token.Boolean)
            {
                if (signedIn)
                {
                    ForgetAccount();
                    Tell("This world was signed out on the website, or its day ran out. Sign in again with a new code.");
                }
                return;
            }
            string reason = ReadString(data, "reason");
            string message = ReadString(data, "message");
            // a profile comes with every answer that changed something
            if (data.TryGetValue("account", TokenType.DataDictionary, out token))
            {
                DataDictionary account = token.DataDictionary;
                // the code was made for one vrchat player. anyone else holding it gets nothing
                string meant = ReadString(account, "vrchatName").Trim();
                string me = Networking.LocalPlayer == null ? "" : Networking.LocalPlayer.displayName.Trim();
                if (meant.Length == 0 || meant.ToLower() != me.ToLower())
                {
                    if (signingIn) Enqueue(signOutUrl);
                    ForgetAccount();
                    Tell("<color=#ff7a86>That code was made for " + Escape(meant) + ", not you. Get your own at aviseek.vrchatlegends.com/account</color>");
                    return;
                }
                ReadProfile(data);
                SaveProfile();
                ShowProfile();
                Changed();
                if (signingIn) message = "Signed in as " + Escape(_name) + ". Favorites and folders now save to your account.";
                else if (message.Length == 0 && reason.Length == 0) message = "Synced with the website.";
            }
            else if (!ok && signingIn && reason.Length == 0) reason = "That code did not work.";
            if (!ok)
            {
                // the slot or tile may be what was refused, so pick it again next time
                _serverSlot = -1;
                _serverTile = -1;
                if (reason.Length > 0) Tell("<color=#ff7a86>" + Escape(reason) + "</color>");
                return;
            }
            if (message.Length > 0) Tell(Escape(message));
        }

        private void ReadProfile(DataDictionary data)
        {
            DataToken token;
            DataDictionary account = data.TryGetValue("account", TokenType.DataDictionary, out token) ? token.DataDictionary : new DataDictionary();
            _name = Clip(ReadString(account, "name"), 40);
            _tier = Clip(ReadString(account, "tier"), 24);
            _rank = Clip(ReadString(account, "rank"), 24);
            if (account.TryGetValue("boardNames", TokenType.DataList, out token))
            {
                DataList names = token.DataList;
                _boardNames = new string[names.Count];
                for (int i = 0; i < names.Count; i++) _boardNames[i] = names[i].TokenType == TokenType.String ? names[i].String : "";
            }
            _credits = data.TryGetValue("credits", TokenType.DataDictionary, out token) ? (int)ReadNumber(token.DataDictionary, "balance") : 0;
            favoriteLimit = (int)ReadNumber(data, "limit");
            int max = (int)ReadNumber(data, "maxFolders");
            if (max > 0) maxFolders = max;
            _signedInAt = ReadString(data, "signedInAt");
            _favorites = data.TryGetValue("favorites", TokenType.DataList, out token) ? token.DataList : new DataList();
            _favoriteIds = new DataDictionary();
            for (int i = 0; i < _favorites.Count; i++)
            {
                if (_favorites[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary row = _favorites[i].DataDictionary;
                // where it sits in the favorites' picture sheet, for the wall
                row.SetValue("_i", i);
                _favoriteIds.SetValue(ReadString(row, "id"), i);
            }
            favoriteCount = Mathf.Max((int)ReadNumber(data, "totalFavorites"), _favorites.Count);
            ReadFolders(data.TryGetValue("folders", TokenType.DataList, out token) ? token.DataList : new DataList());
            signedIn = true;
            if (data.TryGetValue("pics", out token) && token.IsNumber)
            {
                siteSlot = (int)token.Number;
                if (search != null) search.LoadSiteSheet(siteSlot, _favorites, ReadNumber(data, "picsPending") > 0);
            }
            SetPicture(data.TryGetValue("pfp", out token) && token.IsNumber ? (int)token.Number : -1);
        }

        private void ReadFolders(DataList list)
        {
            int count = Mathf.Min(list.Count, 60);
            string[] names = new string[count];
            int[] counts = new int[count];
            int kept = 0;
            for (int i = 0; i < count; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                names[kept] = Clip(ReadString(list[i].DataDictionary, "name"), 32);
                counts[kept] = (int)ReadNumber(list[i].DataDictionary, "count");
                kept++;
            }
            folderNameList = new string[kept];
            folderCountList = new int[kept];
            for (int i = 0; i < kept; i++)
            {
                folderNameList[i] = names[i];
                folderCountList[i] = counts[i];
            }
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

        /* ───────────── the account picture ───────────── */

        // the picture comes as the only tile of its own picture sheet
        private void SetPicture(int slot)
        {
            VRCUrl url = slot >= 0 && search != null && search.pictureUrls != null && slot < search.pictureUrls.Length ? search.pictureUrls[slot] : null;
            if (url == null)
            {
                _pictureUrl = "";
                if (_pictureLoader != null) _pictureLoader.Dispose();
                _pictureLoader = null;
                _picture = null;
                ShowPicture();
                return;
            }
            if (url.Get() == _pictureUrl && _picture != null) return;
            _pictureUrl = url.Get();
            if (_pictureLoader != null) _pictureLoader.Dispose();
            _pictureLoader = new VRCImageDownloader();
            TextureInfo info = new TextureInfo();
            info.GenerateMipMaps = false;
            info.FilterMode = FilterMode.Bilinear;
            info.WrapModeU = TextureWrapMode.Clamp;
            info.WrapModeV = TextureWrapMode.Clamp;
            _pictureLoader.DownloadImage(url, null, (IUdonEventReceiver)this, info);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            if (result.Url == null || result.Url.Get() != _pictureUrl) return;
            _picture = result.Result;
            ShowPicture();
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            Debug.LogWarning("[AviSeek] Account picture failed: " + result.ErrorMessage);
        }

        private void ShowPicture()
        {
            if (avatarPicture == null) return;
            bool has = _picture != null && signedIn;
            avatarPicture.enabled = has;
            avatarPicture.texture = has ? _picture : null;
            // the middle square of the first 204 x 153 tile
            avatarPicture.uvRect = new Rect(25.5f / 2040f, 1f - 153f / 1836f, 153f / 2040f, 153f / 1836f);
            if (initialLabel != null) initialLabel.gameObject.SetActive(!has);
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
            float size;
            if (PlayerData.TryGetFloat(player, SizeKey, out size)) _size = Mathf.Clamp(size, minSize, maxSize);
            ApplyMusic();
            string saved;
            if (!signedIn && PlayerData.TryGetString(player, ProfileKey, out saved) && saved.Length > 0)
            {
                LoadProfile(saved);
                // still linked from last time? this brings the favorites, folders and pictures
                if (signedIn) Enqueue(refreshUrl);
            }
            ShowProfile();
            Changed();
        }

        // only the summary is kept between visits, never favorites or a code
        private void SaveProfile()
        {
            if (!_restored) return;
            DataDictionary data = new DataDictionary();
            data.SetValue("name", _name);
            data.SetValue("tier", _tier);
            data.SetValue("rank", _rank);
            data.SetValue("credits", _credits);
            data.SetValue("total", favoriteCount);
            data.SetValue("limit", favoriteLimit);
            data.SetValue("at", _signedInAt);
            DataList list = new DataList();
            for (int i = 0; i < folderNameList.Length; i++)
            {
                DataDictionary folder = new DataDictionary();
                folder.SetValue("name", folderNameList[i]);
                folder.SetValue("count", folderCountList[i]);
                list.Add(folder);
            }
            data.SetValue("folders", list);
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
            favoriteCount = (int)ReadNumber(data, "total");
            favoriteLimit = (int)ReadNumber(data, "limit");
            _signedInAt = ReadString(data, "at");
            DataToken token;
            ReadFolders(data.TryGetValue("folders", TokenType.DataList, out token) ? token.DataList : new DataList());
            _favorites = null;
            signedIn = true;
        }

        /* ───────────── showing it ───────────── */

        private void ShowProfile()
        {
            if (signedInRoot != null) signedInRoot.SetActive(signedIn);
            if (signedOutRoot != null) signedOutRoot.SetActive(!signedIn);
            string player = Networking.LocalPlayer == null ? "" : Networking.LocalPlayer.displayName;
            if (playerLabel != null) playerLabel.text = Escape(player);
            if (initialLabel != null) initialLabel.text = Initial(signedIn && _name.Length > 0 ? _name : player);
            Color tierColor = TierColor(_tier);
            if (avatarCircle != null) avatarCircle.color = signedIn ? new Color(tierColor.r * 0.45f, tierColor.g * 0.45f, tierColor.b * 0.45f, 1f) : new Color(1f, 1f, 1f, 0.08f);
            if (avatarRing != null) avatarRing.color = signedIn ? tierColor : new Color(1f, 1f, 1f, 0.15f);
            if (avatarGlow != null) avatarGlow.color = signedIn ? new Color(tierColor.r, tierColor.g, tierColor.b, 0.55f) : new Color(accent.r, accent.g, accent.b, 0.25f);
            ShowPicture();
            if (!signedIn)
            {
                if (nameLabel != null) nameLabel.text = "Not signed in";
                if (tierPill != null) tierPill.gameObject.SetActive(false);
                if (rankLabel != null) rankLabel.text = "";
                return;
            }
            if (nameLabel != null) nameLabel.text = Escape(_name);
            if (tierPill != null)
            {
                tierPill.gameObject.SetActive(true);
                tierPill.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.22f);
            }
            if (tierLabel != null)
            {
                tierLabel.text = _tier.Length > 0 ? Escape(_tier) : "Free";
                tierLabel.color = tierColor;
                // long tiers like Platinum Legend+ get a wider pill instead of spilling out of it
                if (tierPill != null)
                {
                    // udon can not measure text, about 11.5 units a letter at this size is close enough
                    float width = Mathf.Clamp(tierLabel.text.Length * 11.5f + 44f, 110f, 330f);
                    tierPill.rectTransform.sizeDelta = new Vector2(width, tierPill.rectTransform.sizeDelta.y);
                }
            }
            if (rankLabel != null) rankLabel.text = _rank.Length > 0 ? Escape(_rank) : "";
            if (creditsValue != null) creditsValue.text = _credits.ToString("N0");
            if (favoritesValue != null) favoritesValue.text = favoriteCount.ToString("N0");
            if (foldersValue != null) foldersValue.text = folderNameList.Length.ToString();
            if (tierValue != null)
            {
                tierValue.text = _tier.Length > 0 ? Escape(_tier) : "Free";
                tierValue.color = _tier.Length > 0 ? tierColor : Color.white;
            }
            if (limitLabel != null) limitLabel.text = favoriteCount + " / " + (favoriteLimit > 0 ? favoriteLimit.ToString() : "?") + " favorites";
            if (limitFill != null)
            {
                float k = favoriteLimit > 0 ? Mathf.Clamp01(favoriteCount / (float)favoriteLimit) : 0f;
                limitFill.localScale = new Vector3(Mathf.Max(0.001f, k), 1f, 1f);
            }
            if (signedInAtLabel != null)
            {
                string day = _signedInAt.Length >= 10 ? _signedInAt.Substring(0, 10) : "";
                signedInAtLabel.text = _favorites == null ? "Saved from " + day + ". Syncing..." : "Linked since " + day;
            }
            ShowRecent();
            ShowFolders();
        }

        // the first few favorites, with their pictures from the favorites' sheet
        private void ShowRecent()
        {
            if (recentEmpty != null) recentEmpty.SetActive(_favorites == null || _favorites.Count == 0);
            if (recentSlots == null) return;
            for (int i = 0; i < recentSlots.Length; i++)
            {
                bool has = _favorites != null && i < _favorites.Count && _favorites[i].TokenType == TokenType.DataDictionary;
                if (recentSlots[i] != null) recentSlots[i].SetActive(has);
                if (!has) continue;
                DataDictionary row = _favorites[i].DataDictionary;
                string id = ReadString(row, "id");
                if (recentNames != null && i < recentNames.Length && recentNames[i] != null) recentNames[i].text = Escape(Clip(ReadString(row, "name"), 18));
                if (recentPictures == null || i >= recentPictures.Length || recentPictures[i] == null || search == null) continue;
                Texture2D sheet = search.TileTexture(id);
                recentPictures[i].texture = sheet;
                recentPictures[i].uvRect = search.TileRect(id);
                recentPictures[i].enabled = sheet != null;
            }
        }

        // the wall calls this once new pictures are in
        public void _PicturesChanged()
        {
            if (_open) ShowRecent();
        }

        public void _Recent0() { ShowRecentOnWall(0); }
        public void _Recent1() { ShowRecentOnWall(1); }
        public void _Recent2() { ShowRecentOnWall(2); }
        public void _Recent3() { ShowRecentOnWall(3); }
        public void _Recent4() { ShowRecentOnWall(4); }

        private void ShowRecentOnWall(int index)
        {
            if (_favorites == null || index >= _favorites.Count || search == null) return;
            search.ShowFolderOnWall(-1);
            if (_favorites[index].TokenType == TokenType.DataDictionary) search.SelectById(ReadString(_favorites[index].DataDictionary, "id"));
            Tell("Your favorites are on the wall.");
        }

        private void ShowFolders()
        {
            if (folderRows == null) return;
            int size = folderRows.Length;
            int pages = Mathf.Max(1, (folderNameList.Length + size - 1) / size);
            if (_folderPage >= pages) _folderPage = pages - 1;
            int start = _folderPage * size;
            for (int i = 0; i < size; i++)
            {
                int index = start + i;
                bool has = index < folderNameList.Length;
                if (folderRows[i] != null) folderRows[i].SetActive(has);
                if (!has) continue;
                if (folderNames != null && i < folderNames.Length && folderNames[i] != null) folderNames[i].text = Escape(folderNameList[index]);
                if (folderCounts != null && i < folderCounts.Length && folderCounts[i] != null) folderCounts[i].text = folderCountList[index].ToString();
            }
            if (folderPageLabel != null) folderPageLabel.text = folderNameList.Length == 0 ? "" : (_folderPage + 1) + " / " + pages;
            if (foldersEmpty != null) foldersEmpty.SetActive(folderNameList.Length == 0);
        }

        public void _FolderNext()
        {
            int size = folderRows == null || folderRows.Length == 0 ? 1 : folderRows.Length;
            if ((_folderPage + 1) * size < folderNameList.Length) _folderPage++;
            ShowFolders();
        }

        public void _FolderPrev()
        {
            if (_folderPage > 0) _folderPage--;
            ShowFolders();
        }

        public void _Folder0() { MenuFolder(0); }
        public void _Folder1() { MenuFolder(1); }
        public void _Folder2() { MenuFolder(2); }
        public void _Folder3() { MenuFolder(3); }
        public void _Folder4() { MenuFolder(4); }
        public void _Folder5() { MenuFolder(5); }

        private void MenuFolder(int slot)
        {
            int size = folderRows == null || folderRows.Length == 0 ? 1 : folderRows.Length;
            ShowFolder(_folderPage * size + slot);
        }

        public void _AllFavorites()
        {
            if (!HaveFavorites()) return;
            search.ShowFolderOnWall(-1);
            Tell("Your favorites are on the wall.");
        }

        // a folder by its place, from 0, on the wall
        public void ShowFolder(int index)
        {
            if (index < 0 || index >= folderNameList.Length || !HaveFavorites()) return;
            search.ShowFolderOnWall(index);
            Tell(Escape(folderNameList[index]) + " is on the wall.");
        }

        // the favorites in a folder, or all of them for -1. null until they are synced
        public DataList FolderList(int index)
        {
            if (_favorites == null) return null;
            if (index < 0) return _favorites;
            DataList picked = new DataList();
            for (int i = 0; i < _favorites.Count; i++)
            {
                if (_favorites[i].TokenType != TokenType.DataDictionary) continue;
                if ((int)ReadNumber(_favorites[i].DataDictionary, "folder") == index) picked.Add(_favorites[i]);
            }
            return picked;
        }

        public string FolderName(int index)
        {
            return index >= 0 && index < folderNameList.Length ? folderNameList[index] : "All favorites";
        }

        private bool HaveFavorites()
        {
            if (search == null) return false;
            if (_favorites != null) return true;
            Tell(signedIn ? "Still syncing your favorites, one moment." : "Sign in first to see your favorites.");
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
            if (volumeFill != null) volumeFill.localScale = new Vector3(Mathf.Max(0.001f, playing ? musicVolume : 0f), 1f, 1f);
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

        private void Play(AudioClip clip, float loudness)
        {
            if (voice == null || clip == null) return;
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player != null) voice.transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            voice.PlayOneShot(clip, loudness);
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
