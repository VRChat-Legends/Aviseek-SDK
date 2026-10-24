using UdonSharp;
using UnityEngine;

namespace VRChatLegends.AviSeek
{
    // one line in a list panel. its buttons tell the panel which line was pressed
    // and then fire the panel's event, so one script serves every list
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Row")]
    [HelpURL("https://aviseek.vrchatlegends.com/world")]
    public class AviSeekRow : UdonSharpBehaviour
    {
        public UdonSharpBehaviour target;
        public int index;
        public string primaryEvent = "";
        public string secondaryEvent = "";

        public void _Primary()
        {
            Send(primaryEvent);
        }

        public void _Secondary()
        {
            Send(secondaryEvent);
        }

        private void Send(string eventName)
        {
            if (target == null || string.IsNullOrEmpty(eventName)) return;
            target.SetProgramVariable("rowIndex", index);
            target.SendCustomEvent(eventName);
        }
    }
}
