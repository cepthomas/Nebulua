using System;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    /// <summary>Application level error. Above lua level.</summary>
    public class AppException(string message) : Exception(message) { }

    /// <summary>Channel playing.</summary>
    public enum PlayState { Normal, Solo, Mute }
}
