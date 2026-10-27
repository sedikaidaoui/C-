using System;
using System.Collections.Generic;
using System.Linq;

namespace AotForms
{
    public static class GUNICON
    {
        private static readonly string[] GunNames;

        static GUNICON()
        {
            GunNames = new string[100000];
            for (int i = 0; i < GunNames.Length; i++)
            {
                GunNames[i] = string.Empty;
            }

            GunNames[0] = "\ue074";
            GunNames[1] = "\ue005"; // FIST
            GunNames[2] = "\ue075"; // M4A1
            GunNames[3] = "\ue04e"; // USP
            GunNames[4] = "\ue04f"; // AWM
            GunNames[5] = "\ue059"; // M1014
            GunNames[6] = "\ue074"; // AK47
            GunNames[7] = "\ue063"; // UMP
            GunNames[8] = "\ue061"; // MP5
            GunNames[9] = "\ue051"; // Desert Eagle
            GunNames[10] = "\ue052"; // G18
            GunNames[11] = "\ue079"; // M14
            GunNames[12] = "\ue078"; // SCAR
            GunNames[13] = GunNames[62] = GunNames[124] = GunNames[125] = "\ue065"; // VSS
            GunNames[14] = GunNames[70] = "\ue07b"; // Groza
            GunNames[15] = "\ue064"; // MP40
            GunNames[16] = "\ue04a"; // Pan
            GunNames[17] = "\ue04b"; // Dao
            GunNames[18] = "\ue071"; // SKS
            GunNames[19] = "\ue06f"; // M249
            GunNames[20] = "\ue057"; // M1873
            GunNames[21] = "\ue050"; // Kar98K
            GunNames[64] = "\ue050"; // "Kar98K-I";
            GunNames[128] = "\ue050"; // "Kar98K-II";
            GunNames[129] = "\ue050"; // "Kar98K-III";
            GunNames[24] = "\ue07a"; // FAMAS
            GunNames[25] = "\ue058"; // M500
            GunNames[26] = "\ue070"; // SVD
            GunNames[27] = "\ue04c"; // Baseball Bat
            GunNames[28] = "\ue077"; // XM8
            GunNames[29] = "\ue05a"; // SPAS12
            GunNames[30] = "\ue06e"; // M60
            GunNames[33] = "\ue07d"; // AN94
            GunNames[34] = "\ue04d"; // Katana
            GunNames[35] = "\ue076"; // CG15
            GunNames[39] = "\ue07c"; // Flash-MA
            GunNames[41] = "\ue05d"; // M1887
            GunNames[43] = "\ue06b"; // Thompson
            GunNames[45] = "\ue053"; // M82B
            GunNames[46] = "\ue076"; // AUG
            GunNames[47] = "\ue07f"; // Parafal
            GunNames[48] = "\ue073"; // Woodpecker
            GunNames[49] = "\ue06a"; // Vector
            GunNames[50] = "\ue05b"; // MAG-7
            GunNames[51] = "\ue04d"; // Scythe
            GunNames[56] = "\ue060"; // USP-2
            GunNames[57] = "\ue07e"; // Kingfisher
            GunNames[58] = "\ue062"; // Mini Uzi
            GunNames[65] = "\ue04f"; // AWM-Y
            GunNames[71] = "\ue06f"; // M249X
            GunNames[72] = "\ue070"; // SVD-Y
            GunNames[73] = "\ue087"; // G36
            GunNames[74] = "\ue087"; // G36
            GunNames[75] = "\ue054"; // M24
            GunNames[86] = "\ue05f"; // Charge Buster
            GunNames[88] = "\ue06d"; // MAC10
            GunNames[89] = "\ue072"; // AC80
            GunNames[93] = "\ue066"; // Treatment Gun
            GunNames[100] = "\ue068"; // Flamethrower
            GunNames[119] = "\ue05d"; // M1887X
            GunNames[150] = "\ue06c"; // Bizon
            GunNames[181] = "\ue05e"; // Trogon
            GunNames[197] = "\ue056"; // VSK94
            GunNames[178] = GunNames[179] = GunNames[180] = "\ue078"; // SCAR variations
            GunNames[184] = GunNames[185] = GunNames[186] = "\ue059"; // M1014 variations
            GunNames[193] = GunNames[194] = GunNames[195] = "\ue076"; // AUG variations
            GunNames[228] = GunNames[229] = GunNames[230] = "\ue06d"; // MAC10 variations
            GunNames[60] = GunNames[120] = GunNames[121] = "\ue061"; // MP5 variations
            GunNames[80] = GunNames[81] = GunNames[82] = "\ue075"; // M4A1 variations
            GunNames[61] = GunNames[122] = GunNames[123] = "\ue06e"; // M60 variations
            GunNames[67] = GunNames[130] = GunNames[131] = "\ue07a"; // FAMAS variations
            GunNames[63] = GunNames[126] = GunNames[127] = "\ue079"; // M14 variations
            GunNames[32] = "\ue069"; // P90
            GunNames[21002] = "\ue084"; // M590
            GunNames[21001] = "\ue066"; // Treatment Gun Y
            GunNames[602] = "\ue082"; // Smoke Grenade
            GunNames[608] = "\ue083";
            GunNames[601] = "\ue080"; // Grenade
            GunNames[1201] = "\ue081"; // Gloo Wall
            GunNames[1204] = "\ue081"; // Gloo Wall
            GunNames[10449] = "TRIPLE SHURIKEN";
            GunNames[21003] = "HEAVY SHURIKEN";
            GunNames[21004] = "LIGHT SHURIKEN";
            GunNames[55] = "\ue05c";
            GunNames[149] = "\ue1f4";
        }

        // for knife = "\ue1f4"; // Unicode for knife icon
        // for m1917 = "\ue05c";

        public static List<string> GetAllGunIds()
        {
            var result = new List<string>();

            for (short i = 0; i < GunNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(GunNames[i]))
                    result.Add($"{i} - {GunNames[i]}");
                else
                    result.Add($"{i}"); // show just the ID if no name is mapped
            }

            return result.Distinct().ToList();
        }

        public static string GetGunName(short gunId)
        {
            try
            {
                switch (gunId)
                {
                    case 1201:  return "\ue081";  // Gloo Wall
                    case 601:   return "\ue080";  // Grenade
                    case 603:   return "\ue086";  // Smoke Grenade
                    case 21001: return "\ue066";  // Treatment Gun Y
                    case 21002: return "\ue084";  // M590
                }

                if (gunId >= 0 && gunId < GunNames.Length && !string.IsNullOrEmpty(GunNames[gunId]))
                    return GunNames[gunId];

                // If not matched, return the gun ID itself
                return $"ID:{gunId}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting gun name: {ex.Message}");
                return $"ID:{gunId}";
            }
        }
    }
}
