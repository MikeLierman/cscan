﻿using System.Collections.Generic;
using Microsoft.Win32;

namespace CScan.Components
{
    internal class Programs : IComponent
    {
        public void Run(ref Report report, List<Dictionary<string, string>> list)
        {
            var registryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

            using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            var displayName = (string) subKey.GetValue("DisplayName");
                            var hidden = subKey.GetValue("SystemComponent") != null;

                            if (displayName != null)
                            {
                                list.Add(new Dictionary<string, string>
                                {
                                    {"token", "Prg"},
                                    {"display_name", displayName},
                                    {"is_hidden", hidden ? "[b](Hidden)[/b]" : null}
                                });
                            }
                        }
                    }
            }

            list.Sort((entry1, entry2) => entry1["display_name"].CompareTo(entry2["display_name"]));

            report.Add(list);
            if (System.Environment.Is64BitOperatingSystem && System.Environment.Is64BitProcess)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey != null)
                                {
                                    var displayName = (string)subKey.GetValue("DisplayName");
                                    var hidden = subKey.GetValue("SystemComponent") != null;
                                    if (displayName != null)
                                    {
                                        list.Add(new Dictionary<string, string>
                                        {
                                            { "token", "Prg"},
                                            { "display_name", displayName},
                                            { "is_hidden", hidden ? "[b](Hidden)[/b]" : null}
                                         });
                                    }
                                }
                            }
                        }
                }
                list.Sort((entry1, entry2) => entry1["display_name"].CompareTo(entry2["display_name"]));
                report.Add(list);
            }
        }
    }
}