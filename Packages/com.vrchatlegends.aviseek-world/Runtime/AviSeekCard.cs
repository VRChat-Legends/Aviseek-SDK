using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace VRChatLegends.AviSeek
{
    // one tile on the results wall. it only paints what the search hands it and
    // passes its three buttons back up, the search owns all the data
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Card")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekCard : UdonSharpBehaviour
    {
        public AviSeekSearch search;
        public int slot;

        [Header("Picture")]
        public RawImage thumb;
        [Tooltip("Shown while the picture is missing or still loading.")]
        public GameObject placeholder;
        public Image frame;

        [Header("Text")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI authorLabel;
        public TextMeshProUGUI heartIcon;

        [Header("Platforms")]
        public GameObject pcBadge;
        public GameObject questBadge;
        public GameObject iosBadge;
        public Image pcDot;
        public Image questDot;
        public Image iosDot;

        public void Show(string avatarName, string author, int platforms, Color pc, Color quest, Color ios, Texture picture, bool favorite, bool selected, Color heartOn, Color heartOff, Color frameOn, Color frameOff)
        {
            gameObject.SetActive(true);
            if (nameLabel != null) nameLabel.text = avatarName;
            if (authorLabel != null) authorLabel.text = "by " + author;
            if (thumb != null)
            {
                thumb.texture = picture;
                thumb.enabled = picture != null;
            }
            if (placeholder != null) placeholder.SetActive(picture == null);
            if (pcBadge != null) pcBadge.SetActive((platforms & 1) != 0);
            if (questBadge != null) questBadge.SetActive((platforms & 2) != 0);
            if (iosBadge != null) iosBadge.SetActive((platforms & 4) != 0);
            if (pcDot != null) pcDot.color = pc;
            if (questDot != null) questDot.color = quest;
            if (iosDot != null) iosDot.color = ios;
            if (heartIcon != null) heartIcon.color = favorite ? heartOn : heartOff;
            if (frame != null) frame.color = selected ? frameOn : frameOff;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void _Use()
        {
            if (search != null) search._CardUse(slot);
        }

        public void _Favorite()
        {
            if (search != null) search._CardFavorite(slot);
        }

        public void _Select()
        {
            if (search != null) search._CardSelect(slot);
        }
    }
}
