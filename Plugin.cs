﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Utilla;
using Utilla.Attributes;

namespace GorillaTagModTemplateProject
{
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom;
        string displayText = "";
        bool showGUI = true;  //maybe add a proper clean & moveable GUI in the future...
        bool showSelfProps = false;
        Font comicSansFont;
        bool useComicSans = true;
        int fontSize = 18;
        bool showPropertyValues = true;


        readonly string configFileName = $"{PluginInfo.GUID}.cfg";

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;

            LoadConfig();

            if (useComicSans)
            {
                comicSansFont = Font.CreateDynamicFontFromOSFont("Comic Sans MS", fontSize);
                if (comicSansFont == null)
                {
                    Debug.LogWarning("Comic Sans MS font not found, reverting to default font.");
                    useComicSans = false; 
                }
            }
        }

        void LoadConfig()
        {
            try
            {
                string path = Path.Combine(Paths.ConfigPath, configFileName);

                if (!File.Exists(path))
                {
                    string defaultConfig = $"#{PluginInfo.Name}  Config\n\n" +
                                           "#Default Value=true\n" +
                                           "UseComicSans=true\n\n" +
                                           "#Default Value=18\n" +
                                           "FontSize=18\n\n" +
                                           "#Default Value=false\n" +
                                           "showSelfProps=false\n\n" +
                                           "#Default Value=false\n" +
                                           "ShowPropertyValues=false\n";
                    File.WriteAllText(path, defaultConfig);
                }

                var lines = File.ReadAllLines(path);

                foreach (var line in lines)
                {
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        continue;

                    var split = line.Split('=');
                    if (split.Length != 2)
                        continue;

                    var key = split[0].Trim();
                    var value = split[1].Trim();

                    if (key.Equals("UseComicSans", StringComparison.OrdinalIgnoreCase))
                        bool.TryParse(value, out useComicSans);
                    else if (key.Equals("FontSize", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(value, out fontSize);
                    else if (key.Equals("showSelfProps", StringComparison.OrdinalIgnoreCase))
                        bool.TryParse(value, out showSelfProps);
                    else if (key.Equals("ShowPropertyValues", StringComparison.OrdinalIgnoreCase))
                        bool.TryParse(value, out showPropertyValues);
                }

                Debug.Log($"[{PluginInfo.Name}] Config loaded: UseComicSans={useComicSans}, FontSize={fontSize}, ShowPropertyValues={showPropertyValues}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{PluginInfo.Name}] Failed to load config: " + e);
            }
        }


        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            // Nothing needed here for now
        }

        void Update()
        {
           

            if (!inRoom || PhotonNetwork.CurrentRoom == null)
                return;

            List<string> playerLines = new List<string>();

            foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (!showSelfProps)
                {
                    if (player == PhotonNetwork.LocalPlayer)
                        continue;
                }

                if (player.CustomProperties != null && player.CustomProperties.Count > 1)
                {
                    List<string> props;

                    if (showPropertyValues)
                    {
                        props = player.CustomProperties
                            .Where(kvp => kvp.Value != null)
                            .Select(kvp => $"{kvp.Key}: {kvp.Value.ToString().Replace("\n", "")}")
                            .ToList();
                    }
                    else
                    {
                        props = player.CustomProperties.Keys
                            .Cast<string>()
                            .ToList();
                    }

                    string propsText = string.Join("  -  ", props);
                    playerLines.Add($"{player.NickName} :    {propsText}\n");

                }
            }

            displayText = string.Join("\n", playerLines);
        }

        void OnGUI()
        {
            if (!inRoom || string.IsNullOrEmpty(displayText))
                return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontSize = fontSize;
            style.normal.textColor = Color.white;

            if (useComicSans && comicSansFont != null)
                style.font = comicSansFont;

            GUI.Label(new Rect(10, 10, Screen.width, Screen.height), displayText, style);
        }


        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            inRoom = true;
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            inRoom = false;
            displayText = "";
        }
    }
}
