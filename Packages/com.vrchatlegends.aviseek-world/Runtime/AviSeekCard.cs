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
        [Tooltip("The silhouette in the placeholder, it pulses while the picture is on its way.")]
        public Graphic placeholderIcon;
        public Image frame;

        [Header("Text")]
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI authorLabel;
        public TextMeshProUGUI heartIcon;

        [Header("Platforms")]
        public GameObject pcBadge;
        public GameObject questBadge;
        public GameObject iosBadge;
        [Tooltip("The Windows, Android and Apple logos in the bottom bar, tinted with each platform's performance rank.")]
        public Image pcDot;
        public Image questDot;
        public Image iosDot;

        private Texture _shown;
        private float _fade = 1f;
        private bool _fading;

        public void Show(string avatarName, string author, int platforms, Color pc, Color quest, Color ios, Texture picture, bool favorite, bool selected, Color heartOn, Color heartOff, Color frameOn, Color frameOff)
        {
            gameObject.SetActive(true);
            if (nameLabel != null) nameLabel.text = avatarName;
            if (authorLabel != null) authorLabel.text = "by " + author;
            if (thumb != null)
            {
                // a picture that just arrived fades in, one swapped for another shows at once
                if (picture != null && _shown == null) StartFade();
                else if (picture != _shown) SetFade(1f);
                thumb.texture = picture;
                thumb.enabled = picture != null;
            }
            _shown = picture;
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
            _shown = null;
            gameObject.SetActive(false);
        }

        // the search drives one shared pulse for every card still waiting on its picture
        public void Pulse(float alpha)
        {
            if (placeholderIcon == null || placeholder == null || !placeholder.activeSelf) return;
            Color color = placeholderIcon.color;
            color.a = alpha;
            placeholderIcon.color = color;
        }

        private void StartFade()
        {
            SetFade(0f);
            if (_fading) return;
            _fading = true;
            SendCustomEventDelayedFrames(nameof(_FadeStep), 1);
        }

        public void _FadeStep()
        {
            SetFade(Mathf.Min(1f, _fade + Time.deltaTime * 4f));
            if (_fade < 1f && gameObject.activeInHierarchy)
            {
                SendCustomEventDelayedFrames(nameof(_FadeStep), 1);
                return;
            }
            SetFade(1f);
            _fading = false;
        }

        private void SetFade(float value)
        {
            _fade = value;
            if (thumb == null) return;
            Color color = thumb.color;
            color.a = value;
            thumb.color = color;
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
