using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SmartGlass.Common;
using SmartGlass.Nano;
using SmartGlass.Nano.Packets;
using SmartGlass.Nano.Consumer;

namespace SmartGlass.Nano.Channels
{
    public class AudioChannel : AudioChannelBase, IStreamingChannel
    {
        readonly Action<AudioDataEventArgs> _fireAudioDataEvent;
        public override NanoChannel Channel => NanoChannel.Audio;
        public override int ProtocolVersion => 4;
        public Packets.AudioFormat[] AvailableFormats { get; internal set; }

        internal AudioChannel(NanoRdpTransport transport, ChannelOpen openPacket,
            Action<AudioDataEventArgs> fireEvent)
            : base(transport, openPacket)
        {
            _fireAudioDataEvent = fireEvent;
        }

        public async Task StartStreamAsync()
        {
            await SendControlAsync(AudioControlFlags.StartStream);
        }

        public async Task StopStreamAsync()
        {
            await SendControlAsync(AudioControlFlags.StopStream);
        }

        public override void OnControl(AudioControl control)
        {
            throw new NotSupportedException("Control message on client side");
        }

        public override void OnData(AudioData data)
        {
            DateTime frameTime = DateTimeHelper.FromTimestampMicroseconds(data.Timestamp, ReferenceTimestamp);
            _fireAudioDataEvent(new AudioDataEventArgs(frameTime, data));
        }

        public async Task SendClientHandshakeAsync(AudioFormat format)
        {
            var packet = new AudioClientHandshake(FrameId, format);

            await SendAsync(packet);
        }

        private async Task SendControlAsync(AudioControlFlags flags)
        {
            var packet = new AudioControl(flags);
            await SendAsync(packet);
        }

        public async Task OpenAsync()
        {
            var handshake = await WaitForMessageAsync<AudioServerHandshake>(
                TimeSpan.FromSeconds(3),
                async () => await SendChannelOpen(Channel, _channelOpenData.Flags),
                p => p.Channel == NanoChannel.Audio
            );

            ReferenceTimestampUlong = handshake.ReferenceTimestamp;
            AvailableFormats = handshake.Formats;
        }
    }
}
