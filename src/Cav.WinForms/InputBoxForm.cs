using Cav.WinForms.BaseClases;

namespace Cav.WinForms
{
    internal partial class InputBoxForm : DialogFormBase
    {
        public InputBoxForm() => InitializeComponent();

        public void CorrrectHeightForm()
        {
            var xtop = lbDescriptionText.Height + lbDescriptionText.Top;
            var xbottob = tbInputText.Top;
            Height = Height - (xbottob - xtop) + 10;
        }
    }
}
