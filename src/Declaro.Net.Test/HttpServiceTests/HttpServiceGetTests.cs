using Declaro.Net.Connection;
using Declaro.Net.Test.Helpers;
using Declaro.Net.Test.HttpServiceTests.Base;
using Declaro.Net.Test.TestDataTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Json;

namespace Declaro.Net.Test.HttpServiceTests
{
    public class HttpServiceGetTests : HttpServiceTestBase
    {
        [Fact]
        public async Task GetAsync_PassRequestObject()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, mockHttpClientFactory, memoryCache);
            var expectedUri = "api/weather?City=Budapest&Date=2023-09-22&District=13";

            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));
            
            // Act
            var response = await httpService.GetAsync<WeatherResponse, WeatherRequest>(
                new WeatherRequest()
                {
                    City = "Budapest",
                    Date = "2023-09-22"
                },
                queryParameters: ("District", "13"));

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetAsync_PassQueryParameters()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, mockHttpClientFactory, memoryCache);
            var expectedUri = "api/weather?City=Budapest&Date=2023-09-22&District=13";

            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));

            // Act
            var response = await httpService.GetAsync<WeatherResponse>(
                requestArguments: ["Budapest", "2023-09-22"],
                queryParameters: ("District", "13"));

            // Assert
            Assert.NotNull(response);
            Assert.Equal("Budapest", response.City);
            Assert.Equal(10, response.Celsius);

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task Validate_GetAsyncWithCaching()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            var mockHttpClientFactory = new MockHttpClientFactory(mockHttpMessageHandler, "http://127.0.0.1/");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Logger<HttpService>(LoggerFactory.Create(configure => { }));
            var httpService = new HttpService(logger, mockHttpClientFactory, memoryCache);
            var expectedUri = "api/weather?City=Budapest&Date=2023-09-22&District=13";

            mockHttpMessageHandler
                .Expect(HttpMethod.Get, $"http://127.0.0.1/{expectedUri}").Respond(HttpStatusCode.OK,
                    JsonContent.Create(new WeatherResponse() { Celsius = 10, City = "Budapest" }));

            var requestData = new WeatherRequest()
            {
                City = "Budapest",
                Date = "2023-09-22"
            };

            Thread.Sleep(3000);

            var cacheStillExist = memoryCache.TryGetValue(expectedUri, out _);

            // Act
            var cachedBeforeCall = memoryCache.TryGetValue(expectedUri, out _);
            var response1 = await httpService.GetAsync<CachedWeatherResponse>(
                requestArguments: [requestData.City, requestData.Date],
                queryParameters: ("District", "13"));

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();
            mockHttpMessageHandler.ResetExpectations();

            var cachedAfterCall = memoryCache.TryGetValue(expectedUri, out _);
            var response2 = await httpService.GetAsync<CachedWeatherResponse>(
                requestArguments: [requestData.City, requestData.Date],
                queryParameters: ("District", "13"));

            mockHttpMessageHandler.VerifyNoOutstandingExpectation();

            // Assert
            Assert.False(cachedBeforeCall);
            Assert.NotNull(response1);
            Assert.Equal("Budapest", response1.City);
            Assert.Equal(10, response1.Celsius);

            Assert.True(cachedAfterCall);
            Assert.NotNull(response2);
            Assert.Equal("Budapest", response2.City);
            Assert.Equal(10, response2.Celsius);

            Assert.False(cacheStillExist);
        }
    }
}