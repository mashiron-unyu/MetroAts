using BveEx.Extensions.Native;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokyuSignal {
    [Plugin(PluginType.VehiclePlugin)]
    public partial class TokyuSignal : AssemblyPluginBase {
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
            var nextSection = sectionManager.Sections[pointer] as Section;

            if (SignalEnable) {
                if (StandAloneMode) {
                    if (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.ATC) {
                        if (!ATC.ATCEnable) ATC.Init(state.Time);
                        if (TokyuATS.ATSEnable) TokyuATS.ResetAll();
                    } else if (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.TokyuATS) {
                        if (!TokyuATS.ATSEnable) TokyuATS.Init(state.Time);
                        if (ATC.ATCEnable) ATC.ResetAll();
                    }
                    if (ATC.ATCEnable) {
                        ATC.Tick(state, currentSection, nextSection, handles, Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset);
                        if (ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (TokyuATS.ATSEnable) TokyuATS.ResetAll();
                    }
                    if (TokyuATS.ATSEnable) {
                        TokyuATS.Tick(state);
                        if (TokyuATS.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, TokyuATS.BrakeCommand);
                            else AtsHandles.BrakeNotch = TokyuATS.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (ATC.ATCEnable) ATC.ResetAll();
                    }
                    panel[28] = Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset ? 1 : 0;//うさプラ互換
                    if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49) {
                        if (!ATC.ATCEnable && (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset))
                            ATC.Init(state.Time);
                        if (TokyuATS.ATSEnable) {
                            TokyuATS.ResetAll();
                            AtsHandles.BrakeNotch = vehicleSpec.BrakeNotches + 1;
                            AtsHandles.ReverserPosition = ReverserPosition.N;
                        }
                        if (!ATC.ATCEnable)
                            sound[0] = Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.ATC ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;//うさプラ互換
                    } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset) {
                        if (ATC.ATCEnable) ATC.ResetAll();
                        sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    }
                } else {
                    if (!corePlugin.SubPluginEnabled) corePlugin.SubPluginEnabled = true;
                    if (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC) {
                        if (!ATC.ATCEnable) ATC.Init(state.Time);
                        if (TokyuATS.ATSEnable) TokyuATS.ResetAll();
                    } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.TokyuATS) {
                        if (!TokyuATS.ATSEnable) TokyuATS.Init(state.Time);
                        if (ATC.ATCEnable) ATC.ResetAll();
                    }
                    if (ATC.ATCEnable) {
                        ATC.Tick(state, currentSection, nextSection, handles, corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset);
                        if (ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (TokyuATS.ATSEnable) TokyuATS.ResetAll();
                    }
                    if (TokyuATS.ATSEnable) {
                        TokyuATS.Tick(state);
                        if (TokyuATS.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2) 
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, TokyuATS.BrakeCommand);
                            else AtsHandles.BrakeNotch = TokyuATS.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (ATC.ATCEnable) ATC.ResetAll();
                    }
                    panel[28] = corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset ? 1 : 0;//うさプラ互換
                    if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49) {
                        if (!ATC.ATCEnable) {
                            if (corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset) {
                                ATC.Init(state.Time);
                            } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.TokyuATS) {
                                ATC.InitNow();
                            }
                        }
                        //if (TokyuATS.ATSEnable) { 
                        //    TokyuATS.ResetAll();
                        //    AtsHandles.BrakeNotch = vehicleSpec.BrakeNotches + 1;
                        //    AtsHandles.ReverserPosition = ReverserPosition.N;
                        //}
                        if (!ATC.ATCEnable) sound[0] = (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC)//うさプラ互換
                            ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;
                    } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset) {
                        if (ATC.ATCEnable) ATC.ResetAll();
                        sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    }
                }
                if (!StandAloneMode) {
                    if (!(corePlugin.KeyPos == MetroAts.KeyPosList.Tokyu) ||
                        (corePlugin.SignalSWPos != MetroAts.SignalSWList.Noset
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.ATC
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.TokyuATS)) {
                        BrakeTriggered = false;
                        SignalEnable = false;
                        ATC.ResetAll();
                        TokyuATS.ResetAll();
                        if (sound[0] != (int)AtsSoundControlInstruction.Stop) sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                        panel[32] = 0;//うさプラ互換
                        panel[28] = 0;//うさプラ互換
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
                    if (!SignalEnable && Keyin && handles.BrakeNotch != vehicleSpec.BrakeNotches + 1)
                        SignalEnable = true;
                    AtsHandles.BrakeNotch = vehicleSpec.BrakeNotches + 1;
                    AtsHandles.ReverserPosition = ReverserPosition.N;
                    if (sound[0] != (int)AtsSoundControlInstruction.Stop) sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    panel[32] = 0;//うさプラ互換
                    panel[28] = 0;//うさプラ互換
                } else {
                    Keyin = corePlugin.KeyPos == MetroAts.KeyPosList.Tokyu;
                    if (!SignalEnable && Keyin &&
                        (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.TokyuATS)
                        && handles.BrakeNotch != vehicleSpec.BrakeNotches + 1)
                        SignalEnable = true;
                }

            }
            if (StandAloneMode) {
                var SignalSWText = "";
                switch (Config.SignalSWLists[NowSignalSW]) {
                    case SignalSWListStandAlone.Noset:
                        SignalSWText = "非設";
                        break;
                    case SignalSWListStandAlone.ATC:
                        SignalSWText = "ATC";
                        break;
                    case SignalSWListStandAlone.TokyuATS:
                        SignalSWText = "東急ATS";
                        break;
                    default:
                        SignalSWText = "無効";
                        break;
                }
                var description = BveHacker.Scenario.Vehicle.Instruments.Cab.GetDescriptionText();
                leverText = (LeverText)BveHacker.MainForm.Assistants.Items.First(item => item is LeverText);
                leverText.Text = $"キー:{(Keyin ? "入" : "切")} 保安:{SignalSWText}\n{description}";
                if (isDoorOpen) AtsHandles.ReverserPosition = ReverserPosition.N;
                sound[10] = (int)Sound_Keyin;//うさプラ互換
                sound[11] = (int)Sound_Keyout;//うさプラ互換
                sound[22] = (int)Sound_SignalSW;//うさプラ互換

                panel[Config.Panel_keyoutput] = Convert.ToInt32(Keyin);
                panel[Config.Panel_SignalSWoutput] = (int)Config.SignalSWLists[NowSignalSW];
            }

            //sound reset
            Sound_Keyin = Sound_Keyout = Sound_ResetSW = Sound_SignalSW = AtsSoundControlInstruction.Continue;
            //handles.PowerNotch = 0;
            //handles.BrakeNotch = 0;
            //handles.ConstantSpeedMode = ConstantSpeedMode.Continue;
            //handles.ReverserPosition = ReverserPosition.N;
        }

        private static void UpdatePanelAndSound(IList<int> panel, IList<int> sound) {
            sound[28] = (int)Sound_ResetSW;//うさプラ互換

            //panel うさプラ互換
            panel[102] = Convert.ToInt32(ATC.ATC_01);
            panel[104] = Convert.ToInt32(ATC.ATC_10);
            panel[105] = Convert.ToInt32(ATC.ATC_15);
            panel[106] = Convert.ToInt32(ATC.ATC_20);
            panel[107] = Convert.ToInt32(ATC.ATC_25);
            panel[108] = Convert.ToInt32(ATC.ATC_30);
            panel[109] = Convert.ToInt32(ATC.ATC_35);
            panel[110] = Convert.ToInt32(ATC.ATC_40);
            panel[111] = Convert.ToInt32(ATC.ATC_45);
            panel[112] = Convert.ToInt32(ATC.ATC_50);
            panel[113] = Convert.ToInt32(ATC.ATC_55);
            panel[114] = Convert.ToInt32(ATC.ATC_60);
            panel[115] = Convert.ToInt32(ATC.ATC_65);
            panel[116] = Convert.ToInt32(ATC.ATC_70);
            panel[117] = Convert.ToInt32(ATC.ATC_75);
            panel[118] = Convert.ToInt32(ATC.ATC_80);
            panel[119] = Convert.ToInt32(ATC.ATC_85);
            panel[120] = Convert.ToInt32(ATC.ATC_90);
            panel[121] = Convert.ToInt32(ATC.ATC_95);
            panel[122] = Convert.ToInt32(ATC.ATC_100);
            panel[123] = Convert.ToInt32(ATC.ATC_105);
            panel[124] = Convert.ToInt32(ATC.ATC_110);

            panel[131] = Convert.ToInt32(ATC.ATC_Stop);
            panel[132] = Convert.ToInt32(ATC.ATC_Proceed);

            panel[134] = Convert.ToInt32(ATC.ATC_P);
            panel[133] = Convert.ToInt32(ATC.ATC_SignalAnn);
            panel[101] = Convert.ToInt32(ATC.ATC_X);

            panel[311] = ATC.ATCNeedle;
            panel[310] = Convert.ToInt32(ATC.ATCNeedle_Disappear);

            panel[21] = Convert.ToInt32(ATC.ATC_ATC);
            if (ATC.ATCEnable) panel[32] = Convert.ToInt32(ATC.ATC_Depot);//うさプラ互換
            if (ATC.ATCEnable && ATC.ATC_Noset) panel[28] = Convert.ToInt32(ATC.ATC_Noset);//うさプラ互換
            panel[23] = Convert.ToInt32(ATC.ATC_ServiceBrake);
            panel[22] = Convert.ToInt32(ATC.ATC_EmergencyBrake);
            panel[282] = Convert.ToInt32(ATC.ATC_EmergencyOperation);//調整中
            panel[254] = Convert.ToInt32(ATC.ATC_StationStop);

            panel[11] = Convert.ToInt32(TokyuATS.ATS_TokyuATS);
            panel[12] = Convert.ToInt32(TokyuATS.ATS_EB);
            panel[16] = Convert.ToInt32(TokyuATS.ATS_WarnNormal);
            panel[17] = Convert.ToInt32(TokyuATS.ATS_WarnTriggered);

            sound[2] = (int)ATC.ATC_Ding;//うさプラ互換
            sound[3] = (int)ATC.ATC_ORPBeep;//うさプラ互換
            if (ATC.ATC_SignalAnnBeep == AtsSoundControlInstruction.Play)
                sound[4] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
            sound[4] = (int)ATC.ATC_SignalAnnBeep;//うさプラ互換
            sound[19] = (int)TokyuATS.ATS_WarnBell;//うさプラ互換
            if (TokyuATS.ATSEnable) {
                sound[0] = (int)TokyuATS.ATS_EBBell;//うさプラ互換
            }
            if (ATC.ATCEnable) {
                sound[0] = (int)ATC.ATC_WarningBell;//うさプラ互換
            }
            sound[261] = (int)ATC.ATC_EmergencyOperationAnnounce;//調整中
        }
    }
}
