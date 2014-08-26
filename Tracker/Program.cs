#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

#endregion

namespace Tracker
{
    internal class Program
    {
        public static Sprite Sprite;
        public static Texture CdFrameTexture;
        private static readonly Dictionary<string, Texture> SummonerTextures = new Dictionary<string, Texture>();
        public static Line ReadyLine;
        public static Font Text;

        public static int X = 0;
        public static int Y = 0;

        public static SpellSlot[] SummonerSpellSlots = { SpellSlot.Q, SpellSlot.W };
        public static SpellSlot[] SpellSlots = { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };

        public static string[] SummonersNames =
        {
            "SummonerBarrier", "SummonerBoost", "SummonerClairvoyance",
            "SummonerDot", "SummonerExhaust", "SummonerFlash", "SummonerHaste", "SummonerHeal", "SummonerMana",
            "SummonerOdinGarrison", "SummonerRevive", "SummonerSmite", "SummonerTeleport"
        };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static Texture GetSummonerTexture(string name)
        {
            Bitmap bitmap;
            switch (name)
            {
                case "SummonerOdinGarrison":
                    bitmap = Properties.Resources.SummonerOdinGarrison;
                    break;
                case "SummonerRevive":
                    bitmap = Properties.Resources.SummonerRevive;
                    break;
                case "SummonerClairvoyance":
                    bitmap = Properties.Resources.SummonerClairvoyance;
                    break;
                case "SummonerBoost":
                    bitmap = Properties.Resources.SummonerBoost;
                    break;
                case "SummonerMana":
                    bitmap = Properties.Resources.SummonerMana;
                    break;
                case "SummonerTeleport":
                    bitmap = Properties.Resources.SummonerTeleport;
                    break;
                case "SummonerHeal":
                    bitmap = Properties.Resources.SummonerHeal;
                    break;
                case "SummonerExhaust":
                    bitmap = Properties.Resources.SummonerExhaust;
                    break;
                case "SummonerSmite":
                    bitmap = Properties.Resources.SummonerSmite;
                    break;
                case "SummonerDot":
                    bitmap = Properties.Resources.SummonerDot;
                    break;
                case "SummonerHaste":
                    bitmap = Properties.Resources.SummonerHaste;
                    break;
                case "SummonerFlash":
                    bitmap = Properties.Resources.SummonerFlash;
                    break;
                default:
                    bitmap = Properties.Resources.SummonerBarrier;
                    break;
            }

            return Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[])), 12, 240, 0,
                Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                foreach (var sName in SummonersNames)
                {
                    SummonerTextures.Add(sName, GetSummonerTexture(sName));
                }

                Sprite = new Sprite(Drawing.Direct3DDevice);
                CdFrameTexture = Texture.FromMemory(
                    Drawing.Direct3DDevice,
                    (byte[])new ImageConverter().ConvertTo(Properties.Resources.hud, typeof(byte[])), 147, 27, 0,
                    Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);

                ReadyLine = new Line(Drawing.Direct3DDevice) { Width = 2 };

                Text = new Font(
                    Drawing.Direct3DDevice,
                    new FontDescription
                    {
                        FaceName = "Calibri",
                        Height = 13,
                        OutputPrecision = FontPrecision.Default,
                        Quality = FontQuality.Default,
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine(@"/ff can't load the textures: " + e);
            }

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            ReadyLine.Dispose();
            Text.Dispose();
            Sprite.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            ReadyLine.OnResetDevice();
            Text.OnResetDevice();
            Sprite.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            ReadyLine.OnLostDevice();
            Text.OnLostDevice();
            Sprite.OnLostDevice();
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_KEYDOWN)
            {
                return;
            }

