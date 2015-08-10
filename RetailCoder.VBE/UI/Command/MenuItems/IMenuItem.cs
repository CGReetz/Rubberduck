﻿using System;
using System.Drawing;

namespace Rubberduck.UI.Command.MenuItems
{
    public interface IMenuItem
    {
        string Key { get; }
        Func<string> Caption { get; }
        bool BeginGroup { get; }
        int DisplayOrder { get; }
    }

    public interface ICommandMenuItem : IMenuItem
    {
        ICommand Command { get; }
        Image Image { get; }
        Image Mask { get; }
    }
}