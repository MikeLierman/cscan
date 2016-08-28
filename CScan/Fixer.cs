﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using CScan.Commands;
using Microsoft.VisualBasic.Devices;

namespace CScan
{
    internal class Fixer
    {
        public static Dictionary<string, string> commands = new Dictionary<string, string>
        {
            // Command => Class
            {"collect", "Collect"},
            {"reset-lsp", "ResetLSP"},
            {"run", "RunCommand"},
        };

        private readonly List<string> lineBuffer = new List<string>();

        private readonly List<List<Dictionary<string, string>>> results = new List<List<Dictionary<string, string>>>();

        private string currentSection;

        public string fixLogPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                   @"\Desktop\CScan Fix.txt";

        private RichTextBox status;

        public string Fix(ref RichTextBox richTextBox, string fileName = null)
        {
            status = richTextBox;

            if (fileName == null)
            {
                fileName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Desktop\fix.txt";
            }

            if (!File.Exists(fileName))
            {
                richTextBox.Text = "Fix file does not exist!";

                throw new InvalidDataException("Fix file does not exist.");
            }

            var contents = File.ReadAllLines(fileName);

            AddHeader();

            foreach (var line in contents)
            {
                var cleanLine = line.Trim();

                if (cleanLine == "" || cleanLine.Substring(0, 1) == "#")
                    continue;

                ProcessLine(cleanLine);
            }

            if (currentSection != null && lineBuffer.Count > 0)
                RunSection();

            File.WriteAllText(fixLogPath, ToString().Trim());

            return fixLogPath;
        }

        public override string ToString()
        {
            var s = "";

            foreach (var section in results)
            {
                foreach (var line in section)
                {
                    s = s + Environment.NewLine;

                    foreach (var component in line)
                    {
                        if (component.Key == "token")
                        {
                            s = s + component.Value + ": ";
                            continue;
                        }

                        s = s + component.Value + " ";
                    }
                }

                s = s + Environment.NewLine;
            }

            return s;
        }

        private void ProcessLine(string line)
        {
            if (line.Substring(0, 2) == "::")
            {
                HandleSection(line.Substring(2).ToLower());
                RunSection();
            }
            else if (line.Substring(0, 1) == ":")
            {
                HandleSection(line.Substring(1).ToLower());
            }
            else
            {
                HandleEntry(line);
            }
        }

        private void HandleSection(string command)
        {
            if (currentSection != null && lineBuffer.Count > 0)
                RunSection();

            currentSection = command;
        }

        private void RunSection()
        {
            status.Text = status.Text + "Processing " + currentSection.Substring(0, 1).ToUpper() +
                          currentSection.Substring(1) + "." + Environment.NewLine;

            var command = ResolveCommand(commands[currentSection]);
            results.Add(command.Run(lineBuffer, new List<Dictionary<string, string>>()));

            currentSection = null;
            lineBuffer.Clear();
        }

        private void HandleEntry(string line)
        {
            if (currentSection == null)
                throw new InvalidDataException("An entry was passed outside of a section.");

            lineBuffer.Add(line);
        }

        private ICommand ResolveCommand(string command)
        {
            var t = Type.GetType("CScan.Commands." + command);

            return (ICommand) Activator.CreateInstance(t);
        }

        private void AddHeader()
        {
            var list = new List<Dictionary<string, string>>();

            list.Add(new Dictionary<string, string>
            {
                {"raw", Main.name + " Version " + Main.version + " (Fix Log)"}
            });

            var ci = CultureInfo.InstalledUICulture;

            list.Add(new Dictionary<string, string>
            {
                {
                    "raw",
                    "Running from " + Assembly.GetExecutingAssembly().Location.Substring(0, 3) + " as " +
                    WindowsIdentity.GetCurrent().Name + " on " + DateTime.Now
                }
            });

            list.Add(new Dictionary<string, string>
            {
                {"raw", "Windows Version " + Environment.OSVersion.Version + " Language " + ci.EnglishName}
            });

            double totalMemory = new ComputerInfo().TotalPhysicalMemory;
            totalMemory = totalMemory/1000000000;

            double freeMemory = new ComputerInfo().AvailablePhysicalMemory;
            freeMemory = freeMemory/1000000000;

            list.Add(new Dictionary<string, string>
            {
                {
                    "raw",
                    totalMemory.ToString("N1") + "GB RAM installed; " + freeMemory.ToString("N1") + "GB RAM available"
                }
            });

            results.Add(list);
        }
    }
}