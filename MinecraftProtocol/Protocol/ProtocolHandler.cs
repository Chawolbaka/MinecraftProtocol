﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol.Packets;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Protocol.VersionCompatible;
using System.Net;

namespace MinecraftProtocol.Protocol
{
    public static class ProtocolHandler
    {
        /*
         * 数据包格式(https://github.com/bangbang93/minecraft-protocol/blob/master/protocol.md)
         * 
         * 未压缩的数据包:
         * 
         * 名称      类型
         * 数据包长度 Varint
         * 数据包ID  Varint
         * 数据
         * 
         * 压缩的数据包:
         * 
         * 名称      类型     备注
         * 数据包长度 Varint  等于下方两者解压前的总字节数
         * 解压后长度 Varint  若为0则表示本包未被压缩
         * 被压缩的数据       经Zlib压缩.开头是一个VarInt字段,代表数据包ID,然后是数据包数据.
         */

        /// <summary>获取数据包的长度</summary>
        public static int GetPacketLength(TcpClient tcp) => GetPacketLength(tcp.Client);
        /// <summary>获取数据包的长度</summary>
        public static int GetPacketLength(Socket tcp) => VarInt.Read(tcp);

        public static byte[] ReceiveData(int start, int offset, Socket tcp)
        {
            byte[] buffer = new byte[offset - start];
            Receive(buffer, start, offset, SocketFlags.None, tcp);
            return buffer;
        }
        public static Packet ReceivePacket(Socket tcp,int compressionThreshold)
        {
            Packet recPacket = new Packet();
            int PacketLength = VarInt.Read(tcp);
            recPacket.WriteBytes(ReceiveData(0, PacketLength, tcp));
            if (compressionThreshold > 0)
            {
                int DataLength = ReadVarInt(recPacket.Data);
                if (DataLength != 0) //如果是0的话就代表这个数据包没有被压缩
                {
                    byte[] uncompressed = ZlibUtils.Decompress(recPacket.Data.ToArray(), DataLength);
                    recPacket.Data.Clear();
                    recPacket.Data.AddRange(uncompressed);
                }
            }
            recPacket.ID = ReadVarInt(recPacket.Data);
            return recPacket;
        }
        public static bool CheckConnect(Socket tcp)
        {
            var connections = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections()
                .Where(x => x.LocalEndPoint.Equals(tcp.LocalEndPoint) && x.RemoteEndPoint.Equals(tcp.RemoteEndPoint));
            return connections != null && connections.Any() && connections.First().State == System.Net.NetworkInformation.TcpState.Established;
        }
        private static void Receive(byte[] buffer, int start, int offset, SocketFlags flags, Socket tcp)
        {
            int read = 0;
            int count = 0;
            while (read < offset)
            {
                if (count >= 26)
                {
                    if (!CheckConnect(tcp))
                    {
                        tcp.Disconnect(false);
                        throw new SocketException((int)SocketError.ConnectionReset);
                    }
                    else
                        count /= 2;
                }
                else
                {
                    read += tcp.Receive(buffer, start + read, offset - read, flags);
                    count++;
                }
            }
        }

        public static bool ReadBoolean(List<byte> cache, bool readOnly = false) => ReadBoolean(cache, 0, out _, readOnly);
        public static bool ReadBoolean(List<byte> cache, int offset, bool readOnly = false) => ReadBoolean(cache, offset, out _, readOnly);
        public static bool ReadBoolean(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            bool result = cache[offset] == 0x01 ? true : false;
            count = offset + 1;
            if (!readOnly)
                cache.RemoveAt(offset);
            return result;
        }

        public static sbyte ReadByte(List<byte> cache, bool readOnly = false) => ReadByte(cache, 0, out _, readOnly);
        public static sbyte ReadByte(List<byte> cache, int offset, bool readOnly = false) => ReadByte(cache, offset, out _, readOnly);
        public static sbyte ReadByte(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            sbyte sb = (sbyte)cache[offset];
            count = offset + 1;
            if (!readOnly)
                cache.RemoveAt(offset);
            return sb;
        }

