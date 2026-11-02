using UnityEngine;

namespace DarkLord.UI
{
    public sealed class LoadingScreenSkin : ScriptableObject
    {
        public Sprite[] walkRightFrames;
        public float framesPerSecond = 14;
        public Material silhouetteMaterial;
    }
}
