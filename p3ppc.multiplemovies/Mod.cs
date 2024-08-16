using p3ppc.multiplemovies.Configuration;
using p3ppc.multiplemovies.Template;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;
using p3ppc.expShare.NuGet.templates.defaultPlus;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;


namespace p3ppc.multiplemovies
{
    public unsafe class Mod : ModBase
    {
        private readonly IModLoader _modLoader;
        private readonly IReloadedHooks? _hooks;
        private readonly ILogger _logger;
        private readonly IMod _owner;
        private Config _configuration;
        private readonly IModConfig _modConfig;

        private int* _introAPlayed;
        private IAsmHook _openingHook;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            Utils.Initialise(_logger, _configuration, _modLoader);

            var memory = Memory.Instance;
            _introAPlayed = (int*)memory.Allocate(sizeof(int)).Address;


            var startupScannerController = _modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out var startupScanner))
            {
                Utils.LogError($"Unable to get controller for Reloaded SigScan Library, stuff won't work :(");
                return;
            }

            startupScanner.AddMainModuleScan("83 3D ?? ?? ?? ?? 00 75 ?? 48 8D 15 ?? ?? ?? ?? EB ??", result =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find PlayOpening, stuff won't work :(");
                    return;
                }

                var function = new[]
                {
                    "use64",
                    $"cmp dword [qword {(nuint)_introAPlayed}], 0",
                    "jne _check_one",
                    "mov rdx, qword ptr [P3OPMV_P3P.usm]",
                    "jmp _end_hook",

                    "label _check_one",
                    $"cmp dword [qword {(nuint)_introAPlayed}], 1",
                    "jne _check_two",
                    "mov rdx, qword ptr [P3OPMV_P3PB.usm]",
                    "jmp _end_hook",

                    "label _check_two",
                    $"cmp dword [qword {(nuint)_introAPlayed}], 2",
                    "jne _end_hook",
                    "mov rdx, qword ptr [P3OPMV_P3PC.usm]",

                    "label _end_hook",
                    "add dword [qword {(nuint)_introAPlayed}], 1",
                    "mov eax, [qword {(nuint)_introAPlayed}]",
                    "xor edx, edx",
                    "mov ecx, 3",
                    "div ecx",
                    "mov dword [qword {(nuint)_introAPlayed}], edx"
                };


                _openingHook = _hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress, AsmHookBehaviour.ExecuteFirst).Activate();
            });
        }


        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
        public Mod() { }
        #endregion
    }
}
