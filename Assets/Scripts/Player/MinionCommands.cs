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
        private InputAction returnToPost;
        private MinionSummoning summoning;
        private DarkLord.Dungeon.DungeonExpansion construction;

        public void Register(MinionController minion)
        {
            var alive = new System.Collections.Generic.List<MinionController>();
            foreach (var existing in minions) if (existing != null && existing.IsAlive && existing != minion) alive.Add(existing);
            if (minion != null) alive.Add(minion);
            minions = alive.ToArray();
        }
        public void Unregister(MinionController minion)
        {
            minions = System.Array.FindAll(minions, existing => existing != null && existing != minion);
        }

        private void Awake()
        {
            summoning = GetComponent<MinionSummoning>();
            construction = GetComponent<DarkLord.Dungeon.DungeonExpansion>();
            returnToPost = new InputAction("Return", InputActionType.Button, "<Keyboard>/e");
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void OnEnable() { returnToPost.Enable(); }
        private void OnDisable() { returnToPost.Disable(); }
        private void OnDestroy() { returnToPost.Dispose(); }

        private void Update()
        {
            if (Time.timeScale == 0 || (summoning != null && summoning.BlocksGameplayInput) ||
                (construction != null && construction.BlocksGameplayInput)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (returnToPost.WasPressedThisFrame())
                foreach (var minion in minions) if (minion != null) minion.ReturnToPost();
        }

    }
}
