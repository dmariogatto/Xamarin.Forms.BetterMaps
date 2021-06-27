﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Xamarin.Forms.BetterMaps
{
    public class MapClickedEventArgs
    {
        public Position Position { get; }

        public MapClickedEventArgs(Position position)
        {
            Position = position;
        }
    }
}