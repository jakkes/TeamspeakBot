namespace TeamspeakBotv2.Commands
{
    public class LoginCommand : NonCollectCommand
    {
        public override void HandleResponse(string msg)
        {
            throw new RegexMatchException();
        }

        public LoginCommand(string username, string password)
        {
            Message = string.Format("login {0} {1}", username, password);
        }
    }
}
