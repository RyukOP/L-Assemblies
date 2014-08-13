using System.Linq;

#region

using System;
using System.Collections.Generic;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

//Credits: TC Crew :^ )

namespace Katarina
{
    internal class Program
    {
        public const string ChampionName = "Katarina";

        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SpellSlot IgniteSlot;

        public static Items.Item DFG;

        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 675);
            W = new Spell(SpellSlot.W, 375);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 500);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            DFG = Utility.Map.GetMap() == Utility.Map.MapType.TwistedTreeline || Utility.Map.GetMap() == Utility.Map.MapType.CrystalScar ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            Config = new Menu(ChampionName, ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("Killsteal", "Enable killsteal").SetValue(false));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));


            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(88, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("UseQFarm", "Use Q").SetValue(
                        new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 2)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("UseWFarm", "Use W").SetValue(
                        new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 2)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("FreezeActive", "Freeze!").SetValue(
                        new KeyBind(Config.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.PrintChat(ChampionName + " Combo enabled!");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();

                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.Item("Killsteal").GetValue<bool>())
                ExecuteKillsteal();

            if (ObjectManager.Player.IsChannelingImportantSpell()) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active ||
                Config.Item("HarassActive").GetValue<KeyBind>().Active)
                ExecuteSkills();

            var lc = Config.Item("LaneClearActive").GetValue<KeyBind>().Active;
            if (lc || Config.Item("FreezeActive").GetValue<KeyBind>().Active)
                Farm(lc);
        }


        private static void ExecuteKillsteal()
        {
            var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;

            if (ObjectManager.Player.Distance(target) < Q.Range && Q.IsReady() &&
                DamageLib.getDmg(target, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage) - 20 > target.Health)
                Q.CastOnUnit(target, true);

            if (ObjectManager.Player.Distance(target) < E.Range && E.IsReady() &&
                DamageLib.getDmg(target, DamageLib.SpellType.E) - 20 > target.Health)
                E.CastOnUnit(target, true);

            if (ObjectManager.Player.Distance(target) < W.Range && W.IsReady() &&
                DamageLib.getDmg(target, DamageLib.SpellType.W) - 20 > target.Health)
                W.Cast();

            if (ObjectManager.Player.Distance(target) < E.Range && E.IsReady() && W.IsReady() &&
                DamageLib.getDmg(target, DamageLib.SpellType.E) + DamageLib.getDmg(target, DamageLib.SpellType.W) - 20 >
                target.Health)
            {
                E.CastOnUnit(target, true);
                if (ObjectManager.Player.Distance(target) < W.Range)
                {
                    W.Cast();
                }
            }

            if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && ObjectManager.Player.Distance(target) < 600 && DamageLib.getDmg(target, DamageLib.SpellType.IGNITE) > target.Health)
                ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
        }

        private static void ExecuteSkills()
        {
            var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;

            if ((GetDamage(target) > target.Health))
            {
                if (ObjectManager.Player.Distance(target) < E.Range && DFG.IsReady())
                    DFG.Cast(target);

                if (ObjectManager.Player.Distance(target) < Q.Range && Q.IsReady())
                    Q.CastOnUnit(target, true);

                if (ObjectManager.Player.Distance(target) < E.Range && E.IsReady())
                    E.CastOnUnit(target, true);

                if (ObjectManager.Player.Distance(target) < W.Range && W.IsReady())
                    W.Cast();

                if (ObjectManager.Player.Distance(target) < R.Range && R.IsReady())
                    R.Cast();

                if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                    ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);

            }
            else if (!(GetDamage(target) > target.Health))
            {
                if (ObjectManager.Player.Distance(target) < Q.Range && Q.IsReady())
                    Q.CastOnUnit(target, true);

                if (Config.Item("ComboActive").GetValue<KeyBind>().Active &&
                    ObjectManager.Player.Distance(target) < E.Range && E.IsReady())
                    E.CastOnUnit(target, true);

                if (ObjectManager.Player.Distance(target) < W.Range && W.IsReady())
                    W.Cast();
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalking.CanMove(40)) return;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var useQi = Config.Item("UseQFarm").GetValue<StringList>().SelectedIndex;
            var useWi = Config.Item("UseWFarm").GetValue<StringList>().SelectedIndex;
            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useW = (laneClear && (useWi == 1 || useWi == 2)) || (!laneClear && (useWi == 0 || useWi == 2));

            if (useQ && Q.IsReady())
            {
                foreach (var minion in allMinions.Where(minion => minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion, (int)(ObjectManager.Player.Distance(minion) * 1000 / 1400))
                < 0.75 * DamageLib.getDmg(minion, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage)))
                {
                    Q.Cast(minion);
                    return;
                }
            }
            else if (useW && W.IsReady())
            {
                if (!allMinions.Any(minion => minion.IsValidTarget(W.Range) && minion.Health < 0.75*DamageLib.getDmg(minion, DamageLib.SpellType.W))) return;
                W.Cast();
                return;
            }

            if (!laneClear) return;
            foreach (var minion in allMinions)
            {
                if (useQ)
                    Q.Cast(minion);

                if (useW && ObjectManager.Player.Distance(minion) < W.Range)
                    W.Cast(minion);
            }
        }

        private static double GetDamage(Obj_AI_Base unit)
        {
            double damage = 0;
            if (Q.IsReady()) damage += DamageLib.getDmg(unit, DamageLib.SpellType.Q);
            if (W.IsReady()) damage += DamageLib.getDmg(unit, DamageLib.SpellType.W);
            if (E.IsReady()) damage += DamageLib.getDmg(unit, DamageLib.SpellType.E);
            if (R.IsReady()) damage += DamageLib.getDmg(unit, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) * 7;
            if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready) damage += DamageLib.getDmg(unit, DamageLib.SpellType.IGNITE);
            return damage * (DFG.IsReady() ? 1.2f : 1);
        }
    }
}