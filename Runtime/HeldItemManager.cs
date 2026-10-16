using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Manages the currently held 3D item in the player's view/hand socket,
    /// as well as dynamic light sources (Flashlight and Lantern).
    /// </summary>
    public class HeldItemManager : MonoBehaviour
    {
        [Header("Inventory")]
        [Tooltip("Inventory whose selected slot is shown in hand. If empty, uses the InventorySystem on this object or its parents.")]
        [SerializeField] private InventorySystem _inventory;

        [Tooltip("Shown in hand when no slot is selected or the selected item has no HeldPrefab (e.g. the primary weapon). Optional.")]
        [SerializeField] private GameObject _defaultHeldPrefab;

        [Header("Socket Configuration")]
        [Tooltip("Transform socket under the camera where held items are attached.")]
        [SerializeField] private Transform _heldSocket;

        [Header("Light Sources")]
        [Tooltip("Spotlight used for the Flashlight tool.")]
        [SerializeField] private Light _flashlightLight;

        [Tooltip("Point light used for the Lantern.")]
        [SerializeField] private Light _lanternLight;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _defaultToggleSound;

        private GameObject _currentHeldInstance;

        public bool IsFlashlightOn => _flashlightLight != null && _flashlightLight.enabled;
        public bool IsLanternOn => _lanternLight != null && _lanternLight.enabled;

        private void Awake()
        {
            if (_inventory == null)
            {
                _inventory = GetComponentInParent<InventorySystem>();
            }

            EnsureSocketAndLights();
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.OnSlotSelected += HandleSlotSelected;
            }
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.OnSlotSelected -= HandleSlotSelected;
            }
        }

        private void EnsureSocketAndLights()
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null && Camera.main != null)
            {
                cam = Camera.main;
            }

            Transform parentTransform = cam != null ? cam.transform : transform;

            // 1. Ensure Held Socket
            if (_heldSocket == null)
            {
                Transform existingSocket = parentTransform.Find("HeldItemSocket");
                if (existingSocket != null)
                {
                    _heldSocket = existingSocket;
                }
                else
                {
                    GameObject socketObj = new GameObject("HeldItemSocket");
                    socketObj.transform.SetParent(parentTransform, false);
                    socketObj.transform.localPosition = new Vector3(0.35f, -0.28f, 0.65f);
                    socketObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    _heldSocket = socketObj.transform;
                }
            }

            // 2. Ensure Flashlight Spotlight
            if (_flashlightLight == null)
            {
                Transform existingLight = parentTransform.Find("FlashlightSpot");
                if (existingLight != null)
                {
                    _flashlightLight = existingLight.GetComponent<Light>();
                }
                else
                {
                    GameObject lightObj = new GameObject("FlashlightSpot");
                    lightObj.transform.SetParent(parentTransform, false);
                    lightObj.transform.localPosition = new Vector3(0.2f, -0.1f, 0.2f);
                    lightObj.transform.localRotation = Quaternion.identity;

                    Light light = lightObj.AddComponent<Light>();
                    light.type = LightType.Spot;
                    light.spotAngle = 55f;
                    light.innerSpotAngle = 35f;
                    light.range = 25f;
                    light.intensity = 2.0f;
                    light.color = new Color(0.92f, 0.95f, 1.0f); // crisp cool white
                    light.enabled = false; // Start off
                    _flashlightLight = light;
                }
            }

            // 3. Ensure Lantern Point Light
            if (_lanternLight == null)
            {
                Transform existingLantern = parentTransform.Find("LanternGlow");
                if (existingLantern != null)
                {
                    _lanternLight = existingLantern.GetComponent<Light>();
                }
                else
                {
                    GameObject lanternObj = new GameObject("LanternGlow");
                    lanternObj.transform.SetParent(parentTransform, false);
                    lanternObj.transform.localPosition = new Vector3(0f, 0f, 0.3f);

                    Light light = lanternObj.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 14f;
                    light.intensity = 2.2f;
                    light.color = new Color(1.0f, 0.78f, 0.45f); // warm amber glow
                    light.enabled = false; // Start off
                    _lanternLight = light;
                }
            }
        }

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            _audioSource.playOnAwake = false;
        }

        private void HandleSlotSelected(int index, ItemStack stack)
        {
            // Clear previous held visual
            if (_currentHeldInstance != null)
            {
                Destroy(_currentHeldInstance);
                _currentHeldInstance = null;
            }

            bool hasItem = stack != null && !stack.IsEmpty && stack.Item != null;
            GameObject prefab = hasItem && stack.Item.HeldPrefab != null ? stack.Item.HeldPrefab : _defaultHeldPrefab;

            // Instantiate 3D held visual if configured
            if (prefab != null && _heldSocket != null)
            {
                _currentHeldInstance = Instantiate(prefab, _heldSocket);
                _currentHeldInstance.transform.localPosition = Vector3.zero;
                _currentHeldInstance.transform.localRotation = Quaternion.identity;

                // Strip physics colliders on held viewmodel so it doesn't block player
                Collider[] colliders = _currentHeldInstance.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    Destroy(col);
                }
            }
        }

        /// <summary>
        /// Changes what is held when nothing is selected (e.g. swapping the primary weapon) and refreshes the hand.
        /// </summary>
        public void SetDefaultHeldPrefab(GameObject prefab)
        {
            _defaultHeldPrefab = prefab;
            if (_inventory != null) HandleSlotSelected(_inventory.SelectedSlotIndex, _inventory.SelectedStack);
        }

        /// <summary>
        /// Toggles the specified light source on or off.
        /// </summary>
        public bool ToggleLight(LightTypeToToggle type, AudioClip sound = null)
        {
            Light targetLight = type == LightTypeToToggle.Flashlight ? _flashlightLight : _lanternLight;
            if (targetLight == null) return false;

            targetLight.enabled = !targetLight.enabled;

            AudioClip clip = sound != null ? sound : _defaultToggleSound;
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }

            return targetLight.enabled;
        }

        public void SetFlashlight(bool on)
        {
            if (_flashlightLight != null) _flashlightLight.enabled = on;
        }

        public void SetLantern(bool on)
        {
            if (_lanternLight != null) _lanternLight.enabled = on;
        }
    }
}
