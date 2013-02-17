﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Xunit;
using Should;
using Moq;
using System.Net;

namespace WebApiContrib.Formatting.CollectionJson.Tests
{
    public class CollectionJsonControllerTests
    {
        private Mock<ICollectionJsonDocumentReader<string>> reader;
        private Mock<ICollectionJsonDocumentWriter<string>> writer;
        private TestController controller;
        private CollectionJsonFormatter formatter = new CollectionJsonFormatter();
        private HttpControllerContext context;
        private ReadDocument testReadDocument;
        private WriteDocument testWriteDocument;
 
        public CollectionJsonControllerTests()
        {
            reader = new Mock<ICollectionJsonDocumentReader<string>>();
            writer = new Mock<ICollectionJsonDocumentWriter<string>>();

            testReadDocument = new ReadDocument();
            testReadDocument.Collection.Href = new Uri("http://test.com");
            testWriteDocument = new WriteDocument();

            writer.Setup(w => w.Write(It.IsAny<IEnumerable<string>>())).Returns(testReadDocument);
            reader.Setup(r => r.Read(It.IsAny<WriteDocument>())).Returns("Test");
            Configure();
        }

        private void Configure()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test/");
            var config = new HttpConfiguration();
            config.Formatters.Add(new CollectionJsonFormatter());
            var route = config.Routes.MapHttpRoute("DefaultApi", "{controller}/{id}", new { id = RouteParameter.Optional });
            var routeData = new HttpRouteData(route, new HttpRouteValueDictionary { { "controller", "test" } });

            controller = new TestController(writer.Object, reader.Object);
            controller.ControllerContext = new HttpControllerContext(config, routeData, request);
            controller.ControllerContext.ControllerDescriptor = new HttpControllerDescriptor(config, "test", typeof(TestController));
            controller.Request = request;
            controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            controller.Request.Properties.Add(HttpPropertyKeys.HttpRouteDataKey, routeData);
        }

        [Fact]
        public void WhenGettingAllShouldReturnDocument()
        {
            var response = controller.Get();
            var value = response.Content.ReadAsAsync<ReadDocument>().Result;
            value.Collection.Href.AbsoluteUri.ShouldEqual("http://test.com/");
        }

        [Fact]
        public void WhenGettingSingleShouldReturnDocument()
        {
            var response = controller.Get(1);
            var value = response.Content.ReadAsAsync<ReadDocument>().Result;
            value.Collection.Href.AbsoluteUri.ShouldEqual("http://test.com/");
        }

        [Fact]
        public void WhenPostingShouldSetLocationHeader()
        {
            controller.Request.Method = HttpMethod.Post;
            var response = controller.Post(testWriteDocument);
            response.Headers.Location.AbsoluteUri.ShouldEqual("http://localhost/test/1");
        }

        [Fact]
        public void WhenPostingShouldSetStatusToCreated()
        {
            controller.Request.Method = HttpMethod.Post;
            var response = controller.Post(testWriteDocument);
            response.StatusCode.ShouldEqual(HttpStatusCode.Created);
        }

        [Fact]
        public void WhenPostingShouldCallCreate()
        {
            controller.Request.Method = HttpMethod.Post;
            var response = controller.Post(testWriteDocument);
            controller.CreateCalled.ShouldBeTrue();
        }

        [Fact]
        public void WhenPutShouldReturnDocument()
        {
            controller.Request.Method = HttpMethod.Put;
            var response = controller.Put(1,testWriteDocument);
            var value = response.Content.ReadAsAsync<ReadDocument>().Result;
            value.Collection.Href.AbsoluteUri.ShouldEqual("http://test.com/");
        }

        [Fact]
        public void WhenPutShouldCallUpdate()
        {
            controller.Request.Method = HttpMethod.Put;
            var response = controller.Put(1, testWriteDocument);
            controller.UpdateCalled.ShouldBeTrue();
        }

        [Fact]
        public void WhenRemoveShouldCallDelete()
        {
            controller.Request.Method = HttpMethod.Delete;
            var response = controller.Remove(1);
            controller.DeleteCalled.ShouldBeTrue();
        }
    }
    
    //needed for overriding protected methods
    public class TestController : CollectionJsonController<string>
    {
        public const string TestValue = "Test";

        public TestController(ICollectionJsonDocumentWriter<string> writer, ICollectionJsonDocumentReader<string> reader)
            : base(writer,reader)
        {
        }

        public bool CreateCalled;
        public bool ReadAllCalled;
        public bool ReadSingleCalled;
        public bool UpdateCalled;
        public bool DeleteCalled;

        protected override int Create(string data, System.Net.Http.HttpResponseMessage response)
        {
            CreateCalled = true;
            return 1;
        }

        protected override string Read(int id, System.Net.Http.HttpResponseMessage response)
        {
            ReadSingleCalled = true;
            return TestValue;
        }

        protected override IEnumerable<string> Read(System.Net.Http.HttpResponseMessage response)
        {
            ReadAllCalled = true;
            return new []{TestValue};
        }

        protected override string Update(int id, string data, System.Net.Http.HttpResponseMessage response)
        {
            UpdateCalled = true;
            return data;
        }

        protected override void Delete(int id, System.Net.Http.HttpResponseMessage response)
        {
            DeleteCalled = true;
        }
    }
}