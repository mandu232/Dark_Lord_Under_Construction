using DarkLord.Minions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    public sealed class MinionCommands : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private MinionController[] minions = new MinionController[0];
        [SerializeField] private LayerMask obstacleLayers;
        private InputAction defend, rally, returnToPost;
        private MinionSummoning summoning;

        public void Register(MinionController minion)
        {
            var alive = new System.Collections.Generic.List<MinionController>();
            foreach (var existing in minions) if (existing != null && existing.IsAlive && existing != minion) alive.Add(existing);
            if (minion != null) alive.Add(minion);
            minions = alive.ToArray();
        }

        private void Awake()
        {
            summoning = GetComponent<MinionSummoning>();
            defend = new InputAction("Defend", InputActionType.Button, "<Mouse>/rightButton");
            rally = new InputAction("Rally", InputActionType.Button, "<Keyboard>/q");
            returnToPost = new InputAction("Return", InputActionType.Button, "<Keyboard>/e");
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void OnEnable() { defend.Enable(); rally.Enable(); returnToPost.Enable(); }
        private void OnDisable() { defend.Disable(); rally.Disable(); returnToPost.Disable(); }
        private void OnDestroy() { defend.Dispose(); rally.Dispose(); returnToPost.Dispose(); }

        private void Update()
        {
            if (Time.timeScale == 0 || (summoning != null && summoning.BlocksGameplayInput)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (rally.WasPressedThisFrame())
                for (int i = 0; i < minions.Length; i++)
                    if (minions[i] != null) minions[i].Rally(transform,
                        minions.Length == 1 ? Vector2.down * 0.8f : FormationOffset(i));
            if (returnToPost.WasPressedThisFrame())
                foreach (var minion in minions) if (minion != null) minion.ReturnToPost();
            if (!defend.WasPressedThisFrame() || Mouse.current == null || worldCamera == null) return;
            Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float distance)) return;
            Vector2 point = ray.GetPoint(distance);
            for (int i = 0; i < minions.Length; i++)
            {
                Vector2 destination = point + FormationOffset(i);
                if (minions[i] != null && !Physics2D.OverlapCircle(destination, 0.25f, obstacleLayers))
                    minions[i].Defend(destination);
            }
        }

        private Vector2 FormationOffset(int index)
        {
            if (minions.Length <= 1) return Vector2.zero;
            float angle = index * Mathf.PI * 2 / minions.Length;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.8f;
        }
    }
}
