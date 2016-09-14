using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Models
{
    public class ErrorModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public ErrorModel(Match match)
        {
            Id = int.Parse(match.Groups[1].Value);
            Message = match.Groups[2].Value.Replace("\\s", " ");
        }
    }
}
