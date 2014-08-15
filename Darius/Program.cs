using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Darius
{
    class Program
    {
        private const string ChampionName = "Darius";
        private static Orbwalking.Orbwalker Orbwalker;
        private static readonly List<Spell> SpellList = new List<Spell>();
        private static Spell Q, W, E, R;
        private static Menu Config;

        public static SpellSlot IgniteSlot;
        public static Items.Item HYDRA;
        public static Items.Item TIAMAT;


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName)
                return;

           Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            try
            {
                Q = new Spell(SpellSlot.Q, 425);
                W = new Spell(SpellSlot.W, 145);
                E = new Spell(SpellSlot.E, 540);
                R = new Spell(SpellSlot.R, 460);

                SpellList.Add(Q);
                SpellList.Add(W);
                SpellList.Add(E);
                SpellList.Add(R);

                IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
                TIAMAT = new Items.Item(3077, 375);
                HYDRA  = new Items.Item(3074, 375);

                Config = new Menu("Darius", "Darius", true);

                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

                Config.AddSubMenu(new Menu("Combo", "Combo"));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseICombo", "Use Items").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("Killsteal", "Killsteal").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));


                Config.AddSubMenu(new Menu("Drawings", "Drawings"));
                Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.AddToMainMenu();

                Drawing.OnDraw += Drawing_OnDraw;
                Game.OnGameUpdate += Game_OnGameUpdate;

            }
            catch (Exception)
            {
                Game.PrintChat("Error found in Darius. Refused to load.");
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
                ExecuteSkills();

            if (Config.Item("Killsteal").GetValue<bool>())
                ExecuteKillsteal();
        }

        private static void CastR(Obj_AI_Base target)
        {
            if (target.IsValidTarget(R.Range))
            {
                foreach (
                    var buff in
                        target.Buffs.Where(
                            buff =>
                                (buff.Name == "dariushemo" &&
                                 (DamageLib.getDmg(target, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage)*
                                  (1 + buff.Count/5) > target.Health + (target.FlatHPRegenMod/5)) ||
                                 DamageLib.getDmg(target, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) >
                                 target.Health + (target.FlatHPRegenMod/5))))
                {
                    R.Cast(target, true);
                }
            }
        }

        private static void ExecuteSkills()
        {
            var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            if (E.IsReady() && useE && ObjectManager.Player.Distance(target) <= E.Range)
                E.Cast(target.ServerPosition);

            if (Q.IsReady() && useQ && ObjectManager.Player.Distance(target) <= Q.Range)
                Q.Cast();

            if (R.IsReady() && useR && ObjectManager.Player.Distance(target) <= R.Range)
                CastR(target);

            if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && DamageLib.getDmg(target, DamageLib.SpellType.IGNITE)
            > target.Health)
            {
                ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
            }
        }

        private static void ExecuteKillsteal()
        {
            foreach (var champion in ObjectManager.Get<Obj_AI_Hero>())
            {
                CastR(champion);

                if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && DamageLib.getDmg(champion, DamageLib.SpellType.IGNITE)
                > champion.Health && champion.Distance(ObjectManager.Player.ServerPosition) < 600)
                {
                    ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, champion);
                }
            }
        }

        static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active && Config.Item("UseWCombo").GetValue<bool>() && unit.IsMe && (target is Obj_AI_Hero))
                W.Cast();

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active && Config.Item("UseICombo").GetValue<bool>() &&
                unit.IsMe && (target is Obj_AI_Hero) && !W.IsReady())
            {
                Items.UseItem(Items.HasItem(3077) ? 3077 : 3074);
            }
        }
    }
}
