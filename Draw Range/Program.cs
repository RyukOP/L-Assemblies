using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Draw_Range
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.IsValidTarget(1000)))
                Drawing.DrawCircle(target.Position, Orbwalking.GetRealAutoAttackRange(target), ObjectManager.Player.Distance(target) < Orbwalking.GetRealAutoAttackRange(target) ? Color.Red : Color.LimeGreen);
        }
    }
}
