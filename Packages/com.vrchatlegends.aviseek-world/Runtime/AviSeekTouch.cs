using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace VRChatLegends.AviSeek
{
    // touch screen for vr players. it follows both index fingertips, shows a ring where a
    // finger hovers over a panel and presses whatever it pokes. the laser pointer keeps
    // working as well. the prefab builder fills the list of things that can be pressed
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Touch")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekTouch : UdonSharpBehaviour
    {
        [Tooltip("Lets vr players press the panels with their fingers.")]
        public bool touchEnabled = true;
        [Tooltip("How close a fingertip has to be before its ring shows, in metres.")]
        public float hoverDistance = 0.14f;
        [Tooltip("How close to the panel a fingertip presses, in metres.")]
        public float pressDistance = 0.014f;
        [Tooltip("How far a finger has to pull back before it can press again, in metres.")]
        public float releaseDistance = 0.035f;
        [Tooltip("Only players this close to the wall are tracked, in metres.")]
        public float nearDistance = 7f;

        [Header("Panels")]
        public RectTransform[] panels;

        [Header("Things to press")]
        public RectTransform[] targets;
        public UdonBehaviour[] targetBehaviours;
        public string[] targetEvents;
        [Tooltip("Text boxes are selected instead, which opens the VRChat keyboard.")]
        public Selectable[] targetFields;

        [Header("Cursors, left hand then right")]
        public Transform[] cursors;
        public RectTransform[] rings;
        public Image[] ringImages;
        public Image[] dots;
        public RectTransform[] ripples;
        public Image[] rippleImages;

        private bool[] _pressed = new bool[2];
        private float[] _ripple = { 9f, 9f };
        private float[] _spin = new float[2];
        private float[] _lastPress = { -9f, -9f };
        private bool _vr;
        private float _nextCheck;

        private void Start()
        {
            HideCursor(0);
            HideCursor(1);
        }

        private void Update()
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (player == null) return;
            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + 2f;
                _vr = player.IsUserInVR();
            }
            if (!touchEnabled || !_vr || panels == null || Vector3.Distance(player.GetPosition(), transform.position) > nearDistance)
            {
                HideCursor(0);
                HideCursor(1);
                return;
            }
            Track(player, 0, HumanBodyBones.LeftIndexDistal, VRCPlayerApi.TrackingDataType.LeftHand);
            Track(player, 1, HumanBodyBones.RightIndexDistal, VRCPlayerApi.TrackingDataType.RightHand);
        }

        private void Track(VRCPlayerApi player, int hand, HumanBodyBones bone, VRCPlayerApi.TrackingDataType tracking)
        {
            Vector3 tip = player.GetBonePosition(bone);
            if (tip == Vector3.zero)
            {
                // avatars without finger bones: a point just past the hand
                VRCPlayerApi.TrackingData data = player.GetTrackingData(tracking);
                tip = data.position + data.rotation * (Vector3.forward * 0.09f);
            }

            // the panel this finger is closest to, in front of it and inside its edges
            int best = -1;
            float bestDistance = 0f;
            Vector3 bestLocal = Vector3.zero;
            for (int i = 0; i < panels.Length; i++)
            {
                RectTransform panel = panels[i];
                if (panel == null || !panel.gameObject.activeInHierarchy) continue;
                Vector3 local = panel.InverseTransformPoint(tip);
                Rect rect = panel.rect;
                if (local.x < rect.xMin || local.x > rect.xMax || local.y < rect.yMin || local.y > rect.yMax) continue;
                // panels face -z, so a finger in front has a negative z, in canvas units
                float distance = -local.z * panel.lossyScale.z;
                if (distance < -0.05f || distance > hoverDistance) continue;
                if (best >= 0 && Mathf.Abs(distance) >= Mathf.Abs(bestDistance)) continue;
                best = i;
                bestDistance = distance;
                bestLocal = local;
            }
            if (best < 0)
            {
                _pressed[hand] = false;
                HideCursor(hand);
                return;
            }

            if (!_pressed[hand] && bestDistance <= pressDistance && Time.time - _lastPress[hand] > 0.25f)
            {
                _pressed[hand] = true;
                _lastPress[hand] = Time.time;
                _ripple[hand] = 0f;
                if (Press(tip)) player.PlayHapticEventInHand(hand == 0 ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, 0.06f, 0.5f, 180f);
            }
            else if (_pressed[hand] && bestDistance > releaseDistance) _pressed[hand] = false;

            DrawCursor(hand, panels[best], bestLocal, bestDistance);
        }

        // the topmost thing under the finger: later entries sit on top of earlier ones
        private bool Press(Vector3 tip)
        {
            if (targets == null) return false;
            for (int i = targets.Length - 1; i >= 0; i--)
            {
                RectTransform target = targets[i];
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                Vector3 local = target.InverseTransformPoint(tip);
                if (!target.rect.Contains(new Vector2(local.x, local.y))) continue;
                Selectable field = targetFields != null && i < targetFields.Length ? targetFields[i] : null;
                if (field != null)
                {
                    field.Select();
                    return true;
                }
                UdonBehaviour behaviour = targetBehaviours != null && i < targetBehaviours.Length ? targetBehaviours[i] : null;
                string eventName = targetEvents != null && i < targetEvents.Length ? targetEvents[i] : "";
                if (behaviour == null || string.IsNullOrEmpty(eventName)) continue;
                behaviour.SendCustomEvent(eventName);
                return true;
            }
            return false;
        }

        private void DrawCursor(int hand, RectTransform panel, Vector3 local, float distance)
        {
            if (cursors == null || hand >= cursors.Length || cursors[hand] == null) return;
            Transform cursor = cursors[hand];
            if (!cursor.gameObject.activeSelf) cursor.gameObject.SetActive(true);
            // sits just in front of the raised cards, so it is never hidden behind them
            cursor.position = panel.TransformPoint(new Vector3(local.x, local.y, -22f));
            cursor.rotation = panel.rotation;

            float near = 1f - Mathf.Clamp01(distance / hoverDistance);
            float pressed = _pressed[hand] ? 1f : 0f;
            _spin[hand] = (_spin[hand] + Time.deltaTime * (60f + near * 200f)) % 360f;
            if (rings != null && hand < rings.Length && rings[hand] != null)
            {
                float size = Mathf.Lerp(2.2f, 0.85f, near) - pressed * 0.15f;
                rings[hand].localScale = new Vector3(size, size, 1f);
                rings[hand].localRotation = Quaternion.Euler(0f, 0f, _spin[hand]);
            }
            SetAlpha(ringImages, hand, Mathf.Lerp(0.15f, 0.95f, near));
            SetAlpha(dots, hand, Mathf.Lerp(0.25f, 1f, near));

            // a ring that spreads out and fades every time the finger presses
            _ripple[hand] += Time.deltaTime;
            float k = Mathf.Clamp01(_ripple[hand] / 0.4f);
            if (ripples != null && hand < ripples.Length && ripples[hand] != null)
            {
                float size = 0.8f + k * 2.6f;
                ripples[hand].localScale = new Vector3(size, size, 1f);
            }
            SetAlpha(rippleImages, hand, (1f - k) * 0.9f);
        }

        private void HideCursor(int hand)
        {
            if (cursors == null || hand >= cursors.Length || cursors[hand] == null) return;
            if (cursors[hand].gameObject.activeSelf) cursors[hand].gameObject.SetActive(false);
        }

        private void SetAlpha(Image[] images, int hand, float alpha)
        {
            if (images == null || hand >= images.Length || images[hand] == null) return;
            Color color = images[hand].color;
            color.a = alpha;
            images[hand].color = color;
        }
    }
}
