using Harmony12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;

namespace HappyLife
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save<Settings>(this, modEntry);
        }
        public int numChild = 1;
        public bool npcUpLimit = false;
        public int numBuild = 0;
        public int numFavor = 0;
        public bool skip = true;

    }

    public static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            settings = Settings.Load<Settings>(modEntry);
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (!value)
                return false;

            enabled = value;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical("Box", new GUILayoutOption[0]);
            Main.settings.skip = GUILayout.Toggle(Main.settings.skip, "跳过测试版说明。", new GUILayoutOption[0]);
            GUILayout.Label("人物可生育孩子总数量", new GUILayoutOption[0]);
            Main.settings.npcUpLimit = GUILayout.Toggle(Main.settings.npcUpLimit, "对NPC生效", new GUILayoutOption[0]);
            Main.settings.numChild = GUILayout.SelectionGrid(Main.settings.numChild, new string[]
            {
        "少生",
        "正常",
        "2倍",
        "3倍",
        "4倍"
            }, 5, new GUILayoutOption[0]);
            GUILayout.Label("建筑每次升级", new GUILayoutOption[0]);
            Main.settings.numBuild = GUILayout.SelectionGrid(Main.settings.numBuild, new string[]
            {
        "1级",
        "2级",
        "3级",
        "4级",
        "5级"
            }, 5, new GUILayoutOption[0]);
            GUILayout.Label("好感增加", new GUILayoutOption[0]);
            Main.settings.numFavor = GUILayout.SelectionGrid(Main.settings.numFavor, new string[]
            {
        "1倍",
        "2倍",
        "3倍",
        "4倍",
        "5倍"
            }, 5, new GUILayoutOption[0]);         
            GUILayout.EndVertical();
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(PeopleLifeAI), "AISetChildren")]
    public static class PeopleLifeAI_AISetChildren_Patch
    {
        static bool Prefix(PeopleLifeAI __instance, ref int fatherId, ref int motherId, ref int setFather, ref int setMother)
        {
            if (!Main.enabled)
            {
                return true;
            }
            //Main.Logger.Log("生孩子倍率：" + Main.settings.numChild);
            int num = 50;
            if (Main.settings.numChild != 0)
            {
                num = 100 * Main.settings.numChild;
            }

            int num4 = num;
            if (int.Parse(DateFile.instance.GetActorDate(motherId, 14, false)) == 2)
            {
                if (!DateFile.instance.HaveLifeDate(motherId, 901) && UnityEngine.Random.Range(0, 15000) < int.Parse(DateFile.instance.GetActorDate(fatherId, 24, true)) * int.Parse(DateFile.instance.GetActorDate(motherId, 24, true)))
                {
                    
                    int num2 = DateFile.instance.MianActorID();
                    bool flag = fatherId == num2 || motherId == num2;
                    int num3 = (!flag) ? Main.settings.npcUpLimit || Main.settings.numChild == 0 ? 50 : 50 * Main.settings.numChild : 25;               
                    num -= DateFile.instance.GetActorSocial(fatherId, 310, false).Count * num3;
                    num -= DateFile.instance.GetActorSocial(motherId, 310, false).Count * num3;
                    if (UnityEngine.Random.Range(0, num4) < num)
                    {
                        DateFile.instance.ChangeActorFeature(motherId, 4002, 4003);
                        if (flag && UnityEngine.Random.Range(0, 100) < (DateFile.instance.getQuquTrun - 100) / 10)
                        {
                            DateFile.instance.getQuquTrun = 0;
                            DateFile.instance.actorLife[motherId].Add(901, new List<int>
                    {
                        1042,
                        fatherId,
                        motherId,
                        setFather,
                        setMother
                    });
                        }
                        else
                        {
                            DateFile.instance.actorLife[motherId].Add(901, new List<int>
                    {
                        UnityEngine.Random.Range(6, 10),
                        fatherId,
                        motherId,
                        setFather,
                        setMother
                    });
                        }
                    }
                }
            }
            return false;
        }

    }

    [HarmonyPatch(typeof(DateFile), "SetHomeBuildingValue")]
    public static class DateFile_SetHomeBuildingValue_Patch
    {
        static void Prefix(DateFile __instance, ref int partId, ref int placeId, ref int buildingIndex, ref int dateIndex, ref int dateValue)
        {

            if (!Main.enabled)
            {
                return;
            }
            //Main.Logger.Log("建筑升级倍率：" + Main.settings.numBuild+1);
            if (dateIndex == 1)
            {
               
                if ( dateValue != 1)
                {
                    dateValue += Main.settings.numBuild;
                    int max = int.Parse(DateFile.instance.basehomePlaceDate[DateFile.instance.homeBuildingsDate[partId][placeId][buildingIndex][0]][1]);
                    if (dateValue > max)
                    {
                        dateValue = max;
                    }


                }
            }
        }
    }

    [HarmonyPatch(typeof(DateFile), "ChangeFavor")]
    public static class DateFile_ChangeFavor_Patch
    {
        static void Prefix(DateFile __instance, ref int value)
        {
            if (!Main.enabled)
            {
                return;
            }
            //Main.Logger.Log("好感倍率：" + Main.settings.numFavor+1);
            if (value > 0&&Main.settings.numFavor>0)
            {
                value *= (Main.settings.numFavor+1);
            }
        }
    }

    [HarmonyPatch(typeof(MainMenu), "CloseStartMask")]
    public static class MainMenu_CloseStartMask_Patch
    {
        static void Prefix(MainMenu __instance, ref bool ___showStartMassage)
        {
            if (!Main.enabled || !Main.settings.skip)
            {
                return;
            }
            ___showStartMassage = false;
        }
    }

}

