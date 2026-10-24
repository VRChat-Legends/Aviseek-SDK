using System;
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
    // the whole search wall. pulls 120 avatar batches off aviseek's public world
    // api as text, pages and filters them locally, and puts people into avatars
    // through one hidden pedestal. the pictures are vrchat's own, drawn by a small
    // pedestal on every card, so they never cost a download from aviseek.
    // nothing here is synced, every player browses on their own
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Search")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekSearch : UdonSharpBehaviour
    {
        private const string ClientPrefix = "https://aviseek.vrchatlegends.com/api/world/client?";
        private const string ShortPrefix = "https://aviapi.vrchatlegends.com/w?";
        private const string FavoritesKey = "aviseek.favorites";
        private const int MaxFavorites = 100;

        [Header("AviSeek API")]
        [Tooltip("What the search box resets to. Players type their search after q=")]
        public VRCUrl searchPrefixUrl;

        [Tooltip("What the search box resets to in creator mode. Players type a creator name after author=")]
        public VRCUrl creatorPrefixUrl;

        [Tooltip("The popular list, one url per batch of 120 (offset 0, 120, 240...). Paging past the end of one loads the next.")]
        public VRCUrl[] browseUrls;

        [Header("Short search address")]
        [Tooltip("VRChat shows players the whole address while they type, so the wall switches to these much shorter ones once the ping below answers.")]
        public VRCUrl shortSearchPrefixUrl;
        public VRCUrl shortCreatorPrefixUrl;
        [Tooltip("Checked once after the first load. Until it answers, the long address above stays in the search box.")]
        public VRCUrl shortPingUrl;

        [Header("Promoted")]
        [Tooltip("The promoted avatars and groups, downloaded after the first search and then every few minutes.")]
        public VRCUrl promotedUrl;
        public AviSeekPromoted promoted;
        public float promotedRefreshSeconds = 600f;

        [Header("Branding")]
        [Tooltip("The white word of the title.")]
        public string brandTitle = "AVATAR";
        [Tooltip("The coloured word of the title.")]
        public string brandHighlight = "SEARCH";
        public Sprite brandLogo;
        public TextMeshProUGUI titleLabel;
        public Image logoImage;
        [Tooltip("Graphics that take the accent colour. The inspector fills these.")]
        public Image[] accentImages;

        [Header("Search box")]
        [Tooltip("Holds the full AviSeek address. Its own text is hidden, players see the search display instead.")]
        public VRCUrlInputField searchField;
        [Tooltip("Shows only what the player typed, without the address in front.")]
        public TextMeshProUGUI searchDisplay;
        [Tooltip("Shown in the search box while nothing has been typed.")]
        public TextMeshProUGUI searchHint;

        [Header("Wall")]
        public AviSeekCard[] cards;
        public TextMeshProUGUI titleCountLabel;
        public TextMeshProUGUI pageLabel;
        public TextMeshProUGUI statusLabel;
        public TextMeshProUGUI statsLabel;
        [Tooltip("The Supporters panel. The supporter list arrives with every search.")]
        public AviSeekSupporters supporters;

        [Header("Filters")]
        [Tooltip("All, PC, Quest, iOS in that order.")]
        public Image[] platformTabs;
        public Image favoritesTab;
        public TextMeshProUGUI favoritesIcon;
        public TextMeshProUGUI favoritesLabel;
        [Tooltip("Avatar, Creator in that order. Picks what the search box looks through.")]
        public Image[] searchModeTabs;
        [Tooltip("Narrows whatever is loaded to creators whose name contains this.")]
        public TMP_InputField creatorFilterField;
        public TextMeshProUGUI sortLabel;
        public TextMeshProUGUI rankLabel;

        [Header("Selected avatar")]
        public GameObject detailsRoot;
        public GameObject detailsEmpty;
        public GameObject detailPlaceholder;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailAuthor;
        public TextMeshProUGUI detailRatings;
        public TextMeshProUGUI detailHeart;
        public TMP_InputField detailId;
        public GameObject detailPcBadge;
        public GameObject detailQuestBadge;
        public GameObject detailIosBadge;

        [Header("Avatar switching")]
        [Tooltip("A VRC Avatar Pedestal the wall drives. It sits far under the floor and only switches avatars.")]
        public GameObject pedestalObject;

        [Header("Pictures")]
        [Tooltip("VRChat's own picture of the selected avatar. It also measures that picture once, before the cards show theirs.")]
        public AviSeekPicture detailPicture;

        [Header("Colours")]
        public Color accent = new Color(0.525f, 0.486f, 0.996f, 1f);
        public Color tabIdle = new Color(1f, 1f, 1f, 0.06f);
        public Color heartOff = new Color(1f, 1f, 1f, 0.85f);
        public Color heartOn = new Color(1f, 0.44f, 0.6f, 1f);
        public Color frameOff = new Color(1f, 1f, 1f, 0.08f);
        public Color rateUnknown = new Color(0.36f, 0.33f, 0.45f, 1f);
        public Color rateExcellent = new Color(0.4f, 0.91f, 0.98f, 1f);
        public Color rateGood = new Color(0.43f, 0.91f, 0.72f, 1f);
        public Color rateMedium = new Color(0.91f, 0.73f, 0.35f, 1f);
        public Color ratePoor = new Color(0.96f, 0.56f, 0.3f, 1f);
        public Color rateVeryPoor = new Color(1f, 0.44f, 0.49f, 1f);
        [Tooltip("The logo of a platform the avatar has no version for.")]
        public Color platformOff = new Color(1f, 1f, 1f, 0.12f);

        // the batch from the api
        private string[] _bId = new string[0];
        private string[] _bName = new string[0];
        private string[] _bAuthor = new string[0];
        private int[] _bPlat = new int[0];
        private int[] _bPerf = new int[0];

        // favorites, kept with enough text to show them with no download
        private string[] _fId = new string[0];
        private string[] _fName = new string[0];
        private string[] _fAuthor = new string[0];
        private int[] _fPlat = new int[0];
        private int[] _fPerf = new int[0];
        private int _fCount;

        // whichever of the two the wall shows right now
        private string[] _rId;
        private string[] _rName;
        private string[] _rAuthor;
        private int[] _rPlat;
        private int[] _rPerf;
        private int _rCount;

        private int[] _view = new int[0];
        private int _viewCount;
        private int _page;

        private int _platformFilter;
        private int _sortMode;
        private int _rankFilter;
        private bool _showFavorites;

        private string _scope = "";
        private string _pendingScope = "";
        private bool _creatorMode;
        private string _creatorNeedle = "";
        private int _batchTotal;
        private int _browseBatch = -1;
        private int _pendingBrowseBatch = -1;
        private bool _pageToEndOnLoad;

        // vrchat's pedestal picture, measured once and then fitted to every spot
        private bool _picReady;
        private bool _picProbing;
        private float _picWidth;
        private float _picHeight;
        private Vector3 _picOffset;

        private VRCUrl _activeUrl;
        private VRCUrl _queuedUrl;
        private bool _loading;
        private float _nextAllowed;
        private bool _flushScheduled;

        private string _selId = "";
        private string _selName = "";
        private string _selAuthor = "";
        private int _selPlat;
        private int _selPerf;

        private bool _favoritesLoaded;

        private bool _useShort;
        private bool _pingWanted;
        private bool _pingLoading;
        private bool _promotedWanted;
        private bool _promotedLoading;
        private bool _promotedRequested;

        private string _typed = "";
        private bool _wasFocused;
        private int _iconRetries;
        private VRCUrl _failedUrl;
        private int _failures;

        private void Start()
        {
            _rId = _bId;
            _rName = _bName;
            _rAuthor = _bAuthor;
            _rPlat = _bPlat;
            _rPerf = _bPerf;
            ResetSearchField();
            RefreshTabs();
            RefreshSelection();
            Render();
            SetStatus("Loading popular avatars...");
            LoadBrowseBatch(0);
        }

        private void Update()
        {
            // the box holds the whole address, so the display copies only what was typed after it
            if (searchField != null)
            {
                bool focused = searchField.isFocused;
                if (focused || _wasFocused) RefreshSearchDisplay();
                _wasFocused = focused;
            }
        }

        private void RefreshSearchDisplay()
        {
            VRCUrl url = searchField.GetUrl();
            string text = url == null ? "" : url.Get();
            if (text == null) text = "";
            VRCUrl current = CurrentPrefix();
            string prefix = current != null ? current.Get() : "";
            string typed = prefix.Length > 0 && text.StartsWith(prefix) ? text.Substring(prefix.Length) : (OurAddress(text) ? "" : text);
            typed = typed.Replace("+", " ").Replace("%20", " ");
            if (typed == _typed) return;
            _typed = typed;
            ShowTyped();
        }

        private void ShowTyped()
        {
            if (searchDisplay != null) searchDisplay.text = _typed;
            if (searchHint != null)
            {
                searchHint.gameObject.SetActive(_typed.Length == 0);
                searchHint.text = _creatorMode ? "Search creators" : "Search avatars";
            }
        }

        /* ───────────── buttons ───────────── */

        public void _Search()
        {
            if (searchField == null) return;
            VRCUrl url = searchField.GetUrl();
            string text = url == null ? "" : url.Get();
            if (text == null || !OurAddress(text))
            {
                SetStatus("Click the search box, type what you want and press Enter.");
                ResetSearchField();
                return;
            }
            string query = UrlValue(text, "q=");
            string creator = UrlValue(text, "author=");
            if (creator.Length == 0) creator = UrlValue(text, "a=");
            if (query.Length == 0 && creator.Length == 0)
            {
                _Home();
                return;
            }
            _pendingScope = creator.Length > 0
                ? "avatars by \"" + Escape(creator) + "\"" + (query.Length > 0 ? " matching \"" + Escape(query) + "\"" : "")
                : "results for \"" + Escape(query) + "\"";
            _pendingBrowseBatch = -1;
            _pageToEndOnLoad = false;
            SetStatus("Searching...");
            Request(url);
        }

        public void _Home()
        {
            _showFavorites = false;
            ResetSearchField();
            _pageToEndOnLoad = false;
            SetStatus("Loading popular avatars...");
            LoadBrowseBatch(0);
        }

        public void _Refresh()
        {
            if (_activeUrl == null) return;
            _pendingBrowseBatch = _browseBatch;
            _pendingScope = _scope;
            SetStatus("Refreshing...");
            Request(_activeUrl);
        }

        public void _NextPage()
        {
            int pages = PageCount();
            if (_page < pages - 1)
            {
                _page++;
                Render();
                return;
            }
            // end of the batch, browsing walks on into the next 120
            if (!_showFavorites && _browseBatch >= 0 && (_browseBatch + 1) * 120 < _batchTotal && browseUrls != null && _browseBatch + 1 < browseUrls.Length)
            {
                _pageToEndOnLoad = false;
                SetStatus("Loading more...");
                LoadBrowseBatch(_browseBatch + 1);
            }
        }

        public void _PrevPage()
        {
            if (_page > 0)
            {
                _page--;
                Render();
                return;
            }
            if (!_showFavorites && _browseBatch > 0)
            {
                _pageToEndOnLoad = true;
                SetStatus("Loading...");
                LoadBrowseBatch(_browseBatch - 1);
            }
        }

        public void _PlatformAll() { SetPlatform(0); }
        public void _PlatformPc() { SetPlatform(1); }
        public void _PlatformQuest() { SetPlatform(2); }
        public void _PlatformIos() { SetPlatform(3); }

        public void _ToggleFavorites()
        {
            _showFavorites = !_showFavorites;
            UseSource();
            Rebuild();
        }

        public void _CycleSort()
        {
            _sortMode = (_sortMode + 1) % 3;
            Rebuild();
        }

        public void _CycleRank()
        {
            _rankFilter = (_rankFilter + 1) % 5;
            Rebuild();
        }

        public void _UseSelected()
        {
            if (_selId.Length > 0) UseAvatar(_selId);
        }

        public void _FavoriteSelected()
        {
            if (_selId.Length == 0) return;
            ToggleFavorite(_selId, _selName, _selAuthor, _selPlat, _selPerf);
        }

        public void _ModeAvatar() { SetSearchMode(false); }
        public void _ModeCreator() { SetSearchMode(true); }

        private void SetSearchMode(bool creator)
        {
            _creatorMode = creator;
            ResetSearchField();
            RefreshTabs();
            SetStatus(creator ? "Click the search box, type a creator's name and press Enter." : "Click the search box, type an avatar name and press Enter.");
        }

        private void ResetSearchField()
        {
            if (searchField == null) return;
            VRCUrl prefix = CurrentPrefix();
            if (prefix != null) searchField.SetUrl(prefix);
            _typed = "";
            ShowTyped();
        }

        // the short address once aviseek has answered on it, the long one until then
        private VRCUrl CurrentPrefix()
        {
            if (_useShort)
            {
                VRCUrl shortPrefix = _creatorMode ? shortCreatorPrefixUrl : shortSearchPrefixUrl;
                if (shortPrefix != null) return shortPrefix;
            }
            return _creatorMode && creatorPrefixUrl != null ? creatorPrefixUrl : searchPrefixUrl;
        }

        private bool OurAddress(string text)
        {
            return text.StartsWith(ClientPrefix) || text.StartsWith(ShortPrefix);
        }

        public void _CreatorFilterChanged()
        {
            string text = creatorFilterField == null ? "" : creatorFilterField.text;
            string needle = text == null ? "" : text.Trim().ToLower();
            if (needle == _creatorNeedle) return;
            _creatorNeedle = needle;
            Rebuild();
        }

        public void _ClearCreatorFilter()
        {
            if (creatorFilterField != null) creatorFilterField.text = "";
            _CreatorFilterChanged();
        }

        // the creator line on the selected avatar narrows the wall to them
        // the avatar id box is editable only so the vrchat keyboard opens for copying
        public void _RestoreId()
        {
            if (detailId != null && detailId.text != _selId) detailId.text = _selId;
        }

        public void _FilterBySelectedCreator()
        {
            if (_selAuthor.Length == 0) return;
            if (creatorFilterField != null) creatorFilterField.text = _selAuthor;
            _CreatorFilterChanged();
        }

        /* ───────────── cards ───────────── */

        public void _CardUse(int slot)
        {
            int row = RowAt(slot);
            if (row < 0) return;
            SelectRow(row);
            UseAvatar(_rId[row]);
        }

        public void _CardFavorite(int slot)
        {
            int row = RowAt(slot);
            if (row < 0) return;
            ToggleFavorite(_rId[row], _rName[row], _rAuthor[row], _rPlat[row], _rPerf[row]);
        }

        public void _CardSelect(int slot)
        {
            int row = RowAt(slot);
            if (row < 0) return;
            SelectRow(row);
            Render();
        }

        private int RowAt(int slot)
        {
            if (cards == null) return -1;
            int index = _page * cards.Length + slot;
            if (index < 0 || index >= _viewCount) return -1;
            return _view[index];
        }

        /* ───────────── downloads ───────────── */

        private void LoadBrowseBatch(int batch)
        {
            if (browseUrls == null || batch < 0 || batch >= browseUrls.Length || browseUrls[batch] == null) return;
            _pendingBrowseBatch = batch;
            _pendingScope = "";
            Request(browseUrls[batch]);
        }

        private void Request(VRCUrl url)
        {
            _queuedUrl = url;
            FlushQueue();
        }

        public void _FlushDelayed()
        {
            _flushScheduled = false;
            FlushQueue();
        }

        // vrchat allows one string download every five seconds, anything sooner waits
        // searches go first, the promoted list only takes a free slot
        private void FlushQueue()
        {
            if (_loading) return;
            bool forPing = _queuedUrl == null && _pingWanted && shortPingUrl != null;
            bool forPromoted = _queuedUrl == null && !forPing;
            if (forPromoted && (!_promotedWanted || promotedUrl == null)) return;
            float wait = _nextAllowed - Time.realtimeSinceStartup;
            if (wait > 0f)
            {
                if (!_flushScheduled)
                {
                    _flushScheduled = true;
                    SendCustomEventDelayedSeconds(nameof(_FlushDelayed), wait + 0.05f);
                }
                return;
            }
            VRCUrl url;
            if (forPing)
            {
                url = shortPingUrl;
                _pingWanted = false;
                _pingLoading = true;
            }
            else if (forPromoted)
            {
                url = promotedUrl;
                _promotedWanted = false;
                _promotedLoading = true;
            }
            else
            {
                url = _queuedUrl;
                _queuedUrl = null;
                _activeUrl = url;
            }
            _loading = true;
            _nextAllowed = Time.realtimeSinceStartup + 5.5f;
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public void _RequestPromoted()
        {
            if (promoted == null || promotedUrl == null || !promoted.gameObject.activeInHierarchy) return;
            _promotedWanted = true;
            FlushQueue();
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _loading = false;
            if (_pingLoading)
            {
                _pingLoading = false;
                DataToken pong;
                if (VRCJson.TryDeserializeFromJson(result.Result, out pong) && pong.TokenType == TokenType.DataDictionary && ReadNumber(pong.DataDictionary, "short") >= 1)
                {
                    _useShort = true;
                    // a player halfway through typing keeps the box they have
                    if (searchField != null && !searchField.isFocused && _typed.Length == 0) ResetSearchField();
                }
                FlushQueue();
                return;
            }
            if (_promotedLoading)
            {
                _promotedLoading = false;
                if (promoted != null) promoted.Apply(result.Result);
                // group icons aviseek is still fetching come with the next copy of the list
                bool icons = promoted != null && promoted.iconsPending > 0 && _iconRetries < 6;
                if (icons) _iconRetries++;
                else _iconRetries = 0;
                SendCustomEventDelayedSeconds(nameof(_RequestPromoted), icons ? 30f : Mathf.Max(promotedRefreshSeconds, 120f));
                FlushQueue();
                return;
            }
            bool current = _queuedUrl == null;
            _failures = 0;
            _failedUrl = null;
            if (current) Apply(result.Result);
            if (_queuedUrl != null) FlushQueue();
        }

        public void _RetryFailed()
        {
            if (_failedUrl == null || _loading || _queuedUrl != null) return;
            _queuedUrl = _failedUrl;
            _failedUrl = null;
            FlushQueue();
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _loading = false;
            if (_pingLoading)
            {
                // no short address on this server yet: the long one keeps working
                _pingLoading = false;
                FlushQueue();
                return;
            }
            if (_promotedLoading)
            {
                _promotedLoading = false;
                Debug.LogWarning("[AviSeek] Promoted list failed: " + result.ErrorCode + " " + result.Error);
                SendCustomEventDelayedSeconds(nameof(_RequestPromoted), 120f);
                FlushQueue();
                return;
            }
            string error = result.Error == null ? "" : result.Error;
            if (result.ErrorCode == 429)
            {
                _nextAllowed = Time.realtimeSinceStartup + 30f;
                SetStatus("AviSeek is busy, trying again in 30 seconds.");
                if (_queuedUrl == null) _queuedUrl = result.Url;
            }
            else if (error.IndexOf("ntrusted") >= 0)
            {
                SetStatus("<color=#ff7a86>Turn on <b>Allow Untrusted URLs</b></color> in VRChat Settings > Comfort & Safety, then press refresh.");
            }
            else
            {
                // a hiccup on the way, such as a 502 while the server restarts, is tried again by itself
                _failures++;
                bool again = _failures <= 5 && _queuedUrl == null;
                SetStatus("<color=#ff7a86>Couldn't reach AviSeek.</color> " + error + (again ? " Trying again in 20 seconds." : ""));
                if (again)
                {
                    _failedUrl = result.Url;
                    SendCustomEventDelayedSeconds(nameof(_RetryFailed), 20f);
                }
            }
            Debug.LogWarning("[AviSeek] Download failed: " + result.ErrorCode + " " + error);
            if (_queuedUrl != null) FlushQueue();
        }

        /* ───────────── parsing ───────────── */

        private void Apply(string json)
        {
            DataToken root;
            if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary)
            {
                SetStatus("<color=#ff7a86>AviSeek sent something unreadable.</color>");
                return;
            }
            DataDictionary data = root.DataDictionary;

            DataToken token;
            if (data.TryGetValue("stats", TokenType.DataDictionary, out token)) ApplyStats(token.DataDictionary);
            if (data.TryGetValue("supporters", TokenType.DataList, out token) && supporters != null) supporters.Apply(token.DataList);
            if (!data.TryGetValue("avatars", TokenType.DataList, out token))
            {
                SetStatus("<color=#ff7a86>AviSeek sent no avatars.</color>");
                return;
            }
            DataList list = token.DataList;

            _batchTotal = (int)ReadNumber(data, "total");
            _scope = _pendingScope;
            _browseBatch = _pendingBrowseBatch;
            _pendingBrowseBatch = -1;
            ReadBatch(list);
            _showFavorites = false;
            UseSource();
            Rebuild();
            if (_pageToEndOnLoad)
            {
                _page = Mathf.Max(0, PageCount() - 1);
                _pageToEndOnLoad = false;
                Render();
            }
            RefreshTabs();
            if (!_promotedRequested)
            {
                _promotedRequested = true;
                _pingWanted = shortPingUrl != null;
                _RequestPromoted();
            }
            SetStatus(_bId.Length == 0 ? (_scope.Length > 0 ? "No " + _scope + "." : "No avatars right now.") : "");
            if (_bId.Length > 0) ProbePictures(_bId[0]);
        }

        // a list from somewhere other than a search, like a favorites folder from the player's
        // aviseek account. it shows like a search result, with no download
        public void ShowSiteList(DataList list, string scope)
        {
            _scope = scope;
            _browseBatch = -1;
            _pendingBrowseBatch = -1;
            _pageToEndOnLoad = false;
            _batchTotal = list == null ? 0 : list.Count;
            ReadBatch(list == null ? new DataList() : list);
            _showFavorites = false;
            UseSource();
            Rebuild();
            RefreshTabs();
            SetStatus(_bId.Length == 0 ? "Nothing in " + scope + " yet." : "");
            if (_bId.Length > 0) ProbePictures(_bId[0]);
        }

        /* ───────────── pictures ───────────── */

        // the first avatar to arrive is drawn once on the detail picture's pedestal under the
        // floor, to learn how big vrchat draws it. the cards only switch theirs on after that
        private void ProbePictures(string id)
        {
            if (_picReady || _picProbing || detailPicture == null) return;
            _picProbing = true;
            detailPicture.Probe(id);
        }

        public void _PictureMeasured(float width, float height, Vector3 offset)
        {
            _picProbing = false;
            _picWidth = width;
            _picHeight = height;
            _picOffset = offset;
            _picReady = true;
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i] != null && cards[i].picture != null) cards[i].picture.Fit(width, height, offset);
                }
            }
            if (detailPicture != null)
            {
                detailPicture.Clear();
                detailPicture.Fit(width, height, offset);
            }
            Render();
            RefreshSelection();
        }

        // whatever an answer holds, the wall keeps at most 300 rows and only real avatar ids,
        // so an edited response can never hand the pedestals anything but an avatar
        private void ReadBatch(DataList list)
        {
            int count = Mathf.Min(list.Count, 300);
            _bId = new string[count];
            _bName = new string[count];
            _bAuthor = new string[count];
            _bPlat = new int[count];
            _bPerf = new int[count];
            int kept = 0;
            for (int i = 0; i < count; i++)
            {
                DataToken item = list[i];
                if (item.TokenType != TokenType.DataDictionary) continue;
                DataDictionary avatar = item.DataDictionary;
                string id = ReadString(avatar, "id");
                if (!id.StartsWith("avtr_") || id.Length > 64) continue;
                _bId[kept] = id;
                _bName[kept] = Clip(ReadString(avatar, "name"), 48);
                string author = ReadString(avatar, "author");
                _bAuthor[kept] = Clip(author.Length > 0 ? author : "Unknown creator", 32);
                _bPlat[kept] = ParsePlatforms(ReadString(avatar, "platforms"));
                DataToken perf;
                _bPerf[kept] = avatar.TryGetValue("performance", TokenType.DataDictionary, out perf) ? ParsePerformance(perf.DataDictionary) : 0;
                kept++;
            }
            if (kept < count) Shrink(kept);
        }

        private void Shrink(int count)
        {
            string[] id = new string[count];
            string[] name = new string[count];
            string[] author = new string[count];
            int[] plat = new int[count];
            int[] perf = new int[count];
            Array.Copy(_bId, id, count);
            Array.Copy(_bName, name, count);
            Array.Copy(_bAuthor, author, count);
            Array.Copy(_bPlat, plat, count);
            Array.Copy(_bPerf, perf, count);
            _bId = id;
            _bName = name;
            _bAuthor = author;
            _bPlat = plat;
            _bPerf = perf;
        }

        private void ApplyStats(DataDictionary stats)
        {
            if (statsLabel == null) return;
            statsLabel.text = ((int)ReadNumber(stats, "active")).ToString("N0") + " avatars  ·  "
                + ((int)ReadNumber(stats, "creators")).ToString("N0") + " creators";
        }

        /* ───────────── view ───────────── */

        private void UseSource()
        {
            if (_showFavorites)
            {
                _rId = _fId;
                _rName = _fName;
                _rAuthor = _fAuthor;
                _rPlat = _fPlat;
                _rPerf = _fPerf;
                _rCount = _fCount;
            }
            else
            {
                _rId = _bId;
                _rName = _bName;
                _rAuthor = _bAuthor;
                _rPlat = _bPlat;
                _rPerf = _bPerf;
                _rCount = _bId.Length;
            }
            RefreshTabs();
        }

        private void SetPlatform(int filter)
        {
            _platformFilter = filter;
            RefreshTabs();
            Rebuild();
        }

        private void Rebuild()
        {
            if (_view.Length < _rCount) _view = new int[_rCount];
            _viewCount = 0;
            int platformBit = _platformFilter == 0 ? 0 : 1 << (_platformFilter - 1);
            for (int i = 0; i < _rCount; i++)
            {
                if (platformBit != 0 && (_rPlat[i] & platformBit) == 0) continue;
                if (_creatorNeedle.Length > 0 && _rAuthor[i].ToLower().IndexOf(_creatorNeedle) < 0) continue;
                if (_rankFilter > 0)
                {
                    int rating = LocalRating(_rPerf[i]);
                    if (rating == 0 || rating > _rankFilter) continue;
                }
                _view[_viewCount++] = i;
            }
            if (_sortMode > 0) SortView();
            _page = 0;
            if (sortLabel != null) sortLabel.text = _sortMode == 0 ? "Popular" : (_sortMode == 1 ? "Name A-Z" : "Creator A-Z");
            if (rankLabel != null) rankLabel.text = RankFilterName(_rankFilter);
            Render();
        }

        // insertion sort, 120 rows at most
        private void SortView()
        {
            string[] keys = _sortMode == 1 ? _rName : _rAuthor;
            for (int i = 1; i < _viewCount; i++)
            {
                int row = _view[i];
                string key = keys[row];
                int j = i - 1;
                while (j >= 0 && string.Compare(keys[_view[j]], key, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    _view[j + 1] = _view[j];
                    j--;
                }
                _view[j + 1] = row;
            }
        }

        private int PageCount()
        {
            int size = cards == null || cards.Length == 0 ? 1 : cards.Length;
            return Mathf.Max(1, (_viewCount + size - 1) / size);
        }

        private void Render()
        {
            if (cards == null) return;
            int size = cards.Length;
            int pages = PageCount();
            if (_page >= pages) _page = pages - 1;
            int start = _page * size;
            for (int slot = 0; slot < size; slot++)
            {
                AviSeekCard card = cards[slot];
                if (card == null) continue;
                int index = start + slot;
                if (index >= _viewCount)
                {
                    card.Hide();
                    continue;
                }
                int row = _view[index];
                int perf = _rPerf[row];
                int plat = _rPlat[row];
                string id = _rId[row];
                card.Show(_rName[row], _rAuthor[row], plat,
                    PlatformColor(plat, 1, perf & 7), PlatformColor(plat, 2, (perf >> 3) & 7), PlatformColor(plat, 4, (perf >> 6) & 7),
                    IsFavorite(id), id == _selId, heartOn, heartOff, accent, frameOff);
                if (_picReady && card.picture != null) card.picture.Show(id);
            }

            if (pageLabel != null)
            {
                // browsing counts on through every set of 120 as if it were one long list
                int page = _page + 1;
                int total = pages;
                if (!_showFavorites && _browseBatch >= 0 && _batchTotal > 120)
                {
                    int batches = Mathf.Min(browseUrls == null ? 1 : browseUrls.Length, (_batchTotal + 119) / 120);
                    page += _browseBatch * pages;
                    total = batches * pages;
                }
                pageLabel.text = "Page " + page + " / " + total;
            }
            if (titleCountLabel != null)
            {
                string scope = _showFavorites ? "favorites" : (_scope.Length > 0 ? _scope : "popular avatars");
                if (_creatorNeedle.Length > 0) scope += ", creator has \"" + Escape(_creatorNeedle) + "\"";
                string text = "Showing " + _viewCount + " / " + _rCount + " " + scope;
                if (!_showFavorites && _batchTotal > _rCount) text += "  ·  " + _batchTotal.ToString("N0") + " matches";
                titleCountLabel.text = text;
            }
        }

        private void RefreshTabs()
        {
            if (platformTabs != null)
            {
                for (int i = 0; i < platformTabs.Length; i++)
                {
                    if (platformTabs[i] != null) platformTabs[i].color = i == _platformFilter ? accent : tabIdle;
                }
            }
            if (favoritesTab != null) favoritesTab.color = _showFavorites ? accent : tabIdle;
            if (favoritesLabel != null) favoritesLabel.text = "Favorites  <color=#ffffff99>" + _fCount + "</color>";
            if (searchModeTabs != null)
            {
                for (int i = 0; i < searchModeTabs.Length; i++)
                {
                    if (searchModeTabs[i] != null) searchModeTabs[i].color = (i == 1) == _creatorMode ? accent : tabIdle;
                }
            }
            if (favoritesIcon != null) favoritesIcon.color = _showFavorites ? Color.white : heartOff;
        }

        /* ───────────── selection ───────────── */

        private void SelectRow(int row)
        {
            _selId = _rId[row];
            _selName = _rName[row];
            _selAuthor = _rAuthor[row];
            _selPlat = _rPlat[row];
            _selPerf = _rPerf[row];
            RefreshSelection();
            // the pedestal paints vrchat's own full size picture of the avatar
            VRCAvatarPedestal pedestal = Pedestal();
            if (pedestal != null) pedestal.SwitchAvatar(_selId);
        }

        public void _SelectExternal(string id, string name, string author, int plat, int perf)
        {
            _selId = id;
            _selName = name;
            _selAuthor = author;
            _selPlat = plat;
            _selPerf = perf;
            RefreshSelection();
            Render();
            VRCAvatarPedestal pedestal = Pedestal();
            if (pedestal != null) pedestal.SwitchAvatar(_selId);
        }

        private void RefreshSelection()
        {
            bool has = _selId.Length > 0;
            if (!has && _picReady && detailPicture != null) detailPicture.Clear();
            if (detailsRoot != null) detailsRoot.SetActive(has);
            if (detailsEmpty != null) detailsEmpty.SetActive(!has);
            if (!has) return;
            if (detailName != null) detailName.text = _selName;
            if (detailAuthor != null) detailAuthor.text = "by " + _selAuthor;
            if (detailId != null) detailId.text = _selId;
            if (detailPcBadge != null) detailPcBadge.SetActive((_selPlat & 1) != 0);
            if (detailQuestBadge != null) detailQuestBadge.SetActive((_selPlat & 2) != 0);
            if (detailIosBadge != null) detailIosBadge.SetActive((_selPlat & 4) != 0);
            if (detailRatings != null)
            {
                detailRatings.text = RatingLine("PC", _selPerf & 7) + "\n"
                    + RatingLine("Quest", (_selPerf >> 3) & 7) + "\n"
                    + RatingLine("iOS", (_selPerf >> 6) & 7);
            }
            if (detailHeart != null) detailHeart.color = IsFavorite(_selId) ? heartOn : heartOff;
            if (_picReady && detailPicture != null) detailPicture.Show(_selId);
        }

        private string RatingLine(string platform, int rating)
        {
            Color color = RatingColor(rating);
            string hex = ((int)(color.r * 255f)).ToString("X2") + ((int)(color.g * 255f)).ToString("X2") + ((int)(color.b * 255f)).ToString("X2");
            return "<color=#ffffff99>" + platform + "</color>  <color=#" + hex + ">" + RatingName(rating) + "</color>";
        }

        /* ───────────── avatars ───────────── */

        private VRCAvatarPedestal Pedestal()
        {
            if (pedestalObject == null) return null;
            return pedestalObject.GetComponent<VRCAvatarPedestal>();
        }

        private void UseAvatar(string id)
        {
            VRCAvatarPedestal pedestal = Pedestal();
            if (pedestal == null)
            {
                SetStatus("<color=#ff7a86>This wall has no avatar pedestal set up.</color>");
                return;
            }
            pedestal.SwitchAvatar(id);
            pedestal.SetAvatarUse(Networking.LocalPlayer);
        }

        /* ───────────── favorites ───────────── */

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            string saved;
            if (PlayerData.TryGetString(player, FavoritesKey, out saved)) LoadFavorites(saved);
            _favoritesLoaded = true;
            if (_showFavorites) UseSource();
            RefreshTabs();
            Rebuild();
            RefreshSelection();
        }

        private bool IsFavorite(string id)
        {
            return FavoriteIndex(id) >= 0;
        }

        private int FavoriteIndex(string id)
        {
            for (int i = 0; i < _fCount; i++)
            {
                if (_fId[i] == id) return i;
            }
            return -1;
        }

        private void ToggleFavorite(string id, string name, string author, int plat, int perf)
        {
            int index = FavoriteIndex(id);
            if (index >= 0)
            {
                for (int i = index; i < _fCount - 1; i++)
                {
                    _fId[i] = _fId[i + 1];
                    _fName[i] = _fName[i + 1];
                    _fAuthor[i] = _fAuthor[i + 1];
                    _fPlat[i] = _fPlat[i + 1];
                    _fPerf[i] = _fPerf[i + 1];
                }
                _fCount--;
            }
            else
            {
                if (_fCount >= MaxFavorites)
                {
                    SetStatus("Favorites are full (" + MaxFavorites + "). Remove one first.");
                    return;
                }
                GrowFavorites(_fCount + 1);
                // newest first
                for (int i = _fCount; i > 0; i--)
                {
                    _fId[i] = _fId[i - 1];
                    _fName[i] = _fName[i - 1];
                    _fAuthor[i] = _fAuthor[i - 1];
                    _fPlat[i] = _fPlat[i - 1];
                    _fPerf[i] = _fPerf[i - 1];
                }
                _fId[0] = id;
                _fName[0] = name;
                _fAuthor[0] = author;
                _fPlat[0] = plat;
                _fPerf[0] = perf;
                _fCount++;
            }
            SaveFavorites();
            RefreshTabs();
            if (_showFavorites)
            {
                UseSource();
                int page = _page;
                Rebuild();
                _page = Mathf.Min(page, PageCount() - 1);
            }
            Render();
            RefreshSelection();
        }

        private void GrowFavorites(int size)
        {
            if (_fId.Length >= size) return;
            int capacity = Mathf.Max(size, _fId.Length * 2);
            string[] id = new string[capacity];
            string[] name = new string[capacity];
            string[] author = new string[capacity];
            int[] plat = new int[capacity];
            int[] perf = new int[capacity];
            Array.Copy(_fId, id, _fCount);
            Array.Copy(_fName, name, _fCount);
            Array.Copy(_fAuthor, author, _fCount);
            Array.Copy(_fPlat, plat, _fCount);
            Array.Copy(_fPerf, perf, _fCount);
            _fId = id;
            _fName = name;
            _fAuthor = author;
            _fPlat = plat;
            _fPerf = perf;
        }

        // one line per favorite: id, name, author, platforms, ratings, tab separated
        private void SaveFavorites()
        {
            if (!_favoritesLoaded) return;
            string text = "";
            for (int i = 0; i < _fCount; i++)
            {
                text += _fId[i] + "\t" + _fName[i].Replace("\t", " ").Replace("\n", " ") + "\t"
                    + _fAuthor[i].Replace("\t", " ").Replace("\n", " ") + "\t" + _fPlat[i] + "\t" + _fPerf[i] + "\n";
            }
            PlayerData.SetString(FavoritesKey, text);
        }

        private void LoadFavorites(string text)
        {
            _fCount = 0;
            if (text == null || text.Length == 0) return;
            string[] lines = text.Split('\n');
            GrowFavorites(Mathf.Min(lines.Length, MaxFavorites));
            for (int i = 0; i < lines.Length && _fCount < MaxFavorites; i++)
            {
                string[] parts = lines[i].Split('\t');
                if (parts.Length < 5 || !parts[0].StartsWith("avtr_")) continue;
                int plat;
                int perf;
                if (!int.TryParse(parts[3], out plat)) plat = 0;
                if (!int.TryParse(parts[4], out perf)) perf = 0;
                _fId[_fCount] = parts[0];
                _fName[_fCount] = parts[1];
                _fAuthor[_fCount] = parts[2];
                _fPlat[_fCount] = plat;
                _fPerf[_fCount] = perf;
                _fCount++;
            }
        }

        /* ───────────── helpers ───────────── */

        private int ParsePlatforms(string text)
        {
            int bits = 0;
            if (text.IndexOf("pc") >= 0) bits |= 1;
            if (text.IndexOf("quest") >= 0) bits |= 2;
            if (text.IndexOf("ios") >= 0) bits |= 4;
            return bits;
        }

        // three bits a platform: pc, quest, ios. 0 is unknown, 1 excellent to 5 very poor
        private int ParsePerformance(DataDictionary perf)
        {
            return RatingValue(ReadString(perf, "pc"))
                | (RatingValue(ReadString(perf, "quest")) << 3)
                | (RatingValue(ReadString(perf, "ios")) << 6);
        }

        private int RatingValue(string text)
        {
            if (text == "Excellent") return 1;
            if (text == "Good") return 2;
            if (text == "Medium") return 3;
            if (text == "Poor") return 4;
            if (text == "VeryPoor" || text == "Very Poor") return 5;
            return 0;
        }

        private string RatingName(int rating)
        {
            if (rating == 1) return "Excellent";
            if (rating == 2) return "Good";
            if (rating == 3) return "Medium";
            if (rating == 4) return "Poor";
            if (rating == 5) return "Very Poor";
            return "Unknown";
        }

        private string RankFilterName(int filter)
        {
            if (filter == 1) return "Excellent";
            if (filter == 2) return "Good or better";
            if (filter == 3) return "Medium or better";
            if (filter == 4) return "Poor or better";
            return "Any Rank";
        }

        // a platform's logo takes its rank colour, or fades out when there is no version for it
        private Color PlatformColor(int platforms, int bit, int rating)
        {
            return (platforms & bit) == 0 ? platformOff : RatingColor(rating);
        }

        private Color RatingColor(int rating)
        {
            if (rating == 1) return rateExcellent;
            if (rating == 2) return rateGood;
            if (rating == 3) return rateMedium;
            if (rating == 4) return ratePoor;
            if (rating == 5) return rateVeryPoor;
            return rateUnknown;
        }

        // rank filters go by the rating for the platform this player is on
        private int LocalRating(int perf)
        {
#if UNITY_ANDROID
            return (perf >> 3) & 7;
#elif UNITY_IOS
            return (perf >> 6) & 7;
#else
            return perf & 7;
#endif
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

        // the value after key= in the address a player typed, with + and %20 back to spaces
        private string UrlValue(string url, string key)
        {
            int start = url.IndexOf("?" + key);
            if (start < 0) start = url.IndexOf("&" + key);
            if (start < 0) return "";
            start += key.Length + 1;
            int end = url.IndexOf('&', start);
            string value = end < 0 ? url.Substring(start) : url.Substring(start, end - start);
            return value.Replace("+", " ").Replace("%20", " ").Trim();
        }

        private string Clip(string text, int length)
        {
            if (text == null) return "";
            text = text.Trim();
            return text.Length > length ? text.Substring(0, length - 1) + "…" : text;
        }

        // avatar names are player written, keep their tags from styling the wall
        private string Escape(string text)
        {
            return text.Replace("<", "<\u200B");
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }
    }
}
