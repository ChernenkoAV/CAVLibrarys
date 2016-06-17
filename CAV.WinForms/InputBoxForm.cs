using CAV.WinForms.BaseClases;

namespace CAV.WinForms
{
    internal partial class InputBoxForm : DialogFormBase
    {
        public InputBoxForm()
        {
            InitializeComponent();
        }

        public void CorrrectHeightForm()
        {
            var Xtop = lbDescriptionText.Height + lbDescriptionText.Top;
            var xbottob = tbInputText.Top;
            this.Height = this.Height - (xbottob - Xtop) + 10;
        }
    }
}
