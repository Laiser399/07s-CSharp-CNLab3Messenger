using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using CNLab3_Messenger.Protocol;
using System.Threading;

namespace CNLab3_Messenger.GUI
{
    // TODO remove ports from brandmauer, router
    public partial class MainWindowViewModel : BaseViewModel
    {
        #region Bindings

        public IPAddress IPAddress => _ipAddress;

        private int _port = 0;
        public int Port => _port;

        private RelayCommand _addContactCmd;
        public RelayCommand AddContactCmd
            => _addContactCmd ?? (_addContactCmd = new RelayCommand(_ => AddContact()));

        private ObservableCollection<ContactViewModel> _contacts;
        public ObservableCollection<ContactViewModel> Contacts
            => _contacts ?? (_contacts = new ObservableCollection<ContactViewModel>());

        private ContactViewModel _selectedContact;
        public ContactViewModel SelectedContact
        {
            get => _selectedContact;
            set
            {
                _selectedContact = value;
                NotifyPropChanged(nameof(SelectedContact));
            }
        }

        private string _inputedMessage = "";
        public string InputedMessage
        {
            get => _inputedMessage;
            set
            {
                _inputedMessage = value;
                NotifyPropChanged(nameof(InputedMessage));
            }
        }

        private RelayCommand _connectCmd;
        public RelayCommand ConnectCmd
            => _connectCmd ?? (_connectCmd = new RelayCommand(_ => ConnectAsync()));

        private RelayCommand _sendMessageCmd;
        public RelayCommand SendMessageCmd
            => _sendMessageCmd ?? (_sendMessageCmd = new RelayCommand(_ => SendMessageAsync()));

        private RelayCommand _sendImageCmd;
        public RelayCommand SendImageCmd
            => _sendImageCmd ?? (_sendImageCmd = new RelayCommand(_ => SendImageAsync()));

        private RelayCommand _sendFileCmd;
        public RelayCommand SendFileCmd
            => _sendFileCmd ?? (_sendFileCmd = new RelayCommand(_ => SendFileAsync()));

        #endregion

        private GovnoServer _server;
        private IPAddress _ipAddress;

        public MainWindowViewModel(int port)
        {
            _port = port;
            InitIpAddress();
            InitServer();
        }

        private void InitIpAddress()
        {
            try
            {
                string ipString = new WebClient().DownloadString("https://ipinfo.io/ip/").Replace("\n", "");
                bool res = IPAddress.TryParse(ipString, out _ipAddress);
                if (!res)
                    _ipAddress = IPAddress.None;
            }
            catch
            {
                _ipAddress = IPAddress.Loopback;
            }
        }

        private void InitServer()
        {
            _server = new GovnoServer(_ipAddress, _port);
            _server.OnConnected += (_, args) =>
            {
                // TODO not add with connected address
                Contacts.Add(new ContactViewModel(args.SenderIPPoint)
                {
                    Connected = true
                });
            };
            _server.OnMessageReceived += (_, args) =>
            {
                if (TryFindContact(args.SenderIPPoint, out var contact))
                {
                    contact.Messages.Add(new TextMsgViewModel
                    {
                        IsMyMessage = false,
                        Text = args.Text
                    });
                }
            };
            _server.OnImageReceived += (_, args) =>
            {
                if (TryFindContact(args.SenderIPPoint, out var contact))
                {
                    contact.Messages.Add(new ImageMsgViewModel
                    {
                        IsMyMessage = false,
                        FileName = args.FileName,
                        ImageData = args.ImageData
                    });
                }
            };
            _server.OnFileReceived += (_, args) =>
            {
                if (TryFindContact(args.SenderIPPoint, out var contact))
                {
                    contact.Messages.Add(new ReceiveFileMsgViewModel(this, args.AccessCode)
                    {
                        FileName = args.FileName,
                        FileSize = args.FileSize
                    });
                }
            };
            _server.Start();
        }

        // TODO delete
        private void MakeTest()
        {
            //var testContact = new ContactViewModel(IPAddress.Loopback, 60401);

            //testContact.Messages.Add(new TextMsgViewModel
            //{
            //    IsMyMessage = true,
            //    Text = "My message"
            //});
            //testContact.Messages.Add(new TextMsgViewModel
            //{
            //    IsMyMessage = false,
            //    Text = "not my message halo 098 709 87 098as7d098as7 d09as87d as098 d7as09 8d7as0 9d8a7s d098as 7duas08ud has08 ds0a d78has d08as7h da0s98 dhas09 8dhas09d8hasnd8a7sdysa"
            //});

            //string imagePath = @"D:\_Google_Synchronized_\Synchronized\Картиночеки\ав\icdd.jpg";
            //byte[] imageData = File.ReadAllBytes(imagePath);
            //testContact.Messages.Add(new ImageMsgViewModel
            //{
            //    IsMyMessage = true,
            //    FileName = "my file.jpg",
            //    ImageData = imageData
            //});
            //testContact.Messages.Add(new ImageMsgViewModel
            //{
            //    IsMyMessage = false,
            //    FileName = "received file.jpg"
            //});

            //testContact.Messages.Add(new SendFileMsgViewModel
            //{
            //    FileName = "my file.pdf",
            //    FileSize = 2000345,

            //});
            //testContact.Messages.Add(new ReceiveFileMsgViewModel
            //{
            //    FileName = "another file.psd",
            //    FileSize = 20000345
            //});

            //Contacts.Add(testContact);

            //SelectedContact = testContact;
        }

