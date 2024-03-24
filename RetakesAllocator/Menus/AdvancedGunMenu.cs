using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocatorCore;
using CounterStrikeSharp.API;
using System.Text;
using RetakesAllocator.Menus;
using RetakesAllocatorCore.Config;
using static RetakesAllocatorCore.PluginInfo;

namespace RetakesAllocator.AdvancedMenus;

public class AdvancedGunMenu
{
    public Dictionary<ulong, bool> menuon = new Dictionary<ulong, bool>();
    public Dictionary<ulong, int> mainmenu = new Dictionary<ulong, int>();
    public Dictionary<ulong, int> currentIndexDict = new Dictionary<ulong, int>();
    public Dictionary<ulong, bool> buttonPressed = new Dictionary<ulong, bool>();

    public HookResult OnEventPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        if(string.IsNullOrEmpty(Configs.GetConfigData().InGameGunMenuCenterCommands) || @event == null)return HookResult.Continue;
        var eventplayer = @event.Userid;
        var eventmessage = @event.Text;
        var player = Utilities.GetPlayerFromUserid(eventplayer);
        
        if (player == null || !player.IsValid)return HookResult.Continue;
        var playerid = player.SteamID;

        if (string.IsNullOrWhiteSpace(eventmessage)) return HookResult.Continue;
        string trimmedMessageStart = eventmessage.TrimStart();
        string message = trimmedMessageStart.TrimEnd();
        string[] CenterMenuCommands = Configs.GetConfigData().InGameGunMenuCenterCommands.Split(',');

