using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace EmergenceGuardian.MpvPlayerUI
{
    public class MpvTest : Control
    {
        static MpvTest()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvTest), new FrameworkPropertyMetadata(typeof(MpvTest)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
