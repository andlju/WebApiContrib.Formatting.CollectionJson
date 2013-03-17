using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Should;
using Xunit;

namespace WebApiContrib.Formatting.CollectionJson.Tests
{
    public class TestOutputOnly
    {
        public string StringProperty { get; set; }
        public int IntegerProperty { get; set; }
    }

    public class TestInputOnly
    {
        public string StringProperty { get; set; }
        public int IntegerProperty { get; set; }
    }

    public class TestInputAndOutput
    {
        public string StringProperty { get; set; }
        public int IntegerProperty { get; set; }
    }

    public class TestOutputWriter : ICollectionJsonDocumentWriter<TestOutputOnly>
    {
        public ReadDocument Write(IEnumerable<TestOutputOnly> data)
        {
            var doc = new ReadDocument();
            var item = new Item();
            foreach (var dataItem in data)
            {
                item.Data.Add(new Data() { Name = "stringProperty", Value = dataItem.StringProperty });
                item.Data.Add(new Data() { Name = "integerProperty", Value = dataItem.IntegerProperty.ToString() });
                doc.Collection.Items.Add(item);
            }
            return doc;
        }
    }

    public class TestInputReader : ICollectionJsonDocumentReader<TestInputOnly>
    {
        public TestInputOnly Read(WriteDocument document)
        {
            var testInputOnly = new TestInputOnly();
            testInputOnly.StringProperty = document.Template.Data.Single(d => d.Name == "stringProperty").Value;
            testInputOnly.IntegerProperty = int.Parse(document.Template.Data.Single(d => d.Name == "integerProperty").Value);
            
            return testInputOnly;
        }
    }

    public class TestInputAndOutputReaderAndWriter : ICollectionJsonDocumentReader<TestInputAndOutput>, ICollectionJsonDocumentWriter<TestInputAndOutput>
    {
        public TestInputAndOutput Read(WriteDocument document)
        {
            var testInputAndOutput = new TestInputAndOutput();
            testInputAndOutput.StringProperty = document.Template.Data.Single(d => d.Name == "stringProperty").Value;
            testInputAndOutput.IntegerProperty = int.Parse(document.Template.Data.Single(d => d.Name == "integerProperty").Value);

            return testInputAndOutput;
        }

        public ReadDocument Write(IEnumerable<TestInputAndOutput> data)
        {
            var doc = new ReadDocument();
            var item = new Item();
            foreach (var dataItem in data)
            {
                item.Data.Add(new Data() { Name = "stringProperty", Value = dataItem.StringProperty });
                item.Data.Add(new Data() { Name = "integerProperty", Value = dataItem.IntegerProperty.ToString() });
                doc.Collection.Items.Add(item);
            }
            return doc;
        }
    }

    public class TypeMappedFormatterTest
    {
        private TypeMappedCollectionJsonFormatter formatter = new TypeMappedCollectionJsonFormatter();

        public TypeMappedFormatterTest()
        {
            formatter.RegisterWriter(new TestOutputWriter());
            formatter.RegisterReader(new TestInputReader());
            formatter.RegisterReader(new TestInputAndOutputReaderAndWriter());
            formatter.RegisterWriter(new TestInputAndOutputReaderAndWriter());
        }

        [Fact]
        public void WhenWriterIsRegisteredShouldBeAbleToWriteSingleItem()
        {
            formatter.CanWriteType(typeof(TestOutputOnly)).ShouldBeTrue();
        }

        [Fact]
        public void WhenWriterIsRegisteredShouldBeAbleToWriteEnumerableOfItems()
        {
            formatter.CanWriteType(typeof(IEnumerable<TestOutputOnly>)).ShouldBeTrue();
        }

        [Fact]
        public void WhenWriterIsNotRegisteredShouldNotBeAbleToWriteSingleItem()
        {
            formatter.CanWriteType(typeof(TestInputOnly)).ShouldBeFalse();
        }

        [Fact]
        public void WhenWriterIsNotRegisteredShouldNotBeAbleToWriteEnumerableOfItems()
        {
            formatter.CanWriteType(typeof(IEnumerable<TestInputOnly>)).ShouldBeFalse();
        }

        [Fact]
        public void WhenReadingRegisteredTypeShouldReturnInstance()
        {
            var obj = JObject.FromObject(new
                                             {
                                                 template = new
                                                                {
                                                                    data = new object[]
                                                                               {
                                                                                   new {name = "stringProperty", value = "Hello World"},
                                                                                   new {name = "integerProperty", value = "1337"}
                                                                               }
                                                                }
                                             });

            
            var jsonDoc = obj.ToString();
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(jsonDoc);
            writer.Flush();

            HttpContent content = new StringContent(String.Empty);
            HttpContentHeaders contentHeaders = content.Headers;
            contentHeaders.ContentLength = stream.Length;
            contentHeaders.ContentType = new MediaTypeHeaderValue("application/vnd.collection+json");

            stream.Seek(0, SeekOrigin.Begin);

            var newDoc = formatter.ReadFromStreamAsync(typeof(TestInputOnly), stream, content, null).Result;//  as WriteDocument;

            newDoc.ShouldNotBeNull();
            newDoc.ShouldBeType<TestInputOnly>();
        }

        [Fact]
        public void WhenWritingRegisteredTypeShouldReturnJson()
        {
            var testObj = new TestOutputOnly() {StringProperty = "Hello World", IntegerProperty = 1337};

            var stream = new MemoryStream();

            formatter.WriteToStreamAsync(typeof(TestOutputOnly), testObj, stream, null, null).Wait();

            stream.Seek(0, SeekOrigin.Begin);

            var result = new StreamReader(stream).ReadToEnd();
            result.ShouldNotBeNull();
            result.ShouldContain("Hello World");
        }
    }
}