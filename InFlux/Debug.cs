using System.Runtime.CompilerServices;

namespace InFlux
{
    internal static class Debug
    {
        /// <summary>
        /// output text to the Output window.
        /// </summary>
        public static void WriteLine(string text, [CallerMemberName] string? caller = null)
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}:{caller}:{text}");
        }
    }
}
