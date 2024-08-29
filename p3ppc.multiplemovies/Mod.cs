using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using p3ppc.multiplemovies.Configuration;
using p3ppc.multiplemovies.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using CriFs.V2.Hook.Interfaces;
using p3ppc.totalkotoneoverhaul;

namespace p3ppc.multiplemovies
{
    public unsafe class Mod : ModBase // <= Do not Remove.
    {
        private delegate nuint IntroDelegate(IntroStruct* introStruct);

        private delegate TaskStruct* PlayMoviePlayDelegate(IntroStruct* introStruct, string moviePath, nuint movieThing1, int param4, int param5, nuint movieThing2);

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

        private readonly IModLoader _modLoader;
        private readonly Reloaded.Hooks.Definitions.IReloadedHooks? _hooks;
        private readonly ILogger _logger;
        private readonly IMod _owner;
        private Config _configuration;
        private readonly IModConfig _modConfig;

        private IHook<IntroDelegate> _introHook;
        private PlayMoviePlayDelegate _moviePlay;
        private nuint _movieThing1;
        private nuint* _movieThing2;
        private List<string> _movieFiles = new List<string>();
        private string movieDir;
        private int _introCount = -1;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            Utils.Initialise(_logger, _configuration, _modLoader);


            Utils.SigScan("48 89 5C 24 ?? 57 48 83 EC 30 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5F ?? 48 63 03", "Intro", address =>
            {
                _introHook = _hooks.CreateHook<IntroDelegate>(Intro, address).Activate();
            });

            Utils.SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 40 49 89 CE 4C 89 CE", "_moviePlay", address =>
            {
                _moviePlay = _hooks.CreateWrapper<PlayMoviePlayDelegate>(address, out _);
            });

            Utils.SigScan("48 8B 05 ?? ?? ?? ?? 4C 8D 05 ?? ?? ?? ?? 48 89 44 24 ??", "PlayMovieArgs", address =>
            {
                _movieThing1 = Utils.GetGlobalAddress(address + 10);

                _movieThing2 = (nuint*)Utils.GetGlobalAddress(address + 3);
            });

            _modLoader.ModLoading += ModLoading;

            if (_configuration.IncludeDefaultMovies)
            {
                _movieFiles.Add("P3OPMV_P3P.usm");
                _movieFiles.Add("P3OPMV_P3PB.usm");
            }

            if (_configuration.IncludeDefaultMovies)
            {
                if (_configuration.NewFirst)
                {
                    _introCount += 2;
                }
            }
        }
      

        private void ModLoading(IModV1 mod, IModConfigV1 modConfig)
        {
            var criFsController = _modLoader.GetController<ICriFsRedirectorApi>();
            if (criFsController == null || !criFsController.TryGetTarget(out var criFsApi))
            {
                _logger.WriteLine("Something in CriFS failed! Normal files will not load properly!", System.Drawing.Color.Red);
                return;
            }

            

            if (modConfig.ModDependencies.Contains(_modConfig.ModId))
            {
                string modDir = _modLoader.GetDirectoryForModId(modConfig.ModId);

                // Correctly set the movieDir path
                movieDir = Path.Combine(modDir, "movieDir", "USM", "umd1.cpk", "data");

                // Check if the directory exists before trying to enumerate files
                if (Directory.Exists(movieDir))
                {
                    // Get only the filenames, not the full paths
                    _movieFiles.AddRange(Directory.GetFiles(movieDir, "*.usm").Select(Path.GetFileName)); 
                    criFsApi.AddProbingPath("movieDir/USM");// Add full movieDir as probing path
                }
                else
                {
                    _logger.WriteLine($"Directory does not exist: {movieDir}");
                }

                
            }


        }

        private nuint Intro(IntroStruct* introStruct)
        {
            var stateInfo = introStruct->StateInfo;

            if (stateInfo->state == IntroState.MovieStart)
            {
                if (_movieFiles.Count == 0)
                {
                    _logger.WriteLine("No movie files found, using default movie.");
                    currentMovie = Path.Combine("data", "sound", "usm", "P3OPMOV_P3P.usm");
                }
                else
                {
                    if (_configuration.randomize)
                    {
                        _introCount = new Random().Next(0, _movieFiles.Count);
                    }
                    else
                    {

                        _introCount = (_introCount + 1) % _movieFiles.Count;
                    }

                    foreach (string MovieFile in _movieFiles)
                    {
                        _logger.WriteLine($"[MultipleMovies] {MovieFile}");
                    }


                    currentMovie = _movieFiles[_introCount];
                }

                if (currentMovie == "P3OPMV_P3P.usm" || currentMovie == "P3OPMV_P3PB.usm")
                {
                    currentMovie = Path.Combine( "sound", "usm", currentMovie);
                }

                // Combine with movie directory path if needed for logging
                string fullMoviePath = Path.Combine(movieDir, currentMovie);

                _logger.WriteLine($"Playing movie: {currentMovie} (Full path: {fullMoviePath})");

                var taskHandle = _moviePlay(introStruct, currentMovie, _movieThing1, 0, 0, *_movieThing2);

                stateInfo->Task = (TaskStruct*)taskHandle;
                stateInfo->state = IntroState.MoviePlaying;
                return 0;
            }

            return _introHook.OriginalFunction(introStruct);
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
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
