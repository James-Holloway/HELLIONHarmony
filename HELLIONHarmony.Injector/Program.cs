using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

using JHolloway.SteamLibrary;
using HELLIONHarmony;
using HELLIONHarmony.Loader;
using System.Text;
using Mono.Cecil.Cil;

namespace HELLIONHarmony.Injector
{
    public class Program
    {
        protected const string AssemblySubPath = @"\HELLION_Data\Managed\Assembly-CSharp.dll";
        protected const string SPServerRelPath = @"..\HELLION_SP\HELLION_SP.exe";
        private const uint gameAppId = 588210;

        private static bool autoconfirm = false;
        private static bool copyAssemblies = true;
        private static bool? overwriteBackup = null;
        private static bool findAndPatchSPServer = true;

        private static List<string> RequiredAssemblies = new List<string>();

        public static void Main(string[] args)
        {
            string? assemblyPath = null;
            bool userInput = false;
            bool unpatch = false;

            Console.WriteLine("=== HELLIONHarmony Injector/Patcher ===" + Environment.NewLine);

            #region Argument Handling
            int usedSwitches = 0;
            bool allowAutoDetect = true;
            if (args.Length == 0)
            {
                WriteHelp();
                return;
            }
            else if (args.Length > 0)
            {
                args = args.Concat(new string[1]).ToArray(); // add an extra param so no issues when we handle --assembly
                for (int a = 0; a < args.Length - 1; a++)
                {
                    string arg = args[a];
                    switch (arg.ToLower())
                    {
                        case "-ad":
                        case "/ad":
                        case "--auto":
                        case "--auto-detect":
                            allowAutoDetect = true;
                            usedSwitches++;
                            break;

                        case "-a":
                        case "/a":
                        case "--assembly":
                            allowAutoDetect = false;
                            assemblyPath = args[1 + a++]?.Trim().Trim('"');
                            usedSwitches++;
                            break;

                        case "-y":
                        case "/y":
                        case "--autoconfirm":
                            autoconfirm = true;
                            overwriteBackup = true;
                            usedSwitches++;
                            break;

                        case "-u":
                        case "/u":
                        case "--undo":
                        case "--unpatch":
                            unpatch = true;
                            usedSwitches++;
                            break;

                        case "-i":
                        case "/i":
                        case "--input":
                            userInput = true;
                            allowAutoDetect = false;
                            usedSwitches++;
                            break;

                        case "-nc":
                        case "/nc":
                        case "--nocopy":
                        case "--no-copy":
                            copyAssemblies = false;
                            usedSwitches++;
                            break;
                        case "-ob":
                        case "/ob":
                        case "--overwrite-backup":
                            overwriteBackup = true;
                            usedSwitches++;
                            break;
                        case "-nob":
                        case "/nob":
                        case "--no-overwrite-backup":
                            overwriteBackup = false;
                            usedSwitches++;
                            break;
                        case "-dpsp":
                        case "/dpsp":
                        case "--dont-patch-sp":
                        case "--dontpatchsp":
                            findAndPatchSPServer = false;
                            usedSwitches++;
                            break;

                        case "h":
                        case "?":
                        case "-h":
                        case "-?":
                        case "/?":
                        case "/h":
                        case "help":
                        case "/help":
                        case "--help":
                            WriteHelp();
                            return;
                        default:
                            Console.WriteLine($"Did not recognise the argument/switch: {arg}");
                            ConsoleExit(1);
                            break;
                    }
                }

                if (usedSwitches == 0 && args.Length <= 2) // arg length was one until we added one
                {
                    assemblyPath = args[0]?.Trim().Trim('"');
                }
                if (userInput)
                {
                    Console.Write("Input path to HELLION's Assembly-CSharp.dll: ");
                    assemblyPath = Console.ReadLine()?.Trim().Trim('"');
                    if (!File.Exists(assemblyPath))
                    {
                        Console.Error.WriteLine($"ERROR: Could not find any file at path {assemblyPath}");
                        ConsoleExit(1);
                        return;
                    }
                }
                else if (string.IsNullOrEmpty(assemblyPath))
                {
                    WriteHelp();
                    return;
                }
            }

            #endregion
            // Sometimes switches are used without overwriting the assembly path
            if (allowAutoDetect)
            {
                assemblyPath = AutoDetectAssemblyPath();
            }

            if (string.IsNullOrEmpty(assemblyPath) || string.IsNullOrWhiteSpace(assemblyPath))
            {
                Console.WriteLine("No assembly path passed, auto detecting");
                assemblyPath = AutoDetectAssemblyPath();
            }
            if (!File.Exists(assemblyPath))
            {
                Console.Error.WriteLine("Failed to find assembly path at " + assemblyPath);
                ConsoleExit(1);
                return;
            }

            string backupPath = assemblyPath + ".bak";

            if (!unpatch)
                PatchAssembly(assemblyPath, backupPath);
            else
                UnpatchAssembly(assemblyPath, backupPath);

            if (findAndPatchSPServer && IsClient(assemblyPath))
            {
                string? spPath = Path.GetFullPath(SPServerRelPath, new FileInfo(assemblyPath)?.DirectoryName ?? "");

                if (string.IsNullOrEmpty(spPath) || !File.Exists(spPath))
                {
                    Console.Error.WriteLine("Warning: Failed to find . If you want to use SP server plugins you will need to ");
                }
                else
                {
                    string spBackupPath = spPath + ".bak";
                    if (!unpatch)
                        PatchAssembly(spPath, spBackupPath);
                    else
                        UnpatchAssembly(spPath, spBackupPath);
                }
            }

            ConsoleExit(0);
        }

