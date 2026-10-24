using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Economy;
using VRC.SDK3.Data;

namespace VRChatLegends.AviSeek
{
    // the promoted panel: paid avatar and group spots from aviseek. the search wall
    // downloads the list (it owns the download queue) and hands the text over here.
    // there can be more spots than lines, so each list pages, and turns over by itself.
    // every group's icon comes inside that same list, so icons cost no download of their own
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Promoted")]
    [HelpURL("https://aviseek.vrchatlegends.com/promote")]
    public class AviSeekPromoted : UdonSharpBehaviour
    {
        private const int MaxItems = 60;
        private const int IconSize = 64;

        public AviSeekSearch search;

        [Tooltip("Lists with more than one page turn over by themselves this often. 0 keeps them still.")]
        public float autoPageSeconds = 12f;

        [Header("Avatar lines")]
        public GameObject[] avatarRows;
        public TextMeshProUGUI[] avatarNames;
        public TextMeshProUGUI[] avatarAuthors;
        public GameObject avatarsEmpty;
        public GameObject avatarPager;
        public TextMeshProUGUI avatarPageLabel;

        [Header("Group lines")]
        public GameObject[] groupRows;
        public TextMeshProUGUI[] groupNames;
        public TextMeshProUGUI[] groupDetails;
        [Tooltip("The group icon on each line, over the placeholder glyph.")]
        public RawImage[] groupPictures;
        public GameObject groupsEmpty;
        public GameObject groupPager;
        public TextMeshProUGUI groupPageLabel;

        [HideInInspector] public int rowIndex;
        [Tooltip("How many group icons AviSeek is still fetching. The wall asks for the list again soon while any are.")]
        [HideInInspector] public int iconsPending;

        private string[] _avatarId = new string[0];
        private string[] _avatarName = new string[0];
        private string[] _avatarAuthor = new string[0];
        private int[] _avatarPlat = new int[0];
        private int[] _avatarPerf = new int[0];
        private int _avatarCount;
        private int _avatarPage;

        private string[] _groupId = new string[0];
        private string[] _groupName = new string[0];
        private string[] _groupDetail = new string[0];
        private Texture2D[] _groupIcon = new Texture2D[0];
        // group id -> Texture2D, so a list that comes again keeps the icons it already has
        private DataDictionary _iconCache = new DataDictionary();
        private int _groupCount;
        private int _groupPage;

        // bumped by every page turn, so an older timer knows it was overtaken
        private int _turn;

        private void Start()
        {
            ShowAvatars();
            ShowGroups();
        }

        public void Apply(string json)
        {
            DataToken root;
            if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary) return;
            iconsPending = (int)ReadNumber(root.DataDictionary, "iconsPending");
            DataToken sections;
            if (!root.DataDictionary.TryGetValue("sections", TokenType.DataDictionary, out sections)) return;
            DataToken list;
            if (sections.DataDictionary.TryGetValue("promoted", TokenType.DataList, out list)) ReadAvatars(list.DataList);
            if (sections.DataDictionary.TryGetValue("promotedGroups", TokenType.DataList, out list)) ReadGroups(list.DataList);
            ScheduleTurn();
        }

        private void ReadAvatars(DataList list)
        {
            int size = Mathf.Min(list.Count, MaxItems);
            _avatarId = new string[size];
            _avatarName = new string[size];
            _avatarAuthor = new string[size];
            _avatarPlat = new int[size];
            _avatarPerf = new int[size];
            int count = 0;
            for (int i = 0; i < list.Count && count < size; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary avatar = list[i].DataDictionary;
                string id = ReadString(avatar, "id");
                if (!id.StartsWith("avtr_")) continue;
                _avatarId[count] = id;
                _avatarName[count] = ReadString(avatar, "name");
                string author = ReadString(avatar, "authorName");
                _avatarAuthor[count] = author.Length > 0 ? author : "Unknown creator";
                DataToken token;
                _avatarPlat[count] = avatar.TryGetValue("platforms", TokenType.DataList, out token) ? ParsePlatforms(token.DataList) : 0;
                _avatarPerf[count] = avatar.TryGetValue("performance", TokenType.DataDictionary, out token) ? ParsePerformance(token.DataDictionary) : 0;
                count++;
            }
            _avatarCount = count;
            if (_avatarPage >= Pages(_avatarCount, avatarRows)) _avatarPage = 0;
            ShowAvatars();
        }

