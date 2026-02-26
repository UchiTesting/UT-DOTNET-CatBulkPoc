using System;
using System.Collections.Generic;
using System.Data;

namespace CatBulk.Domain
{
    public class CatReader : IDataReader
    {
        private readonly IEnumerator<Cat> _enumerator;
        private bool _disposed;

        public CatReader(IEnumerable<Cat> source) { _enumerator = source.GetEnumerator(); }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CatReader));
        }

        public bool Read()
        {
            ThrowIfDisposed();
            return _enumerator.MoveNext();
        }
        public int FieldCount { get { return 7; } }
        public object GetValue(int i)
        {
            ThrowIfDisposed();
            Cat c = _enumerator.Current;
            switch (i)
            {
                case 0: return c.Name;
                case 1: return c.OwnerLastName;
                case 2: return c.OwnerFirstName;
                case 3: return c.Age;
                case 4: return c.Gender;
                case 5: return c.Fur;
                case 6: return c.EyeColor;
                default: throw new DataOutOfScopeException();
            }
        }
        public string GetName(int i)
        {
            switch (i)
            {
                case 0: return "Name";
                case 1: return "OwnerLastName";
                case 2: return "OwnerFirstName";
                case 3: return "Age";
                case 4: return "Gender";
                case 5: return "Fur";
                case 6: return "EyeColor";
                default: throw new DataOutOfScopeException();
            }
        }

        public int GetValues(object[] values)
        {
            ThrowIfDisposed();
            int count = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }
            return count;
        }

        public bool IsDBNull(int i)
        {
            ThrowIfDisposed();
            return GetValue(i) == null || GetValue(i) == DBNull.Value;
        }

        // Dispose pattern implementation
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // dispose managed resources
                _enumerator.Dispose();
            }

            // no unmanaged resources to release

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CatReader()
        {
            Dispose(false);
        }

        public bool IsClosed { get { return false; } }
        public int RecordsAffected { get { return -1; } }
        public void Close() { Dispose(); }
        public DataTable GetSchemaTable() { throw new NotImplementedException(); }
        public bool NextResult() { return false; }
        public int Depth { get { return 0; } }
        #region NotImplemented
        public bool GetBoolean(int i) { throw new NotImplementedException(); }
        public byte GetByte(int i) { throw new NotImplementedException(); }
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) { throw new NotImplementedException(); }
        public char GetChar(int i) { throw new NotImplementedException(); }
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) { throw new NotImplementedException(); }
        public IDataReader GetData(int i) { throw new NotImplementedException(); }
        public string GetDataTypeName(int i) { return "string"; }
        public DateTime GetDateTime(int i) { throw new NotImplementedException(); }
        public decimal GetDecimal(int i) { throw new NotImplementedException(); }
        public double GetDouble(int i) { throw new NotImplementedException(); }
        public Type GetFieldType(int i) { return typeof(string); }
        public float GetFloat(int i) { throw new NotImplementedException(); }
        public Guid GetGuid(int i) { throw new NotImplementedException(); }
        public short GetInt16(int i) { throw new NotImplementedException(); }
        public int GetInt32(int i) { throw new NotImplementedException(); }
        public long GetInt64(int i) { throw new NotImplementedException(); }
        public string GetString(int i) { return (string)GetValue(i); }
        public int GetOrdinal(string name)
        {
            switch (name)
            {
                case "Name": return 0;
                case "OwnerLastName": return 1;
                case "OwnerFirstName": return 2;
                case "Age": return 3;
                case "Gender": return 4;
                case "Fur": return 5;
                case "EyeColor": return 6;
                default: throw new DataOutOfScopeException();
            }
        }
        public object this[int i] { get { return GetValue(i); } }
        public object this[string name] { get { return GetValue(GetOrdinal(name)); } }
        #endregion
    }
}