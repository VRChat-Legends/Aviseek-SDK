using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.Economy;

namespace VRChatLegends.AviSeek
{
    // the links panel. worlds can't open web pages, so each link sits in a box
    // players can select and copy. vrchat groups also get a button that opens
    // the group right in game. edit the list in the inspector
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Socials")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekSocials : UdonSharpBehaviour
    {
        [Header("Links, one entry per line")]
        public string[] labels = new string[0];
        public string[] links = new string[0];
        [Tooltip("The icon character, from the Simple Icons font when Brand Icon is ticked, otherwise from Lucide.")]
        public string[] icons = new string[0];
        public bool[] brandIcons = new bool[0];
        public Color[] iconColors = new Color[0];
        [Tooltip("A VRChat group id (grp_...) to show an Open button for, or leave empty.")]
        public string[] groupIds = new string[0];

        [Header("Lines on the panel")]
        public GameObject[] rows;
        public TextMeshProUGUI[] rowIcons;
        public TextMeshProUGUI[] rowBrandIcons;
        public TextMeshProUGUI[] rowLabels;
        public TMP_InputField[] rowLinks;
        public GameObject[] rowOpenButtons;

        [HideInInspector] public int rowIndex;

        private void Start()
        {
            Refresh();
        }

        // also run by the inspector, so the panel in the editor matches the list
        public void Refresh()
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = labels != null && i < labels.Length && !string.IsNullOrEmpty(labels[i]);
                if (rows[i] != null) rows[i].SetActive(used);
                if (!used) continue;
                bool brand = brandIcons != null && i < brandIcons.Length && brandIcons[i];
                string icon = icons != null && i < icons.Length ? icons[i] : "";
                Color color = iconColors != null && i < iconColors.Length ? iconColors[i] : Color.white;
                if (rowIcons != null && i < rowIcons.Length && rowIcons[i] != null)
                {
                    rowIcons[i].text = brand ? "" : icon;
                    rowIcons[i].color = color;
                }
                if (rowBrandIcons != null && i < rowBrandIcons.Length && rowBrandIcons[i] != null)
                {
                    rowBrandIcons[i].text = brand ? icon : "";
                    rowBrandIcons[i].color = color;
                }
                if (rowLabels != null && i < rowLabels.Length && rowLabels[i] != null) rowLabels[i].text = labels[i];
                if (rowLinks != null && i < rowLinks.Length && rowLinks[i] != null) rowLinks[i].text = links != null && i < links.Length ? links[i] : "";
                bool group = groupIds != null && i < groupIds.Length && !string.IsNullOrEmpty(groupIds[i]);
                if (rowOpenButtons != null && i < rowOpenButtons.Length && rowOpenButtons[i] != null) rowOpenButtons[i].SetActive(group);
            }
        }

        public void _OpenGroup()
        {
            if (groupIds == null || rowIndex < 0 || rowIndex >= groupIds.Length || string.IsNullOrEmpty(groupIds[rowIndex])) return;
            Store.OpenGroupPage(groupIds[rowIndex]);
        }
    }
}