        private void ReadGroups(DataList list)
        {
            int size = Mathf.Min(list.Count, MaxItems);
            _groupId = new string[size];
            _groupName = new string[size];
            _groupDetail = new string[size];
            _groupIcon = new Texture2D[size];
            int count = 0;
            for (int i = 0; i < list.Count && count < size; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary group = list[i].DataDictionary;
                string id = ReadString(group, "id");
                if (!id.StartsWith("grp_")) continue;
                _groupId[count] = id;
                _groupName[count] = ReadString(group, "name");
                string code = ReadString(group, "shortCode");
                string members = ((int)ReadNumber(group, "memberCount")).ToString("N0") + " members";
                int online = (int)ReadNumber(group, "onlineMemberCount");
                if (online > 0) members += "  ·  " + online.ToString("N0") + " online";
                _groupDetail[count] = (code.Length > 0 ? code + "  ·  " : "") + members;
                _groupIcon[count] = Icon(id, ReadString(group, "icon"));
                count++;
            }
            _groupCount = count;
            if (_groupPage >= Pages(_groupCount, groupRows)) _groupPage = 0;
            ShowGroups();
        }

        private void ShowAvatars()
        {
            int slots = avatarRows == null ? 0 : avatarRows.Length;
            int start = _avatarPage * slots;
            for (int i = 0; i < slots; i++)
            {
                int item = start + i;
                bool used = item < _avatarCount;
                if (avatarRows[i] != null) avatarRows[i].SetActive(used);
                if (!used) continue;
                if (avatarNames != null && i < avatarNames.Length && avatarNames[i] != null) avatarNames[i].text = _avatarName[item];
                if (avatarAuthors != null && i < avatarAuthors.Length && avatarAuthors[i] != null) avatarAuthors[i].text = "by " + _avatarAuthor[item];
            }
            if (avatarsEmpty != null) avatarsEmpty.SetActive(_avatarCount == 0);
            ShowPager(avatarPager, avatarPageLabel, _avatarPage, Pages(_avatarCount, avatarRows));
        }

        private void ShowGroups()
        {
            int slots = groupRows == null ? 0 : groupRows.Length;
            int start = _groupPage * slots;
            for (int i = 0; i < slots; i++)
            {
                int item = start + i;
                bool used = item < _groupCount;
                if (groupRows[i] != null) groupRows[i].SetActive(used);
                if (!used) continue;
                if (groupNames != null && i < groupNames.Length && groupNames[i] != null) groupNames[i].text = _groupName[item];
                if (groupDetails != null && i < groupDetails.Length && groupDetails[i] != null) groupDetails[i].text = _groupDetail[item];
                if (groupPictures != null && i < groupPictures.Length && groupPictures[i] != null)
                {
                    groupPictures[i].texture = _groupIcon[item];
                    groupPictures[i].enabled = _groupIcon[item] != null;
                }
            }
            if (groupsEmpty != null) groupsEmpty.SetActive(_groupCount == 0);
            ShowPager(groupPager, groupPageLabel, _groupPage, Pages(_groupCount, groupRows));
        }

        private void ShowPager(GameObject pager, TextMeshProUGUI label, int page, int pages)
        {
            if (pager != null) pager.SetActive(pages > 1);
            if (label != null) label.text = (page + 1) + " / " + pages;
        }

        private int Pages(int count, GameObject[] rows)
        {
            int slots = rows == null || rows.Length == 0 ? 1 : rows.Length;
            return Mathf.Max(1, (count + slots - 1) / slots);
        }

        /* ───────────── paging ───────────── */

