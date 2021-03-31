using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CNLab3_Messenger.GUI
{
    class ImageMsgVM : BaseMsgVM
    {
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

        private byte[] _imageData;
        public byte[] ImageData
        {
            get => _imageData;
            set
            {
                _imageData = value;

                if (_imageData is null)
                {
                    _image = null;
                }
                else
                {
                    try
                    {
                        BitmapImage image = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(_imageData))
                        {
                            image.BeginInit();
                            image.UriSource = null;
                            image.StreamSource = stream;
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                            image.EndInit();
                        }
                        image.Freeze();
                        _image = image;
                    }
                    catch
                    {
                        _image = null;
                    }
                }
                
                NotifyPropChanged(nameof(ImageData), nameof(Image));
            }
        }

        private BitmapImage _image;
        public BitmapImage Image => _image;

    }
}