        #region Assembly Patching
        public static void PatchAssembly(string assemblyPath, string backupPath)
        {
            Console.WriteLine($"Assembly: {new FileInfo(assemblyPath).Name}{Environment.NewLine}\t{assemblyPath}");

            if (new FileInfo(assemblyPath).Length == 0)
            {
                Console.Error.WriteLine("Unable to patch assembly as it is 0 bytes. This normally results from a failed patch");
                if (ConsoleUserInput("Restore backup?"))
                {
                    RestoreBackup(assemblyPath, backupPath);
                }
                else
                    ConsoleExit(1);
                return;
            }

            if (!ConsoleUserInput("Are you sure you want to patch this assembly?", true))
                return;

            ClearRequiredAssemblies();

            AssemblyDefinition assembly = Patch.GetAssemblyDefinition(assemblyPath);

            // Backup existing assembly
            Console.WriteLine(Environment.NewLine + "Backing up assembly to " + backupPath);
            if (File.Exists(backupPath))
            {
                if (!IsPatched(assembly))
                {
                    if (overwriteBackup == null && ConsoleUserInput("Assembly already has a backup, do you want to overwrite it? (not recommended)", false))
                        File.Copy(assemblyPath, backupPath, true);
                    else if (overwriteBackup == true)
                        File.Copy(assemblyPath, backupPath, true);
                }
                else if (overwriteBackup == true)
                {
                    if (ConsoleUserInput(""))
                        File.Copy(assemblyPath, backupPath, true);
                }
                else
                {
                    Console.WriteLine("A backup exists and the assembly is already patched. If you wish to overwrite the backup, use the -OB switch in command line");
                }
            }
            else
                File.Copy(assemblyPath, backupPath, true);


            if (assembly.MainModule.GetType("ZeroGravity.Client") != null)
            {
                PatchClient(assembly, assemblyPath);
            }
            else if (assembly.MainModule.GetType("ZeroGravity.Server") != null)
            {
                PatchServer(assembly, assemblyPath, IsDedicated(assemblyPath));
            }
            else
            {
                throw new FileNotFoundException("File does not contain the class ZeroGravity.Client or ZeroGravity.Server");
            }

            AddRequiredAssembly("0Harmony.dll"); // HarmonyLib
            AddRequiredAssemblyByType(typeof(Plugin)); // HELLIONHarmony
            AddRequiredAssemblyByType(typeof(Patches)); // HELLIONHarmony.Loader

            try
            {
                assembly.Write(assemblyPath);
                Console.WriteLine("Successfully written patched assembly");
            }
            catch
            {
                Console.WriteLine("Failed when writing assembly, restoring from backup");
                if (File.Exists(backupPath))
                    RestoreBackup(assemblyPath, backupPath);
                throw;
            }

            if (copyAssemblies)
                CopyRequiredAssemblies(assemblyPath);

            Console.WriteLine(Environment.NewLine + $"Finished patching assembly {new FileInfo(assemblyPath).Name}! " + Environment.NewLine);
        }

