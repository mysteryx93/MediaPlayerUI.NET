using System;

namespace HanumanInstitute.MediaPlayerUI
{
    /// <summary>
    /// Holds a typed value as an event argument.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    public class ValueEventArgs<T> : EventArgs
    {
        public T Value { get; set; }

        //public ValueEventArgs()
        //{ }

        public ValueEventArgs(T value)
        {
            Value = value;
        }
    }
}
