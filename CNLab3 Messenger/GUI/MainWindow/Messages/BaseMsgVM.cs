using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNLab3_Messenger.GUI
{
    public abstract class BaseMsgVM : BaseViewModel
    {
        private bool _isMyMessage = true;
        public virtual bool IsMyMessage
        {
            get => _isMyMessage;
            set
            {
                _isMyMessage = value;
                NotifyPropChanged(nameof(IsMyMessage));
            }
        }

        private DateTime _creationTime = DateTime.Now;
        public string CreationTimeStrRepr
            => $"{_creationTime.Hour}:{_creationTime.Minute.ToString().PadLeft(2, '0')}";
    }
}
