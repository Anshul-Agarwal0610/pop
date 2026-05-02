namespace BackendAPI.Interfaces
{
    /// <summary>
    /// Abstraction over LLM providers (OpenAI, Anthropic, Custom VM).
    /// Each provider receives a structured prompt and returns raw JSON string
    /// that PollGenerationService will parse into a GeneratedPoll.
    /// </summary>
    public interface ILlmProvider
    {
        /// <summary>Provider identifier — must match PollGen:Provider config value.</summary>
        string ProviderName { get; }

        /// <summary>
        /// Send the prompt to the LLM and return the raw response text.
        /// Returns null if the call fails or the model returns an unusable response.
        /// </summary>
        Task<string?> CompleteAsync(string prompt, CancellationToken ct = default);
    }
}
