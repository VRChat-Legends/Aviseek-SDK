using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace VRChatLegends.AviSeek
{
    // the supporters panel: the people keeping aviseek running, by tier. the list arrives
    // with every search, so this panel never downloads anything of its own
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Supporters")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekSupporters : UdonSharpBehaviour
    {
        private const int MaxItems = 100;

        [Tooltip("Pages turn over by themselves this often when there are more supporters than lines. 0 keeps them still.")]
        public float autoPageSeconds = 10f;

        [Tooltip("Where to send people who want to support. Players copy it from the box at the bottom.")]
        public string supportLink = "https://www.patreon.com/vrchatlegends";

        [Header("Lines on the panel")]
        public GameObject[] rows;
        public TextMeshProUGUI[] rowNames;
        public TextMeshProUGUI[] rowTiers;
        public Image[] rowTierBacks;
        public TextMeshProUGUI[] rowRanks;
        public GameObject emptyLabel;
        public GameObject pager;
        public TextMeshProUGUI pageLabel;
        public TextMeshProUGUI countLabel;
        public TMP_InputField linkField;

        [Header("Tier colours")]
        public Color platinum = new Color(0.62f, 0.9f, 1f, 1f);
        public Color gold = new Color(1f, 0.8f, 0.3f, 1f);
        public Color silver = new Color(0.82f, 0.84f, 0.9f, 1f);
        public Color bronze = new Color(0.86f, 0.55f, 0.33f, 1f);
        public Color other = new Color(0.65f, 0.55f, 0.98f, 1f);

        private string[] _name = new string[0];
        private string[] _tier = new string[0];
        private int _count;
        private int _page;
        private int _turn;

        private void Start()
        {
            Show();
            if (linkField != null) linkField.text = supportLink;
        }

        public void Apply(DataList list)
        {
            int size = Mathf.Min(list.Count, MaxItems);
            _name = new string[size];
            _tier = new string[size];
            int count = 0;
            for (int i = 0; i < list.Count && count < size; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary entry = list[i].DataDictionary;
                string name = ReadString(entry, "name").Trim();
                if (name.Length == 0) continue;
                _name[count] = name.Length > 28 ? name.Substring(0, 27) + "…" : name;
                _tier[count] = ReadString(entry, "tier");
                count++;
            }
            bool grew = count != _count;
            _count = count;
            if (_page >= Pages()) _page = 0;
            Show();
            if (grew) ScheduleTurn();
        }

        private void Show()
        {
            int slots = rows == null ? 0 : rows.Length;
            int start = _page * slots;
            for (int i = 0; i < slots; i++)
            {
                int item = start + i;
                bool used = item < _count;
                if (rows[i] != null) rows[i].SetActive(used);
                if (!used) continue;
                Color color = TierColor(_tier[item]);
                // names are player written, keep their tags from styling the panel
                if (rowNames != null && i < rowNames.Length && rowNames[i] != null) rowNames[i].text = _name[item].Replace("<", "<​");
                if (rowTiers != null && i < rowTiers.Length && rowTiers[i] != null)
                {
                    rowTiers[i].text = _tier[item].Length > 0 ? _tier[item] : "Supporter";
                    rowTiers[i].color = color;
                }
                if (rowTierBacks != null && i < rowTierBacks.Length && rowTierBacks[i] != null) rowTierBacks[i].color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 1f);
                if (rowRanks != null && i < rowRanks.Length && rowRanks[i] != null)
                {
                    rowRanks[i].text = (item + 1).ToString();
                    rowRanks[i].color = color;
                }
            }
            if (emptyLabel != null) emptyLabel.SetActive(_count == 0);
            if (pager != null) pager.SetActive(Pages() > 1);
            if (pageLabel != null) pageLabel.text = (_page + 1) + " / " + Pages();
            if (countLabel != null) countLabel.text = _count == 1 ? "1 supporter" : _count + " supporters";
        }

        private int Pages()
        {
            int slots = rows == null || rows.Length == 0 ? 1 : rows.Length;
            return Mathf.Max(1, (_count + slots - 1) / slots);
        }

        private Color TierColor(string tier)
        {
            string t = tier.ToLower();
            if (t.IndexOf("platinum") >= 0 || t.IndexOf("diamond") >= 0) return platinum;
            if (t.IndexOf("gold") >= 0) return gold;
            if (t.IndexOf("silver") >= 0) return silver;
            if (t.IndexOf("bronze") >= 0) return bronze;
            return other;
        }

        /* ───────────── buttons ───────────── */

        public void _Next() { Turn(1); ScheduleTurn(); }
        public void _Prev() { Turn(-1); ScheduleTurn(); }

        // the link box is editable only so the vrchat keyboard opens for copying
        public void _RestoreLink()
        {
            if (linkField != null && linkField.text != supportLink) linkField.text = supportLink;
        }

        private void Turn(int step)
        {
            int pages = Pages();
            _page = (_page + step + pages) % pages;
            Show();
        }

        private void ScheduleTurn()
        {
            if (autoPageSeconds <= 0f || Pages() < 2) return;
            _turn++;
            SendCustomEventDelayedSeconds(nameof(_AutoTurn), autoPageSeconds);
        }

        public void _AutoTurn()
        {
            _turn--;
            if (_turn > 0) return;
            _turn = 0;
            if (Pages() > 1) Turn(1);
            ScheduleTurn();
        }

        private string ReadString(DataDictionary data, string key)
        {
            DataToken token;
            if (data.TryGetValue(key, TokenType.String, out token)) return token.String;
            return "";
        }
    }
}
