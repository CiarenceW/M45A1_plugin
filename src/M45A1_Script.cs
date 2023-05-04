using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Receiver2;
using Receiver2ModdingKit;
using Receiver2ModdingKit.CustomSounds;
using UnityEngine;

namespace M45A1_plugin
{
    public class M45A1_Script : ModGunScript
    {
        private ModHelpEntry help_entry;
        private readonly float[] slide_push_hammer_curve = new float[] {
            0.03f,
            0,
            0.05f,
            1
        };
        private bool decocking;
        private bool hammer_rest;
        private RotateMover sear;
        private float sear_cocked;
        private float sear_halfcocked;
        private float sear_uncocked;
        private float sear_almost_cocked;
        private float sear_hammer_back;
        public override ModHelpEntry GetGunHelpEntry()
        {
            return help_entry = new ModHelpEntry("m45a1")
            {
                info_sprite = spawn_info_sprite,
                title = "Colt M45A1",
                description = "M45A1, M45 Marine expeditionary unit (special operations capable), M45A1 CQBP\n"
                            + "Capacity: 7 + 1, .45 Automatic Colt Pistol\n"
                            + "\n"
                            + "The Colt M45A1 is a single-action recoil operated pistol chambered in .45 ACP, first introduced in 2012 as a modern version of the classic Colt M1911 design, specifically for the US Marine Corps Special Operations Command (MARSOC) as a replacement for the aging M1911A1 pistol. The improved M45A1 features several changes to the original M1911A1 design. One feature is the dual recoil spring system that spreads out the recoil force of the .45 ACP round by lowering the peak force of the recoil pulse.\n"
                            + "\n"
                            + "It was designed to meet the Marine Corps' strict requirements for reliability, durability, and accuracy in a sidearm, and underwent rigorous testing before being adopted as the M45A1 Close Quarter Battle Pistol (CQBP) in 2012. Since then, the M45A1 has gained popularity among civilians and law enforcement agencies for its rugged construction and classic styling."
            };
        }
        public override LocaleTactics GetGunTactics()
        {
            return new LocaleTactics()
            {
                title = "Colt M45A1",
                gun_internal_name = "Ciarencew.M45A1",
                text = "A modded pistol that shoots boolets, made with bad intentions >:)\n" +
                "The Colt M45A1 is a modern, 1911-style pistol with a stainless steel frame and slide, chambered in .45 ACP and featuring a Picatinny accessory rail, Novak night sights, and a desert tan finish. To safely holster the M45A1, decock the hammer, or switch on the safety while the hammer is cocked."
            };
        }
        public override void InitializeGun()
        {
            var RCS = ReceiverCoreScript.Instance();
            pooled_muzzle_flash = ((GunScript)ReceiverCoreScript.Instance().generic_prefabs.First(it => { return it is GunScript && ((GunScript)it).gun_model == GunModel.m1911; })).pooled_muzzle_flash;
            var mag_root_types = ((GunScript)ReceiverCoreScript.Instance().generic_prefabs.First(it => { return it is GunScript && ((GunScript)it).gun_model == GunModel.m1911; })).magazine_root_types;
            mag_root_types = new List<string>(mag_root_types)
            {
                "colt_m45",
                "colt_m1911_ext"
            }.ToArray();
            ((GunScript)ReceiverCoreScript.Instance().generic_prefabs.First(it => { return it is GunScript && ((GunScript)it).gun_model == GunModel.m1911; })).magazine_root_types = mag_root_types;

            RCS.TryGetMagazinePrefabFromRoot("colt_m45", MagazineClass.LowCapacity, out var m45PrefabLow);
            m45PrefabLow.glint_renderer.material = RCS.GetMagazinePrefab("wolfire.glock_17", MagazineClass.StandardCapacity).glint_renderer.material;
            RCS.TryGetMagazinePrefabFromRoot("colt_m45", MagazineClass.StandardCapacity, out var m45PrefabStd);
            m45PrefabStd.glint_renderer.material = RCS.GetMagazinePrefab("wolfire.glock_17", MagazineClass.StandardCapacity).glint_renderer.material;
            RCS.TryGetMagazinePrefabFromRoot("colt_m1911_ext", MagazineClass.LowCapacity, out var m1911ExtLow);
            m1911ExtLow.glint_renderer.material = RCS.GetMagazinePrefab("wolfire.glock_17", MagazineClass.StandardCapacity).glint_renderer.material;
            RCS.TryGetMagazinePrefabFromRoot("colt_m1911_ext", MagazineClass.StandardCapacity, out var m1911ExtStd);
            m1911ExtStd.glint_renderer.material = RCS.GetMagazinePrefab("wolfire.glock_17", MagazineClass.StandardCapacity).glint_renderer.material;
        }
        public override void AwakeGun()
        {
            hammer.amount = 1f;

            sear = (RotateMover)typeof(GunScript).GetField("sear", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_cocked = (float)typeof(GunScript).GetField("sear_cocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_halfcocked = (float)typeof(GunScript).GetField("sear_halfcocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_uncocked = (float)typeof(GunScript).GetField("sear_uncocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_almost_cocked = (float)typeof(GunScript).GetField("sear_almost_cocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            sear_hammer_back = (float)typeof(GunScript).GetField("sear_hammer_back", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
        }
        public override void UpdateGun()
        {
            // Decocking logic (I'm pretty sure the game does this on its own but it didn't work when I first tried it but when I removed this section of code it still worked idk why I just hate it here man for fuck's sake)
            if (player_input.GetButton(14) && player_input.GetButtonDown(2) && slide.amount == 0f) decocking = true;

            hammer.asleep = false;
            if (decocking)
            {
                if (!player_input.GetButton(14))
                {
                    hammer.amount = Mathf.MoveTowards(hammer.amount, 0f, Time.deltaTime * 5);
                }
                if (hammer.amount == 0 || !player_input.GetButton(2))
                {
                    _hammer_state = 0;
                    decocking = false;

                    if (hammer.amount == 0f) AudioManager.PlayOneShotAttached(sound_decock, hammer.transform.gameObject, 0.3f);
                }
            }
            if (!decocking)
            {
                if (_hammer_state == 0 && hammer.amount >= _hammer_halfcocked)
                {
                    _hammer_state = 1;
                    if (!hammer.asleep)
                    {
                        hammer.target_amount = _hammer_halfcocked;
                    }
                    if (!ReceiverCoreScript.Instance().player.lah.PullingTrigger) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_half_cock", hammer.transform.gameObject);
                }
                if (_hammer_state == 1 && hammer.amount >= _hammer_cocked_val)
                {
                    _hammer_state = 2;
                    if (!hammer.asleep)
                    {
                        hammer.target_amount = _hammer_cocked_val;
                    }
                    if (!ReceiverCoreScript.Instance().player.lah.PullingTrigger) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_full_cock", hammer.transform.gameObject);
                }
                if ((trigger.amount == 0f && !player_input.GetButton(14)))
                {
                    if (_hammer_state == 1)
                    {
                        hammer.target_amount = _hammer_halfcocked;
                        if (!hammer_rest) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_hammer_rest", this.hammer.transform.gameObject);
                    }
                    if (_hammer_state == 2)
                    {
                        hammer.target_amount = _hammer_cocked_val;
                        if (!hammer_rest) AudioManager.PlayOneShotAttached("event:/guns/1911/1911_hammer_rest", this.hammer.transform.gameObject);
                    }
                    hammer_rest = true;
                }
                else
                {
                    hammer_rest = false;
                }
            }
            hammer.TimeStep(Time.deltaTime * 5);
            hammer.UpdateDisplay();

            if (IsSafetyOn())
            { // Safety blocks the trigger from moving, also blocks the hammer for being cocked because for some reason the game still allows for the player to decock??????
                _hammer_state = 2;

                hammer.amount = Mathf.Clamp(hammer.amount, _hammer_cocked_val, 1f);
                hammer.UpdateDisplay();

                trigger.amount = Mathf.Min(trigger.amount, 0.1f);
                trigger.UpdateDisplay();
            }
            if (slide.amount > 0f)
            {
                _disconnector_needs_reset = true;
            }
            if (_disconnector_needs_reset && slide.amount == 0f && trigger.amount == 0f) //makes it so you have to unpress the trigger to be able to shoot again I think actually I don't know really but it seems like what it is
            {
                _disconnector_needs_reset = false;
                AudioManager.PlayOneShotAttached(sound_trigger_reset, trigger.transform.gameObject);
            }
            if (slide.amount > 0.2f && _hammer_state != 2) //makes the hammer go to its max value
            {
                hammer.amount = Mathf.Max(hammer.amount, InterpCurve(slide_push_hammer_curve, slide.amount));
                hammer.UpdateDisplay();
            }
            if (trigger.amount == 1f && hammer.amount == _hammer_cocked_val && !_disconnector_needs_reset && !IsSafetyOn()) //hammer firing logic
            {
                if (slide.amount == 0f)
                {
                    hammer.target_amount = 0f;
                    hammer.vel = -0.1f * ReceiverCoreScript.Instance().player_stats.animation_speed;
                }
            }
            hammer.TimeStep(Time.deltaTime);
            if (hammer.amount == 0f && _hammer_state == 2 && !decocking) //shooting logic
            {
                TryFireBullet(1);
                _hammer_state = 0;
            }
            if (hammer.amount < _hammer_halfcocked)
            {
                sear.amount = sear_uncocked;
            }
            else if (hammer.amount < _hammer_cocked_val)
            {
                sear.amount = Mathf.Lerp(sear_halfcocked, sear_almost_cocked, (hammer.amount - _hammer_halfcocked) / (_hammer_cocked_val - _hammer_halfcocked));
            }
            else
            {
                sear.amount = Mathf.Lerp(sear_cocked, sear_hammer_back, (hammer.amount - _hammer_cocked_val) / (1f - _hammer_cocked_val));
            }
            sear.UpdateDisplay();
            trigger.UpdateDisplay();
            UpdateAnimatedComponents();
        }
    }
}