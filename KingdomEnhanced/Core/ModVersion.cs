namespace KingdomEnhanced.Core
{
    /// <summary>Holds the version constants used by the mod for identification and logging.</summary>
    public static class ModVersion
    {
        /// <summary>Major version number.</summary>
        public const string MAJOR = "2";
        /// <summary>Minor version number.</summary>
        public const string MINOR = "2";
        /// <summary>Patch version number.</summary>
        public const string PATCH = "0";
        /// <summary>Optional version suffix (e.g. prerelease tag); empty for stable builds.</summary>
        public const string SUFFIX = "";
        /// <summary>Full numeric version string (MAJOR.MINOR.PATCH).</summary>
        public const string FULL = MAJOR + "." + MINOR + "." + PATCH;
        /// <summary>Display version string, prefixed with "v" and appended with the suffix.</summary>
        public const string DISPLAY = "v" + FULL + SUFFIX;
    }
}
