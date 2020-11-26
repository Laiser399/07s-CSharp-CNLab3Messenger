using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.Protocol
{
    class MsgEventArgs : EventArgs
    {
        public IPEndPoint SenderIPPoint { get; set; }
    }

    class ConnectedEventArgs : MsgEventArgs { }

    class TextMsgEventArgs : MsgEventArgs
    {
        public string Text { get; set; }
    }

    class ImageMsgEventArgs : MsgEventArgs
    {
        public int FileSize { get; set; }
        public string FileName { get; set; }
        public byte[] ImageData { get; set; }
    }

    class FileMsgEventArgs : MsgEventArgs
    {
        public int FileSize { get; set; }
        public string FileName { get; set; }
        public string AccessCode { get; set; }
    }

}
