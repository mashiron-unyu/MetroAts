using BveEx.Extensions.Native;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroSignal {
    [Plugin(PluginType.VehiclePlugin)]
    public partial class MetroSignal : AssemblyPluginBase {
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
                    if (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.WS_ATC) {
                        if (!WS_ATC.ATCEnable) WS_ATC.Init(state.Time);
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                    } else if (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.ATC) {
                        if (!CS_ATC.ATCEnable) CS_ATC.Init(state.Time);
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                    }
                    if (CS_ATC.ATCEnable) {
                        CS_ATC.Tick(state, currentSection, nextSection, handles, Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset, Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.InDepot);
                        if (CS_ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, CS_ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = CS_ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                    }
                    if (WS_ATC.ATCEnable) {
                        WS_ATC.Tick(state, currentSection, Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset);
                        if (WS_ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, WS_ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = WS_ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                    }
                    panel[31] = Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.InDepot ? 1 : 0;//うさプラ互換
                    panel[29] = Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset ? 1 : 0;//うさプラ互換
                    if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49) {
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                        if (!CS_ATC.ATCEnable && (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.InDepot
                            || Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset)) CS_ATC.Init(state.Time);
                        sound[0] = ((Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.InDepot && currentSection.CurrentSignalIndex >= 38 && currentSection.CurrentSignalIndex <= 48)//うさプラ互換
                        || Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.ATC) ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;
                    } else if (currentSection.CurrentSignalIndex >= 50 && currentSection.CurrentSignalIndex <= 54) {
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                        if (!WS_ATC.ATCEnable &&
                            (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset)) WS_ATC.Init(state.Time);
                        sound[0] = Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.WS_ATC ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;//うさプラ互換
                    } else if (Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.InDepot || Config.SignalSWLists[NowSignalSW] == SignalSWListStandAlone.Noset) {
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                        sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    }
                } else {
                    if (!corePlugin.SubPluginEnabled) corePlugin.SubPluginEnabled = true;
                    if (corePlugin.SignalSWPos == MetroAts.SignalSWList.WS_ATC) {
                        if (!WS_ATC.ATCEnable) WS_ATC.Init(state.Time);
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                    } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC) {
                        if (!CS_ATC.ATCEnable) CS_ATC.Init(state.Time);
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();

                    }
                    if (CS_ATC.ATCEnable) {
                        CS_ATC.Tick(state, currentSection, nextSection, handles, corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset, corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot);
                        if (CS_ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, CS_ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = CS_ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                    }
                    if (WS_ATC.ATCEnable) {
                        WS_ATC.Tick(state, currentSection, corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset);
                        if (WS_ATC.BrakeCommand > 0) {
                            if (AtsHandles.BrakeNotch < vehicleSpec.BrakeNotches + 2)
                                AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, WS_ATC.BrakeCommand);
                            else AtsHandles.BrakeNotch = WS_ATC.BrakeCommand;
                            BrakeTriggered = true;
                        }
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                    }
                    if (!CS_ATC.ATCEnable) panel[31] = corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot ? 1 : 0;//うさプラ互換
                    panel[29] = (corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset || corePlugin.SignalSWPos == MetroAts.SignalSWList.JR) ? 1 : 0;//うさプラ互換
                    if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49) {
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                        if (!CS_ATC.ATCEnable && (corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot
                            || corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset
                            || corePlugin.SignalSWPos == MetroAts.SignalSWList.JR))
                            CS_ATC.Init(state.Time);
                        sound[0] = ((corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot && currentSection.CurrentSignalIndex >= 38 && currentSection.CurrentSignalIndex <= 48)//うさプラ互換
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC) ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;
                    } else if (currentSection.CurrentSignalIndex >= 50 && currentSection.CurrentSignalIndex <= 54) {
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                        if (!WS_ATC.ATCEnable &&
                            (corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset
                            || corePlugin.SignalSWPos == MetroAts.SignalSWList.JR)) WS_ATC.Init(state.Time);
                        sound[0] = corePlugin.SignalSWPos == MetroAts.SignalSWList.WS_ATC ? (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;//うさプラ互換
                    } else if (corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot || corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset) {
                        if (CS_ATC.ATCEnable) CS_ATC.ResetAll();
                        if (WS_ATC.ATCEnable) WS_ATC.ResetAll();
                        sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                    }
                }
                if (!StandAloneMode) {
                    if (!(corePlugin.KeyPos == MetroAts.KeyPosList.Metro || corePlugin.KeyPos == MetroAts.KeyPosList.ToyoKosoku) ||
                        (corePlugin.SignalSWPos != MetroAts.SignalSWList.InDepot
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.Noset
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.ATC
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.ATP
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.WS_ATC
                        && corePlugin.SignalSWPos != MetroAts.SignalSWList.JR)) {
                        BrakeTriggered = false;
                        SignalEnable = false;
                        WS_ATC.ResetAll();
                        CS_ATC.ResetAll();
                        if (sound[0] != (int)AtsSoundControlInstruction.Stop) sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
                        panel[31] = 0;//うさプラ互換
                        panel[29] = 0;//うさプラ互換
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
                    panel[0] = 0;//うさプラ互換
                    panel[0] = 0;//うさプラ互換
                } else {
                    Keyin = corePlugin.KeyPos == MetroAts.KeyPosList.Metro || corePlugin.KeyPos == MetroAts.KeyPosList.ToyoKosoku;
                    if (!SignalEnable && Keyin &&
                        (corePlugin.SignalSWPos == MetroAts.SignalSWList.ATC
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.InDepot
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.Noset
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.JR
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.WS_ATC
                        || corePlugin.SignalSWPos == MetroAts.SignalSWList.ATP)
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
                    case SignalSWListStandAlone.InDepot:
                        SignalSWText = "構内";
                        break;
                    case SignalSWListStandAlone.ATC:
                        SignalSWText = "ATC";
                        break;
                    case SignalSWListStandAlone.WS_ATC:
                        SignalSWText = "WS-ATC";
                        break;
                    case SignalSWListStandAlone.ATP:
                        SignalSWText = "ATP";
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
            panel[102] = Convert.ToInt32(CS_ATC.ATC_01);
            panel[104] = Convert.ToInt32(CS_ATC.ATC_10);
            panel[105] = Convert.ToInt32(CS_ATC.ATC_15);
            panel[106] = Convert.ToInt32(CS_ATC.ATC_20);
            panel[107] = Convert.ToInt32(CS_ATC.ATC_25);
            panel[108] = Convert.ToInt32(CS_ATC.ATC_30);
            panel[109] = Convert.ToInt32(CS_ATC.ATC_35);
            panel[110] = Convert.ToInt32(CS_ATC.ATC_40);
            panel[111] = Convert.ToInt32(CS_ATC.ATC_45);
            panel[112] = Convert.ToInt32(CS_ATC.ATC_50);
            panel[113] = Convert.ToInt32(CS_ATC.ATC_55);
            panel[114] = Convert.ToInt32(CS_ATC.ATC_60);
            panel[115] = Convert.ToInt32(CS_ATC.ATC_65);
            panel[116] = Convert.ToInt32(CS_ATC.ATC_70);
            panel[117] = Convert.ToInt32(CS_ATC.ATC_75);
            panel[118] = Convert.ToInt32(CS_ATC.ATC_80);
            panel[119] = Convert.ToInt32(CS_ATC.ATC_85);
            panel[120] = Convert.ToInt32(CS_ATC.ATC_90);
            panel[121] = Convert.ToInt32(CS_ATC.ATC_95);
            panel[122] = Convert.ToInt32(CS_ATC.ATC_100);
            panel[123] = Convert.ToInt32(CS_ATC.ATC_105);
            panel[124] = Convert.ToInt32(CS_ATC.ATC_110);

            panel[131] = Convert.ToInt32(CS_ATC.ATC_Stop);
            panel[132] = Convert.ToInt32(CS_ATC.ATC_Proceed);

            panel[134] = Convert.ToInt32(CS_ATC.ATC_P);
            panel[133] = Convert.ToInt32(CS_ATC.ATC_SignalAnn);
            panel[101] = Convert.ToInt32(CS_ATC.ATC_X);

            panel[135] = CS_ATC.ORPNeedle;
            panel[311] = CS_ATC.ATCNeedle;//調整中
            panel[310] = Convert.ToInt32(CS_ATC.ATCNeedle_Disappear);//調整中

            panel[19] = Convert.ToInt32(CS_ATC.ATC_ATC);//うさプラ互換
            if (CS_ATC.ATCEnable) panel[31] = Convert.ToInt32(CS_ATC.ATC_Depot);//うさプラ互換
            if ((CS_ATC.ATCEnable && CS_ATC.ATC_Noset) || (WS_ATC.ATCEnable && WS_ATC.ATC_Noset)) panel[29] = Convert.ToInt32(CS_ATC.ATC_Noset || WS_ATC.ATC_Noset);//うさプラ互換
            panel[23] = Convert.ToInt32(CS_ATC.ATC_ServiceBrake || WS_ATC.ATC_ServiceBrake);//うさプラ互換
            panel[22] = Convert.ToInt32(CS_ATC.ATC_EmergencyBrake || WS_ATC.ATC_EmergencyBrake);//うさプラ互換
            panel[280] = Convert.ToInt32(CS_ATC.ATC_EmergencyOperation);//調整中

            panel[34] = Convert.ToInt32(WS_ATC.ATC_WSATC);//うさプラ互換

            sound[2] = (int)CS_ATC.ATC_Ding;//うさプラ互換
            sound[3] = (int)CS_ATC.ATC_ORPBeep;//うさプラ互換
            if (CS_ATC.ATCEnable && CS_ATC.ATC_Noset) { sound[0] = (int)CS_ATC.ATC_WarningBell; }//うさプラ互換
            sound[261] = (int)CS_ATC.ATC_EmergencyOperationAnnounce;//調整中
        }
    }
}
