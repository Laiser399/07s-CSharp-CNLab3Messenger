using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        public abstract partial class FileMsgViewModel : BaseMsgViewModel
        {
            public class InProgressDispatchStatus : BaseViewModel
            {

                #region Bindings

                private double _progress = 0;
                public double Progress
                {
                    get => _progress;
                    set
                    {
                        _progress = value;
                        NotifyPropChanged(nameof(Progress), nameof(ProgressStrRepr));
                    }
                }
                public string ProgressStrRepr => (Progress * 100).ToString("F") + "%";

                private RelayCommand _cancelDispatchCmd;
                public RelayCommand CancelDispatchCmd
                    => _cancelDispatchCmd ?? (_cancelDispatchCmd = new RelayCommand(_ => _owner.CancelFileDispatch()));

                #endregion

                private FileMsgViewModel _owner;

                public InProgressDispatchStatus(FileMsgViewModel owner)
                {
                    _owner = owner;
                }
            }

            public class DoneDispatchStatus : BaseViewModel
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

        public partial class SendFileMsgViewModel : FileMsgViewModel
        {
            public class WaitDispatchStatus
            {
                public string Text => "Waiting...";

                private RelayCommand _cancelSendingCmd;
                public RelayCommand CancelSendingCmd
                    => _cancelSendingCmd ?? (_cancelSendingCmd = new RelayCommand(_ => _owner.BlockFileAccess()));

                private SendFileMsgViewModel _owner;

                public WaitDispatchStatus(SendFileMsgViewModel owner)
                {
                    _owner = owner;
                }
            }

        }

        public partial class ReceiveFileMsgViewModel : FileMsgViewModel
        {
            public class WaitDispatchStatus
            {
                private RelayCommand _downloadFileCmd;
                public RelayCommand DownloadFileCmd
                    => _downloadFileCmd ?? (_downloadFileCmd = new RelayCommand(_ => _owner.ReceiveFile()));

                private ReceiveFileMsgViewModel _owner;

                public WaitDispatchStatus(ReceiveFileMsgViewModel owner)
                {
                    _owner = owner;
                }
            }
        }
    }

    

    


}