        public static void PatchClient(AssemblyDefinition assembly, string assemblyPath)
        {
            // ZeroGravity.Client::Modded = true;
            FieldDefinition moddedField = Patch.GetField(assembly, "ZeroGravity.Client", "Modded", typeof(bool)) ??
                Patch.NewStaticField(assembly, "ZeroGravity.Client", "Modded", typeof(bool));
            moddedField.Constant = true;
            moddedField.InitialValue = new byte[] { 0x1 };

            // Init Harmony and load plugins
            Patch.Method0(assembly, "ZeroGravity.Client", ".ctor", typeof(Patches), "StartHarmonyClient", false);

            // Make it easier to debug the executable, so that it isn't restarting
            Patch.Method0(assembly, "SteamChecker", "Start", typeof(Patches), "SteamCheckerBypass", true);

            MethodDefinition clientAwakeMethod = Patch.GetMethodDefinition(assembly, "ZeroGravity.Client", "Awake");
            InstructionSequence clientAwakeSequence = new InstructionSequence()
                .AddOperandContains(OpCodes.Call, "::get_Initialized()")
                .AddAnyOperand(OpCodes.Brtrue)
                .AddInstruction(OpCodes.Ldstr, "http://store.steampowered.com/app/588210/")
                .AddOperandContains(OpCodes.Call, ".Application::OpenURL")
                .AddAnyOperand(OpCodes.Ldarg_0)
                .AddOperandContains(OpCodes.Call, "ZeroGravity.Client::ExitGame");

            // Prevent clients from exiting if steam isn't started
            PatchedInstructionSet clientAwakeSet = Patch.MidMethod(assembly, clientAwakeMethod, clientAwakeSequence, null, "No URL, no exit", out PatchedInstructionSet skipSet, true, Patch.PatchMode.NoInsert);

            // TODO replace Unity's [UnityEngine.CoreModule] UnityEngine.Debug::s_Logger and redirect it to debug out 
            // OOORRRRRR use "-logFile -" in cmd line
        }

        public static void PatchServer(AssemblyDefinition assembly, string assemblyPath, bool isDedicated)
        {
            FieldDefinition moddedField = Patch.GetField(assembly, "ZeroGravity.Server", "Modded", typeof(bool)) ??
                Patch.NewStaticField(assembly, "ZeroGravity.Server", "Modded", typeof(bool));
            moddedField.Constant = true;
            moddedField.InitialValue = new byte[] { 0x1 }; // ZeroGravity.Server::Modded = true;

            Patch.Method0(assembly, "ZeroGravity.Server", ".ctor", typeof(Patches), isDedicated ? "StartHarmonyDedicated" : "StartHarmonySP", false);
        }

        #endregion

        #region Unpatching Assemblies
        public static void UnpatchAssembly(string assemblyPath, string backupPath)
        {
            AssemblyDefinition assembly = Patch.GetAssemblyDefinition(assemblyPath);
            if (!IsPatched(assembly))
            {
                if (!ConsoleUserInput("Target assembly isn't patched, are you sure you want to overwrite it with the backup?", false))
                    return;
            }

            if (!File.Exists(backupPath))
            {
                Console.WriteLine("Cannot revert the assembly as the backup doesn't exist. Try redownloading");
                return;
            }
            AssemblyDefinition backupAssembly = Patch.GetAssemblyDefinition(assemblyPath);
            if (IsPatched(backupAssembly))
            {
                if (!ConsoleUserInput("Backup is a patched file, are you sure?", false))
                    return;
            }

            if (!ConsoleUserInput("Are you sure you want to revert?"))
            {
                Console.WriteLine("Cancelling, assembly not reverted");
                return;
            }

            Console.WriteLine("Reverting to backup" + backupPath);
            File.Copy(backupPath, assemblyPath, true);
        }

        public static void RestoreBackup(string destinationPath, string backupPath)
        {
            File.Copy(backupPath, destinationPath, true);
        }
        #endregion

        #region Is Functions
        internal static bool IsPatched(AssemblyDefinition assembly)
        {
            TypeDefinition typeDefinition = assembly.MainModule.GetType("ZeroGravity.Client") ?? assembly.MainModule.GetType("ZeroGravity.Server");
            if (typeDefinition == null)
                return true; // either patched or it isn't actually a valid type

            if (Patch.GetField(typeDefinition, "Modded", typeof(bool)) != null) // does the type have the bool Modded field?
                return true;

            return false;
        }

        internal static bool IsClient(string assemblyPath)
        {
            string? originalFilename = FileVersionInfo.GetVersionInfo(assemblyPath).OriginalFilename;
            if (originalFilename == "Assembly-CSharp.dll")
                return true;
            return false;
        }

        internal static bool IsDedicated(string assemblyPath)
        {
            string? fileDescription = FileVersionInfo.GetVersionInfo(assemblyPath).FileDescription;
            if (fileDescription == "HELLION_Dedicated")
                return true;
            return false;
        }
        #endregion

        #region Adding Required Assemblies
        public static void AddRequiredAssembly(string path)
        {
            if (RequiredAssemblies.Contains(path))
                return;
            RequiredAssemblies.Add(path);
        }
        public static void AddRequiredAssemblyByType(Type type)
        {
            AddRequiredAssembly(type.Assembly.Location);
        }
        public static void ClearRequiredAssemblies()
        {
            RequiredAssemblies.Clear();
        }

        protected static void CopyRequiredAssemblies(string path)
        {
            if (!Directory.Exists(path))
                path = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory name of provided path could not be found", nameof(path));
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);

