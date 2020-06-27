using Kingmaker.Blueprints.Classes;
using ModMaker.Extensions;
using ModMaker.Utils;
using Multiclass.HarmonyPatches;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using static ModMaker.Extensions.RichText;
using static Multiclass.Main;

namespace Multiclass.Menus
{
    public class MultipleClassesSelections : ModBase.Menu.IToggleablePage
    {
        private enum SelectedCharacterType
        {
            MainCharacter,
            Companion,
            CharGenCustom
        }

        private SelectedCharacterType _selectedCharacterType = default;
        private string _selectedCharacterName = default;

        private float _classNameButtonWidth = default;

        public string Name => "Multiple Classes";

        public int Priority => 100;

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Core == null || !Core.Enabled)
                return;

            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };

            MultipleClasses.Enabled = GUIHelper.ToggleButton(MultipleClasses.Enabled, "Multiple Classes", style);

            if (MultipleClasses.Enabled)
            {
                string[] companionNames = default;
                HashSet<string> selectedMulticlassSet = default;

                GUILayout.Space(10);

                // selections
                using (new GUILayout.HorizontalScope())
                {
                    // character selections
                    using(new GUILayout.VerticalScope(GUILayout.ExpandWidth(false)))
                    {
                        string playerName = default;
                        string activeScene = Core.Mod.ActiveScene.name;
                        if (activeScene != "MainMenu" && activeScene != "Start" && Core.Mod.Player != null)
                        {
                            playerName = Core.Mod.Player.MainCharacter.Value.CharacterName;
                            companionNames = Core.Mod.Player.AllCharacters
                                .Where(c => c.IsPlayerFaction && !c.IsMainCharacter && !c.Descriptor.IsPet)
                                .Select(c => c.CharacterName)
                                .ToArray();
                        }
                        else
                        {
                            playerName = "[Player]";
                            companionNames = new string[0];
                        }

                        int selectedCharacterIndex =
                            _selectedCharacterType == SelectedCharacterType.MainCharacter ? 0 :
                            _selectedCharacterType == SelectedCharacterType.CharGenCustom ? companionNames.Length + 1 :
                            Array.FindIndex(companionNames, name => name == _selectedCharacterName) + 1;

                        GUIHelper.SelectionGrid(ref selectedCharacterIndex,
                            new[] { playerName }.Concat(companionNames).Concat(new[] { "[Custom]".Color(RGBA.silver) }).ToArray(),
                            1, null, GUILayout.ExpandWidth(false));

                        if (selectedCharacterIndex <= 0)
                        {
                            _selectedCharacterType = SelectedCharacterType.MainCharacter;
                            selectedMulticlassSet = MultipleClasses.MainCharSet;
                        }
                        else if (selectedCharacterIndex > companionNames.Length)
                        {
                            _selectedCharacterType = SelectedCharacterType.CharGenCustom;
                            selectedMulticlassSet = MultipleClasses.CharGenSet;
                        }
                        else
                        {
                            _selectedCharacterType = SelectedCharacterType.Companion;
                            _selectedCharacterName = companionNames[selectedCharacterIndex - 1];
                            selectedMulticlassSet = MultipleClasses.DemandCompanionSet(_selectedCharacterName);
                        }
                    }

                    // multiclass selections
                    using(new GUILayout.VerticalScope(GUILayout.ExpandWidth(false)))
                    {
                        foreach (BlueprintCharacterClass characterClass in Core.Mod.CharacterClasses)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                // class
                                bool selected = selectedMulticlassSet.Contains(characterClass.AssetGuid);
                                GUIHelper.ToggleButton(ref selected, characterClass.Name, ref _classNameButtonWidth,
                                    () => selectedMulticlassSet.Add(characterClass.AssetGuid),
                                    () => selectedMulticlassSet.Remove(characterClass.AssetGuid),
                                    style);

                                // archetypes
                                if (selected && characterClass.Archetypes.Any())
                                {
                                    int originalArchetype;
                                    int selectedArchetype = originalArchetype = Array.FindIndex(characterClass.Archetypes,
                                        archetype => selectedMulticlassSet.Contains(archetype.AssetGuid)) + 1;
                                    GUIHelper.Toolbar(ref selectedArchetype,
                                        new string[] { characterClass.Name }.Concat(characterClass.Archetypes.Select(item => item.Name)).ToArray());
                                    if (selectedArchetype != originalArchetype)
                                    {
                                        if (originalArchetype > 0)
                                            selectedMulticlassSet.Remove(characterClass.Archetypes[originalArchetype - 1].AssetGuid);
                                        if (selectedArchetype > 0)
                                            selectedMulticlassSet.Add(characterClass.Archetypes[selectedArchetype - 1].AssetGuid);
                                    }
                                }
                                else
                                {
                                    GUILayout.FlexibleSpace();
                                }
                            }
                        }
                    }

                    if (_selectedCharacterType == SelectedCharacterType.Companion && selectedMulticlassSet.Count == 0)
                        MultipleClasses.CompanionSets.Remove(_selectedCharacterName);
                }

                GUILayout.Space(10);

                // clear
                if (GUILayout.Button("Clear Settings For Unavailable Characters", style))
                {
                    foreach (string key in MultipleClasses.CompanionSets.Keys.Except(companionNames).ToList())
                    {
                        MultipleClasses.CompanionSets.Remove(key);
                    }
                }
                if (GUILayout.Button("Clear Settings For All Companions", style))
                {
                    MultipleClasses.CompanionSets.Clear();
                }
                if (GUILayout.Button("Clear Settings For All Characters", style))
                {
                    MultipleClasses.MainCharSet.Clear();
                    MultipleClasses.CharGenSet.Clear();
                    MultipleClasses.CompanionSets.Clear();
                }
            }
        }
    }
}
