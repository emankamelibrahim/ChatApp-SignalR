using ChatApp.Desktop.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;

namespace ChatApp.Desktop;

public class MainForm : Form
{
    private const string HubUrl = "https://localhost:7225/chatHub"; 

    private readonly LoginResult _loginResult;
    private HubConnection? _connection;

    private List<UserDto> _otherUsers = [];

    public record RoomDto(int Id, string Name, Guid CreatedByUserId);
    public record UserDto(Guid Id, string Email);

    private readonly Label _welcomeLabel = new() { Left = 20, Top = 10, Width = 400, Font = new Font("Segoe UI", 11) };

    private readonly TextBox _roomNameBox = new() { Left = 20, Top = 40, Width = 250 };
    private readonly Button _createRoomButton = new() { Left = 280, Top = 40, Width = 130, Text = "Create Room" };
    private readonly CheckedListBox _memberCheckList = new() { Left = 20, Top = 70, Width = 390, Height = 80 };

    private readonly ComboBox _roomComboBox = new() { Left = 20, Top = 160, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _deleteRoomButton = new() { Left = 280, Top = 160, Width = 130, Text = "Delete Room" };

    private readonly TextBox _roomMessageBox = new() { Left = 20, Top = 195, Width = 250 };
    private readonly Button _sendRoomMessageButton = new() { Left = 280, Top = 195, Width = 130, Text = "Send to Room" };

    private readonly ComboBox _userComboBox = new() { Left = 20, Top = 230, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _privateMessageBox = new() { Left = 20, Top = 260, Width = 250 };
    private readonly Button _sendPrivateMessageButton = new() { Left = 280, Top = 260, Width = 130, Text = "Send Private" };

    private readonly TextBox _activityList = new()
    {
        Left = 20,
        Top = 295,
        Width = 390,
        Height = 260,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        WordWrap = true
    };

    public MainForm(LoginResult loginResult)
    {
        _loginResult = loginResult;

        Text = "Chat App";
        Width = 520;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 600);

        Controls.Add(_welcomeLabel);
        Controls.Add(_roomNameBox);
        Controls.Add(_createRoomButton);
        Controls.Add(_memberCheckList);
        Controls.Add(_roomComboBox);
        Controls.Add(_deleteRoomButton);
        Controls.Add(_roomMessageBox);
        Controls.Add(_sendRoomMessageButton);
        Controls.Add(_userComboBox);
        Controls.Add(_privateMessageBox);
        Controls.Add(_sendPrivateMessageButton);
        Controls.Add(_activityList);

        _welcomeLabel.Text = $"Logged in as {_loginResult.Email}";

        _createRoomButton.Click += CreateRoomButton_Click;
        _deleteRoomButton.Click += DeleteRoomButton_Click;
        _sendRoomMessageButton.Click += SendRoomMessageButton_Click;
        _sendPrivateMessageButton.Click += SendPrivateMessageButton_Click;
        _roomComboBox.SelectedIndexChanged += (s, e) => UpdateDeleteButtonState();

        Load += MainForm_Load;
        FormClosed += MainForm_FormClosed;
    }

    private void Log(string message)
    {
        _activityList.AppendText(message + Environment.NewLine);
    }
    private void UpdateDeleteButtonState()
    {
        if (_roomComboBox.SelectedItem is RoomDto room)
        {
            _deleteRoomButton.Enabled = room.CreatedByUserId.ToString() == _loginResult.UserId;
        }
        else
        {
            _deleteRoomButton.Enabled = false;
        }
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(_loginResult.Token);
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                    return handler;
                };
            })
            .Build();

        RegisterHandlers();

        try
        {
            await _connection.StartAsync();
            Invoke(() => Log("Connected."));

            await LoadInitialData();
        }
        catch (Exception ex)
        {
            Invoke(() => Log($"Connection error: {ex.Message}"));
        }
    }

    private void RegisterHandlers()
    {
        _connection!.On<string>("ReceiveActivity", message =>
        {
            Invoke(() => Log(message));
        });

        _connection.On<int, string, string>("ReceiveRoomMessage", (roomId, senderName, content) =>
        {
            Invoke(() =>
            {
                var roomName = _roomComboBox.Items.Cast<RoomDto>().FirstOrDefault(r => r.Id == roomId)?.Name ?? $"Room {roomId}";
                Log($"[Room: {roomName}] {senderName}: {content}");
            });
        });

        _connection.On<string, string>("ReceivePrivateMessage", (senderName, content) =>
        {
            Invoke(() => Log($"[Private] {senderName}: {content}"));
        });

        _connection.On<int, string, Guid>("RoomCreated", (roomId, roomName, creatorUserId) =>
        {
            Invoke(async () =>
            {
                _roomComboBox.Items.Add(new RoomDto(roomId, roomName, creatorUserId));
                UpdateDeleteButtonState();
                await _connection!.InvokeAsync("JoinRoomGroup", roomId);
            });
        });

        _connection.On<int>("RoomDeleted", roomId =>
        {
            Invoke(() =>
            {
                var item = _roomComboBox.Items.Cast<RoomDto>().FirstOrDefault(r => r.Id == roomId);
                if (item is not null) _roomComboBox.Items.Remove(item);
                UpdateDeleteButtonState();
            });
        });
    }

    private async Task LoadInitialData()
    {
        var users = await _connection!.InvokeAsync<List<UserDto>>("GetAllUsers");
        var rooms = await _connection.InvokeAsync<List<RoomDto>>("GetMyRooms");

        Invoke(() =>
        {
            _otherUsers = users;
            _memberCheckList.Items.Clear();
            _userComboBox.Items.Clear();

            foreach (var u in users)
            {
                _memberCheckList.Items.Add(u.Email);
                _userComboBox.Items.Add(u);
            }
            _userComboBox.DisplayMember = "Email";

            _roomComboBox.Items.Clear();
            foreach (var r in rooms)
            {
                _roomComboBox.Items.Add(r);
            }
            _roomComboBox.DisplayMember = "Name";

            UpdateDeleteButtonState();
        });
    }

    private async void CreateRoomButton_Click(object? sender, EventArgs e)
    {
        var name = _roomNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var memberIds = new List<Guid>();
        for (int i = 0; i < _memberCheckList.Items.Count; i++)
        {
            if (_memberCheckList.GetItemChecked(i))
            {
                memberIds.Add(_otherUsers[i].Id);
            }
        }

        await _connection!.InvokeAsync("CreateRoom", name, memberIds);
        _roomNameBox.Clear();
    }

    private async void DeleteRoomButton_Click(object? sender, EventArgs e)
    {
        if (_roomComboBox.SelectedItem is RoomDto room)
        {
            await _connection!.InvokeAsync("DeleteRoom", room.Id);
        }
    }

    private async void SendRoomMessageButton_Click(object? sender, EventArgs e)
    {
        if (_roomComboBox.SelectedItem is not RoomDto room) return;
        var content = _roomMessageBox.Text.Trim();
        if (string.IsNullOrEmpty(content)) return;

        await _connection!.InvokeAsync("SendRoomMessage", room.Id, content);
        _roomMessageBox.Clear();
    }

    private async void SendPrivateMessageButton_Click(object? sender, EventArgs e)
    {
        if (_userComboBox.SelectedItem is not UserDto user) return;
        var content = _privateMessageBox.Text.Trim();
        if (string.IsNullOrEmpty(content)) return;

        await _connection!.InvokeAsync("SendPrivateMessage", user.Id, content);
        _privateMessageBox.Clear();
    }

    private async void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}