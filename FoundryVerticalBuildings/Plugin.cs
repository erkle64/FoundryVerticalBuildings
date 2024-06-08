using C3;
using C3.ModKit;
using HarmonyLib;
using System;
using System.IO;
using Unfoundry;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeersMod
{
    [UnfoundryMod(GUID)]
    public class Plugin : UnfoundryPlugin
    {
        public const string
            MODNAME = "SeersMod",
            AUTHOR = "seer",
            GUID = AUTHOR + "." + MODNAME,
            VERSION = "0.1.0";

        public static LogSource log;
        public static TypedConfigEntry<bool> verbose;

        public Plugin()
        {
            log = new LogSource(MODNAME);

            new Config(GUID)
                .Group("Log")
                .Entry(out verbose, "verbose", false, true, "Log extra information.")
                .EndGroup()
                .Load()
                .Save();

            //BuildableObjectTemplate
        }

        public override void Load(Mod mod)
        {
            log.Log($"Loading {MODNAME}");
        }

        [HarmonyPatch]
        public static class Patch
        {


            [HarmonyPatch(typeof(ItemTemplate), nameof(ItemTemplate.onLoad))]
            [HarmonyPrefix]
            public static void onLoadItemTemplate(ItemTemplate __instance)
            {
                //only patch the base conveyor balancer
                if (__instance.identifier != "_base_conveyor_balancer_i")
                {
                    return;
                }
                log.Log($"onLoadItemTemplate - {__instance.identifier} -- {__instance.modIdentifier}");

                var buildingTemplates = ItemTemplateManager.getAllBuildableObjectTemplates();
                var templateHash = BuildableObjectTemplate.generateStringHash(__instance.buildableObjectIdentifer);
                if (!buildingTemplates.TryGetValue(templateHash, out var template))
                {
                    log.LogError($"Template building {__instance.buildableObjectIdentifer} not found!");
                    return;
                }

                var newBuilding = CreateVerticalTemplate(template);
                AssetManager.registerAsset(newBuilding, true);

                __instance.toggleableModeType = ItemTemplate.ItemTemplateToggleableModeTypes.MultipleBuildings;
                __instance.toggleableModes = new ItemTemplate.ItemMode[]
                    {
                        new ItemTemplate.ItemMode()
                        {
                            name = "Horizontal",
                            identifier = __instance.buildableObjectIdentifer,
                            isDefault = false,
                            //icon = __instance.icon //Todo: Add icon
                        },
                        new ItemTemplate.ItemMode()
                        {
                            name = "Vertical",
                            identifier = newBuilding.identifier,//$"{__instance.buildableObjectIdentifer}_v",
                            isDefault = false,
                            //buildableObjectTemplate = CreateVerticalTemplate(template),
                            //icon = __instance.icon //Todo: Add icon
                        },
                    };
            }



           // [HarmonyPatch(typeof(BuildableObjectTemplate), nameof(BuildableObjectTemplate.onLoad))]
           // [HarmonyPrefix]
            public static void onLoadBuildableObjectTemplate(BuildableObjectTemplate __instance)
            {

                //only patch the base conveyor balancer
                if (__instance.identifier != "_base_conveyor_balancer_i")
                {
                    return;
                }

                log.Log($"onLoadBuildableObjectTemplate - {__instance.identifier} -- {__instance.modIdentifier}");
                log.Log($"__instance.prefabOnDisk.transform.rotation - {__instance.prefabOnDisk.transform.rotation}");
                log.Log($"__instance.prefabOnDisk.transform.position - {__instance.prefabOnDisk.transform.position}");
                log.Log($"__instance.size - {__instance.size}");
                log.Log($"__instance.prefabOnDisk.name - {__instance.prefabOnDisk.name}");


                BuildableObjectTemplate instance = Object.Instantiate(__instance);
                instance.identifier = $"{instance.identifier}_v";
                var prefab = instance.prefabOnDisk;

                //prefab.transform.Rotate(90, 0, 0);

                prefab.iterateChildren((child) =>
                {
                    if (verbose.Get())
                    {
                        log.Log($"child.name - {child.name}");
                        log.Log($"child.transform.rotation - {child.transform.rotation}");
                        log.Log($"child.transform.position - {child.transform.position}");
                    }

                    switch(child.name)
                    {
                        case "convey_01_straight (1)":
                            child.transform.position = new Vector3(-0.5f, 0, 0);
                            break;
                        case "convey_01_straight (2)":
                            child.transform.position = new Vector3(-0.5f, 1, 0);
                            break;
                        case "convey_01_straight (3)":
                            child.transform.position = new Vector3(0.5f, 0, 0);
                            break;
                        case "convey_01_straight (4)":
                            child.transform.position = new Vector3(0.5f, 1, 0);
                            break;
                        case "TerrainTileCollider":
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            child.transform.position = new Vector3(0, 0, 0);
                            break;
                        case "Conveyor_balancer":
                            child.transform.position = new Vector3(0, 1, 0.5f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;
                        case "BalancerControlPanel_IN":
                            child.transform.position = new Vector3(-0.359f, 1.5f, -0.359f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;
                        case "BalancerControlPanel_OUT":
                            child.transform.position = new Vector3(0.359f, 1.5f, -0.359f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;

                        default:
                            log.Log($"Unexpected child!!!!! - {child.name}");
                            break;
                    }
                    if (verbose.Get())
                    {
                        log.Log($"Final: child.transform.rotation - {child.transform.rotation}");
                        log.Log($"Final: child.transform.position - {child.transform.position}");
                    }


                }, false);

                //If the building’s original size is (2, 1, 2) and you want to rotate it vertically from the ground up, the new size would be (2, 2, 1).
                //This is because you’re essentially swapping the Y (height) and Z (depth) dimensions.
                instance.size = new Vector3Int(instance.size.x, instance.size.z, instance.size.y);

                log.Log($"Trying to add new asset to the game");
                AssetManager.registerAsset(instance, true);
                instance.onLoad();
            }


            private static BuildableObjectTemplate CreateVerticalTemplate(BuildableObjectTemplate original)
            {
                log.Log($"CreateVerticalTemplate - {original.identifier} -- {original.modIdentifier}");
                BuildableObjectTemplate instance = Object.Instantiate(original);
                instance.identifier = $"{instance.identifier}_v";
                var prefab = instance.prefabOnDisk;

                //prefab.transform.Rotate(90, 0, 0);

                prefab.iterateChildren((child) =>
                {
                    if (verbose.Get())
                    {
                        log.Log($"child.name - {child.name}");
                        log.Log($"child.transform.rotation - {child.transform.rotation}");
                        log.Log($"child.transform.position - {child.transform.position}");
                    }

                    switch (child.name)
                    {
                        case "convey_01_straight (1)":
                            child.transform.position = new Vector3(-0.5f, 0, 0);
                            break;
                        case "convey_01_straight (2)":
                            child.transform.position = new Vector3(-0.5f, 1, 0);
                            break;
                        case "convey_01_straight (3)":
                            child.transform.position = new Vector3(0.5f, 0, 0);
                            break;
                        case "convey_01_straight (4)":
                            child.transform.position = new Vector3(0.5f, 1, 0);
                            break;
                        case "TerrainTileCollider":
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            child.transform.position = new Vector3(0, 0, 0);
                            break;
                        case "Conveyor_balancer":
                            child.transform.position = new Vector3(0, 1, 0.5f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;
                        case "BalancerControlPanel_IN":
                            child.transform.position = new Vector3(-0.359f, 1.5f, -0.359f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;
                        case "BalancerControlPanel_OUT":
                            child.transform.position = new Vector3(0.359f, 1.5f, -0.359f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;

                        default:
                            log.Log($"Unexpected child!!!!! - {child.name}");
                            break;
                    }
                    if (verbose.Get())
                    {
                        log.Log($"Final: child.transform.rotation - {child.transform.rotation}");
                        log.Log($"Final: child.transform.position - {child.transform.position}");
                    }


                }, false);

                //If the building’s original size is (2, 1, 2) and you want to rotate it vertically from the ground up, the new size would be (2, 2, 1).
                //This is because you’re essentially swapping the Y (height) and Z (depth) dimensions.
                instance.size = new Vector3Int(instance.size.x, instance.size.z, instance.size.y);
                return instance;
            }


            public static void onLoadBuildableObjectTemplate_old(BuildableObjectTemplate __instance)
            {
                //only patch the base conveyor balancer
                if (__instance.identifier != "_base_conveyor_balancer_i")
                {
                    return;
                }

                log.Log($"onLoadBuildableObjectTemplate - {__instance.identifier} -- {__instance.modIdentifier}");
                log.Log($"__instance.prefabOnDisk.transform.rotation - {__instance.prefabOnDisk.transform.rotation}");
                log.Log($"__instance.prefabOnDisk.transform.position - {__instance.prefabOnDisk.transform.position}");
                log.Log($"__instance.size - {__instance.size}");
                log.Log($"__instance.prefabOnDisk.name - {__instance.prefabOnDisk.name}");
                //var clonedPrefab = GameObject.Instantiate(__instance.prefabOnDisk);
                //clonedPrefab.iterateChildren((child) =>
                //{
                //    log.Log($"child.name - {child.name}");
                //}, false);


                //clonedPrefab.transform.rotation = Quaternion.Euler(90, 90, 0);
                //var newRoot = new GameObject("My New Root");
                //clonedPrefab.transform.SetParent(newRoot.transform, false);
                //__instance.prefabOnDisk = newRoot;



                var prefab = __instance.prefabOnDisk;

                //prefab.transform.Rotate(90, 0, 0);

                prefab.iterateChildren((child) =>
                {
                    if (verbose.Get())
                    {
                        log.Log($"child.name - {child.name}");
                        log.Log($"child.transform.rotation - {child.transform.rotation}");
                        log.Log($"child.transform.position - {child.transform.position}");
                    }

                    switch (child.name)
                    {
                        case "convey_01_straight (1)":
                            child.transform.position = new Vector3(-0.5f, 0, 0);
                            break;
                        case "convey_01_straight (2)":
                            child.transform.position = new Vector3(-0.5f, 1, 0);
                            break;
                        case "convey_01_straight (3)":
                            child.transform.position = new Vector3(0.5f, 0, 0);
                            break;
                        case "convey_01_straight (4)":
                            child.transform.position = new Vector3(0.5f, 1, 0);
                            break;
                        case "TerrainTileCollider":
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            child.transform.position = new Vector3(0, 0, 0);
                            break;
                        case "Conveyor_balancer":
                            child.transform.position = new Vector3(0, 1, 0.5f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;
                        case "BalancerControlPanel_IN":
                            child.transform.position = new Vector3(-0.359f, 1.5f, -0.359f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;
                        case "BalancerControlPanel_OUT":
                            child.transform.position = new Vector3(0.359f, 1.5f, -0.359f);
                            child.transform.rotation = Quaternion.Euler(child.transform.rotation.x - 90, child.transform.rotation.y, child.transform.rotation.z);
                            break;

                        default:
                            log.Log($"Unexpected child!!!!! - {child.name}");
                            break;
                    }
                    if (verbose.Get())
                    {
                        log.Log($"Final: child.transform.rotation - {child.transform.rotation}");
                        log.Log($"Final: child.transform.position - {child.transform.position}");
                    }


                }, false);

                //If the building’s original size is (2, 1, 2) and you want to rotate it vertically from the ground up, the new size would be (2, 2, 1).
                //This is because you’re essentially swapping the Y (height) and Z (depth) dimensions.
                __instance.size = new Vector3Int(__instance.size.x, __instance.size.z, __instance.size.y);
            }

        }
    }
}




