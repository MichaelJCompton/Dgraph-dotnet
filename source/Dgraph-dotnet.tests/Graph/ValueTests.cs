using System;
using System.Globalization;
using System.Text;
using DgraphDotNet.Graph;
using NUnit.Framework;

namespace Dgraph_dotnet.tests.Graph {
    public class ValueTests {
        [SetUp]
        public void Setup() { }

        [Test]
        public void ValuesAreRightType() {
            Assert.IsTrue(GraphValue.BuildBoolValue(true).IsBoolValue);
            Assert.IsTrue(GraphValue.BuildBytesValue(new byte[] { 0x20, 0x20, 0x20 }).IsBytesValue);
            Assert.IsTrue(GraphValue.BuildDateValue(
                    Encoding.UTF8.GetBytes(
                        DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo)))
                .IsDateValue);
            Assert.IsTrue(GraphValue.BuildDateValue(DateTime.Now).IsDateValue);
            Assert.IsTrue(GraphValue.BuildDefaultValue("blaa").IsDefaultValue);
            Assert.IsTrue(GraphValue.BuildDoubleValue(123).IsDoubleValue);
            Assert.IsTrue(GraphValue.BuildGeoValue(
                    Encoding.UTF8.GetBytes("{'type':'Point','coordinates':[-122.4220186,37.772318]}"))
                .IsGeoValue);
            Assert.IsTrue(GraphValue.BuildGeoValue("{'type':'Point','coordinates':[-122.4220186,37.772318]}").IsGeoValue);
            Assert.IsTrue(GraphValue.BuildIntValue(123).IsIntValue);
            Assert.IsTrue(GraphValue.BuildPasswordValue("secret").IsPasswordValue);
            Assert.IsTrue(GraphValue.BuildStringValue("something").IsStringValue);
        }

        [Test]
        public void ValuesAreRight() {
            Assert.AreEqual(
                true,
                GraphValue.BuildBoolValue(true).BoolValue);

            Assert.AreEqual(
                new byte[] { 0x20, 0x20, 0x20 },
                GraphValue.BuildBytesValue(new byte[] { 0x20, 0x20, 0x20 }).Bytesvalue);

            var now = DateTime.Now;
            Assert.AreEqual(
                Encoding.UTF8.GetBytes(
                    now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo)),
                GraphValue.BuildDateValue(
                    Encoding.UTF8.GetBytes(
                        now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo))).DateValue);

            var valNow = GraphValue.BuildDateValue(now);
            Assert.AreEqual(
                now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo),
                Encoding.UTF8.GetString(valNow.DateValue, 0, valNow.DateValue.Length));

            Assert.AreEqual(
                "blaa",
                GraphValue.BuildDefaultValue("blaa").DefaultValue);

            Assert.AreEqual(
                123,
                GraphValue.BuildDoubleValue(123).DoubleValue);

            Assert.AreEqual(
                Encoding.UTF8.GetBytes("{'type':'Point','coordinates':[-122.4220186,37.772318]}"),
                GraphValue.BuildGeoValue(
                    Encoding.UTF8.GetBytes("{'type':'Point','coordinates':[-122.4220186,37.772318]}")).GeoValue);

            var geojson = "{'type':'Point','coordinates':[-122.4220186,37.772318]}";
            var geojsonVal = GraphValue.BuildGeoValue(geojson);
            Assert.AreEqual(
                geojson,
                Encoding.UTF8.GetString(geojsonVal.GeoValue, 0, geojsonVal.GeoValue.Length));

            Assert.AreEqual(
                123,
                GraphValue.BuildIntValue(123).IntValue);

            Assert.AreEqual(
                "secret",
                GraphValue.BuildPasswordValue("secret").PasswordValue);

            Assert.AreEqual(
                "something",
                GraphValue.BuildStringValue("something").StringValue);
        }

        [Test]
        public void ToStringWorks() {
            Assert.AreEqual(
                "True",
                GraphValue.BuildBoolValue(true).ToString());

            var bytes = System.Text.Encoding.UTF8.GetBytes("test");
            Assert.AreEqual(
                System.Text.Encoding.UTF8.GetString(bytes),
                GraphValue.BuildBytesValue(bytes).ToString());

            var now = DateTime.Now;
            var format = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";
            Assert.AreEqual(
                    now.ToString(format, DateTimeFormatInfo.InvariantInfo),
                GraphValue.BuildDateValue(
                    Encoding.UTF8.GetBytes(
                        now.ToString(format, DateTimeFormatInfo.InvariantInfo))).ToString());

            var geojson = "{'type':'Point','coordinates':[-122.4220186,37.772318]}";
            var geojsonVal = GraphValue.BuildGeoValue(geojson);
            Assert.AreEqual(
                geojson,
                geojsonVal.ToString());
        }
    }
}