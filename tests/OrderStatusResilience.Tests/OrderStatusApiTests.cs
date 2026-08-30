using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OrderStatusResilience.Tests;

[TestClass]
public sealed class OrderStatusApiTests
{
    [TestMethod]
    public async Task Success_returns_the_order_on_the_first_attempt()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/order/status/123?scenario=success");
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("shipped", payload.RootElement.GetProperty("status").GetString());
        Assert.AreEqual(1, payload.RootElement.GetProperty("attempts").GetInt32());
    }

    [TestMethod]
    public async Task Transient_failure_is_retried_until_it_succeeds()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/order/status/123?scenario=transientFailure");
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(3, payload.RootElement.GetProperty("attempts").GetInt32());
    }

    [TestMethod]
    public async Task Persistent_failure_returns_service_unavailable_after_retries()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/order/status/123?scenario=persistentFailure");
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual(3, payload.RootElement.GetProperty("attempts").GetInt32());
    }

    [TestMethod]
    public async Task Timeout_returns_gateway_timeout_after_bounded_attempts()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/order/status/123?scenario=timeout");
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.AreEqual(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.AreEqual(3, payload.RootElement.GetProperty("attempts").GetInt32());
    }

    [TestMethod]
    public async Task Repeated_failures_open_the_circuit()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/order/status/first?scenario=persistentFailure");
        using var secondResponse = await client.GetAsync("/order/status/second?scenario=persistentFailure");
        using var payload = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, secondResponse.StatusCode);
        StringAssert.Contains(payload.RootElement.GetProperty("title").GetString(), "circuit is open");
    }

    [TestMethod]
    public async Task Health_endpoint_reports_the_dependency_as_healthy()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", payload.GetProperty("status").GetString());
    }
}
