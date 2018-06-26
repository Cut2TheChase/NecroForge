using System;
using System.Collections.Generic;
using Necro;
using HBS.Text;
using HBS.Collections;

namespace NecroForge
{
    public static class Utils
    {
        public static Action<string, int> NetworkAddIngredient;

        public static Action<Equippable> RemoveEquipped;
    }
}
