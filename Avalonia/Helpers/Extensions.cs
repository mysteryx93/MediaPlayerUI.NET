using Avalonia;

namespace HanumanInstitute.MediaPlayer.Avalonia.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Returns the parent of specified type. 
        /// </summary>
        /// <param name="obj">The object to find the parent for.</param>
        /// <typeparam name="T">The type of parent to find.</typeparam>
        /// <returns>The parent of specified type, or null.</returns>
        public static T? FindAncestor<T>(this IStyledElement obj) 
            where T : IStyledElement
        {
            obj.CheckNotNull(nameof(obj));

            var i = obj;
            do
            {
                i = i.Parent;
            } while (i is not T && i != null);

            return (T?)i;
        }
    }
}
