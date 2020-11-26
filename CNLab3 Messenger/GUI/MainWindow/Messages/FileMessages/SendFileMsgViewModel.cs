using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        public partial class SendFileMsgViewModel : FileMsgViewModel
        {
            #region Bindings

            public override bool IsMyMessage
            {
                get => true;
                set { }
            }

            #endregion

            private MainWindowViewModel _owner;

            public SendFileMsgViewModel(MainWindowViewModel owner, string accessCode) : base(accessCode)
            {
                _owner = owner;
                DispatchStatus = new WaitDispatchStatus(this);
            }

            private void BlockFileAccess()
            {
                _owner.BlockFileAccess(AccessCode);
                DispatchStatus = new DoneDispatchStatus
                {
                    Text = "Canceled"
                };
            }

            protected override void CancelFileDispatch()
            {
                _owner.CancelFileSending(AccessCode);
            }

            public void OnStart()
            {
                DispatchStatus = new InProgressDispatchStatus(this);
            }

            public void OnProgressChanged(double progress)
            {
                if (DispatchStatus is InProgressDispatchStatus status)
                    status.Progress = progress;
            }

            public void OnCanceled()
            {
                DispatchStatus = new DoneDispatchStatus
                {
                    Text = "Canceled"
                };
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
