﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Protocol;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// Contains a list of all mods installed on the server or client.
    /// Sent from the client to the server first, and then the server responds with its mod list.
    /// The server's mod list matches the one sent in the ping.
    /// </summary>
    public class ModList : IList<ModInfo>, IForgeStructure, IEquatable<ModList>
    {
        /// <summary>Always 2 for ModList</summary>
        public const byte Discriminator = 2;

        public int Count => _modList.Count;

        public bool IsReadOnly => false;

        public ModInfo this[int index] { get => _modList[index]; set => _modList[index] = value; }

        private List<ModInfo> _modList;

        public ModList() { this._modList = new List<ModInfo>(); }
        public ModList(params ModInfo[] mods) : this((IEnumerable<ModInfo>)mods) { }
        public ModList(IEnumerable<ModInfo> mods):this()
        {
            if (mods is null)
                throw new ArgumentNullException(nameof(mods));

            this._modList.AddRange(mods);
        }

        public byte[] ToBytes()
        {
            byte[] data;
            using (MinecraftMemoryStream ms = new MinecraftMemoryStream())
            {
                ms.WriteByte(Discriminator);
                ms.WriteVarInt(_modList.Count);
                foreach (var mod in _modList)
                {
                    ms.WriteString(mod.Name);
                    ms.WriteString(mod.Version);
                }
                data = ms.ToArray();
            }
            return data;
        }
        public static ModList Read(List<byte> data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "data length too short");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            ModList ML = new ModList();
            ReadOnlySpan<byte> buffer = data.ToArray().AsSpan().Slice(1);
            buffer.ReadVarInt(out int ModCount).ReadStringArray(out string[] array, ModCount * 2);
            for (int i = 0; i < ModCount; i++)
                ML.Add(new ModInfo(array[i + i], array[i + i + 1]));

            return ML;
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendJoin(",", _modList);
            return sb.ToString();
        }

        public override bool Equals(object obj) => Equals(obj as ModList);
        public bool Equals(ModList other) => ProtocolHandler.Compare(this._modList, other?._modList);
        public bool Equals(ModInfo[] other) => ProtocolHandler.Compare(this._modList, other);
        public bool Equals(List<ModInfo> other) => ProtocolHandler.Compare(this._modList, other);

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, _modList);
        }

        public ModInfo[] ToArray()
        {
            return _modList.ToArray();
        }
        public IEnumerator<ModInfo> GetEnumerator()
        {
            return _modList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _modList.GetEnumerator();
        }

        public int IndexOf(ModInfo item)
        {
            return _modList.IndexOf(item);
        }

        public void Insert(int index, ModInfo item)
        {
            _modList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _modList.RemoveAt(index);
        }

        public void Add(ModInfo item)
        {
            _modList.Add(item);
        }

        public void AddRange(IEnumerable<ModInfo> mods)
        {
            _modList.AddRange(mods);
        }

        public void Clear()
        {
            _modList.Clear();
        }

        public bool Contains(ModInfo item)
        {
            return _modList.Contains(item);
        }

        public void CopyTo(ModInfo[] array, int arrayIndex)
        {
            _modList.CopyTo(array, arrayIndex);
        }

        public bool Remove(ModInfo item)
        {
            return _modList.Remove(item);
        }
    }
}
