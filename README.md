# Buccaneer
A netcore3.1 generator for Swagger (OpenAPI) Specs that use OAuth2 and OpenIDConnect for authentication, 
without the fuss and dependencies of Swashbuckle.

Fast and simple (<600 loc) and it's only dependency is the Microsoft OpenAPI library package.

Consists only of a few methods and a couple of decorator attributes, so it's ultra simple to get started.

Usage :

1. Generate a swagger defintiion in YAML from inside C# code for an API using Azure AD B2C to authenticate via the implicit flow.

            return new Buccaneer.SwaggerGenerator().GetSwaggerAsHttpResponseMessageWithYAMLString(
                _ass: Assembly.GetExecutingAssembly(),
                _title: "Wills Blog API",
                _apiversion: "0.1",
                _description: "A sample blog API",
                _contactname: "Will Eastbury",
                _contactemail: "willeastbury@gmail.com",
                _apiServerPaths: new List<string>() { "http://localhost:7071/api", "https://www.willeastbury.com/api" },
                _oauth2scopes: new Dictionary<string, string> { { "https://threeshadesb2c.onmicrosoft.com/willeastburycom/connect", "Connect" }},
                _headerapikeyname: null,
                _oauth2AuthUrl: "https://sample.b2clogin.com/threeshadesb2c.onmicrosoft.com/oauth2/v2.0/authorize?p=B2C_1_willeastbury.com",
                _securitySchemeName: "oauth2"
                );
        }

2a. Somewhere in your assembly, you need to decorate **something** with one of the following attributes, this can be a concrete class OR an interface (Unlike Swashbuckle, this doesn't have to be a asp.net Web api call).

Methods can be decorated with TWO attributes

- OpenApiTagInputParameterAttribute() which tells the engine which class your method expects to receive and we will serialize that to the output, applying recursion for nested types. 

- OpenApiTagMethodAttribute() This tells the engine that you intend to expose this method over swagger, simply add this attribute to the methods that you want to expose.

Here's a sample that exposes an *interface* with no concrete implementation, which is awesome for mocking an API, or for example building a swagger for a service that doesn't use c#, like a logic app, or a facade implemented in Azure Functions Proxies.

    public interface ITenantAPI
    {
        [OpenApiTagMethod("Tenant", "Add Tenant", "Adds a BlogTenant to the platform", new int[] { 200, 404 }, "BlogTenant", "oauth2", "", route: "Tenants", methodlist: "post")]
        public Task<IActionResult> AddTenant();

        [OpenApiTagInputParameter("tenantId", true, "string", "Path")]
        [OpenApiTagMethod("Tenant", "Delete Tenant", "Deletes a BlogTenant from the platform", new int[] { 200, 404 }, "BlogTenant", "oauth2", "", route: "Tenants/{tenantId}", methodlist: "delete")]
        public Task<IActionResult> DeleteTenant();

    }
    
2b. Here's an example of generating a swagger for an http bound Azure Function (and yes it works with netcore 3.1 and the new v3 functions runtime) behind AAD easyauth with an AAD B2C domain.

using static Buccaneer.SwaggerGenerator;

namespace Threeshades_Blog_Engine
{
    public class Categories
    {

        [FunctionName("GetCategoryData")]
        [OpenApiTagMethod("Category", "Get Category Data", "Retrieves categories on a blog", new int[] { 200, 409 }, "ListOfCategories", null, null, route: "{tenantId}/Categories", methodlist: "get")]
        [OpenApiTagInputParameter("tenantId", true, "string", "Path")]
        public static async Task<IActionResult> GetCategoryData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{tenantId:alpha}/Categories")] HttpRequest req,
            [Blob("categories-{tenantId}", FileAccess.ReadWrite)] CloudBlobContainer cbc,
            string tenantId,
            ILogger log)
        {

            log.LogInformation($"C# HTTP trigger function processed a request. for categories for {tenantId}");
            CloudBlockBlob cbb = cbc.GetBlockBlobReference($"categorylist-{tenantId}.blob");
            return (ActionResult)new OkObjectResult(await cbb.DownloadTextAsync());

        }
   }
}

And boom, you're done, it really is **that** simple ! 

Instant Swagger Definition, you can also call GetSwaggerAsJSONString() if you want a JSON swagger instead of a YAML one.
    
