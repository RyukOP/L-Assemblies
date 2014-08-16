#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Darius
{
    internal class Program
    {
        private const string ChampionName = "Darius";
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Menu _config;

        public static SpellSlot IgniteSlot;
        public static Items.Item Hydra;
        public static Items.Item Tiamat;


        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 425);
            _w = new Spell(SpellSlot.W, 145);
            _e = new Spell(SpellSlot.E, 540);
            _r = new Spell(SpellSlot.R, 460);

            SpellList.Add(_q);
            SpellList.Add(_w);
            SpellList.Add(_e);
            SpellList.Add(_r);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            Tiamat = new Items.Item(3077, 375);
            Hydra = new Items.Item(3074, 375);

            _config = new Menu("Darius", "Darius", true);

            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseICombo", "Use Items").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("Killsteal", "Killsteal").SetValue(true));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(88, KeyBindType.Press)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0],
                        KeyBindType.Toggle)));

            _config.AddSubMenu(new Menu("Drawings", "Drawings"));
            _config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            _config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            _config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            _config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (SpellList == null) return;

            foreach (var spell in SpellList)
            {
                var menuItem = _config.Item(spell.Slot + "Range").GetValue<Circle>();

                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_config.Item("ComboActive").GetValue<KeyBind>().Active)
                ExecuteSkills();

            if (_config.Item("Killsteal").GetValue<bool>())
                ExecuteKillsteal();

            if ((_config.Item("HarassActive").GetValue<KeyBind>().Active) ||
                (_config.Item("HarassActiveT").GetValue<KeyBind>().Active))
                ExecuteHarass();
        }

        private static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (_config.Item("ComboActive").GetValue<KeyBind>().Active && _config.Item("UseWCombo").GetValue<bool>() &&
                unit.IsMe && (target is Obj_AI_Hero))
                _w.Cast();

            if (_config.Item("ComboActive").GetValue<KeyBind>().Active && _config.Item("UseICombo").GetValue<bool>() &&
                unit.IsMe && (target is Obj_AI_Hero) && !_w.IsReady())
            {
                Items.UseItem(Items.HasItem(3077) ? 3077 : 3074);
            }
        }

        private static void CastR(Obj_AI_Base target)
        {
            if (!target.IsValidTarget(_r.Range) || !_r.IsReady()) return;

            if (!(DamageLib.getDmg(target, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) > target.Health))
            {
                foreach (var buff in target.Buffs)
                {
                    if (buff.Name == "dariushemo")
                    {
                        if (DamageLib.getDmg(target, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) *
                            (1 + buff.Count / 5) - 1 > target.Health)
                        {
                            _r.CastOnUnit(target, true);
                        }
                    }
                }
            }
            else if (DamageLib.getDmg(target, DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) - 15 >
                     target.Health)
            {
                _r.CastOnUnit(target, true);
            }
        }

        private static void ExecuteHarass()
        {
            if (!_config.Item("UseQHarass").GetValue<bool>() || !_q.IsReady()) return;

            var c =
                (from hero in ObjectManager.Get<Obj_AI_Hero>()
                    where hero.IsValidTarget()
                    select ObjectManager.Player.Distance(hero)).Count(dist => dist > 270 && dist <= _q.Range);

            if (c > 0)
                _q.Cast();
        }

        private static void ExecuteKillsteal()
        {
            foreach (var champion in ObjectManager.Get<Obj_AI_Hero>())
            {
                CastR(champion);
                if (_r.IsReady()) continue;

                if (IgniteSlot != SpellSlot.Unknown &&
                    ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                    DamageLib.getDmg(champion, DamageLib.SpellType.IGNITE) - 5 > champion.Health)
                    ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, champion);
            }
        }

        private static void ExecuteSkills()
        {
            var target = SimpleTs.GetTarget(_e.Range, SimpleTs.DamageType.Physical);
            if (target == null) return;

            if (_e.IsReady() && _config.Item("UseECombo").GetValue<bool>() &&
                ObjectManager.Player.Distance(target) <= _e.Range)
                _e.Cast(target.ServerPosition);

            if (_q.IsReady() && _config.Item("UseQCombo").GetValue<bool>() &&
                ObjectManager.Player.Distance(target) <= _q.Range)
                _q.Cast();

            if (_r.IsReady() && _config.Item("UseRCombo").GetValue<bool>() &&
                ObjectManager.Player.Distance(target) <= _r.Range)
                CastR(target);

            if (_r.IsReady()) return;
            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                DamageLib.getDmg(target, DamageLib.SpellType.IGNITE) - 5 > target.Health)
                ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
        }
    }
}