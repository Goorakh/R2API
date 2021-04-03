﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API {

    [R2APISubmodule]
    // ReSharper disable once InconsistentNaming
    public static class ItemAPI {
        public static ObservableCollection<CustomItem?>? ItemDefinitions = new ObservableCollection<CustomItem?>();
        public static ObservableCollection<CustomEquipment?> EquipmentDefinitions = new ObservableCollection<CustomEquipment?>();

        private static bool _itemCatalogInitialized;
        private static bool _equipmentCatalogInitialized;

        public static int CustomItemCount, CustomEquipmentCount;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        #region ModHelper Events and Hooks

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            R2APIContentPackProvider.WhenContentPackReady += AddItemsToGame;

            IL.RoR2.CharacterModel.UpdateMaterials += MaterialFixForItemDisplayOnCharacter;
            R2API.R2APIStart += AddingItemDisplayRulesToCharacterModels;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddItemsToGame;

            IL.RoR2.CharacterModel.UpdateMaterials -= MaterialFixForItemDisplayOnCharacter;
            R2API.R2APIStart -= AddingItemDisplayRulesToCharacterModels;
        }

        private static void AddItemsToGame(ContentPack r2apiContentPack) {
            var itemDefs = new List<ItemDef>();
            foreach (var customItem in ItemDefinitions) {
                itemDefs.Add(customItem.ItemDef);

                R2API.Logger.LogInfo($"Custom Item: {customItem.ItemDef.name} added");
            }

            r2apiContentPack.itemDefs = itemDefs.ToArray();
            _itemCatalogInitialized = true;

            var equipmentDefs = new List<EquipmentDef>();
            foreach (var customEquipment in EquipmentDefinitions) {
                equipmentDefs.Add(customEquipment.EquipmentDef);

                R2API.Logger.LogInfo($"Custom Equipment: {customEquipment.EquipmentDef.name} added");
            }

            LoadRelatedAPIs();

            r2apiContentPack.equipmentDefs = equipmentDefs.ToArray();
            _equipmentCatalogInitialized = true;
        }

        private static void LoadRelatedAPIs() {
            if (!ItemDropAPI.Loaded) {
                try {
                    ItemDropAPI.SetHooks();
                    ItemDropAPI.Loaded = true;
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"ItemDropAPI hooks failed to initialize. Disabling the submodule. {e}");
                    ItemDropAPI.UnsetHooks();
                }
            }

            if (!MonsterItemsAPI.Loaded) {
                try {
                    MonsterItemsAPI.SetHooks();
                    MonsterItemsAPI.Loaded = true;
                }
                catch (Exception e) {
                    R2API.Logger.LogError($"MonsterItemsAPI hooks failed to initialize. Disabling the submodule. {e}");
                    MonsterItemsAPI.UnsetHooks();
                }
            }
        }

        #endregion ModHelper Events and Hooks

        #region Add Methods

        /// <summary>
        /// Add a custom item to the list of available items.
        /// Value for ItemDef.ItemIndex can be ignored.
        /// We can't give you the ItemIndex anymore in the method return param. Instead use ItemCatalog.FindItemIndex after catalog are init
        /// If this is called after the ItemCatalog inits then this will return false and ignore the custom item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(CustomItem? item) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ItemAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ItemAPI)})]");
            }

            if (_itemCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add item: {item.ItemDef.nameToken} after the item list was created");
            }

            if (item.ItemDef == null) {
                R2API.Logger.LogError("Your ItemDef is null ! Can't add your item.");
            }

            if (string.IsNullOrEmpty(item.ItemDef.name)) {
                R2API.Logger.LogError("Your ItemDef.name is null or empty ! Can't add your item.");
            }

            bool xmlSafe = false;
            try {
                XElement element = new XElement(item.ItemDef.name);
                xmlSafe = true;
            }
            catch {
                R2API.Logger.LogError($"Custom item '{item.ItemDef.name}' is not XMLsafe. Item not added.");
            }
            if (xmlSafe) {
                ItemDefinitions.Add(item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a custom equipment item to the list of available items.
        /// Value for EquipmentDef.equipmentIndex can be ignored.
        /// We can't give you the EquipmentIndex anymore in the method return param. Instead use EquipmentCatalog.FindEquipmentIndex after catalog are init
        /// If this is called after the EquipmentCatalog inits then this will return false and ignore the custom equipment item.
        /// </summary>
        /// <param name="item">The equipment item to add.</param>
        /// <returns>true if added, false otherwise</returns>
        public static bool Add(CustomEquipment? item) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(ItemAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(ItemAPI)})]");
            }

            if (_equipmentCatalogInitialized) {
                R2API.Logger.LogError($"Too late ! Tried to add equipment item: {item.EquipmentDef.nameToken} after the equipment list was created");
            }

            if (item.EquipmentDef == null) {
                R2API.Logger.LogError("Your EquipmentDef is null ! Can't add your Equipment.");
            }

            if (string.IsNullOrEmpty(item.EquipmentDef.name)) {
                R2API.Logger.LogError("Your EquipmentDef.name is null or empty ! Can't add your Equipment.");
            }

            bool xmlSafe = false;
            try {
                XElement element = new XElement(item.EquipmentDef.name);
                xmlSafe = true;
            }
            catch {
                R2API.Logger.LogError($"Custom equipment '{item.EquipmentDef.name}' is not XMLsafe. Item not added.");
            }
            if (xmlSafe) {
                EquipmentDefinitions.Add(item);
                return true;
            }

            return false;
        }

        #endregion Add Methods

        #region ItemDisplay Hooks

        // With how unfriendly it is to makes your 3D Prefab work with shaders from the game,
        // makes it so that if the custom prefab doesnt have rendering support for when the player is cloaked, or burning, still display the item on the player.
        private static void MaterialFixForItemDisplayOnCharacter(ILContext il) {
            var cursor = new ILCursor(il);
            var forCounterLoc = 0;
            var itemDisplayLoc = 0;

            cursor.GotoNext(
                i => i.MatchLdarg(0),
                i => i.MatchLdfld("RoR2.CharacterModel", "parentedPrefabDisplays"),
                i => i.MatchLdloc(out forCounterLoc)
            );

            cursor.GotoNext(
                i => i.MatchCallOrCallvirt("RoR2.CharacterModel/ParentedPrefabDisplay", "get_itemDisplay"),
                i => i.MatchStloc(out itemDisplayLoc)
            );
            cursor.Index += 2;

            cursor.Emit(OpCodes.Ldloc, itemDisplayLoc);
            cursor.Emit(OpCodes.Call, typeof(Object).GetMethodCached("op_Implicit"));
            cursor.Emit(OpCodes.Brfalse, cursor.MarkLabel());
            var brFalsePos = cursor.Index - 1;

            cursor.GotoNext(
                i => i.MatchLdloc(forCounterLoc)
            );
            var label = cursor.MarkLabel();

            cursor.Index = brFalsePos;
            cursor.Next.Operand = label;
        }

        // todo : allow override of existing item display rules
        // This method only allow the addition of custom rules.
        //
		private static void AddingItemDisplayRulesToCharacterModels(object _, EventArgs __) {
            foreach (var bodyPrefab in BodyCatalog.allBodyPrefabs) {
                var characterModel = bodyPrefab.GetComponentInChildren<CharacterModel>();
                if (characterModel != null) {
                    if (!characterModel.itemDisplayRuleSet) {
                        characterModel.itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
                    }

                    foreach (var customItem in ItemDefinitions) {
                        var customRules = customItem.ItemDisplayRules;
                        if (customRules != null) {
                            //if a specific rule for this model exists, or the model has no rules for this item
                            if (customRules.TryGetRules(characterModel.name, out var rules) ||
                                customRules.TryGetRules(bodyPrefab.name, out rules) || 
                                characterModel.itemDisplayRuleSet.GetItemDisplayRuleGroup(customItem.ItemDef.itemIndex).rules == null) {
                                characterModel.itemDisplayRuleSet.SetDisplayRuleGroup(customItem.ItemDef, new DisplayRuleGroup { rules = rules });
                            }
                        }
                    }

                    foreach (var customEquipment in EquipmentDefinitions) {
                        var customRules = customEquipment.ItemDisplayRules;
                        if (customRules != null) {
                            //if a specific rule for this model exists, or the model has no rules for this equipment
                            if (customRules.TryGetRules(characterModel.name, out var rules) ||
                                customRules.TryGetRules(bodyPrefab.name, out rules) ||
                                characterModel.itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(customEquipment.EquipmentDef.equipmentIndex).rules == null) {
                                characterModel.itemDisplayRuleSet.SetDisplayRuleGroup(customEquipment.EquipmentDef, new DisplayRuleGroup { rules = rules });
                            }
                        }
                    }

                    characterModel.itemDisplayRuleSet.GenerateRuntimeValues();
                }
            }
        }

        #endregion ItemDisplay Hooks
    }

    public class CustomItem {
        public ItemDef? ItemDef;
        public ItemDisplayRuleDict? ItemDisplayRules;

        public CustomItem(ItemDef? itemDef, ItemDisplayRule[]? itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomItem(ItemDef? itemDef, ItemDisplayRuleDict? itemDisplayRules) {
            ItemDef = itemDef;
            ItemDisplayRules = itemDisplayRules;
        }
    }

    public class CustomEquipment {
        public EquipmentDef? EquipmentDef;
        public ItemDisplayRuleDict? ItemDisplayRules;

        public CustomEquipment(EquipmentDef? equipmentDef, ItemDisplayRule[]? itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = new ItemDisplayRuleDict(itemDisplayRules);
        }

        public CustomEquipment(EquipmentDef? equipmentDef, ItemDisplayRuleDict? itemDisplayRules) {
            EquipmentDef = equipmentDef;
            ItemDisplayRules = itemDisplayRules;
        }
    }

    public class ItemDisplayRuleDict {

        /// <summary>
        /// Get the applicable rule for this charactermodel. Returns the default rules if no specific rule is found.
        /// </summary>
        /// <param name="bodyPrefabName">The model to look for. Null and empty strings are also accepted.</param>
        /// <returns>The item display rules for this model, or the default rules if no specifics are found.</returns>
        public ItemDisplayRule[]? this[string? bodyPrefabName] {
            get {
                if (string.IsNullOrEmpty(bodyPrefabName) || !Dictionary.ContainsKey(bodyPrefabName))
                    return DefaultRules;
                else
                    return Dictionary[bodyPrefabName];
            }
            set {
                if (string.IsNullOrEmpty(bodyPrefabName)) {
                    R2API.Logger.LogWarning("DefaultRules overwritten with Indexer! Please set them with the constructor instead!");
                    DefaultRules = value;
                    return;
                }

                if (Dictionary.ContainsKey(bodyPrefabName)) {
                    Dictionary[bodyPrefabName] = value;
                }
                else {
                    Dictionary.Add(bodyPrefabName, value);
                }
            }
        }


        /// <summary>
        /// Equivalent to using the set property of the indexer, but added bonus is the ability to ignore the array wrapper normally needed.
        /// </summary>
        /// <param name="bodyPrefabName"></param>
        /// <param name="itemDisplayRules"></param>
        public void Add(string? bodyPrefabName, params ItemDisplayRule[]? itemDisplayRules) {
            this[bodyPrefabName] = itemDisplayRules;
        }

        /// <summary>
        /// Safe way of getting a characters rules, with the promise that the out is always filled.
        /// </summary>
        /// <param name="bodyPrefabName"></param>
        /// <param name="itemDisplayRules">The specific rules for this model, or if false is returned, the default rules.</param>
        /// <returns>True if there's a specific rule for this model. False otherwise.</returns>
        public bool TryGetRules(string? bodyPrefabName, out ItemDisplayRule[] itemDisplayRules) {
            itemDisplayRules = this[bodyPrefabName];
            return bodyPrefabName != null && Dictionary.ContainsKey(bodyPrefabName);
        }

        /// <summary>
        /// The default rules to apply when no matching model is found.
        /// </summary>
        public ItemDisplayRule[]? DefaultRules { get; private set; }

        private readonly Dictionary<string, ItemDisplayRule[]?> Dictionary;

        public ItemDisplayRuleDict(params ItemDisplayRule[]? defaultRules) {
            DefaultRules = defaultRules;
            Dictionary = new Dictionary<string, ItemDisplayRule[]?>();
        }
    }
}
