// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web.Common.ModelBinders;
using Umbraco.Web.Common.Routing;
using Umbraco.Web.Models;
using Umbraco.Web.Routing;

namespace Umbraco.Tests.UnitTests.Umbraco.Web.Common.ModelBinders
{
    [TestFixture]
    public class ContentModelBinderTests
    {
        private ContentModelBinder _contentModelBinder;

        [SetUp]
        public void SetUp() => _contentModelBinder = new ContentModelBinder();

        [Test]
        [TestCase(typeof(IPublishedContent), false)]
        [TestCase(typeof(ContentModel), false)]
        [TestCase(typeof(ContentType1), false)]
        [TestCase(typeof(ContentModel<ContentType1>), false)]
        [TestCase(typeof(NonContentModel), true)]
        [TestCase(typeof(MyCustomContentModel), true)]
        [TestCase(typeof(IContentModel), true)]
        public void Returns_Binder_For_IPublishedContent_And_IRenderModel(Type testType, bool expectNull)
        {
            var binderProvider = new ContentModelBinderProvider();
            var contextMock = new Mock<ModelBinderProviderContext>();
            contextMock.Setup(x => x.Metadata).Returns(new EmptyModelMetadataProvider().GetMetadataForType(testType));

            IModelBinder found = binderProvider.GetBinder(contextMock.Object);
            if (expectNull)
            {
                Assert.IsNull(found);
            }
            else
            {
                Assert.IsNotNull(found);
            }
        }

        [Test]
        public async Task Does_Not_Bind_Model_When_UmbracoToken_Not_In_Route_Values()
        {
            // Arrange
            IPublishedContent pc = CreatePublishedContent();
            ModelBindingContext bindingContext = CreateBindingContextForUmbracoRequest(typeof(ContentModel), pc);
            bindingContext.ActionContext.RouteData.Values.Remove(Constants.Web.UmbracoRouteDefinitionDataToken);

            // Act
            await _contentModelBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
        }

        [Test]
        public async Task Does_Not_Bind_Model_When_UmbracoToken_Has_Incorrect_Model()
        {
            // Arrange
            IPublishedContent pc = CreatePublishedContent();
            ModelBindingContext bindingContext = CreateBindingContextForUmbracoRequest(typeof(ContentModel), pc);
            bindingContext.ActionContext.RouteData.Values[Constants.Web.UmbracoRouteDefinitionDataToken] = new NonContentModel();

            // Act
            await _contentModelBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
        }

        [Test]
        public async Task Bind_Model_When_UmbracoToken_Is_In_Route_Values()
        {
            // Arrange
            IPublishedContent pc = CreatePublishedContent();
            ModelBindingContext bindingContext = CreateBindingContextForUmbracoRequest(typeof(ContentModel), pc);

            // Act
            await _contentModelBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
        }

        [Test]
        public void Throws_When_Source_Not_Of_Expected_Type()
        {
            // Arrange
            IPublishedContent pc = CreatePublishedContent();
            var bindingContext = new DefaultModelBindingContext();

            // Act/Assert
            Assert.Throws<ModelBindingException>(() => _contentModelBinder.BindModel(bindingContext, new NonContentModel(), typeof(ContentModel)));
        }

        [Test]
        public void Binds_From_IPublishedContent_To_Content_Model()
        {
            // Arrange
            IPublishedContent pc = CreatePublishedContent();
            ModelBindingContext bindingContext = new DefaultModelBindingContext();

            // Act
            _contentModelBinder.BindModel(bindingContext, pc, typeof(ContentModel));

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
        }

        [Test]
        public void Binds_From_IPublishedContent_To_Content_Model_Of_T()
        {
            // Arrange
            IPublishedContent pc = CreatePublishedContent();
            ModelBindingContext bindingContext = new DefaultModelBindingContext();

            // Act
            _contentModelBinder.BindModel(bindingContext, new ContentModel<ContentType2>(new ContentType2(pc)), typeof(ContentModel<ContentType1>));

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
        }

