using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    class AddContactDialogViewModel : BaseViewModel
    {
        private string _ipAddress = "";
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                NotifyPropChanged(nameof(IpAddress));
            }
        }

        private string _port = "";
        public string Port
        {
            get => _port;
            set
            {
                _port = value;
                NotifyPropChanged(nameof(Port));
            }
        }
    }
}