        if (CenterMenuCommands.Any(cmd => cmd.Equals(message, StringComparison.OrdinalIgnoreCase)))
        {
            if (!menuon.ContainsKey(playerid))
            {
                menuon.Add(playerid, true);
            }
            if (!mainmenu.ContainsKey(playerid))
            {
                mainmenu.Add(playerid, 0);
            }
            if (!currentIndexDict.ContainsKey(playerid))
            {
                currentIndexDict.Add(playerid, 0);
            }
            if (!buttonPressed.ContainsKey(playerid))
            {
                buttonPressed.Add(playerid, false);
            }
        }
        return HookResult.Continue;
    }

    public void OnTick()
    {
        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        foreach (var player in playerEntities)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive || player.IsBot || player.IsHLTV) continue;
            
            var playerid = player.SteamID;
            if (menuon.ContainsKey(playerid))
            {
                string Imageleft = string.IsNullOrEmpty(Translator.Instance["menu.left.image"]) ? "" : Translator.Instance["menu.left.image"];
                string ImageRight = string.IsNullOrEmpty(Translator.Instance["menu.right.image"]) ? "" : Translator.Instance["menu.right.image"];
                string BottomMenu = string.IsNullOrEmpty(Translator.Instance["menu.bottom.text"]) ? "" : Translator.Instance["menu.bottom.text"];
                string BottomMenuOnpistol = string.IsNullOrEmpty(Translator.Instance["menu.bottom.text.pistol"]) ? "" : Translator.Instance["menu.bottom.text.pistol"];

                string[] Main = { 
                    string.IsNullOrEmpty(Translator.Instance["menu.main.tloadout"]) ? "█░ T Loadout ░█" : Translator.Instance["menu.main.tloadout"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.main.ctloadout"]) ? "█░ CT Loadout ░█" : Translator.Instance["menu.main.ctloadout"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.main.awp"]) ? "█░ AWP ░█" : Translator.Instance["menu.main.awp"]
                };

                List<string> TFullBuyList = new List<string>();
                List<string> TSecondaryList = new List<string>();
                List<string> TPistolRoundList = new List<string>();

                List<string> CTFullBuyList = new List<string>();
                List<string> CTSecondaryList = new List<string>();
                List<string> CTPistolRoundList = new List<string>();

                string[] Tloadout = { 
                    string.IsNullOrEmpty(Translator.Instance["menu.tprimary"]) ? "█ T Primary █" : Translator.Instance["menu.tprimary"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.tsecondary"]) ? "█ T Secondary █" : Translator.Instance["menu.tsecondary"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.tPistol"]) ? "█ T Pistol Round █" : Translator.Instance["menu.tPistol"]
                };
                
                string[] CTloadout = { 
                    string.IsNullOrEmpty(Translator.Instance["menu.ctprimary"]) ? "█ CT Primary █" : Translator.Instance["menu.ctprimary"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.ctsecondary"]) ? "█ CT Secondary █" : Translator.Instance["menu.ctsecondary"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.ctPistol"]) ? "█ CT Pistol Round █" : Translator.Instance["menu.ctPistol"]
                };

                string[] AWP = { 
                    string.IsNullOrEmpty(Translator.Instance["menu.awp.always"]) ? "Always" : Translator.Instance["menu.awp.always"], 
                    string.IsNullOrEmpty(Translator.Instance["menu.awp.never"]) ? "Never" : Translator.Instance["menu.awp.never"]
                };


                foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary, CsTeam.Terrorist))
                {
                    TFullBuyList.Add(weapon.GetName());
                }
                foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary, CsTeam.Terrorist))
                {
                    TSecondaryList.Add(weapon.GetName());
                }
                foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound, CsTeam.Terrorist))
                {
                    TPistolRoundList.Add(weapon.GetName());
                }

                foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.FullBuyPrimary, CsTeam.CounterTerrorist))
                {
                    CTFullBuyList.Add(weapon.GetName());
                }
                foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.Secondary, CsTeam.CounterTerrorist))
                {
                    CTSecondaryList.Add(weapon.GetName());
                }
                foreach (var weapon in WeaponHelpers.GetPossibleWeaponsForAllocationType(WeaponAllocationType.PistolRound, CsTeam.CounterTerrorist))
                {
                    CTPistolRoundList.Add(weapon.GetName());
                }

                string[] TFullBuy = TFullBuyList.ToArray();
                string[] TSecondary = TSecondaryList.ToArray();
                string[] TPistolRound = TPistolRoundList.ToArray();

                string[] CTFullBuy = CTFullBuyList.ToArray();
                string[] CTSecondary = CTSecondaryList.ToArray();
                string[] CTPistolRound = CTPistolRoundList.ToArray();

                if (player.Buttons == 0)
                {
                    buttonPressed[playerid] = false;
                }
                else if (player.Buttons == PlayerButtons.Back && !buttonPressed[playerid])
                {
                    if (mainmenu[playerid] == 0)//main
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == Main.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }

                    if (mainmenu[playerid] == 1)//T loadout
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == Tloadout.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    if (mainmenu[playerid] == 2)//T Primary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == TFullBuy.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    if (mainmenu[playerid] == 3)//T Secondary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == TSecondary.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    if (mainmenu[playerid] == 4)//T Pistol Round
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == TPistolRound.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }

                    if (mainmenu[playerid] == 5)//CT loadout
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == CTloadout.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    if (mainmenu[playerid] == 6)//CT Primary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == CTFullBuy.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    if (mainmenu[playerid] == 7)//CT Secondary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == CTSecondary.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    if (mainmenu[playerid] == 8)//CT Pistol Round
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == CTPistolRound.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }

                    if (mainmenu[playerid] == 9)//AWP loadout
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == AWP.Length - 1) ? 0 : currentIndexDict[playerid] + 1;
                    }
                    
                    buttonPressed[playerid] = true;
                    player.ExecuteClientCommand("play sounds/ui/csgo_ui_page_scroll.vsnd_c");
                }
                else if (player.Buttons == PlayerButtons.Forward && !buttonPressed[playerid])
                {
                    if (mainmenu[playerid] == 0)//main
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? Main.Length - 1 : currentIndexDict[playerid] - 1;
                    }

                    if (mainmenu[playerid] == 1)//T loadout
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? Tloadout.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    if (mainmenu[playerid] == 2)//T Primary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? TFullBuy.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    if (mainmenu[playerid] == 3)//T Secondary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? TSecondary.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    if (mainmenu[playerid] == 4)//T Pistol Round
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? TPistolRound.Length - 1 : currentIndexDict[playerid] - 1;
                    }

                    if (mainmenu[playerid] == 5)//CT loadout
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? CTloadout.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    if (mainmenu[playerid] == 6)//CT Primary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? CTFullBuy.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    if (mainmenu[playerid] == 7)//CT Secondary
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? CTSecondary.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    if (mainmenu[playerid] == 8)//CT Pistol Round
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? CTPistolRound.Length - 1 : currentIndexDict[playerid] - 1;
                    }

                    if (mainmenu[playerid] == 9)//AWP loadout
                    {
                        currentIndexDict[playerid] = (currentIndexDict[playerid] == 0) ? AWP.Length - 1 : currentIndexDict[playerid] - 1;
                    }
                    
                    buttonPressed[playerid] = true;
                    player.ExecuteClientCommand("play sounds/ui/csgo_ui_page_scroll.vsnd_c");
                }else if ((player.Buttons == PlayerButtons.Moveleft || player.Buttons == PlayerButtons.Moveright) && !buttonPressed[playerid])
                {
                    int currentLineIndex = currentIndexDict[playerid];

                    if (mainmenu[playerid] == 2)
                    {
                        string currentLineTFullBuy = TFullBuy[currentLineIndex];
                        GunsMenu.HandlePreferenceSelection(player, CsTeam.Terrorist, currentLineTFullBuy);
                        player.PrintToChat($"{MessagePrefix} You selected {currentLineTFullBuy} as T Primary!");
                    }
                    if (mainmenu[playerid] == 3)
                    {
                        string currentLineTSec = TSecondary[currentLineIndex];
                        GunsMenu.HandlePreferenceSelection(player, CsTeam.Terrorist, currentLineTSec, RoundType.FullBuy);
                        player.PrintToChat($"{MessagePrefix} You selected {currentLineTSec} as T Secondary!");
                    }
                    if (mainmenu[playerid] == 4)
                    {
                        string currentLineTPR = TPistolRound[currentLineIndex];
                        GunsMenu.HandlePreferenceSelection(player, CsTeam.Terrorist, currentLineTPR);
                        player.PrintToChat($"{MessagePrefix} You selected {currentLineTPR} as T Pistol Round Weapon!");
                    }

                    if (mainmenu[playerid] == 6)
                    {
                        string currentLineCTFullBuy = CTFullBuy[currentLineIndex];
                        GunsMenu.HandlePreferenceSelection(player, CsTeam.CounterTerrorist, currentLineCTFullBuy);
                        player.PrintToChat($"{MessagePrefix} You selected {currentLineCTFullBuy} as CT Primary!");
                    }
                    if (mainmenu[playerid] == 7)
                    {
                        string currentLineCTSec = CTSecondary[currentLineIndex];
                        GunsMenu.HandlePreferenceSelection(player, CsTeam.CounterTerrorist, currentLineCTSec, RoundType.FullBuy);
                        player.PrintToChat($"{MessagePrefix} You selected {currentLineCTSec} as CT Secondary!");
                    }
                    if (mainmenu[playerid] == 8)
                    {
                        string currentLineCTPR = CTPistolRound[currentLineIndex];
                        GunsMenu.HandlePreferenceSelection(player, CsTeam.CounterTerrorist, currentLineCTPR);
                        player.PrintToChat($"{MessagePrefix} You selected {currentLineCTPR} as CT Pistol Round Weapon!");
                    }

                    if (mainmenu[playerid] == 9)
                    {
                        string currentLineName = AWP[currentLineIndex];
                        if (currentLineName == AWP[0])
                        {
                            GunsMenu.HandlePreferenceSelection(player, CsTeam.Terrorist, CsItem.AWP.ToString(), remove: false);
                            player.PrintToChat($"{MessagePrefix} You selected '{currentLineName}' as when to give the AWP!");
                        }
                        if (currentLineName == AWP[1])
                        {
                            GunsMenu.HandlePreferenceSelection(player, CsTeam.Terrorist, CsItem.AWP.ToString(), remove: true);
                            player.PrintToChat($"{MessagePrefix} You selected '{currentLineName}' as when to give the AWP!");
                        }
                    }

                    if (mainmenu[playerid] == 5)
                    {
                        string currentLineName = CTloadout[currentLineIndex];
                        if (currentLineName == CTloadout[0])
                        {
                            mainmenu[playerid] = 6;
                            currentIndexDict[playerid] = 0;
                        }
                        if (currentLineName == CTloadout[1])
                        {
                            mainmenu[playerid] = 7;
                            currentIndexDict[playerid] = 0;
                        }
                        if (currentLineName == CTloadout[2])
                        {
                            mainmenu[playerid] = 8;
                            currentIndexDict[playerid] = 0;
                        }
                    }
                    if (mainmenu[playerid] == 1)
                    {
                        string currentLineName = Tloadout[currentLineIndex];
                        if (currentLineName == Tloadout[0])
                        {
                            mainmenu[playerid] = 2;
                            currentIndexDict[playerid] = 0;
                        }
                        if (currentLineName == Tloadout[1])
                        {
                            mainmenu[playerid] = 3;
                            currentIndexDict[playerid] = 0;
                        }
                        if (currentLineName == Tloadout[2])
                        {
                            mainmenu[playerid] = 4;
                            currentIndexDict[playerid] = 0;
                        }
                    }
                    if (mainmenu[playerid] == 0)
                    {
                        string currentLineName = Main[currentLineIndex];
                        if (currentLineName == Main[0])
                        {
                            mainmenu[playerid] = 1;
                            currentIndexDict[playerid] = 0;
                        }
                        if (currentLineName == Main[1])
                        {
                            mainmenu[playerid] = 5;
                            currentIndexDict[playerid] = 0;
                        }
                        if (currentLineName == Main[2])
                        {
                            mainmenu[playerid] = 9;
                            currentIndexDict[playerid] = 0;
                        }
                    }
                    buttonPressed[playerid] = true;
                    player.ExecuteClientCommand("play sounds/ui/item_sticker_select.vsnd_c");
                }
                else if ((long)player.Buttons == 8589934592 && !buttonPressed[playerid])
                {
                    if (mainmenu[playerid] == 0)
                    {
                        if (currentIndexDict.ContainsKey(playerid))
                        {
                            currentIndexDict.Remove(playerid);
                        }
                        if(buttonPressed.ContainsKey(playerid))
                        {
                            buttonPressed.Remove(playerid);
                        }
                        if(mainmenu.ContainsKey(playerid))
                        {
                            mainmenu.Remove(playerid);
                        }
                        if(menuon.ContainsKey(playerid))
                        {
                            menuon.Remove(playerid);
                        }
                    }

                    if(mainmenu.ContainsKey(playerid))
                    {
                        if (mainmenu[playerid] == 1)
                        {
                            mainmenu[playerid] = 0;
                            currentIndexDict[playerid] = 0;
                        }
                        if (mainmenu[playerid] == 5)
                        {
                            mainmenu[playerid] = 0;
                            currentIndexDict[playerid] = 0;
                        }

                        if (mainmenu[playerid] == 2)
                        {
                            mainmenu[playerid] = 1;
                            currentIndexDict[playerid] = 0;
                        }
                        if (mainmenu[playerid] == 3)
                        {
                            mainmenu[playerid] = 1;
                            currentIndexDict[playerid] = 0;
                        }
                        if (mainmenu[playerid] == 4)
                        {
                            mainmenu[playerid] = 1;
                            currentIndexDict[playerid] = 0;
                        }

                        if (mainmenu[playerid] == 6)
                        {
                            mainmenu[playerid] = 5;
                            currentIndexDict[playerid] = 0;
                        }
                        if (mainmenu[playerid] == 7)
                        {
                            mainmenu[playerid] = 5;
                            currentIndexDict[playerid] = 0;
                        }
                        if (mainmenu[playerid] == 8)
                        {
                            mainmenu[playerid] = 5;
                            currentIndexDict[playerid] = 0;
                        }

                        if (mainmenu[playerid] == 9)
                        {
                            mainmenu[playerid] = 0;
                            currentIndexDict[playerid] = 0;
                        }
                    }
                    buttonPressed[playerid] = true;
                    player.ExecuteClientCommand("play sounds/ui/item_sticker_select.vsnd_c");
                }

                StringBuilder builder = new StringBuilder();
                if (mainmenu.ContainsKey(playerid))
                {
                    if(mainmenu[playerid] == 0)
                    {
                        for (int i = 0; i < Main.Length; i++)
                        {
                            if (i == currentIndexDict[playerid])
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {Main[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{Main[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenu);
                    }

                    if(mainmenu[playerid] == 1)
                    {
                        for (int i = 0; i < Tloadout.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {Tloadout[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{Tloadout[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenu);
                    }
                    if(mainmenu[playerid] == 5)
                    {
                        for (int i = 0; i < CTloadout.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {CTloadout[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{CTloadout[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenu);
                    }
                    if(mainmenu[playerid] == 9)
                    {
                        for (int i = 0; i < AWP.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {AWP[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{AWP[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenu);
                    }

                    if(mainmenu[playerid] == 2)
                    {
                        for (int i = 0; i < TFullBuy.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {TFullBuy[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{TFullBuy[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenu);
                    }
                    if(mainmenu[playerid] == 3)
                    {
                        for (int i = 0; i < TSecondary.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {TSecondary[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{TSecondary[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenuOnpistol);
                    }
                    if(mainmenu[playerid] == 4)
                    {
                        for (int i = 0; i < TPistolRound.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {TPistolRound[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{TPistolRound[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenuOnpistol);
                    }

                    if(mainmenu[playerid] == 6)
                    {
                        for (int i = 0; i < CTFullBuy.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {CTFullBuy[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{CTFullBuy[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenu);
                    }
                    if(mainmenu[playerid] == 7)
                    {
                        for (int i = 0; i < CTSecondary.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {CTSecondary[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{CTSecondary[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenuOnpistol);
                    }
                    if(mainmenu[playerid] == 8)
                    {
                        for (int i = 0; i < CTPistolRound.Length; i++)
                        {
                            if (i == currentIndexDict[playerid]) 
                            {
                                string lineHtml = $"<font color='orange'>{Imageleft} {CTPistolRound[i]} {ImageRight}</font><br>";
                                builder.AppendLine(lineHtml);
                            }
                            else
                            {
                                builder.AppendLine($"<font color='white'>{CTPistolRound[i]}</font><br>");
                            }
                        }
                        builder.AppendLine(BottomMenuOnpistol);
                    }
                    
                }
                builder.AppendLine("</div>");
                var centerhtml = builder.ToString();
                player?.PrintToCenterHtml(centerhtml);
            }
        }
    }
    public HookResult OnEventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;
        var player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)return HookResult.Continue;
        var playerid = player.SteamID;
        menuon.Remove(playerid);
        mainmenu.Remove(playerid);
        currentIndexDict.Remove(playerid);
        buttonPressed.Remove(playerid);

        return HookResult.Continue;
    }
}
