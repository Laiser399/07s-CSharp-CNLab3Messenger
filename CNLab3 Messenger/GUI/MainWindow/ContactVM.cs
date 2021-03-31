using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace CNLab3_Messenger.GUI
{
    public class ContactVM : BaseViewModel
    {
        #region Bindings

        private bool _connected = false;
        public bool Connected
        {
            get => _connected;
            set
            {
                _connected = value;
                NotifyPropChanged(nameof(Connected));
            }
        }

        private IPEndPoint _ipEndPoint;
        public IPEndPoint IPEndPoint => _ipEndPoint;
        public string AddressStrRepr => IPEndPoint.ToString();

        private ObservableCollection<BaseMsgVM> _messages;
        public ObservableCollection<BaseMsgVM> Messages
            => _messages ?? (_messages = new ObservableCollection<BaseMsgVM>());

        #endregion

        public ContactVM(IPAddress ipAddress, int port)
        {
            _ipEndPoint = new IPEndPoint(ipAddress, port);
        }

        public ContactVM(IPEndPoint point)
        {
            _ipEndPoint = point;
        }


    }
}
