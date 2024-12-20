﻿using System.Collections.Generic;
using WrathCombo.Core;
using WrathCombo.CustomComboNS;
using WrathCombo.CustomComboNS.Functions;

namespace WrathCombo.Combos.PvP
{
    internal static class PvPCommon
    {
        public const uint
            Teleport = 5,
            Return = 6,
            StandardElixir = 29055,
            Recuperate = 29711,
            Purify = 29056,
            Guard = 29054,
            Sprint = 29057;

        internal class Config
        {
            public const string
                EmergencyHealThreshold = "EmergencyHealThreshold",
                EmergencyGuardThreshold = "EmergencyGuardThreshold",
                QuickPurifyStatuses = "QuickPurifyStatuses";
        }

        internal class Debuffs
        {
            public const ushort
                Silence = 1347,
                Bind = 1345,
                Stun = 1343,
                HalfAsleep = 3022,
                Sleep = 1348,
                DeepFreeze = 3219,
                Heavy = 1344,
                Unguarded = 3021;
        }

        internal class Buffs
        {
            public const ushort
                Sprint = 1342,
                Guard = 3054;
        }

        /// <summary> Checks if the target is immune to damage. Optionally, include buffs that provide significant damage reduction. </summary>
        /// <param name="includeReductions"> Includes buffs that provide significant damage reduction. </param>
        public static bool IsImmuneToDamage(bool includeReductions = true)
        {
            if (CustomComboFunctions.CurrentTarget is null || !CustomComboFunctions.InPvP()) return false;

            bool targetHasReductions = CustomComboFunctions.TargetHasEffectAny(Buffs.Guard) || CustomComboFunctions.TargetHasEffectAny(VPRPvP.Buffs.HardenedScales);
            bool targetHasImmunities = CustomComboFunctions.TargetHasEffectAny(DRKPvP.Buffs.UndeadRedemption) || CustomComboFunctions.TargetHasEffectAny(PLDPvP.Buffs.HallowedGround);

            return includeReductions
                ? targetHasReductions || targetHasImmunities
                : targetHasImmunities;
        }

        // Lists of Excluded skills 
        internal static readonly List<uint>
            MovmentSkills = [WARPvP.Onslaught, NINPvP.Shukuchi, DNCPvP.EnAvant, MNKPvP.ThunderClap, RDMPvP.CorpsACorps, RDMPvP.Displacement, SGEPvP.Icarus, RPRPvP.HellsIngress, RPRPvP.Regress, BRDPvP.RepellingShot, BLMPvP.AetherialManipulation, DRGPvP.ElusiveJump, GNBPvP.RoughDivide,
            GNBPvP.RelentlessRush, SAMPvP.Zantetsuken, RPRPvP.TenebraeLemurum, DRKPvP.Eventide, MCHPvP.MarksmanSpite, RDMPvP.SouthernCross, NINPvP.SeitonTenchu, NINPvP.Huton, NINPvP.Meisui, NINPvP.ThreeMudra, SGEPvP.Pneuma, SGEPvP.Mesotes, DRKPvP.BlackestNight,
            DRGPvP.HorridRoar, SAMPvP.Soten, SAMPvP.Chiten, MNKPvP.RiddleOfEarth, MNKPvP.EarthsReply, DNCPvP.CuringWaltz, DNCPvP.Contradance, PLDPvP.Phalanx, PLDPvP.HolySheltron, DRKPvP.Impalement, DRKPvP.SaltedEarth, DRKPvP.SaltAndDarkness, DRKPvP.Plunge, VPRPvP.Slither, VPRPvP.Backlash, VPRPvP.WorldSwallower, VPRPvP.SnakeScales, PCTPvP.Smudge, PCTPvP.HolyInWhite, PCTPvP.TemperaCoat, PCTPvP.StarPrism,
            PLDPvP.HolySpirit, PLDPvP.Guardian, PLDPvP.Intervene, WARPvP.Onslaught, WARPvP.PrimalRend, WARPvP.Bloodwhetting, WARPvP.Blota, WARPvP.PrimalScream, GNBPvP.RelentlessRush, GNBPvP.HeartOfCorundum, GNBPvP.FatedCircle, Recuperate, Sprint, Purify, StandardElixir, Teleport, RDMPvP.Forte, RDMPvP.Displacement, MCHPvP.BishopTurret, MCHPvP.Scattergun, 
            BRDPvP.RepellingShot, SCHPvP.Expedient, SCHPvP.Aqloquilum, SMNPvP.CrimsonCyclone, SMNPvP.RadiantAegis, ASTPvP.Microcosmos, ASTPvP.Macrocosmos, WHMPvP.Cure2, WHMPvP.Cure3, WHMPvP.AfflatusPurgation, WHMPvP.Aquaveil, BLMPvP.Burst, DRGPvP.SkyHigh],
            GlobalSkills = [Teleport, Guard, Recuperate, Purify, StandardElixir, Sprint];

