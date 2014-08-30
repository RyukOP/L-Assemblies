using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Tracker
{
    internal enum WardType
    {
        Green,
        Pink,
        Trap,
    }

    internal class WardData
    {
        public int Duration;
        public string ObjectBaseSkinName;
        public int Range;
        public string SpellName;
        public WardType Type;

        public Bitmap Bitmap
        {
            get
            {
                switch (Type)
                {
                    case WardType.Green:
                        return Properties.Resources.Minimap_Ward_Green_Enemy;
                    case WardType.Pink:
                        return Properties.Resources.Minimap_Ward_Pink_Enemy;
                    default:
                        return Properties.Resources.Minimap_Ward_Green_Enemy;
                }
            }
        }

        public Color Color
        {
            get
            {
                switch (Type)
                {
                    case WardType.Green:
                        return Color.Lime;
                    case WardType.Pink:
                        return Color.Magenta;
                    default:
                        return Color.Red;
                }
            }
        }
    }

    internal class DetectedWard
    {
        private Render.Circle _defaultCircle;
        private Render.Circle _defaultCircleFilled;
        private Render.Sprite _minimapSprite;
        private Render.Line _missileLine;
        private Render.Circle _rangeCircle;
        private Render.Circle _rangeCircleFilled;
        private float _scale = 0.7f;
        private Render.Text _timerText;

        public DetectedWard(WardData data,
            Vector3 position,
            int startT,
            Obj_AI_Base wardObject = null,
            bool isFromMissile = false)
        {
            WardData = data;
            Position = position;
            StartT = startT;
            WardObject = wardObject;
            IsFromMissile = isFromMissile;
            CreateRenderObjects();
        }

        public WardData WardData { get; set; }
        public int StartT { get; set; }

        public int Duration
        {
            get { return WardData.Duration; }
        }

        public int EndT
        {
            get { return StartT + Duration; }
        }

        public Vector3 StartPosition { get; set; }
        public Vector3 Position { get; set; }

        public Color Color
        {
            get { return WardData.Color; }
        }

        public int Range
        {
            get { return WardData.Range; }
        }

        public bool IsFromMissile { get; set; }
        public Obj_AI_Base WardObject { get; set; }

        private Vector2 MinimapPosition
        {
            get
            {
                return Drawing.WorldToMinimap(Position) +
                       new Vector2(-WardData.Bitmap.Width / 2 * _scale, -WardData.Bitmap.Height / 2 * _scale);
            }
        }

        public void CreateRenderObjects()
        {
            //Create the minimap sprite.
            if (Range == 1100)
            {
                _minimapSprite = new Render.Sprite(WardData.Bitmap, MinimapPosition);
                _minimapSprite.Scale = new Vector2(0.7f, 0.7f);
                _minimapSprite.PositionUpdate += () => MinimapPosition;
                _minimapSprite.VisibleCondition += sender => WardTracker.Config.Item("Enabled").GetValue<bool>();
                _minimapSprite.Add(0);
            }

            //Create the circle:
            _defaultCircle = new Render.Circle(Position, 200, Color, 5, true);
            _defaultCircle.VisibleCondition +=
                sender =>
                    WardTracker.Config.Item("Enabled").GetValue<bool>() &&
                    !WardTracker.Config.Item("Details").GetValue<KeyBind>().Active;
            _defaultCircle.Add(0);
            _defaultCircleFilled = new Render.Circle(Position, 200, Color.FromArgb(25, Color), -142857, true);
            _defaultCircleFilled.VisibleCondition +=
                sender =>
                    WardTracker.Config.Item("Enabled").GetValue<bool>() &&
                    !WardTracker.Config.Item("Details").GetValue<KeyBind>().Active;
            _defaultCircleFilled.Add(-1);

            //Create the circle that shows the range
            _rangeCircle = new Render.Circle(Position, Range, Color, 10, false);
            _rangeCircle.VisibleCondition +=
                sender =>
                    WardTracker.Config.Item("Enabled").GetValue<bool>() &&
                    WardTracker.Config.Item("Details").GetValue<KeyBind>().Active;
            _rangeCircle.Add(0);

            _rangeCircleFilled = new Render.Circle(Position, Range, Color.FromArgb(25, Color), -142857, true);
            _rangeCircleFilled.VisibleCondition +=
                sender =>
                    WardTracker.Config.Item("Enabled").GetValue<bool>() &&
                    WardTracker.Config.Item("Details").GetValue<KeyBind>().Active;
            _rangeCircleFilled.Add(-1);


            //Missile line;
            if (IsFromMissile)
            {
                _missileLine = new Render.Line(new Vector2(), new Vector2(), 2, new ColorBGRA(255, 255, 255, 255));
                _missileLine.EndPositionUpdate = () => Drawing.WorldToScreen(Position);
                _missileLine.StartPositionUpdate = () => Drawing.WorldToScreen(StartPosition);
                _missileLine.VisibleCondition +=
                    sender =>
                        WardTracker.Config.Item("Enabled").GetValue<bool>() &&
                        WardTracker.Config.Item("Details").GetValue<KeyBind>().Active;
                _missileLine.Add(0);
            }


            //Create the timer text:
            if (Duration != int.MaxValue)
            {
                _timerText = new Render.Text(10, 10, "t", 18, new ColorBGRA(255, 255, 255, 255));
                _timerText.OutLined = true;
                _timerText.PositionUpdate = () => Drawing.WorldToScreen(Position);
                _timerText.Centered = true;
                _timerText.VisibleCondition += sender => WardTracker.Config.Item("Enabled").GetValue<bool>();

                _timerText.TextUpdate = delegate
                {
                    var t = TimeSpan.FromSeconds((EndT - Environment.TickCount) / 1000f);


                    return (IsFromMissile ? "?? " : "") +
                           (t.Minutes != 0
                               ? string.Format("{0:D1} : {1:D2}", t.Minutes, t.Seconds)
                               : string.Format("{0:D2}", t.Seconds)) + (IsFromMissile ? " ??" : "");
                };
                _timerText.Add(2);
            }
        }

        public bool Remove()
        {
            if (_minimapSprite != null)
            {
                _minimapSprite.Remove();
            }

            _defaultCircle.Remove();
            _rangeCircle.Remove();
            _rangeCircleFilled.Remove();
            _defaultCircleFilled.Remove();

            if (_timerText != null)
            {
                _timerText.Remove();
            }

            if (_missileLine != null)
            {
                _missileLine.Remove();
            }
            return true;
        }
    }

    /// <summary>
    /// Ward tracker tracks enemy wards and traps.
    /// </summary>
    public static class WardTracker
    {
        private static readonly List<WardData> PosibleWards = new List<WardData>();
        private static readonly List<DetectedWard> DetectedWards = new List<DetectedWard>();
        public static Menu Config;

        static WardTracker()
        {
            //Add the posible wards and their detection type:

            #region PosibleWards

            //Trinkets:
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 1000,
                    ObjectBaseSkinName = "YellowTrinket",
                    Range = 1100,
                    SpellName = "TrinketTotemLvl1",
                    Type = WardType.Green
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 3 * 1000,
                    ObjectBaseSkinName = "YellowTrinketUpgrade",
                    Range = 1100,
                    SpellName = "TrinketTotemLvl2",
                    Type = WardType.Green
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 3 * 1000,
                    ObjectBaseSkinName = "SightWard",
                    Range = 1100,
                    SpellName = "TrinketTotemLvl3",
                    Type = WardType.Green
                });

            //Ward items and normal wards:
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 3 * 1000,
                    ObjectBaseSkinName = "SightWard",
                    Range = 1100,
                    SpellName = "SightWard",
                    Type = WardType.Green
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 3 * 1000,
                    ObjectBaseSkinName = "SightWard",
                    Range = 1100,
                    SpellName = "ItemGhostWard",
                    Type = WardType.Green
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 3 * 1000,
                    ObjectBaseSkinName = "SightWard",
                    Range = 1100,
                    SpellName = "wrigglelantern",
                    Type = WardType.Green
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 3 * 1000,
                    ObjectBaseSkinName = "SightWard",
                    Range = 1100,
                    SpellName = "ItemFeralFlare",
                    Type = WardType.Green
                });

            //Pinks:
            PosibleWards.Add(
                new WardData
                {
                    Duration = int.MaxValue,
                    ObjectBaseSkinName = "VisionWard",
                    Range = 1100,
                    SpellName = "TrinketTotemLvl3B",
                    Type = WardType.Pink
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = int.MaxValue,
                    ObjectBaseSkinName = "VisionWard",
                    Range = 1100,
                    SpellName = "VisionWard",
                    Type = WardType.Pink
                });

            //Traps
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 4 * 1000,
                    ObjectBaseSkinName = "CaitlynTrap",
                    Range = 300,
                    SpellName = "CaitlynYordleTrap",
                    Type = WardType.Trap
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 10 * 1000,
                    ObjectBaseSkinName = "TeemoMushroom",
                    Range = 212,
                    SpellName = "BantamTrap",
                    Type = WardType.Trap
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 1 * 1000,
                    ObjectBaseSkinName = "ShacoBox",
                    Range = 212,
                    SpellName = "JackInTheBox",
                    Type = WardType.Trap
                });
            PosibleWards.Add(
                new WardData
                {
                    Duration = 60 * 2 * 1000,
                    ObjectBaseSkinName = "Nidalee_Spear",
                    Range = 212,
                    SpellName = "Bushwhack",
                    Type = WardType.Trap
                });

            #endregion

            //Used for removing the wards that expire:
            Game.OnGameUpdate += GameOnOnGameUpdate;

            //Used to detect the wards when the unit that places the ward is visible:
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

            //Used to detect the wards when the unit is not visible but the ward is.
            GameObject.OnCreate += Obj_AI_Base_OnCreate;

            //Used to detect the ward missile when neither the unit or the ward are visible:
            GameObject.OnCreate += ObjSpellMissileOnOnCreate;

            //Process the detected ward objects on the map.
            foreach (var obj in ObjectManager.Get<GameObject>().Where(o => o is Obj_AI_Base))
            {
                Obj_AI_Base_OnCreate(obj, null);
            }
        }

        public static void AttachToMenu(Menu menu)
        {
            Config = menu.AddSubMenu(new Menu("Ward Tracker", "Ward Tracker"));
            Config.AddItem(new MenuItem("Details", "Show more info").SetValue(new KeyBind(16, KeyBindType.Press)));
            Config.AddItem(new MenuItem("Enabled", "Enabled").SetValue(true));
        }

        private static void ObjSpellMissileOnOnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_SpellMissile))
            {
                return;
            }

            var missile = (Obj_SpellMissile)sender;

            if (!missile.SpellCaster.IsAlly)
            {
                if (missile.SData.Name == "itemplacementmissile" && !missile.SpellCaster.IsVisible)
                {
                    var sPos = missile.StartPosition;
                    var ePos = missile.EndPosition;
                    Utility.DelayAction.Add(
                        1000, delegate
                        {
                            if (
                                !DetectedWards.Any(
                                    w =>
                                        w.Position.To2D().Distance(sPos.To2D(), ePos.To2D(), false, false) < 300 &&
                                        Math.Abs(w.StartT - Environment.TickCount) < 2000))
                            {
                                var detectedWard = new DetectedWard(
                                    PosibleWards[3],
                                    new Vector3(ePos.X, ePos.Y, NavMesh.GetHeightForPosition(ePos.X, ePos.Y)),
                                    Environment.TickCount, null, true);
                                detectedWard.StartPosition = new Vector3(
                                    sPos.X, sPos.Y, NavMesh.GetHeightForPosition(sPos.X, sPos.Y));
                                DetectedWards.Add(detectedWard);
                            }
                        });
                }
            }
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_AI_Base))
            {
                return;
            }
            var wardObject = (Obj_AI_Base)sender;

            if (sender.IsAlly)
            {
                return;
            }

            foreach (var wardData in PosibleWards)
            {
                if (String.Equals(
                    wardObject.BaseSkinName, wardData.ObjectBaseSkinName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var startT = Environment.TickCount - (int)((wardObject.MaxMana - wardObject.Mana) * 1000);
                    DetectedWards.RemoveAll(
                        w =>
                            w.Position.Distance(wardObject.Position) < 200 && (Math.Abs(w.StartT - startT) < 1000 || wardData.Type != WardType.Green) &&
                            w.Remove());
                    DetectedWards.Add(new DetectedWard(wardData, wardObject.Position, startT, wardObject));
                }
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsAlly)
            {
                return;
            }

            foreach (var wardData in PosibleWards)
            {
                if (String.Equals(args.SData.Name, wardData.SpellName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var endPosition = ObjectManager.Player.GetPath(args.End).ToList().Last();
                    DetectedWards.Add(new DetectedWard(wardData, endPosition, Environment.TickCount));
                }
            }
        }

        private static void GameOnOnGameUpdate(EventArgs args)
        {
            //Delete the wards that expire:
            DetectedWards.RemoveAll(w => w.EndT <= Environment.TickCount && w.Duration != int.MaxValue && w.Remove());

            //Delete the wards that get destroyed:
            DetectedWards.RemoveAll(w => w.WardObject != null && !w.WardObject.IsValid && w.Remove());
        }
    }
}
