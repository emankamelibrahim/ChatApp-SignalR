using System.Net.Http;
using System.Net.Http.Json;
using ChatApp.Desktop.Models;

namespace ChatApp.Desktop;

public class LoginForm : Form
{
    private const string ApiBaseUrl = "https://localhost:7225"; 

    private readonly TextBox _emailBox = new() { Left = 100, Top = 20, Width = 180 };
    private readonly TextBox _passwordBox = new() { Left = 100, Top = 55, Width = 180, UseSystemPasswordChar = true };
    private readonly Button _loginButton = new() { Text = "Login", Left = 100, Top = 90, Width = 80, Height = 30 };
    private readonly Label _errorLabel = new() { Left = 20, Top = 125, Width = 260, Height = 40, ForeColor = Color.Red };

    public LoginForm()
    {
        Text = "Chat App - Login";
        Width = 320;
        Height = 220;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Text = "Email:", Left = 20, Top = 23, Width = 70 });
        Controls.Add(_emailBox);
        Controls.Add(new Label { Text = "Password:", Left = 20, Top = 58, Width = 70 });
        Controls.Add(_passwordBox);
        Controls.Add(_loginButton);
        Controls.Add(_errorLabel);

        _loginButton.Click += LoginButton_Click;
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        _errorLabel.Text = string.Empty;
        _loginButton.Enabled = false;

        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(ApiBaseUrl) };

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = _emailBox.Text,
                password = _passwordBox.Text
            });

            if (!response.IsSuccessStatusCode)
            {
                _errorLabel.Text = "Login failed. Check your email and password.";
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResult>();

            if (result is null)
            {
                _errorLabel.Text = "Unexpected response from server.";
                return;
            }

            var mainForm = new MainForm(result);
            mainForm.Show();
            Hide();
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }
}