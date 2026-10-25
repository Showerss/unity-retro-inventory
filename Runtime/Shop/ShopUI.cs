using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Retro.Inventory
{
    /// <summary>
    /// Trade window. Opens when any Shopkeeper is interacted with; click a row to buy or sell one.
    /// Builds its own UI on Awake, so it only needs to sit in the scene (an EventSystem must exist).
    /// Closes on Esc / E, the Leave button, or when the player walks out of the shopkeeper's range.
    /// While open the cursor is free, which stops looking and interacting.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Tooltip("Font for the window. Falls back to the TMP default.")]
        [SerializeField] private TMP_FontAsset _font;

        [Tooltip("Sorting order of the window's canvas; above the HUD, below the pause menu.")]
        [SerializeField] private int _sortingOrder = 40;

        [Header("Colours")]
        [SerializeField] private Color _windowColor = new Color(0.10f, 0.07f, 0.04f, 0.96f);
        [SerializeField] private Color _borderColor = new Color(0.62f, 0.48f, 0.22f, 1f);
        [SerializeField] private Color _rowColor = new Color(0.20f, 0.15f, 0.09f, 1f);
        [SerializeField] private Color _textColor = new Color(0.95f, 0.88f, 0.70f, 1f);
        [SerializeField] private Color _disabledTextColor = new Color(0.55f, 0.50f, 0.42f, 1f);

        private const float RowHeight = 34f;

        private GameObject _window;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _money;
        private TextMeshProUGUI _message;
        private RectTransform _buyList;
        private RectTransform _sellList;

        private Shopkeeper _shop;
        private GameObject _customer;
        private Wallet _wallet;
        private InventorySystem[] _inventories;
        private int _openedFrame;

        // Reused when listing the player's items, to avoid allocating per rebuild.
        private readonly List<ItemDefinition> _sellItems = new List<ItemDefinition>();
        private readonly Dictionary<string, int> _sellCounts = new Dictionary<string, int>();

        public bool IsOpen => _shop != null;

        private void Awake()
        {
            Build();
            _window.SetActive(false);
        }

        private void OnEnable() => Shopkeeper.OnOpenRequested += Open;

        private void OnDisable()
        {
            Shopkeeper.OnOpenRequested -= Open;
            if (IsOpen) Close();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Time.timeScale == 0f) return; // paused

            Keyboard kb = Keyboard.current;
            bool closePressed = kb != null && Time.frameCount != _openedFrame &&
                                (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame);
            bool walkedAway = _customer == null ||
                              Vector3.Distance(_customer.transform.position, _shop.transform.position) > _shop.TradeRange;
            bool cursorTaken = Cursor.lockState == CursorLockMode.Locked; // e.g. the pause menu resumed

            if (closePressed || walkedAway || cursorTaken) Close();
        }

        public void Open(Shopkeeper shop, GameObject customer)
        {
            if (IsOpen) Close();

            _shop = shop;
            _customer = customer;
            _wallet = customer.GetComponentInChildren<Wallet>();
            _inventories = customer.GetComponentsInChildren<InventorySystem>();
            _openedFrame = Time.frameCount;

            _shop.OnStockChanged += Refresh;
            if (_wallet != null) _wallet.OnChanged += HandleMoneyChanged;

            _title.text = shop.ShopName;
            _message.text = string.Empty;
            _window.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        public void Close()
        {
            if (_shop != null) _shop.OnStockChanged -= Refresh;
            if (_wallet != null) _wallet.OnChanged -= HandleMoneyChanged;
            _shop = null;
            _customer = null;
            _wallet = null;

            _window.SetActive(false);
            if (Time.timeScale > 0f) // leave the cursor alone if the pause menu owns it
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleMoneyChanged(int amount) => RefreshMoney();

        private void RefreshMoney()
        {
            _money.text = _wallet != null ? $"{_wallet.CurrencyName}: {_wallet.Amount}" : string.Empty;
        }

        private void Refresh()
        {
            if (!IsOpen) return;
            RefreshMoney();
            ClearChildren(_buyList);
            ClearChildren(_sellList);

            int money = _wallet != null ? _wallet.Amount : 0;
            string currency = _wallet != null ? _wallet.CurrencyName : string.Empty;

            IReadOnlyList<Shopkeeper.StockEntry> stock = _shop.Stock;
            for (int i = 0; i < stock.Count; i++)
            {
                int index = i;
                int price = _shop.BuyPrice(stock[i].Item);
                CreateRow(_buyList, $"{stock[i].Item.DisplayName} x{stock[i].Count}", $"{price} {currency}",
                          price <= money, () => Buy(index));
            }
            if (stock.Count == 0) CreateRow(_buyList, "Sold out", string.Empty, false, null);

            CollectSellableItems();
            foreach (ItemDefinition item in _sellItems)
            {
                ItemDefinition captured = item;
                bool sellable = _shop.CanBuyFromPlayer(item);
                string label = _sellCounts[item.Id] > 1 ? $"{item.DisplayName} x{_sellCounts[item.Id]}" : item.DisplayName;
                CreateRow(_sellList, label, sellable ? $"{_shop.SellPrice(item)} {currency}" : "Quest item",
                          sellable, () => Sell(captured));
            }
            if (_sellItems.Count == 0) CreateRow(_sellList, "Nothing to sell", string.Empty, false, null);
        }

        private void Buy(int index)
        {
            if (!IsOpen) return;
            _shop.TryBuy(index, _customer, out string message);
            _message.text = message;
        }

        private void Sell(ItemDefinition item)
        {
            if (!IsOpen) return;
            _shop.TrySell(item, _customer, out string message);
            _message.text = message;
        }

        private void CollectSellableItems()
        {
            _sellItems.Clear();
            _sellCounts.Clear();
            foreach (InventorySystem inventory in _inventories)
            {
                if (inventory == null) continue;
                foreach (ItemStack stack in inventory.Slots)
                {
                    if (stack == null || stack.IsEmpty) continue;
                    if (_sellCounts.TryGetValue(stack.Item.Id, out int count))
                    {
                        _sellCounts[stack.Item.Id] = count + stack.Quantity;
                    }
                    else
                    {
                        _sellCounts.Add(stack.Item.Id, stack.Quantity);
                        _sellItems.Add(stack.Item);
                    }
                }
            }
        }

        // --- UI construction ---

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = _sortingOrder;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Border, then the window inset by 3 px.
            RectTransform border = CreateImage("Border", transform, _borderColor);
            border.anchorMin = border.anchorMax = new Vector2(0.5f, 0.5f);
            border.sizeDelta = new Vector2(880f, 540f);
            _window = border.gameObject;

            RectTransform window = CreateImage("Window", border, _windowColor);
            Stretch(window, 3f);

            _title = CreateText("Title", window, 30f, HorizontalAlignmentOptions.Left);
            Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -60f), new Vector2(-24f, -14f));
            _money = CreateText("Money", window, 24f, HorizontalAlignmentOptions.Right);
            Place(_money.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -60f), new Vector2(-24f, -14f));

            _buyList = CreateColumn("Buy", window, 0f, 0.5f);
            _sellList = CreateColumn("Sell", window, 0.5f, 1f);

            _message = CreateText("Message", window, 20f, HorizontalAlignmentOptions.Left);
            Place(_message.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 14f), new Vector2(-200f, 54f));

            RectTransform leave = CreateRow(window, "Leave", string.Empty, true, Close);
            Place(leave, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 14f), new Vector2(-24f, 54f));
        }

        private RectTransform CreateColumn(string header, RectTransform window, float xMin, float xMax)
        {
            TextMeshProUGUI label = CreateText(header + "Header", window, 24f, HorizontalAlignmentOptions.Left);
            label.text = header;
            label.color = _borderColor;
            Place(label.rectTransform, new Vector2(xMin, 1f), new Vector2(xMax, 1f), new Vector2(24f, -100f), new Vector2(-12f, -66f));

            RectTransform list = new GameObject(header + "List", typeof(RectTransform)).GetComponent<RectTransform>();
            list.SetParent(window, false);
            Place(list, new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(24f, 66f), new Vector2(-12f, -106f));

            VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return list;
        }

        private RectTransform CreateRow(Transform parent, string left, string right, bool interactable, UnityAction onClick)
        {
            RectTransform row = CreateImage("Row", parent, _rowColor);
            row.sizeDelta = new Vector2(0f, RowHeight);

            Button button = row.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.35f, 1.25f, 1.1f, 1f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.colors = colors;
            button.interactable = interactable && onClick != null;
            if (onClick != null) button.onClick.AddListener(onClick);

            Color color = button.interactable ? _textColor : _disabledTextColor;

            TextMeshProUGUI leftText = CreateText("Name", row, 20f, HorizontalAlignmentOptions.Left);
            leftText.text = left;
            leftText.color = color;
            Stretch(leftText.rectTransform, 0f);
            leftText.margin = new Vector4(10f, 0f, 10f, 0f);

            TextMeshProUGUI rightText = CreateText("Price", row, 20f, HorizontalAlignmentOptions.Right);
            rightText.text = right;
            rightText.color = color;
            Stretch(rightText.rectTransform, 0f);
            rightText.margin = new Vector4(10f, 0f, 10f, 0f);
            return row;
        }

        private RectTransform CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateText(string objectName, Transform parent, float size, HorizontalAlignmentOptions alignment)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = size;
            text.horizontalAlignment = alignment;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.color = _textColor;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            Place(rect, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
