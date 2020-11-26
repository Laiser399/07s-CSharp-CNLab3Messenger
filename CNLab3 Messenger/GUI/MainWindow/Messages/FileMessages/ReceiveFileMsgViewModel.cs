using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        public partial class ReceiveFileMsgViewModel : FileMsgViewModel
        {
            #region Bindings

            public override bool IsMyMessage
            {
                get => false;
                set { }
            }


            #endregion

            private MainWindowViewModel _owner;
            public string AccessCode { get; private set; }
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public ReceiveFileMsgViewModel(MainWindowViewModel owner, string accessCode)
            {
                _owner = owner;
                AccessCode = accessCode;
                DispatchStatus = new WaitDispatchStatus(this);
            }

            private void ReceiveFile()
            {
                if (DispatchStatus is WaitDispatchStatus)
                {
                    DispatchStatus = new InProgressDispatchStatus(this);
                    _owner.ReceiveFileAsync(this, _cts.Token);
                }
            }

            protected override void CancelFileDispatch()
            {
                _cts.Cancel();
            }
        }
    }
    
}
