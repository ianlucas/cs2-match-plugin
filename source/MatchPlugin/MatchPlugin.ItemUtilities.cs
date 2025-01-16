/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace MatchPlugin;

public class ItemUtilities
{
    private static readonly Dictionary<string, int> _itemDefinitionIndexes =
        new()
        {
            { "weapon_deagle", 1 },
            { "weapon_elite", 2 },
            { "weapon_fiveseven", 3 },
            { "weapon_glock", 4 },
            { "weapon_ak47", 7 },
            { "weapon_aug", 8 },
            { "weapon_awp", 9 },
            { "weapon_famas", 10 },
            { "weapon_g3sg1", 11 },
            { "weapon_galilar", 13 },
            { "weapon_m249", 14 },
            { "weapon_m4a1", 16 },
            { "weapon_mac10", 17 },
            { "weapon_p90", 19 },
            { "weapon_zone_repulsor", 20 },
            { "weapon_mp5sd", 23 },
            { "weapon_ump45", 24 },
            { "weapon_xm1014", 25 },
            { "weapon_bizon", 26 },
            { "weapon_mag7", 27 },
            { "weapon_negev", 28 },
            { "weapon_sawedoff", 29 },
            { "weapon_tec9", 30 },
            { "weapon_taser", 31 },
            { "weapon_hkp2000", 32 },
            { "weapon_mp7", 33 },
            { "weapon_mp9", 34 },
            { "weapon_nova", 35 },
            { "weapon_p250", 36 },
            { "weapon_scar20", 38 },
            { "weapon_sg556", 39 },
            { "weapon_ssg08", 40 },
            { "weapon_knifegg", 41 },
            { "weapon_knife", 42 },
            { "weapon_flashbang", 43 },
            { "weapon_hegrenade", 44 },
            { "weapon_smokegrenade", 45 },
            { "weapon_molotov", 46 },
            { "weapon_decoy", 47 },
            { "weapon_incgrenade", 48 },
            { "weapon_c4", 49 },
            { "item_kevlar", 50 },
            { "item_assaultsuit", 51 },
            { "item_heavyassaultsuit", 52 },
            { "item_nvg", 54 },
            { "item_defuser", 55 },
            { "item_cutters", 56 },
            { "weapon_healthshot", 57 },
            { "musickit_default", 58 },
            { "weapon_knife_t", 59 },
            { "weapon_m4a1_silencer", 60 },
            { "weapon_usp_silencer", 61 },
            { "weapon_cz75a", 63 },
            { "weapon_revolver", 64 },
            { "weapon_tagrenade", 68 },
            { "weapon_fists", 69 },
            { "weapon_breachcharge", 70 },
            { "weapon_tablet", 72 },
            { "weapon_knife_ghost", 80 },
            { "weapon_firebomb", 81 },
            { "weapon_diversion", 82 },
            { "weapon_frag_grenade", 83 },
            { "weapon_snowball", 84 },
            { "weapon_bumpmine", 85 },
            { "t_gloves", 5028 },
            { "ct_gloves", 5029 },
            { "customplayer_t_map_based", 5036 },
            { "customplayer_ct_map_based", 5037 }
        };

    public static int GetItemDefIndex(string designerName)
    {
        return _itemDefinitionIndexes
            .Where(w => w.Key.Contains(designerName))
            .Select(w => w.Value)
            .FirstOrDefault(65536);
    }
}
