using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Cached outfit palette variants; never resize or regenerate original worker poses.</summary>
    public static class StreetWorkerAppearance
    {
        public static Color OutfitColor(Color color, StreetWorkerRole role, float bodyY)
        {
            if (color.a <= .01f || bodyY < .36f || bodyY > .88f) return color;
            if (role == StreetWorkerRole.ParrilleroPremium && color.r > .38f &&
                color.g >= color.r*.78f && color.b >= color.g*.78f &&
                Mathf.Max(color.r,Mathf.Max(color.g,color.b))-Mathf.Min(color.r,Mathf.Min(color.g,color.b)) < .27f)
                return new Color(color.r*.34f, color.g*.60f, color.b*.95f, color.a);
            if (role == StreetWorkerRole.Fernetero && color.r > .20f && color.g < color.r*.35f && color.b < color.r*.30f)
                return new Color(color.r*.16f, color.r*.17f, color.r*.18f, color.a);
            return color;
        }
        public static Texture2D CreateVariant(Texture2D source, StreetWorkerRole role, Rect[] poses, Vector2 authoredSize)
        {
            if (source == null) return null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture staging = RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            var result = new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            try
            {
                Graphics.Blit(source,staging);
                RenderTexture.active = staging;
                result.ReadPixels(new Rect(0,0,source.width,source.height),0,0);
                Color[] pixels = result.GetPixels();
                foreach (Rect original in poses)
                {
                    Rect pose = new Rect(original.x*source.width/authoredSize.x,original.y*source.height/authoredSize.y,
                        original.width*source.width/authoredSize.x,original.height*source.height/authoredSize.y);
                    for (int topY=Mathf.Max(0,Mathf.FloorToInt(pose.y)); topY<Mathf.Min(source.height,Mathf.CeilToInt(pose.yMax)); topY++)
                    for (int x=Mathf.Max(0,Mathf.FloorToInt(pose.x)); x<Mathf.Min(source.width,Mathf.CeilToInt(pose.xMax)); x++)
                    {
                        int i=(source.height-1-topY)*source.width+x;
                        pixels[i]=OutfitColor(pixels[i],role,(topY-pose.y)/pose.height);
                    }
                }
                result.SetPixels(pixels); result.Apply(false,true);
                result.name=source.name+"-"+role; result.filterMode=source.filterMode; result.wrapMode=TextureWrapMode.Clamp;
                return result;
            }
            finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(staging); }
        }
    }
}
