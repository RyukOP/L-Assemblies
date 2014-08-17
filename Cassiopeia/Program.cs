using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Services;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Cassio
{
    class Program
    {
        private static Orbwalking.Orbwalker Orbwalker;
        private static readonly List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static Menu Config;

        private const string ChampionName = "Cassiopeia";

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName)
                return;

            Q = new Spell(SpellSlot.Q, 850);
            W = new Spell(SpellSlot.W, 850);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 825);

            Q.SetSkillshot(0.60f, 150f, int.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.60f, 300f, int.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.30f, 80f , int.MaxValue, false, Prediction.SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);

            Config = new Menu(ChampionName, ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (SpellList == null) return;

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();

                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                ExecuteCombo();
        }

        static bool IsPoisoned(Obj_AI_Base unit)
        {
            return unit.Buffs.Where(buff => buff.IsActive && buff.Type == BuffType.Poison).Any(buff => buff.EndTime >= (Game.Time + 0.35 + 700/1900));
        }

        private static void ExecuteCombo()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;
            
            Orbwalker.SetAttacks(false);

            if (DamageLib.IsKillable(target,
                new[]
                {
                    DamageLib.SpellType.Q, DamageLib.SpellType.W, DamageLib.SpellType.E, DamageLib.SpellType.E,
                    DamageLib.SpellType.R, DamageLib.SpellType.R, DamageLib.SpellType.IGNITE
                }))
            {
                if (R.IsReady() && ObjectManager.Player.Distance(target) <= R.Range + R.Width)
                    R.Cast(target, true, true);

                if (W.IsReady() && ObjectManager.Player.Distance(target) <= W.Range + W.Width)
                    W.Cast(target, true, true);

                if (Q.IsReady() && ObjectManager.Player.Distance(target) <= Q.Range + Q.Width)
                    Q.Cast(target, true, true);

                if (E.IsReady() && ObjectManager.Player.Distance(target) <= E.Range + target.BoundingRadius && IsPoisoned(target) || DamageLib.IsKillable(target, new[] { DamageLib.SpellType.E }))
                    E.CastOnUnit(target, true);
            }
            else
            {
                if (W.IsReady() && ObjectManager.Player.Distance(target) <= W.Range + W.Width)
                    W.Cast(target, true, true);

                if (Q.IsReady() && ObjectManager.Player.Distance(target) <= Q.Range + Q.Width)
                    Q.Cast(target, true, true);

                if (E.IsReady() && ObjectManager.Player.Distance(target) <= E.Range + target.BoundingRadius && IsPoisoned(target) || DamageLib.IsKillable(target, new []{DamageLib.SpellType.E}))
                    E.CastOnUnit(target, true);
            }

            if (!Q.IsReady() && !W.IsReady() && !E.IsReady())
                Orbwalker.SetAttacks(true);
        }
    }
}
