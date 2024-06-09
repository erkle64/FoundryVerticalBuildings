using C3;
using C3.ModKit;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unfoundry;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeersMod
{
    [UnfoundryMod(GUID)]
    public class Plugin : UnfoundryPlugin
    {
        public const string
            MODNAME = "FoundryVerticalBuildings",
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



                // Create a new 2D sprite texture for horizontal icon
                Texture2D horizontalSpriteTexture = new Texture2D(128, 128);

                // Fill the texture with color
                Color fillColor = Color.yellow;
                Color[] fillPixels = new Color[horizontalSpriteTexture.width * horizontalSpriteTexture.height];
                for (int i = 0; i < fillPixels.Length; i++)
                {
                    fillPixels[i] = fillColor;
                }
                horizontalSpriteTexture.SetPixels(fillPixels);
                horizontalSpriteTexture.Apply();

                // Create a new Sprite for horizontal icon
                Sprite horizontalSprite = Sprite.Create(horizontalSpriteTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100.0f);

                Texture2D verticalSpriteTexture = new Texture2D(128, 128);
                verticalSpriteTexture.SetPixels(fillPixels);
                verticalSpriteTexture.Apply();
                Sprite verticalSprite = Sprite.Create(verticalSpriteTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100.0f);


                __instance.toggleableModeType = ItemTemplate.ItemTemplateToggleableModeTypes.MultipleBuildings;
                __instance.toggleableModes = new ItemTemplate.ItemMode[]
                    {
                        new ItemTemplate.ItemMode()
                        {
                            name = "Horizontal",
                            identifier = __instance.buildableObjectIdentifer,
                            isDefault = false,
                            icon = horizontalSprite //Todo: Add icon
                        },
                        new ItemTemplate.ItemMode()
                        {
                            name = "Vertical",
                            identifier = newBuilding.identifier,
                            isDefault = false,
                            icon = verticalSprite //Todo: Add icon
                        },
                    };
            }
            private static BuildableObjectTemplate CreateVerticalTemplate(BuildableObjectTemplate original)
            {
                log.Log($"CreateVerticalTemplate - {original.identifier} -- {original.modIdentifier}");
                BuildableObjectTemplate instance = Object.Instantiate(original);
                instance.identifier = $"{instance.identifier}_v";
            
                var prefab = instance.prefabOnDisk;
                prefab = Object.Instantiate(original.prefabOnDisk);

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

                //If the building�s original size is (2, 1, 2) and you want to rotate it vertically from the ground up, the new size would be (2, 2, 1).
                //This is because you�re essentially swapping the Y (height) and Z (depth) dimensions.
                instance.size = new Vector3Int(instance.size.x, instance.size.z, instance.size.y);
                instance.onLoad();
                botIdToTextureArray[instance.id] = new Texture2D[] {
                    instance.buildingPart_texture_albedo,
                    instance.buildingPart_texture_bottom_albedo,
                    instance.buildingPart_texture_side_albedo
                }.Where(x => x != null).ToArray();


                return instance;
            }

            [HarmonyPatch(typeof(TextureStreamingProcessor), nameof(TextureStreamingProcessor.OnAddedToManager))]
            [HarmonyPostfix]
            public static void TextureStreamingProcessorOnAddedToManager(TextureStreamingProcessor __instance)
            {
                botIdToTextureArray = typeof(TextureStreamingProcessor).GetField("botIdToTextureArray", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance) as Dictionary<ulong, Texture2D[]>;
            }


            public static Dictionary<ulong, Texture2D[]> botIdToTextureArray = null;
        }
    }
}




