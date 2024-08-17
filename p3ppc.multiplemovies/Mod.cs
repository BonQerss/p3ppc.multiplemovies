using System.Runtime.InteropServices;
using p3ppc.multiplemovies.Configuration;
using p3ppc.multiplemovies.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.SigScan;
using Reloaded.Hooks;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Diagnostics;
using p3ppc.totalkotoneoverhaul;
using System.Text;
using System.Runtime.CompilerServices;

namespace p3ppc.multiplemovies
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public unsafe class Mod : ModBase // <= Do not Remove.
    {
        private delegate nuint IntroDelegate(IntroStruct* introStruct);

        private delegate nuint MoviePlayDelegate(TaskStruct* TaskStruct, string param2, nuint param3, nuint* param_4, nuint param_5,
         nuint param_6);

        private int _IntroCount;
        private string currentMovie;

        [StructLayout(LayoutKind.Explicit)]
        private struct IntroStruct
        {
            [FieldOffset(0x48)]
            internal IntroStateStruct* StateInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntroStateStruct
        {
            [FieldOffset(0)]
            internal IntroState state;

            [FieldOffset(0x10)]
            internal TaskStruct* Task;
        }

        private struct TaskStruct
        {
        }

        private enum IntroState : int
        {
            MovieStart = 4,
            MoviePlaying = 5,
            TitleScreen = 7
        }

        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly Reloaded.Hooks.Definitions.IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;


        private IHook<IntroDelegate> _introHook;
        private MoviePlayDelegate _moviePlay;
        private nuint _movieThing1;
        private nuint* _movieThing2;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            Utils.Initialise(_logger, _configuration, _modLoader);

            Debugger.Launch();

            Utils.SigScan("48 89 5C 24 ?? 57 48 83 EC 30 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5F ?? 48 63 03", "Intro", address =>
            {
                _introHook = _hooks.CreateHook<IntroDelegate>(Intro, address).Activate();
            });

            Utils.SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 40 49 89 CE 4C 89 CE", "_moviePlay", address =>
            {
                _moviePlay = _hooks.CreateWrapper<MoviePlayDelegate>(address, out _);
            });


            Utils.SigScan("48 8B 05 ?? ?? ?? ?? 4C 8D 05 ?? ?? ?? ?? 48 89 44 24 ??", "SuperMegaUltra Important", result =>
            {
                _movieThing1 = Utils.GetGlobalAddress(Utils.BaseAddress + 10);

                _movieThing2 = (nuint*)Utils.GetGlobalAddress(Utils.BaseAddress + 3);
            });

           

            // For more information about this template, please see
            // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

        // If you want to implement e.g. unload support in your mod,
        // and some other neat features, override the methods in ModBase.

        // TODO: Implement some mod logic
        }


        private nuint Intro(IntroStruct* introStruct)
        {
            var stateInfo = introStruct->StateInfo;
            

            if (stateInfo->state == IntroState.MovieStart)
            {
                if (_IntroCount == 0)
                {
                    currentMovie = "data/sound/usm/P3OPMOV_P3P.usm";
                }
                else if (_IntroCount == 1)
                {
                    currentMovie = "data/sound/usm/P3OPMV_P3PB.usm";
                }
                else
                {
                    currentMovie = "data/sound/usm/P3OPMV_P3PC.usm";
                }

               var taskHandle = _moviePlay(stateInfo->Task, currentMovie, _movieThing1, (nuint*)0, 0, (nuint)_movieThing2);

                stateInfo->Task = (TaskStruct*)taskHandle;
                _IntroCount = (_IntroCount + 1) % 3;
                stateInfo->state = IntroState.MovieStart;
                return 0;
            }

            return _introHook.OriginalFunction(introStruct);
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}