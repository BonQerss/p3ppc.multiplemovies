using System.ComponentModel;
using p3ppc.multiplemovies.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace p3ppc.multiplemovies.Configuration
{
    public class Config : Configurable<Config>
    {
        /*
            User Properties:
                - Please put all of your configurable properties here.
    
            By default, configuration saves as "Config.json" in mod user config folder.    
            Need more config files/classes? See Configuration.cs
    
            Available Attributes:
            - Category
            - DisplayName
            - Description
            - DefaultValue

            // Technically Supported but not Useful
            - Browsable
            - Localizable

            The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
        */


        [DisplayName("Randomize")]
        [Description("Randomizes intro FMVs.")]
        [DefaultValue(false)]
        public bool randomize { get; set; } = false;

        [DisplayName("Include Default Movies")]
        [Description("Decides whether or not default movies are played.")]
        [DefaultValue(true)]
        public bool IncludeDefaultMovies { get; set; } = false;

        [DisplayName("New Movies First")]
        [Description("Plays new movies first, then original movies once all have played. Requires Include Default Movies")]
        [DefaultValue(false)]
        public bool NewFirst { get; set; } = false;

    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
