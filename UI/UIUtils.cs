using ColossalFramework.UI;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TreeAnarchy.UI {
    public static class UIUtils {
        internal static UITextureAtlas CreateTextureAtlas(string atlasName, string[] spriteNames) {
            const int spriteMaxSize = 1024;
            Texture2D texture2D = new Texture2D(spriteMaxSize, spriteMaxSize, TextureFormat.ARGB32, false);
            Texture2D[] textures = new Texture2D[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++) {
                textures[i] = LoadTextureFromAssembly(@"TreeAnarchy.Resources." + spriteNames[i] + @".png");
                textures[i].name = spriteNames[i];
            }
            Rect[] regions = texture2D.PackTextures(textures, 2, spriteMaxSize);
            UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Material material = UnityEngine.Object.Instantiate(UIView.GetAView().defaultAtlas.material);
            material.mainTexture = texture2D;
            uITextureAtlas.material = material;
            uITextureAtlas.name = atlasName;
            for (int j = 0; j < spriteNames.Length; j++) {
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo() {
                    name = spriteNames[j],
                    texture = textures[j],
                    region = regions[j]
                };
                uITextureAtlas.AddSprite(item);
            }
            return uITextureAtlas;
        }

        private static Texture2D LoadTextureFromAssembly(string filename) {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
            byte[] array = new byte[s.Length];
            s.Read(array, 0, array.Length);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(array);
            return texture;
        }
    }
}