        private void AddContact()
        {
            var dlg = new AddContactDialog();
            if (dlg.ShowDialog(out IPAddress ipAddres, out int port))
            {
                if (TryFindContact(ipAddres, port, out var _))
                    MessageBox.Show($"Contact with ip address {ipAddres} and port {port} already exists.");
                else
                    Contacts.Add(new ContactViewModel(ipAddres, port));
            }
        }

        private bool TryFindContact(IPAddress ipAddress, int port, out ContactViewModel result)
        {
            IPEndPoint point = new IPEndPoint(ipAddress, port);
            return TryFindContact(point, out result);
        }

        private bool TryFindContact(IPEndPoint point, out ContactViewModel result)
        {
            foreach (ContactViewModel contact in Contacts)
            {
                if (contact.IPEndPoint.Equals(point))
                {
                    result = contact;
                    return true;
                }
            }
            result = null;
            return false;
        }

        private async void ConnectAsync()
        {
            if (SelectedContact is null)
                return;

            try
            {
                await _server.ConnectAsync(SelectedContact.IPEndPoint);
                SelectedContact.Connected = true;
            }
            catch
            {
                MessageBox.Show("Error connecting.");// TODO delete
            }
        }

        private async void SendMessageAsync()
        {
            if (SelectedContact is null)
                return;
            // TODO not send in input is empty
            string message = InputedMessage;
            InputedMessage = "";

            try
            {
                await _server.SendTextMessageAsync(SelectedContact.IPEndPoint, message);
                SelectedContact.Messages.Add(new TextMsgViewModel
                {
                    IsMyMessage = true,
                    Text = message
                });
            }
            catch
            {
                MessageBox.Show("Error sending message.");// TODO delete
            }
        }

        private async void SendImageAsync()
        {
            if (SelectedContact is null)
                return;

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.EnsureFileExists = true;
                dialog.Filters.Add(new CommonFileDialogFilter("Image", "*.jpg, *.jpeg, *.png"));
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    long fileSize = new FileInfo(dialog.FileName).Length;
                    if (fileSize > GovnoServer.MaxImageSize)
                    {
                        MessageBox.Show($"Max size of file is {GovnoServer.MaxImageSize / 1024 / 1024} MB.");
                        return;
                    }

                    byte[] imageData = File.ReadAllBytes(dialog.FileName);
                    try
                    {
                        await _server.SendImageAsync(SelectedContact.IPEndPoint, 
                            Path.GetFileName(dialog.FileName), imageData);
                        SelectedContact.Messages.Add(new ImageMsgViewModel
                        {
                            IsMyMessage = true,
                            FileName = dialog.FileName,
                            ImageData = imageData
                        });
                    }
                    catch
                    {
                        MessageBox.Show("Error sending image message.");// TODO delete
                    }
                }
            }
        }

        private async void SendFileAsync()
        {
            if (SelectedContact is null)
                return;

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.EnsureFileExists = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    long fileSize = new FileInfo(dialog.FileName).Length;
                    if (fileSize > GovnoServer.MaxFileSize)
                    {
                        MessageBox.Show($"Max size of file is {GovnoServer.MaxFileSize / 1024 / 1024} MB.");
                        return;
                    }
                    try
                    {
                        await _server.SendFileAccessAsync(SelectedContact.IPEndPoint, dialog.FileName, (int)fileSize);
                        SelectedContact.Messages.Add(new SendFileMsgViewModel(this)
                        {
                            FileName = dialog.FileName,
                            FileSize = (int)fileSize
                        });
                    }
                    catch
                    {
                        MessageBox.Show("Error sending file access.");// TODO delete
                    }
                }
            }
        }

        private async void ReceiveFileAsync(ReceiveFileMsgViewModel msg, CancellationToken token)
        {
            if (SelectedContact is null)
                return;

            using (var dialog = new CommonSaveFileDialog())
            {
                dialog.DefaultExtension = Path.GetExtension(msg.FileName);
                dialog.DefaultFileName = msg.FileName;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        await _server.ReceiveFileAsync(SelectedContact.IPEndPoint, dialog.FileName, 
                            msg.AccessCode, msg.FileSize, token, progress =>
                            {
                                if (msg.DispatchStatus is FileMsgViewModel.InProgressDispatchStatus status)
                                    status.Progress = progress;
                            });
                    }
                    catch
                    {
                        MessageBox.Show("Error downloading file.");// TODO delete
                    }
                }
            }
        }
    }
}
