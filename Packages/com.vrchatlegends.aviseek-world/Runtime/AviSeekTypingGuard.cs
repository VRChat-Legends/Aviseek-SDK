using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace VRChatLegends.AviSeek
{
    // on desktop, a text box that is still selected keeps the keyboard, so walking off with WASD
    // typed into it. while a box is selected the player stands still, so letters only type. once
    // they stop typing for a moment, a movement key leaves the box instead: the letter is taken
    // back out and they walk off as usual. vr players type on vrchat's own keyboard, so this
    // never touches them
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Typing Guard")]
    public class AviSeekTypingGuard : UdonSharpBehaviour
    {
        public VRCUrlInputField[] urlFields;
        public TMP_InputField[] textFields;
        [Tooltip("Seconds without typing after which a movement key leaves the box.")]
        public float idleSeconds = 2f;
        public AviSeekSearch search;
        public AviSeekMenu menu;

        private bool _held;
        private bool _vr;
        private float _nextCheck;
        private int _url = -1;
        private int _text = -1;
        private string _seen = "";
        private VRCUrl _lastUrl;
        private string _lastText = "";
        private float _lastEdit;
        private bool _edited;
        // the box that was left, and what it held before the movement key, put back for a few frames
        private int _undoUrl = -1;
        private int _undoText = -1;
        private VRCUrl _keepUrl;
        private string _keepText = "";
        private int _undoFrames;

        private void Update()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + 2f;
                _vr = player.IsUserInVR();
            }
            if (_undoFrames > 0) Undo();
            if (_vr)
            {
                Release(player);
                return;
            }

            int url = -1;
            int text = -1;
            if (urlFields != null)
            {
                for (int i = 0; i < urlFields.Length; i++) if (urlFields[i] != null && urlFields[i].isFocused) { url = i; break; }
            }
            if (url < 0 && textFields != null)
            {
                for (int i = 0; i < textFields.Length; i++) if (textFields[i] != null && textFields[i].isFocused) { text = i; break; }
            }
            if (url < 0 && text < 0)
            {
                _url = -1;
                _text = -1;
                Release(player);
                return;
            }

            string now = Current(url, text);
            if (url != _url || text != _text)
            {
                // a box was just selected
                _url = url;
                _text = text;
                _seen = now;
                _lastEdit = Time.time;
                _edited = false;
                Snapshot(url, text);
            }
            if (!_held)
            {
                player.Immobilize(true);
                _held = true;
            }

            bool move = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)
                || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
            if (move && _edited && Time.time - _lastEdit > idleSeconds)
            {
                // done typing and walking off: the box lets go, and the letter comes back out
                _undoUrl = url;
                _undoText = text;
                _keepUrl = _lastUrl;
                _keepText = _lastText;
                _undoFrames = 3;
                if (url >= 0) urlFields[url].DeactivateInputField();
                else
                {
                    // udon can not call DeactivateInputField on these, and switching one off does the same
                    textFields[text].enabled = false;
                    textFields[text].enabled = true;
                }
                Undo();
                _url = -1;
                _text = -1;
                Release(player);
                return;
            }

            if (now != _seen)
            {
                _seen = now;
                _lastEdit = Time.time;
                _edited = true;
            }
            Snapshot(url, text);
        }

        private string Current(int url, int text)
        {
            if (url >= 0)
            {
                VRCUrl value = urlFields[url].GetUrl();
                string got = value == null ? "" : value.Get();
                return got == null ? "" : got;
            }
            string typed = textFields[text].text;
            return typed == null ? "" : typed;
        }

        private void Snapshot(int url, int text)
        {
            if (url >= 0) _lastUrl = urlFields[url].GetUrl();
            else _lastText = textFields[text].text;
        }

        private void Undo()
        {
            _undoFrames--;
            if (_undoUrl >= 0 && urlFields != null && _undoUrl < urlFields.Length && urlFields[_undoUrl] != null && _keepUrl != null)
            {
                urlFields[_undoUrl].SetUrl(_keepUrl);
            }
            else if (_undoText >= 0 && textFields != null && _undoText < textFields.Length && textFields[_undoText] != null)
            {
                textFields[_undoText].text = _keepText;
            }
            if (search != null) search._TypedChanged();
            if (menu != null) menu._TypedChanged();
        }

        private void Release(VRCPlayerApi player)
        {
            if (!_held) return;
            _held = false;
            player.Immobilize(false);
        }
    }
}
