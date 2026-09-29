using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectOLDR
{
    public class AuthService
    {
        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<bool> RegisterUserAsync(string email, string password, Label errorLabel, TextBox emailBox, TextBox passwordBox)
        {
            try
            {
                ApiResult result = await SupabaseApi.RegisterAsync(email, password);
                if (result.Success)
                {
                    emailBox.Clear();
                    passwordBox.Clear();
                    errorLabel.Text = string.Empty;
                    return true;
                }
                errorLabel.Text = result.Message;
                return false;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Rate limit exceeded"))
                {
                    errorLabel.Text = "Rate limit of account created please be patient";
                }
                else
                {
                    errorLabel.Text = "An error occurred during registration.";
                }
                return false;
            }
        }
    }
}
