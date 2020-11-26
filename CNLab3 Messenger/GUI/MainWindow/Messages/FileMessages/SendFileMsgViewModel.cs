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

            public SendFileMsgViewModel(MainWindowViewModel owner)
            {
                DispatchStatus = new WaitDispatchStatus(this);
            }

            private void BlockFileDispatch()
            {
                throw new NotImplementedException();
            }

            protected override void CancelFileDispatch()
            {
                throw new NotImplementedException();// TODO
            }
        }
    }
    
}
