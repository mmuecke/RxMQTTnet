using System.Text.RegularExpressions;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// A filter for a mqtt topic allowing wildcards.
    /// </summary>
    /// <remarks>Wildcards '#' and '+' are allowed.</remarks>
    public class TopicFilter
    {
        private const string charsToIgnore = @"#\+/ ";
        private readonly Regex topicRegex;

        /// <summary>
        /// Crate a filter for a mqtt topic allowing wildcards.
        /// </summary>
        /// <remarks>Wildcards '#' and '+' are allowed.</remarks>
        /// <param name="topic">The topic to filter for.</param>
        public TopicFilter(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new System.ArgumentException($"'{nameof(topic)}' cannot be null or whitespace", nameof(topic));

            Topic = topic;
            var topicRegexStrings = topic == "#"
                ? ".*"
                : topic
                    .Replace("/+/", $"/([^{charsToIgnore}]+)?/")
                    .Replace("/#", $"/([^{charsToIgnore}]+/?)+")
                    .Replace("/", @"\/");

            topicRegex = new Regex($"^{topicRegexStrings}$");
        }

        /// <summary>
        /// Check if the string matches the topic.
        /// </summary>
        /// <param name="topic">The topic to check.</param>
        /// <returns>If the topic match the string.</returns>
        public bool IsTopicMatch(string topic) => topicRegex.IsMatch(topic);

        /// <summary>
        /// The topic to filter for.
        /// </summary>
        public string Topic { get; }
    }
}