        public void _NextAvatars() { TurnAvatars(1); ScheduleTurn(); }
        public void _PrevAvatars() { TurnAvatars(-1); ScheduleTurn(); }
        public void _NextGroups() { TurnGroups(1); ScheduleTurn(); }
        public void _PrevGroups() { TurnGroups(-1); ScheduleTurn(); }

        private void TurnAvatars(int step)
        {
            int pages = Pages(_avatarCount, avatarRows);
            _avatarPage = (_avatarPage + step + pages) % pages;
            ShowAvatars();
        }

        private void TurnGroups(int step)
        {
            int pages = Pages(_groupCount, groupRows);
            _groupPage = (_groupPage + step + pages) % pages;
            ShowGroups();
        }

        // pressing an arrow starts the wait again, so a list never flips under a player's hand
        private void ScheduleTurn()
        {
            if (autoPageSeconds <= 0f) return;
            if (Pages(_avatarCount, avatarRows) < 2 && Pages(_groupCount, groupRows) < 2) return;
            _turn++;
            SendCustomEventDelayedSeconds(nameof(_AutoTurn), autoPageSeconds);
        }

        public void _AutoTurn()
        {
            _turn--;
            if (_turn > 0) return;
            _turn = 0;
            if (Pages(_avatarCount, avatarRows) > 1) TurnAvatars(1);
            if (Pages(_groupCount, groupRows) > 1) TurnGroups(1);
            ScheduleTurn();
        }

        /* ───────────── row buttons ───────────── */

        private int AvatarAt(int line)
        {
            int slots = avatarRows == null ? 0 : avatarRows.Length;
            int item = _avatarPage * slots + line;
            return line >= 0 && item < _avatarCount ? item : -1;
        }

        private int GroupAt(int line)
        {
            int slots = groupRows == null ? 0 : groupRows.Length;
            int item = _groupPage * slots + line;
            return line >= 0 && item < _groupCount ? item : -1;
        }

        public void _SelectAvatar()
        {
            int item = AvatarAt(rowIndex);
            if (search == null || item < 0) return;
            search._SelectExternal(_avatarId[item], _avatarName[item], _avatarAuthor[item], _avatarPlat[item], _avatarPerf[item]);
        }

        public void _UseAvatar()
        {
            int item = AvatarAt(rowIndex);
            if (search == null || item < 0) return;
            search._SelectExternal(_avatarId[item], _avatarName[item], _avatarAuthor[item], _avatarPlat[item], _avatarPerf[item]);
            search._UseSelected();
        }

        // vrchat opens the group's own page, where players can join
        public void _OpenGroup()
        {
            int item = GroupAt(rowIndex);
            if (item < 0) return;
            Store.OpenGroupPage(_groupId[item]);
        }

        /* ───────────── icons ───────────── */

        private Texture2D Icon(string id, string base64)
        {
            DataToken cached;
            if (_iconCache.TryGetValue(id, out cached)) return (Texture2D)cached.Reference;
            Texture2D texture = Decode(base64);
            if (texture != null) _iconCache.SetValue(id, new DataToken(texture));
            return texture;
        }

        // the api sends 64x64 rgb24, already flipped for unity's bottom left origin
        private Texture2D Decode(string base64)
        {
            if (base64 == null || base64.Length == 0) return null;
            byte[] raw = Convert.FromBase64String(base64);
            if (raw == null || raw.Length != IconSize * IconSize * 3) return null;
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGB24, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.LoadRawTextureData(raw);
            texture.Apply(false, true);
            return texture;
        }

        /* ───────────── helpers ───────────── */

        private int ParsePlatforms(DataList list)
        {
            int bits = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].TokenType != TokenType.String) continue;
                string platform = list[i].String;
                if (platform == "pc") bits |= 1;
                else if (platform == "quest") bits |= 2;
                else if (platform == "ios") bits |= 4;
            }
            return bits;
        }

        // same packing as the search wall: three bits a platform, pc, quest, ios
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
    }
}
