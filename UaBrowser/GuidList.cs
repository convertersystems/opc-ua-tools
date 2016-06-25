// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.UaBrowser
{
    internal static class GuidList
    {
        public const string GuidUaBrowserPkgString = "3ebf0b3e-abaa-4161-a990-435fa37f8c69";
        public const string GuidUaBrowserCmdSetString = "0076ad9f-bb50-4305-9361-516301194ea5";
        public const string GuidToolWindowPersistanceString = "a0afbf3c-c410-4b90-b4be-2955e7088d13";
        public const string GuidOptionsDialogPersistanceString = "7BBDD0E3-9181-4356-8338-926538B97870";
        public static readonly Guid GuidUaBrowserCmdSet = new Guid(GuidUaBrowserCmdSetString);
    }
}