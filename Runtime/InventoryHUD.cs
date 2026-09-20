using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Retro.Inventory
{
    /// <summary>
    /// Complete retro 90s HUD for inventory, interaction prompt, hotbar, and document inspection.
    /// Works automatically with the New Input System and FirstPersonController.
    /// </summary>
    public class InventoryHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventorySystem _inventory;
        [SerializeField] private PlayerInteraction _playerInteraction;

        [Header("UI Elements - Prompt & Notification")]
        [SerializeField] private TextMeshProUGUI _promptText;
        [SerializeField] private TextMeshProUGUI _notificationText;

        [Header("UI Elements - Hotbar")]
        [SerializeField] private RectTransform _hotbarContainer;
        [SerializeField] private List<HotbarSlotUI> _hotbarSlots = new List<HotbarSlotUI>();

        [Header("UI Elements - Document Reader")]
        [SerializeField] private GameObject _documentReaderPanel;
        [SerializeField] private TextMeshProUGUI _documentTitleText;
        [SerializeField] private TextMeshProUGUI _documentBodyText;

        [Header("UI Elements - Bag Panel")]
        [SerializeField] private GameObject _bagPanel;
        [SerializeField] private TextMeshProUGUI _bagItemTitle;
        [SerializeField] private TextMeshProUGUI _bagItemCategory;
        [SerializeField] private TextMeshProUGUI _bagItemDescription;

        [Header("Hold To Use")]
        [Tooltip("Require holding the Use key (F/Q) instead of a single tap.")]
        [SerializeField] private bool _holdToUse = true;

        [Tooltip("Seconds the Use key must be held before the item is used.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _useHoldDuration = 0.75f;

        [Tooltip("Optional radial Image (Image Type = Filled, Fill Method = Radial 360) that fills while holding.")]
        [SerializeField] private Image _holdProgressRing;

        [Header("Notifications")]
        [Tooltip("How long notification messages stay on screen.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _notificationDuration = 2.8f;

        private float _notificationTimer = 0f;
        private bool _isDocumentOpen = false;
        private bool _isBagOpen = false;

        private float _useHoldTimer = 0f;
        private bool _useHoldLatched = false;

        public bool IsAnyModalOpen => _isDocumentOpen || _isBagOpen;

        /// <summary>0-1 progress of the current Use hold. 0 when not holding.</summary>
        public float UseHoldProgress =>
            (_holdToUse && _useHoldDuration > 0f && !_useHoldLatched)
                ? Mathf.Clamp01(_useHoldTimer / _useHoldDuration)
                : 0f;

        private void Awake()
        {
            FindReferencesIfMissing();
        }

        private void OnEnable()
        {
            ReadDocumentAction.OnReadDocumentRequested += OpenDocumentReader;

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += RefreshHUD;
                _inventory.OnSlotSelected += HandleSlotSelected;
                _inventory.OnNotification += ShowNotification;
            }
        }

        private void OnDisable()
        {
            ReadDocumentAction.OnReadDocumentRequested -= OpenDocumentReader;

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= RefreshHUD;
                _inventory.OnSlotSelected -= HandleSlotSelected;
                _inventory.OnNotification -= ShowNotification;
            }
        }

        private void Start()
        {
            FindReferencesIfMissing();
            RefreshHUD();
            if (_inventory != null)
            {
                HandleSlotSelected(_inventory.SelectedSlotIndex, _inventory.SelectedStack);
            }
        }

        private void Update()
        {
            UpdateInteractionPrompt();
            UpdateNotification();
            HandleInput();
            UpdateHoldRing();
        }

        private void FindReferencesIfMissing()
        {
            if (_inventory == null)
            {
                _inventory = Object.FindAnyObjectByType<InventorySystem>();
            }
            if (_playerInteraction == null)
            {
                _playerInteraction = Object.FindAnyObjectByType<PlayerInteraction>();
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (_promptText == null) return;

            if (_isDocumentOpen || _isBagOpen)
            {
                _promptText.gameObject.SetActive(false);
                return;
            }

            if (_playerInteraction != null && !string.IsNullOrEmpty(_playerInteraction.CurrentPrompt))
            {
                _promptText.text = _playerInteraction.CurrentPrompt;
                if (!_promptText.gameObject.activeSelf)
                {
                    _promptText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (_promptText.gameObject.activeSelf)
                {
                    _promptText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateNotification()
        {
            if (_notificationText == null) return;

            if (_notificationTimer > 0f)
            {
                _notificationTimer -= Time.deltaTime;
                if (_notificationTimer <= 0f)
                {
                    _notificationText.gameObject.SetActive(false);
                }
            }
        }

        public void ShowNotification(string message)
        {
            if (_notificationText == null) return;
            _notificationText.text = message;
            _notificationText.gameObject.SetActive(true);
            _notificationTimer = _notificationDuration;
        }

        private void HandleInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // 1. Close modals with Escape
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (_isDocumentOpen)
                {
                    CloseDocumentReader();
                    return;
                }
                if (_isBagOpen)
                {
                    ToggleBag();
                    return;
                }
            }

            // If reading document, E or Space also closes it
            if (_isDocumentOpen)
            {
                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    CloseDocumentReader();
                }
                return;
            }

            // 2. Toggle Inventory Bag with I or Tab
            if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            {
                ToggleBag();
                return;
            }

            if (_inventory == null) return;

            // 3. Hotbar slot selection 1-8
            if (kb.digit1Key.wasPressedThisFrame) _inventory.SelectSlot(0);
            else if (kb.digit2Key.wasPressedThisFrame) _inventory.SelectSlot(1);
            else if (kb.digit3Key.wasPressedThisFrame) _inventory.SelectSlot(2);
            else if (kb.digit4Key.wasPressedThisFrame) _inventory.SelectSlot(3);
            else if (kb.digit5Key.wasPressedThisFrame) _inventory.SelectSlot(4);
            else if (kb.digit6Key.wasPressedThisFrame) _inventory.SelectSlot(5);
            else if (kb.digit7Key.wasPressedThisFrame) _inventory.SelectSlot(6);
            else if (kb.digit8Key.wasPressedThisFrame) _inventory.SelectSlot(7);

            // 4. Mouse Scroll wheel to cycle hotbar
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f) _inventory.CycleSlot(-1);
                else if (scroll < -0.1f) _inventory.CycleSlot(1);
            }

            // 5. Use item with F or Q (tap or hold, configurable)
            HandleUseInput(kb);

            // 6. Drop item with G
            if (kb.gKey.wasPressedThisFrame)
            {
                _inventory.DropSlot(_inventory.SelectedSlotIndex);
            }
        }

        private void HandleUseInput(Keyboard kb)
        {
            if (!_holdToUse)
            {
                if (kb.fKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame)
                {
                    _inventory.UseSelectedItem();
                }
                return;
            }

            bool held = kb.fKey.isPressed || kb.qKey.isPressed;

            if (!held || !HasUsableSelection())
            {
                _useHoldTimer = 0f;
                if (!held) _useHoldLatched = false;   // released: next press starts fresh
                return;
            }

            if (_useHoldLatched) return;              // already fired for this press

            _useHoldTimer += Time.deltaTime;
            if (_useHoldTimer >= _useHoldDuration)
            {
                _inventory.UseSelectedItem();
                _useHoldLatched = true;
                _useHoldTimer = 0f;
            }
        }

        private bool HasUsableSelection()
        {
            var stack = _inventory != null ? _inventory.SelectedStack : null;
            return stack != null && !stack.IsEmpty;
        }

        private void UpdateHoldRing()
        {
            if (IsAnyModalOpen) _useHoldTimer = 0f;
            if (_holdProgressRing == null) return;

            float progress = UseHoldProgress;
            bool show = progress > 0.001f;

            if (_holdProgressRing.gameObject.activeSelf != show)
            {
                _holdProgressRing.gameObject.SetActive(show);
            }
            if (show)
            {
                _holdProgressRing.fillAmount = progress;
            }
        }

        public void RefreshHUD()
        {
            if (_inventory == null) return;

            for (int i = 0; i < _hotbarSlots.Count; i++)
            {
                if (i < _inventory.Slots.Count)
                {
                    _hotbarSlots[i].Bind(i, _inventory.Slots[i]);
                }
                else
                {
                    _hotbarSlots[i].Clear();
                }
            }

            UpdateBagSelectionDetails();
        }

        private void HandleSlotSelected(int index, ItemStack stack)
        {
            for (int i = 0; i < _hotbarSlots.Count; i++)
            {
                _hotbarSlots[i].SetSelected(i == index);
            }

            UpdateBagSelectionDetails();
        }

        private void UpdateBagSelectionDetails()
        {
            if (_bagPanel == null || !_bagPanel.activeSelf || _inventory == null) return;

            var stack = _inventory.SelectedStack;
            if (stack != null && !stack.IsEmpty && stack.Item != null)
            {
                if (_bagItemTitle != null) _bagItemTitle.text = stack.Item.DisplayName;
                if (_bagItemCategory != null) _bagItemCategory.text = $"[{stack.Item.Category.ToString().ToUpper()}]";
                if (_bagItemDescription != null) _bagItemDescription.text = stack.Item.Description;
            }
            else
            {
                if (_bagItemTitle != null) _bagItemTitle.text = "Empty Slot";
                if (_bagItemCategory != null) _bagItemCategory.text = "[EMPTY]";
                if (_bagItemDescription != null) _bagItemDescription.text = "Select an item to view details.";
            }
        }

        public void ToggleBag()
        {
            if (_bagPanel == null) return;

            _isBagOpen = !_isBagOpen;
            _bagPanel.SetActive(_isBagOpen);

            UpdateCursorState();
            UpdateBagSelectionDetails();
        }

        public void OpenDocumentReader(string title, string content)
        {
            if (_documentReaderPanel == null) return;

            _isDocumentOpen = true;
            _documentReaderPanel.SetActive(true);

            if (_documentTitleText != null) _documentTitleText.text = title;
            if (_documentBodyText != null) _documentBodyText.text = content;

            UpdateCursorState();
        }

        public void CloseDocumentReader()
        {
            if (_documentReaderPanel == null) return;

            _isDocumentOpen = false;
            _documentReaderPanel.SetActive(false);

            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            bool modalActive = _isDocumentOpen || _isBagOpen;
            Cursor.lockState = modalActive ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = modalActive;
        }

        // UI Button callbacks
        public void OnClickUseSelected()
        {
            if (_inventory != null)
            {
                _inventory.UseSelectedItem();
            }
        }

        public void OnClickDropSelected()
        {
            if (_inventory != null)
            {
                _inventory.DropSlot(_inventory.SelectedSlotIndex);
            }
        }

        public void SetReferences(InventorySystem inv, PlayerInteraction pi, TextMeshProUGUI prompt, TextMeshProUGUI notification)
        {
            _inventory = inv;
            _playerInteraction = pi;
            _promptText = prompt;
            _notificationText = notification;
        }

        public void AddHotbarSlot(HotbarSlotUI slot)
        {
            _hotbarSlots.Add(slot);
        }

        public void SetDocumentReaderElements(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI body)
        {
            _documentReaderPanel = panel;
            _documentTitleText = title;
            _documentBodyText = body;
        }

        public void SetBagElements(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI category, TextMeshProUGUI desc)
        {
            _bagPanel = panel;
            _bagItemTitle = title;
            _bagItemCategory = category;
            _bagItemDescription = desc;
        }
    }
}