        internal class GlobalEmergencyHeals : CustomCombo
        {
            protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.PvP_EmergencyHeals;

            protected override uint Invoke(uint actionID, uint lastComboActionID, float comboTime, byte level)
            {
                if ((HasEffect(Buffs.Guard) || JustUsed(Guard)) && IsEnabled(CustomComboPreset.PvP_MashCancel))
                {
                    if (MovmentSkills.Contains(actionID))
                    {
                        return actionID; //allow for an exemption list
                    }
                    else
                        return OriginalHook(11); //execute the original action
                }

                if (Execute() &&
                     InPvP() &&
                    !GlobalSkills.Contains(actionID) &&
                    !MovmentSkills.Contains(actionID))
                    return OriginalHook(Recuperate);

                return actionID;
            }

            public static bool Execute()
            {
                var jobMaxHp = LocalPlayer.MaxHp;
                var threshold = PluginConfiguration.GetCustomIntValue(Config.EmergencyHealThreshold);
                var maxHPThreshold = jobMaxHp - 15000;
                var remainingPercentage = (float)LocalPlayer.CurrentHp / (float)maxHPThreshold;


                if (HasEffect(3180)) return false; //DRG LB buff
                if (HasEffectAny(1420)) return false; //Rival Wings Mounted
                if (HasEffect(DRKPvP.Buffs.UndeadRedemption)) return false;
                if (LocalPlayer.CurrentMp < 2500) return false;
                if (remainingPercentage * 100 > threshold) return false;

                return true;

            }
        }

        internal class GlobalEmergencyGuard : CustomCombo
        {
            protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.PvP_EmergencyGuard;

            protected override uint Invoke(uint actionID, uint lastComboActionID, float comboTime, byte level)
            {
                if ((HasEffect(Buffs.Guard) || JustUsed(Guard)) && IsEnabled(CustomComboPreset.PvP_MashCancel))
                {
                    if (MovmentSkills.Contains(actionID))
                    {
                        return actionID; //allow for an exemption list
                    }
                    else
                        return OriginalHook(11); //execute the original action
                }

                if (Execute() &&
                    InPvP() &&
                    !GlobalSkills.Contains(actionID) &&
                    !MovmentSkills.Contains(actionID))
                    return OriginalHook(Guard);

                return actionID;
            }

            public static bool Execute()
            {
                var jobMaxHp = LocalPlayer.MaxHp;
                var threshold = PluginConfiguration.GetCustomIntValue(Config.EmergencyGuardThreshold);
                var remainingPercentage = (float)LocalPlayer.CurrentHp / (float)jobMaxHp;

                if (HasEffect(3180)) return false; //DRG LB buff
                if (HasEffectAny(1420)) return false; //Rival Wings Mounted
                if (HasEffect(DRKPvP.Buffs.UndeadRedemption)) return false;
                if (HasEffectAny(Debuffs.Unguarded) || HasEffect(WARPvP.Buffs.InnerRelease)) return false;
                if (GetCooldown(Guard).IsCooldown) return false;
                if (remainingPercentage * 100 > threshold) return false;

                return true;

            }
        }

        internal class QuickPurify : CustomCombo
        {
            protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.PvP_QuickPurify;

            protected override uint Invoke(uint actionID, uint lastComboActionID, float comboTime, byte level)
            {
                if ((HasEffect(Buffs.Guard) || JustUsed(Guard)) && IsEnabled(CustomComboPreset.PvP_MashCancel))
                {
                    if (MovmentSkills.Contains(actionID))
                    {
                        return actionID; //allow for an exemption list
                    }
                    else
                        return OriginalHook(11); //execute the original action
                }

                if (Execute() &&
                    InPvP() &&
                    !GlobalSkills.Contains(actionID))
                    return OriginalHook(Purify);

                return actionID;
            }

            public static bool Execute()
            {
                var selectedStatuses = PluginConfiguration.GetCustomBoolArrayValue(Config.QuickPurifyStatuses);

                if (HasEffect(3180)) return false; //DRG LB buff
                if (HasEffectAny(1420)) return false; //Rival Wings Mounted

                if (selectedStatuses.Length == 0) return false;
                if (GetCooldown(Purify).IsCooldown) return false;
                if (HasEffectAny(Debuffs.Stun) && selectedStatuses[0]) return true;
                if (HasEffectAny(Debuffs.DeepFreeze) && selectedStatuses[1]) return true;
                if (HasEffectAny(Debuffs.HalfAsleep) && selectedStatuses[2]) return true;
                if (HasEffectAny(Debuffs.Sleep) && selectedStatuses[3]) return true;
                if (HasEffectAny(Debuffs.Bind) && selectedStatuses[4]) return true;
                if (HasEffectAny(Debuffs.Heavy) && selectedStatuses[5]) return true;
                if (HasEffectAny(Debuffs.Silence) && selectedStatuses[6]) return true;

                return false;

            }
        }

    }

}
