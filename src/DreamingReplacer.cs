using BepInEx;
using HarmonyLib;
using Receiver2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace M45A1_plugin
{

    [BepInPlugin("Ciarencew.M45A1_dreaming_replacer", "M45A1 Dreaming Magazine Replacer", "1.0.0")]
    public class DreamingReplacer : BaseUnityPlugin
    {
        private static GameObject m10;
        private static GameObject intro_tile_with_gun;
        private static Vector3 pos_gun = new Vector3(2.5152f, 200.2212f, 35.7299f);
        private static Quaternion rot_gun = new Quaternion(-.7064f, .7064f, -.031f, .031f); //you don't have to write zero before a decimal number????? how fucking cool is that?????

        public static float m1911_mag_chance = 0.4f;
        public static float big_1911_mag_chance = 0.2f;

        private void Awake()
        {
            Harmony.CreateAndPatchAll(this.GetType());
        }

        [HarmonyPatch(typeof(ReceiverCoreScript), "SpawnPlayer")]
        [HarmonyPostfix]
        private static void OnStartIntro(ref ReceiverCoreScript __instance)
        {
            if (__instance.game_mode.GetGameMode() != GameMode.RankingCampaign) return;
            if (__instance.player.lah.loadout == null) return;
            if (__instance.player.lah.loadout.gun_internal_name != "Ciarencew.M45A1") return;
            if (((RankingProgressionGameMode)__instance.game_mode).progression_data.receiver_rank == 0)
            {
                intro_tile_with_gun = RuntimeTileLevelGenerator.instance.GetTiles()[2];
                m10 = (intro_tile_with_gun.transform.Find("model_10(Clone)")).gameObject;
                UnityEngine.Object.Destroy(m10);
                InventoryItem gun;
                if (__instance.TryGetItemPrefab<InventoryItem>(__instance.player.lah.loadout.gun_internal_name, out gun))
                {
                    UnityEngine.Object.Instantiate<InventoryItem>(gun, pos_gun, rot_gun, intro_tile_with_gun.transform).Move(null);
                }
            }
        }

        [HarmonyPatch(typeof(RuntimeTileLevelGenerator),
            nameof(RuntimeTileLevelGenerator.instance.InstantiateMagazine),
            new[] { typeof(Vector3), typeof(Quaternion), typeof(Transform), typeof(MagazineClass) }
            )]
        [HarmonyPostfix]
        private static void patchInstantiateMagazine(ref GameObject __result)
        {
            var RCS = ReceiverCoreScript.Instance();
            if (RCS.CurrentLoadout.gun_internal_name != "Ciarencew.M45A1") return; //this whole thing should only be active when the STM-9 is held, but still.
            if (Probability.Chance(m1911_mag_chance)) return;
            var mag_root_type = "colt_m1911";
            if (Probability.Chance(big_1911_mag_chance)) mag_root_type = "colt_m1911_ext";

            GameObject magObj;
            magObj = __result;
            Destroy(__result);

            magObj.name = "Hey! this shouldn't show up, I mean you shouldn't even see this, at all.";

            var magScript = magObj.GetComponent<MagazineScript>();
            RCS.TryGetMagazinePrefabFromRoot(mag_root_type, magScript.MagazineClass, out var magPrefab);
            var replacedMag = RuntimeTileLevelGenerator.instance.InstantiateMagazine(magObj.transform.position, magObj.transform.rotation, magObj.transform.parent, magPrefab);
            replacedMag.GetComponent<MagazineScript>().SetRoundCount(UnityEngine.Random.Range(0, replacedMag.GetComponent<MagazineScript>().kMaxRounds));
        }
    }
}
