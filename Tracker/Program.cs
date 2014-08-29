#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;

#endregion

namespace Tracker
{
    
    internal class Program
    {
        public static Menu Config;

        static void Main(string[] args)
        {
            Config = new Menu("Tracker", "Tracker", true);
            HbTracker.AttachToMenu(Config);
            WardTracker.AttachToMenu(Config);
            Config.AddToMainMenu();
        }
    }

}
