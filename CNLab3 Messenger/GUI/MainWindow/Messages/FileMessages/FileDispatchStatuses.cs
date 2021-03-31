using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    public partial class MainWindowVM : BaseViewModel
    {
        public abstract partial class FileMsgVM : BaseMsgVM
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

                private FileMsgVM _owner;

                public InProgressDispatchStatus(FileMsgVM owner)
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

        public partial class SendFileMsgVM : FileMsgVM
        {
            public class WaitDispatchStatus
            {
                public string Text => "Waiting...";

                private RelayCommand _cancelSendingCmd;
                public RelayCommand CancelSendingCmd
                    => _cancelSendingCmd ?? (_cancelSendingCmd = new RelayCommand(_ => _owner.BlockFileAccess()));

                private SendFileMsgVM _owner;

                public WaitDispatchStatus(SendFileMsgVM owner)
                {
                    _owner = owner;
                }
            }

        }

        public partial class ReceiveFileMsgVM : FileMsgVM
        {
            public class WaitDispatchStatus
            {
                private RelayCommand _downloadFileCmd;
                public RelayCommand DownloadFileCmd
                    => _downloadFileCmd ?? (_downloadFileCmd = new RelayCommand(_ => _owner.ReceiveFile()));

                private ReceiveFileMsgVM _owner;

                public WaitDispatchStatus(ReceiveFileMsgVM owner)
                {
                    _owner = owner;
                }
            }
        }
    }

    

    


}
