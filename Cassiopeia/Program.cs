using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Cassio
{
    class Program
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Menu Config;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                Q = new Spell(SpellSlot.Q, 850);
                W = new Spell(SpellSlot.W, 850);
                E = new Spell(SpellSlot.E, 700);

                Q.SetSkillshot(0.60f, 150f, int.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);
                W.SetSkillshot(0.60f, 300f, int.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);

                SpellList.Add(Q);
                SpellList.Add(W);
                SpellList.Add(E);

                Config = new Menu("ESLCassio", "ESLCassio", true);

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
            catch (Exception)
            {
                Game.PrintChat("Error found in Cassio. Refused to load.");
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (SpellList == null) return;

            foreach (Spell spell in SpellList)
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
            foreach (var buff in unit.Buffs)
            {
                if (buff.IsActive && buff.Type == BuffType.Poison)
                {
                    if (buff.EndTime >= (Game.Time + 0.35 + 700 / 1900))
                        return true;

                }
            }
            return false;
        }

        private static void ExecuteCombo()
        {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;
            
            Orbwalker.SetAttacks(false);

            if (W.IsReady() && ObjectManager.Player.Distance(target) <= W.Range)
                W.Cast(target, true, true);

            if (Q.IsReady() && ObjectManager.Player.Distance(target) <= Q.Range)
                Q.Cast(target, true, true);

            if (E.IsReady() && ObjectManager.Player.Distance(target) <= E.Range && IsPoisoned(target) || DamageLib.getDmg(target, DamageLib.SpellType.E) > target.Health)
                E.CastOnUnit(target, true);

        }
    }
}
