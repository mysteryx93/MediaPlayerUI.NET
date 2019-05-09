using System;
using System.Windows.Markup;

namespace EmergenceGuardian.MediaPlayerUI {
    /// <summary>
    /// Allows binding int value for command parameters using syntax {ui:Int32 5}
    /// </summary>
    public sealed class Int32Extension : MarkupExtension {
        public Int32Extension(int value) { this.Value = value; }
        public int Value { get; set; }
        public override Object ProvideValue(IServiceProvider sp) { return Value; }
    };
}
