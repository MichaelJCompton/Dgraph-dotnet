using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Google.Protobuf;
using static Api.Value;

namespace DgraphDotNet.Graph {
    public class GraphValue : IEdgeTarget {

        internal readonly Api.Value Value = new Api.Value();

        private GraphValue() { }

        private GraphValue(Api.Value value) {
            Value = value;
        }

        internal static GraphValue BuildFromValue(Api.Value value) {
            return new GraphValue(value);
        }

        public static GraphValue BuildDefaultValue(string defaultVal) {
            GraphValue result = new GraphValue();
            result.Value.DefaultVal = defaultVal;
            return result;
        }

        /// <remarks>Precondition : <c><paramref name="bytesVal"/> != null</c></remarks>
        public static GraphValue BuildBytesValue(byte[] bytesVal) {
            GraphValue result = new GraphValue();
            result.Value.BytesVal = ByteString.CopyFrom(bytesVal);
            return result;
        }

        /// <remarks> A Dgraph (go) int, not a C# int. </remarks>
        public static GraphValue BuildIntValue(long intVal) {
            GraphValue result = new GraphValue();
            result.Value.IntVal = intVal;
            return result;
        }

        public static GraphValue BuildBoolValue(bool boolVal) {
            GraphValue result = new GraphValue();
            result.Value.BoolVal = boolVal;
            return result;
        }

        public static GraphValue BuildStringValue(string stringVal) {
            GraphValue result = new GraphValue();
            result.Value.StrVal = stringVal;
            return result;
        }

        public static GraphValue BuildDoubleValue(double doubleVal) {
            GraphValue result = new GraphValue();
            result.Value.DoubleVal = doubleVal;
            return result;
        }

        /// <remarks>Precondition : <c><paramref name="geoVal"/> != null</c></remarks>
        public static GraphValue BuildGeoValue(byte[] geoVal) {
            GraphValue result = new GraphValue();
            result.Value.GeoVal = ByteString.CopyFrom(geoVal);
            return result;
        }

        /// <remarks>Precondition : <c><paramref name="datetime"/> != null</c></remarks>
        public static GraphValue BuildGeoValue(string geojson) {
            GraphValue result = new GraphValue();
            result.Value.GeoVal = ByteString.CopyFrom(
                Encoding.UTF8.GetBytes(geojson));
            return result;
        }

        /// <remarks>Precondition : <c><paramref name="dateVal"/> != null</c></remarks>
        public static GraphValue BuildDateValue(byte[] dateVal) {
            GraphValue result = new GraphValue();
            result.Value.DatetimeVal = ByteString.CopyFrom(dateVal);
            return result;
        }

        /// <remarks>Precondition : <c><paramref name="datetime"/> != null</c></remarks>
        public static GraphValue BuildDateValue(DateTime datetime) {
            GraphValue result = new GraphValue();
            result.Value.DatetimeVal = ByteString.CopyFrom(
                Encoding.UTF8.GetBytes(
                    datetime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo)));
            return result;
        }

        public static GraphValue BuildPasswordValue(string passwordVal) {
            GraphValue result = new GraphValue();
            result.Value.PasswordVal = passwordVal;
            return result;
        }

        public bool IsDefaultValue => Value.ValCase.Equals(Api.Value.ValOneofCase.DefaultVal);
        public bool IsBytesValue => Value.ValCase.Equals(Api.Value.ValOneofCase.BytesVal);
        public bool IsIntValue => Value.ValCase.Equals(Api.Value.ValOneofCase.IntVal);
        public bool IsBoolValue => Value.ValCase.Equals(Api.Value.ValOneofCase.BoolVal);
        public bool IsStringValue => Value.ValCase.Equals(Api.Value.ValOneofCase.StrVal);
        public bool IsDoubleValue => Value.ValCase.Equals(Api.Value.ValOneofCase.DoubleVal);
        public bool IsGeoValue => Value.ValCase.Equals(Api.Value.ValOneofCase.GeoVal);
        public bool IsDateValue => Value.ValCase.Equals(Api.Value.ValOneofCase.DatetimeVal);
        public bool IsPasswordValue => Value.ValCase.Equals(Api.Value.ValOneofCase.PasswordVal);

        // these return defaults if the corresponding Is.. fn is not true
        public string DefaultValue => Value.DefaultVal;
        public byte[] Bytesvalue => Value.BytesVal.ToByteArray();
        public long IntValue => Value.IntVal;
        public bool BoolValue => Value.BoolVal;
        public string StringValue => Value.StrVal;
        public double DoubleValue => Value.DoubleVal;
        public byte[] GeoValue => Value.GeoVal.ToByteArray();
        public byte[] DateValue => Value.DatetimeVal.ToByteArray();
        public string PasswordValue => Value.PasswordVal;

        public override string ToString() {
            switch (Value.ValCase) {
                case ValOneofCase.DefaultVal:
                    return Value.DefaultVal;
                case ValOneofCase.BytesVal:
                    return Value.BytesVal.ToStringUtf8();
                case ValOneofCase.IntVal:
                    return Value.IntVal.ToString();
                case ValOneofCase.BoolVal:
                    return Value.BoolVal.ToString();
                case ValOneofCase.StrVal:
                    return Value.StrVal;
                case ValOneofCase.DoubleVal:
                    return Value.DoubleVal.ToString();
                case ValOneofCase.GeoVal:
                    return Value.GeoVal.ToStringUtf8();
                case ValOneofCase.DateVal:
                    return Value.DateVal.ToStringUtf8();
                case ValOneofCase.DatetimeVal:
                    return Value.DatetimeVal.ToStringUtf8();
                case ValOneofCase.PasswordVal:
                    return Value.PasswordVal;
                case ValOneofCase.UidVal:
                    return Value.UidVal.ToString();
            }
            return "";
        }
    }

}