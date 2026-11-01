using DarkLord.Actors;
using UnityEngine;
namespace DarkLord.Gameplay
{
    // Nibble is a visual visitor, independent of combat actors and colliders.
    public sealed class GoblinMerchantVisual : MonoBehaviour
    {
        private void Awake()
        {
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Characters/NibbleMerchant");
            gameObject.AddComponent<ActorDepthSorting>();
        }
    }
}
