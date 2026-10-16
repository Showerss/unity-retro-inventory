using UnityEngine;
using UnityEngine.InputSystem;

namespace Retro.Inventory
{
    /// <summary>
    /// Placeholder swing for a 3D held melee weapon: left-click rotates the model through a quick slash.
    /// Lives on the held prefab, so it only listens while the weapon is in hand. Visual only, no hit detection.
    /// </summary>
    public sealed class HeldMeleeSwing : MonoBehaviour
    {
        [Tooltip("Seconds for a full swing: wind-up, slash and recovery.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _duration = 0.32f;

        [Tooltip("Local rotation at the end of the wind-up (degrees).")]
        [SerializeField] private Vector3 _windUpEuler = new Vector3(-25f, 0f, -15f);

        [Tooltip("Local rotation at the end of the slash (degrees).")]
        [SerializeField] private Vector3 _slashEuler = new Vector3(55f, -35f, 30f);

        [Tooltip("Optional sound played when the swing starts.")]
        [SerializeField] private AudioClip _swingSound;

        [Tooltip("Volume of the swing sound.")]
        [Range(0f, 1f)]
        [SerializeField] private float _swingVolume = 0.8f;

        private Quaternion _restRotation;
        private Quaternion _windUp;
        private Quaternion _slash;
        private float _elapsed = -1f;

        private void Awake()
        {
            _restRotation = transform.localRotation;
            _windUp = _restRotation * Quaternion.Euler(_windUpEuler);
            _slash = _restRotation * Quaternion.Euler(_slashEuler);
        }

        private void OnDisable()
        {
            _elapsed = -1f;
            transform.localRotation = _restRotation;
        }

        private void Update()
        {
            if (_elapsed < 0f)
            {
                // Ignore clicks while a menu has freed the cursor.
                var mouse = Mouse.current;
                if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Cursor.lockState != CursorLockMode.Locked) return;

                _elapsed = 0f;
                if (_swingSound != null) AudioSource.PlayClipAtPoint(_swingSound, transform.position, _swingVolume);
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            if (t < 0.25f) transform.localRotation = Quaternion.Slerp(_restRotation, _windUp, t / 0.25f);
            else if (t < 0.65f) transform.localRotation = Quaternion.Slerp(_windUp, _slash, Mathf.Sin((t - 0.25f) / 0.4f * Mathf.PI * 0.5f));
            else transform.localRotation = Quaternion.Slerp(_slash, _restRotation, Mathf.SmoothStep(0f, 1f, (t - 0.65f) / 0.35f));

            if (t >= 1f)
            {
                _elapsed = -1f;
                transform.localRotation = _restRotation;
            }
        }
    }
}
