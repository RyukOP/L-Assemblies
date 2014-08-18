#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Cassio
{
    internal class Program
    {
        private const string ChampionName = "Cassiopeia";
        private static Orbwalking.Orbwalker Orbwalker;
        private static readonly List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static Menu Config;

        private static SpellSlot IgniteSlot;

        private static void Main(string[] args)
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

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            const double ultAngle = 80 * Math.PI / 180;
            const float fUltAngle = (float)ultAngle;

            Q.SetSkillshot(0.60f, 75f, int.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.50f, 106f, 2500f, false, Prediction.SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.30f, fUltAngle, int.MaxValue, false, Prediction.SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);

            Config = new Menu(ChampionName, ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
        }

        private static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] != Packet.C2S.Cast.Header) return;

            var decodedPacket = Packet.C2S.Cast.Decoded(args.PacketData);
            if (decodedPacket.SourceNetworkId != ObjectManager.Player.NetworkId || decodedPacket.Slot != SpellSlot.R)
                return;

            if (ObjectManager.Get<Obj_AI_Hero>().Count(hero => R.WillHit(hero, R.GetPrediction(hero).CastPosition)) == 0)
                args.Process = false;
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            args.Process = (!Q.IsReady() && !W.IsReady() && !E.IsReady());
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

        private static bool IsPoisoned(Obj_AI_Base unit)
        {
            return
                unit.Buffs.Where(buff => buff.IsActive && buff.Type == BuffType.Poison)
                    .Any(buff => buff.EndTime >= (Game.Time + 0.35 + 700 / 1900));
        }

        private static void ExecuteCombo()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (target == null) return;

            if (Q.IsReady(2000) && E.IsReady(2000) && R.IsReady() && DamageLib.IsKillable(target,
                new[]
                {
                    DamageLib.SpellType.Q, DamageLib.SpellType.W, DamageLib.SpellType.E, DamageLib.SpellType.E,
                    DamageLib.SpellType.R, DamageLib.SpellType.R, DamageLib.SpellType.IGNITE
                }))
            {
                if (IgniteSlot != SpellSlot.Unknown &&
                    ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                    ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);

                if (ObjectManager.Player.Distance(target) <= R.Range + R.Width)
                    R.Cast(target, true, true);

                if (ObjectManager.Player.Distance(target) <= W.Range + W.Width)
                    W.Cast(target, true, true);

                if (ObjectManager.Player.Distance(target) <= Q.Range + Q.Width)
                    Q.Cast(target, true, true);

                if (ObjectManager.Player.Distance(target) <= E.Range + target.BoundingRadius && IsPoisoned(target) ||
                    DamageLib.IsKillable(target, new[] { DamageLib.SpellType.E }))
                    E.CastOnUnit(target, true);
            }
            else
            {
                if (W.IsReady() && ObjectManager.Player.Distance(target) <= W.Range + W.Width)
                    W.Cast(target, true, true);

                if (Q.IsReady() && ObjectManager.Player.Distance(target) <= Q.Range + Q.Width)
                    Q.Cast(target, true, true);

                if (E.IsReady() && ObjectManager.Player.Distance(target) <= E.Range + target.BoundingRadius &&
                    IsPoisoned(target) || DamageLib.IsKillable(target, new[] { DamageLib.SpellType.E }))
                    E.CastOnUnit(target, true);
            }
        }
    }
}