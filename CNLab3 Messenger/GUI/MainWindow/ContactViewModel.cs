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
    public class ContactViewModel : BaseViewModel
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

        private ObservableCollection<BaseMsgViewModel> _messages;
        public ObservableCollection<BaseMsgViewModel> Messages
            => _messages ?? (_messages = new ObservableCollection<BaseMsgViewModel>());

        #endregion

        public ContactViewModel(IPAddress ipAddress, int port)
        {
            _ipEndPoint = new IPEndPoint(ipAddress, port);
        }

        public ContactViewModel(IPEndPoint point)
        {
            _ipEndPoint = point;
        }


    }
}