            Console.WriteLine("Copying required assemblies for destination assembly");
            foreach (string assembly in RequiredAssemblies)
            {
                string assemblyName = Path.GetFileName(assembly);
                Console.WriteLine($"Copying {assemblyName} to {path}");
                string destinationFilePath = Path.Combine(path, assemblyName);
                try
                {
                    File.Copy(assemblyName, destinationFilePath, true);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Failed to copy file {assemblyName}: " + e.Message);
                    Environment.Exit(1);
                }
#if DEBUG
                try
                {
                    string pdbPath = Path.ChangeExtension(assemblyName, "pdb");
                    string destinationPdbPath = Path.ChangeExtension(destinationFilePath, "pdb");
                    if (File.Exists(pdbPath))
                    {
                        File.Copy(pdbPath, Path.Combine(path, destinationPdbPath), true);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Failed to copy file {assemblyName}: " + e.Message);
                    Environment.Exit(1);
                }
#endif
            }
        }
        #endregion

        #region Assembly Detection through JHolloway.SteamLibrary
        public static string AutoDetectAssemblyPath()
        {
            try
            {
                SteamLibrary[] libraries = SteamLibrary.GetSteamLibraries();
                foreach (SteamLibrary library in libraries)
                {
                    SteamGame? HELLION = (from game in library.Games where game.AppId == gameAppId select game).FirstOrDefault();
                    if (HELLION == null || string.IsNullOrEmpty(HELLION.InstallPath))
                        continue;

                    return Path.Combine(HELLION.InstallPath, AssemblySubPath);
                }
            }
            catch (FileNotFoundException) { } // thrown when registry key not found (steam not installed)
            return "";
        }
        #endregion

        #region Console helper functions
        public static void WriteHelp()
        {
            Console.WriteLine("HellionHarmonyInjector Usage:" + Environment.NewLine +
                $".{Path.DirectorySeparatorChar}HellionHarmonyInjector [-A] <Assembly Path> [-Y] [-U]" + @"
-A <Path>  : Path to the HELLION Assembly-CSharp.dll or HELLION_Dedicated.exe to inject into [/A, --assembly]
-Y         : Autoconfirm, don't prompt for changes. Enables -OB [/Y, --autoconfirm]
-U         : Unpatch the assembly, using <Path>.bak [/U, --unpatch, --undo]
-AD        : Automatically detect the client and SP server assembly through Steam Libraries [/AD, --auto, --auto-detect]
-I         : Use user input for assembly path [/I, --input]
-H         : Show this help screen

When patching:
-DPSP      : Don't automatically search for and patch HELLION_SP.exe after patching the client [-dpsp, --dont-patch-sp]
-NC        : Don't copy assemblies automatically [/NC, --nocopy]
-OB        : Overwrite Backup - always overwrite the backup file without prompting [/OB, --overwrite-backup]
-NOB       : No Overwrite Backup - don't overwrite the backup, should be after -Y if used [/NOB, --no-overwrite-backup]
  All switches are case insensitive
");
            if (!autoconfirm)
                ConsoleExit(0);
        }

        public static void ConsolePause(string? message = "Press any key to continue...")
        {
            if (message != null)
                Console.WriteLine(message);
            Console.ReadKey();
        }

        public static void ConsoleExit(int exitCode = 0, string? message = "Press any key to exit...", string? autoconfirmMessage = "Exiting...")
        {
            if (!autoconfirm)
            {
                if (message != null)
                    Console.WriteLine(message);
                Console.ReadKey();
            }
            else
            {
                if (autoconfirmMessage != null)
                    Console.WriteLine(autoconfirmMessage);
            }
            Environment.Exit(exitCode);
        }

        private static readonly string[] yes = { "y", "ye", "yes", "true", "1", "oui" };
        private static readonly string[] no = { "n", "no", "non", "false", "0", "exit", "quit" };
        public static bool ConsoleUserInput(string? message = "", bool defualtOption = true, bool writeOptions = true)
        {
            do
            {
                if (message != null)
                    Console.Write(message + " ");
                if (writeOptions)
                {
                    if (defualtOption)
                        Console.Write("[Y/n] ");
                    else
                        Console.Write("[y/N] ");
                }
                if (autoconfirm)
                {
                    Console.WriteLine("Y");
                    return true;
                }

                string line = Console.ReadLine()?.ToLower().Trim() ?? "";
                if (string.IsNullOrEmpty(line))
                    return defualtOption;

                if (yes.Contains(line))
                    return true;
                else if (no.Contains(line))
                    return false;
                else
                    Console.WriteLine("Unknown input, try Y or N");
            }
            while (true);
        }
        #endregion
    }
}