        public static byte ReadUnsignedByte(List<byte> cache, bool readOnly = false) => ReadUnsignedByte(cache, 0, out _, readOnly);
        public static byte ReadUnsignedByte(List<byte> cache, int offset, bool readOnly = false) => ReadUnsignedByte(cache, offset, out _, readOnly);
        public static byte ReadUnsignedByte(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            byte b = cache[offset];
            count = offset + 1;
            if (!readOnly)
                cache.RemoveAt(offset);
            return b;
        }

        public static short ReadShort(List<byte> cache, bool readOnly = false) => ReadShort(cache, 0, out _, readOnly);
        public static short ReadShort(List<byte> cache, int offset, bool readOnly = false) => ReadShort(cache, offset, out _, readOnly);
        public static short ReadShort(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            short result = (short)(cache[offset] << 8 | cache[offset + 1]);
            count = offset + 2;
            if (!readOnly)
                cache.RemoveRange(offset, 2);
            return result;
        }

        public static ushort ReadUnsignedShort(List<byte> cache, bool readOnly = false) => ReadUnsignedShort(cache, 0, out _, readOnly);
        public static ushort ReadUnsignedShort(List<byte> cache, int offset, bool readOnly = false) => ReadUnsignedShort(cache, 0, out _, readOnly);
        public static ushort ReadUnsignedShort(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            ushort result = (ushort)(cache[offset] << 8 | cache[offset + 1]);
            count = offset + 2;
            if (!readOnly)
                cache.RemoveRange(offset, 2);
            return result;
        }

        public static int ReadInt(List<byte> cache, bool readOnly = false) => ReadInt(cache, 0, out _, readOnly);
        public static int ReadInt(List<byte> cache, int offset, bool readOnly = false) => ReadInt(cache, offset, out _, readOnly);
        public static int ReadInt(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            int result = 
                cache[offset]   << 24 |
                cache[offset+1] << 16 |
                cache[offset+2] << 08 |
                cache[offset+3];
            count = offset + 4;
            if (!readOnly)
                cache.RemoveRange(offset, 4);
            return result;
        }

        public static long ReadLong(List<byte> cache, bool readOnly = false) => ReadLong(cache, 0, out _, readOnly);
        public static long ReadLong(List<byte> cache, int offset, bool readOnly = false) => ReadLong(cache, offset, out _, readOnly);
        public static long ReadLong(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            long result =
                ((long)cache[offset])   << 56 |
                ((long)cache[offset+1]) << 48 |
                ((long)cache[offset+2]) << 40 |
                ((long)cache[offset+3]) << 32 |
                ((long)cache[offset+4]) << 24 |
                ((long)cache[offset+5]) << 16 |
                ((long)cache[offset+6]) << 08 |
                cache[offset + 7];

            count = offset + 8;
            if (!readOnly)
                cache.RemoveRange(offset, 8);
            return result;
        }

        public static float ReadFloat(List<byte> cache, bool readOnly = false) => ReadFloat(cache, 0, out _, readOnly);
        public static float ReadFloat(List<byte> cache, int offset, bool readOnly = false) => ReadFloat(cache, offset, out _, readOnly);
        public static float ReadFloat(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            count = offset + size;
            for (int i = 0; i < size; i++)
                buffer[i] = cache[offset+3-i];
            if (!readOnly)
                cache.RemoveRange(offset, size);
            return BitConverter.ToSingle(buffer);
        }

        public static double ReadDouble(List<byte> cache, bool readOnly = false) => ReadDouble(cache, 0, out _, readOnly);
        public static double ReadDouble(List<byte> cache, int offset, bool readOnly = false) => ReadDouble(cache, offset, out _, readOnly);
        public static double ReadDouble(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            count = offset + size;
            for (int i = 0; i <size; i++)
                buffer[i] = cache[offset+7-i];
            if (!readOnly)
                cache.RemoveRange(offset, size);
            return BitConverter.ToDouble(buffer);
        }

        public static string ReadString(List<byte> cache, bool readOnly = false) => ReadString(cache, 0, out _, readOnly);
        public static string ReadString(List<byte> cache, int offset, bool readOnly = false) => ReadString(cache, offset, out _, readOnly);
        public static string ReadString(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            int length = ReadVarInt(cache, offset, out int EndPos, true);
            byte[] buffer = new byte[length];
            for (int i = EndPos; i < length + EndPos; i++)
                buffer[i - EndPos] = cache[i];
            string result = Encoding.UTF8.GetString(buffer);
            count = EndPos + length;
            if (!readOnly)
                cache.RemoveRange(offset, length + (EndPos - offset));
            return result;
        }

