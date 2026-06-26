namespace VK.Audio.Core
{
    /// <summary>How a voice is positioned in the mix.</summary>
    public enum SpatialMode
    {
        /// <summary>2D, non-positional (spatialBlend = 0). Music, UI, ambience beds, narrator VO.</summary>
        TwoD = 0,
        /// <summary>3D, positional with distance attenuation (spatialBlend = 1).</summary>
        ThreeD = 1
    }
}