            var key = args.WParam;
            switch (key)
            {
                case 97: //97 left
                    X--;
                    break;
                case 101: //101 up
                    Y--;
                    break;
                case 99: //99 right
                    X++;
                    break;
                case 98: //98 down
                    Y++;
                    break;
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
            {
                return;
            }

            try
            {
                if (Sprite.IsDisposed)
                {
                    return;
                }

                foreach (var hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(hero => hero.IsValid && !hero.IsMe && hero.IsVisible && !hero.IsDead))
                {
                    Sprite.Begin();

                    var indicator = new HpBarIndicator { Unit = hero };

                    X = (int)indicator.Position.X;
                    Y = (int)indicator.Position.Y;

                    var k = 0;
                    foreach (var sSlot in SummonerSpellSlots)
                    {
                        var spell = hero.SummonerSpellbook.GetSpell(sSlot);
                        var texture = SummonerTextures.ContainsKey(spell.Name)
                            ? SummonerTextures[spell.Name]
                            : SummonerTextures["SummonerBarrier"];
                        var t = spell.CooldownExpires - Game.Time;

                        var percent = (Math.Abs(spell.Cooldown) > float.Epsilon) ? t / spell.Cooldown : 1f;
                        var n = (t > 0) ? (int)(19 * (1f - percent)) : 19;
                        var ts = TimeSpan.FromSeconds((int)t);
                        var s = t > 60 ? string.Format("{0}:{1:D2}", ts.Minutes, ts.Seconds) : String.Format("{0:0}", t);
                        if (t > 0)
                        {
                            Text.DrawText(
                                null, s, X - 5 - s.Length * 5, Y + 1 + 13 * k, new ColorBGRA(255, 255, 255, 255));
                        }

                        Sprite.Draw(
                            texture, new ColorBGRA(255, 255, 255, 255), new SharpDX.Rectangle(0, 12 * n, 12, 12),
                            new Vector3(-X - 3, -Y - 1 - 13 * k, 0));
                        k++;
                    }

                    Sprite.Draw(CdFrameTexture, new ColorBGRA(255, 255, 255, 255), null, new Vector3(-X, -Y, 0));
                    Sprite.End();

                    var startX = X + 19;
                    var startY = Y + 20;

                    ReadyLine.Begin();
                    foreach (var slot in SpellSlots)
                    {
                        var spell = hero.Spellbook.GetSpell(slot);
                        var t = spell.CooldownExpires - Game.Time;
                        var percent = (t > 0 && Math.Abs(spell.Cooldown) > float.Epsilon)
                            ? 1f - (t / spell.Cooldown)
                            : 1f;

                        if (t > 0 && t < 100)
                        {
                            var s = string.Format(t < 1f ? "{0:0.0}" : "{0:0}", t);
                            Text.DrawText(
                                null, s, startX + (23 - s.Length * 4) / 2, startY + 6, new ColorBGRA(255, 255, 255, 255));
                        }

                        var darkColor = (t > 0) ? new ColorBGRA(168, 98, 0, 255) : new ColorBGRA(0, 130, 15, 255);
                        var lightColor = (t > 0) ? new ColorBGRA(235, 137, 0, 255) : new ColorBGRA(0, 168, 25, 255);

                        if (hero.Spellbook.CanUseSpell(slot) != SpellState.NotLearned)
                        {
                            for (var i = 0; i < 2; i++)
                            {
                                ReadyLine.Draw(
                                    new[]
                                    {
                                        new Vector2(startX, startY + i * 2),
                                        new Vector2(startX + percent * 23, startY + i * 2)
                                    },
                                    i == 0 ? lightColor : darkColor);
                            }
                        }

                        startX = startX + 27;
                    }
                    ReadyLine.End();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"/ff can't draw sprites: " + e);
            }
        }

        internal class HpBarIndicator
        {
            internal Obj_AI_Hero Unit { get; set; }

            private Vector2 Offset
            {
                get
                {
                    if (Unit != null)
                    {
                        return Unit.IsAlly ? new Vector2(-9, 14) : new Vector2(-9, 17);
                    }

                    return new Vector2();
                }
            }

            internal Vector2 Position
            {
                get { return new Vector2(Unit.HPBarPosition.X + Offset.X, Unit.HPBarPosition.Y + Offset.Y); }
            }
        }
    }
}