        public static int ReadVarShort(List<byte> cache, bool readOnly = false) => ReadVarShort(cache, 0, out _, readOnly);
        public static int ReadVarShort(List<byte> cache, int offset, bool readOnly = false) => ReadVarShort(cache, offset, out _, readOnly);
        public static int ReadVarShort(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            int result = VarShort.Read(cache, offset, out int length);
            count = length + offset;
            if (!readOnly)
                cache.RemoveRange(offset, length);
            return result;
        }

        public static int ReadVarInt(List<byte> cache, bool readOnly = false) => ReadVarInt(cache, 0, out _, readOnly);
        public static int ReadVarInt(List<byte> cache, int offset, bool readOnly = false) => ReadVarInt(cache, offset, out _, readOnly);
        public static int ReadVarInt(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            int result = VarInt.Read(cache, offset, out int length);
            count = length + offset;
            if (!readOnly)
                cache.RemoveRange(offset, length);
            return result;
        }

        public static long ReadVarLong(List<byte> cache, bool readOnly = false) => ReadVarLong(cache, 0, out _, readOnly);
        public static long ReadVarLong(List<byte> cache, int offset, bool readOnly = false) => ReadVarLong(cache, offset, out _, readOnly);
        public static long ReadVarLong(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            long result = VarLong.Read(cache, offset, out int length);
            count = length + offset;
            if (!readOnly)
                cache.RemoveRange(offset, length);
            return result;
        }

