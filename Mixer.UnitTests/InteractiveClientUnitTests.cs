﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mixer.Base;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mixer.UnitTests
{
    [TestClass]
    public class InteractiveClientUnitTests : UnitTestBase
    {
        List<InteractiveReplyPacket> replyPackets = new List<InteractiveReplyPacket>();
        List<InteractiveMethodPacket> methodPackets = new List<InteractiveMethodPacket>();

        [TestInitialize]
        public void TestInitialize()
        {
            this.ClearRepliesAndMethods();
        }

        [TestMethod]
        public void ConnectToInteractive()
        {
            this.InteractiveWrapper((MixerClient client, InteractiveClient interactiveClient) =>
            {
                return Task.FromResult(0);
            });
        }

        [TestMethod]
        public void ReadyInteractive()
        {
            this.InteractiveWrapper(async (MixerClient client, InteractiveClient interactiveClient) =>
            {
                await this.ReadyInteractive(interactiveClient);
            });
        }

        [TestMethod]
        public void GetTime()
        {
            this.InteractiveWrapper(async (MixerClient client, InteractiveClient interactiveClient) =>
            {
                await this.ReadyInteractive(interactiveClient);

                this.ClearRepliesAndMethods();

                DateTimeOffset? dateTime = await interactiveClient.GetTime();

                Assert.IsNotNull(dateTime);
                Assert.IsTrue(DateTimeOffset.UtcNow.Date.Equals(dateTime.GetValueOrDefault().Date));
            });
        }

        [TestMethod]
        public void GetMemoryStats()
        {
            this.InteractiveWrapper(async (MixerClient client, InteractiveClient interactiveClient) =>
            {
                await this.ReadyInteractive(interactiveClient);

                this.ClearRepliesAndMethods();

                InteractiveIssueMemoryWarningModel memory = await interactiveClient.GetMemoryStates();

                Assert.IsNotNull(memory);
                Assert.IsTrue(memory.usedBytes > 0);
                Assert.IsTrue(memory.totalBytes > 0);
                Assert.IsNotNull(memory.resources);
            });
        }

        private void InteractiveWrapper(Func<MixerClient, InteractiveClient, Task> function)
        {
            this.TestWrapper(async (MixerClient client) =>
            {
                this.ClearRepliesAndMethods();

                ChannelModel channel = await ChannelsServiceUnitTests.GetChannel(client);
                IEnumerable<InteractiveGameListingModel> games = await client.Interactive.GetOwnedInteractiveGames(channel);

                Assert.IsNotNull(games);
                Assert.IsTrue(games.Count() > 0);

                InteractiveClient interactiveClient = await InteractiveClient.CreateFromChannel(client, channel, games.First());

                interactiveClient.ReplyOccurred += InteractiveClient_ReplyOccurred;
                interactiveClient.MethodOccurred += InteractiveClient_MethodOccurred;

                Assert.IsTrue(await interactiveClient.Connect());

                await function(client, interactiveClient);

                await interactiveClient.Disconnect();
            });
        }

        private async Task ReadyInteractive(InteractiveClient interactiveClient)
        {
            this.ClearRepliesAndMethods();

            Assert.IsTrue(await interactiveClient.Ready());
        }

        private void InteractiveClient_ReplyOccurred(object sender, InteractiveReplyPacket e)
        {
            this.replyPackets.Add(e);
        }

        private void InteractiveClient_MethodOccurred(object sender, InteractiveMethodPacket e)
        {
            this.methodPackets.Add(e);
        }

        private void ClearRepliesAndMethods()
        {
            this.replyPackets.Clear();
            this.methodPackets.Clear();
        }
    }
}