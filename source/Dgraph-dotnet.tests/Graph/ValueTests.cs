using System;
using System.Globalization;
using NUnit.Framework;
using DgraphDotNet.Graph;
using System.Text;

namespace Dgraph_dotnet.tests.Graph
{
    public class ValueTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ValuesAreRightType()
        {
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
        public void ValuesAreRight()
        {
            Assert.AreEqual(
                GraphValue.BuildBoolValue(true).BoolValue,
                true);

            Assert.AreEqual(
                GraphValue.BuildBytesValue(new byte[] { 0x20, 0x20, 0x20 }).Bytesvalue,
                new byte[] { 0x20, 0x20, 0x20 });

            var now = DateTime.Now;
            Assert.AreEqual(GraphValue.BuildDateValue(
                Encoding.UTF8.GetBytes(
                     now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo))).DateValue,
                Encoding.UTF8.GetBytes(
                     now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo)));

            var valNow = GraphValue.BuildDateValue(now);
            Assert.AreEqual(
                Encoding.UTF8.GetString(valNow.DateValue, 0, valNow.DateValue.Length), 
                now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo));

            Assert.AreEqual(
                GraphValue.BuildDefaultValue("blaa").DefaultValue,
                "blaa");

            Assert.AreEqual(
                GraphValue.BuildDoubleValue(123).DoubleValue,
                123);

            Assert.AreEqual(
                GraphValue.BuildGeoValue(
                 Encoding.UTF8.GetBytes("{'type':'Point','coordinates':[-122.4220186,37.772318]}")).GeoValue,
                 Encoding.UTF8.GetBytes("{'type':'Point','coordinates':[-122.4220186,37.772318]}"));

            var geojson = "{'type':'Point','coordinates':[-122.4220186,37.772318]}";
            var geojsonVal = GraphValue.BuildGeoValue(geojson);
            Assert.AreEqual(
                Encoding.UTF8.GetString(geojsonVal.GeoValue, 0, geojsonVal.GeoValue.Length),
                geojson
            );

            Assert.AreEqual(
                GraphValue.BuildIntValue(123).IntValue,
                123);

            Assert.AreEqual(
                GraphValue.BuildPasswordValue("secret").PasswordValue,
                "secret");

            Assert.AreEqual(
                GraphValue.BuildStringValue("something").StringValue,
                "something");
        }
    }
}