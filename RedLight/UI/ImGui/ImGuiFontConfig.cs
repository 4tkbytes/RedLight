// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Hexa.NET.ImGui;

#if GL
namespace RedLight.UI.ImGui
#elif GLES
namespace RedLight.UI.ImGui
#elif LEGACY
namespace RedLight.UI.ImGui
#endif
{
    public readonly struct ImGuiFontConfig
    {
        public ImGuiFontConfig(string fontPath, float fontSize, Func<ImGuiIOPtr, IntPtr> getGlyphRange = null)
        {
            if (fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
            FontPath = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
            FontSize = fontSize;
            GetGlyphRange = getGlyphRange;
        }

        public string FontPath { get; }
        public float FontSize { get; }
        public Func<ImGuiIOPtr, IntPtr> GetGlyphRange { get; }
    }
}