        [Test]
        public void BindModel_Null_Source_Returns_Null()
        {
            var bindingContext = new DefaultModelBindingContext();
            _contentModelBinder.BindModel(bindingContext, null, typeof(ContentType1));
            Assert.IsNull(bindingContext.Result.Model);
        }

        [Test]
        public void BindModel_Returns_If_Same_Type()
        {
            var content = new ContentType1(Mock.Of<IPublishedContent>());
            var bindingContext = new DefaultModelBindingContext();

            _contentModelBinder.BindModel(bindingContext, content, typeof(ContentType1));

            Assert.AreSame(content, bindingContext.Result.Model);
        }

        [Test]
        public void BindModel_RenderModel_To_IPublishedContent()
        {
            var content = new ContentType1(Mock.Of<IPublishedContent>());
            var renderModel = new ContentModel(content);

            var bindingContext = new DefaultModelBindingContext();
            _contentModelBinder.BindModel(bindingContext, renderModel, typeof(IPublishedContent));

            Assert.AreSame(content, bindingContext.Result.Model);
        }

        [Test]
        public void BindModel_IPublishedContent_To_RenderModel()
        {
            var content = new ContentType1(Mock.Of<IPublishedContent>());
            var bindingContext = new DefaultModelBindingContext();

            _contentModelBinder.BindModel(bindingContext, content, typeof(ContentModel));
            var bound = (IContentModel)bindingContext.Result.Model;

            Assert.AreSame(content, bound.Content);
        }

        [Test]
        public void BindModel_IPublishedContent_To_Generic_RenderModel()
        {
            var content = new ContentType1(Mock.Of<IPublishedContent>());
            var bindingContext = new DefaultModelBindingContext();

            _contentModelBinder.BindModel(bindingContext, content, typeof(ContentModel<ContentType1>));
            var bound = (IContentModel)bindingContext.Result.Model;

            Assert.AreSame(content, bound.Content);
        }

        [Test]
        public void Null_Model_Binds_To_Null()
        {
            IPublishedContent pc = Mock.Of<IPublishedContent>();
            var bindingContext = new DefaultModelBindingContext();
            _contentModelBinder.BindModel(bindingContext, null, typeof(ContentModel));
            Assert.IsNull(bindingContext.Result.Model);
        }

        [Test]
        public void Invalid_Model_Type_Throws_Exception()
        {
            IPublishedContent pc = Mock.Of<IPublishedContent>();
            var bindingContext = new DefaultModelBindingContext();
            Assert.Throws<ModelBindingException>(() => _contentModelBinder.BindModel(bindingContext, "Hello", typeof(IPublishedContent)));
        }

        /// <summary>
        /// Creates a binding context with the route values populated to similute an Umbraco dynamically routed request
        /// </summary>
        private ModelBindingContext CreateBindingContextForUmbracoRequest(Type modelType, IPublishedContent publishedContent)
        {
            var builder = new PublishedRequestBuilder(new Uri("https://example.com"), Mock.Of<IFileService>());
            builder.SetPublishedContent(publishedContent);
            IPublishedRequest publishedRequest = builder.Build();

            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            routeData.Values.Add(Constants.Web.UmbracoRouteDefinitionDataToken, new UmbracoRouteValues(publishedRequest));
            {
            }

            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
            var metadataProvider = new EmptyModelMetadataProvider();
            var routeValueDictionary = new RouteValueDictionary();
            var valueProvider = new RouteValueProvider(BindingSource.Path, routeValueDictionary);
            return new DefaultModelBindingContext
            {
                ActionContext = actionContext,
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = modelType.Name,
                ValueProvider = valueProvider,
            };
        }

        private class NonContentModel
        {
        }

        private IPublishedContent CreatePublishedContent() => new ContentType2(new Mock<IPublishedContent>().Object);

        public class ContentType1 : PublishedContentWrapped
        {
            public ContentType1(IPublishedContent content)
                : base(content)
            {
            }
        }

        public class ContentType2 : ContentType1
        {
            public ContentType2(IPublishedContent content)
                : base(content)
            {
            }
        }

        public class MyCustomContentModel : ContentModel
        {
            public MyCustomContentModel(IPublishedContent content)
                : base(content)
            { }
        }
    }
}
