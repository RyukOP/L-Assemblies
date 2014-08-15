using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Cho_Gath
{
    internal class Program
    {
        public static string ChampionName = "Chogath";
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell R;
        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName)
                return;

            try
            {
                Q = new Spell(SpellSlot.Q, 950);
                W = new Spell(SpellSlot.W, 675);
                R = new Spell(SpellSlot.R, 175);

                Q.SetSkillshot(0.75f, 175f, 1000f, false, Prediction.SkillshotType.SkillshotCircle);
                W.SetSkillshot(0.60f, 300f, 1750f, false, Prediction.SkillshotType.SkillshotCone);

                SpellList.Add(Q);
                SpellList.Add(W);
                SpellList.Add(R);

                Config = new Menu(ChampionName, ChampionName, true);

                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

                Config.AddSubMenu(new Menu("Combo", "Combo"));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("Killsteal", "Enable killsteal").SetValue(false));
                Config.SubMenu("Combo")
                    .AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

                Config.AddSubMenu(new Menu("AutoSpell", "AutoSpell"));
                Config.SubMenu("AutoSpell").AddItem(new MenuItem("AutoQ1", "Auto-Q inmobile").SetValue(true));
                Config.SubMenu("AutoSpell").AddItem(new MenuItem("AutoQ2", "Auto-Q dashing").SetValue(true));

                Config.AddSubMenu(new Menu("Additonal", "Additonal"));
                Config.SubMenu("Additonal").AddItem(new MenuItem("AutoStack", "Auto Stack ult").SetValue(true));
                Config.SubMenu("Additonal").AddItem(new MenuItem("AutoInterrupt", "Interrupt spells").SetValue(true));

                Config.AddSubMenu(new Menu("Drawings", "Drawings"));
                Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.AddToMainMenu();

                Drawing.OnDraw    += Drawing_OnDraw;
                Game.OnGameUpdate += Game_OnGameUpdate;
                Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            }
            catch (Exception)
            {
                Game.PrintChat("Error found in Cho_Gath. Refused to load.");
            }
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

            if (Config.Item("Killsteal").GetValue<bool>())
                ExecuteKillsteal();

            ExecuteAdditionals();
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("AutoInterrupt").GetValue<bool>() || !unit.IsValidTarget()) return;
            W.Cast(unit);
            Q.Cast(unit);
        }

        private static void ExecuteAdditionals()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var autoStack = Config.Item("AutoStack").GetValue<bool>();
            var count = 0;

            foreach (var buffs in ObjectManager.Player.Buffs.Where(buffs => buffs.DisplayName == "Feast"))
            {
                count = buffs.Count;
            }

            if (R.IsReady() && autoStack)
            foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(R.Range) && DamageLib.getDmg(minion, DamageLib.SpellType.R) > minion.Health).Where(minion => count < 6))
                R.CastOnUnit(minion);

            var autoQ1 = Config.Item("AutoQ1").GetValue<bool>();
            var autoQ2 = Config.Item("AutoQ2").GetValue<bool>();

            foreach (var champion in from champion in ObjectManager.Get<Obj_AI_Hero>() 
            where champion.IsValidTarget(Q.Range) let QPrediction = Q.GetPrediction(champion) 
            where (QPrediction.HitChance == Prediction.HitChance.Immobile && autoQ1) ||(QPrediction.HitChance == Prediction.HitChance.Dashing && autoQ2) select champion)
                Q.Cast(champion, true, true);
        }

        private static void ExecuteCombo()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            if (W.IsReady() && useW && ObjectManager.Player.Distance(target) <= W.Range)
                W.Cast(target, false, true);

            if (Q.IsReady() && useQ && ObjectManager.Player.Distance(target) <= Q.Range)
                Q.Cast(target, false, true);

            if (R.IsReady() && useR && DamageLib.getDmg(target, DamageLib.SpellType.R) > target.Health)
                R.CastOnUnit(target, true);
        }

        private static void ExecuteKillsteal()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(Q.Range)))
            {
                if (R.IsReady() && hero.Distance(ObjectManager.Player) <= R.Range &&
                    DamageLib.getDmg(hero, DamageLib.SpellType.R) > hero.Health)
                    R.CastOnUnit(hero, true);

                if (W.IsReady() && hero.Distance(ObjectManager.Player) <= W.Range &&
                    DamageLib.getDmg(hero, DamageLib.SpellType.W) > hero.Health)
                    W.CastIfHitchanceEquals(hero, Prediction.HitChance.HighHitchance, true);

                if (Q.IsReady() && hero.Distance(ObjectManager.Player) <= Q.Range &&
                    DamageLib.getDmg(hero, DamageLib.SpellType.Q) > hero.Health)
                    Q.CastIfHitchanceEquals(hero, Prediction.HitChance.HighHitchance, true);
            }
        }
    }
}