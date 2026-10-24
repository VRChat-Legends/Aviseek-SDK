using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Economy;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace VRChatLegends.AviSeek
{
    // the creator store panel: shows the world's VRChat Creator Economy products, opens
    // their listings, marks what the player already owns and can switch on perks for
    // owners. the panel is off in the prefab, the world maker switches it on
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    [AddComponentMenu("AviSeek/AviSeek Store")]
    [HelpURL("https://creators.vrchat.com/economy/")]
    public class AviSeekStore : UdonSharpBehaviour
    {
        [Header("Products")]
        [Tooltip("The UdonProduct assets to show, in order.")]
        public UdonProduct[] products = new UdonProduct[0];
        [Tooltip("The store listing id for each product, from the VRChat Creator Dashboard. Get opens that listing. Leave one empty to open the store page instead.")]
        public string[] listingIds = new string[0];

        [Header("Store buttons")]
        [Tooltip("Your VRChat group id (grp_...). Shows a Group store button and is used when a product has no listing id.")]
        public string groupId = "";
        [Tooltip("Shows a World store button that opens this world's store page.")]
        public bool showWorldStore = true;

        [Header("Extras")]
        [Tooltip("Shows a thank you line on the panel when anyone in the instance buys something.")]
        public bool announcePurchases = true;
        [Tooltip("Hides the Get button on products the player already owns.")]
        public bool hideGetWhenOwned = true;
        [Tooltip("Objects switched on for the local player when they own the matching product, such as a badge, a room key or a special prop.")]
        public GameObject[] perkObjects = new GameObject[0];
        [Tooltip("For each perk object, the index of the product (from 0) that unlocks it.")]
        public int[] perkProducts = new int[0];

        [Header("Lines on the panel")]
        public GameObject[] rows;
        public TextMeshProUGUI[] rowNames;
        public TextMeshProUGUI[] rowDescriptions;
        public RawImage[] rowPictures;
        public GameObject[] rowOwned;
        public GameObject[] rowGetButtons;
        public GameObject emptyLabel;
        public GameObject pager;
        public TextMeshProUGUI pageLabel;
        public GameObject groupStoreButton;
        public GameObject worldStoreButton;
        public TextMeshProUGUI announceLabel;

        [HideInInspector] public int rowIndex;

        private bool[] _owned = new bool[0];
        private int _page;
        private int _announce;

        private void Start()
        {
            _owned = new bool[products == null ? 0 : products.Length];
            if (groupStoreButton != null) groupStoreButton.SetActive(!string.IsNullOrEmpty(groupId));
            if (worldStoreButton != null) worldStoreButton.SetActive(showWorldStore);
            if (announceLabel != null) announceLabel.text = "";
            Show();
            ApplyPerks();
            if (_owned.Length > 0 && Networking.LocalPlayer != null) Store.ListPurchases((IUdonEventReceiver)this, Networking.LocalPlayer);
        }

        private void Show()
        {
            int slots = rows == null ? 0 : rows.Length;
            int count = products == null ? 0 : products.Length;
            int start = _page * slots;
            for (int i = 0; i < slots; i++)
            {
                int item = start + i;
                bool used = item < count && products[item] != null;
                if (rows[i] != null) rows[i].SetActive(used);
                if (!used) continue;
                UdonProduct product = products[item];
                bool owned = item < _owned.Length && _owned[item];
                if (rowNames != null && i < rowNames.Length && rowNames[i] != null) rowNames[i].text = product.Name;
                if (rowDescriptions != null && i < rowDescriptions.Length && rowDescriptions[i] != null) rowDescriptions[i].text = product.Description;
                if (rowPictures != null && i < rowPictures.Length && rowPictures[i] != null)
                {
                    Texture2D picture = product.Image;
                    rowPictures[i].texture = picture;
                    rowPictures[i].enabled = picture != null;
                }
                if (rowOwned != null && i < rowOwned.Length && rowOwned[i] != null) rowOwned[i].SetActive(owned);
                if (rowGetButtons != null && i < rowGetButtons.Length && rowGetButtons[i] != null) rowGetButtons[i].SetActive(!(owned && hideGetWhenOwned));
            }
            if (emptyLabel != null) emptyLabel.SetActive(count == 0);
            if (pager != null) pager.SetActive(Pages() > 1);
            if (pageLabel != null) pageLabel.text = (_page + 1) + " / " + Pages();
        }

        private int Pages()
        {
            int slots = rows == null || rows.Length == 0 ? 1 : rows.Length;
            int count = products == null ? 0 : products.Length;
            return Mathf.Max(1, (count + slots - 1) / slots);
        }

        private void ApplyPerks()
        {
            if (perkObjects == null) return;
            for (int i = 0; i < perkObjects.Length; i++)
            {
                if (perkObjects[i] == null) continue;
                int product = perkProducts != null && i < perkProducts.Length ? perkProducts[i] : -1;
                perkObjects[i].SetActive(product >= 0 && product < _owned.Length && _owned[product]);
            }
        }

        private int ProductIndex(IProduct product)
        {
            if (product == null || products == null) return -1;
            string id = product.ID;
            for (int i = 0; i < products.Length; i++) if (products[i] != null && products[i].ID == id) return i;
            return -1;
        }

        private void MarkOwned(IProduct product, bool owned)
        {
            int index = ProductIndex(product);
            if (index < 0 || index >= _owned.Length) return;
            _owned[index] = owned;
        }

        /* ───────────── vrchat store events ───────────── */

        public override void OnListPurchases(IProduct[] purchased, VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            for (int i = 0; i < _owned.Length; i++) _owned[i] = false;
            if (purchased != null) for (int i = 0; i < purchased.Length; i++) MarkOwned(purchased[i], true);
            Show();
            ApplyPerks();
        }

        public override void OnPurchasesLoaded(IProduct[] purchased, VRCPlayerApi player)
        {
            OnListPurchases(purchased, player);
        }

        public override void OnPurchaseConfirmed(IProduct product, VRCPlayerApi player, bool purchased)
        {
            if (player == null) return;
            if (player.isLocal)
            {
                MarkOwned(product, true);
                Show();
                ApplyPerks();
            }
            if (announcePurchases && purchased && product != null && ProductIndex(product) >= 0) Announce(player.displayName + " just picked up " + product.Name + ". Thank you!");
        }

        public override void OnPurchaseExpired(IProduct product, VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            MarkOwned(product, false);
            Show();
            ApplyPerks();
        }

        private void Announce(string text)
        {
            if (announceLabel == null) return;
            announceLabel.text = text.Replace("<", "<​");
            _announce++;
            SendCustomEventDelayedSeconds(nameof(_ClearAnnounce), 8f);
        }

        public void _ClearAnnounce()
        {
            _announce--;
            if (_announce > 0 || announceLabel == null) return;
            _announce = 0;
            announceLabel.text = "";
        }

        /* ───────────── buttons ───────────── */

        public void _Get()
        {
            int slots = rows == null ? 0 : rows.Length;
            int item = _page * slots + rowIndex;
            if (products == null || rowIndex < 0 || item >= products.Length) return;
            string listing = listingIds != null && item < listingIds.Length ? listingIds[item] : "";
            if (!string.IsNullOrEmpty(listing)) Store.OpenListing(listing);
            else if (!string.IsNullOrEmpty(groupId)) Store.OpenGroupStorePage(groupId);
            else Store.OpenWorldStorePage();
        }

        public void _OpenGroupStore()
        {
            if (!string.IsNullOrEmpty(groupId)) Store.OpenGroupStorePage(groupId);
        }

        public void _OpenWorldStore()
        {
            Store.OpenWorldStorePage();
        }

        public void _Next()
        {
            _page = (_page + 1) % Pages();
            Show();
        }

        public void _Prev()
        {
            _page = (_page - 1 + Pages()) % Pages();
            Show();
        }
    }
}
