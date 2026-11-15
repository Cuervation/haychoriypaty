using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Tarjetas de mejoras. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private void Upgrade(Rect r,int action,int icon,string name,int cost,bool affordable)
        {
            // Anchor the card responsively, but keep its internal typography/icons at one scale.
            float previousVertical = layoutVerticalScale;
            r.y *= previousVertical;
            layoutVerticalScale = 1f;
            // On short canvases (e.g. 3:4 tablets) the card footprint shrinks with the vertical scale. Lay the
            // content out at the authored card size and scale it down as a whole, so titles, status and price
            // keep their designed proportions instead of clipping or overlapping.
            Rect hitBounds = r;
            Matrix4x4 savedMatrix = GUI.matrix;
            float cardScale = Mathf.Min(1f, Mathf.Max(.01f, previousVertical));
            if (cardScale < .999f)
            {
                GUI.matrix = savedMatrix * Matrix4x4.TRS(new Vector3(r.x, r.y, 0f), Quaternion.identity,
                    new Vector3(cardScale, cardScale, 1f));
                r = new Rect(0f, 0f, r.width / cardScale, r.height / cardScale);
            }
            bool available=affordable&&game.Sim.Phase==RoundPhase.Playing;
            Color old=GUI.color;GUI.color=available?Color.white:new Color(.57f,.57f,.57f,1);
            Rect drawn=r;if(lastAction==action&&Time.unscaledTime<feedbackUntil){drawn.width*=feedbackScale;drawn.height*=feedbackScale;drawn.center=r.center;}
            Texture2D glossyCard = GetGlossyUpgradeCardTexture(r);
            if (glossyCard != null) GUI.DrawTexture(drawn, glossyCard, ScaleMode.StretchToFill, true);
            else FillRect(drawn, new Color(.025f, .14f, .40f, 1f));
            GUI.color=old;
            if (available) DrawAffordableUpgradeBorder(r);
            bool compact = r.width < 200;
            int titleSize = upgradeTitle.fontSize;
            upgradeTitle.fontSize = r.height < 120 ? (action == HirePremiumAction ? 13 : compact ? 15 : 17)
                : compact ? 16 : 18;
            Rect titleBounds = UpgradeTitleBounds(drawn);
            upgradeTitle.fontSize = FitUpgradeStatusFontSize(upgradeTitle,name,titleBounds.width);
            UpgradeOutline(titleBounds,name,upgradeTitle,new Color(.98f,.99f,1f));
            upgradeTitle.fontSize = titleSize;
            string statusText = action == 3
                ? "×" + game.Sim.WorkRate.ToString("0.00").Replace('.', ',') + "  ·  +10%"
                : "EQUIPO " + game.Sim.WorkerCount(RoleForAction(action));
            if (compact && action == 3) statusText = "×" + game.Sim.WorkRate.ToString("0.00").Replace('.', ',') + "\n+10%";
            Rect statusBounds = action == 3 ? UpgradeSpeedStatusBounds(drawn) : UpgradeStatusBounds(drawn);
            int originalStatusSize = upgradePercent.fontSize;
            if (compact) upgradePercent.fontSize = action == 3 ? 12 : 13;
            upgradePercent.fontSize = FitUpgradeStatusFontSize(upgradePercent, statusText, statusBounds.width);
            UpgradeOutline(statusBounds,statusText,upgradePercent,new Color(.55f,.90f,1f));
            upgradePercent.fontSize = originalStatusSize;
            if(action==3)
            {
                if(speedArrows!=null) DrawUpgradeOvalContent(drawn,speedArrows,SpeedArrowBounds);
            }
            else if((action==HireCocacoleroAction || action==HireFerneteroAction) && CocacoleroArtForLevel(SpecialistUpgradePortraitSourceLevel(game.Sim.LevelIndex))!=null)
            {
                int artLevel = SpecialistUpgradePortraitSourceLevel(game.Sim.LevelIndex);
                Texture2D art = action == HireFerneteroAction ? (UsesMatchedCocacoleroArt(artLevel) ? fernetWorkerLater : fernetWorker) : CocacoleroArtForLevel(artLevel);
                DrawUpgradeOvalContent(drawn,art,CocacoleroPoseForLevel(artLevel,0));
            }
            else if (action == HirePremiumAction && premiumWorker != null) DrawUpgradeOvalContent(drawn,premiumWorker,ParrilleroPoses[0]);
            else if (action == HireMozoAction && mozoWorker != null) DrawUpgradeOvalContent(drawn,mozoWorker,ParrilleroPoses[0]);
            else if (parrilleroIcon != null) DrawUpgradeOvalContent(drawn,parrilleroIcon,new Rect(22,6,1269,1188));
            else if (items != null) DrawUpgradeOvalContent(drawn,items,Items[icon]);
            string priceText=cost>0?cost.ToString():"MAX";
            if(cost>0)Item(UpgradeCoinBounds(drawn),10);
            int originalPriceSize=upgradePrice.fontSize;
            if (compact || r.height < 120) upgradePrice.fontSize = 24;
            upgradePrice.fontSize=FitUpgradePriceFontSize(upgradePrice,priceText,UpgradePriceTextBounds(drawn).width);
            UpgradeOutline(UpgradePriceTextBounds(drawn),priceText,upgradePrice,new Color(.98f,.99f,1f));
            upgradePrice.fontSize=originalPriceSize;
            GUI.matrix = savedMatrix;
            bool prev=GUI.enabled;GUI.enabled=available;
            if(GUI.Button(LayoutRect(hitBounds),"",invisible))NativeAction(action);GUI.enabled=prev;
            layoutVerticalScale = previousVertical;
        }
        private void DrawAffordableUpgradeBorder(Rect card)
        {
            Texture2D border = GetAffordableUpgradeBorderTexture(card);
            if (border == null) return;
            float inset = AffordableUpgradeBorderPaddingPixels(Mathf.RoundToInt(card.width * 2f)) * .5f;
            Color previous = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(card.x - inset, card.y - inset,
                    card.width + inset * 2f, card.height + inset * 2f), border, ScaleMode.StretchToFill, true);
            }
            finally { GUI.color = previous; }
        }
        private Texture2D GetAffordableUpgradeBorderTexture(Rect card)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(card.width * 2f), 128, 1024);
            int height = Mathf.Clamp(Mathf.RoundToInt(card.height * 2f), 96, 768);
            var key = (width, height);
            if (affordableUpgradeBorderTextures.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            Texture2D created = CreateAffordableUpgradeBorderTexture(width, height);
            affordableUpgradeBorderTextures[key] = created;
            return created;
        }
        private static int AffordableUpgradeBorderPaddingPixels(int cardWidth) => cardWidth < 400 ? 10 : 14;
        private static Rect InflateUpgradeBorderRect(Rect rect, float amount) =>
            new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        private static float UpgradeBorderRingCoverage(float x, float y, Rect subject, float outerWidth, float innerWidth, float baseRadius)
        {
            Rect outer = InflateUpgradeBorderRect(subject, outerWidth);
            Rect inner = InflateUpgradeBorderRect(subject, innerWidth);
            float outerRadius = Mathf.Max(1f, baseRadius + outerWidth);
            float innerRadius = Mathf.Max(1f, baseRadius + innerWidth);
            return Mathf.Max(0f, RoundedCoverage(x, y, outer, outerRadius) - RoundedCoverage(x, y, inner, innerRadius));
        }
        private static Texture2D CreateAffordableUpgradeBorderTexture(int cardWidth, int cardHeight, bool keepReadable = false)
        {
            int padding = AffordableUpgradeBorderPaddingPixels(cardWidth);
            int width = cardWidth + padding * 2, height = cardHeight + padding * 2;
            bool compact = cardWidth < 400;
            float outerLine = compact ? 8f : 12f;
            float outerGap = compact ? 5f : 8f;
            float innerLine = compact ? 3f : 6f;
            float innerEdge = compact ? 0f : 3f;
            float radius = Mathf.Min(cardWidth, cardHeight) * .12f;
            var subject = new Rect(padding, padding, cardWidth, cardHeight);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Double white border for affordable upgrade cards " + cardWidth + "x" + cardHeight,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                Color color = Color.clear;
                color = Over(color, new Color(.96f, .99f, 1f, .98f),
                    UpgradeBorderRingCoverage(px, py, subject, outerLine, outerGap, radius));
                color = Over(color, Color.white,
                    UpgradeBorderRingCoverage(px, py, subject, innerLine, innerEdge, radius));
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }
        private Texture2D GetGlossyUpgradeCardTexture(Rect card)
        {
            int width = Mathf.Clamp(Mathf.RoundToInt(card.width * 2f), 128, 1024);
            int height = Mathf.Clamp(Mathf.RoundToInt(card.height * 2f), 96, 768);
            var key = (width, height);
            if (glossyUpgradeCardTextures.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
            Texture2D created = CreateGlossyUpgradeCardTexture(width, height);
            glossyUpgradeCardTextures[key] = created;
            return created;
        }
        private static Texture2D CreateGlossyUpgradeCardTexture(int width, int height, bool keepReadable = false)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Barrio animado cyan-gloss upgrade card with circular medallion " + width + "x" + height,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            float radius = Mathf.Min(width, height) * .12f;
            var shadow = new Rect(2f, 4f, width - 4f, height - 7f);
            var outer = new Rect(1f, 1f, width - 2f, height - 5f);
            var rim = new Rect(3f, 3f, width - 6f, height - 9f);
            var face = new Rect(6f, 6f, width - 12f, height - 15f);
            var priceRim = new Rect(face.x + face.width * .49f, face.y + face.height * .64f,
                face.width * .49f, face.height * .30f);
            var priceFace = new Rect(priceRim.x + 2f, priceRim.y + 2f,
                Mathf.Max(1f, priceRim.width - 4f), Mathf.Max(1f, priceRim.height - 4f));
            float priceRadius = Mathf.Min(priceRim.height * .5f, radius * .52f);
            Vector2 medalCenter = new Vector2(face.x + face.width * .25f, face.center.y);
            float medalOuterRadius = face.height * .35f;
            float medalRimRadius = face.height * .32f;
            float medalInnerRadius = face.height * .285f;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = height - y - .5f;
                float t = Mathf.Clamp01((py - face.y) / Mathf.Max(1f, face.height));
                Color color = new Color(.004f, .015f, .075f, .72f * RoundedCoverage(px, py, shadow, radius));
                color = Over(color, new Color(.008f, .035f, .15f), RoundedCoverage(px, py, outer, radius));
                color = Over(color, Color.Lerp(new Color(.48f, .94f, 1f), new Color(.02f, .27f, .82f), 1f - t),
                    RoundedCoverage(px, py, rim, Mathf.Max(2f, radius - 2f)));
                float faceCoverage = RoundedCoverage(px, py, face, Mathf.Max(2f, radius - 5f));
                Color faceColor = Color.Lerp(new Color(.045f, .42f, .92f), new Color(.012f, .16f, .62f), 1f - t);
                color = Over(color, faceColor, faceCoverage);
                float gloss = .20f * (1f - IntroEase((t - .56f) / .36f));
                color = Over(color, new Color(.76f, .96f, 1f), faceCoverage * gloss);

                float priceOuterCoverage = RoundedCoverage(px, py, priceRim, priceRadius);
                float priceInnerCoverage = RoundedCoverage(px, py, priceFace, Mathf.Max(1f, priceRadius - 2f));
                color = Over(color, new Color(.008f, .055f, .24f, .96f), priceOuterCoverage);
                Color priceColor = Color.Lerp(new Color(.06f, .66f, 1f), new Color(.015f, .28f, .80f), 1f - t);
                color = Over(color, priceColor, priceInnerCoverage);
                color = Over(color, new Color(.64f, .95f, 1f), priceInnerCoverage * .20f *
                    (1f - IntroEase((t - .62f) / .30f)));

                float medalDistance = Vector2.Distance(new Vector2(px, py), medalCenter);
                color = Over(color, new Color(.002f, .025f, .12f, .72f), CircleCoverage(px, py, medalCenter, medalOuterRadius + 4f));
                color = Over(color, new Color(.008f, .045f, .20f), CircleCoverage(px, py, medalCenter, medalOuterRadius));
                color = Over(color, new Color(.32f, .90f, 1f), CircleCoverage(px, py, medalCenter, medalRimRadius));
                float innerCoverage = CircleCoverage(px, py, medalCenter, medalInnerRadius);
                float medalT = Mathf.Clamp01((py - (medalCenter.y - medalInnerRadius)) / (2f * medalInnerRadius));
                Color medalFace = Color.Lerp(new Color(.035f, .35f, .82f), new Color(.008f, .105f, .40f), 1f - medalT);
                color = Over(color, medalFace, innerCoverage);
                float innerEdge = Mathf.Max(0f, innerCoverage - CircleCoverage(px, py, medalCenter, medalInnerRadius - 2.5f));
                color = Over(color, new Color(.82f, .98f, 1f), innerEdge * .9f);
                float glint = Mathf.Clamp01(1f - Mathf.Abs(medalDistance - medalOuterRadius * .73f) / 2.5f)
                    * Mathf.Clamp01((py - medalCenter.y) / Mathf.Max(1f, medalOuterRadius));
                color = Over(color, Color.white, glint * .52f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }
        private static Rect UpgradeOvalBounds(Rect card)
        {
            float diameter = Mathf.Min(card.height * .68f, card.width * .43f);
            return new Rect(card.x + 8f, card.y + (card.height - diameter) * .5f, diameter, diameter);
        }
        private static Rect UpgradeOvalContentBounds(Rect card, Vector2 sourceSize)
        {
            Rect medallion = UpgradeOvalBounds(card);
            float inset = medallion.width * .10f;
            Rect inner = new Rect(medallion.x + inset, medallion.y + inset,
                medallion.width - inset * 2f, medallion.height - inset * 2f);
            float scale = Mathf.Min(inner.width / Mathf.Max(1f, sourceSize.x),
                inner.height / Mathf.Max(1f, sourceSize.y));
            Vector2 size = sourceSize * scale;
            return new Rect(medallion.center - size * .5f, size);
        }
        private void DrawUpgradeOvalContent(Rect card, Texture2D texture, Rect source)
        {
            if (texture != null) Draw(UpgradeOvalContentBounds(card,source.size),texture,source,true,false);
        }
        private static float UpgradeContentLeft(Rect card) => UpgradeOvalBounds(card).xMax + 6f;
        private static Rect UpgradeSpeedStatusBounds(Rect card)
        {
            float x = UpgradeContentLeft(card);
            return new Rect(x, card.y + 33f, Mathf.Max(0f, card.xMax - 8f - x), 28f);
        }
        private static Rect UpgradeTitleBounds(Rect card)
        {
            float x = UpgradeContentLeft(card);
            return new Rect(x, card.y + 5f, Mathf.Max(0f, card.xMax - 8f - x), 30f);
        }
        private static Rect UpgradeStatusBounds(Rect card)
        {
            float x = UpgradeContentLeft(card);
            return new Rect(x, card.y + 36f, Mathf.Max(0f, card.xMax - 8f - x), 23f);
        }
        private static Rect UpgradeCoinBounds(Rect card)
        {
            float x = UpgradeContentLeft(card) + 1f;
            return new Rect(x, card.y + card.height * .69f, 20f, 20f);
        }
        private static Rect UpgradePriceTextBounds(Rect card)
        {
            Rect coin = UpgradeCoinBounds(card);
            float x = coin.xMax + 2f;
            return new Rect(x, card.y + card.height * .65f, Mathf.Max(0f, card.xMax - 8f - x), card.height * .30f);
        }
        private static int FitUpgradePriceFontSize(GUIStyle style,string value,float maxWidth)
        {
            int original=style.fontSize;float needed=CachedCalcSize(style,value).x;
            return needed>maxWidth-2?Mathf.Max(14,Mathf.FloorToInt(original*(maxWidth-2)/needed)):original;
        }
        private static int FitUpgradeStatusFontSize(GUIStyle style,string value,float maxWidth)
        {
            int original=style.fontSize;float needed=CachedCalcSize(style,value).x;
            return needed>maxWidth-2?Mathf.Max(10,Mathf.FloorToInt(original*(maxWidth-2)/needed)):original;
        }
        private void UpgradeOutline(Rect bounds,string value,GUIStyle style,Color fill)
        {
            Color previous=style.normal.textColor;style.normal.textColor=new Color(.005f,.035f,.16f,1);
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(x!=0||y!=0)
                Label(new Rect(bounds.x+x*1.25f,bounds.y+y*1.25f,bounds.width,bounds.height),value,style);
            style.normal.textColor=fill;Label(bounds,value,style);style.normal.textColor=previous;
        }
        private static float CircleCoverage(float x, float y, Vector2 center, float radius) =>
            Mathf.Clamp01(radius + .5f - Vector2.Distance(new Vector2(x, y), center));
    }
}
