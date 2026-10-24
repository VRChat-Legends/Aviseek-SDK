using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;

namespace VRChatLegends.AviSeek
{
    // the supporters panel: the people keeping aviseek running, by tier. the list arrives
    // with every search, so this panel never downloads anything of its own. every tier gets
    // its own pages in that tier's colour, pages fade over, and the names flip in one by one
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Supporters")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekSupporters : UdonSharpBehaviour
    {
        private const int MaxItems = 100;
        private const int Tiers = 6;
        private const float FadeOut = 0.22f;
        private const float FadeIn = 0.3f;
        private const float FlipTime = 0.42f;
        private const float FlipStagger = 0.055f;

        [Tooltip("Pages turn over by themselves this often when there is more than one. 0 keeps them still.")]
        public float autoPageSeconds = 10f;

        [Tooltip("Where to send people who want to support. Players copy it from the box at the bottom.")]
        public string supportLink = "https://www.patreon.com/vrchatlegends";

        [Header("Lines on the panel")]
        [Tooltip("Holds every line, it fades when the page turns.")]
        public CanvasGroup rowsGroup;
        public GameObject[] rows;
        public Image[] rowBacks;
        public Image[] rowAccents;
        public TextMeshProUGUI[] rowNames;
        public TextMeshProUGUI[] rowTiers;
        public Image[] rowTierBacks;
        public TextMeshProUGUI[] rowRanks;
        public GameObject emptyLabel;
        public GameObject pager;
        public TextMeshProUGUI pageLabel;
        [Tooltip("The tier the page shows, in that tier's colour.")]
        public TextMeshProUGUI countLabel;
        public TMP_InputField linkField;

        [Header("Tier colours")]
        public Color platinumPlus = new Color(0.93f, 0.6f, 1f, 1f);
        public Color platinum = new Color(0.55f, 0.92f, 1f, 1f);
        public Color gold = new Color(1f, 0.79f, 0.29f, 1f);
        public Color silver = new Color(0.78f, 0.81f, 0.89f, 1f);
        public Color bronze = new Color(0.86f, 0.53f, 0.3f, 1f);
        public Color other = new Color(0.65f, 0.55f, 0.98f, 1f);
        [Tooltip("The plain line colour the tier colour is mixed into.")]
        public Color rowBase = new Color(0.114f, 0.09f, 0.192f, 1f);

        // supporters sorted best tier first
        private string[] _name = new string[0];
        private string[] _tier = new string[0];
        private int[] _rank = new int[0];
        private int _count;

        // pages: each holds part of one tier
        private int[] _pageStart = new int[0];
        private int[] _pageLength = new int[0];
        private int _pages;
        private int _page;
        private int _turn;

        // page turn animation
        private bool _animating;
        private int _phase; // 1 fading out, 2 fading and flipping in
        private float _phaseStart;
        private int _target;

        private void Start()
        {
            BuildPages();
            ShowPage(0);
            StartIn();
            if (linkField != null) linkField.text = supportLink;
        }

        public void Apply(DataList list)
        {
            int size = Mathf.Min(list.Count, MaxItems);
            string[] names = new string[size];
            string[] tiers = new string[size];
            int[] ranks = new int[size];
            int count = 0;
            for (int i = 0; i < list.Count && count < size; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary entry = list[i].DataDictionary;
                string name = ReadString(entry, "name").Trim();
                if (name.Length == 0) continue;
                names[count] = name.Length > 28 ? name.Substring(0, 27) + "…" : name;
                tiers[count] = ReadString(entry, "tier");
                ranks[count] = TierRank(tiers[count]);
                count++;
            }
            // the same list again changes nothing on screen, so a search never restarts the animation
            bool same = count == _count;
            for (int i = 0; same && i < count; i++) same = names[i] == _name[i] && tiers[i] == _tier[i];
            if (same) return;

            // best tier first, keeping aviseek's order inside a tier
            for (int i = 1; i < count; i++)
            {
                string n = names[i];
                string t = tiers[i];
                int r = ranks[i];
                int j = i - 1;
                while (j >= 0 && ranks[j] > r)
                {
                    names[j + 1] = names[j];
                    tiers[j + 1] = tiers[j];
                    ranks[j + 1] = ranks[j];
                    j--;
                }
                names[j + 1] = n;
                tiers[j + 1] = t;
                ranks[j + 1] = r;
            }
            _name = names;
            _tier = tiers;
            _rank = ranks;
            _count = count;
            BuildPages();
            _page = 0;
            ShowPage(0);
            StartIn();
            ScheduleTurn();
        }

        private void BuildPages()
        {
            int slots = Slots();
            int[] starts = new int[_count + 1];
            int[] lengths = new int[_count + 1];
            int pages = 0;
            int i = 0;
            while (i < _count)
            {
                int start = i;
                int rank = _rank[i];
                while (i < _count && _rank[i] == rank && i - start < slots) i++;
                starts[pages] = start;
                lengths[pages] = i - start;
                pages++;
            }
            if (pages == 0)
            {
                starts[0] = 0;
                lengths[0] = 0;
                pages = 1;
            }
            _pageStart = starts;
            _pageLength = lengths;
            _pages = pages;
        }

        private int Slots()
        {
            return rows == null || rows.Length == 0 ? 1 : rows.Length;
        }

        private void ShowPage(int page)
        {
            _page = Mathf.Clamp(page, 0, _pages - 1);
            int slots = rows == null ? 0 : rows.Length;
            int start = _pageStart[_page];
            int length = _pageLength[_page];
            for (int i = 0; i < slots; i++)
            {
                bool used = i < length;
                if (rows[i] != null) rows[i].SetActive(used);
                if (!used) continue;
                int item = start + i;
                Color color = TierColor(_rank[item]);
                if (rowBacks != null && i < rowBacks.Length && rowBacks[i] != null) rowBacks[i].color = Color.Lerp(rowBase, color, 0.16f);
                if (rowAccents != null && i < rowAccents.Length && rowAccents[i] != null) rowAccents[i].color = color;
                // names are player written, keep their tags from styling the panel
                if (rowNames != null && i < rowNames.Length && rowNames[i] != null)
                {
                    rowNames[i].text = _name[item].Replace("<", "<​");
                    rowNames[i].color = Color.Lerp(Color.white, color, 0.3f);
                }
                if (rowTiers != null && i < rowTiers.Length && rowTiers[i] != null)
                {
                    rowTiers[i].text = _tier[item].Length > 0 ? _tier[item] : "Supporter";
                    rowTiers[i].color = color;
                }
                if (rowTierBacks != null && i < rowTierBacks.Length && rowTierBacks[i] != null) rowTierBacks[i].color = new Color(color.r * 0.26f, color.g * 0.26f, color.b * 0.26f, 1f);
                if (rowRanks != null && i < rowRanks.Length && rowRanks[i] != null)
                {
                    rowRanks[i].text = (item + 1).ToString();
                    rowRanks[i].color = color;
                }
            }
            if (emptyLabel != null) emptyLabel.SetActive(_count == 0);
            if (pager != null) pager.SetActive(_pages > 1);
            if (pageLabel != null) pageLabel.text = (_page + 1) + " / " + _pages;
            if (countLabel != null)
            {
                if (_count == 0) countLabel.text = "";
                else
                {
                    int rank = _rank[start];
                    string tier = _tier[start].Length > 0 ? _tier[start] : "Supporters";
                    countLabel.text = "<color=#" + Hex(TierColor(rank)) + ">" + tier.ToUpper() + "</color>  <color=#ffffff88>" + TierCount(rank) + "  ·  " + _count + " in all</color>";
                }
            }
        }

        private int TierCount(int rank)
        {
            int n = 0;
            for (int i = 0; i < _count; i++) if (_rank[i] == rank) n++;
            return n;
        }

        // lower is better: platinum legend+, platinum, gold, silver, bronze, anything else
        private int TierRank(string tier)
        {
            string t = tier.ToLower();
            bool plus = t.IndexOf("+") >= 0 || t.IndexOf("plus") >= 0;
            if (t.IndexOf("platinum") >= 0 || t.IndexOf("diamond") >= 0) return plus ? 0 : 1;
            if (t.IndexOf("gold") >= 0) return 2;
            if (t.IndexOf("silver") >= 0) return 3;
            if (t.IndexOf("bronze") >= 0) return 4;
            return Tiers - 1;
        }

        private Color TierColor(int rank)
        {
            if (rank == 0) return platinumPlus;
            if (rank == 1) return platinum;
            if (rank == 2) return gold;
            if (rank == 3) return silver;
            if (rank == 4) return bronze;
            return other;
        }

        private string Hex(Color color)
        {
            return ((int)(color.r * 255f)).ToString("X2") + ((int)(color.g * 255f)).ToString("X2") + ((int)(color.b * 255f)).ToString("X2");
        }

        /* ───────────── page turns ───────────── */

        public void _Next() { TurnTo(_page + 1); ScheduleTurn(); }
        public void _Prev() { TurnTo(_page - 1); ScheduleTurn(); }

        private void TurnTo(int page)
        {
            if (_pages < 2) return;
            _target = (page % _pages + _pages) % _pages;
            _phase = 1;
            _phaseStart = Time.time;
            Run();
        }

        // the page fades and its names flip down into place, one after another
        private void StartIn()
        {
            _phase = 2;
            _phaseStart = Time.time;
            SetFlip(0f);
            Run();
        }

        private void Run()
        {
            if (_animating) return;
            _animating = true;
            SendCustomEventDelayedFrames(nameof(_Animate), 1);
        }

        public void _Animate()
        {
            float t = Time.time - _phaseStart;
            if (_phase == 1)
            {
                SetAlpha(1f - Mathf.Clamp01(t / FadeOut));
                if (t >= FadeOut)
                {
                    ShowPage(_target);
                    _phase = 2;
                    _phaseStart = Time.time;
                    SetFlip(0f);
                }
            }
            else if (_phase == 2)
            {
                SetAlpha(Mathf.Clamp01(t / FadeIn));
                SetFlip(t);
                if (t >= FadeIn && t >= FlipTime + FlipStagger * Slots())
                {
                    SetAlpha(1f);
                    SetFlip(999f);
                    _phase = 0;
                }
            }
            if (_phase != 0 && gameObject.activeInHierarchy)
            {
                SendCustomEventDelayedFrames(nameof(_Animate), 1);
                return;
            }
            if (_phase != 0)
            {
                // a hidden panel just lands on the page
                if (_phase == 1) ShowPage(_target);
                SetAlpha(1f);
                SetFlip(999f);
                _phase = 0;
            }
            _animating = false;
        }

        private void SetAlpha(float alpha)
        {
            if (rowsGroup != null) rowsGroup.alpha = alpha;
        }

        // each line swings down from flat with a small overshoot, like a flip board
        private void SetFlip(float t)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                float k = Mathf.Clamp01((t - i * FlipStagger) / FlipTime);
                float c = 1.9f;
                float ease = 1f + (c + 1f) * Mathf.Pow(k - 1f, 3f) + c * Mathf.Pow(k - 1f, 2f);
                rows[i].transform.localRotation = Quaternion.Euler((1f - ease) * -90f, 0f, 0f);
            }
        }

        // the link box is editable only so the vrchat keyboard opens for copying
        public void _RestoreLink()
        {
            if (linkField != null && linkField.text != supportLink) linkField.text = supportLink;
        }

        private void ScheduleTurn()
        {
            if (autoPageSeconds <= 0f || _pages < 2) return;
            _turn++;
            SendCustomEventDelayedSeconds(nameof(_AutoTurn), autoPageSeconds);
        }

        public void _AutoTurn()
        {
            _turn--;
            if (_turn > 0) return;
            _turn = 0;
            if (_pages > 1) TurnTo(_page + 1);
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
