#region

/*
 * Credits to:
 * Eskor
 * Roach_
 * Both for helping me alot doing this Assembly and start On L# 
 * lepqm for cleaning my shit up

 */
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Annie
{
    internal class Program
    {
        //vars to be used
        public const string CharName = "Annie";
        //Orbwalker
        public static Orbwalking.Orbwalker Orbwalker;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
        public static int StunCount = 0;
        public static bool DoingCombo = false;
        public static SpellDataInst flash;

        //Menu
        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != CharName) return;
            flash = ObjectManager.Player.SummonerSpellbook.Spells.FirstOrDefault(spell => spell.Name == "SummonerFlash");
            //ObjectManager.Player.SummonerSpellbook.CastSpell(flash.Slot, )
            //Create the spells
            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W, 625);
            E = new Spell(SpellSlot.E, 0);
            R = new Spell(SpellSlot.R, 600);
            R1 = new Spell(SpellSlot.R, 900);

            W.SetSkillshot(0.60f, 625f, float.MaxValue, false, Prediction.SkillshotType.SkillshotCone);
            R.SetSkillshot(0.20f, 200f, float.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);
            R1.SetSkillshot(0.25f, 200f, float.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(R);
            SpellList.Add(R1);

            try
            {
                Config = new Menu(CharName, CharName, true);

                Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
                Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

                var targetSelectorMenu = new Menu("Target Selector", "Target Selector"); //to add on .dll update for all
                SimpleTs.AddToMenu(targetSelectorMenu);

                Config.AddSubMenu(targetSelectorMenu);
                Config.AddSubMenu(new Menu("Combo settings", "combo"));
                Config.SubMenu("combo").AddItem(new MenuItem("comboItems", "Use Items")).SetValue(true); //done
                Config.SubMenu("combo").AddItem(new MenuItem("flashCombo", "Targets needed to Flash -> R")).SetValue(new Slider(4, 5, 1)); //done

                Config.AddSubMenu(new Menu("Farm Settings", "lasthit"));
                Config.SubMenu("lasthit")
                    .AddItem(new MenuItem("qFarm", "Last hit with Disintegrate (Q)").SetValue(true)); //done
                Config.SubMenu("lasthit")
                    .AddItem(new MenuItem("wFarm", "Lane Clear with Incinerate (W)").SetValue(true)); // done
                Config.SubMenu("lasthit")
                    .AddItem(new MenuItem("saveqStun", "Don't Last Hit with Q while stun is up").SetValue(true)); //done
                Config.AddSubMenu(new Menu("Draw Settings", "draw"));
                Config.SubMenu("draw")
                    .AddItem(
                        new MenuItem("QDraw", "Draw Disintegrate (Q) Range").SetValue(new Circle(true,
                            Color.FromArgb(128, 178, 0, 0)))); //done
                Config.SubMenu("draw")
                    .AddItem(
                        new MenuItem("WDraw", "Draw Incinerate (W) Range").SetValue(new Circle(false,
                            Color.FromArgb(128, 32, 178, 170)))); //done
                Config.SubMenu("draw")
                    .AddItem(
                        new MenuItem("RDraw", "Draw Tibbers (R) Range").SetValue(new Circle(true,
                            Color.FromArgb(128, 128, 0, 128)))); //done
                Config.SubMenu("draw")
                    .AddItem(
                        new MenuItem("R1Draw", "Draw Flash -> R combo Range").SetValue(new Circle(true,
                            Color.FromArgb(128, 128, 0, 128)))); //done
                Config.AddItem(new MenuItem("PCast", "Packet Cast Spells").SetValue(true)); //done
                Config.AddToMainMenu();

                //Add the events we are going to use:
                Drawing.OnDraw += OnDraw;
                Game.OnGameUpdate += OnGameUpdate;
                GameObject.OnCreate += OnCreateObject;

                Game.PrintChat("Annie# Loaded");
            }
            catch
            {
                Game.PrintChat("Oops. Something went wrong with Annie#");
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var FlashRtarget = SimpleTs.GetTarget(900, SimpleTs.DamageType.Magical);
            StunCount = GetStunCount();

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo(target, FlashRtarget);
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Farm(false);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm(true);
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            Utility.DrawCircle(R1.GetPrediction(SimpleTs.GetTarget(900, SimpleTs.DamageType.Magical)).CastPosition, 250, Color.Aquamarine);
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Draw").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly || !(sender is Obj_SpellMissile)) return;

            var missile = (Obj_SpellMissile)sender;
            if (!(missile.SpellCaster is Obj_AI_Hero) || !(ObjectManager.Player.Distance(missile.EndPosition) <= 35))
                return;

            if (E.IsReady())
            {
                E.Cast(ObjectManager.Player);
            }
            else if (!ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(missile.SpellCaster.NetworkId).IsMelee())
            {
                var ecd = (int)(ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).CooldownExpires - Game.Time) * 1000;
                if ((int)Vector3.Distance(missile.Position, ObjectManager.Player.ServerPosition) /
                    ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(missile.SpellCaster.NetworkId)
                        .BasicAttack.MissileSpeed * 1000 > ecd)
                {
                    Utility.DelayAction.Add(ecd, () => E.CastOnUnit(ObjectManager.Player));
                }
            }
        }

        private static void Combo(Obj_AI_Hero target, Obj_AI_Hero FlashRtarger)
        {
            if ((target == null && FlashRtarger == null) || DoingCombo || (!Q.IsReady() && !W.IsReady() && !R.IsReady())) return;
            if (Config.Item("comboItems").GetValue<bool>()) // DFG Ussage
                Items.UseItem(3128, target);
            Orbwalker.SetAttacks(false);
            //bool FlashRComboIsReady = flash != null && flash.Slot != SpellSlot.Unknown && flash.State == SpellState.Ready && R.IsReady() && StunCount == 4; //kappa
            //bool hasTibbers = ObjectManager.Player.HasBuff("infernalguardiantimer", true);
            switch (StunCount)
            {
                case 3:
                    if (Q.IsReady())
                    {
                        DoingCombo = true;
                        Q.CastOnUnit(target, Config.Item("PCast").GetValue<bool>());
                        //Stack only goes up when Q hits
                        Utility.DelayAction.Add(
                            (int)(ObjectManager.Player.Distance(target) / Q.Speed * 1000 - 100 - Game.Ping/2),
                            () =>
                            {
                                if (R.IsReady() &&
                                    !(DamageLib.getDmg(target, DamageLib.SpellType.R) * 0.6 > target.Health))
                                    R.Cast(target, false, true);

                                DoingCombo = false;
                            });
                    }
                    else if (W.IsReady())
                        DoingCombo = true;
                    W.Cast(target, false, true); //stack only goes up after 650 secs
                    Utility.DelayAction.Add(650 - 100 - Game.Ping,
                        () =>
                        {
                            if (R.IsReady() && !(DamageLib.getDmg(target, DamageLib.SpellType.R) * 0.6 > target.Health))
                                R.Cast(target, false, true);

                            DoingCombo = false;
                        });
                    break;
                case 4:
                    Console.WriteLine("0");
                    if (flash != null && flash.State == SpellState.Ready &&
                        R.IsReady() && target == null)
                    {
                        //Console.WriteLine("1a");
                        //Console.WriteLine((ObjectManager.Player.Distance(FlashRtarger).ToString()));
                        Console.WriteLine(Config.Item("flashCombo").GetValue<Slider>().Value.ToString() + GetEnemiesInRange(R1.GetPrediction(FlashRtarger, true).CastPosition, 250));
                        var position = R1.GetPrediction(FlashRtarger, true).CastPosition;
                        if (ObjectManager.Player.Distance(FlashRtarger) > 600 && GetEnemiesInRange(position, 250) >= Config.Item("flashCombo").GetValue<Slider>().Value)
                        {
                            Console.WriteLine("1aa");
                            ObjectManager.Player.SummonerSpellbook.CastSpell(flash.Slot, position);
                            R.Cast(FlashRtarger, false, true);
                            if (W.IsReady())
                                W.Cast(FlashRtarger, false, true);
                        }

                    }
                    else
                    {
                        Console.WriteLine("2");
                        if (R.IsReady() && !(DamageLib.getDmg(target, DamageLib.SpellType.R) * 0.6 > target.Health))
                            R.Cast(target, false, true);
                        if (W.IsReady())
                            W.Cast(target, false, true);
                        if (Q.IsReady())
                            Q.Cast(target, false, true); 
                    }

                    break;
                default:
                    if (Q.IsReady())
                        Q.CastOnUnit(target, Config.Item("PCast").GetValue<bool>());
                    if (W.IsReady())
                        W.Cast(target, false, true);
                    break;
            }


            Orbwalker.SetAttacks(true);
        }

        private static void Farm(bool laneclear)
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var jungleMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            minions.AddRange(jungleMinions);
            if (laneclear && Config.Item("wFarm").GetValue<bool>() && W.IsReady())
                if (minions.Count > 0)
                {
                    W.Cast(W.GetLineFarmLocation(minions).Position.To3D()); 
                }
            if (!Config.Item("qFarm").GetValue<bool>() || (Config.Item("saveqStun").GetValue<bool>() && StunCount == 4) ||
                !Q.IsReady()) return;

            foreach (
                var minion in
                    minions.OrderByDescending(Minions => Minions.MaxHealth).Where(
                        minion =>
                            minion.IsValidTarget(Q.Range)))
            {
                var predictedHealth = Q.GetHealthPrediction(minion);
                if (predictedHealth < DamageLib.getDmg(minion, DamageLib.SpellType.Q) * 0.9 && predictedHealth > 0)
                    Q.CastOnUnit(minion, Config.Item("PCast").GetValue<bool>());
            }
        }

        private static int GetStunCount()
        {
            foreach (
                var buff in
                    ObjectManager.Player.Buffs.Where(
                        buff => buff.Name == "pyromania" || buff.Name == "pyromania_particle"))
            {
                switch (buff.Name)
                {
                    case "pyromania":
                        return buff.Count;
                    case "pyromania_particle":
                        return 4;
                }
            }

            return 0;
        }

        private static int GetEnemiesInRange(Vector3 pos, float range)
        {
            //var Pos = pos;
            return ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team).Count(hero => Vector3.Distance(pos, hero.ServerPosition) <= range);
        }
    }
}