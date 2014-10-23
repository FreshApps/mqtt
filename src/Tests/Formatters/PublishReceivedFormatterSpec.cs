﻿using System;
using System.IO;
using System.Threading.Tasks;
using Hermes;
using Hermes.Formatters;
using Hermes.Messages;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Tests.Formatters
{
	public class PublishReceivedFormatterSpec
	{
		readonly Mock<IChannel<IMessage>> messageChannel;
		readonly Mock<IChannel<byte[]>> byteChannel;

		public PublishReceivedFormatterSpec ()
		{
			this.messageChannel = new Mock<IChannel<IMessage>> ();
			this.byteChannel = new Mock<IChannel<byte[]>> ();
		}

		[Theory]
		[InlineData("Files/Packets/PublishReceived.packet", "Files/Messages/PublishReceived.json")]
		public async Task when_reading_publish_received_packet_then_succeeds(string packetPath, string jsonPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);

			var expectedPublishReceived = Packet.ReadMessage<PublishReceived> (jsonPath);
			var sentPublishReceived = default(PublishReceived);

			this.messageChannel
				.Setup (c => c.SendAsync (It.IsAny<IMessage>()))
				.Returns(Task.Delay(0))
				.Callback<IMessage>(m =>  {
					sentPublishReceived = m as PublishReceived;
				});

			var formatter = new FlowMessageFormatter<PublishReceived>(MessageType.PublishReceived, id => new PublishReceived(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);

			await formatter.ReadAsync (packet);

			Assert.Equal (expectedPublishReceived, sentPublishReceived);
		}

		[Theory]
		[InlineData("Files/Packets/PublishReceived_Invalid_HeaderFlag.packet")]
		public void when_reading_invalid_publish_received_packet_then_fails(string packetPath)
		{
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var formatter = new FlowMessageFormatter<PublishReceived> (MessageType.PublishReceived, id => new PublishReceived(id), this.messageChannel.Object, this.byteChannel.Object);
			var packet = Packet.ReadAllBytes (packetPath);
			
			var ex = Assert.Throws<AggregateException> (() => formatter.ReadAsync (packet).Wait());

			Assert.True (ex.InnerException is ProtocolException);
		}

		[Theory]
		[InlineData("Files/Messages/PublishReceived.json", "Files/Packets/PublishReceived.packet")]
		public async Task when_writing_publish_received_packet_then_succeeds(string jsonPath, string packetPath)
		{
			jsonPath = Path.Combine (Environment.CurrentDirectory, jsonPath);
			packetPath = Path.Combine (Environment.CurrentDirectory, packetPath);

			var expectedPacket = Packet.ReadAllBytes (packetPath);
			var sentPacket = default(byte[]);

			this.byteChannel
				.Setup (c => c.SendAsync (It.IsAny<byte[]>()))
				.Returns(Task.Delay(0))
				.Callback<byte[]>(b =>  {
					sentPacket = b;
				});

			var formatter = new FlowMessageFormatter<PublishReceived>(MessageType.PublishReceived, id => new PublishReceived(id), this.messageChannel.Object, this.byteChannel.Object);
			var publishReceived = Packet.ReadMessage<PublishReceived> (jsonPath);

			await formatter.WriteAsync (publishReceived);

			Assert.Equal (expectedPacket, sentPacket);
		}
	}
}