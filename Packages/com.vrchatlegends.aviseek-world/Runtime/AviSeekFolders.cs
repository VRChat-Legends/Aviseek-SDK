using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRChatLegends.AviSeek
{
    // the folders panel on the wall: the player's aviseek folders, the same ones as on the
    // website. a folder's row shows it on the wall, + puts the selected avatar in it and the
    // bin deletes it after a second tap. everything goes through the profile menu, which holds
    // the account, so this panel keeps no state of its own beyond the page and the typing
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Folders")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekFolders : UdonSharpBehaviour
    {
        public AviSeekMenu menu;
        public AviSeekSearch search;

        [Header("Signed in or not")]
        public GameObject signedInRoot;
        public GameObject signedOutRoot;

        [Header("Folder rows")]
        public GameObject[] rows;
        public Image[] rowBacks;
        public TextMeshProUGUI[] rowNames;
        public TextMeshProUGUI[] rowCounts;
        public TextMeshProUGUI[] deleteLabels;
        public TextMeshProUGUI pageLabel;
        public Image allBack;
        public TextMeshProUGUI allCount;

        [Header("New folder")]
        public VRCUrlInputField nameField;
        public TextMeshProUGUI nameDisplay;
        public TextMeshProUGUI nameHint;

        [Header("Summary")]
        public TextMeshProUGUI selectedLabel;
        public TextMeshProUGUI limitLabel;
        public RectTransform limitFill;
        public TextMeshProUGUI statusLabel;

        public Color accent = new Color(0.52f, 0.48f, 0.99f, 1f);
        public Color rowIdle = new Color(1f, 1f, 1f, 0.05f);
        public Color danger = new Color(1f, 0.44f, 0.49f, 1f);

        private int _page;
        private int _confirm = -1;
        private float _confirmUntil;
        private int _shown = -2;
        private string _typed = "";
        private bool _wasFocused;
        private string _lastSelected = "";

        private void Start()
        {
            ResetField();
            Refresh();
        }

        private void Update()
        {
            if (nameField != null)
            {
                bool focused = nameField.isFocused;
                if (focused || _wasFocused) RefreshTyped();
                _wasFocused = focused;
            }
            if (_confirm >= 0 && Time.time > _confirmUntil)
            {
                _confirm = -1;
                ShowRows();
            }
            // the selected avatar changes on the wall, a cheap check keeps the line current
            if (search != null && search.selId != _lastSelected) ShowSelected();
        }

        // the menu calls this whenever the account changed
        public void Refresh()
        {
            bool signedIn = menu != null && menu.signedIn;
            if (signedInRoot != null) signedInRoot.SetActive(signedIn);
            if (signedOutRoot != null) signedOutRoot.SetActive(!signedIn);
            if (!signedIn)
            {
                _shown = -2;
                return;
            }
            ShowRows();
            ShowSelected();
            if (limitLabel != null) limitLabel.text = menu.favoriteCount + " / " + (menu.favoriteLimit > 0 ? menu.favoriteLimit.ToString() : "?") + " favorites  ·  " + menu.folderNameList.Length + " / " + menu.maxFolders + " folders";
            if (limitFill != null)
            {
                float k = menu.favoriteLimit > 0 ? Mathf.Clamp01(menu.favoriteCount / (float)menu.favoriteLimit) : 0f;
                limitFill.localScale = new Vector3(Mathf.Max(0.001f, k), 1f, 1f);
            }
        }

        public void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }

        private void ShowRows()
        {
            if (rows == null || menu == null) return;
            string[] names = menu.folderNameList;
            int[] counts = menu.folderCountList;
            int size = rows.Length;
            int pages = Mathf.Max(1, (names.Length + size - 1) / size);
            if (_page >= pages) _page = pages - 1;
            int start = _page * size;
            for (int i = 0; i < size; i++)
            {
                int index = start + i;
                bool has = index < names.Length;
                if (rows[i] != null) rows[i].SetActive(has);
                if (!has) continue;
                if (rowNames != null && i < rowNames.Length && rowNames[i] != null) rowNames[i].text = names[index].Replace("<", "<​");
                if (rowCounts != null && i < rowCounts.Length && rowCounts[i] != null) rowCounts[i].text = counts[index].ToString();
                if (rowBacks != null && i < rowBacks.Length && rowBacks[i] != null) rowBacks[i].color = index == _shown ? new Color(accent.r, accent.g, accent.b, 0.35f) : rowIdle;
                if (deleteLabels != null && i < deleteLabels.Length && deleteLabels[i] != null)
                {
                    bool asking = index == _confirm;
                    deleteLabels[i].text = asking ? "Sure?" : "";
                    deleteLabels[i].color = asking ? danger : new Color(1f, 1f, 1f, 0.55f);
                }
            }
            if (pageLabel != null) pageLabel.text = names.Length == 0 ? "No folders yet. Make one below." : (_page + 1) + " / " + pages;
            if (allBack != null) allBack.color = _shown == -1 ? new Color(accent.r, accent.g, accent.b, 0.35f) : rowIdle;
            if (allCount != null) allCount.text = menu.favoriteCount.ToString();
        }

        private void ShowSelected()
        {
            _lastSelected = search == null ? "" : search.selId;
            if (selectedLabel == null) return;
            if (_lastSelected.Length == 0)
            {
                selectedLabel.text = "<color=#ffffff88>Pick an avatar on the wall, then press + on a folder.</color>";
                return;
            }
            bool saved = menu != null && menu.IsFavorite(_lastSelected);
            selectedLabel.text = "Selected: <b>" + search.selName.Replace("<", "<​") + "</b>" + (saved ? "" : "  <color=#ffffff88>(press the heart to save it first)</color>");
        }

        /* ───────────── buttons ───────────── */

        public void _Show0() { ShowRow(0); }
        public void _Show1() { ShowRow(1); }
        public void _Show2() { ShowRow(2); }
        public void _Show3() { ShowRow(3); }
        public void _Show4() { ShowRow(4); }
        public void _Show5() { ShowRow(5); }

        public void _Add0() { AddRow(0); }
        public void _Add1() { AddRow(1); }
        public void _Add2() { AddRow(2); }
        public void _Add3() { AddRow(3); }
        public void _Add4() { AddRow(4); }
        public void _Add5() { AddRow(5); }

        public void _Delete0() { DeleteRow(0); }
        public void _Delete1() { DeleteRow(1); }
        public void _Delete2() { DeleteRow(2); }
        public void _Delete3() { DeleteRow(3); }
        public void _Delete4() { DeleteRow(4); }
        public void _Delete5() { DeleteRow(5); }

        public void _ShowAll()
        {
            if (menu == null) return;
            _shown = -1;
            menu._AllFavorites();
            ShowRows();
        }

        // takes the selected avatar out of whatever folder it is in
        public void _Unfile()
        {
            if (!HaveSelected()) return;
            menu.MoveToFolder(search.selId, search.selSlot, search.selTile, 0);
        }

        public void _Next()
        {
            if (menu == null || rows == null) return;
            if ((_page + 1) * rows.Length < menu.folderNameList.Length) _page++;
            ShowRows();
        }

        public void _Prev()
        {
            if (_page > 0) _page--;
            ShowRows();
        }

        public void _Sync()
        {
            if (menu != null) menu._Sync();
        }

        public void _OpenProfile()
        {
            if (menu != null) menu._Open();
        }

        public void _Create()
        {
            if (menu == null || nameField == null) return;
            // the box's own address, not the copy shown while typing: enter can come first
            RefreshTyped();
            if (_typed.Trim().Length == 0)
            {
                SetStatus("Click the name box, type a name after the address and press Enter.");
                ResetField();
                return;
            }
            menu.CreateFolder(nameField.GetUrl());
            ResetField();
        }

        private int Index(int slot)
        {
            return _page * (rows == null ? 1 : rows.Length) + slot;
        }

        private void ShowRow(int slot)
        {
            if (menu == null) return;
            _shown = Index(slot);
            menu.ShowFolder(_shown);
            ShowRows();
        }

        private void AddRow(int slot)
        {
            if (!HaveSelected()) return;
            menu.MoveToFolder(search.selId, search.selSlot, search.selTile, Index(slot) + 1);
        }

        private void DeleteRow(int slot)
        {
            if (menu == null) return;
            int index = Index(slot);
            if (_confirm != index)
            {
                _confirm = index;
                _confirmUntil = Time.time + 4f;
                ShowRows();
                SetStatus("Tap again to delete " + menu.FolderName(index).Replace("<", "<​") + ". Its avatars stay in your favorites.");
                return;
            }
            _confirm = -1;
            if (_shown == index) _shown = -2;
            menu.DeleteFolder(index + 1);
            ShowRows();
        }

        private bool HaveSelected()
        {
            if (menu == null || search == null) return false;
            if (search.selId.Length == 0)
            {
                SetStatus("Pick an avatar on the wall first.");
                return false;
            }
            if (search.selSlot < 0 || search.selTile < 0)
            {
                SetStatus("Search for this avatar first, then put it in a folder.");
                return false;
            }
            return true;
        }

        /* ───────────── typing a name ───────────── */

        private void RefreshTyped()
        {
            VRCUrl url = nameField.GetUrl();
            string text = url == null ? "" : url.Get();
            if (text == null) text = "";
            VRCUrl prefixUrl = menu == null ? null : menu.createFolderPrefixUrl;
            string prefix = prefixUrl == null ? "" : prefixUrl.Get();
            string typed = prefix.Length > 0 && text.StartsWith(prefix) ? text.Substring(prefix.Length) : "";
            typed = typed.Replace("+", " ").Replace("%20", " ");
            if (typed == _typed) return;
            _typed = typed;
            if (nameDisplay != null) nameDisplay.text = _typed.Replace("<", "<​");
            if (nameHint != null) nameHint.gameObject.SetActive(_typed.Length == 0);
        }

        private void ResetField()
        {
            if (nameField != null && menu != null && menu.createFolderPrefixUrl != null) nameField.SetUrl(menu.createFolderPrefixUrl);
            _typed = "";
            if (nameDisplay != null) nameDisplay.text = "";
            if (nameHint != null) nameHint.gameObject.SetActive(true);
        }
    }
}
