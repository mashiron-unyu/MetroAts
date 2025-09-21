using BveEx.Extensions.Native;
using BveEx.Extensions.PreTrainPatch;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TobuSignal {
    [Plugin(PluginType.VehiclePlugin)]
    public partial class TobuSignal : AssemblyPluginBase {

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
                if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49 && Config.EnableATC) {
                    //T-DATC
                    if (T_DATC.ATCEnable) {
                        T_DATC.Tick(state, sectionManager, handles);
                        AtsHandles.BrakeNotch = Math.Max(AtsHandles.BrakeNotch, T_DATC.BrakeCommand);
                        if (T_DATC.BrakeCommand > 0) BrakeTriggered = true;
                    } else {
                        if (TSP_ATS.ATSEnable) {
                            TSP_ATS.Disable();
                            T_DATC.SwitchFromATS();
                            Sound_Switchover = AtsSoundControlInstruction.Play;
                        } else {
                            T_DATC.Init(state.Time);
                        }
                    }
                } else {
                    //TSP-ATS
                    if (TSP_ATS.ATSEnable) {
                        TSP_ATS.Tick(state);
                        if (TSP_ATS.BrakeCommand > 0) {
                            AtsHandles.BrakeNotch = Math.Max(Math.Min(AtsHandles.BrakeNotch, vehicleSpec.BrakeNotches + 1), TSP_ATS.BrakeCommand);
                            BrakeTriggered = true;
                        }
                    } else {
                        if (T_DATC.ATCEnable) {
                            T_DATC.Disable();
                            TSP_ATS.SwitchFromATC();
                            Sound_Switchover = AtsSoundControlInstruction.Play;
                        } else {
                            TSP_ATS.Init(state.Time);
                        }
                    }
                }
                if (currentSection.CurrentSignalIndex >= 9 && currentSection.CurrentSignalIndex != 34 && currentSection.CurrentSignalIndex < 49 && !StandAloneMode)
                    sound[0] = corePlugin.SignalSWPos == MetroAts.SignalSWList.Tobu ?//うさプラ互換
                        (int)AtsSoundControlInstruction.Stop : (int)AtsSoundControlInstruction.PlayLooping;
                if (!StandAloneMode) {
                    if (!corePlugin.SubPluginEnabled) corePlugin.SubPluginEnabled = true;
                    if (corePlugin.KeyPos != MetroAts.KeyPosList.Tobu || corePlugin.SignalSWPos != MetroAts.SignalSWList.Tobu) {
                        BrakeTriggered = false;
                        SignalEnable = false;
                        T_DATC.ResetAll();
                        TSP_ATS.ResetAll();
                        sound[0] = (int)AtsSoundControlInstruction.Stop;//うさプラ互換
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
                if (!StandAloneMode) {
                    Keyin = corePlugin.KeyPos == MetroAts.KeyPosList.Tobu;
                    if (!SignalEnable && Keyin && (corePlugin.SignalSWPos == MetroAts.SignalSWList.Tobu) && handles.BrakeNotch != vehicleSpec.BrakeNotches + 1)
                        SignalEnable = true;
                } else {
                    if (!SignalEnable && Keyin && handles.BrakeNotch != vehicleSpec.BrakeNotches + 1)
                        SignalEnable = true;
                    AtsHandles.BrakeNotch = vehicleSpec.BrakeNotches + 1;
                    AtsHandles.ReverserPosition = ReverserPosition.N;
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
            Sound_Keyin = Sound_Keyout = Sound_ResetSW = Sound_Switchover = AtsSoundControlInstruction.Continue;

            //handles.PowerNotch = 0;
            //handles.BrakeNotch = 0;
            //handles.ConstantSpeedMode = ConstantSpeedMode.Continue;
            //handles.ReverserPosition = ReverserPosition.N;
        }

        private static void UpdatePanelAndSound(IList<int> panel,IList<int> sound) {
            sound[28] = (int)Sound_ResetSW;//うさプラ互換

            //panel うさプラ互換
            panel[102] = Convert.ToInt32(T_DATC.ATC_01);
            panel[104] = Convert.ToInt32(T_DATC.ATC_10);
            panel[105] = Convert.ToInt32(T_DATC.ATC_15);
            panel[106] = Convert.ToInt32(T_DATC.ATC_20);
            panel[107] = Convert.ToInt32(T_DATC.ATC_25);
            panel[108] = Convert.ToInt32(T_DATC.ATC_30);
            panel[109] = Convert.ToInt32(T_DATC.ATC_35);
            panel[110] = Convert.ToInt32(T_DATC.ATC_40);
            panel[111] = Convert.ToInt32(T_DATC.ATC_45);
            panel[112] = Convert.ToInt32(T_DATC.ATC_50);
            panel[113] = Convert.ToInt32(T_DATC.ATC_55);
            panel[114] = Convert.ToInt32(T_DATC.ATC_60);
            panel[115] = Convert.ToInt32(T_DATC.ATC_65);
            panel[116] = Convert.ToInt32(T_DATC.ATC_70);
            panel[117] = Convert.ToInt32(T_DATC.ATC_75);
            panel[118] = Convert.ToInt32(T_DATC.ATC_80);
            panel[119] = Convert.ToInt32(T_DATC.ATC_85);
            panel[120] = Convert.ToInt32(T_DATC.ATC_90);
            panel[121] = Convert.ToInt32(T_DATC.ATC_95);
            panel[122] = Convert.ToInt32(T_DATC.ATC_100);
            panel[123] = Convert.ToInt32(T_DATC.ATC_105);
            panel[124] = Convert.ToInt32(T_DATC.ATC_110);

            if (!Config.SeparateATCGRlamp) {
                panel[131] = Convert.ToInt32(T_DATC.ATC_Stop);
                panel[132] = Convert.ToInt32(T_DATC.ATC_Proceed);
            }
            else {
                panel[316] = Convert.ToInt32(T_DATC.ATC_Stop);
                panel[317] = Convert.ToInt32(T_DATC.ATC_Proceed);
            }


            panel[134] = Convert.ToInt32(T_DATC.ATC_P);
            panel[101] = Convert.ToInt32(T_DATC.ATC_X);

            panel[135] = T_DATC.ORPNeedle;
            panel[311] = T_DATC.ATCNeedle;
            panel[310] = Convert.ToInt32(T_DATC.ATCNeedle_Disappear);
            panel[129] = T_DATC.ATC_EndPointDistance;
            panel[130] = T_DATC.ATC_SwitcherPosition;

            panel[74] = Convert.ToInt32(T_DATC.ATC_TobuATC);
            panel[75] = Convert.ToInt32(T_DATC.ATC_Depot);
            panel[77] = Convert.ToInt32(T_DATC.ATC_ServiceBrake);
            panel[76] = Convert.ToInt32(T_DATC.ATC_EmergencyBrake);
            panel[326] = Convert.ToInt32(T_DATC.ATC_EmergencyOperation);//調整中
            panel[252] = Convert.ToInt32(T_DATC.ATC_StationStop);
            panel[128] = Convert.ToInt32(T_DATC.ATC_PatternApproach);

            panel[41] = Convert.ToInt32(TSP_ATS.ATS_TobuAts);
            panel[44] = Convert.ToInt32(TSP_ATS.ATS_ATSEmergencyBrake);
            panel[252] = Convert.ToInt32(TSP_ATS.ATS_StopAnnounce);
            panel[333] = Convert.ToInt32(TSP_ATS.ATS_EmergencyOperation);//調整中
            panel[331] = Convert.ToInt32(TSP_ATS.ATS_Confirm);//調整中
            panel[43] = Convert.ToInt32(TSP_ATS.ATS_60);
            panel[42] = Convert.ToInt32(TSP_ATS.ATS_15);

            //sound
            sound[2] = (int)T_DATC.ATC_Ding;//うさプラ互換
            var soundPlayMode = SoundPlayCommands.GetMode(sound[116]);
            if (soundPlayMode == SoundPlayMode.Continue && T_DATC.ATC_PatternApproachBeep == AtsSoundControlInstruction.Play)
                sound[116] = (int)AtsSoundControlInstruction.Stop;
            sound[116] = (int)T_DATC.ATC_PatternApproachBeep;
            sound[117] = (int)T_DATC.ATC_StationStopAnnounce;
            sound[118] = (int)Sound_Switchover;//うさプラ互換
            sound[268] = (int)T_DATC.ATC_EmergencyOperationAnnounce;//調整中
        }
    }
}
