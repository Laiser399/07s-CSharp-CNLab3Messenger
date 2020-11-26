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
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public ReceiveFileMsgViewModel(MainWindowViewModel owner, string accessCode) : base(accessCode)
            {
                _owner = owner;
                DispatchStatus = new WaitDispatchStatus(this);
            }

            private void ReceiveFile()
            {
                if (DispatchStatus is WaitDispatchStatus)
                {
                    _owner.ReceiveFileAsync(this, _cts.Token, progress =>
                    {
                        if (DispatchStatus is InProgressDispatchStatus status)
                            status.Progress = progress;
                    });
                }
            }

            protected override void CancelFileDispatch()
            {
                _cts.Cancel();
            }

            public void OnStarted()
            {
                DispatchStatus = new InProgressDispatchStatus(this);
            }

            public void OnCanceled()
            {
                DispatchStatus = new DoneDispatchStatus { Text = "Canceled" };
            }

            public void OnDoneSuccessfully()
            {
                DispatchStatus = new DoneDispatchStatus
                {
                    Text = "Done"
                };
            }

            public void OnDoneWithError()
            {
                DispatchStatus = new DoneDispatchStatus
                {
                    Text = "Error"
                };
            }
        }
    }
    
}
