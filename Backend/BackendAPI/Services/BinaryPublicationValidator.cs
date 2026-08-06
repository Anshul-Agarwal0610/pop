using BackendAPI.Interfaces;

namespace BackendAPI.Services;

public static class BinaryPublicationValidator
{
    public static bool IsPublishable(GeneratedPoll poll) =>
        poll.Options.Count == 2
        && poll.Options[0].Equals("Up", StringComparison.OrdinalIgnoreCase)
        && poll.Options[1].Equals("Against", StringComparison.OrdinalIgnoreCase);
}
