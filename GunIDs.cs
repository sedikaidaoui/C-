using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AotForms
{
    internal class GunIDs
    {
        private static readonly string[] GunIds;
        static GunIDs()
        {
            GunIds = new string[100000];
            for (int i = 0; i < GunIds.Length; i++)
            {
                GunIds[i] = string.Empty;
            }

            GunIds[1] = "FIST";
            GunIds[2] = "M4A1";
            GunIds[3] = "USP";
            GunIds[4] = "AWM";
            GunIds[5] = "M1014";
            GunIds[6] = "AK47";
            GunIds[7] = "UMP";
            GunIds[8] = "MP5";
            GunIds[9] = "DESERT EAGLE";
            GunIds[10] = "G18";
            GunIds[11] = "M14";
            GunIds[12] = "SCAR";
            GunIds[13] = GunIds[62] = GunIds[124] = GunIds[125] = "VSS";
            GunIds[14] = GunIds[70] = "Groza";
            GunIds[15] = "MP40";
            GunIds[16] = "Pan";
            GunIds[17] = "PARANG";
            GunIds[18] = "SKS";
            GunIds[19] = "M249";
            GunIds[20] = "M1873";
            GunIds[21] = "Kar98K";
            GunIds[64] = "Kar98K-I";
            GunIds[128] = "Kar98K-II";
            GunIds[129] = "Kar98K-III";
            GunIds[24] = "FAMAS";
            GunIds[25] = "M500";
            GunIds[26] = "SVD";
            GunIds[27] = "BAT";
            GunIds[28] = "XM8";
            GunIds[29] = "SPAS12";
            GunIds[30] = "M60";
            GunIds[32] = "P90";
            GunIds[33] = "AN94";
            GunIds[34] = "Katana";
            GunIds[35] = "CG15";
            GunIds[39] = "Flash-MA";
            GunIds[41] = "M1887";
            GunIds[43] = "Thompson";
            GunIds[45] = "M82B";
            GunIds[46] = "AUG";
            GunIds[47] = "PARAFAL";
            GunIds[48] = "WOODPECKER";
            GunIds[49] = "Vector";
            GunIds[50] = "MAG-7";
            GunIds[51] = "Scythe";
            GunIds[54] = "KORD";
            GunIds[55] = "M1917";
            GunIds[56] = "USP-2";
            GunIds[57] = "Kingfisher";
            GunIds[58] = "Mini Uzi";
            GunIds[60] = "MP5-I";

            GunIds[61] = GunIds[122] = GunIds[123] = "M60 V";
            GunIds[63] = GunIds[126] = GunIds[127] = "M14 V";
            GunIds[65] = "AWM-Y";
            GunIds[67] = GunIds[130] = GunIds[131] = "FAMAS V";
            GunIds[71] = "M249X";
            GunIds[72] = "SVD-Y";
            GunIds[73] = "ASSAULT-G36";
            GunIds[74] = "RANGE-G36";
            GunIds[75] = "M24";
            GunIds[80] = GunIds[81] = GunIds[82] = "M4A1 V";
            GunIds[86] = "Charge Buster";
            GunIds[88] = "MAC10";
            GunIds[89] = "AC80";
            GunIds[93] = "Treatment Gun";
            GunIds[99] = "SHIELD GUN";
            GunIds[100] = "Flamethrower";
            GunIds[119] = "M1887X";
            GunIds[120] = "MP5-II";
            GunIds[121] = "MP5-III";
            GunIds[149] = "FF KNIFE";
            GunIds[150] = "Bizon";
            GunIds[178] = GunIds[179] = GunIds[180] = "SCAR V";
            GunIds[181] = "TROGON";
            GunIds[184] = GunIds[185] = GunIds[186] = "M1014 V";
            GunIds[193] = GunIds[194] = GunIds[195] = "AUG V";
            GunIds[197] = "VSK94";
            GunIds[21001] = "HEAL PISTOL Y";
            GunIds[21002] = "M590";
            GunIds[228] = GunIds[229] = GunIds[230] = "MAC10 V";
            GunIds[601] = "Grenade";
            GunIds[602] = "FlashBang";
            GunIds[608] = "FlashFreeze";
            GunIds[1201] = "GlooWall";
            GunIds[1204] = "GlooWall";
            GunIds[10449] = "TRIPLE SHURIKEN";
            GunIds[21003] = "HEAVY SHURIKEN";
            GunIds[21004] = "LIGHT SHURIKEN";
        }


        public static List<string> GetAllGunIds()
        {
            var result = new List<string>();

            for (short i = 0; i < GunIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(GunIds[i]))
                    result.Add($"{i} - {GunIds[i]}");
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
                    case 1201: return "Gloo Wall";  // Gloo Wall
                    case 601: return "Grenade";     // Grenade
                    case 603: return "Smoke Grenade";  // Smoke Grenade
                    case 21001: return "Treatment Gun Y";  // Treatment Gun Y
                    case 21002: return "M590";  // M590
                }

                if (gunId >= 0 && gunId < GunIds.Length && !string.IsNullOrEmpty(GunIds[gunId]))
                    return GunIds[gunId];

                // If not matched, return the gun ID itself
                return $"ID:{gunId}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting gun ids: {ex.Message}");
                return $"ID:{gunId}";
            }
        }

    }
}
