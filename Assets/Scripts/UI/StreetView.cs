using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HayChoriYPaty
{
    /// <summary>Sprite-backed portrait renderer of actual worker/order state; never simulates deliveries.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StreetGame))]
    public sealed class StreetView : MonoBehaviour
    {
        private const float W = 540, H = 960;
        private static readonly Rect Start = new Rect(161, 555, 218, 61);
        private static readonly Rect Speed = new Rect(84, 680, 174, 179);
        private static readonly Rect HireParrillero = new Rect(282, 680, 174, 179);
        private static readonly Rect Slider = new Rect(87, 484, 366, 36);
        private static readonly Rect Again = new Rect(101, 536, 338, 56);
        private static readonly Rect Next = new Rect(101, 601, 338, 56);
        private StreetGame game;
        private Texture2D backdrop, people, items, parrillero, parrilleroIcon, largeGrill;
        private Texture2D[] fanWardrobe;
        private Texture2D coverArt, titleLogo, introGlow;
        private bool introActive;
        private float introStarted;
        private const float IntroDuration = 4.1f;
        private const string ParrilleroLabel = "Parrillero";
        private static readonly string[] FanWardrobeResourceNames = {
            "01-home-white-black", "02-alternative-blue", "03-october-rosa-pink",
            "04-goalkeeper-aqua", "05-alternative-graphite", "06-home-band-2025",
            "07-goalkeeper-skyblue", "08-goalkeeper-orange", "09-alternative-blue-original",
            "10-tee-black", "11-polo-white-black", "12-tee-gray",
            "13-training-thermal-white", "14-training-jacket-black-white", "15-hoodie-cream"
        };
        private GUIStyle tiny, small, text, title, header, amount, invisible;
        private int pressedAction, lastAction, priceProduct;
        private bool priceDrag;
        private float feedbackUntil, feedbackScale;
        // Individually reviewed authored pixel bounds, not a geometric-grid assumption.
        private static readonly Rect[] Items = {
            new Rect(13,58,298,240), new Rect(310,48,253,250), new Rect(566,66,303,232), new Rect(856,63,334,232), new Rect(1190,63,195,236),
            new Rect(8,298,244,309), new Rect(328,321,179,286), new Rect(515,309,326,305), new Rect(842,350,278,265), new Rect(1125,360,275,254),
            new Rect(47,640,216,216), new Rect(280,621,296,225), new Rect(586,625,237,226), new Rect(829,670,338,172), new Rect(1168,621,210,239),
            new Rect(5,870,315,231), new Rect(320,846,265,276), new Rect(640,854,175,246), new Rect(875,855,249,245), new Rect(1155,857,233,261)
        };
        private static readonly Rect[] People = {
            new Rect(82,9,186,298),new Rect(378,7,194,303),new Rect(679,8,185,306),new Rect(988,7,194,307),
            new Rect(96,317,168,302),new Rect(395,315,176,305),new Rect(690,314,173,305),new Rect(958,314,232,308),
            new Rect(49,631,236,306),new Rect(350,627,256,313),new Rect(658,629,252,311),new Rect(955,628,261,308),
            new Rect(79,943,196,304),new Rect(383,942,208,306),new Rect(690,940,200,308),new Rect(982,940,242,308)
        };
        // Reviewed alpha bounds from the original 1315x1197 atlas; existing fans unchanged.
        private static readonly Rect[] ParrilleroPoses = {
            new Rect(30,10,266,303),
            new Rect(357,16,263,309),
            new Rect(690,10,246,316),
            new Rect(1020,10,255,303),
            new Rect(31,331,264,288),
            new Rect(367,330,229,291),
            new Rect(708,330,228,290),
            new Rect(1018,335,265,285),
            new Rect(77,622,185,288),
            new Rect(382,623,233,287),
            new Rect(702,622,221,287),
            new Rect(1045,640,242,268),
            new Rect(45,908,234,278),
            new Rect(374,908,227,276),
            new Rect(687,908,249,276),
            new Rect(1044,909,235,277)
        };
        private void OnEnable()
        {
            game = GetComponent<StreetGame>();
            backdrop = Resources.Load<Texture2D>("street-background-mural-v3");
            people = Resources.Load<Texture2D>("street-characters");
            items = Resources.Load<Texture2D>("street-items");
            parrillero = Resources.Load<Texture2D>("street-parrillero");
            parrilleroIcon = Resources.Load<Texture2D>("street-parrillero-icon");
            largeGrill = Resources.Load<Texture2D>("street-parrilla-large");
            fanWardrobe = new Texture2D[FanWardrobeResourceNames.Length];
            for (int i = 0; i < FanWardrobeResourceNames.Length; i++)
                fanWardrobe[i] = Resources.Load<Texture2D>("FanWardrobe/" + FanWardrobeResourceNames[i]);
            coverArt = Resources.Load<Texture2D>("street-cover");
            titleLogo = Resources.Load<Texture2D>("street-logo");
            introGlow = CreateIntroGlow();
            introActive = coverArt != null && titleLogo != null;
            introStarted = Time.unscaledTime;
            tiny = small = text = title = header = amount = invisible = null;
            pressedAction = lastAction = priceProduct = 0; priceDrag = false;
        }
        private void OnDisable()
        {
            if (introGlow != null) Destroy(introGlow);
            introGlow = null;
        }
        private void Update()
        {
            if (introActive)
            {
                if (Time.unscaledTime - introStarted < IntroDuration) return;
                introActive = false;
            }
#if !(UNITY_ANDROID && !UNITY_EDITOR) && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var touch = Touchscreen.current; var mouse = Mouse.current;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
                HandlePointer(touch.primaryTouch.position.ReadValue(), touch.primaryTouch.press.wasPressedThisFrame, touch.primaryTouch.press.wasReleasedThisFrame);
            else if (mouse != null) HandlePointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame, mouse.leftButton.wasReleasedThisFrame);
#endif
        }
        private static Rect CanvasViewport(Vector2 size, Rect safe)
        {
            if(safe.xMax>size.x+1||safe.yMax>size.y+1) safe=new Rect(0,0,size.x,size.y);
            float l = Mathf.Clamp(safe.xMin,0,size.x), r = Mathf.Clamp(safe.xMax,0,size.x);
            float b = Mathf.Clamp(safe.yMin,0,size.y), t = Mathf.Clamp(safe.yMax,0,size.y);
            if (r <= l || t <= b) { l = b = 0; r = size.x; t = size.y; }
            float s = Mathf.Min((r-l)/W,(t-b)/H);
            return new Rect(l+(r-l-W*s)/2,size.y-t+(t-b-H*s)/2,W*s,H*s);
        }
        private void HandlePointer(Vector2 pixel, bool down, bool up)
        {
            if (game == null || game.Sim == null) return;
            Rect v = CanvasViewport(new Vector2(Screen.width,Screen.height),Screen.safeArea);
            if (v.width <= 0) return;
            Vector2 p = (new Vector2(pixel.x,Screen.height-pixel.y)-v.position)/(v.width/W);
            if (down) { priceDrag = game.Sim.Phase == RoundPhase.Ready && game.Sim.CanEditPrices && Slider.Contains(p); pressedAction = HitAction(p); }
            if (priceDrag) SetSlider(p.x);
            if (!up) return;
            if (!priceDrag && pressedAction != 0 && pressedAction == HitAction(p)) DispatchAction(pressedAction);
            priceDrag = false; pressedAction = 0;
        }
        private void SetSlider(float x)
        {
            if (!game.Sim.CanEditPrices) return;
            game.SetProductPrice(priceProduct,Mathf.Round(Mathf.Lerp(game.Balance.minPrice,game.Balance.maxPrice,Mathf.Clamp01((x-Slider.x)/Slider.width))));
        }
        private int HitAction(Vector2 p)
        {
            if (game.Sim.Phase == RoundPhase.Ready)
            {
                if (Start.Contains(p)) return 1;
                for(int i=0;i<game.Sim.ProductCount;i++)if(ProductButton(i).Contains(p))return 20+i;
                for (int i=0;i<5;i++) if (LevelButton(i).Contains(p) && i<=game.UnlockedLevel) return 10+i;
                return 0;
            }
            if (game.Sim.Phase == RoundPhase.Playing) return Speed.Contains(p) ? 3 : HireParrillero.Contains(p) ? 2 : 0;
            return Again.Contains(p) ? 4 : Next.Contains(p) && game.Sim.Phase==RoundPhase.Won && game.SelectedLevel<4 ? 5 : 0;
        }
        private void DispatchAction(int a)
        {
            if (introActive) return; // Also gates Android's native IMGUI action path.
            bool ok = true;
            if (a==1) game.StartRound(); else if (a==2) ok=game.TryHire(); else if (a==3) ok=game.TryUpgradeSpeed();
            else if (a==4) ok=game.Retry(); else if (a==5) ok=game.NextLevel(); else if (a>=10&&a<15) {ok=game.SelectLevel(a-10);priceProduct=0;} else if(a>=20&&a<27)priceProduct=a-20;
            lastAction=a; feedbackUntil=Time.unscaledTime+0.22f; feedbackScale=ok?1.04f:0.97f;
        }
        private void NativeAction(int a)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            DispatchAction(a);
#elif !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
            DispatchAction(a);
#endif
        }
        private void OnGUI()
        {
            if (game==null||game.Sim==null) return;
            Styles(); Rect v=CanvasViewport(new Vector2(Screen.width,Screen.height),Screen.safeArea); if(v.width<=0)return;
            Matrix4x4 m=GUI.matrix; Color old=GUI.color;
            GUI.matrix=Matrix4x4.TRS(new Vector3(v.x,v.y),Quaternion.identity,new Vector3(v.width/W,v.width/W,1)); GUI.color=Color.white;
            if(backdrop!=null)GUI.DrawTexture(new Rect(0,0,W,H),backdrop,ScaleMode.StretchToFill);
            var sim=game.Sim;
            // Upper signs contain live product sprites rather than a baked competing menu.
            Item(new Rect(40,130,47,37),0); Item(new Rect(444,130,47,37),sim.ProductCount-1);
            for(int row=4;row>=0;row--)for(int i=0;i<sim.Customers.Count;i++)
            {
                var c=sim.Customers[i]; if(Mathf.RoundToInt((310-c.Target.y)/70)!=row)continue;
                DrawCustomer(c);
            }
            DrawGrill();
            for(int i=0;i<7;i++) DrawStation(i,i<sim.ProductCount);
            for(int i=0;i<sim.Workers.Count;i++) DrawWorker(sim.Workers[i]);
            for(int i=0;i<sim.Sales.Count;i++)
            {
                var s=sim.Sales[i];float a=Mathf.Clamp01(2.2f-s.Age);GUI.color=new Color(1,1,1,a);
                Item(new Rect(s.Position.x-27,s.Position.y-86-s.Age*33,23,23),10);
                Label(new Rect(s.Position.x-3,s.Position.y-86-s.Age*33,48,25),"+"+s.Amount,text);GUI.color=Color.white;
            }
            Item(new Rect(24,644,26,26),10);Label(new Rect(51,644,119,27),sim.Coins.ToString(),text);
            Label(new Rect(168,644,212,27),"Ventas "+sim.Delivered+" / "+sim.Goal,small);
            Label(new Rect(385,644,137,27),Mathf.CeilToInt(sim.TimeRemaining)+" s",small);
            Upgrade(Speed,3,15,"Velocidad",sim.SpeedCost,sim.Coins>=sim.SpeedCost);
            Upgrade(HireParrillero,2,16,ParrilleroLabel,sim.HireCost,sim.Coins>=sim.HireCost&&sim.StaffCount<game.Balance.maxStaff);
            Label(new Rect(70,858,400,26),"Equipo "+sim.StaffCount+"  ·  Velocidad ×"+sim.WorkRate.ToString("0.00"),small);
            if(sim.Phase==RoundPhase.Ready) ReadyPanel();
            else if(sim.Phase==RoundPhase.Won||sim.Phase==RoundPhase.Lost) ResultPanel();
            else Label(new Rect(45,896,450,35),StreetSimulation.LevelNames[sim.LevelIndex],text);
            if (introActive) DrawIntro(Time.unscaledTime - introStarted);
            GUI.color=old;GUI.matrix=m;
        }
        private static Texture2D CreateIntroGlow()
        {
            const int size = 128;
            var glow = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Intro radial light flash",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f;
                float dy = (y + .5f) / size * 2f - 1f;
                float alpha = Mathf.Exp(-(dx * dx + dy * dy) * 7f);
                pixels[y * size + x] = new Color(1f, .91f, .69f, alpha);
            }
            glow.SetPixels(pixels);
            glow.Apply(false, true);
            return glow;
        }
        private void DrawIntro(float elapsed)
        {
            float coverFadeIn = IntroEase(elapsed / .28f);
            float logoFadeIn = IntroEase((elapsed - 1.62f) / .35f);
            float exitFade = 1f - IntroEase((elapsed - 3.48f) / .58f);
            float coverAlpha = coverFadeIn * exitFade;
            GUI.color = new Color(0f, 0f, 0f, exitFade);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = new Color(1f, 1f, 1f, coverAlpha);
            GUI.DrawTexture(new Rect(0, 0, W, H), coverArt, ScaleMode.ScaleAndCrop, true);

            float shadeAlpha = .48f * logoFadeIn * exitFade;
            GUI.color = new Color(.035f, .025f, .045f, shadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);

            Rect logoRect = new Rect(78f, 292f, 384f, 376f);
            float logoScale = Mathf.Lerp(.76f, 1f, logoFadeIn);
            logoRect = new Rect(270f - logoRect.width * logoScale * .5f,
                                480f - logoRect.height * logoScale * .5f,
                                logoRect.width * logoScale, logoRect.height * logoScale);

            float flashTime = elapsed - 2.04f;
            if (flashTime >= 0f && introGlow != null)
            {
                float glowProgress = IntroEase(flashTime / .72f);
                float glowAlpha = .9f * (1f - glowProgress) * logoFadeIn * exitFade;
                float glowScale = Mathf.Lerp(.7f, 1.9f, glowProgress);
                Rect glowRect = new Rect(270f - 235f * glowScale, 480f - 230f * glowScale,
                                         470f * glowScale, 460f * glowScale);
                GUI.color = new Color(1f, .91f, .72f, glowAlpha);
                GUI.DrawTexture(glowRect, introGlow, ScaleMode.StretchToFill, true);

                float flashAlpha = .32f * (1f - IntroEase(flashTime / .18f)) * logoFadeIn * exitFade;
                if (flashAlpha > .001f)
                {
                    GUI.color = new Color(1f, 1f, .96f, flashAlpha);
                    GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                }
            }

            GUI.color = new Color(1f, 1f, 1f, logoFadeIn * exitFade);
            GUI.DrawTexture(logoRect, titleLogo, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }
        private static float IntroEase(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private void DrawCustomer(StreetCustomer c)
        {
            bool walking=c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving;
            float bob=walking?Mathf.Sin(c.AnimationTime*14)*2:Mathf.Sin(c.AnimationTime*2+c.Id)*0.8f;
            if(c.State==StreetCustomerState.Receiving)bob-=3;
            Rect person = new Rect(c.Position.x-39,c.Position.y-76+bob,78,76);
            Person(person,8+(c.Id%4)+(walking?4:0),walking&&c.Target.x>c.Position.x);
            if (game.Sim.LevelIndex == 0) DrawFanWardrobe(person, c.Id);
            if(c.State==StreetCustomerState.Entering||c.State==StreetCustomerState.Leaving)return;
            Item(new Rect(c.Position.x-29,c.Position.y-132,58,65),11,true);
            Item(new Rect(c.Position.x-19,c.Position.y-125,38,30),c.Product);
            Label(new Rect(c.Position.x-29,c.Position.y-101,58,24),c.Remaining.ToString(),text);
            // A sprite-backed patience strip; no placeholder shape stands in for game art.
            GUI.color=new Color(.32f,.7f,.32f);Item(new Rect(c.Position.x-22,c.Position.y-64,44*c.PatienceFraction,4),13,true);GUI.color=Color.white;
        }
        private void DrawFanWardrobe(Rect person, int customerId)
        {
            if (fanWardrobe == null || fanWardrobe.Length == 0) return;
            Texture2D garment = fanWardrobe[GetFanWardrobeIndex(customerId)];
            if (garment == null) return;

            // The underlying original fan remains unchanged; this transparent shirt overlay
            // covers only its existing jersey area and follows the same bob/movement rect.
            Rect shirt = new Rect(person.center.x - 16, person.yMax - 38, 32, 22);
            GUI.DrawTexture(shirt, garment, ScaleMode.StretchToFill, true);
        }
        private static int GetFanWardrobeIndex(int customerId)
        {
            int index = customerId % FanWardrobeResourceNames.Length;
            return index < 0 ? index + FanWardrobeResourceNames.Length : index;
        }
        private void DrawWorker(StreetWorker w)
        {
            bool moving=w.State==StreetWorkerState.ToStation||w.State==StreetWorkerState.ToCounter;
            bool back=w.Target.y<w.Position.y;int frame=0;
            if(moving)
            {
                bool sideways=Mathf.Abs(w.Target.x-w.Position.x)>Mathf.Abs(w.Target.y-w.Position.y);
                frame=(sideways?9:(back?5:1))+((int)(w.AnimationTime*8)%2);
            }
            if(w.State==StreetWorkerState.Pickup)frame=7;
            if(w.State==StreetWorkerState.Handoff)frame=3;
            float bob=moving?Mathf.Sin(w.AnimationTime*16)*1.5f:0;
            Parrillero(new Rect(w.Position.x-45,w.Position.y-98+bob,90,98),frame,w.Target.x>w.Position.x&&Mathf.Abs(w.Target.x-w.Position.x)>30);
            if(w.State==StreetWorkerState.ToCounter||w.State==StreetWorkerState.Handoff)
                Item(new Rect(w.Position.x-25,w.Position.y-46+bob,31,29),w.Product);
            if(w.State==StreetWorkerState.Pickup) {GUI.color=new Color(1,1,1,.55f);Item(new Rect(w.Position.x-15,w.Position.y-45,30,36),19);GUI.color=Color.white;}
        }
        // Shared hot-food surface. Floresta devotes the full row to its only product;
        // later levels reserve the right side for the unchanged beverage stations.
        private static Rect GrillRect(int productCount)
        {
            return productCount <= 4 ? new Rect(23, 530, 494, 108) : new Rect(23, 548, 280, 90);
        }
        private void DrawGrill()
        {
            Rect r = GrillRect(game.Sim.ProductCount);
            if (largeGrill != null) GUI.DrawTexture(r, largeGrill, ScaleMode.StretchToFill);
            else Item(r, 7); // Existing original parrilla is a safe missing-resource fallback.
        }
        private void DrawStation(int i,bool unlocked)
        {
            // A single broad parrilla replaces seven tiny BBQs. Locked products do not
            // add miniature stations; unlock logic and pickup coordinates remain intact.
            if (!unlocked) return;
            float x=StreetSimulation.StationPosition(i).x;
            if(i>=4) Item(new Rect(x-33,555,66,72),8);
            float y=i<4 ? GrillRect(game.Sim.ProductCount).y-15 : 548;
            Item(new Rect(x-20,y,40,29),i);
            Label(new Rect(x-34,618,68,20),StreetSimulation.ProductNames[i],tiny);
        }
        private void Upgrade(Rect r,int action,int icon,string name,int cost,bool affordable)
        {
            bool available=affordable&&game.Sim.Phase==RoundPhase.Playing;
            Color old=GUI.color;GUI.color=available?Color.white:new Color(.57f,.57f,.57f,1);
            Rect drawn=r;if(lastAction==action&&Time.unscaledTime<feedbackUntil){drawn.width*=feedbackScale;drawn.height*=feedbackScale;drawn.center=r.center;}
            Item(drawn,12,true);GUI.color=old;
            Label(new Rect(r.x+5,r.y+12,r.width-10,32),name,title);
            if(action==3 && !game.Sim.CanEditPrices) Label(new Rect(r.x+5,r.y+43,r.width-10,18),"+10%",tiny);
            Item(new Rect(r.x+45,r.y+57,r.width-90,66),icon);
            Item(new Rect(r.x+37,r.y+139,28,28),10);Label(new Rect(r.x+69,r.y+132,80,37),cost.ToString(),amount);
            bool prev=GUI.enabled;GUI.enabled=available;
            if(GUI.Button(r,"",invisible))NativeAction(action);GUI.enabled=prev;
        }
        private void ReadyPanel()
        {
            Rect panel=new Rect(38,193,464,454);Item(panel,14,true);
            bool editable=game.Sim.CanEditPrices;
            Label(new Rect(54,207,432,64),editable?"Precio: "+StreetSimulation.ProductNames[priceProduct]:"Chori · precio fijo",header);
            Item(new Rect(188,306,164,107),priceProduct);
            Label(new Rect(86,421,368,54),"$ "+game.Sim.GetProductPrice(priceProduct).ToString("0"),amount);
            if(editable)
            {
                Item(new Rect(Slider.x,Slider.y+13,Slider.width,10),13,true);
                float f=Mathf.InverseLerp(game.Balance.minPrice,game.Balance.maxPrice,game.Sim.GetProductPrice(priceProduct));
                Item(new Rect(Slider.x+f*Slider.width-16,Slider.y+1,32,32),10);
#if UNITY_ANDROID && !UNITY_EDITOR || !(ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
                var e=Event.current;
                if(e.type==EventType.MouseDown&&Slider.Contains(e.mousePosition)){priceDrag=true;SetSlider(e.mousePosition.x);e.Use();}
                else if(priceDrag&&e.type==EventType.MouseDrag){SetSlider(e.mousePosition.x);e.Use();}
                else if(priceDrag&&e.type==EventType.MouseUp){SetSlider(e.mousePosition.x);priceDrag=false;e.Use();}
#endif
            }
            Label(new Rect(60,519,420,30),editable?(game.Sim.DemandFraction>.6f?"Precio bajo · mucha demanda":"Precio alto · menor demanda"):"Cada hincha pide de 1 a 4 choris",small);
            Button(Start,1,"Empezar");
            if(game.Sim.ProductCount>1)for(int i=0;i<game.Sim.ProductCount;i++){Rect r=ProductButton(i);Item(r,12,true);Item(new Rect(r.x+4,r.y+3,r.width-8,r.height-6),i);if(GUI.Button(r,"",invisible))NativeAction(20+i);}
            Label(new Rect(43,878,454,26),StreetSimulation.LevelNames[game.SelectedLevel],text);
            for(int i=0;i<5;i++)
            {
                Rect r=LevelButton(i);bool unlocked=i<=game.UnlockedLevel;GUI.color=unlocked?Color.white:new Color(.6f,.6f,.6f);
                Item(r,12,true);GUI.color=Color.white;
                if(unlocked)Label(r,(i+1).ToString(),text);else Item(new Rect(r.center.x-9,r.y+5,18,23),17);
                bool prev=GUI.enabled;GUI.enabled=unlocked;if(GUI.Button(r,"",invisible))NativeAction(10+i);GUI.enabled=prev;
            }
        }
        private static Rect ProductButton(int i){return new Rect(72+i*57,273,50,31);}
        private static Rect LevelButton(int i){return new Rect(108+i*67,912,57,35);}
        private void ResultPanel()
        {
            bool won=game.Sim.Phase==RoundPhase.Won;Item(new Rect(38,206,464,486),14,true);
            Label(new Rect(54,221,432,75),won?"¡Aguante el puesto!":"Se terminó el tiempo",header);
            Item(new Rect(224,318,92,96),won?18:16);
            Label(new Rect(74,421,392,82),game.Sim.Delivered+" ventas · $"+game.Sim.Coins+"\nEquipo y mejoras guardados",text);
            Button(Again,4,game.Sim.CanEditPrices?"Volver a elegir precio":"Jugar de nuevo");
            if(won&&game.SelectedLevel<4)Button(Next,5,"Siguiente cancha");
            else if(won)Label(new Rect(73,601,394,58),"¡Las cinco canchas completas!",text);
        }
        private void Button(Rect r,int action,string label){Item(r,13,true);Label(r,label,title);if(GUI.Button(r,"",invisible))NativeAction(action);}
        private void Styles()
        {
            if(tiny!=null)return;
            tiny=Style(11);small=Style(14);text=Style(18);title=Style(23);header=Style(25);header.normal.textColor=Color.white;amount=Style(31);
            invisible=new GUIStyle();
        }
        private static GUIStyle Style(int size){var s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};s.normal.textColor=new Color(.16f,.10f,.08f);return s;}
        private static void Label(Rect r,string value,GUIStyle style){GUI.Label(r,value,style);}
        private void Item(Rect r,int id,bool stretch=false)
        {
            if(id==16 && parrilleroIcon!=null)Draw(r,parrilleroIcon,new Rect(22,6,1269,1188),false,false);
            else if(items!=null&&id>=0&&id<Items.Length)Draw(r,items,Items[id],stretch,false);
        }
        private void Parrillero(Rect r,int id,bool flip)
        {
            if(parrillero!=null)Draw(r,parrillero,ParrilleroPoses[id],false,flip);
        }
        private void Person(Rect r,int id,bool flip){if(people==null)return;Draw(r,people,People[id],false,flip);}
        private static void Draw(Rect dest,Texture2D texture,Rect src,bool stretch,bool flip)
        {
            if(!stretch){float scale=Mathf.Min(dest.width/src.width,dest.height/src.height);dest=new Rect(dest.center.x-src.width*scale*.5f,dest.yMax-src.height*scale,src.width*scale,src.height*scale);}
            Rect uv=new Rect(src.x/texture.width,1-src.yMax/texture.height,src.width/texture.width,src.height/texture.height);
            if(flip){uv.x+=uv.width;uv.width=-uv.width;}GUI.DrawTextureWithTexCoords(dest,texture,uv,true);
        }
    }
}
