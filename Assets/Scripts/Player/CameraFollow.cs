using UnityEngine;

namespace DarkLord.Player
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        private void LateUpdate()
        {
            if (target != null)
                transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }
}