        public static UUID ReadUUID(List<byte> cache, bool readOnly = false) => ReadUUID(cache, 0, out _, readOnly);
        public static UUID ReadUUID(List<byte> cache, int offset, bool readOnly = false) => ReadUUID(cache, offset, out _, readOnly);
        public static UUID ReadUUID(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            return new UUID(
                ReadLong(cache, offset, out offset, readOnly),
                ReadLong(cache, offset, out count, readOnly));
        }

        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, bool readOnly = false) => ReadByteArray(cache, protocolVersion, 0, out _, readOnly);
        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, int offset, bool readOnly = false) => ReadByteArray(cache, protocolVersion, offset, out _, readOnly);
        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, out int count, bool readOnly = false) => ReadByteArray(cache, protocolVersion, 0, out count, readOnly);
        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, int offset, out int count, bool readOnly = false)
        {
            int ArrayLength;
            int EndPos;
            if (protocolVersion>=ProtocolVersionNumbers.V14w21a)
                ArrayLength = ReadVarInt(cache, offset, out EndPos, true);
            else
                ArrayLength = ReadShort(cache, offset, out EndPos, true);
            count = EndPos + ArrayLength;
            byte[] buffer = new byte[ArrayLength];
            for (int i = 0; i < ArrayLength; i++)
                buffer[i] = cache[EndPos+i];
            if (!readOnly)
                cache.RemoveRange(offset, ArrayLength+(EndPos-offset));
            return buffer;
        }
        public static byte GetBytes(bool value) => (byte)(value ? 1 : 0);
        public static byte[] GetBytes(short value)
        {
            byte[] data = new byte[sizeof(short)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            return data;
        }
        public static byte[] GetBytes(ushort value)
        {
            byte[] data = new byte[sizeof(ushort)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            return data;
        }
        public static byte[] GetBytes(int value)
        {
            byte[] data = new byte[sizeof(int)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            return data;
        }
        public static byte[] GetBytes(long value)
        {
            byte[] data = new byte[sizeof(long)];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            return data;
        }
        public static byte[] GetBytes(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            return ConcatBytes(VarInt.GetBytes(str.Length), str);
        }

        /// <summary>
        /// 拼接Byte数组
        /// </summary>
        public static byte[] ConcatBytes(params IEnumerable<byte>[] bytes)
        {
            List<byte> buffer = new List<byte>();
            foreach (var array in bytes)
            {
                if (array != null)
                    buffer.AddRange(array);
            }
            return buffer.ToArray();
        }
        /// <summary>
        /// 对比两个Dictionary的值是否相等
        /// </summary>
        public static bool Compare<K, V>(IDictionary<K, V> a, IDictionary<K, V> b)
        {
            if (a is null && b is null || ReferenceEquals(a, b)) return true;
            if ((a is null && b != null) || (a != null && b is null)) return false;
            if (a.Count != b.Count) return false;
            if (a.Count == 0 && b.Count == 0) return true;
            if (a.GetEnumerator().Current is IEquatable<V>&& b.GetEnumerator().Current is IEquatable<V>)
            {
                foreach (var value in a)
                    if (!b.ContainsKey(value.Key) || !((IEquatable<V>)b[value.Key]).Equals(value.Value)) return false;
            }
            else
            {
                foreach (var value in a)
                    if (!b.ContainsKey(value.Key) || !b[value.Key].Equals(value.Value)) return false;
            }
            return true;
        }
        /// <summary>
        /// 对比两个集合的值是否相等
        /// </summary>
        public static bool Compare<T>(IList<T> a, IList<T> b)
        {
            if (a is null && b is null || ReferenceEquals(a, b)) return true;
            if ((a is null && b != null) || (a != null && b is null)) return false;
            if (a.Count != b.Count) return false;
            if (a.Count == 0 && b.Count == 0) return true;
            if (a[0] is IEquatable<T> && b[0] is IEquatable<T>)
            {
                for (int i = 0; i < a.Count; i++)
                    if (!((IEquatable<T>)a[i]).Equals(b[i])) return false;
            }
            else
            {
                for (int i = 0; i < a.Count; i++)
                    if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }
        /// <summary>
        /// 对比两个Byte数组的值是否相等
        /// </summary>
        public static bool Compare(byte[] b1, byte[] b2)
        {
            if (b1 != null && b2 != null && b1.Length != b2.Length)
                return false;
            if (ReferenceEquals(b1, b2) || (b1.Length == 0 && b2.Length == 0))
                return true;
            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i]) return false;
            return true;
        }

        /// <summary>
        /// 检查IP是否是保留地址
        /// </summary>
        public static bool IsReserved(this IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return IsAddressReserved(ip.GetAddressBytes());
            else
                throw new NotSupportedException("不支持IPv6");
        }

        public static bool IsAddressReserved(byte[] ip)
        {
            //数据来源:https://zh.wikipedia.org/wiki/%E4%BF%9D%E7%95%99IP%E5%9C%B0%E5%9D%80

            //效用域:专用网络

            //用于专用网络内的本地通信。
            if (ip[0] == 10) return true;
            //用于在电信级NAT环境中服务提供商与其用户通信。[3]
            else if (ip[0] == 100 && (ip[1] & 0xC0) == 64) return true;
            //用于专用网络中的本地通信。
            else if (ip[0] == 172 && (ip[1] & 0xF0) == 16) return true;
            //用于专用网络中的本地通信。
            else if (ip[0] == 192 && ip[1] == 168) return true;
            //用于IANA的IPv4特殊用途地址表。
            else if (ip[0] == 192 && ip[1] == 0 && ip[2] == 0) return true;
            //用于测试两个不同的子网的网间通信。
            else if (ip[0] == 192 && (ip[1] & 0xFE) == 18) return true;


            //效用域:主机

            //用于到本地主机的环回地址。
            else if (ip[0] == 127) return true;


            //效用域:文档

            //分配为用于文档和示例中的“TEST-NET”（测试网），它不应该被公开使用。
            else if (ip[0] == 192 && ip[1] == 0 && ip[2] == 2) return true;
            //分配为用于文档和示例中的“TEST-NET-2”（测试-网-2），它不应该被公开使用。
            else if (ip[0] == 192 && ip[1] == 51 && ip[2] == 100) return true;
            //分配为用于文档和示例中的“TEST-NET-3”（测试-网-3），它不应该被公开使用。
            else if (ip[0] == 203 && ip[1] == 0 && ip[2] == 113) return true;

            //效用域:子网

            //用于单链路的两个主机之间的本地链路地址，而没有另外指定IP地址，例如通常从DHCP服务器所检索到的IP地址。
            else if (ip[0] == 169 && ip[1] == 254) return true;
            else return false;
        }
    }
}
