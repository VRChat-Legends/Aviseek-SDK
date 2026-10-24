using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.Economy;
using VRC.SDK3.Data;

namespace VRChatLegends.AviSeek
{
    // the promoted panel: paid avatar and group spots from aviseek. the search wall
    // downloads the list (it owns the download queue) and hands the text over here
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Promoted")]
    [HelpURL("https://aviseek.vrchatlegends.com/promote")]
    public class AviSeekPromoted : UdonSharpBehaviour
    {
        public AviSeekSearch search;

        [Header("Avatar lines")]
        public GameObject[] avatarRows;
        public TextMeshProUGUI[] avatarNames;
        public TextMeshProUGUI[] avatarAuthors;
        public GameObject avatarsEmpty;

        [Header("Group lines")]
        public GameObject[] groupRows;
        public TextMeshProUGUI[] groupNames;
        public TextMeshProUGUI[] groupDetails;
        public GameObject groupsEmpty;

        [HideInInspector] public int rowIndex;

        private string[] _avatarId = new string[0];
        private string[] _avatarName = new string[0];
        private string[] _avatarAuthor = new string[0];
        private int[] _avatarPlat = new int[0];
        private int[] _avatarPerf = new int[0];
        private string[] _groupId = new string[0];

        private void Start()
        {
            ShowAvatars(0);
            ShowGroups(0);
        }

        public void Apply(string json)
        {
            DataToken root;
            if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary) return;
            DataToken sections;
            if (!root.DataDictionary.TryGetValue("sections", TokenType.DataDictionary, out sections)) return;
            DataToken list;
            if (sections.DataDictionary.TryGetValue("promoted", TokenType.DataList, out list)) ReadAvatars(list.DataList);
            if (sections.DataDictionary.TryGetValue("promotedGroups", TokenType.DataList, out list)) ReadGroups(list.DataList);
        }

        private void ReadAvatars(DataList list)
        {
            int slots = avatarRows == null ? 0 : avatarRows.Length;
            _avatarId = new string[slots];
            _avatarName = new string[slots];
            _avatarAuthor = new string[slots];
            _avatarPlat = new int[slots];
            _avatarPerf = new int[slots];
            int count = 0;
            for (int i = 0; i < list.Count && count < slots; i++)
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
                if (avatarNames != null && count < avatarNames.Length && avatarNames[count] != null) avatarNames[count].text = _avatarName[count];
                if (avatarAuthors != null && count < avatarAuthors.Length && avatarAuthors[count] != null) avatarAuthors[count].text = "by " + _avatarAuthor[count];
                count++;
            }
            ShowAvatars(count);
        }

        private void ReadGroups(DataList list)
        {
            int slots = groupRows == null ? 0 : groupRows.Length;
            _groupId = new string[slots];
            int count = 0;
            for (int i = 0; i < list.Count && count < slots; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary group = list[i].DataDictionary;
                string id = ReadString(group, "id");
                if (!id.StartsWith("grp_")) continue;
                _groupId[count] = id;
                if (groupNames != null && count < groupNames.Length && groupNames[count] != null) groupNames[count].text = ReadString(group, "name");
                if (groupDetails != null && count < groupDetails.Length && groupDetails[count] != null)
                {
                    string code = ReadString(group, "shortCode");
                    string members = ((int)ReadNumber(group, "memberCount")).ToString("N0") + " members";
                    int online = (int)ReadNumber(group, "onlineMemberCount");
                    if (online > 0) members += "  ·  " + online.ToString("N0") + " online";
                    groupDetails[count].text = (code.Length > 0 ? code + "  ·  " : "") + members;
                }
                count++;
            }
            ShowGroups(count);
        }

        private void ShowAvatars(int count)
        {
            if (avatarRows != null)
            {
                for (int i = 0; i < avatarRows.Length; i++) if (avatarRows[i] != null) avatarRows[i].SetActive(i < count);
            }
            if (avatarsEmpty != null) avatarsEmpty.SetActive(count == 0);
        }

        private void ShowGroups(int count)
        {
            if (groupRows != null)
            {
                for (int i = 0; i < groupRows.Length; i++) if (groupRows[i] != null) groupRows[i].SetActive(i < count);
            }
            if (groupsEmpty != null) groupsEmpty.SetActive(count == 0);
        }

        /* ───────────── row buttons ───────────── */

        public void _SelectAvatar()
        {
            if (search == null || rowIndex < 0 || rowIndex >= _avatarId.Length || string.IsNullOrEmpty(_avatarId[rowIndex])) return;
            search._SelectExternal(_avatarId[rowIndex], _avatarName[rowIndex], _avatarAuthor[rowIndex], _avatarPlat[rowIndex], _avatarPerf[rowIndex]);
        }

        public void _UseAvatar()
        {
            if (search == null || rowIndex < 0 || rowIndex >= _avatarId.Length || string.IsNullOrEmpty(_avatarId[rowIndex])) return;
            search._SelectExternal(_avatarId[rowIndex], _avatarName[rowIndex], _avatarAuthor[rowIndex], _avatarPlat[rowIndex], _avatarPerf[rowIndex]);
            search._UseSelected();
        }

        // vrchat opens the group's own page, where players can join
        public void _OpenGroup()
        {
            if (rowIndex < 0 || rowIndex >= _groupId.Length || string.IsNullOrEmpty(_groupId[rowIndex])) return;
            Store.OpenGroupPage(_groupId[rowIndex]);
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
