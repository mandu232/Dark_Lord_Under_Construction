using UnityEngine;
namespace DarkLord.Gameplay
{
    // A tiny pixel character keeps the visiting merchant independent of combat actors and colliders.
    public sealed class GoblinMerchantVisual : MonoBehaviour
    {
        private Texture2D texture;
        private Sprite sprite;
        private void Awake()
        {
            string[] pixels = {
                "......HHHH......", ".....HHHHHH.....", "....HHHHHHHH....", "...HHHHHHHHHH...",
                "..GGHHHHHHHHGG..", ".GGGGGGGGGGGGGG.", "..GGWGGGGWGGGG..", "...GKGGGGKGGG...",
                "....GGGGGGGG....", ".....GTTTTG.....", ".....BBBBBB.....", "....BBBBBBBB....",
                "...GBBBBBBBBG...", "..GGBBBYYBBBGG..", "..GG..BYYB..GG..", "......BBBBYY....",
                ".....BBBBYYYY...", ".....BBBBYKKY...", ".....BB..YYYY...", ".....KK..KK....." };
            texture = new Texture2D(16, 20, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 20; y++) for (int x = 0; x < 16; x++)
            {
                char c = pixels[y][x];
                Color color = c == 'H' ? new Color(.43f,.16f,.32f) : c == 'G' ? new Color(.37f,.72f,.27f) : c == 'W' ? new Color(1,.9f,.55f) :
                    c == 'K' ? new Color(.09f,.09f,.1f) : c == 'T' ? new Color(.84f,.65f,.4f) : c == 'B' ? new Color(.31f,.19f,.12f) : c == 'Y' ? new Color(.96f,.69f,.2f) : Color.clear;
                texture.SetPixel(x, 19-y, color);
            }
            texture.Apply(); sprite = Sprite.Create(texture, new Rect(0,0,16,20), new Vector2(.5f,.15f), 12);
            var renderer = gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 100;
        }
        private void OnDestroy() { if (sprite != null) Destroy(sprite); if (texture != null) Destroy(texture); }
    }
}
