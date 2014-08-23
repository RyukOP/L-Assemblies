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
            {
                var dangerColor = ObjectManager.Player.Distance(target) < target.AttackRange + target.BoundingRadius ? Color.Red : Color.LimeGreen;
                Drawing.DrawCircle(target.Position, target.AttackRange + target.BoundingRadius, dangerColor);
            }
        }
    }
}
