using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace VRChatLegends.AviSeek
{
    // the mirror panel: off, players only, or the full world, chosen by each player for
    // themselves. a mirror costs a lot to draw, so it starts off, switches itself off while the
    // player is far from it and comes back as they walk up
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Mirror")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekMirror : UdonSharpBehaviour
    {
        private const string ModeKey = "aviseek.mirror";

        [Tooltip("Shows players only, the cheap one.")]
        public GameObject playersMirror;
        [Tooltip("Shows the whole world.")]
        public GameObject fullMirror;
        [Tooltip("Shown over the frame while the mirror is off.")]
        public GameObject offCover;
        [Tooltip("The Off, Players and Full buttons, in that order.")]
        public Image[] buttons;
        public TextMeshProUGUI[] buttonIcons;
        public TextMeshProUGUI hintLabel;
        [Tooltip("The mirror switches off past this distance, in metres, and back on inside it.")]
        public float wakeDistance = 9f;
        public Color accent = new Color(0.52f, 0.48f, 0.99f, 1f);
        public Color idle = new Color(0.165f, 0.133f, 0.267f, 1f);

        // 0 off, 1 players, 2 full
        private int _mode;
        private bool _near = true;
        private bool _restored;
        private float _nextCheck;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (_mode == 0 || Time.time < _nextCheck) return;
            _nextCheck = Time.time + 0.5f;
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;
            bool near = Vector3.Distance(player.GetPosition(), transform.position) < wakeDistance;
            if (near == _near) return;
            _near = near;
            Apply();
        }

        public void _Off() { SetMode(0); }
        public void _Players() { SetMode(1); }
        public void _Full() { SetMode(2); }

        private void SetMode(int mode)
        {
            _mode = mode;
            _near = true;
            _nextCheck = 0f;
            if (_restored) PlayerData.SetInt(ModeKey, _mode);
            Apply();
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            _restored = true;
            int mode;
            if (PlayerData.TryGetInt(player, ModeKey, out mode)) _mode = Mathf.Clamp(mode, 0, 2);
            Apply();
        }

        private void Apply()
        {
            bool shown = _near && _mode > 0;
            if (playersMirror != null) playersMirror.SetActive(shown && _mode == 1);
            if (fullMirror != null) fullMirror.SetActive(shown && _mode == 2);
            if (offCover != null) offCover.SetActive(!shown);
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++) if (buttons[i] != null) buttons[i].color = i == _mode ? accent : idle;
            }
            if (buttonIcons != null)
            {
                for (int i = 0; i < buttonIcons.Length; i++) if (buttonIcons[i] != null) buttonIcons[i].color = i == _mode ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            }
            if (hintLabel != null)
            {
                hintLabel.text = _mode == 0 ? "Pick Players or Full to look at your avatar."
                    : (_mode == 1 ? "Players only: lighter on your frame rate." : "The full world, lights and all.");
            }
        }
    }
}
