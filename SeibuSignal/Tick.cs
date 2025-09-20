using BveEx.Extensions.Native;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeibuSignal {
    [Plugin(PluginType.VehiclePlugin)]
    public partial class SeibuSignal : AssemblyPluginBase {
        public override void Tick(TimeSpan elapsed) {
            var AtsHandles = BveHacker.Scenario.Vehicle.Instruments.AtsPlugin.AtsHandles;
            var handles = BveHacker.Scenario.Vehicle.Instruments.AtsPlugin.Handles;
            var state = Native.VehicleState;
            var panel = Native.AtsPanelArray;
            var sound = Native.AtsSoundArray;

            int pointer = 0;
            while (sectionManager.Sections[pointer].Location < state.Location) {
                pointer++;
                if (pointer >= sectionManager.Sections.Count) {
                    pointer = sectionManager.Sections.Count - 1;
                    break;
                }
            }

            var currentSection = sectionManager.Sections[pointer == 0 ? 0 : pointer - 1] as Section;

            if (SignalEnable) {
                if (StandAloneMode) {
                    if (SeibuATS.ATSEnable) {
                        SeibuATS.Tick(state, sectionManager);
                        if (SeibuATS.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, SeibuATS.BrakeCommand);
                            else AtsHandles.BrakeNotch = SeibuATS.BrakeCommand;
                            BrakeTriggered = true;
                        }
                    } else {
                        SeibuATS.Init(state.Time);
                    }
                } else {
                    if (!corePlugin.SubPluginEnabled) corePlugin.SubPluginEnabled = true;
                    if (corePlugin.SignalSWPos == MetroAts.SignalSWList.SeibuATS) {
                        if (!SeibuATS.ATSEnable) SeibuATS.Init(state.Time);
                        if (ATC.ATCEnable)
                            ATC.ResetAll();
                    } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC) {
                        if (!ATC.ATCEnable) ATC.Init(state.Time);
                        if (SeibuATS.ATSEnable)
                            SeibuATS.ResetAll();
                    }

                    if (SeibuATS.ATSEnable) {
                        SeibuATS.Tick(state, sectionManager);
                        if (SeibuATS.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, SeibuATS.BrakeCommand);
                            else AtsHandles.BrakeNotch = SeibuATS.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (ATC.ATCEnable && !(currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49))
                            ATC.ResetAll();
                    }
                    if (ATC.ATCEnable) {
                        ATC.Tick(state, handles, currentSection, corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset, corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot);
                        if (ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (SeibuATS.ATSEnable)
                            SeibuATS.ResetAll();
                    }
                    if (!ATC.ATCEnable) panel[33] = corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot ? 1 : 0;//うさプラ互換
                    panel[30] = corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset ? 1 : 0;//うさプラ互換
                    if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49) {
                        if (!ATC.ATCEnable) {
                            if (corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot || corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset) {
                                ATC.Init(state.Time);
                            }else if(corePlugin.SignalSWPos == MetroAts.SignalSWList.SeibuATS) {
                                ATC.InitNow();
                            }
                        }
                        sound[0] = ((corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot && currentSection.CurrentSignalIndex >= 38 && currentSection.CurrentSignalIndex <= 48)//うさプラ互換
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC) ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;
                    } else if (corePlugin.SignalSWPos != MetroAts.SignalSWList.ATC) {
                        if (ATC.ATCEnable) ATC.ResetAll();
                        sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    }
                }
                if (!StandAloneMode) {
                    if (corePlugin.KeyPos != MetroAts.KeyPosList.Seibu || (corePlugin.SignalSWPos != MetroAts.SignalSWList.InDepot
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.Noset
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.ATC
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.SeibuATS)) {
                        BrakeTriggered = false;
                        SignalEnable = false;
                        SeibuATS.ResetAll();
                        ATC.ResetAll();
                        if (sound[0] != (int)AtsSoundControlInstruction.Stop) sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                        panel[33] = 0;//うさプラ互換
                        panel[30] = 0;//うさプラ互換
                    }
                }
                if (BrakeTriggered) {
                    AtsHandles.PowerNotch = 0;
                    if (handles.PowerNotch == 0) BrakeTriggered = false;
                }
                UpdatePanelAndSound(panel, sound);
                panel[Config.Panel_poweroutput] = AtsHandles.PowerNotch;
                panel[Config.Panel_brakeoutput] = AtsHandles.BrakeNotch;
            } else {
                if (StandAloneMode) {
                    if (!SignalEnable && Keyin)
                        SignalEnable = true;
                    AtsHandles.BrakeNotch = vehicleSpec.BrakeNotches + 1;
                    AtsHandles.ReverserPosition = ReverserPosition.N;
                    if (sound[0] != (int)AtsSoundControlInstruction.Stop) sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    panel[33] = 0;//うさプラ互換
                    panel[30] = 0;//うさプラ互換
                } else {
                    Keyin = corePlugin.KeyPos == MetroAts.KeyPosList.Seibu;
                    if (!SignalEnable && Keyin && corePlugin.SignalSWPos == MetroAts.SignalSWList.SeibuATS)
                        SignalEnable = true;
                    else if (!SignalEnable && Keyin && (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot || corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset)
                        && handles.BrakeNotch != vehicleSpec.BrakeNotches + 1)
                        SignalEnable = true;
                }
            }
            if (StandAloneMode) {
                var description = BveHacker.Scenario.Vehicle.Instruments.Cab.GetDescriptionText();
                leverText = (LeverText)BveHacker.MainForm.Assistants.Items.First(item => item is LeverText);
                leverText.Text = $"キー:{(Keyin ? "入" : "切")} \n{description}";
                if (isDoorOpen) AtsHandles.ReverserPosition = ReverserPosition.N;
                sound[10] = (int)Sound_Keyin;//うさプラ互換
                sound[11] = (int)Sound_Keyout;//うさプラ互換
                panel[Config.Panel_keyoutput] = Convert.ToInt32(Keyin);
            }

            //sound reset
            Sound_Keyin = Sound_Keyout = Sound_ResetSW = AtsSoundControlInstruction.Continue;
            //handles.PowerNotch = 0;
            //handles.BrakeNotch = 0;
            //handles.ConstantSpeedMode = ConstantSpeedMode.Continue;
            //handles.ReverserPosition = ReverserPosition.N;
        }

        private static void UpdatePanelAndSound(IList<int> panel, IList<int> sound) {
            sound[28] = (int)Sound_ResetSW;//うさプラ互換

            //panel うさプラ互換
            panel[102] = Convert.ToInt32(ATC.ATC_01);
            panel[107] = Convert.ToInt32(ATC.ATC_25);
            panel[110] = Convert.ToInt32(ATC.ATC_40);
            panel[113] = Convert.ToInt32(ATC.ATC_55);
            panel[117] = Convert.ToInt32(ATC.ATC_75);
            panel[120] = Convert.ToInt32(ATC.ATC_90);

            panel[131] = Convert.ToInt32(ATC.ATC_Stop);
            panel[132] = Convert.ToInt32(ATC.ATC_Proceed);

            panel[101] = Convert.ToInt32(ATC.ATC_X);

            panel[311] = ATC.ATCNeedle;
            panel[310] = Convert.ToInt32(ATC.ATCNeedle_Disappear);

            panel[20] = Convert.ToInt32(ATC.ATC_ATC);
            if (ATC.ATCEnable) panel[33] = Convert.ToInt32(ATC.ATC_Depot);//うさプラ互換
            if (ATC.ATCEnable && ATC.ATC_Noset) panel[30] = Convert.ToInt32(ATC.ATC_Noset);//うさプラ互換
            panel[26] = Convert.ToInt32(ATC.ATC_ServiceBrake);
            panel[25] = Convert.ToInt32(ATC.ATC_EmergencyBrake);
            panel[281] = Convert.ToInt32(ATC.ATC_EmergencyOperation);//調整中

            panel[46] = Convert.ToInt32(SeibuATS.ATS_Power);
            panel[47] = Convert.ToInt32(SeibuATS.ATS_EB);
            panel[253] = Convert.ToInt32(SeibuATS.ATS_Stop);
            panel[48] = Convert.ToInt32(SeibuATS.ATS_Confirm);
            panel[49] = Convert.ToInt32(SeibuATS.ATS_Limit);
            //panel[339] = Convert.ToInt32(SeibuATS.);

            sound[2] = (int)ATC.ATC_Ding;//うさプラ互換
            if (ATC.ATCEnable && ATC.ATC_Noset) { sound[0] = (int)ATC.ATC_WarningBell; }//うさプラ互換
            sound[261] = (int)ATC.ATC_EmergencyOperationAnnounce;//調整中
            sound[8] = (int)SeibuATS.ATS_StopAnnounce;//うさプラ互換
            sound[9] = (int)SeibuATS.ATS_EBAnnounce;//うさプラ互換
        }
    }
}
