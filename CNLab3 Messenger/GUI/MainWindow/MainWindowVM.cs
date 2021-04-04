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
    // TODO GLOBAL remove ports from brandmauer, router
    public partial class MainWindowVM : BaseViewModel
    {
        private int _port = 0;

        #region Bindings

        public IPAddress IPAddress => _ipAddress;

        public int Port => _port;

        private RelayCommand _addContactCmd;
        public RelayCommand AddContactCmd
            => _addContactCmd ?? (_addContactCmd = new RelayCommand(_ => AddContact()));

        private RelayCommand _removeContactCmd;
        public RelayCommand RemoveContactCmd
            => _removeContactCmd ?? (_removeContactCmd = new RelayCommand(_ => RemoveContact()));

        private ObservableCollection<ContactVM> _contacts;
        public ObservableCollection<ContactVM> Contacts
            => _contacts ?? (_contacts = new ObservableCollection<ContactVM>());

        private ContactVM _selectedContact;
        public ContactVM SelectedContact
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

        private Window _window;
        private SomeServer _server;
        private IPAddress _ipAddress;
        private Dictionary<string, SendFileMsgVM> _accessCodeToSendFileMsg = 
            new Dictionary<string, SendFileMsgVM>();

        public MainWindowVM(Window window, int port)
        {
            _window = window;

            _port = port;
            InitIpAddress();
            InitServer();

            // MakeTest1();
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
            _server = new SomeServer(_ipAddress, _port);
            _server.OnConnected += (_, args) =>
            {
                if (TryFindContact(args.SenderIPPoint, out var contact))
                    contact.Connected = true;
                else
                {
                    contact = new ContactVM(args.SenderIPPoint)
                    {
                        Connected = true
                    };
                    Contacts.Add(contact);
                    if (Contacts.Count == 1)
                        SelectedContact = contact;
                }
            };
            _server.OnMessageReceived += (_, args) =>
            {
                if (TryFindContact(args.SenderIPPoint, out var contact))
                {
                    contact.Messages.Add(new TextMsgVM
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
                    contact.Messages.Add(new ImageMsgVM
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
                    contact.Messages.Add(new ReceiveFileMsgVM(this, args.AccessCode)
                    {
                        FileName = args.FileName,
                        FileSize = args.FileSize
                    });
                }
            };
            _server.OnStartFileSending += (_, args) =>
            {
                if (_accessCodeToSendFileMsg.ContainsKey(args.AccessCode))
                {
                    var viewModel = _accessCodeToSendFileMsg[args.AccessCode];
                    viewModel.OnStart();
                }
            };
            _server.OnFileSendingProgressChanged += (_, args) =>
            {
                if (_accessCodeToSendFileMsg.ContainsKey(args.AccessCode))
                {
                    var viewModel = _accessCodeToSendFileMsg[args.AccessCode];
                    viewModel.OnProgressChanged(args.Progress);

                    if (args.Progress >= 1)
                    {
                        viewModel.OnDoneSuccessfully();
                        _accessCodeToSendFileMsg.Remove(args.AccessCode);
                    }
                }
            };
            _server.OnCanceledSending += (_, args) =>
            {
                if (_accessCodeToSendFileMsg.ContainsKey(args.AccessCode))
                {
                    var viewModel = _accessCodeToSendFileMsg[args.AccessCode];
                    viewModel.OnCanceled();
                    _accessCodeToSendFileMsg.Remove(args.AccessCode);
                }
            };
            _server.OnErrorFileSending += (_, args) =>
            {
                if (_accessCodeToSendFileMsg.ContainsKey(args.AccessCode))
                {
                    var viewModel = _accessCodeToSendFileMsg[args.AccessCode];
                    viewModel.OnDoneWithError();
                }
            };
            _server.Start();
        }

        private async void MakeTest1()
        {
            if (_port == 60399)
            {
                IPEndPoint point = new IPEndPoint(IPAddress.Parse("77.37.245.90"), 60400);
                var contact = new ContactVM(point);
                Contacts.Add(contact);
                await _server.ConnectAsync(point);
                contact.Connected = true;
                SelectedContact = contact;
            }
        }

        private void AddContact()
        {
            var dlg = new AddContactDialog()
            {
                Owner = _window
            };

            if (dlg.ShowDialog(out IPAddress ipAddres, out int port))
            {
                if (TryFindContact(ipAddres, port, out var _))
                    MessageBox.Show($"Contact with ip address {ipAddres} and port {port} already exists.");
                else
                    Contacts.Add(new ContactVM(ipAddres, port));
            }
        }

        private void RemoveContact()
        {
            if (SelectedContact is null)
                return;

            var result = MessageBox.Show($"Are you sure about disconnect contact with address {SelectedContact.IPEndPoint}?",
                "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
                return;

            var contact = SelectedContact;
            SelectedContact = null;
            Contacts.Remove(contact);
            _server.Disconnect(contact.IPEndPoint);
        }

        private bool TryFindContact(IPAddress ipAddress, int port, out ContactVM result)
        {
            IPEndPoint point = new IPEndPoint(ipAddress, port);
            return TryFindContact(point, out result);
        }

        private bool TryFindContact(IPEndPoint point, out ContactVM result)
        {
            foreach (ContactVM contact in Contacts)
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

            if (SelectedContact.Connected)
            {
                var result = MessageBox.Show("Selected contact connected already. Do you want to reconnect?",
                    "?", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    SelectedContact.Connected = false;
                else
                    return;
            }

            try
            {
                await _server.ConnectAsync(SelectedContact.IPEndPoint);
                SelectedContact.Connected = true;
            }
            catch
            {
                MessageBox.Show($"Error connecting to {SelectedContact.IPEndPoint}.");
            }
        }

        private async void SendMessageAsync()
        {
            if (SelectedContact is null || InputedMessage.Length == 0)
                return;

            string message = InputedMessage;
            InputedMessage = "";

            try
            {
                await _server.SendTextMessageAsync(SelectedContact.IPEndPoint, message);
                SelectedContact.Messages.Add(new TextMsgVM
                {
                    IsMyMessage = true,
                    Text = message
                });
            }
            catch
            {
                MessageBox.Show($"Error sending message to {SelectedContact.IPEndPoint}.");
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
                    if (fileSize > SomeServer.MaxImageSize)
                    {
                        MessageBox.Show($"Max size of file is {SomeServer.MaxImageSize / 1024 / 1024} MB.");
                        return;
                    }

                    byte[] imageData = File.ReadAllBytes(dialog.FileName);
                    try
                    {
                        await _server.SendImageAsync(SelectedContact.IPEndPoint, 
                            Path.GetFileName(dialog.FileName), imageData);
                        SelectedContact.Messages.Add(new ImageMsgVM
                        {
                            IsMyMessage = true,
                            FileName = dialog.FileName,
                            ImageData = imageData
                        });
                    }
                    catch
                    {
                        MessageBox.Show($"Error sending image to {SelectedContact.IPEndPoint}.");
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
                    if (fileSize > SomeServer.MaxFileSize)
                    {
                        MessageBox.Show($"Max size of file is {SomeServer.MaxFileSize / 1024 / 1024} MB.");
                        return;
                    }
                    try
                    {
                        string accessCode = await _server.SendFileAccessAsync(SelectedContact.IPEndPoint, dialog.FileName, (int)fileSize);
                        var sendFileMsg = new SendFileMsgVM(this, accessCode)
                        {
                            FileName = dialog.FileName,
                            FileSize = (int)fileSize
                        };
                        _accessCodeToSendFileMsg.Add(accessCode, sendFileMsg);
                        SelectedContact.Messages.Add(sendFileMsg);
                    }
                    catch
                    {
                        MessageBox.Show($"Error sending file to {SelectedContact.IPEndPoint}.");
                    }
                }
            }
        }

        private async void ReceiveFileAsync(ReceiveFileMsgVM msg,
            CancellationToken token, Action<double> progressCallback)
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
                        msg.OnStarted();
                        await _server.ReceiveFileAsync(SelectedContact.IPEndPoint, dialog.FileName, 
                            msg.AccessCode, msg.FileSize, token, progressCallback);
                        msg.OnDoneSuccessfully();
                    }
                    catch (TaskCanceledException)
                    {
                        msg.OnCanceled();
                    }
                    catch (Exception)
                    {
                        msg.OnDoneWithError();
                    }
                }
            }
        }

        private void BlockFileAccess(string accessCode)
        {
            _server.BlockFileAccess(accessCode);
        }

        private void CancelFileSending(string accessCode)
        {
            _server.CancelFileSending(accessCode);
        }
    }
}
