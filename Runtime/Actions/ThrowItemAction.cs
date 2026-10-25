using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Item use action that throws a projectile prefab from the camera along the view direction.
    /// The prefab owns what happens next (fuse, impact, explosion), so the inventory knows nothing about combat.
    /// Mark the item consumable so each throw removes one from the stack.
    /// </summary>
    [CreateAssetMenu(fileName = "Action_Throw", menuName = "Retro/Inventory/Actions/Throw Item Action")]
    public class ThrowItemAction : ItemUseAction
    {
        [Tooltip("Spawned and launched on use. Needs a Rigidbody.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Launch speed in m/s.")]
        [SerializeField, Min(0f)] private float _speed = 14f;

        [Tooltip("Extra upward aim added to the view direction, for a lob.")]
        [SerializeField, Range(0f, 1f)] private float _upwardArc = 0.15f;

        [Tooltip("Spawn offset from the camera (right, up, forward), in metres.")]
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0.2f, -0.15f, 0.5f);

        [Tooltip("Random tumble added on throw (rad/s).")]
        [SerializeField, Min(0f)] private float _spin = 6f;

        public override bool Execute(GameObject user, ItemDefinition item)
        {
            if (user == null || _projectilePrefab == null) return false;

            Camera cam = Camera.main;
            Transform aim = cam != null ? cam.transform : user.transform;

            Vector3 spawnPos = aim.TransformPoint(_spawnOffset);
            GameObject projectile = Instantiate(_projectilePrefab, spawnPos, aim.rotation);

            IgnoreUserCollisions(user, projectile);

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                Vector3 velocity = (aim.forward + Vector3.up * _upwardArc).normalized * _speed;
                CharacterController cc = user.GetComponent<CharacterController>();
                if (cc != null) velocity += cc.velocity; // inherit running speed
                rb.linearVelocity = velocity;
                rb.angularVelocity = Random.insideUnitSphere * _spin;
            }

            return true;
        }

        private static void IgnoreUserCollisions(GameObject user, GameObject projectile)
        {
            Collider[] userColliders = user.GetComponentsInChildren<Collider>();
            Collider[] projColliders = projectile.GetComponentsInChildren<Collider>();
            foreach (Collider p in projColliders)
            foreach (Collider u in userColliders)
                Physics.IgnoreCollision(p, u);
        }

        public override string GetFailureReason() => "Nothing to throw.";
    }
}
