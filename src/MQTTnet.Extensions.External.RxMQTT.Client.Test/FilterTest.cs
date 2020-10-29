using MQTTnet.Extensions.External.RxMQTT.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class FilterTest
    {
        [Theory]
        [InlineData("Test/#", "Test/P/Te/T", true)]
        [InlineData("Test/Pre/#", "Test/Pre/Te/T", true)]
        [InlineData("P/+/Test", "P/T/Test", true)]
        [InlineData("P/+/Test", "Pre/T/Test", false)]
        [InlineData("Pre/+/Test", "Pre/T/Test", true)]
        [InlineData("Pre/+/Test", "P/T/Test", false)]
        public void Wildcards(string topicFilter, string topicRecived, bool result)
        {
            var filter = new TopicFilter(topicFilter);
            Assert.Equal(result, filter.IsTopicMatch(topicRecived));
        }

        [Theory]
        [InlineData("Test/Pre", "Test/Pre", true)]
        [InlineData("Test/Pre", "Test/Pre/Te/T", false)]
        [InlineData("Test/Te/T/Pre", "Test/Te/T/Pre", true)]
        [InlineData("Test/Te/T/Pre", "Test/Te/T/", false)]
        public void Topic_IsTopicMatch(string topicFilter, string topicRecived, bool result)
        {
            var filter = new TopicFilter(topicFilter);
            Assert.Equal(result, filter.IsTopicMatch(topicRecived));
        }

        [Fact]
        public void Topic_Set()
        {
            var topic = "Topic";

            // act
            var filter = new TopicFilter(topic);

            // test
            Assert.Equal(topic, filter.Topic);
        }
    }
}