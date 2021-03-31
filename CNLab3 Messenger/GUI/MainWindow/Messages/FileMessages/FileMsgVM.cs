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
            #region Bindings

            private string _fileName = "";
            public string FileName
            {
                get => _fileName;
                set
                {
                    _fileName = value;
                    NotifyPropChanged(nameof(FileName));
                }
            }

            private int _fileSize = 0;
            public int FileSize
            {
                get => _fileSize;
                set
                {
                    _fileSize = value;
                    NotifyPropChanged(nameof(FileSize), nameof(FileSizeStrRepr));
                }
            }
            public string FileSizeStrRepr
            {
                get
                {
                    if (FileSize < 1024)
                        return $"{FileSize} B";
                    else if (FileSize < 1048576)
                    {
                        double kbSize = FileSize / 1024d;
                        return $"{kbSize.ToString("F1")} KB";
                    }
                    else
                    {
                        double mbSize = FileSize / 1024d / 1024d;
                        return $"{mbSize.ToString("F1")} MB";
                    }
                }
            }

            private object _dispatchStatus;
            public object DispatchStatus
            {
                get => _dispatchStatus;
                protected set
                {
                    _dispatchStatus = value;
                    NotifyPropChanged(nameof(DispatchStatus));
                }
            }

            #endregion

            public string AccessCode { get; private set; }

            protected FileMsgVM(string accessCode)
            {
                AccessCode = accessCode;
            }

            protected abstract void CancelFileDispatch();

        }
    }
    
}
