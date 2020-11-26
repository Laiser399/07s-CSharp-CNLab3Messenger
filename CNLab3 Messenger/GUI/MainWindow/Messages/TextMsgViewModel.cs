using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    public class TextMsgViewModel : BaseMsgViewModel
    {
        private string _text = "";
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                NotifyPropChanged(nameof(Text));
            }
        }
    }
}
