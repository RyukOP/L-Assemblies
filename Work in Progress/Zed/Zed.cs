#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Zed
{
    internal class Zed
    {
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;

        internal Zed()
        {
            _q = new Spell(SpellSlot.Q, 925f);
            _w = new Spell(SpellSlot.W, 600f);
            _e = new Spell(SpellSlot.E, 325f);
            _r = new Spell(SpellSlot.R, 650f);

            _q.SetSkillshot(0.25f, 50f, 1700f, false, Prediction.SkillshotType.SkillshotLine);

            // Just menu things
            Config = new Menu(ObjectManager.Player.ChampionName, ObjectManager.Player.ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static Obj_AI_Minion Shadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");
            }
        }

        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!_w.IsReady()) return ShadowCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Shadow != null)
                Utility.DrawCircle(Shadow.ServerPosition, Shadow.BoundingRadius * 2, Color.MistyRose);

            foreach (var vector in GetPossibleShadowPositions())
                Utility.DrawCircle(vector, 50f, Color.RoyalBlue);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(2000)))
            {
                CastQ(hero);
                CastE();
            }
        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (!_q.IsReady()) return;

            _q.UpdateSourcePosition(ObjectManager.Player.ServerPosition, ObjectManager.Player.ServerPosition);
            if (_q.Cast(target, false, true) == Spell.CastStates.SuccessfullyCasted)
                return;

            if (Shadow != null)
            {
                _q.UpdateSourcePosition(Shadow.ServerPosition, Shadow.ServerPosition);
                _q.Cast(target, false, true);
            }

            if (ShadowStage == ShadowCastStage.First &&
                ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost)
            {
                foreach (
                    var castPosition in
                        GetPossibleShadowPositions()
                            .OrderBy(castPosition => castPosition.Distance(target.ServerPosition)))
                {
                    _q.UpdateSourcePosition(castPosition, castPosition);

                    if (_q.WillHit(target, target.ServerPosition))
                    {
                        if (LastCastedSpell.LastCastPacketSent.Slot != SpellSlot.W)
                            _w.Cast(castPosition);

                        var position = castPosition;
                        Utility.DelayAction.Add(250, () =>
                        {
                            _q.UpdateSourcePosition(position, position);
                            _q.Cast(target, false, true);
                        });

                        if (ShadowStage != ShadowCastStage.First)
                            return;
                    }
                }
            }
        }

        private static void CastE()
        {
            if (ObjectManager.Get<Obj_AI_Hero>()
                .Count(
                    hero =>
                        hero.IsValidTarget() &&
                        (hero.Distance(ObjectManager.Player.ServerPosition) <= _e.Range ||
                         (Shadow != null && hero.Distance(Shadow.ServerPosition) <= _e.Range))) > 0)
                _e.Cast();
        }

        private static IEnumerable<Vector3> GetPossibleShadowPositions()
        {
            var pointList = new List<Vector3>();

            for (var j = _w.Range; j >= 50; j -= 100)
            {
                var offset = (int)(2 * Math.PI * j / 100);

                for (var i = 0; i <= offset; i++)
                {
                    var angle = i * Math.PI * 2 / offset;
                    var point = new Vector3((float)(ObjectManager.Player.Position.X + j * Math.Cos(angle)),
                        (float)(ObjectManager.Player.Position.Y - j * Math.Sin(angle)),
                        ObjectManager.Player.Position.Z);

                    if (!NavMesh.GetCollisionFlags(point).HasFlag(CollisionFlags.Wall))
                        pointList.Add(point);
                }
            }

            return pointList;
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }
    }
}